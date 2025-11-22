#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Unity.VisualScripting;
using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;
using FUnity.UI;

namespace FUnity.EditorTools
{
    // プロジェクト/Stage/Actor/UI を一括生成する初期化コマンド。新規導入直後やサンプル起動前に実行する。UNITY_EDITOR 前提。
    // CreateDefaultUIProfile で整えるロード設定と組み合わせ、FUnity 学習用シーンの最低限の土台を揃える。
    public static class CreateProjectData
    {
        private const string ResourcesFolderPath = "Assets/Resources";
        private const string FUnityFolderPath = "Assets/FUnity";
        private const string FUnityUiFolderPath = FUnityFolderPath + "/UI";
        private const string FUnityUiUssFolderPath = FUnityUiFolderPath + "/USS";
        private const string ProjectAssetPath = ResourcesFolderPath + "/FUnityProjectData.asset";
        private const string StageAssetPath = ResourcesFolderPath + "/FUnityStageData.asset";
        private const string PanelThemeSettingsAssetPath = ResourcesFolderPath + "/FUnityPanelThemeSettings.asset";

        // Theme は UI Builder 標準配置（Legacy）を最優先し、無ければ FUnity 配下の正規 USS を生成して使用する。
        private const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss";
        private const string CanonicalThemePath = FUnityUiUssFolderPath + "/UnityDefaultRuntimeTheme.uss";

        private const string PanelSettingsAssetPath = FUnityUiFolderPath + "/FUnityPanelSettings.asset";
        private const string DuplicateActorResourcePath = ResourcesFolderPath + "/FUnityActorData_Fooni.asset";
        private const string ActorRootFolderPath = "Assets/FUnity/Data";
        private const string ActorDataFolderPath = ActorRootFolderPath + "/Actors";
        private const string ActorAssetPath = ActorDataFolderPath + "/FUnityActorData_Fooni.asset";
        private const string FooniScriptGraphSearchFilter = "t:ScriptGraphAsset Fooni_FloatSetup";
        private const string GeneratedRootFolderPath = FUnityFolderPath + "/Generated";
        private const string GeneratedTextureFolderPath = GeneratedRootFolderPath + "/Textures";
        private const string GeneratedGraphsFolderPath = GeneratedRootFolderPath + "/Graphs";
        private const string GeneratedFooniTextureAssetPath = GeneratedTextureFolderPath + "/Fooni.png";
        private const string GeneratedFooniGraphAssetPath = GeneratedGraphsFolderPath + "/Fooni_FloatSetup.asset";
        private const string PackageRootPath = "Packages/com.papacoder.funity";
        private const string ScratchModeConfigFileName = "FUnityModeConfig_Scratch.asset";
        private const string UnityroomModeConfigFileName = "FUnityModeConfig_Unityroom.asset";

        /// <summary>Unity バージョンごとに異なる PanelSettings のテーマプロパティ候補名。</summary>
        private static readonly string[] PanelThemePropertyCandidates =
        {
            "themeStyleSheets",
            "m_ThemeStyleSheets",
            "themeStyleSheet",
            "m_ThemeStyleSheet",
            "themeUss",
            "m_ThemeUss",
            "theme"
        };

        // 背景テクスチャはプロジェクト直下の正規パスを優先し、無ければパッケージ同梱版から順に探す。
        private static readonly string[] StageBackgroundCandidates =
        {
            "Assets/FUnity/Runtime/Resources/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Runtime/Resources/Backgrounds/Background_01.png",
            "Assets/FUnity/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Runtime/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Art/Backgrounds/Background_01.png"
        };

        // Fooni 用ポートレート/UXML/USS も正規パス→パッケージの順で探し、テンプレート想定の root/portrait 要素を持つファイルを期待する。
        private static readonly string[] FooniPortraitCandidates =
        {
            "Assets/FUnity/Art/Characters/Fooni.png",
            "Assets/Art/Characters/Fooni.png",
            "Packages/com.papacoder.funity/Art/Characters/Fooni.png",
            GeneratedFooniTextureAssetPath
        };

