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

            // ---- Ensure PanelSettings & Theme (auto-create if not exists) ----
            EnsureFolder("Assets/FUnity");
            EnsureFolder("Assets/FUnity/UI");
            EnsureFolder("Assets/FUnity/UI/USS");

            // 1) Theme(USS) を確保
            var themePath = "Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss";
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(themePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<StyleSheet>();
                AssetDatabase.CreateAsset(theme, themePath);
                EditorUtility.SetDirty(theme);
            }

            // 2) PanelSettings を確保
            var panelPath = "Assets/FUnity/UI/FUnityPanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                // 任意の初期値（必要なら）
                //  panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; // 既定のままでOKならコメントのまま
                AssetDatabase.CreateAsset(panel, panelPath);
            }

            // 3) PanelSettings に Theme を積む（重複チェック）
            if (panel != null && theme != null && panel.themeStyleSheets != null && !panel.themeStyleSheets.Contains(theme))
            {
                panel.themeStyleSheets.Add(theme);
                EditorUtility.SetDirty(panel);
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
    }
}
#endif
