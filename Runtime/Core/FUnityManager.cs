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
using FUnity.Runtime.Variables;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif
using FUnity.Runtime.Authoring;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// FUnity ランタイム全体をオーケストレーションする Presenter 層の MonoBehaviour。
    /// UI ドキュメント、俳優、Visual Scripting Runner を初期化し、Visual Scripting から Presenter への命令経路を構築する。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnityProjectData"/>, <see cref="UIDocument"/>, <see cref="ActorPresenterAdapter"/>, <see cref="ActorPresenter"/>, <see cref="VSPresenterBridge"/>
    /// 想定ライフサイクル: シーン常駐。Awake で必要な GameObject を生成し、Start で Stage/Actor を構築、その後は Visual Scripting
    ///     グラフから Presenter へ命令が流入する。
    /// スレッド/GC: Unity メインスレッド専用。生成物は MonoBehaviour と ScriptableObject のみ。
    /// </remarks>
    public sealed class FUnityManager : MonoBehaviour
    {
        /// <summary>クローン Runner 名に付与するサフィックス。</summary>
        public const string CloneSuffix = "(clone)";

        /// <summary>シーン内で最後に有効化された FUnityManager への参照。</summary>
        public static FUnityManager Instance { get; private set; }

        /// <summary>現在利用可能なマウス座標プロバイダ。未初期化時は null。</summary>
        public static IMousePositionProvider MouseProvider { get; private set; }

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

        /// <summary>アクティブな制作モード設定。ProjectData からの選択結果をキャッシュし、未設定時は Inspector 値を利用する。</summary>
        [SerializeField]
        private FUnityModeConfig m_ActiveModeConfig;

        /// <summary>現在使用中の ProjectData。RootLayoutBootstrapper など外部コンポーネントが参照する。</summary>
        public FUnityProjectData ProjectData => m_Project;

        /// <summary>
        /// 現在アクティブな制作モード設定を返します。ProjectData からの解決を優先し、未設定の場合は Inspector の値を利用します。
        /// </summary>
        public FUnityModeConfig ActiveModeConfig => ResolveActiveModeConfig();

        /// <summary>
        /// Visual Scripting から参照する既定の <see cref="ActorPresenterAdapter"/>。Inspector で明示設定し、未設定時はシーン内を探索する。
        /// </summary>
        [SerializeField]
        private ActorPresenterAdapter m_DefaultActorPresenterAdapter;

        /// <summary>生成・確保した UI ルート GameObject。</summary>
        private GameObject m_FUnityUI;

        /// <summary>俳優ごとの Presenter インスタンス。</summary>
        private readonly List<ActorPresenter> m_ActorPresenters = new List<ActorPresenter>();

        /// <summary>ActorPresenterAdapter 未割り当て時に重複警告を避けるフラグ。</summary>
        private bool m_LoggedMissingAdapter;

        /// <summary>俳優設定と Presenter を対応付けるマップ。</summary>
        private readonly Dictionary<FUnityActorData, ActorPresenter> m_ActorPresenterMap = new Dictionary<FUnityActorData, ActorPresenter>();

        /// <summary>Presenter 初期化待ちの ActorPresenterAdapter 群。</summary>
        private readonly Dictionary<FUnityActorData, List<ActorPresenterAdapter>> m_PendingActorControllers = new Dictionary<FUnityActorData, List<ActorPresenterAdapter>>();

        /// <summary>生成済み俳優 UI 要素と設定のペア。</summary>
        private readonly List<ActorVisual> m_ActorVisuals = new List<ActorVisual>();

        /// <summary>俳優設定と ScriptMachine を対応付け、Graph Variables に Self を注入するための辞書。</summary>
        private readonly Dictionary<FUnityActorData, ScriptMachine> m_ActorScriptMachines = new Dictionary<FUnityActorData, ScriptMachine>();

        /// <summary>生成済みの俳優 Runner GameObject 群。Play/Stop を跨いでクリーンアップする。</summary>
        private readonly List<GameObject> m_ActorRunnerInstances = new List<GameObject>();

        /// <summary>俳優 Runner を階層で整理するための親 GameObject。</summary>
        private GameObject m_ActorRunnerRoot;

        /// <summary>ステージ全体を表現する UI ルート要素。</summary>
        private StageElement m_StageElement;

        /// <summary>Visual Scripting との仲介役。</summary>
        private VSPresenterBridge m_VsBridge;

        /// <summary>ステージ背景適用を担当するサービス。</summary>
        private readonly StageBackgroundService m_StageBackgroundService = new StageBackgroundService();

        /// <summary>Scratch 互換変数を管理するサービス。</summary>
        private readonly FUnityVariableService m_VariableService = new FUnityVariableService();

        /// <summary>マウス座標を監視し、Scratch 座標系へ変換するサービス。</summary>
        private MousePositionService m_MousePositionService;

        /// <summary>変数モニターの UI 表示を担当する Presenter。</summary>
        private VariableUiPresenter m_VariableUiPresenter;

        /// <summary>背景レイヤーを初期化済みかどうかを示すフラグ。</summary>
        private bool m_BackgroundInitialized;

        /// <summary>遅延実行を提供するタイマーサービス。</summary>
        private TimerServiceBehaviour m_TimerService;

        /// <summary>アクティブモード設定探索の警告重複を防ぐフラグ。</summary>
        private bool m_LoggedMissingModeConfig;

        /// <summary>実行時に生成されたクローン俳優を追跡するリスト。</summary>
        private readonly List<ActorCloneRuntime> m_RuntimeActorClones = new List<ActorCloneRuntime>();

        /// <summary>既定俳優テンプレートの Resources パス。</summary>
        private const string DefaultActorTemplatePath = "UI/ActorElement";

        /// <summary>フォールバック俳優テンプレートの Resources パス。</summary>
        private const string FallbackActorTemplatePath = "UI/FooniElement";

        /// <summary>ステージ背景のフォールバックに使用する Resources/Backgrounds 内テクスチャ名。</summary>
        private const string DefaultStageBackgroundName = "Background_01";

        /// <summary>既定で探索するポートレート要素名。</summary>
        private const string PortraitElementName = "portrait";

        /// <summary>既定で探索するルート要素名。</summary>
        private const string RootElementName = "root";

        /// <summary>既定で探索する俳優コンテナの USS クラス名。</summary>
        private const string RootElementClassName = "actor-root";

        private struct ActorVisual
        {
            /// <summary>俳優設定。</summary>
            public FUnityActorData Data;

            /// <summary>生成された UI Toolkit 要素。</summary>
            public VisualElement Element;

            /// <summary>俳優設定に対応する Presenter。初期化前は null。</summary>
            public ActorPresenter Presenter;

            /// <summary>俳優表示用に確保した View 実装。再初期化時に再利用する。</summary>
            public IActorView View;

            /// <summary>View をアタッチした GameObject。Domain Reload 後も再利用する。</summary>
            public GameObject ViewHost;
        }

        /// <summary>
        /// 実行中に生成した俳優クローンの付帯情報をまとめる構造体。
        /// </summary>
        private struct ActorCloneRuntime
        {
            /// <summary>元となった俳優設定。null の場合もある。</summary>
            public FUnityActorData ActorData;

            /// <summary>クローンとして生成された Presenter。</summary>
            public ActorPresenter Presenter;

            /// <summary>クローン用に割り当てた View。</summary>
            public IActorView View;

            /// <summary>クローンが使用する UI Toolkit 要素。</summary>
            public VisualElement Element;

            /// <summary>View を保持する GameObject。</summary>
            public GameObject ViewHost;

            /// <summary>Visual Scripting Runner のインスタンス。</summary>
            public GameObject Runner;

            /// <summary>Runner に付与された ActorPresenterAdapter。</summary>
            public ActorPresenterAdapter Adapter;
        }

        /// <summary>
        /// Resources から設定アセットを探索し、UI ルートと Presenter ブリッジを生成する。
        /// Visual Scripting Runner もこの段階でスポーンする。
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[FUnity] FUnityManager: 複数のインスタンスが同時に有効化されています。最新のコンポーネントを Instance として使用します。");
            }

            Instance = this;

            MouseProvider = null;
            FUnityServices.MousePosition = null;
            FUnityServices.Variables = m_VariableService;

            if (m_Project == null)
            {
                m_Project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (m_Project == null || m_Project.ensureFUnityUI)
            {
                EnsureFUnityUI();
                EnsurePresenterBridge();
                ResolveDefaultActorPresenterAdapter();
            }

            UpdateBootstrapperProjectData();

            ResolveActiveModeConfig();

            ApplyProjectFrameRate();

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
        /// 破棄時にシングルトン参照を解放する。
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            DisposeMousePositionService();
            m_VariableService.SetUiPresenter(null);
            FUnityServices.ResetAll();
        }

        /// <summary>
        /// UI ドキュメントから設定を読み取り、ステージと俳優 UI を動的に再構築するエントリーポイント。
        /// </summary>
        private void Start()
        {
            RebuildUI();
        }

        /// <summary>
        /// <see cref="FUnityProjectData"/> の内容からステージと俳優 UI を再生成する。既存要素は安全に破棄し、背景サービスを再構成する。
        /// </summary>
        public void RebuildUI()
        {
            var doc = m_UIDocument != null ? m_UIDocument : GetComponent<UIDocument>();
            if (doc == null)
            {
                Debug.LogError("[FUnity] UIDocument not found on FUnityManager GameObject.");
                return;
            }

            m_UIDocument = doc;

            var root = doc.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[FUnity] UIDocument.rootVisualElement returned null.");
                return;
            }

            var stageElement = EnsureStageElement(root);
            EnsureVariableUiPresenter(stageElement);
            ApplyScratchStageSizing(stageElement);
            var backgroundRoot = stageElement != null ? stageElement.StageViewport : root;

            EnsureStageBackgroundRoot(backgroundRoot);
            InitializeMousePositionService(backgroundRoot);

            if (m_VsBridge != null)
            {
                m_VsBridge.SetStageBackgroundService(m_StageBackgroundService);
            }

            ResolveDefaultActorPresenterAdapter();

            if (m_Project == null)
            {
                m_Project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (m_Project == null)
            {
                Debug.LogWarning("[FUnity] FUnityProjectData not found. メニュー FUnity/Create/FUnityProjectData を実行して初期アセットを生成してください。");
                return;
            }

            ResetActorVisualState(stageElement);

            SpawnActorRunnersFromProjectData();

            ApplyStage(stageElement, m_Project.Stage);

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
                    if (actorVE == null)
                    {
                        Debug.LogWarning($"[FUnity] Failed to create actor element for '{actor.DisplayName}'.");
                        continue;
                    }

                    if (stageElement != null)
                    {
                        stageElement.AddActorElement(actorVE);
                    }
                    else
                    {
                        backgroundRoot.Add(actorVE);
                    }

                    AttachControllerIfNeeded(actorVE, actor);
                    m_ActorVisuals.Add(new ActorVisual
                    {
                        Data = actor,
                        Element = actorVE
                    });
                }
            }

            Debug.Log($"[FUnity] Project-driven load completed. Actors={(m_Project.Actors?.Count ?? 0)}");

            InitializeActorPresenters();
        }

        /// <summary>
        /// StageElement を配置する親要素を決定する。UIScaleService が生成したスケール用子要素を優先する。
        /// </summary>
        /// <param name="root">UIDocument のルート要素。</param>
        /// <returns>StageElement を追加すべき親要素。見つからない場合は null。</returns>
        private static VisualElement ResolveStageParent(VisualElement root)
        {
            if (root == null)
            {
                return null;
            }

            var scaledRoot = root.Q<VisualElement>(UIScaleService.ScaledContentRootName);
            return scaledRoot ?? root;
        }

        /// <summary>
        /// UIScaleService が用意したコンテンツルート配下に <see cref="StageElement"/> を確保し、再構築時に再利用できるようにする。
        /// </summary>
        /// <param name="root">UIDocument のルート要素。</param>
        /// <returns>確保した <see cref="StageElement"/>。失敗時は null。</returns>
        private StageElement EnsureStageElement(VisualElement root)
        {
            if (root == null)
            {
                return null;
            }

            var stageParent = ResolveStageParent(root);
            if (stageParent == null)
            {
                return null;
            }

            if (m_StageElement != null && m_StageElement.parent != stageParent)
            {
                m_StageElement.RemoveFromHierarchy();
                m_StageElement = null;
            }

            if (m_StageElement == null)
            {
                m_StageElement = stageParent.Q<StageElement>();
            }

            if (m_StageElement == null)
            {
                m_StageElement = root.Q<StageElement>();
                if (m_StageElement != null && m_StageElement.parent != stageParent)
                {
                    m_StageElement.RemoveFromHierarchy();
                }
            }

            if (m_StageElement == null)
            {
                var existingByName = stageParent.Q<VisualElement>(StageElement.StageRootName)
                    ?? root.Q<VisualElement>(StageElement.StageRootName);
                if (existingByName is StageElement typedElement)
                {
                    m_StageElement = typedElement;
                    if (m_StageElement.parent != stageParent)
                    {
                        m_StageElement.RemoveFromHierarchy();
                    }
                }
            }

            if (m_StageElement == null)
            {
                m_StageElement = new StageElement();
            }

            if (m_StageElement.parent != stageParent)
            {
                stageParent.Add(m_StageElement);
            }

            return m_StageElement;
        }

        /// <summary>
        /// 変数モニター Presenter を生成し、Stage のオーバーレイへコンテナを接続する。
        /// </summary>
        /// <param name="stageElement">対象の StageElement。null の場合は処理を行わない。</param>
        private void EnsureVariableUiPresenter(StageElement stageElement)
        {
            if (stageElement == null)
            {
                return;
            }

            var overlay = stageElement.OverlayContainer;
            if (overlay == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager: StageElement にオーバーレイコンテナが存在しないため変数モニターを配置できません。");
                return;
            }

            if (m_VariableUiPresenter == null)
            {
                m_VariableUiPresenter = new VariableUiPresenter();
            }

            m_VariableUiPresenter.SetRoot(overlay);
            m_VariableService.SetUiPresenter(m_VariableUiPresenter);
        }

        /// <summary>
        /// 背景サービスの対象ルートを保証し、初回は既定背景を読み込む。2 回目以降は現在の状態を再適用する。
        /// </summary>
        /// <param name="stageRoot">背景レイヤーを挿入する対象要素。</param>
        private void EnsureStageBackgroundRoot(VisualElement stageRoot)
        {
            if (stageRoot == null)
            {
                return;
            }

            var modeConfig = ResolveActiveModeConfig();
            var origin = CoordinateConverter.GetActiveOrigin(modeConfig);
            var originMode = CoordinateConverter.ToOriginMode(origin);
            m_StageBackgroundService.SetCoordinateOrigin(originMode);

            if (!m_BackgroundInitialized)
            {
                m_StageBackgroundService.Initialize(stageRoot, DefaultStageBackgroundName);
                m_BackgroundInitialized = true;
                return;
            }

            m_StageBackgroundService.Configure(stageRoot);
        }

        /// <summary>
        /// ステージビューポートを対象にマウス座標サービスを初期化し、Visual Scripting から参照できるようにします。
        /// </summary>
        /// <param name="stageRoot">ステージのローカル座標系を持つ要素。</param>
        private void InitializeMousePositionService(VisualElement stageRoot)
        {
            DisposeMousePositionService();

            if (stageRoot == null)
            {
                return;
            }

            Vector2 ResolveSize() => ResolveMouseStageSize();

            m_MousePositionService = new MousePositionService(stageRoot, ResolveSize, true);
            MouseProvider = m_MousePositionService;
            FUnityServices.MousePosition = m_MousePositionService;
        }

        /// <summary>
        /// マウス座標サービスを破棄し、静的アクセサを安全な状態へ戻します。
        /// </summary>
        private void DisposeMousePositionService()
        {
            if (m_MousePositionService != null)
            {
                m_MousePositionService.Dispose();
                m_MousePositionService = null;
            }

            MouseProvider = null;
            FUnityServices.MousePosition = null;
        }

        /// <summary>
        /// 現在のステージサイズを取得し、マウス座標サービスへ提供する値に変換します。
        /// </summary>
        /// <returns>ステージの論理幅と高さ（px）。</returns>
        private Vector2 ResolveMouseStageSize()
        {
            if (m_StageElement != null)
            {
                var stageSize = m_StageElement.StageSize;
                if (stageSize.x > 0 && stageSize.y > 0)
                {
                    return new Vector2(stageSize.x, stageSize.y);
                }
            }

            if (m_ActiveModeConfig != null && m_ActiveModeConfig.Mode == FUnityAuthoringMode.Scratch)
            {
                var width = Mathf.Max(1, m_ActiveModeConfig.ScratchStageWidth);
                var height = Mathf.Max(1, m_ActiveModeConfig.ScratchStageHeight);
                return new Vector2(width, height);
            }

            return new Vector2(FUnityStageData.DefaultStageWidth, FUnityStageData.DefaultStageHeight);
        }

        /// <summary>
        /// 既存の俳優 UI と Presenter 状態をクリアし、再構築の準備を整える。
        /// </summary>
        /// <param name="stageElement">俳優コンテナを提供するステージ要素。</param>
        private void ResetActorVisualState(StageElement stageElement)
        {
            CleanupActorRunners();

            if (stageElement != null)
            {
                stageElement.ClearActors();
            }

            for (var i = 0; i < m_ActorVisuals.Count; i++)
            {
                var visual = m_ActorVisuals[i];
                if (visual.Element != null && visual.Element.parent != null)
                {
                    visual.Element.RemoveFromHierarchy();
                }
            }

            m_ActorVisuals.Clear();
            m_ActorPresenters.Clear();
            m_ActorPresenterMap.Clear();
            m_ActorScriptMachines.Clear();
            m_PendingActorControllers.Clear();

            if (m_VsBridge != null)
            {
                m_VsBridge.Target = null;
            }

            m_VariableService.Initialize(m_Project, null);
        }

        /// <summary>
        /// FUnity UI ルート GameObject を生成し、必須コンポーネント（UIDocument 等）を確保する。
        /// </summary>
        private void EnsureFUnityUI()
        {
            m_FUnityUI = GameObject.Find("FUnity UI");
            if (m_FUnityUI == null)
            {
                m_FUnityUI = new GameObject("FUnity UI");
            }

            m_UIDocument = m_FUnityUI.GetComponent<UIDocument>() ?? m_FUnityUI.AddComponent<UIDocument>();

            EnsureRootLayoutBootstrapperComponent();
            EnsureFooniUIBridge(m_FUnityUI);
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
        }

        /// <summary>
        /// FUnity UI ルートに RootLayoutBootstrapper を付与し、UI Toolkit ルート要素の初期レイアウト確定を保証する。
        /// </summary>
        private void EnsureRootLayoutBootstrapperComponent()
        {
            if (m_FUnityUI == null)
            {
                return;
            }

            var bootstrapper = m_FUnityUI.GetComponent<RootLayoutBootstrapper>();
            if (bootstrapper == null)
            {
                bootstrapper = m_FUnityUI.AddComponent<RootLayoutBootstrapper>();
            }

            bootstrapper.SetProjectData(m_Project);
        }

        /// <summary>
        /// RootLayoutBootstrapper に ProjectData を再割り当てし、モード設定変更時の参照ずれを解消する。
        /// </summary>
        private void UpdateBootstrapperProjectData()
        {
            if (m_FUnityUI == null)
            {
                m_FUnityUI = GameObject.Find("FUnity UI");
            }

            if (m_FUnityUI == null)
            {
                return;
            }

            var bootstrapper = m_FUnityUI.GetComponent<RootLayoutBootstrapper>();
            if (bootstrapper == null)
            {
                return;
            }

            bootstrapper.SetProjectData(m_Project);
        }

        /// <summary>
        /// ProjectData に設定されたフレームレート構成を適用し、必要に応じて vSync を無効化する。
        /// </summary>
        private void ApplyProjectFrameRate()
        {
            if (m_Project == null)
            {
                return;
            }

            if (!m_Project.ApplyFrameRateOnStartup)
            {
                return;
            }

            if (m_Project.DisableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }

            Application.targetFrameRate = m_Project.TargetFPS;
        }

        /// <summary>
        /// 既定の <see cref="ActorPresenterAdapter"/> を Inspector 指定優先で確保し、未確定ならシーン内から既存コンポーネントを探索する。
        /// </summary>
        private void ResolveDefaultActorPresenterAdapter()
        {
            if (m_DefaultActorPresenterAdapter != null)
            {
                return;
            }

            if (m_UIDocument != null)
            {
                var candidate = m_UIDocument.GetComponent<ActorPresenterAdapter>();
                if (candidate != null)
                {
                    m_DefaultActorPresenterAdapter = candidate;
                    m_LoggedMissingAdapter = false;
                    return;
                }
            }

            if (m_FUnityUI != null)
            {
                var candidate = m_FUnityUI.GetComponent<ActorPresenterAdapter>();
                if (candidate != null)
                {
                    m_DefaultActorPresenterAdapter = candidate;
                    m_LoggedMissingAdapter = false;
                    return;
                }
            }

            var adapters = FindObjectsOfType<ActorPresenterAdapter>();
            if (adapters != null)
            {
                foreach (var adapter in adapters)
                {
                    if (adapter == null)
                    {
                        continue;
                    }

                    m_DefaultActorPresenterAdapter = adapter;
                    m_LoggedMissingAdapter = false;
                    return;
                }
            }

            if (!m_LoggedMissingAdapter)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter が見つかりません。FUnityManager の Default Actor Presenter Adapter に割り当てるか、Visual Scripting グラフから参照してください。");
                m_LoggedMissingAdapter = true;
            }
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
            if (m_ActorVisuals.Count == 0)
            {
                return;
            }

            if (m_FUnityUI == null)
            {
                EnsureFUnityUI();
                if (m_FUnityUI == null)
                {
                    FUnityLog.LogWarning("FUnity UI GameObject が存在しないため、俳優 Presenter の初期化をスキップします。");
                    return;
                }
            }

            if (m_VsBridge == null)
            {
                EnsurePresenterBridge();
            }

            var existingBridges = m_FUnityUI.GetComponents<FooniUIBridge>();
            var bridgeCache = existingBridges != null && existingBridges.Length > 0
                ? new List<FooniUIBridge>(existingBridges)
                : new List<FooniUIBridge>(Mathf.Max(4, m_ActorVisuals.Count));
            var bridgeIndex = 0;

            m_ActorPresenterMap.Clear();

            for (var i = 0; i < m_ActorVisuals.Count; i++)
            {
                var visual = m_ActorVisuals[i];

                if (visual.Data == null)
                {
                    FUnityLog.LogWarning($"俳優データが null です (index={i})。初期化をスキップします。");
                    continue;
                }

                var view = CreateOrConfigureActorView(ref visual, ref bridgeCache, ref bridgeIndex);
                if (view == null || ReferenceEquals(view, NullActorView.Instance))
                {
                    FUnityLog.LogWarning($"俳優ビューの構築に失敗したため '{visual.Data.DisplayName}' をスキップします。");
                    continue;
                }

                var state = new ActorState();
                var presenter = new ActorPresenter();
                var stageRoot = m_StageElement != null ? m_StageElement.ActorContainer : null;
                presenter.Initialize(visual.Data, state, view, m_ActiveModeConfig, stageRoot);
                view.SetActorPresenter(presenter);

                m_ActorPresenters.Add(presenter);

                m_ActorPresenterMap[visual.Data] = presenter;
                BindActorGraphContext(visual.Data, presenter, view, visual.Element);
                BindPresenterToControllers(visual.Data, presenter);

                visual.Presenter = presenter;
                visual.View = view;
                visual.ViewHost = (view as Component)?.gameObject;
                m_ActorVisuals[i] = visual;
            }

            m_VariableService.Initialize(m_Project, m_ActorPresenters);

            if (m_VsBridge != null)
            {
                m_VsBridge.Target = m_ActorPresenters.Count > 0 ? m_ActorPresenters[0] : null;
            }

            TriggerGreenFlagInternal();
        }

        /// <summary>
        /// 俳優 UI 要素に <see cref="ActorView"/> と <see cref="FooniUIBridge"/> を結び付け、テンプレート欠如時はフォールバックを生成する。
        /// </summary>
        /// <param name="visual">俳優設定と生成済み要素の組。</param>
        /// <param name="bridgeCache">再利用するブリッジのリスト。null の場合は生成する。</param>
        /// <param name="bridgeIndex">キャッシュ利用位置。負値の場合は 0 に補正する。</param>
        /// <returns>構成済みの <see cref="IActorView"/>。致命的に失敗した場合は <see cref="NullActorView.Instance"/>。</returns>
        private IActorView CreateOrConfigureActorView(ref ActorVisual visual, ref List<FooniUIBridge> bridgeCache, ref int bridgeIndex)
        {
            if (m_FUnityUI == null)
            {
                FUnityLog.LogWarning("FUnity UI GameObject が未確定のため、ActorView を生成できません。");
                return NullActorView.Instance;
            }

            if (bridgeCache == null)
            {
                bridgeCache = new List<FooniUIBridge>(Mathf.Max(4, m_ActorVisuals.Count));
            }

            if (bridgeIndex < 0)
            {
                bridgeIndex = 0;
            }

            while (bridgeCache.Count <= bridgeIndex)
            {
                bridgeCache.Add(null);
            }

            var bridge = bridgeCache[bridgeIndex];
            if (bridge == null)
            {
                var existingBridges = m_FUnityUI.GetComponents<FooniUIBridge>();
                if (existingBridges != null && existingBridges.Length > bridgeIndex)
                {
                    bridge = existingBridges[bridgeIndex];
                }

                if (bridge == null)
                {
                    bridge = m_FUnityUI.AddComponent<FooniUIBridge>();
                    FUnityLog.LogCreateFallback("FooniUIBridge コンポーネント");
                }

                bridgeCache[bridgeIndex] = bridge;
            }

            if (bridge == null)
            {
                FUnityLog.LogWarning("FooniUIBridge コンポーネントを確保できなかったため、ActorView の構成を中止します。");
                return NullActorView.Instance;
            }

            bridgeIndex++;

            if (visual.Data != null)
            {
                bridge.defaultSpeed = Mathf.Max(0f, visual.Data.MoveSpeed);
            }

            EnsureActorElement(ref visual);

            if (visual.Element == null)
            {
                FUnityLog.LogWarning("俳優 UI 要素を生成できなかったため、ActorView の構成を中止します。");
                return NullActorView.Instance;
            }

            ActorView actorView = null;

            if (visual.View is ActorView cachedView && cachedView != null)
            {
                actorView = cachedView;
            }
            else if (visual.ViewHost != null)
            {
                actorView = visual.ViewHost.GetComponent<ActorView>();
            }

            if (actorView == null)
            {
                var host = visual.ViewHost != null ? visual.ViewHost : m_FUnityUI;
                if (host == null)
                {
                    FUnityLog.LogWarning("ActorView を追加する対象 GameObject が確定していないため、俳優ビューを構成できません。");
                    return NullActorView.Instance;
                }

                actorView = host.AddComponent<ActorView>();
                FUnityLog.LogInfo($"ActorView コンポーネントを '{host.name}' に追加しました (actor='{visual.Data?.DisplayName ?? "(Unknown)"}').");
            }

            if (actorView == null)
            {
                FUnityLog.LogWarning("ActorView コンポーネントを確保できなかったため、俳優ビューを構成できません。");
                return NullActorView.Instance;
            }

            actorView.Configure(bridge, visual.Element);
            actorView.ApplyActorData(visual.Data);
            return actorView;
        }

        /// <summary>
        /// 俳優に対応する UI 要素の存在を保証し、スタイルとポートレートを適用する。
        /// </summary>
        /// <param name="visual">俳優設定と要素の組。</param>
        private void EnsureActorElement(ref ActorVisual visual)
        {
            if (visual.Data == null)
            {
                return;
            }

            if (visual.Element == null)
            {
                var createdElement = CreateActorElement(visual.Data);
                if (createdElement == null)
                {
                    createdElement = CreateFallbackActorElement(visual.Data);
                    if (createdElement == null)
                    {
                        FUnityLog.LogWarning($"'{visual.Data.DisplayName}' の俳優要素を構築できませんでした。");
                        return;
                    }

                    FUnityLog.LogCreateFallback($"'{visual.Data.DisplayName}' 用俳優要素 (フォールバック)");
                }

                AttachActorElementToStage(createdElement);
                visual.Element = createdElement;
            }

            if (visual.Element == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(visual.Element.name) && !string.IsNullOrEmpty(visual.Data.DisplayName))
            {
                visual.Element.name = visual.Data.DisplayName;
            }

            ApplyActorElementStyles(visual.Element, visual.Data);
            ApplyActorPortrait(visual.Element, visual.Data);
        }

        /// <summary>
        /// 俳優要素を <see cref="StageElement"/> の俳優コンテナへ追加する。StageElement が存在しない場合は UIDocument 直下へ追加す
        /// る。
        /// </summary>
        /// <param name="element">追加対象の要素。</param>
        private void AttachActorElementToStage(VisualElement element)
        {
            if (element == null || element.parent != null)
            {
                return;
            }

            if (m_StageElement != null)
            {
                m_StageElement.AddActorElement(element);
                return;
            }

            var root = m_UIDocument != null ? m_UIDocument.rootVisualElement : null;
            if (root == null)
            {
                FUnityLog.LogWarning("UIDocument の rootVisualElement が null のため、俳優要素を追加できません。");
                return;
            }

            root.Add(element);
        }

        /// <summary>
        /// スタイルシートを適用し、サイズや初期座標を設定する。
        /// </summary>
        /// <param name="element">対象の UI 要素。</param>
        /// <param name="data">参照する俳優設定。</param>
        private void ApplyActorElementStyles(VisualElement element, FUnityActorData data)
        {
            if (element == null)
            {
                return;
            }

            var actorRoot = ResolveActorRootElement(element);
            if (actorRoot == null && data != null)
            {
                var displayName = string.IsNullOrEmpty(data.DisplayName) ? "(Unknown)" : data.DisplayName;
                FUnityLog.LogWarning($"'{displayName}' の俳優コンテナ (#root / .actor-root) が見つからないため、InitialPosition の適用をスキップします。");
            }

            var styleSheet = LoadActorStyleSheet();
            if (styleSheet != null)
            {
                if (!ContainsStyleSheet(element, styleSheet))
                {
                    element.styleSheets.Add(styleSheet);
                }

                if (actorRoot != null && !ContainsStyleSheet(actorRoot, styleSheet))
                {
                    actorRoot.styleSheets.Add(styleSheet);
                }
            }

            element.style.position = Position.Relative;
            element.style.left = StyleKeyword.Auto;
            element.style.top = StyleKeyword.Auto;
            element.style.translate = new Translate(0f, 0f);
            element.style.flexGrow = 0f;
            element.style.flexShrink = 0f;

            if (actorRoot != null)
            {
                actorRoot.style.position = Position.Absolute;
                actorRoot.style.left = data != null ? data.InitialPosition.x : 0f;
                actorRoot.style.top = data != null ? data.InitialPosition.y : 0f;
                actorRoot.style.translate = new Translate(0f, 0f);
                actorRoot.style.flexGrow = 0f;
                actorRoot.style.flexShrink = 0f;

                if (data != null)
                {
                    var size = data.Size;
                    actorRoot.style.width = size.x > 0f ? size.x : StyleKeyword.Auto;
                    actorRoot.style.height = size.y > 0f ? size.y : StyleKeyword.Auto;
                }
                else
                {
                    actorRoot.style.width = StyleKeyword.Auto;
                    actorRoot.style.height = StyleKeyword.Auto;
                }
            }
            else if (data != null)
            {
                var fallbackSize = data.Size;
                element.style.width = fallbackSize.x > 0f ? fallbackSize.x : StyleKeyword.Auto;
                element.style.height = fallbackSize.y > 0f ? fallbackSize.y : StyleKeyword.Auto;
            }

            element.AddToClassList("actor-container");
            element.AddToClassList("actor");
            if (actorRoot != null)
            {
                actorRoot.AddToClassList("actor");
            }
        }

        /// <summary>
        /// UXML で定義された俳優コンテナを名前優先で探索し、見つからなければクラス名で解決する。
        /// </summary>
        /// <param name="element">探索の起点となる VisualElement。</param>
        /// <returns>見つかった俳優コンテナ。存在しない場合は null。</returns>
        private static VisualElement ResolveActorRootElement(VisualElement element)
        {
            if (element == null)
            {
                return null;
            }

            var byName = element.Q<VisualElement>(RootElementName);
            if (byName != null)
            {
                return byName;
            }

            return element.Q<VisualElement>(className: RootElementClassName);
        }

        /// <summary>
        /// ポートレート画像を適用し、未設定時は単色背景へフォールバックする。
        /// </summary>
        /// <param name="element">検索対象の UI 要素。</param>
        /// <param name="data">俳優設定。</param>
        private void ApplyActorPortrait(VisualElement element, FUnityActorData data)
        {
            if (element == null)
            {
                return;
            }

            var root = element.Q<VisualElement>(RootElementName) ?? element;
            var portrait = root.Q<VisualElement>(PortraitElementName)
                ?? root.Q<VisualElement>(className: PortraitElementName)
                ?? element.Q<VisualElement>(PortraitElementName)
                ?? element.Q<VisualElement>(className: PortraitElementName);

            if (portrait == null)
            {
                FUnityLog.LogWarning($"'{data?.DisplayName ?? "(Unknown)"}' のポートレート要素が見つかりません。");
                return;
            }

            Sprite resolvedSprite = null;
            if (data != null)
            {
                if (data.PortraitSprite != null)
                {
                    resolvedSprite = data.PortraitSprite;
                }
                else if (data.Sprites != null)
                {
                    var spriteList = data.Sprites;
                    for (var i = 0; i < spriteList.Count; i++)
                    {
                        var candidate = spriteList[i];
                        if (candidate != null)
                        {
                            resolvedSprite = candidate;
                            break;
                        }
                    }
                }
            }

            var texture = data?.Portrait;

            Image imageElement;
            if (portrait is Image existingImage)
            {
                imageElement = existingImage;
            }
            else
            {
                imageElement = portrait.Q<Image>("portrait-image") ?? portrait.Q<Image>(className: "portrait-image");
                if (imageElement == null)
                {
                    imageElement = new Image
                    {
                        name = "portrait-image",
                        scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore,
                    };
                    imageElement.style.flexGrow = 1f;
                }

                if (imageElement.parent != portrait)
                {
                    imageElement.RemoveFromHierarchy();
                    portrait.Insert(0, imageElement);
                }
            }

            portrait.style.backgroundImage = new StyleBackground();
            portrait.style.backgroundSize = StyleKeyword.Null;
            portrait.style.backgroundColor = StyleKeyword.Null;
            imageElement.scaleMode = ScaleMode.ScaleToFit;
            imageElement.pickingMode = PickingMode.Ignore;
            imageElement.style.flexGrow = 1f;

            if (resolvedSprite != null)
            {
                imageElement.sprite = resolvedSprite;
                imageElement.image = null;
                return;
            }

            if (texture != null)
            {
                imageElement.sprite = null;
                imageElement.image = texture;
                return;
            }

            imageElement.sprite = null;
            imageElement.image = null;
            portrait.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            FUnityLog.LogWarning($"'{data?.DisplayName ?? "(Unknown)"}' のポートレート Sprite/Texture が設定されていないため単色背景を使用します。");
        }

        /// <summary>
        /// Resources から俳優用 VisualTreeAsset を読み込む。既定パスが失敗した場合はフォールバックを試みる。
        /// </summary>
        /// <returns>読み込んだ VisualTreeAsset。両方失敗した場合は null。</returns>
        private static VisualTreeAsset LoadActorVisualTreeAsset()
        {
            var primary = Resources.Load<VisualTreeAsset>(DefaultActorTemplatePath);
            if (primary != null)
            {
                return primary;
            }

            FUnityLog.LogMissingResource("俳優 UI テンプレート (VisualTreeAsset)", $"{DefaultActorTemplatePath}.uxml");

            var fallback = Resources.Load<VisualTreeAsset>(FallbackActorTemplatePath);
            if (fallback == null)
            {
                FUnityLog.LogMissingResource("俳優 UI テンプレート (VisualTreeAsset)", $"{FallbackActorTemplatePath}.uxml");
            }

            return fallback;
        }

        /// <summary>
        /// Resources から俳優用 StyleSheet を読み込む。既定パスが存在しない場合はフォールバックを試みる。
        /// </summary>
        /// <returns>読み込んだ StyleSheet。見つからない場合は null。</returns>
        private static StyleSheet LoadActorStyleSheet()
        {
            var primary = Resources.Load<StyleSheet>(DefaultActorTemplatePath);
            if (primary != null)
            {
                return primary;
            }

            FUnityLog.LogMissingResource("俳優 UI スタイル", $"{DefaultActorTemplatePath}.uss");

            var fallback = Resources.Load<StyleSheet>(FallbackActorTemplatePath);
            if (fallback == null)
            {
                FUnityLog.LogMissingResource("俳優 UI スタイル", $"{FallbackActorTemplatePath}.uss");
            }

            return fallback;
        }

        /// <summary>
        /// 指定したスタイルシートが既に追加されているかを確認する。
        /// </summary>
        /// <param name="element">確認対象の要素。</param>
        /// <param name="styleSheet">探索するスタイルシート。</param>
        /// <returns>既に含まれている場合は <c>true</c>。</returns>
        private static bool ContainsStyleSheet(VisualElement element, StyleSheet styleSheet)
        {
            if (element == null || styleSheet == null)
            {
                return false;
            }

            for (var i = 0; i < element.styleSheets.count; i++)
            {
                if (element.styleSheets[i] == styleSheet)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// テンプレートが存在しない場合の最小俳優要素を生成する。
        /// </summary>
        /// <param name="data">名前表示に使用する俳優設定。</param>
        /// <returns>生成した要素。Resources 読み込みも失敗した場合は単純な <see cref="VisualElement"/>。</returns>
        private static VisualElement CreateFallbackActorElement(FUnityActorData data)
        {
            var visualTree = LoadActorVisualTreeAsset();
            if (visualTree != null)
            {
                return visualTree.CloneTree();
            }

            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Column;
            element.style.alignItems = Align.Center;
            element.style.justifyContent = Justify.Center;
            element.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            element.style.minWidth = 96f;
            element.style.minHeight = 96f;
            element.style.paddingTop = 8f;
            element.style.paddingBottom = 8f;
            element.style.paddingLeft = 8f;
            element.style.paddingRight = 8f;

            var label = new Label(!string.IsNullOrEmpty(data?.DisplayName) ? data.DisplayName : "Actor");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginBottom = 4f;
            label.style.marginTop = 4f;
            label.style.flexGrow = 0f;

            element.Add(label);

            FUnityLog.LogCreateFallback("簡易俳優 UI 要素");
            return element;
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
        /// Scratch モード時にステージサイズを固定し、領域外の描画をマスクしながら、他モードでは柔軟レイアウトへ戻す。
        /// </summary>
        /// <param name="stageElement">対象のステージ要素。</param>
        private void ApplyScratchStageSizing(StageElement stageElement)
        {
            if (stageElement == null)
            {
                return;
            }

            var modeConfig = ResolveActiveModeConfig();
            var stageSize = stageElement.StageSize;
            if (stageSize.x <= 0 || stageSize.y <= 0)
            {
                var fallbackWidth = modeConfig != null ? Mathf.Max(1, modeConfig.ScratchStageWidth) : FUnityStageData.DefaultStageWidth;
                var fallbackHeight = modeConfig != null ? Mathf.Max(1, modeConfig.ScratchStageHeight) : FUnityStageData.DefaultStageHeight;
                stageSize = new Vector2Int(fallbackWidth, fallbackHeight);
            }

            if (modeConfig != null && modeConfig.Mode == FUnityAuthoringMode.Scratch && modeConfig.UseScratchFixedStage)
            {
                stageElement.style.width = stageSize.x;
                stageElement.style.height = stageSize.y;
                stageElement.style.flexGrow = 0f;
                stageElement.style.flexShrink = 0f;
                stageElement.style.marginLeft = StyleKeyword.Auto;
                stageElement.style.marginRight = StyleKeyword.Auto;
                stageElement.style.overflow = Overflow.Visible;
                stageElement.AddToClassList(StageElement.ScratchStageClassName);
                return;
            }

            stageElement.style.width = StyleKeyword.Null;
            stageElement.style.height = StyleKeyword.Null;
            stageElement.style.flexGrow = 1f;
            stageElement.style.flexShrink = 0f;
            stageElement.style.marginLeft = StyleKeyword.Null;
            stageElement.style.marginRight = StyleKeyword.Null;
            stageElement.style.overflow = StyleKeyword.Null;
            stageElement.RemoveFromClassList(StageElement.ScratchStageClassName);
        }

        /// <summary>
        /// ステージ設定を <see cref="StageElement"/> と背景サービスへ反映する。
        /// </summary>
        /// <param name="stageElement">UI 上のステージ要素。</param>
        /// <param name="stage">ステージ設定。</param>
        private void ApplyStage(StageElement stageElement, FUnityStageData stage)
        {
            stageElement?.Configure(stage);
            ApplyScratchStageSizing(stageElement);

            if (stage == null)
            {
                return;
            }

            m_StageBackgroundService.SetBackgroundColor(stage.BackgroundColor);

            if (stage.BackgroundImage != null)
            {
                m_StageBackgroundService.SetBackground(stage.BackgroundImage, stage.BackgroundScale);
                return;
            }

            m_StageBackgroundService.SetBackgroundFromResources(DefaultStageBackgroundName, stage.BackgroundScale);
        }

        /// <summary>
        /// アクティブモード設定を取得し、ProjectData の選択結果に基づいて Scratch 固有設定を適用する。
        /// </summary>
        /// <returns>取得した <see cref="FUnityModeConfig"/>。見つからない場合は null。</returns>
        private FUnityModeConfig ResolveActiveModeConfig()
        {
            if (m_Project != null)
            {
                var fromProject = m_Project.GetActiveModeConfig();
                if (fromProject != null)
                {
                    m_ActiveModeConfig = fromProject;
                    m_ActiveModeConfig.EnsureScratchStageDefaults();
                    m_LoggedMissingModeConfig = false;
                    return m_ActiveModeConfig;
                }

                if (!m_LoggedMissingModeConfig)
                {
                    Debug.LogWarning("[FUnity] FUnityProjectData に ModeConfig が割り当てられていません。メニュー FUnity/Create/FUnityProjectData を再実行するか、Inspector で ModeConfig を設定してください。暫定的に FUnityManager の ActiveModeConfig を利用します。");
                    m_LoggedMissingModeConfig = true;
                }
            }
            else if (!m_LoggedMissingModeConfig)
            {
                Debug.LogWarning("[FUnity] FUnityProjectData が未設定のため ModeConfig を決定できません。メニュー FUnity/Create/FUnityProjectData を実行して ProjectData を作成してください。");
                m_LoggedMissingModeConfig = true;
            }

            if (m_ActiveModeConfig != null)
            {
                m_ActiveModeConfig.EnsureScratchStageDefaults();
                return m_ActiveModeConfig;
            }

            return null;
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

            VisualElement templateInstance = null;

            if (actor.ElementUxml != null)
            {
                templateInstance = actor.ElementUxml.Instantiate();
            }
            else
            {
                templateInstance = new FooniElement();
            }

            if (templateInstance == null)
            {
                return null;
            }

            if (actor.ElementStyle != null)
            {
                templateInstance.styleSheets.Add(actor.ElementStyle);
            }

            var container = new VisualElement
            {
                focusable = false
            };
            container.AddToClassList("actor-container");
            container.style.position = Position.Relative;
            container.style.left = StyleKeyword.Auto;
            container.style.top = StyleKeyword.Auto;
            container.style.translate = new Translate(0f, 0f);
            container.style.flexGrow = 0f;
            container.style.flexShrink = 0f;

            container.Add(templateInstance);

            var actorRoot = ResolveActorRootElement(container);

            ApplyActorPortrait(actorRoot ?? container, actor);

            if (actorRoot != null)
            {
                var size = actor.Size;
                actorRoot.style.width = size.x > 0f ? size.x : StyleKeyword.Auto;
                actorRoot.style.height = size.y > 0f ? size.y : StyleKeyword.Auto;
                actorRoot.style.flexGrow = 0f;
                actorRoot.style.flexShrink = 0f;
                actorRoot.style.position = Position.Absolute;
                actorRoot.style.left = actor.InitialPosition.x;
                actorRoot.style.top = actor.InitialPosition.y;
                actorRoot.style.translate = new Translate(0f, 0f);
                actorRoot.AddToClassList("actor");
            }

            container.AddToClassList("actor");

            if (string.IsNullOrEmpty(container.name) && !string.IsNullOrEmpty(actor.DisplayName))
            {
                container.name = actor.DisplayName;
            }

            return container;
        }

        /// <summary>
        /// <see cref="ActorPresenterAdapter"/> に俳優要素をバインドし、浮遊アニメーション設定を同期する。
        /// </summary>
        /// <param name="actorVE">俳優 UI 要素。</param>
        /// <param name="data">俳優設定。</param>
        private void AttachControllerIfNeeded(VisualElement actorVE, FUnityActorData data)
        {
            if (actorVE == null)
            {
                return;
            }

            ResolveDefaultActorPresenterAdapter();
            var controller = m_DefaultActorPresenterAdapter;

            if (controller == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter がシーンに存在しません。俳優要素と Presenter を橋渡しするコンポーネントを配置してください。");
                return;
            }

            if (controller.BoundElement != null && controller.BoundElement != actorVE)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter が他の要素に結合済みのため、最新の俳優に再バインドします。");
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

                    Unity.VisualScripting.Variables.Object(go).Set(variable.key, variable.value);
                }
            }

            if (!Unity.VisualScripting.Variables.Object(go).IsDefined("FUnityUI") && m_FUnityUI != null)
            {
                Unity.VisualScripting.Variables.Object(go).Set("FUnityUI", m_FUnityUI);
            }

            if (m_VsBridge == null)
            {
                EnsurePresenterBridge();
            }

            if (!Unity.VisualScripting.Variables.Object(go).IsDefined("VSPresenterBridge") && m_VsBridge != null)
            {
                Unity.VisualScripting.Variables.Object(go).Set("VSPresenterBridge", m_VsBridge);
            }
        }

        /// <summary>
        /// 俳優 Runner をすべて破棄し、次回初期化時の重複生成を防ぐ。
        /// </summary>
        private void CleanupActorRunners()
        {
            for (var i = m_ActorRunnerInstances.Count - 1; i >= 0; i--)
            {
                var runner = m_ActorRunnerInstances[i];
                if (runner != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(runner);
                    }
#if UNITY_EDITOR
                    else
                    {
                        DestroyImmediate(runner);
                    }
#else
                    else
                    {
                        Destroy(runner);
                    }
#endif
                }

                m_ActorRunnerInstances.RemoveAt(i);
            }

            if (m_ActorRunnerRoot != null && m_ActorRunnerRoot.transform.childCount == 0)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_ActorRunnerRoot);
                }
