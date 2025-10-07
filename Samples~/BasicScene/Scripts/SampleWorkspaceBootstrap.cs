using FUnity.Core;
using FUnity.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Samples
{
    /// <summary>
    /// Bootstraps the sample scene by wiring the workspace, layout, and styles together.
    /// </summary>
    [RequireComponent(typeof(WorkspaceView))]
    [RequireComponent(typeof(UIDocument))]
    public class SampleWorkspaceBootstrap : MonoBehaviour
    {
        [SerializeField]
        private FUnityWorkspace m_Workspace;

        [SerializeField]
        private VisualTreeAsset m_WorkspaceLayout;

        [SerializeField]
        private StyleSheet m_WorkspaceStyles;

        private void Awake()
        {
            if (m_WorkspaceLayout == null)
            {
                m_WorkspaceLayout = Resources.Load<VisualTreeAsset>("UXML/WorkspaceLayout");
            }

            if (m_WorkspaceStyles == null)
            {
                m_WorkspaceStyles = Resources.Load<StyleSheet>("USS/WorkspaceStyles");
            }

            WorkspaceView view = GetComponent<WorkspaceView>();
            view.SetWorkspace(GetOrCreateWorkspace());
            view.SetLayout(m_WorkspaceLayout);
            view.SetStyles(m_WorkspaceStyles);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateSampleObjects()
        {
            if (!IsSampleSceneActive() || Object.FindObjectOfType<WorkspaceView>() != null)
            {
                return;
            }

            GameObject go = new("FUnity Sample Workspace");
            UIDocument document = go.AddComponent<UIDocument>();
            document.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

            go.AddComponent<WorkspaceView>();
            go.AddComponent<SampleWorkspaceBootstrap>();
        }

        private static bool IsSampleSceneActive()
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            return activeScene.IsValid() && activeScene.name == "BasicScratchScene";
        }

        private FUnityWorkspace GetOrCreateWorkspace()
        {
            if (m_Workspace != null)
            {
                return m_Workspace;
            }

            FUnityWorkspace workspace = ScriptableObject.CreateInstance<FUnityWorkspace>();

            BlockDefinition move = ScriptableObject.CreateInstance<BlockDefinition>();
            move.Configure("Move 10 Steps", "Moves the actor forward by ten units.", "Motion", new Color(0.27f, 0.62f, 0.95f));

            BlockDefinition turn = ScriptableObject.CreateInstance<BlockDefinition>();
            turn.Configure("Turn Right", "Rotates the actor clockwise by fifteen degrees.", "Motion", new Color(0.27f, 0.62f, 0.95f));

            BlockCollection motion = ScriptableObject.CreateInstance<BlockCollection>();
            motion.Configure("Motion", new[] { move, turn });

            BlockDefinition say = ScriptableObject.CreateInstance<BlockDefinition>();
            say.Configure("Say Hello", "Displays a dialogue bubble.", "Looks", new Color(0.79f, 0.48f, 0.98f));

            BlockCollection looks = ScriptableObject.CreateInstance<BlockCollection>();
            looks.Configure("Looks", new[] { say });

            workspace.Configure("FUnity Sample", new[] { motion, looks });
            m_Workspace = workspace;
            return m_Workspace;
        }
    }
}
