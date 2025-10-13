#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using FUnity.Runtime.Core;

namespace FUnity.EditorTools
{
    /// <summary>
    /// FUnity の初期データ一式を Editor メニューから生成し、既定アセットを相互リンクさせる。
    /// Editor 実行前提のため UnityEditor API への依存を許容する。
    /// </summary>
    public static class CreateProjectData
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string FUnityFolderPath = "Assets/FUnity";
        private const string FUnityUiFolderPath = FUnityFolderPath + "/UI";
        private const string FUnityUiUssFolderPath = FUnityUiFolderPath + "/USS";
        private const string ProjectAssetPath = ResourcesFolderPath + "/FUnityProjectData.asset";
        private const string StageAssetPath = ResourcesFolderPath + "/FUnityStageData.asset";

        // UI Builder 旧配置を最優先し、存在しなければ FUnity 管理下の正規コピーへフォールバックする。
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

        /// <summary>
        /// Resources 以下にプロジェクト用 ScriptableObject と関連 Actor/Stage を生成し既定値を流し込む。
        /// テーマや PanelSettings を AssetDatabase で操作するため実行後に SaveAssets/Refresh を行う。
        /// </summary>
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

        /// <summary>
        /// Fooni 用 ActorData を確保し、既存リソースの候補パスから見つかったアセットへ差し替える。
        /// 旧 Resources 版が残っている場合は後続で削除するため、ここでは生成のみを担う。
        /// </summary>
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

        /// <summary>
        /// 指定パス配下のフォルダを親から順番に生成し、AssetDatabase が参照可能な構造に整える。
        /// 無効なルートを避けるため、Split 結果が空なら即時終了する。
        /// </summary>
        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            if (parts.Length == 0)
            {
                // ルートが空の場合は AssetDatabase.CreateFolder に渡せずエラーになるため抜ける。
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

        /// <summary>
        /// 指定プロパティが見つかり、既存値と異なる場合だけ文字列を更新するユーティリティ。
        /// SerializedObject.ApplyModifiedPropertiesWithoutUndo は呼び出し元に委ねる。
        /// </summary>
        private static bool SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || property.stringValue == value)
            {
                // プロパティが無い／既に一致している場合は変更フラグを立てずに戻る。
                return false;
            }

            property.stringValue = value;
            return true;
        }

        /// <summary>
        /// ObjectReference 型プロパティに対して、値が変わる時のみ参照を差し替える補助メソッド。
        /// SerializedPropertyType の検証は事前に済んでいる前提で軽量化している。
        /// </summary>
        private static bool SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
            {
                // プロパティ未発見や同一参照なら再インポートを避けるため早期 return。
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        /// <summary>
        /// 候補パス群を優先順位順に試し、見つからなければ GUID 検索で該当アセットを拾う。
        /// 編集環境ごとの差異を吸収するため、最終手段として AssetDatabase.FindAssets を用いる。
        /// </summary>
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

        /// <summary>
        /// 既存テーマが UI Builder 旧フォルダに残っていればそれを使用し、無ければ正規 USS を再生成する。
        /// Canonical を書き出した場合は Import まで行い、以降の AssetDatabase.Load を成功させる。
        /// </summary>
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

        /// <summary>
        /// Canonical な USS が YAML 由来のゴミデータや空ファイルになっていないか軽量判定する。
        /// YAML の区切り線や空文字のみの場合は再生成を促す。
        /// </summary>
        private static bool NeedsCanonicalThemeRefresh()
        {
            var firstLines = File.ReadLines(CanonicalThemePath).Take(3).ToArray();
            var content = File.ReadAllText(CanonicalThemePath).Trim();
            return firstLines.Any(l => l.TrimStart().StartsWith("---")) || string.IsNullOrEmpty(content);
        }

        /// <summary>
        /// FUnity 用の安全な最小テーマを UTF-8 で上書きし、ImportAsset で再インポートを明示する。
        /// UI Builder 標準に依存せず、基礎スタイルのみ持つ USS を配布する。
        /// </summary>
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

        /// <summary>
        /// FUnity UI 専用の PanelSettings アセットを確保し、未存在なら ScriptableObject から生成する。
        /// 再生成時にも同じパスを使うため、AssetDatabase.Load と Create をセットで扱う。
        /// </summary>
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

        /// <summary>
        /// PanelSettings の theme フィールドに指定テーマを割り当て、存在チェック済みなら重複追加を避ける。
        /// theme が null の場合は ScriptableObject を汚さないよう即時終了する。
        /// </summary>
        private static void AssignThemeToPanelSettings(PanelSettings panelSettings, StyleSheet theme)
        {
            if (panelSettings == null || theme == null)
            {
                // PanelSettings や Theme が無いと SerializedObject 生成が無駄になるため処理を打ち切る。
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
                // 値が変わっていなければ Apply/Save を避けて AssetDatabase の再インポートを抑制する。
                return;
            }

            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panelSettings);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Unity バージョン差異に対応するため、themeStyleSheets → m_ThemeStyleSheets → themeStyleSheet の順で探す。
        /// UI Builder API の内部名が変わっても対応できるようフォールバックを多段化している。
        /// </summary>
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

        /// <summary>
        /// StageData に背景テクスチャを結び付ける。候補が無ければ既存設定を維持する。
        /// Resources 直下の背景を想定するため、null なら SerializedObject 生成を避ける。
        /// </summary>
        private static void AssignStageBackground(FUnityStageData stage)
        {
            if (stage == null)
            {
                // Stage が null の場合は背景書き込みが失敗するため何もせず戻る。
                return;
            }

            var texture = LoadFirst<Texture2D>(StageBackgroundCandidates, BackgroundSearchFilter);
            if (texture == null)
            {
                // 既定背景が存在しない場合は警告を出さず静かにスキップし、カスタム背景を尊重する。
                return;
            }

            var serializedStage = new SerializedObject(stage);
            var backgroundProperty = serializedStage.FindProperty("m_backgroundImage");
            if (backgroundProperty == null || backgroundProperty.objectReferenceValue == texture)
            {
                // フィールドが見つからない／同一値の場合は無駄な SaveAssets を避ける。
                return;
            }

            backgroundProperty.objectReferenceValue = texture;
            serializedStage.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Resources 内に残った旧 Fooni ActorData を削除し、重複ロードを防ぐ。
        /// FUnityActorData_Fooni.asset が存在する場合のみ AssetDatabase.DeleteAsset を実行する。
        /// </summary>
        private static void RemoveDuplicateActorResource()
        {
            if (AssetDatabase.LoadAssetAtPath<FUnityActorData>(DuplicateActorResourcePath) != null)
            {
                AssetDatabase.DeleteAsset(DuplicateActorResourcePath);
            }
        }

        /// <summary>
        /// プロジェクトデータに Stage と Actor を割り当て、配列サイズを 1 件に保つ。
        /// 未対応フィールド構造のケースでは警告を出しつつ既定リンクを維持する。
        /// </summary>
        private static void LinkProjectData(FUnityProjectData project, FUnityStageData stage, FUnityActorData actor)
        {
            if (project == null)
            {
                // Project が null なら SerializedObject 化できないため早期 return。
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
