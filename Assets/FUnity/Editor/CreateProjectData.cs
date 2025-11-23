#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;

namespace FUnity.EditorTools
{
    // プロジェクト作成時の共通初期化ロジックをまとめたエディター用ユーティリティ。UNITY_EDITOR 前提。
    // FUnityProjectCreatorWindow から呼び出され、新規 Project/Stage に背景や ModeConfig を適用する役割のみ担う。
    public static class CreateProjectData
    {
        private const string FUnityFolderPath = "Assets/FUnity";
        private const string PackageRootPath = "Packages/com.papacoder.funity";
        private const string ScratchModeConfigFileName = "FUnityModeConfig_Scratch.asset";
        private const string UnityroomModeConfigFileName = "FUnityModeConfig_Unityroom.asset";

        // 背景テクスチャはプロジェクト直下の正規パスを優先し、無ければパッケージ同梱版から順に探す。
        private static readonly string[] StageBackgroundCandidates =
        {
            "Assets/FUnity/Runtime/Resources/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Runtime/Resources/Backgrounds/Background_01.png",
            "Assets/FUnity/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Runtime/Art/Backgrounds/Background_01.png",
            "Packages/com.papacoder.funity/Art/Backgrounds/Background_01.png"
        };

        private const string BackgroundSearchFilter = "t:Texture2D Background_01";

        /// <summary>
        /// 新規作成直後の Project/Stage に対し、Actor 以外の共通初期設定をまとめて適用する。
        /// 背景設定や ModeConfig のデフォルト化など、プロジェクト固有の初期化のみを担当する。
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

            AssignStageBackground(stage);

            AssignDefaultModeConfigs(project);
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
