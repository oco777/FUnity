using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.UI;
using FUnity.Runtime.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_VISUAL_SCRIPTING
using Unity.VisualScripting;
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

        private void Awake()
        {
            if (m_Project == null)
            {
                m_Project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (m_Project == null || m_Project.ensureFUnityUI)
            {
                EnsureFUnityUI();
            }

#if UNITY_VISUAL_SCRIPTING
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
#endif
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
                    }
                    else
                    {
                        Debug.LogWarning($"[FUnity] Failed to create actor element for '{actor.DisplayName}'.");
                    }
                }
            }

            Debug.Log($"[FUnity] Project-driven load completed. Actors={(m_Project.Actors?.Count ?? 0)}");
        }

        private void EnsureFUnityUI()
        {
            m_FUnityUI = GameObject.Find("FUnity UI");
            if (m_FUnityUI == null)
            {
                m_FUnityUI = new GameObject("FUnity UI");
            }

            m_UIDocument = m_FUnityUI.GetComponent<UIDocument>() ?? m_FUnityUI.AddComponent<UIDocument>();

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
        }

        private VisualElement CreateActorElement(FUnityActorData data)
        {
            if (data == null)
            {
                return null;
            }

            var element = new FooniElement
            {
                name = string.IsNullOrEmpty(data.DisplayName) ? "FUnityActor" : data.DisplayName
            };

            if (data.Portrait != null)
            {
                element.style.backgroundImage = new StyleBackground(data.Portrait);
            }

            element.style.width = 128;
            element.style.height = 128;
            element.style.translate = new Translate(data.InitialPosition.x, data.InitialPosition.y, 0f);

            return element;
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

#if UNITY_VISUAL_SCRIPTING
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
        }
#endif
    }
}