        private static readonly string[] FooniElementUxmlCandidates =
        {
            "Assets/FUnity/UI/UXML/FooniElement.uxml",
            "Assets/FUnity/UI/UXML/ActorElement.uxml",
            "Packages/com.papacoder.funity/Runtime/UI/UXML/FooniElement.uxml",
            "Packages/com.papacoder.funity/Runtime/Resources/UI/FooniElement.uxml",
            "Packages/com.papacoder.funity/Runtime/Resources/UI/ActorElement.uxml"
        };

        private static readonly string[] FooniElementStyleCandidates =
        {
            "Assets/FUnity/UI/USS/FooniElement.uss",
            "Assets/FUnity/UI/USS/ActorElement.uss",
            "Packages/com.papacoder.funity/Runtime/UI/USS/FooniElement.uss",
            "Packages/com.papacoder.funity/Runtime/Resources/UI/FooniElement.uss",
            "Packages/com.papacoder.funity/Runtime/Resources/UI/ActorElement.uss"
        };

        private static readonly string[] FooniScriptGraphCandidates =
        {
            "Assets/FUnity/VisualScripting/Macros/Fooni_FloatSetup.asset",
            GeneratedFooniGraphAssetPath,
            "Packages/com.papacoder.funity/VisualScripting/Macros/Fooni_FloatSetup.asset",
            "Packages/com.papacoder.funity/Runtime/VisualScripting/Macros/Fooni_FloatSetup.asset"
        };
        private const string BackgroundSearchFilter = "t:Texture2D Background_01";
        private const string FooniPortraitSearchFilter = "t:Texture2D Fooni";
        private const string FooniElementUxmlSearchFilter = "t:VisualTreeAsset FooniElement";
        private const string FooniElementStyleSearchFilter = "t:StyleSheet FooniElement";

        // FUnity → Create → FUnityProjectData。ProjectData/StageData/ActorData/PanelSettings を生成し相互リンクする。
        [MenuItem("FUnity/Create/FUnityProjectData")]
        /// <summary>
        /// - Resources/Assets/FUnity 配下を作成し、Project/Stage/Actor の ScriptableObject を用意する。
        /// - UI Theme は正規/旧配置から探索し、PanelSettings を確保して SerializedObject 経由で割り当てる。
        /// - Stage 背景に Runtime/Resources/Backgrounds/Background_01.* を設定し、ActorData_Fooni に Portrait/UXML/USS を適用する。
        /// - 不足アセットは警告にとどめて続行し、最後に SaveAssets/Refresh で AssetDatabase の状態を確定させる。
        /// </summary>
        public static void CreateDefault()
        {
            Directory.CreateDirectory(ResourcesFolderPath);

            var project = ScriptableObject.CreateInstance<FUnityProjectData>();
            AssetDatabase.CreateAsset(project, ProjectAssetPath);

            var stage = ScriptableObject.CreateInstance<FUnityStageData>();
            AssetDatabase.CreateAsset(stage, StageAssetPath);

            ApplyCommonProjectDefaults(project, stage);

            var actor = ConfigureFooniActorData();
            RemoveDuplicateActorResource();
            LinkProjectData(project, stage, actor);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = project;

            Debug.Log("[FUnity] Created default project data assets and linked the Fooni actor.");
        }

        /// <summary>
        /// 新規作成直後の Project/Stage に対し、Actor 以外の共通初期設定をまとめて適用する。
        /// テーマの確保や PanelSettings への割り当て、背景設定、ModeConfig のデフォルト化を担当する。
        /// </summary>
        /// <param name="project">ModeConfig を設定する対象の ProjectData。</param>
        /// <param name="stage">背景を割り当てる対象の StageData。</param>
        internal static void ApplyCommonProjectDefaults(FUnityProjectData project, FUnityStageData stage)
        {
            if (project == null || stage == null)
            {
                return;
            }

            EnsureFolder(FUnityFolderPath);
            EnsureFolder(FUnityUiFolderPath);
            EnsureFolder(FUnityUiUssFolderPath);

            var theme = EnsureThemeStyleSheet();
            var panelSettings = EnsurePanelSettingsAsset();
            AssignThemeToPanelSettings(panelSettings, theme);

            AssignStageBackground(stage);

            AssignDefaultModeConfigs(project);
        }

