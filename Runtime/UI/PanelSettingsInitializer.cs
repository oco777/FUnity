using System.IO;
using System.Linq;
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
            const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss";
            const string CanonicalThemeDir = "Assets/FUnity/UI/USS";
            const string CanonicalThemePath = CanonicalThemeDir + "/UnityDefaultRuntimeTheme.uss";

            if (!Directory.Exists(CanonicalThemeDir))
            {
                Directory.CreateDirectory(CanonicalThemeDir);
            }

            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(LegacyThemePath);
            if (theme == null)
            {
                bool needWrite = !File.Exists(CanonicalThemePath);
                if (!needWrite)
                {
                    var firstLines = File.ReadLines(CanonicalThemePath).Take(3).ToArray();
                    var content = File.ReadAllText(CanonicalThemePath).Trim();
                    if (firstLines.Any(l => l.TrimStart().StartsWith("---")) || string.IsNullOrEmpty(content))
                    {
                        needWrite = true;
                    }
                }

                if (needWrite)
                {
                    File.WriteAllText(CanonicalThemePath,
@"/* Unity Default Runtime Theme (safe minimal) */
Label { font-size: 14px; }
Button { min-width: 80px; min-height: 24px; }
.actor { flex-shrink: 0; }
.portrait { width: 100%; height: 100%; -unity-background-scale-mode: scale-to-fit; }
", System.Text.Encoding.UTF8);
                    AssetDatabase.ImportAsset(CanonicalThemePath);
                }

                theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(CanonicalThemePath);
            }

            if (theme != null && panelSettings != null)
            {
                var soPanel = new SerializedObject(panelSettings);
                SerializedProperty themeProp =
                      soPanel.FindProperty("themeStyleSheets")
                   ?? soPanel.FindProperty("m_ThemeStyleSheets")
                   ?? soPanel.FindProperty("themeStyleSheet");

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
                            var element = themeProp.GetArrayElementAtIndex(i);
                            if (element != null && element.objectReferenceValue == theme)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            int index = themeProp.arraySize;
                            themeProp.InsertArrayElementAtIndex(index);
                            var newElement = themeProp.GetArrayElementAtIndex(index);
                            if (newElement != null)
                            {
                                newElement.objectReferenceValue = theme;
                            }
                            assigned = true;
                        }
                    }

                    if (assigned)
                    {
                        soPanel.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(panelSettings);
                        AssetDatabase.SaveAssets();
                    }
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
