#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Unity.VisualScripting;
using FUnity.Runtime.Core;

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

        // Theme は UI Builder 標準配置（Legacy）を最優先し、無ければ FUnity 配下の正規 USS を生成して使用する。
        private const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss";
        private const string CanonicalThemePath = FUnityUiUssFolderPath + "/UnityDefaultRuntimeTheme.uss";

        private const string PanelSettingsAssetPath = FUnityUiFolderPath + "/FUnityPanelSettings.asset";
        private const string DuplicateActorResourcePath = ResourcesFolderPath + "/FUnityActorData_Fooni.asset";
        private const string ActorRootFolderPath = "Assets/FUnity/Data";
        private const string ActorDataFolderPath = ActorRootFolderPath + "/Actors";
        private const string ActorAssetPath = ActorDataFolderPath + "/FUnityActorData_Fooni.asset";
        private const string ScratchActorAssetPath = ActorDataFolderPath + "/FUnityActorData_Starter.asset";
        private const string FooniScriptGraphSearchFilter = "t:ScriptGraphAsset Fooni_FloatSetup";
        private const string ScratchRunnerMacroSearchFilter = "t:ScriptGraphAsset MoveWithArrow";

        // 背景テクスチャはプロジェクト直下の正規パスを優先し、無ければパッケージ同梱版から順に探す。
        private static readonly string[] StageBackgroundCandidates =
        {
            "Assets/FUnity/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Runtime/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Art/Backgrounds/Background_01.png"
        };

        // Fooni 用ポートレート/UXML/USS も正規パス→パッケージの順で探し、テンプレート想定の root/portrait 要素を持つファイルを期待する。
        private static readonly string[] FooniPortraitCandidates =
        {
            "Assets/FUnity/Art/Characters/Fooni.png",
            "Packages/com.papacoder.funity/Runtime/Art/Characters/Fooni.png"
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
            "Assets/FUnity/VisualScripting/Macros/Fooni_FloatSetup.asset"
        };
        private const string FooniScriptGraphFolder = "Assets/FUnity/VisualScripting/Macros";
        private static readonly string[] ScratchRunnerMacroCandidates =
        {
            "Assets/FUnity/VisualScripting/Macros/MoveWithArrow.asset",
            "Packages/com.papacoder.funity/VisualScripting/Macros/MoveWithArrow.asset",
            "Packages/com.papacoder.funity/Runtime/VisualScripting/Macros/MoveWithArrow.asset"
        };

        private const string BackgroundSearchFilter = "t:Texture2D Background_01";
        private const string FooniPortraitSearchFilter = "t:Texture2D Fooni";
        private const string FooniElementUxmlSearchFilter = "t:VisualTreeAsset FooniElement";
        private const string FooniElementStyleSearchFilter = "t:StyleSheet FooniElement";

        // FUnity → Create → Default Project Data。ProjectData/StageData/ActorData/PanelSettings を生成し相互リンクする。
        [MenuItem("FUnity/Create/Default Project Data")]
        /// <summary>
        /// - Resources/Assets/FUnity 配下を作成し、Project/Stage/Actor の ScriptableObject を用意する。
        /// - UI Theme は正規/旧配置から探索し、PanelSettings を確保して SerializedObject 経由で割り当てる。
        /// - Stage 背景に Art/Backgrounds/Background_01.* を設定し、ActorData_Fooni に Portrait/UXML/USS を適用する。
        /// - 不足アセットは警告にとどめて続行し、最後に SaveAssets/Refresh で AssetDatabase の状態を確定させる。
        /// </summary>
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

        [MenuItem("FUnity/Create/Scratch Starter (Actor+Stage+VS)")]
        /// <summary>
        /// Scratch 本の練習に必要な Project/Stage/Actor/Runner を一括生成する。
        /// 既存アセットは尊重しつつ、Starter 用 ActorData と VS Runner を最低限構成する。
        /// </summary>
        public static void CreateScratchStarter()
        {
            CreateDefault();

            var project = AssetDatabase.LoadAssetAtPath<FUnityProjectData>(ProjectAssetPath);
            var stage = AssetDatabase.LoadAssetAtPath<FUnityStageData>(StageAssetPath);
            var actor = ConfigureScratchActorData();

            AssignStageBackground(stage);
            LinkProjectData(project, stage, actor);
            ConfigureScratchRunner(project);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (project != null)
            {
                Selection.activeObject = project;
            }

            Debug.Log("[FUnity] Created Scratch starter assets (Project / Stage / Actor / Runner).");
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
            changed |= SetObject(serializedActor, "m_portrait", LoadFirst<Texture2D>(FooniPortraitCandidates, FooniPortraitSearchFilter));
            changed |= SetObject(serializedActor, "m_ElementUxml", LoadFirst<VisualTreeAsset>(FooniElementUxmlCandidates, FooniElementUxmlSearchFilter));
            changed |= SetObject(serializedActor, "m_ElementStyle", LoadFirst<StyleSheet>(FooniElementStyleCandidates, FooniElementStyleSearchFilter));

            var scriptGraphProperty = serializedActor.FindProperty("m_scriptGraph");
            if (scriptGraphProperty != null && scriptGraphProperty.objectReferenceValue == null)
            {
                var defaultScriptGraph = LoadFirst<ScriptGraphAsset>(FooniScriptGraphCandidates, FooniScriptGraphSearchFilter);

                if (defaultScriptGraph == null)
                {
                    EnsureFolder(FooniScriptGraphFolder);
                    var targetPath = FooniScriptGraphCandidates.FirstOrDefault();
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        defaultScriptGraph = CreateScriptGraphAsset(targetPath);
                    }
                }

                if (defaultScriptGraph != null)
                {
                    scriptGraphProperty.objectReferenceValue = defaultScriptGraph;
                    changed = true;
                }
                else
                {
                    Debug.LogWarning("[FUnity] Failed to assign or create a ScriptGraph for Fooni actor.");
                }
            }

            if (changed)
            {
                serializedActor.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(actorObj);
                AssetDatabase.SaveAssets();
            }

            return actorObj;
        }

        /// <summary>
        /// Scratch Starter 向けの ActorData を確保し、初期位置・サイズを標準化する。
        /// Portrait/UXML/USS は Fooni と同じ候補を流用し、浮遊アニメーションは無効化する。
        /// </summary>
        private static FUnityActorData ConfigureScratchActorData()
        {
            EnsureFolder(FUnityFolderPath);
            EnsureFolder(ActorRootFolderPath);
            EnsureFolder(ActorDataFolderPath);

            var actorObj = AssetDatabase.LoadAssetAtPath<FUnityActorData>(ScratchActorAssetPath);
            if (actorObj == null)
            {
                actorObj = ScriptableObject.CreateInstance<FUnityActorData>();
                AssetDatabase.CreateAsset(actorObj, ScratchActorAssetPath);
            }

            var serializedActor = new SerializedObject(actorObj);
            var changed = false;

            changed |= SetString(serializedActor, "m_displayName", "Scratch Starter");
            changed |= SetObject(serializedActor, "m_portrait", LoadFirst<Texture2D>(FooniPortraitCandidates, FooniPortraitSearchFilter));
            changed |= SetObject(serializedActor, "m_ElementUxml", LoadFirst<VisualTreeAsset>(FooniElementUxmlCandidates, FooniElementUxmlSearchFilter));
            changed |= SetObject(serializedActor, "m_ElementStyle", LoadFirst<StyleSheet>(FooniElementStyleCandidates, FooniElementStyleSearchFilter));
            changed |= SetVector2(serializedActor, "m_initialPosition", new Vector2(240f, 200f));
            changed |= SetVector2(serializedActor, "m_size", new Vector2(180f, 180f));
            changed |= SetFloat(serializedActor, "m_moveSpeed", 240f);
            changed |= SetBool(serializedActor, "m_floatAnimation", false);

            if (changed)
            {
                serializedActor.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(actorObj);
                AssetDatabase.SaveAssets();
            }

            return actorObj;
        }

        /// <summary>
        /// Scratch Starter 用の Visual Scripting Runner 定義を <see cref="FUnityProjectData"/> に書き込む。
        /// 既存リストは 1 件に揃え、MoveWithArrow マクロを割り当てる。
        /// </summary>
        private static void ConfigureScratchRunner(FUnityProjectData project)
        {
            if (project == null)
            {
                return;
            }

            if (project.runners == null)
            {
                project.runners = new List<FUnityProjectData.RunnerEntry>();
            }

            var macro = LoadFirst<ScriptGraphAsset>(ScratchRunnerMacroCandidates, ScratchRunnerMacroSearchFilter);

            if (project.runners.Count == 0)
            {
                project.runners.Add(new FUnityProjectData.RunnerEntry());
            }

            var entry = project.runners[0];
            entry.name = "Scratch VS Runner";
            entry.macro = macro;

            if (entry.objectVariables == null)
            {
                entry.objectVariables = new List<FUnityProjectData.RunnerEntry.ObjectVar>();
            }

            EditorUtility.SetDirty(project);
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

            Debug.Log($"[FUnity] Created ScriptGraphAsset: {assetPath}");
            return macro;
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
        /// themeStyleSheets → m_ThemeStyleSheets → themeStyleSheet の順で探索し、新旧 API 名と内部フィールドを網羅する。
        /// Unity のバージョン差異を吸収するため、SerializedObject を介した動的アクセスを採用する。
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
    }
}
#endif
