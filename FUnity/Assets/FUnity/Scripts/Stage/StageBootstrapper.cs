using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Stage
{
    /// <summary>
    /// Ensures that a UI Toolkit based stage is available whenever a scene is loaded.
    /// The bootstrapper is executed automatically (both in Play Mode and builds) and
    /// spawns a dedicated GameObject with an UIDocument + StageRuntime pair.
    /// </summary>
    public static class StageBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        /// <summary>
        /// Guarantees that the stage GameObject exists after a scene has finished loading.
        /// </summary>
        private static void EnsureStage()
        {
            if (Object.FindObjectOfType<StageRuntime>() != null)
            {
                return;
            }

            var root = new GameObject("FUnity Stage");
            Object.DontDestroyOnLoad(root);

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "FUnity Stage Panel Settings (Runtime)";
            panelSettings.clearColor = true;
            panelSettings.colorClearValue = Color.clear;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;

            var document = root.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            root.AddComponent<StageRuntime>();
        }
    }
}
