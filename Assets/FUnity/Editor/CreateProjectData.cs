#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using FUnity.Runtime.Core;

namespace FUnity.EditorTools
{
    public static class CreateProjectData
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string FUnityFolderPath = "Assets/FUnity";
        private const string FUnityUiFolderPath = FUnityFolderPath + "/UI";
        private const string FUnityUiUssFolderPath = FUnityUiFolderPath + "/USS";
        private const string ProjectAssetPath = ResourcesFolderPath + "/FUnityProjectData.asset";
        private const string StageAssetPath = ResourcesFolderPath + "/FUnityStageData.asset";

        // Prefer the legacy UI Builder theme first; fall back to the canonical FUnity-managed copy if missing.
        private const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss";
        private const string CanonicalThemePath = FUnityUiUssFolderPath + "/UnityDefaultRuntimeTheme.uss";

        private const string PanelSettingsAssetPath = FUnityUiFolderPath + "/FUnityPanelSettings.asset";
        private const string DuplicateActorResourcePath = ResourcesFolderPath + "/FUnityActorData_Fooni.asset";
        private const string ActorRootFolderPath = "Assets/FUnity/Data";
        private const string ActorDataFolderPath = ActorRootFolderPath + "/Actors";
        private const string ActorAssetPath = ActorDataFolderPath + "/FUnityActorData_Fooni.asset";

        private static readonly string[] StageBackgroundCandidates =
        {
            "Assets/FUnity/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Runtime/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Art/Backgrounds/Background_01.png"
        };

        private static readonly string[] FooniPortraitCandidates =
        {
            "Assets/FUnity/Art/Characters/Fooni.png",
            "Packages/com.papacoder.funity/Runtime/Art/Characters/Fooni.png"
        };

        private static readonly string[] FooniElementUxmlCandidates =
        {
            "Assets/FUnity/UI/UXML/FooniElement.uxml",
            "Packages/com.papacoder.funity/Runtime/UI/UXML/FooniElement.uxml"
        };

        private static readonly string[] FooniElementStyleCandidates =
        {
            "Assets/FUnity/UI/USS/FooniElement.uss",
            "Packages/com.papacoder.funity/Runtime/UI/USS/FooniElement.uss"
        };

        private const string BackgroundSearchFilter = "t:Texture2D Background_01";
        private const string FooniPortraitSearchFilter = "t:Texture2D Fooni";
        private const string FooniElementUxmlSearchFilter = "t:VisualTreeAsset FooniElement";
        private const string FooniElementStyleSearchFilter = "t:StyleSheet FooniElement";

        [MenuItem("FUnity/Create/Default Project Data")]
        public static void CreateDefault()
        {
            Directory.CreateDirectory(ResourcesFolderPath);

            var project = ScriptableObject.CreateInstance<FUnityProjectData>();
            AssetDatabase.CreateAsset(project, ProjectAssetPath);

            var stage = ScriptableObject.CreateInstance<FUnityStageData>();
            AssetDatabase.CreateAsset(stage, StageAssetPath);

            EnsureFolder(FUnityFolderPath);
            EnsureFolder(FUnityUiFolderPath);
            EnsureFolder(FUnityUiUssFolderPath);

            var theme = EnsureThemeStyleSheet();
            var panelSettings = EnsurePanelSettingsAsset();
            AssignThemeToPanelSettings(panelSettings, theme);

            AssignStageBackground(stage);

            var actor = ConfigureFooniActorData();
            RemoveDuplicateActorResource();
            LinkProjectData(project, stage, actor);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = project;

            Debug.Log("[FUnity] Created default project data assets and linked the Fooni actor.");
        }

        private static FUnityActorData ConfigureFooniActorData()
        {
            EnsureFolder(FUnityFolderPath);
            EnsureFolder(ActorRootFolderPath);
            EnsureFolder(ActorDataFolderPath);

            var actorObj = AssetDatabase.LoadAssetAtPath<FUnityActorData>(ActorAssetPath);
            if (actorObj == null)
            {
                actorObj = ScriptableObject.CreateInstance<FUnityActorData>();
                AssetDatabase.CreateAsset(actorObj, ActorAssetPath);
            }

            var serializedActor = new SerializedObject(actorObj);
            var changed = false;

            changed |= SetString(serializedActor, "m_displayName", "Fooni");
            changed |= SetObject(serializedActor, "m_portrait", LoadFirst<Texture2D>(FooniPortraitCandidates, FooniPortraitSearchFilter));
            changed |= SetObject(serializedActor, "m_ElementUxml", LoadFirst<VisualTreeAsset>(FooniElementUxmlCandidates, FooniElementUxmlSearchFilter));
            changed |= SetObject(serializedActor, "m_ElementStyle", LoadFirst<StyleSheet>(FooniElementStyleCandidates, FooniElementStyleSearchFilter));

            if (changed)
            {
                serializedActor.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(actorObj);
                AssetDatabase.SaveAssets();
            }

            return actorObj;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static bool SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || property.stringValue == value)
            {
                return false;
            }

