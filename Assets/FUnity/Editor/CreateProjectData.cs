#if UNITY_EDITOR
using System.IO;
using System.Linq;
using FUnity.Runtime.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.EditorTools
{
    public static class CreateProjectData
    {
        [MenuItem("FUnity/Create/Default Project Data")]
        public static void CreateDefault()
        {
            const string dir = "Assets/Resources";
            Directory.CreateDirectory(dir);

            var project = ScriptableObject.CreateInstance<FUnityProjectData>();
            var projectPath = $"{dir}/FUnityProjectData.asset";
            AssetDatabase.CreateAsset(project, projectPath);

            var stage = ScriptableObject.CreateInstance<FUnityStageData>();
            var stagePath = $"{dir}/FUnityStageData.asset";
            AssetDatabase.CreateAsset(stage, stagePath);

            // ---- Ensure UnityDefaultRuntimeTheme.uss (write correct USS text and reimport) ----
            EnsureFolder("Assets/FUnity");
            EnsureFolder("Assets/FUnity/UI");
            EnsureFolder("Assets/FUnity/UI/USS");

            const string CanonicalThemePath = "Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss";
            const string LegacyThemeDir = "Assets/UI Toolkit/UnityThemes";
            const string LegacyThemePath = LegacyThemeDir + "/UnityDefaultRuntimeTheme.uss";

            var themeUssPath = CanonicalThemePath;
            var needWrite = true;

            if (File.Exists(themeUssPath))
            {
                var firstLines = File.ReadLines(themeUssPath).Take(3).ToArray();
                var content = File.ReadAllText(themeUssPath).Trim();
                if (!firstLines.Any(l => l.TrimStart().StartsWith("---")) && !string.IsNullOrEmpty(content))
                {
                    needWrite = false;
                }
            }

            if (needWrite)
            {
                var ussText = @"/* Unity Default Runtime Theme (safe minimal)
   - No YAML front matter, no unsupported at-rules.
   - Avoids shorthand traps; uses explicit px where needed.
   - Keep it minimal; project-specific styles can be layered later.
*/

/* Base label/text */
Label {
    font-size: 14px;
}

/* Buttons baseline */
Button {
    min-width: 80px;
    min-height: 24px;
}

/* Actor template defaults */
.actor {
    flex-shrink: 0;
}

.portrait {
    width: 100%;
    height: 100%;
    /* Keep aspect and fit inside parent */
    -unity-background-scale-mode: scale-to-fit;
}
";

                File.WriteAllText(themeUssPath, ussText, System.Text.Encoding.UTF8);
                AssetDatabase.ImportAsset(themeUssPath);
            }

            // ---- Remove duplicate theme under 'Assets/UI Toolkit/UnityThemes' if exists ----
            var legacyTheme = AssetDatabase.LoadAssetAtPath<StyleSheet>(LegacyThemePath);
            if (legacyTheme != null)
            {
                AssetDatabase.DeleteAsset(LegacyThemePath);
                TryDeleteIfEmpty(LegacyThemeDir);
                TryDeleteIfEmpty("Assets/UI Toolkit");
            }

            // 以降は必ず CanonicalThemePath を基準に Load して使う
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(CanonicalThemePath);
            if (theme == null)
            {
                Debug.LogWarning("[FUnity] UnityDefaultRuntimeTheme.uss could not be loaded after write.");
            }

            // ---- Ensure PanelSettings (auto-create if not exists) ----
            var panelPath = "Assets/FUnity/UI/FUnityPanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                // 任意の初期値（必要なら）
                //  panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; // 既定のままでOKならコメントのまま
                AssetDatabase.CreateAsset(panel, panelPath);
            }

            // ---- Assign Theme to PanelSettings if the API exposes it ----
            if (panel != null && theme != null)
            {
                var soPanel = new SerializedObject(panel);
                SerializedProperty themeProp = null;

                // 候補 1: 配列 themeStyleSheets（新API系）
                themeProp = soPanel.FindProperty("themeStyleSheets");

                // 候補 2: 内部名 m_ThemeStyleSheets（バージョン差異対策）
                if (themeProp == null) themeProp = soPanel.FindProperty("m_ThemeStyleSheets");

                // 候補 3: 単数 themeStyleSheet（旧API系; ObjectRef）
                if (themeProp == null) themeProp = soPanel.FindProperty("themeStyleSheet");

                var assigned = false;
                if (themeProp != null)
                {
                    if (themeProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // 単数フィールドに代入
                        if (themeProp.objectReferenceValue != theme)
                        {
                            themeProp.objectReferenceValue = theme;
                            assigned = true;
                        }
                    }
                    else if (themeProp.isArray)
                    {
                        // 配列に重複なく追加
                        bool exists = false;
                        for (int i = 0; i < themeProp.arraySize; i++)
                        {
                            var elementProp = themeProp.GetArrayElementAtIndex(i);
                            if (elementProp != null && elementProp.objectReferenceValue == theme)
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

                    if (assigned)
                    {
                        soPanel.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(panel);
                        Debug.Log("[FUnity] UnityDefaultRuntimeTheme assigned to PanelSettings.");
                    }
                    else
                    {
                        Debug.Log("[FUnity] PanelSettings theme already set or unchanged.");
                    }
                }
                else
                {
                    Debug.LogWarning("[FUnity] PanelSettings does not expose a theme field. Falling back to UILoadProfile.uss.");
                    TryAddThemeToUILoadProfile(theme); // フォールバック
                }
            }
            else if (theme != null)
            {
                // PanelSettings が取得できなかった場合でもフォールバックしておく
                TryAddThemeToUILoadProfile(theme);
            }

            /* ---- Stage の BackgroundImage を設定（最小差分） ---- */
            var bgTex = LoadFirst<Texture2D>(new[]
            {
                "Assets/FUnity/Art/Backgrounds/Background_01.png",
                "Packages/com.papacoder.funity/Runtime/Art/Backgrounds/Background_01.png",
                "Packages/com.papacoder.funity/Art/Backgrounds/Background_01.png",
            }, "t:Texture2D Background_01");

            var soStage = new SerializedObject(stage);
            var bgProp = soStage.FindProperty("m_backgroundImage"); // FUnityStageData の Serialized 名
            if (bgProp != null)
            {
                bgProp.objectReferenceValue = bgTex;
                soStage.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stage);
            }

            var actor = ConfigureFooniActorData();

            var duplicateActorPath = $"{dir}/FUnityActorData_Fooni.asset";
            if (AssetDatabase.LoadAssetAtPath<FUnityActorData>(duplicateActorPath) != null)
            {
                AssetDatabase.DeleteAsset(duplicateActorPath);
            }

            var so = new SerializedObject(project);
            so.FindProperty("m_stage").objectReferenceValue = stage;

            var actorsProp = so.FindProperty("m_actors");
            actorsProp.arraySize = 1;
            actorsProp.GetArrayElementAtIndex(0).objectReferenceValue = actor;
            so.ApplyModifiedProperties();

            Debug.Log("[FUnity] Default Project Data: FUnityActorData_Fooni configured.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = project;

            Debug.Log("✅ Created & linked: Project/Stage assets under Assets/Resources, actor under Assets/FUnity/Data/Actors");
        }

        private static FUnityActorData ConfigureFooniActorData()
        {
            EnsureFolder("Assets/FUnity");
            EnsureFolder("Assets/FUnity/Data");
            EnsureFolder("Assets/FUnity/Data/Actors");

            const string actorPath = "Assets/FUnity/Data/Actors/FUnityActorData_Fooni.asset";
            var actorObj = AssetDatabase.LoadAssetAtPath<FUnityActorData>(actorPath);
            if (actorObj == null)
            {
                actorObj = ScriptableObject.CreateInstance<FUnityActorData>();
                AssetDatabase.CreateAsset(actorObj, actorPath);
            }

            var so = new SerializedObject(actorObj);
            SetString(so, "m_displayName", "Fooni");

            SetObject(so, "m_portrait", LoadFirst<Texture2D>(new[]
            {
                "Assets/FUnity/Art/Characters/Fooni.png",
                "Packages/com.papacoder.funity/Runtime/Art/Characters/Fooni.png"
            }, "t:Texture2D Fooni"));

            SetObject(so, "m_ElementUxml", LoadFirst<VisualTreeAsset>(new[]
            {
                "Assets/FUnity/UI/UXML/FooniElement.uxml",
                "Packages/com.papacoder.funity/Runtime/UI/UXML/FooniElement.uxml"
            }, "t:VisualTreeAsset FooniElement"));

            SetObject(so, "m_ElementStyle", LoadFirst<StyleSheet>(new[]
            {
                "Assets/FUnity/UI/USS/FooniElement.uss",
                "Packages/com.papacoder.funity/Runtime/UI/USS/FooniElement.uss"
            }, "t:StyleSheet FooniElement"));

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(actorObj);

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

        private static void SetString(SerializedObject so, string property, string value)
        {
            var prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.stringValue = value;
            }
        }

        private static void SetObject(SerializedObject so, string property, Object obj)
        {
            var prop = so.FindProperty(property);
            if (prop != null)
            {
                prop.objectReferenceValue = obj;
            }
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

        private static void TryAddThemeToUILoadProfile(StyleSheet theme)
        {
            if (theme == null) return;

            // 既定プロファイル候補
            var profilePath = "Assets/FUnity/Configs/UIProfiles/UIProfile_Default.asset";
            var profile = AssetDatabase.LoadAssetAtPath<FUnity.Runtime.UI.UILoadProfile>(profilePath);
            if (profile == null)
            {
                // なければ検索
                var guid = AssetDatabase.FindAssets("t:FUnity.Runtime.UI.UILoadProfile").FirstOrDefault();
                if (!string.IsNullOrEmpty(guid))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    profile = AssetDatabase.LoadAssetAtPath<FUnity.Runtime.UI.UILoadProfile>(path);
                }
            }

            if (profile == null)
            {
                Debug.LogWarning("[FUnity] UILoadProfile not found. Theme will not be auto-applied.");
                return;
            }

            if (profile.uss == null)
            {
                Debug.LogWarning("[FUnity] UILoadProfile.uss list is null. Theme will not be auto-applied.");
                return;
            }

            if (!profile.uss.Contains(theme))
            {
                profile.uss.Add(theme);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                Debug.Log("[FUnity] UnityDefaultRuntimeTheme added to UILoadProfile.uss (fallback).");
            }
        }

        private static void TryDeleteIfEmpty(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath)) return;

            var guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            if (guids == null || guids.Length == 0)
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }
    }
}
#endif