        /// <summary>
        /// Fooni ActorData を確保し、候補パスと GUID 検索で集めた Portrait/UXML/USS を割り当てる。テンプレート構造は root/portrait 要素を前提。
        /// 旧 Resources 版が残る場合でも生成を優先し、削除は RemoveDuplicateActorResource に委ねる。
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

            // SerializedObject 経由で m_displayName など内部プロパティにアクセスし、Unity のバージョン差異を吸収する。
            var serializedActor = new SerializedObject(actorObj);
            var changed = false;

            changed |= SetString(serializedActor, "m_displayName", "Fooni");
            var portraitTexture = EnsureFooniPortraitTexture();
            changed |= SetObject(serializedActor, "m_portrait", portraitTexture);
            changed |= SetObject(serializedActor, "m_ElementUxml", LoadFirst<VisualTreeAsset>(FooniElementUxmlCandidates, FooniElementUxmlSearchFilter));
            changed |= SetObject(serializedActor, "m_ElementStyle", LoadFirst<StyleSheet>(FooniElementStyleCandidates, FooniElementStyleSearchFilter));
            var scriptGraph = EnsureFooniScriptGraph();
            changed |= SetObject(serializedActor, "m_scriptGraph", scriptGraph);

            if (changed)
            {
                serializedActor.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(actorObj);
                AssetDatabase.SaveAssets();
            }

            return actorObj;
        }

        /// <summary>
        /// 指定パスに ScriptGraphAsset を作成し、既に存在する場合はそれを返す。
        /// </summary>
        private static ScriptGraphAsset CreateScriptGraphAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            var existing = AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                var normalized = directory.Replace("\\", "/");
                EnsureFolder(normalized);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            var macro = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            macro.graph = new FlowGraph();

