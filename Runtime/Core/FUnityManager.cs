using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.UI;
using FUnity.Runtime.Core;
using FUnity.Runtime.Model;
using FUnity.Runtime.View;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Input;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FUnity.Core
{
    /// <summary>
    /// 起動時に FUnity UI（UIDocument + FooniController）と VS Runner を動的生成。
    /// </summary>
    public sealed class FUnityManager : MonoBehaviour
    {
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiDocument")]
        private UIDocument m_UIDocument;

        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("project")]
        private FUnityProjectData m_Project;

        private GameObject m_FUnityUI;

        private readonly List<ActorPresenter> m_ActorPresenters = new List<ActorPresenter>();
        private readonly List<ActorVisual> m_ActorVisuals = new List<ActorVisual>();
        private InputPresenter m_InputPresenter;
        private VSPresenterBridge m_VsBridge;

        private struct ActorVisual
        {
            public FUnityActorData Data;
            public VisualElement Element;
        }

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

        private void Update()
        {
            if (m_ActorPresenters.Count == 0 || m_InputPresenter == null)
            {
                return;
            }

            var move = m_InputPresenter.ReadMove();
            var deltaTime = Time.deltaTime;

            foreach (var presenter in m_ActorPresenters)
            {
                presenter.Tick(deltaTime, move);
            }
        }

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
            controller.SetUIDocument(m_UIDocument);
        }

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
        }

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

            if (m_InputPresenter == null)
            {
                m_InputPresenter = new InputPresenter();
            }

            var bridgeCache = new List<FooniUIBridge>(m_FUnityUI.GetComponents<FooniUIBridge>());
            var bridgeIndex = 0;

            foreach (var visual in m_ActorVisuals)
            {
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
            }

            if (m_VsBridge != null)
            {
                m_VsBridge.Target = m_ActorPresenters.Count > 0 ? m_ActorPresenters[0] : null;
            }
        }

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

        private void ApplyStage(VisualElement root, FUnityStageData stage)
        {
            if (root == null || stage == null)
            {
                return;
            }

            root.style.backgroundColor = stage.BackgroundColor;

            var texture = stage.BackgroundImage;
            if (texture != null)
            {
                root.style.backgroundImage = new StyleBackground(texture);
                root.style.unityBackgroundScaleMode = stage.BackgroundScale;
            }
            else
            {
                root.style.backgroundImage = new StyleBackground();
            }
        }

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
        }

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

        private void SpawnActorRunnersFromProjectData()
        {
            var project = m_Project;
            if (project == null)
            {
                project = Resources.Load<FUnityProjectData>("FUnityProjectData");
                if (project != null)
                {
                    m_Project = project;
                }
            }

            if (project == null || project.Actors == null || project.Actors.Count == 0)
            {
                Debug.Log("[FUnity] No actors to spawn.");
                return;
            }

            Transform parent = null;
            GameObject uiGameObject = null;
            if (m_FUnityUI != null)
            {
                parent = m_FUnityUI.transform;
                uiGameObject = m_FUnityUI;
            }
            else
            {
                var uiRoot = GameObject.Find("FUnity UI");
                if (uiRoot != null)
                {
                    parent = uiRoot.transform;
                    uiGameObject = uiRoot;
                }
            }

            if (m_VsBridge == null)
            {
                EnsurePresenterBridge();
            }

            foreach (var actor in project.Actors)
            {
                if (actor == null)
                {
                    continue;
                }

                var go = new GameObject($"ActorRunner - {actor.DisplayName}");
                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                }

                var machine = go.AddComponent<ScriptMachine>();
                if (actor.ScriptGraph != null)
                {
                    machine.nest.source = GraphSource.Macro;
                    machine.nest.macro = actor.ScriptGraph;
                }
                else
                {
                    Debug.LogWarning($"[FUnity] '{actor.DisplayName}' has no ScriptGraph assigned. Runner created without macro.");
                }

                if (!Variables.Object(go).IsDefined("FUnityUI") && uiGameObject != null)
                {
                    Variables.Object(go).Set("FUnityUI", uiGameObject);
                }

                if (!Variables.Object(go).IsDefined("VSPresenterBridge") && m_VsBridge != null)
                {
                    Variables.Object(go).Set("VSPresenterBridge", m_VsBridge);
                }
            }
        }
    }
}
