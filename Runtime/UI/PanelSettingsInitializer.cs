using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FUnity.UI
{
    public static class PanelSettingsInitializer
    {
        private const string ResourceName = "FUnityPanelSettings";
        private static readonly string ResourceDirectory = Path.Combine("Assets", "Resources");
        private static readonly string AssetPath = Path.Combine(ResourceDirectory, ResourceName + ".asset");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsurePanelSettings()
        {
            var panelSettings = Resources.Load<PanelSettings>(ResourceName);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
#if UNITY_EDITOR
                if (!Directory.Exists(ResourceDirectory))
                {
                    Directory.CreateDirectory(ResourceDirectory);
                }

                AssetDatabase.CreateAsset(panelSettings, AssetPath);
                AssetDatabase.SaveAssets();
#endif
            }

            var uiDocuments = Object.FindObjectsOfType<UIDocument>();
            if (uiDocuments == null || uiDocuments.Length == 0)
            {
                return;
            }

            foreach (var uiDocument in uiDocuments)
            {
                if (uiDocument != null && uiDocument.panelSettings == null)
                {
                    uiDocument.panelSettings = panelSettings;
                }
            }
        }
    }
}
