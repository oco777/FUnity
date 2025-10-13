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

#if UNITY_EDITOR
            // ---- Assign UI Builder default theme to Resources/FUnityPanelSettings ----
            const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss";
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(LegacyThemePath);
            if (theme != null && panelSettings != null)
            {
                var so = new SerializedObject(panelSettings);
                var themeProp =
                      so.FindProperty("themeStyleSheets")
                   ?? so.FindProperty("m_ThemeStyleSheets")
                   ?? so.FindProperty("themeStyleSheet");

                bool assigned = false;
                if (themeProp != null)
                {
                    if (themeProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (themeProp.objectReferenceValue != theme)
                        {
                            themeProp.objectReferenceValue = theme;
                            assigned = true;
                        }
                    }
                    else if (themeProp.isArray)
                    {
                        bool exists = false;
                        for (int i = 0; i < themeProp.arraySize; i++)
                        {
                            var e = themeProp.GetArrayElementAtIndex(i);
                            if (e != null && e.objectReferenceValue == theme)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            int idx = themeProp.arraySize;
                            themeProp.InsertArrayElementAtIndex(idx);
                            var newElement = themeProp.GetArrayElementAtIndex(idx);
                            if (newElement != null)
                            {
                                newElement.objectReferenceValue = theme;
                            }
                            assigned = true;
                        }
                    }
                }

                if (assigned)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(panelSettings);
                    AssetDatabase.SaveAssets();
                }
            }
#endif

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