            property.stringValue = value;
            return true;
        }

        private static bool SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private static T LoadFirst<T>(string[] candidatePaths, string searchFilter) where T : Object
        {
            foreach (var candidate in candidatePaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(candidate);
                if (asset != null)
                {
                    return asset;
                }
            }

            if (!string.IsNullOrEmpty(searchFilter))
            {
                var guids = AssetDatabase.FindAssets(searchFilter);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
            }

            Debug.LogWarning($"[FUnity] Asset not found: {typeof(T).Name} ({searchFilter})");
            return null;
        }

        private static StyleSheet EnsureThemeStyleSheet()
        {
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(LegacyThemePath);
            if (theme != null)
            {
                return theme;
            }

            if (!File.Exists(CanonicalThemePath) || NeedsCanonicalThemeRefresh())
            {
                WriteCanonicalTheme();
            }

            theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(CanonicalThemePath);
            if (theme == null)
            {
                Debug.LogWarning("[FUnity] UnityDefaultRuntimeTheme.uss could not be loaded.");
            }

            return theme;
        }

        private static bool NeedsCanonicalThemeRefresh()
        {
            var firstLines = File.ReadLines(CanonicalThemePath).Take(3).ToArray();
            var content = File.ReadAllText(CanonicalThemePath).Trim();
            return firstLines.Any(l => l.TrimStart().StartsWith("---")) || string.IsNullOrEmpty(content);
        }

        private static void WriteCanonicalTheme()
        {
            const string ussText = "/* Unity Default Runtime Theme (safe minimal)\n"
                + "   - No YAML front matter, no unsupported at-rules.\n"
                + "   - Avoids shorthand traps; uses explicit px where needed.\n"
                + "   - Keep it minimal; project-specific styles can be layered later.\n"
                + "*/\n\n"
                + "/* Base label/text */\n"
                + "Label {\n"
                + "    font-size: 14px;\n"
                + "}\n\n"
                + "/* Buttons baseline */\n"
                + "Button {\n"
                + "    min-width: 80px;\n"
                + "    min-height: 24px;\n"
                + "}\n\n"
                + "/* Actor template defaults */\n"
                + ".actor {\n"
                + "    flex-shrink: 0;\n"
                + "}\n\n"
                + ".portrait {\n"
                + "    width: 100%;\n"
                + "    height: 100%;\n"
                + "    /* Keep aspect and fit inside parent */\n"
                + "    -unity-background-scale-mode: scale-to-fit;\n"
                + "}\n";

            File.WriteAllText(CanonicalThemePath, ussText, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(CanonicalThemePath);
        }

        private static PanelSettings EnsurePanelSettingsAsset()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAssetPath);
            if (panelSettings != null)
            {
                return panelSettings;
            }

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsAssetPath);
            return panelSettings;
        }

        private static void AssignThemeToPanelSettings(PanelSettings panelSettings, StyleSheet theme)
        {
            if (panelSettings == null || theme == null)
            {
                return;
            }

            var serializedPanel = new SerializedObject(panelSettings);
            var themeProperty = FindThemeProperty(serializedPanel);
            if (themeProperty == null)
            {
                Debug.LogWarning("[FUnity] PanelSettings does not expose a theme field; skipping assignment.");
                return;
            }

            var changed = false;
            if (themeProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (themeProperty.objectReferenceValue != theme)
                {
                    themeProperty.objectReferenceValue = theme;
                    changed = true;
                }
            }
            else if (themeProperty.isArray)
            {
                var exists = false;
                for (var i = 0; i < themeProperty.arraySize; i++)
                {
                    var element = themeProperty.GetArrayElementAtIndex(i);
                    if (element != null && element.objectReferenceValue == theme)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var insertIndex = themeProperty.arraySize;
                    themeProperty.InsertArrayElementAtIndex(insertIndex);
                    var element = themeProperty.GetArrayElementAtIndex(insertIndex);
                    if (element != null)
                    {
                        element.objectReferenceValue = theme;
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panelSettings);
            AssetDatabase.SaveAssets();
        }

        private static SerializedProperty FindThemeProperty(SerializedObject serializedPanel)
        {
            var property = serializedPanel.FindProperty("themeStyleSheets");
            if (property == null)
            {
                property = serializedPanel.FindProperty("m_ThemeStyleSheets");
            }

            if (property == null)
            {
                property = serializedPanel.FindProperty("themeStyleSheet");
            }

            return property;
        }

        private static void AssignStageBackground(FUnityStageData stage)
        {
            if (stage == null)
            {
                return;
            }

            var texture = LoadFirst<Texture2D>(StageBackgroundCandidates, BackgroundSearchFilter);
            if (texture == null)
            {
                return;
            }

            var serializedStage = new SerializedObject(stage);
            var backgroundProperty = serializedStage.FindProperty("m_backgroundImage");
            if (backgroundProperty == null || backgroundProperty.objectReferenceValue == texture)
            {
                return;
            }

            backgroundProperty.objectReferenceValue = texture;
            serializedStage.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
        }

        private static void RemoveDuplicateActorResource()
        {
            if (AssetDatabase.LoadAssetAtPath<FUnityActorData>(DuplicateActorResourcePath) != null)
            {
                AssetDatabase.DeleteAsset(DuplicateActorResourcePath);
            }
        }

        private static void LinkProjectData(FUnityProjectData project, FUnityStageData stage, FUnityActorData actor)
        {
            if (project == null)
            {
                return;
            }

            var serializedProject = new SerializedObject(project);
            var changed = SetObject(serializedProject, "m_stage", stage);

            var actorsProperty = serializedProject.FindProperty("m_actors");
            if (actorsProperty != null)
            {
                if (!actorsProperty.isArray)
                {
                    Debug.LogWarning("[FUnity] Project data actors field is not an array; cannot link Fooni actor.");
                }
                else
                {
                    if (actorsProperty.arraySize != 1)
                    {
                        actorsProperty.arraySize = 1;
                        changed = true;
                    }

                    var element = actorsProperty.GetArrayElementAtIndex(0);
                    if (element != null && element.objectReferenceValue != actor)
                    {
                        element.objectReferenceValue = actor;
                        changed = true;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            serializedProject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(project);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
