// Updated: 2025-02-14
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Model;
using FUnity.Runtime.View;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Input;
using FUnity.Runtime.UI;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FUnity.Core
{
    /// <summary>
    /// FUnity ランタイム全体をオーケストレーションする Presenter 層の MonoBehaviour。
    /// UI ドキュメント、俳優、Visual Scripting Runner を初期化し、Visual Scripting から Presenter への命令経路を構築する。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnityProjectData"/>, <see cref="UIDocument"/>, <see cref="FooniController"/>, <see cref="ActorPresenter"/>
    /// 想定ライフサイクル: シーン常駐。Awake で必要な GameObject を生成し、Start で Stage/Actor を構築、その後は Visual Scripting
    ///     グラフから Presenter へ命令が流入する。
    /// スレッド/GC: Unity メインスレッド専用。生成物は MonoBehaviour と ScriptableObject のみ。
    /// </remarks>
    public sealed class FUnityManager : MonoBehaviour
    {
        /// <summary>
        /// 制御対象の <see cref="UIDocument"/>。Inspector で未設定の場合は Awake/Start で探索する。
        /// </summary>
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiDocument")]
        private UIDocument m_UIDocument;

        /// <summary>
        /// プロジェクト設定アセット。null の場合は Resources からロードする。
        /// </summary>
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("project")]
        private FUnityProjectData m_Project;

        /// <summary>生成・確保した UI ルート GameObject。</summary>
        private GameObject m_FUnityUI;

        /// <summary>俳優ごとの Presenter インスタンス。</summary>
        private readonly List<ActorPresenter> m_ActorPresenters = new List<ActorPresenter>();

        /// <summary>俳優設定と Presenter を対応付けるマップ。</summary>
        private readonly Dictionary<FUnityActorData, ActorPresenter> m_ActorPresenterMap = new Dictionary<FUnityActorData, ActorPresenter>();

        /// <summary>Presenter 初期化待ちの FooniController 群。</summary>
        private readonly Dictionary<FUnityActorData, List<FooniController>> m_PendingActorControllers = new Dictionary<FUnityActorData, List<FooniController>>();

        /// <summary>生成済み俳優 UI 要素と設定のペア。</summary>
        private readonly List<ActorVisual> m_ActorVisuals = new List<ActorVisual>();

        /// <summary>Visual Scripting との仲介役。</summary>
        private VSPresenterBridge m_VsBridge;

        /// <summary>ステージ背景適用を担当するサービス。</summary>
        private readonly StageBackgroundService m_StageBackgroundService = new StageBackgroundService();

        /// <summary>遅延実行を提供するタイマーサービス。</summary>
        private TimerServiceBehaviour m_TimerService;

        private struct ActorVisual
        {
            /// <summary>俳優設定。</summary>
            public FUnityActorData Data;

            /// <summary>生成された UI Toolkit 要素。</summary>
            public VisualElement Element;

            /// <summary>俳優設定に対応する Presenter。初期化前は null。</summary>
            public ActorPresenter Presenter;
        }

        /// <summary>
        /// Resources から設定アセットを探索し、UI ルートと Presenter ブリッジを生成する。
        /// Visual Scripting Runner もこの段階でスポーンする。
        /// </summary>
        private void Awake()
        {
            if (m_Project == null)
            {
                m_Project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (m_Project == null || m_Project.ensureFUnityUI)
            {
                EnsureFUnityUI();
                EnsurePresenterBridge();
            }

            if (m_Project != null && m_Project.runners != null)
            {
                foreach (var runner in m_Project.runners)
                {
                    if (runner == null)
                    {
                        continue;
                    }

                    CreateRunner(runner);
                }
            }

            SpawnActorRunnersFromProjectData();
        }

        /// <summary>
        /// UI ドキュメントからルート要素を取得し、ステージ設定と俳優要素を構築する。
        /// </summary>
        private void Start()
        {
            // Resolve UIDocument
            var doc = m_UIDocument != null ? m_UIDocument : GetComponent<UIDocument>();
            if (doc == null)
            {
                Debug.LogError("[FUnity] UIDocument not found on FUnityManager GameObject.");
                return;
            }

            var root = doc.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[FUnity] UIDocument.rootVisualElement returned null.");
                return;
            }

            m_StageBackgroundService.Configure(root);
            if (m_VsBridge != null)
            {
                m_VsBridge.SetStageBackgroundService(m_StageBackgroundService);
            }

            // Resolve ProjectData
            if (m_Project == null)
            {
                m_Project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (m_Project == null)
            {
                Debug.LogWarning("[FUnity] FUnityProjectData not found. Skipping Project-driven UI setup.");
                return;
            }

            // Apply Stage
            ApplyStage(root, m_Project.Stage);

            m_ActorVisuals.Clear();
            m_ActorPresenters.Clear();

            // Create & add actors
            if (m_Project.Actors != null)
            {
                foreach (var actor in m_Project.Actors)
                {
                    if (actor == null)
                    {
                        Debug.LogWarning("[FUnity] Encountered null actor entry in ProjectData.");
                        continue;
                    }

                    var actorVE = CreateActorElement(actor);
                    if (actorVE != null)
                    {
                        root.Add(actorVE);
                        AttachControllerIfNeeded(actorVE, actor);
                        m_ActorVisuals.Add(new ActorVisual
                        {
                            Data = actor,
                            Element = actorVE
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[FUnity] Failed to create actor element for '{actor.DisplayName}'.");
                    }
                }
            }

            Debug.Log($"[FUnity] Project-driven load completed. Actors={(m_Project.Actors?.Count ?? 0)}");

            InitializeActorPresenters();
        }

        /// <summary>
        /// FUnity UI ルート GameObject を生成し、必須コンポーネント（UIDocument, FooniController, ScriptMachine 等）を確保する。
        /// </summary>
        private void EnsureFUnityUI()
        {
            m_FUnityUI = GameObject.Find("FUnity UI");
            if (m_FUnityUI == null)
            {
                m_FUnityUI = new GameObject("FUnity UI");
            }

            m_UIDocument = m_FUnityUI.GetComponent<UIDocument>() ?? m_FUnityUI.AddComponent<UIDocument>();

            EnsureFooniUIBridge(m_FUnityUI);
            EnsureScriptMachine(m_FUnityUI);
            EnsureTimerService();

            if (m_UIDocument.panelSettings == null)
            {
                var panelSettings = FindDefaultPanelSettings();
                if (panelSettings != null)
                {
                    m_UIDocument.panelSettings = panelSettings;
                    Debug.Log($"[FUnity] Assigned PanelSettings: {panelSettings.name} to UIDocument on '{m_FUnityUI.name}'.");
                }
                else
                {
                    Debug.LogWarning("[FUnity] FUnityPanelSettings not found. Place a PanelSettings asset named 'FUnityPanelSettings' under Resources or import the sample so it can be found in the Editor.");
                }
            }

            var controller = m_FUnityUI.GetComponent<FooniController>() ?? m_FUnityUI.AddComponent<FooniController>();
        }

        /// <summary>
        /// Visual Scripting から Presenter を呼び出す <see cref="VSPresenterBridge"/> を確保する。
        /// </summary>
        private void EnsurePresenterBridge()
        {
            if (m_FUnityUI == null)
            {
                m_FUnityUI = GameObject.Find("FUnity UI");
            }

            if (m_FUnityUI == null)
            {
                return;
            }

            m_VsBridge = m_FUnityUI.GetComponent<VSPresenterBridge>() ?? m_FUnityUI.AddComponent<VSPresenterBridge>();
            if (m_VsBridge != null)
            {
                m_VsBridge.SetStageBackgroundService(m_StageBackgroundService);
                if (m_TimerService != null)
                {
                    m_VsBridge.SetTimerService(m_TimerService);
                }
            }
        }

        /// <summary>
        /// 生成済みの UI 要素に対して Presenter を組み立て、Visual Scripting ブリッジへ登録する。
        /// </summary>
        private void InitializeActorPresenters()
        {
            if (m_FUnityUI == null || m_ActorVisuals.Count == 0)
            {
                return;
            }

            if (m_VsBridge == null)
            {
                EnsurePresenterBridge();
            }

            var bridgeCache = new List<FooniUIBridge>(m_FUnityUI.GetComponents<FooniUIBridge>());
            var bridgeIndex = 0;

            m_ActorPresenterMap.Clear();

            for (var i = 0; i < m_ActorVisuals.Count; i++)
            {
                var visual = m_ActorVisuals[i];
                if (visual.Element == null)
                {
                    continue;
                }

                var view = CreateOrConfigureActorView(visual, bridgeCache, ref bridgeIndex);
                if (view == null)
                {
                    continue;
                }

                var state = new ActorState();
                var presenter = new ActorPresenter();
                presenter.Initialize(visual.Data, state, view);

                m_ActorPresenters.Add(presenter);

                m_ActorPresenterMap[visual.Data] = presenter;
                BindPresenterToControllers(visual.Data, presenter);

                visual.Presenter = presenter;
                m_ActorVisuals[i] = visual;
            }

            if (m_VsBridge != null)
            {
                m_VsBridge.Target = m_ActorPresenters.Count > 0 ? m_ActorPresenters[0] : null;
            }
        }

        /// <summary>
        /// 俳優 UI 要素に <see cref="ActorView"/> と <see cref="FooniUIBridge"/> を紐付ける。
        /// </summary>
        /// <param name="visual">俳優設定と要素のペア。</param>
        /// <param name="bridgeCache">再利用するブリッジのキャッシュ。</param>
        /// <param name="bridgeIndex">現在のキャッシュ使用位置。</param>
        /// <returns>構成済みの <see cref="ActorView"/>。失敗時は null。</returns>
        private ActorView CreateOrConfigureActorView(ActorVisual visual, List<FooniUIBridge> bridgeCache, ref int bridgeIndex)
        {
            if (m_FUnityUI == null)
            {
                return null;
            }

            FooniUIBridge bridge;
            if (bridgeIndex < bridgeCache.Count)
            {
                bridge = bridgeCache[bridgeIndex];
            }
            else
            {
                bridge = m_FUnityUI.AddComponent<FooniUIBridge>();
                bridgeCache.Add(bridge);
            }

            bridgeIndex++;

            if (visual.Data != null)
            {
                bridge.defaultSpeed = Mathf.Max(0f, visual.Data.MoveSpeed);
            }

            var view = m_FUnityUI.AddComponent<ActorView>();
            view.Configure(bridge, visual.Element);
            return view;
        }

        /// <summary>
        /// FUnity UI GameObject に <see cref="FooniUIBridge"/> が存在することを保証する。
        /// </summary>
        /// <param name="uiGO">対象 GameObject。</param>
        private static void EnsureFooniUIBridge(GameObject uiGO)
        {
            if (uiGO == null)
            {
                return;
            }

            if (!uiGO.TryGetComponent(out FUnity.Runtime.Input.FooniUIBridge _))
            {
                uiGO.AddComponent<FUnity.Runtime.Input.FooniUIBridge>();
                Debug.Log("[FUnity] Added FooniUIBridge to 'FUnity UI'.");
            }
        }

        /// <summary>
        /// Visual Scripting 実行のため <see cref="ScriptMachine"/> を付与する。
        /// </summary>
        /// <param name="uiGO">対象 GameObject。</param>
        private static void EnsureScriptMachine(GameObject uiGO)
        {
            if (uiGO == null)
            {
                return;
            }

            if (!uiGO.TryGetComponent(out ScriptMachine _))
            {
                uiGO.AddComponent<ScriptMachine>();
                Debug.Log("[FUnity] Added ScriptMachine to 'FUnity UI'.");
            }
        }

        /// <summary>
        /// 遅延実行サービス <see cref="TimerServiceBehaviour"/> を FUnity UI GameObject に確保する。
        /// Visual Scripting からのタイマー利用を統一するため、常に同一インスタンスを提供する。
        /// </summary>
        private void EnsureTimerService()
        {
            if (m_FUnityUI == null)
            {
                return;
            }

            m_TimerService = m_FUnityUI.GetComponent<TimerServiceBehaviour>() ?? m_FUnityUI.AddComponent<TimerServiceBehaviour>();

            if (m_VsBridge != null)
            {
                m_VsBridge.SetTimerService(m_TimerService);
            }
        }

        /// <summary>
        /// Resources またはアセットデータベースから既定の PanelSettings を探索する。
        /// </summary>
        /// <returns>見つかった PanelSettings。無い場合は null。</returns>
        private PanelSettings FindDefaultPanelSettings()
        {
            var panelSettings = Resources.Load<PanelSettings>("FUnityPanelSettings");
            if (panelSettings != null)
            {
                return panelSettings;
            }

#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets("t:PanelSettings FUnityPanelSettings");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
                if (asset != null)
                {
                    return asset;
                }
            }
#endif

            return null;
        }

        /// <summary>
        /// ステージ設定を UI ルートへ反映し、背景色・背景画像を適用する。
        /// </summary>
        /// <param name="root">UI Toolkit ルート要素。</param>
        /// <param name="stage">ステージ設定。</param>
        private void ApplyStage(VisualElement root, FUnityStageData stage)
        {
            if (root == null || stage == null)
            {
                return;
            }

            m_StageBackgroundService.SetBackgroundColor(stage.BackgroundColor);
            m_StageBackgroundService.SetBackground(stage.BackgroundImage, stage.BackgroundScale);
        }

        /// <summary>
        /// 俳優設定から UI Toolkit 要素を生成し、サイズやスタイルを適用する。
        /// </summary>
        /// <param name="actor">俳優設定。</param>
        /// <returns>生成した要素。失敗時は null。</returns>
        private VisualElement CreateActorElement(FUnityActorData actor)
        {
            if (actor == null)
            {
                return null;
            }

            VisualElement ve = null;

            if (actor.ElementUxml != null)
            {
                ve = actor.ElementUxml.Instantiate();
            }
            else
            {
                ve = new FooniElement();
            }

            if (ve == null)
            {
                return null;
            }

            if (actor.ElementStyle != null)
            {
                ve.styleSheets.Add(actor.ElementStyle);
            }

            var root = ve.Q<VisualElement>("root");
            var portrait = root != null
                ? root.Q<VisualElement>("portrait")
                : null;

            if (portrait == null)
            {
                portrait = ve.Q<VisualElement>("portrait");
            }

            if (portrait != null && actor.Portrait != null)
            {
                portrait.style.backgroundImage = Background.FromTexture2D(actor.Portrait);
            }

            if (root != null)
            {
                var size = actor.Size;
                if (size.x > 0f)
                {
                    root.style.width = size.x;
                }
                else
                {
                    root.style.width = StyleKeyword.Auto;
                }

                if (size.y > 0f)
                {
                    root.style.height = size.y;
                }
                else
                {
                    root.style.height = StyleKeyword.Auto;
                }

                root.style.flexGrow = 0f;
                root.style.flexShrink = 0f;
                root.AddToClassList("actor");
                ve.AddToClassList("actor");
            }
            else
            {
                ve.AddToClassList("actor");
            }

            ve.style.position = Position.Absolute;
            ve.style.left = actor.InitialPosition.x;
            ve.style.top = actor.InitialPosition.y;
            ve.style.translate = new Translate(0f, 0f);

            if (string.IsNullOrEmpty(ve.name) && !string.IsNullOrEmpty(actor.DisplayName))
            {
                ve.name = actor.DisplayName;
            }

            return ve;
        }

        /// <summary>
        /// <see cref="FooniController"/> に俳優要素をバインドし、浮遊アニメーション設定を同期する。
        /// </summary>
        /// <param name="actorVE">俳優 UI 要素。</param>
        /// <param name="data">俳優設定。</param>
        private void AttachControllerIfNeeded(VisualElement actorVE, FUnityActorData data)
        {
            if (actorVE == null)
            {
                return;
            }

            FooniController controller = null;

            if (m_UIDocument != null)
            {
                controller = m_UIDocument.GetComponent<FooniController>();
            }

            if (controller == null && m_FUnityUI != null)
            {
                controller = m_FUnityUI.GetComponent<FooniController>();
            }

            if (controller == null)
            {
                var go = GameObject.Find("FUnity UI");
                if (go != null)
                {
                    controller = go.GetComponent<FooniController>();
                }
            }

            if (controller == null)
            {
                Debug.LogWarning("[FUnity] FooniController not found. Actor binding skipped.");
                return;
            }

            if (controller.BoundElement != null && controller.BoundElement != actorVE)
            {
                Debug.LogWarning("[FUnity] FooniController already bound to another element. Rebinding to the latest actor.");
            }

            controller.BindActorElement(actorVE);

            if (data != null)
            {
                controller.EnableFloat(data.FloatAnimation);
            }

            RegisterControllerForActor(data, controller);
        }

        /// <summary>
        /// Visual Scripting Runner を生成し、定義されたマクロや変数を適用する。
        /// </summary>
        /// <param name="entry">Runner 設定。</param>
        private void CreateRunner(FUnityProjectData.RunnerEntry entry)
        {
            var go = new GameObject(string.IsNullOrEmpty(entry.name) ? "FUnity VS Runner" : entry.name);

            var machine = go.AddComponent<ScriptMachine>();
            machine.nest.source = GraphSource.Macro;
            machine.nest.macro = entry.macro;

            if (entry.objectVariables != null)
            {
                foreach (var variable in entry.objectVariables)
                {
                    if (string.IsNullOrEmpty(variable.key))
                    {
                        continue;
                    }

                    Variables.Object(go).Set(variable.key, variable.value);
                }
            }

            if (!Variables.Object(go).IsDefined("FUnityUI") && m_FUnityUI != null)
            {
                Variables.Object(go).Set("FUnityUI", m_FUnityUI);
            }

            if (m_VsBridge == null)
            {
                EnsurePresenterBridge();
            }

            if (!Variables.Object(go).IsDefined("VSPresenterBridge") && m_VsBridge != null)
            {
                Variables.Object(go).Set("VSPresenterBridge", m_VsBridge);
            }
        }

        /// <summary>
        /// 俳優設定ごとに Visual Scripting Runner を生成し、必要なコンポーネントと参照を付与する。
        /// </summary>
        private void SpawnActorRunnersFromProjectData()
        {
            if (m_Project == null)
            {
                m_Project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (m_Project == null)
            {
                Debug.LogWarning("[FUnity] FUnityProjectData not found. Skip runner spawn.");
                return;
            }

            if (m_Project.Actors == null || m_Project.Actors.Count == 0)
            {
                Debug.Log("[FUnity] No actors to spawn.");
                return;
            }

            if (m_VsBridge == null)
            {
                EnsurePresenterBridge();
            }

            foreach (var actor in m_Project.Actors)
            {
                if (actor == null)
                {
                    continue;
                }

                var runner = CreateActorRunner(actor);
                if (runner == null)
                {
                    continue;
                }

                ConfigureScriptMachine(runner, actor.ScriptGraph);
                ConfigureFooniController(runner, actor);

                var objectVariables = Variables.Object(runner);
                if (m_FUnityUI != null && !objectVariables.IsDefined("FUnityUI"))
                {
                    objectVariables.Set("FUnityUI", m_FUnityUI);
                }

                if (m_VsBridge != null && !objectVariables.IsDefined("VSPresenterBridge"))
                {
                    objectVariables.Set("VSPresenterBridge", m_VsBridge);
                }
            }
        }

        /// <summary>
        /// 俳優ごとの Runner GameObject を生成または再利用し、基礎的な設定を行う。
        /// </summary>
        /// <param name="actor">対象の俳優設定。</param>
        /// <returns>生成または再利用した Runner。失敗時は null。</returns>
        private GameObject CreateActorRunner(FUnityActorData actor)
        {
            if (actor == null)
            {
                return null;
            }

            var name = $"{actor.DisplayName} VS Runner";
            var runner = GameObject.Find(name) ?? new GameObject(name);
            runner.tag = "Untagged";
            runner.layer = 0;

            if (runner.transform.parent != transform)
            {
                runner.transform.SetParent(transform, false);
            }

            return runner;
        }

        /// <summary>
        /// Runner に <see cref="ScriptMachine"/> を付与し、マクロを割り当てる。
        /// </summary>
        /// <param name="runner">構成対象の Runner。</param>
        /// <param name="macro">適用する Visual Scripting マクロ。</param>
        private void ConfigureScriptMachine(GameObject runner, ScriptGraphAsset macro)
        {
            if (runner == null)
            {
                return;
            }

            var machine = runner.GetComponent<ScriptMachine>();
            if (machine == null)
            {
                machine = runner.AddComponent<ScriptMachine>();
            }

            if (macro == null)
            {
                Debug.LogWarning($"[FUnity] ScriptGraph is null. Runner='{runner.name}'");
                return;
            }

            machine.nest.source = GraphSource.Macro;
            machine.nest.macro = macro;

            Debug.Log($"[FUnity] Spawned actor runner: {runner.name}");
        }

        /// <summary>
        /// Runner に <see cref="FooniController"/> を設定し、俳優 Presenter との橋渡しを行う。
        /// </summary>
        /// <param name="runner">構成対象の Runner。</param>
        /// <param name="actor">俳優設定。</param>
        private void ConfigureFooniController(GameObject runner, FUnityActorData actor)
        {
            if (runner == null || actor == null)
            {
                return;
            }

            var controller = runner.GetComponent<FooniController>();
            if (controller == null)
            {
                controller = runner.AddComponent<FooniController>();
            }

            if (!actor.FloatAnimation)
            {
                controller.EnableFloat(false);
            }

            RegisterControllerForActor(actor, controller);
        }

        /// <summary>
        /// 俳優設定に紐づく Presenter が未初期化の場合、FooniController を保留リストに追加する。Presenter 済みであれば即座に結線する。
        /// </summary>
        /// <param name="actor">紐付け対象の俳優設定。</param>
        /// <param name="controller">Presenter へ命令を委譲する FooniController。</param>
        private void RegisterControllerForActor(FUnityActorData actor, FooniController controller)
        {
            if (actor == null || controller == null)
            {
                return;
            }

            if (m_ActorPresenterMap.TryGetValue(actor, out var presenter) && presenter != null)
            {
                controller.SetActorPresenter(presenter);
                return;
            }

            if (!m_PendingActorControllers.TryGetValue(actor, out var controllers))
            {
                controllers = new List<FooniController>();
                m_PendingActorControllers[actor] = controllers;
            }

            if (!controllers.Contains(controller))
            {
                controllers.Add(controller);
            }
        }

        /// <summary>
        /// 初期化済みの Presenter を同一俳優の FooniController 群へ割り当てる。
        /// </summary>
        /// <param name="actor">対象俳優。</param>
        /// <param name="presenter">割り当てる Presenter。</param>
        private void BindPresenterToControllers(FUnityActorData actor, ActorPresenter presenter)
        {
            if (actor == null || presenter == null)
            {
                return;
            }

            if (!m_PendingActorControllers.TryGetValue(actor, out var controllers))
            {
                return;
            }

            for (var i = controllers.Count - 1; i >= 0; i--)
            {
                var controller = controllers[i];
                if (controller == null)
                {
                    controllers.RemoveAt(i);
                    continue;
                }

                controller.SetActorPresenter(presenter);
            }

            controllers.Clear();
            m_PendingActorControllers.Remove(actor);
        }
    }
}