#if UNITY_EDITOR
                else
                {
                    DestroyImmediate(m_ActorRunnerRoot);
                }
#else
                else
                {
                    Destroy(m_ActorRunnerRoot);
                }
#endif

                m_ActorRunnerRoot = null;
            }

            m_ActorRunnerInstances.Clear();
            m_ActorScriptMachines.Clear();
        }

        /// <summary>
        /// 俳優 Runner を格納するルート GameObject を生成または再利用する。
        /// </summary>
        /// <returns>Runner をぶら下げる親 GameObject。</returns>
        private GameObject EnsureActorRunnerRoot()
        {
            if (m_ActorRunnerRoot != null)
            {
                return m_ActorRunnerRoot;
            }

            const string rootName = "FUnity VS Runners";
            var found = GameObject.Find(rootName);
            if (found != null && found.transform.parent != transform)
            {
                found.transform.SetParent(transform, false);
            }

            m_ActorRunnerRoot = found != null ? found : new GameObject(rootName);
            m_ActorRunnerRoot.transform.SetParent(transform, false);
            return m_ActorRunnerRoot;
        }

        /// <summary>
        /// Runner 名に利用する俳優表示名を決定する。空文字の場合は Actor_{index} を返す。
        /// </summary>
        /// <param name="actor">対象の俳優データ。</param>
        /// <param name="index">俳優の列挙インデックス。</param>
        /// <returns>Runner 名に利用する表示名。</returns>
        private static string ResolveActorDisplayName(FUnityActorData actor, int index)
        {
            if (actor == null)
            {
                return $"Actor_{index}";
            }

            return string.IsNullOrEmpty(actor.DisplayName) ? $"Actor_{index}" : actor.DisplayName;
        }

        /// <summary>
        /// 俳優設定ごとに Visual Scripting Runner を生成し、必要なコンポーネントと参照を付与する。
        /// </summary>
        private void SpawnActorRunnersFromProjectData()
        {
            CleanupActorRunners();

            if (m_Project == null)
            {
                m_Project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (m_Project == null)
            {
                Debug.LogWarning("[FUnity] FUnityProjectData not found. メニュー FUnity/Create/FUnityProjectData を実行した後に Runner を再生成してください。");
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

            for (var i = 0; i < m_Project.Actors.Count; i++)
            {
                var actor = m_Project.Actors[i];
                if (actor == null)
                {
                    continue;
                }

                var runner = CreateActorRunner(actor, i);
                if (runner == null)
                {
                    continue;
                }

                ConfigureScriptMachine(actor, runner, actor.ScriptGraph);
                ConfigureActorPresenterAdapter(runner, actor);

                var objectVariables = Unity.VisualScripting.Variables.Object(runner);
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
        private GameObject CreateActorRunner(FUnityActorData actor, int index)
        {
            if (actor == null)
            {
                return null;
            }

            var displayName = ResolveActorDisplayName(actor, index);
            var runnerRoot = EnsureActorRunnerRoot();
            var runner = new GameObject($"{displayName} VS Runner");
            runner.transform.SetParent(runnerRoot != null ? runnerRoot.transform : transform, false);
            runner.tag = "Untagged";
            runner.layer = 0;

            m_ActorRunnerInstances.Add(runner);

            return runner;
        }

        /// <summary>
        /// Runner に <see cref="ScriptMachine"/> を付与し、マクロを割り当てる。
        /// </summary>
        /// <param name="runner">構成対象の Runner。</param>
        /// <param name="macro">適用する Visual Scripting マクロ。</param>
        private ScriptMachine ConfigureScriptMachine(FUnityActorData actor, GameObject runner, ScriptGraphAsset macro)
        {
            if (runner == null)
            {
                return null;
            }

            var machine = runner.GetComponent<ScriptMachine>();
            if (machine == null)
            {
                machine = runner.AddComponent<ScriptMachine>();
            }

            if (macro == null)
            {
                Debug.LogWarning($"[FUnity] ScriptGraph is null. Runner='{runner.name}'");
                if (actor != null)
                {
                    m_ActorScriptMachines.Remove(actor);
                }

                return machine;
            }

            machine.nest.source = GraphSource.Macro;
            machine.nest.macro = macro;

            if (actor != null)
            {
                m_ActorScriptMachines[actor] = machine;
            }

            Debug.Log($"[FUnity] Spawned actor runner: {runner.name}");
            return machine;
        }

        /// <summary>
        /// Runner に <see cref="ActorPresenterAdapter"/> を設定し、俳優 Presenter との橋渡しを行う。
        /// </summary>
        /// <param name="runner">構成対象の Runner。</param>
        /// <param name="actor">俳優設定。</param>
        private void ConfigureActorPresenterAdapter(GameObject runner, FUnityActorData actor)
        {
            if (runner == null || actor == null)
            {
                return;
            }

            var controller = runner.GetComponent<ActorPresenterAdapter>();
            if (controller == null)
            {
                controller = runner.AddComponent<ActorPresenterAdapter>();
            }

            if (!actor.FloatAnimation)
            {
                controller.EnableFloat(false);
            }

            RegisterControllerForActor(actor, controller);
        }

        /// <summary>
        /// 指定した俳優の ScriptMachine に Presenter・UI コンテキストをバインドし、グラフ内の Self を分離する。
        /// </summary>
        /// <param name="actor">対象の俳優設定。</param>
        /// <param name="presenter">Self として注入する Presenter。</param>
        /// <param name="view">UI 反映を担当する View。</param>
        /// <param name="uiElement">俳優に対応する UI 要素。</param>
        private void BindActorGraphContext(FUnityActorData actor, ActorPresenter presenter, IActorView view, VisualElement uiElement)
        {
            if (actor == null || presenter == null)
            {
                return;
            }

            if (!m_ActorScriptMachines.TryGetValue(actor, out var machine) || machine == null)
            {
                var displayName = string.IsNullOrEmpty(actor.DisplayName) ? "(Unknown)" : actor.DisplayName;
                FUnityLog.LogWarning($"'{displayName}' 用の ScriptMachine が未割り当てのため Graph Variables への Self バインドをスキップします。");
                return;
            }

            VisualElement resolvedUi = uiElement;
            if (view is ActorView actorView)
            {
                resolvedUi = actorView.ActorRoot ?? actorView.BoundElement ?? resolvedUi;
            }

            presenter.BindScriptMachine(machine, resolvedUi);
        }

        /// <summary>
        /// 俳優設定に紐づく Presenter が未初期化の場合、ActorPresenterAdapter を保留リストに追加する。Presenter 済みであれば即座に結線する。
        /// </summary>
        /// <param name="actor">紐付け対象の俳優設定。</param>
        /// <param name="controller">Presenter へ命令を委譲する ActorPresenterAdapter。</param>
        private void RegisterControllerForActor(FUnityActorData actor, ActorPresenterAdapter controller)
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
                controllers = new List<ActorPresenterAdapter>();
                m_PendingActorControllers[actor] = controllers;
            }

            if (!controllers.Contains(controller))
            {
                controllers.Add(controller);
            }
        }

        /// <summary>
        /// 初期化済みの Presenter を同一俳優の ActorPresenterAdapter 群へ割り当てる。
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

        /// <summary>
        /// Presenter を複製してクローンを生成します。
        /// </summary>
        /// <param name="original">複製元の Presenter。</param>
        /// <returns>生成されたクローン Presenter。失敗時は null。</returns>
        public static ActorPresenter CloneActor(ActorPresenter original)
        {
            var manager = Instance != null ? Instance : FindObjectOfType<FUnityManager>();
            if (manager == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager.CloneActor: FUnityManager が見つからないためクローンを生成できません。");
                return null;
            }

            return manager.CloneActorInternal(original);
        }

        /// <summary>
        /// Scratch の「緑の旗が押されたとき」イベントを全俳優（クローン除く）へ配信します。
        /// </summary>
        public static void TriggerGreenFlag()
        {
            var manager = Instance != null ? Instance : FindObjectOfType<FUnityManager>();
            if (manager == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager.TriggerGreenFlag: FUnityManager が見つからないためイベントを発火できません。");
                return;
            }

            manager.TriggerGreenFlagInternal();
        }

        /// <summary>
        /// 指定したクローン Presenter を破棄します。
        /// </summary>
        /// <param name="clonePresenter">破棄対象の Presenter。</param>
        public static void DeleteActorClone(ActorPresenter clonePresenter)
        {
            var manager = Instance != null ? Instance : FindObjectOfType<FUnityManager>();
            if (manager == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager.DeleteActorClone: FUnityManager が見つからないためクローンを削除できません。");
                return;
            }

            manager.DeleteActorCloneInternal(clonePresenter);
        }

        /// <summary>
        /// 複製元 Presenter をもとにクローンを生成し、状態をコピーします。
        /// </summary>
        /// <param name="original">複製元の Presenter。</param>
        /// <returns>生成したクローン Presenter。失敗時は null。</returns>
        private ActorPresenter CloneActorInternal(ActorPresenter original, bool suppressCloneStartEvent = false)
        {
            if (original == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager: CloneActorInternal に null が渡されたため処理を中止します。");
                return null;
            }

            if (!TryResolveCloneContext(original, out var actorData, out _, out var sourceRunner, out _))
            {
                Debug.LogWarning("[FUnity] FUnityManager: 複製元のコンテキストを解決できないためクローンを生成できません。");
                return null;
            }

            var cloneVisual = new ActorVisual
            {
                Data = actorData
            };

            EnsureActorElement(ref cloneVisual);
            if (cloneVisual.Element == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager: クローン用の俳優要素を生成できなかったため処理を終了します。");
                return null;
            }

            var displayName = actorData != null && !string.IsNullOrEmpty(actorData.DisplayName)
                ? actorData.DisplayName
                : "Actor";
            if (string.IsNullOrEmpty(cloneVisual.Element.name))
            {
                cloneVisual.Element.name = ($"{displayName} {CloneSuffix}").Trim();
            }

            var hostNameBase = string.IsNullOrEmpty(displayName) ? "Actor" : displayName;
            var viewHostParent = m_FUnityUI != null ? m_FUnityUI.transform : transform;
            var cloneViewHost = new GameObject($"{hostNameBase} Clone View");
            cloneViewHost.transform.SetParent(viewHostParent, false);
            cloneVisual.ViewHost = cloneViewHost;

            List<FooniUIBridge> bridgeCache = null;
            var bridgeIndex = 0;
            var view = CreateOrConfigureActorView(ref cloneVisual, ref bridgeCache, ref bridgeIndex);
            if (view == null || ReferenceEquals(view, NullActorView.Instance))
            {
                Debug.LogWarning("[FUnity] FUnityManager: クローン用 View の構築に失敗したため生成を中止します。");
                cloneVisual.Element.RemoveFromHierarchy();
                Object.Destroy(cloneViewHost);
                return null;
            }

            var state = new ActorState();
            var presenter = new ActorPresenter();
            var stageRoot = m_StageElement != null ? m_StageElement.ActorContainer : null;
            presenter.Initialize(actorData, state, view, m_ActiveModeConfig, stageRoot);
            view.SetActorPresenter(presenter);

            GameObject cloneRunner = null;
            ScriptMachine cloneMachine = null;
            ActorPresenterAdapter cloneAdapter = null;

            if (sourceRunner != null)
            {
                cloneRunner = Object.Instantiate(sourceRunner, sourceRunner.transform.parent);
                cloneRunner.name = $"{sourceRunner.name} {CloneSuffix}";
                cloneMachine = cloneRunner.GetComponent<ScriptMachine>();
                cloneAdapter = cloneRunner.GetComponent<ActorPresenterAdapter>();
            }
            else
            {
                Debug.LogWarning("[FUnity] FUnityManager: 複製元 Runner が未設定のため、クローン Runner を生成できません。");
            }

            if (cloneAdapter != null)
            {
                cloneAdapter.BindActorElement(cloneVisual.Element);
                if (actorData != null)
                {
                    cloneAdapter.EnableFloat(actorData.FloatAnimation);
                }
                cloneAdapter.SetActorPresenter(presenter);
            }
            else if (cloneRunner != null)
            {
                Debug.LogWarning($"[FUnity] FUnityManager: '{cloneRunner.name}' に ActorPresenterAdapter が見つからないため、自動解決に依存します。");
            }

            if (cloneMachine != null)
            {
                presenter.BindScriptMachine(cloneMachine, cloneVisual.Element);
            }
            else if (cloneRunner != null)
            {
                Debug.LogWarning($"[FUnity] FUnityManager: '{cloneRunner.name}' に ScriptMachine が見つからないため Graph Variables への Self 登録をスキップします。");
            }

            if (cloneRunner != null)
            {
                var runnerVariables = Unity.VisualScripting.Variables.Object(cloneRunner);
                if (runnerVariables != null)
                {
                    runnerVariables.Set("presenter", presenter);
                    runnerVariables.Set(nameof(ActorPresenter), presenter);
                    if (cloneAdapter != null)
                    {
                        runnerVariables.Set("adapter", cloneAdapter);
                        runnerVariables.Set(nameof(ActorPresenterAdapter), cloneAdapter);
                    }
                    runnerVariables.Set("view", view);
                    runnerVariables.Set("ui", cloneVisual.Element);
                }

                m_ActorRunnerInstances.Add(cloneRunner);
            }

            presenter.Runner = cloneRunner;
            presenter.IsClone = true;
            presenter.Original = original;
            presenter.CopyRuntimeStateFrom(original);

            var cloneInfo = new ActorCloneRuntime
            {
                ActorData = actorData,
                Presenter = presenter,
                View = view,
                Element = cloneVisual.Element,
                ViewHost = cloneVisual.ViewHost,
                Runner = cloneRunner,
                Adapter = cloneAdapter
            };

            m_RuntimeActorClones.Add(cloneInfo);
            m_ActorPresenters.Add(presenter);

            m_VariableService.RegisterActor(presenter, original);

            if (!suppressCloneStartEvent)
            {
                RaiseVsEvent_WhenStartAsClone(cloneAdapter, presenter);
            }

            return presenter;
        }

        /// <summary>
        /// DisplayName で指定された俳優設定をテンプレートとしてクローンを生成します。
        /// </summary>
        /// <param name="source">クローン生成を要求したアダプタ。null 時は警告のみ表示します。</param>
        /// <param name="template">複製元となる俳優設定。</param>
        /// <returns>生成したクローンの <see cref="ActorPresenterAdapter"/>。失敗時は null。</returns>
        internal ActorPresenterAdapter SpawnCloneFromTemplate(ActorPresenterAdapter source, FUnityActorData template)
        {
            if (template == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager: SpawnCloneFromTemplate に null テンプレートが渡されたため処理を中止します。");
                return null;
            }

            if (source == null)
            {
                Debug.LogWarning($"[FUnity] FUnityManager: SpawnCloneFromTemplate に送信元アダプタが指定されていません。(template={template.DisplayName})");
            }

            if (!m_ActorPresenterMap.TryGetValue(template, out var originalPresenter) || originalPresenter == null)
            {
                Debug.LogWarning($"[FUnity] FUnityManager: '{template.DisplayName}' の Presenter を解決できないためクローンを生成できません。");
                return null;
            }

            var clonePresenter = CloneActorInternal(originalPresenter, true);
            if (clonePresenter == null)
            {
                return null;
            }

            var cloneAdapter = ResolveAdapterForPresenter(clonePresenter);
            RaiseVsEvent_WhenStartAsClone(cloneAdapter, clonePresenter);
            return cloneAdapter;
        }

        /// <summary>
        /// クローン生成直後の VS イベントを発火し、「クローンされたとき」ユニットを起動します。
        /// </summary>
        /// <param name="cloneAdapter">生成したクローンのアダプタ。null の場合は presenter から解決を試みます。</param>
        /// <param name="clonePresenter">クローンの Presenter。null の場合は adapter から取得します。</param>
        internal void RaiseVsEvent_WhenStartAsClone(ActorPresenterAdapter cloneAdapter, ActorPresenter clonePresenter = null)
        {
            var presenter = clonePresenter ?? (cloneAdapter != null ? cloneAdapter.Presenter : null);
            object eventTarget = null;

            if (cloneAdapter != null)
            {
                eventTarget = cloneAdapter.gameObject;
            }

            if (eventTarget == null)
            {
                if (cloneAdapter != null && TryFindRuntimeClone(cloneAdapter, out var runtimeByAdapter))
                {
                    eventTarget = runtimeByAdapter.Runner != null ? (object)runtimeByAdapter.Runner : runtimeByAdapter.ViewHost;
                }
                else if (presenter != null && TryFindRuntimeClone(presenter, out var runtimeByPresenter))
                {
                    eventTarget = runtimeByPresenter.Runner != null ? (object)runtimeByPresenter.Runner : runtimeByPresenter.ViewHost;
                }
            }

            if (eventTarget == null)
            {
                Debug.LogWarning("[FUnity] FUnityManager: クローン開始イベントを発火する対象を特定できませんでした。");
                return;
            }

            EventBus.Trigger(new EventHook(FUnityEventNames.OnCloneStart, eventTarget), new CloneEventArgs());
        }

        /// <summary>
        /// 指定した Presenter に対応するクローンのアダプタを解決します。
        /// </summary>
        /// <param name="presenter">対象の Presenter。</param>
        /// <returns>対応する <see cref="ActorPresenterAdapter"/>。見つからない場合は null。</returns>
        private ActorPresenterAdapter ResolveAdapterForPresenter(ActorPresenter presenter)
        {
            if (presenter == null)
            {
                return null;
            }

            if (presenter.Runner != null)
            {
                var fromRunner = presenter.Runner.GetComponent<ActorPresenterAdapter>();
                if (fromRunner != null)
                {
                    return fromRunner;
                }
            }

            if (TryFindRuntimeClone(presenter, out var runtime))
            {
                return runtime.Adapter;
            }

            return null;
        }

        /// <summary>
        /// Runtime で追跡中のクローン情報から、指定アダプタに対応するエントリを検索します。
        /// </summary>
        /// <param name="adapter">検索対象のアダプタ。</param>
        /// <param name="info">見つかった場合のクローン情報。</param>
        /// <returns>見つかった場合 true。</returns>
        private bool TryFindRuntimeClone(ActorPresenterAdapter adapter, out ActorCloneRuntime info)
        {
            for (var i = 0; i < m_RuntimeActorClones.Count; i++)
            {
                var entry = m_RuntimeActorClones[i];
                if (entry.Adapter == adapter)
                {
                    info = entry;
                    return true;
                }
            }

            info = default;
            return false;
        }

        /// <summary>
        /// Runtime で追跡中のクローン情報から、指定 Presenter に対応するエントリを検索します。
        /// </summary>
        /// <param name="presenter">検索対象の Presenter。</param>
        /// <param name="info">見つかった場合のクローン情報。</param>
        /// <returns>見つかった場合 true。</returns>
        private bool TryFindRuntimeClone(ActorPresenter presenter, out ActorCloneRuntime info)
        {
            for (var i = 0; i < m_RuntimeActorClones.Count; i++)
            {
                var entry = m_RuntimeActorClones[i];
                if (entry.Presenter == presenter)
                {
                    info = entry;
                    return true;
                }
            }

            info = default;
            return false;
        }

        /// <summary>
        /// 登録済みの俳優 Presenter のうちクローンではない対象に対して緑の旗イベントを発火します。
        /// </summary>
        private void TriggerGreenFlagInternal()
        {
            if (m_ActorPresenters.Count == 0)
            {
                return;
            }

            var eventArgs = new EmptyEventArgs();
            var dispatched = false;

            for (var i = 0; i < m_ActorPresenters.Count; i++)
            {
                var presenter = m_ActorPresenters[i];
                if (presenter == null || presenter.IsClone)
                {
                    continue;
                }

                var runner = presenter.Runner;
                if (runner == null)
                {
                    Debug.LogWarning("[FUnity] FUnityManager: Runner が未割り当ての俳優が存在するため、緑の旗イベントをスキップしました。");
                    continue;
                }

                EventBus.Trigger(new EventHook(FUnityEventNames.OnGreenFlag, runner), eventArgs);
                dispatched = true;
            }

            if (!dispatched)
            {
                Debug.LogWarning("[FUnity] FUnityManager: 緑の旗イベントを配送できる俳優が存在しませんでした。");
            }
        }

        /// <summary>
        /// 指定したクローン Presenter を破棄し、関連する Runner や UI を後始末します。
        /// </summary>
        /// <param name="presenter">破棄するクローン Presenter。</param>
        private void DeleteActorCloneInternal(ActorPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            if (!presenter.IsClone)
            {
                Debug.LogWarning("[FUnity] FUnityManager: DeleteActorCloneInternal が本体 Presenter に対して呼び出されたため処理をスキップします。");
                return;
            }

            var index = m_RuntimeActorClones.FindIndex(x => x.Presenter == presenter);
            if (index < 0)
            {
                Debug.LogWarning("[FUnity] FUnityManager: 破棄対象のクローンが追跡リストに存在しません。");
                return;
            }

            var info = m_RuntimeActorClones[index];
            m_RuntimeActorClones.RemoveAt(index);
            m_VariableService.UnregisterActor(presenter);
            m_ActorPresenters.Remove(presenter);

            if (info.Element != null)
            {
                info.Element.RemoveFromHierarchy();
            }

            if (info.ViewHost != null)
            {
                Object.Destroy(info.ViewHost);
            }

            if (info.Runner != null)
            {
                m_ActorRunnerInstances.Remove(info.Runner);
                Object.Destroy(info.Runner);
            }

            presenter.IsClone = false;
            presenter.Original = null;
            presenter.Runner = null;

            if (m_VsBridge != null && m_VsBridge.Target == presenter)
            {
                m_VsBridge.Target = m_ActorPresenters.Count > 0 ? m_ActorPresenters[0] : null;
            }
        }

        /// <summary>
        /// クローン生成元の情報を解析し、俳優設定や Runner を取得します。
        /// </summary>
        /// <param name="presenter">解析対象の Presenter。</param>
        /// <param name="actorData">関連付けられた俳優設定。</param>
        /// <param name="element">割り当て済みの UI 要素。</param>
        /// <param name="runner">紐付く Runner。</param>
        /// <param name="adapter">Runner 上の ActorPresenterAdapter。</param>
        /// <returns>解決に成功した場合は true。</returns>
        private bool TryResolveCloneContext(ActorPresenter presenter, out FUnityActorData actorData, out VisualElement element, out GameObject runner, out ActorPresenterAdapter adapter)
        {
            actorData = null;
            element = null;
            runner = null;
            adapter = null;

            if (presenter == null)
            {
                return false;
            }

            for (var i = 0; i < m_ActorVisuals.Count; i++)
            {
                var visual = m_ActorVisuals[i];
                if (visual.Presenter == presenter)
                {
                    actorData = visual.Data;
                    element = visual.Element;
                    runner = presenter.Runner;
                    adapter = runner != null ? runner.GetComponent<ActorPresenterAdapter>() : null;
                    return true;
                }
            }

            for (var i = 0; i < m_RuntimeActorClones.Count; i++)
            {
                var clone = m_RuntimeActorClones[i];
                if (clone.Presenter == presenter)
                {
                    actorData = clone.ActorData;
                    element = clone.Element;
                    runner = clone.Runner;
                    adapter = clone.Adapter;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// FUnity の制作モードに関する共通ユーティリティです。ランタイムから Scratch モードかどうかを簡易判定できます。
    /// </summary>
    public static class FUnityModeUtil
    {
        /// <summary>
        /// 現在のランタイムが Scratch モードで動作しているかどうかを返します。FUnityManager の解決済み設定を参照し、
        /// ProjectData によるゲームモード指定も併せてチェックします。
        /// </summary>
        public static bool IsScratchMode
        {
            get
            {
                var manager = FUnityManager.Instance;
                if (manager == null)
                {
                    return false;
                }

                var modeConfig = manager.ActiveModeConfig;
                if (modeConfig != null)
                {
                    return modeConfig.Mode == FUnityAuthoringMode.Scratch;
                }

                var project = manager.ProjectData;
                if (project != null)
                {
                    if (project.GameMode == FUnityGameMode.Scratch)
                    {
                        return true;
                    }

                    var fallbackConfig = project.GetActiveModeConfig();
                    if (fallbackConfig != null)
                    {
                        return fallbackConfig.Mode == FUnityAuthoringMode.Scratch;
                    }
                }

                return false;
            }
        }
    }

    /// <summary>Visual Scripting 用のイベント名定数をまとめたクラス。</summary>
    public static class FUnityEventNames
    {
        /// <summary>Scratch の緑の旗が押された際に発火するイベント名。</summary>
        public const string OnGreenFlag = "FUnity.OnGreenFlag";

        /// <summary>クローン開始時に発火するイベント名。</summary>
        public const string OnCloneStart = "FUnity.OnCloneStart";
    }

    /// <summary>クローン開始イベントに付随する引数（値は持たない）。</summary>
    public readonly struct CloneEventArgs
    {
    }
}