            AssetDatabase.CreateAsset(macro, assetPath);
            EditorUtility.SetDirty(macro);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return macro;
        }

        /// <summary>
        /// Fooni 用ポートレートを候補パスと GUID 検索で探し、見つからなければプレースホルダーを生成する。
        /// 生成済みのテクスチャがあれば再利用し、AssetDatabase への不要な書き込みを避ける。
        /// </summary>
        private static Texture2D EnsureFooniPortraitTexture()
        {
            var texture = LoadFirst<Texture2D>(FooniPortraitCandidates, FooniPortraitSearchFilter, "Fooni", false);
            if (texture != null)
            {
                return texture;
            }

            texture = CreateFooniPlaceholderTexture();
            if (texture != null)
            {
                return texture;
            }

            Debug.LogWarning("[FUnity.Setup] Fooni 用ポートレートを生成できませんでした。");
            return null;
        }

        /// <summary>
        /// Fooni 向けの ScriptGraphAsset を候補から取得し、無ければプレースホルダーを生成する。
        /// Visual Scripting は必須依存のため、生成失敗時のみ警告を出す。
        /// </summary>
        private static ScriptGraphAsset EnsureFooniScriptGraph()
        {
            var scriptGraph = LoadFirst<ScriptGraphAsset>(FooniScriptGraphCandidates, FooniScriptGraphSearchFilter, "Fooni_FloatSetup", false);
            if (scriptGraph != null)
            {
                return scriptGraph;
            }

            scriptGraph = CreateScriptGraphAsset(GeneratedFooniGraphAssetPath);
            if (scriptGraph != null)
            {
                Debug.Log($"[FUnity.Setup] プレースホルダーの ScriptGraphAsset を生成しました: {GeneratedFooniGraphAssetPath}");
                return scriptGraph;
            }

            Debug.LogWarning("[FUnity.Setup] Fooni 用 ScriptGraphAsset を生成できませんでした。");
            return null;
        }

        /// <summary>
        /// Fooni 用の仮ポートレート画像を PNG で生成し、Generated 配下に保存する。
        /// 既に存在する場合はロードだけ行い、複数回の再生成を避ける。
        /// </summary>
        private static Texture2D CreateFooniPlaceholderTexture()
        {
            EnsureFolder(GeneratedRootFolderPath);
            EnsureFolder(GeneratedTextureFolderPath);

            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GeneratedFooniTextureAssetPath);
            if (existing != null)
            {
                return existing;
            }

            var absolutePath = Path.GetFullPath(GeneratedFooniTextureAssetPath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (string.IsNullOrEmpty(directory))
            {
                return null;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            const int width = 256;
            const int height = 256;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "FooniPlaceholder"
            };

            var colors = new Color32[width * height];
            var lightColor = new Color32(123, 167, 255, 255);
            var darkColor = new Color32(92, 134, 221, 255);
            var accentColor = new Color32(255, 215, 120, 255);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    if (x > width / 3 && x < width * 2 / 3 && y > height / 3 && y < height * 2 / 3)
                    {
                        colors[index] = accentColor;
                        continue;
                    }

                    var checker = ((x / 32) + (y / 32)) % 2 == 0;
                    colors[index] = checker ? lightColor : darkColor;
                }
            }

            texture.SetPixels32(colors);
            texture.Apply();

            var pngBytes = texture.EncodeToPNG();
            if (pngBytes == null || pngBytes.Length == 0)
            {
                Object.DestroyImmediate(texture);
                Debug.LogWarning("[FUnity.Setup] Fooni 用プレースホルダーテクスチャのエンコードに失敗しました。");
                return null;
            }

            File.WriteAllBytes(absolutePath, pngBytes);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(GeneratedFooniTextureAssetPath);
            var created = AssetDatabase.LoadAssetAtPath<Texture2D>(GeneratedFooniTextureAssetPath);
            if (created != null)
            {
                Debug.Log($"[FUnity.Setup] プレースホルダーの Fooni ポートレートを生成しました: {GeneratedFooniTextureAssetPath}");
            }

            return created;
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
        /// AssetDatabase.FindAssets に渡すフォルダ候補から、存在しないパスを除外して返す。
        /// 除外したフォルダは情報ログとして記録し、全検索に切り替える判断材料とする。
        /// </summary>
        private static string[] FilterValidFolders(IEnumerable<string> folders)
        {
            if (folders == null)
            {
                return System.Array.Empty<string>();
            }

            var result = new List<string>();
            foreach (var folder in folders)
            {
                if (string.IsNullOrEmpty(folder))
                {
                    continue;
                }

                var normalized = folder.Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(normalized))
                {
                    Debug.Log($"[FUnity.Setup] 検索対象から無効フォルダを除外しました: {normalized}");
                    continue;
                }

                if (!result.Contains(normalized))
                {
                    result.Add(normalized);
                }
            }

            return result.ToArray();
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
        /// Vector2 プロパティを更新する補助。値が同一の場合は変更しない。
        /// </summary>
        private static bool SetVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return false;
            }

            if (property.vector2Value == value)
            {
                return false;
            }

            property.vector2Value = value;
            return true;
        }

        /// <summary>
        /// float プロパティを更新する補助。差分がある場合のみ書き込む。
        /// </summary>
        private static bool SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || Mathf.Approximately(property.floatValue, value))
            {
                return false;
            }

            property.floatValue = value;
            return true;
        }

        /// <summary>
        /// bool プロパティの値を更新する。既に同じ値なら変更フラグは立てない。
        /// </summary>
        private static bool SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
            {
                return false;
            }

            property.boolValue = value;
            return true;
        }

        /// <summary>
        /// 候補パス群を優先順位順に試し、見つからなければ GUID 検索で該当アセットを拾う。
        /// 編集環境ごとの差異を吸収するため、最終手段として AssetDatabase.FindAssets を用いる。
        /// </summary>
        private static T LoadFirst<T>(string[] candidatePaths, string searchFilter, string assetName = null, bool logWhenMissing = true) where T : Object
        {
            if (candidatePaths != null)
            {
                foreach (var candidate in candidatePaths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<T>(candidate);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
            }

            var searchFolders = candidatePaths == null
                ? System.Array.Empty<string>()
                : candidatePaths
                    .Select(Path.GetDirectoryName)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Select(path => path.Replace("\\", "/"))
                    .ToArray();

            searchFolders = FilterValidFolders(searchFolders);

            if (!string.IsNullOrEmpty(searchFilter))
            {
                var guids = searchFolders.Length > 0
                    ? AssetDatabase.FindAssets(searchFilter, searchFolders)
                    : AssetDatabase.FindAssets(searchFilter);

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

            if (!string.IsNullOrEmpty(assetName))
            {
                var nameFilter = $"t:{typeof(T).Name} {assetName}";
                var guids = searchFolders.Length > 0
                    ? AssetDatabase.FindAssets(nameFilter, searchFolders)
                    : AssetDatabase.FindAssets(nameFilter);

                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(path) != assetName)
                    {
                        continue;
                    }

                    var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        return asset;
                    }
                }

                if (guids.Length == 0 && searchFolders.Length > 0)
                {
                    var fallbackGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
                    foreach (var guid in fallbackGuids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (Path.GetFileNameWithoutExtension(path) != assetName)
                        {
                            continue;
                        }

                        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                        if (asset != null)
                        {
                            return asset;
                        }
                    }
                }
            }

            if (logWhenMissing)
            {
                Debug.Log($"[FUnity.Setup] Asset not found: {typeof(T).Name} ({assetName ?? searchFilter})");
            }

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
                + "    background-size: contain;\n"
                + "    background-position: center;\n"
                + "    background-repeat: no-repeat;\n"
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
        /// PanelSettings の theme フィールドに指定テーマを割り当てる。SerializedObject を介し、Unity バージョン差異や配列型にも対応する。
        /// theme が null の場合は ScriptableObject を汚さないよう即時終了する。
        /// </summary>
        private static void AssignThemeToPanelSettings(PanelSettings panelSettings, StyleSheet theme)
        {
            if (panelSettings == null || theme == null)
            {
                // PanelSettings や Theme が無いと SerializedObject 生成が無駄になるため処理を打ち切る。
                return;
            }

            // SerializedObject を使い、公開 API の差異を無視して theme 関連フィールドを動的に操作する。
            var serializedPanel = new SerializedObject(panelSettings);
            var themeProperty = FindThemeProperty(serializedPanel);
            if (themeProperty == null)
            {
                Debug.Log("[FUnity.Setup] PanelSettings に theme プロパティが見つからなかったため、ランタイム適用にフォールバックします。");
                EnsurePanelThemeSettingsAsset(theme);
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
            else
            {
                Debug.Log("[FUnity.Setup] PanelSettings の theme プロパティ型が未対応のため、ランタイム適用にフォールバックします。");
                EnsurePanelThemeSettingsAsset(theme);
                return;
            }

            if (!changed)
            {
                // 値が変わっていなければ Apply/Save を避けて AssetDatabase の再インポートを抑制する。
                EnsurePanelThemeSettingsAsset(theme);
                return;
            }

            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panelSettings);
            AssetDatabase.SaveAssets();

            EnsurePanelThemeSettingsAsset(theme);
        }

        /// <summary>
        /// PanelSettings がテーマを保持できない場合に備え、フォールバック用の設定アセットを生成しテーマ参照を永続化する。
        /// </summary>
        private static void EnsurePanelThemeSettingsAsset(StyleSheet theme)
        {
            if (theme == null)
            {
                return;
            }

            EnsureFolder(ResourcesFolderPath);

            var settings = AssetDatabase.LoadAssetAtPath<FUnityPanelThemeSettings>(PanelThemeSettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<FUnityPanelThemeSettings>();
                AssetDatabase.CreateAsset(settings, PanelThemeSettingsAssetPath);
            }

            var serializedSettings = new SerializedObject(settings);
            var themeProperty = serializedSettings.FindProperty("m_theme");
            if (themeProperty == null)
            {
                return;
            }

            if (themeProperty.objectReferenceValue == theme)
            {
                return;
            }

            themeProperty.objectReferenceValue = theme;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// PanelSettings のテーマ関連プロパティを候補名順に探索し、最初に見つかった SerializedProperty を返す。
        /// </summary>
        private static SerializedProperty FindThemeProperty(SerializedObject serializedPanel)
        {
            foreach (var candidate in PanelThemePropertyCandidates)
            {
                var property = serializedPanel.FindProperty(candidate);
                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }

        /// <summary>
        /// StageData に学習用背景 Background_01 を割り当てる。候補が無ければ既存設定を維持する。
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

            // SerializedObject によって非公開フィールド m_backgroundImage にアクセスし、互換性を確保する。
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
        /// Resources 内に残った旧 Fooni ActorData を削除し、重複ロードを防ぐ。.gitkeep 等は残しつつ対象アセットのみ削除する。
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

            // SerializedObject を介して m_stage や m_actors といった内部名プロパティを一括更新する。
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

        /// <summary>
        /// ProjectData に Scratch / unityroom 用 ModeConfig を自動設定し、生成直後でも実行モードを切り替えられるようにする。
        /// 指定アセットが存在しない場合は警告を表示し、該当フィールドは null のまま保持する。
        /// </summary>
        private static void AssignDefaultModeConfigs(FUnityProjectData project)
        {
            if (project == null)
            {
                // Project が無い場合は SerializedObject を作成できないため、割り当て処理をスキップする。
                return;
            }

            var serializedProject = new SerializedObject(project);
            var changed = false;

            var scratchConfig = LoadModeConfigAsset<FUnityModeConfig>(ScratchModeConfigFileName);
            if (scratchConfig != null)
            {
                changed |= SetObject(serializedProject, "m_ScratchModeConfig", scratchConfig);
            }
            else
            {
                Debug.LogWarning("[FUnity] ScratchModeConfig 用の ModeConfig アセットが Assets または Packages 配下で見つかりませんでした。");
            }

            var unityroomConfig = LoadModeConfigAsset<FUnityModeConfig>(UnityroomModeConfigFileName);
            if (unityroomConfig != null)
            {
                changed |= SetObject(serializedProject, "m_UnityroomModeConfig", unityroomConfig);
            }
            else
            {
                Debug.LogWarning("[FUnity] UnityroomModeConfig 用の ModeConfig アセットが Assets または Packages 配下で見つかりませんでした。");
            }

            if (!changed)
            {
                return;
            }

            serializedProject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(project);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 指定ファイル名の ModeConfig アセットを Assets 直下と FUnity パッケージ配下から順に検索する。
        /// </summary>
        /// <typeparam name="T">検索対象となる ScriptableObject 型。</typeparam>
        /// <param name="fileName">一致させたいアセットファイル名。</param>
        /// <returns>見つかったアセット。存在しない場合は null。</returns>
        private static T LoadModeConfigAsset<T>(string fileName) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var assetFromAssets = FindAssetByName<T>(fileName, "Assets");
            if (assetFromAssets != null)
            {
                return assetFromAssets;
            }

            var assetFromPackage = FindAssetByName<T>(fileName, PackageRootPath);
            if (assetFromPackage != null)
            {
                return assetFromPackage;
            }

            return null;
        }

        /// <summary>
        /// 指定フォルダ配下のアセットからファイル名一致するものを検索し、最初に見つかったものを返す。
        /// </summary>
        /// <typeparam name="T">読み込むアセットの型。</typeparam>
        /// <param name="fileName">判定に利用するファイル名。</param>
        /// <param name="searchRoot">検索を行うルートフォルダ。</param>
        /// <returns>条件を満たすアセット。見つからない場合は null。</returns>
        private static T FindAssetByName<T>(string fileName, string searchRoot) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(searchRoot))
            {
                return null;
            }

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchRoot });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith("/" + fileName, System.StringComparison.Ordinal))
                {
                    return AssetDatabase.LoadAssetAtPath<T>(assetPath);
                }
            }

            return null;
        }
    }
}
#endif
