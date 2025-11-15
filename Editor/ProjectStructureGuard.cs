using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FUnity.Editor
{
    /// <summary>
    /// パッケージ内のランタイムコード配置を監視し、`Assets/FUnity/Runtime/` に C# や asmdef が混入した場合に警告を行うエディタガードです。
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectStructureGuard
    {
        /// <summary>`Assets/FUnity/Runtime` 配下で検査する相対ルート。</summary>
        private const string ForbiddenAssetRoot = "Assets/FUnity/Runtime";

        /// <summary>監視対象とするファイル拡張子一覧。</summary>
        private static readonly string[] s_WatchedExtensions = { ".cs", ".asmdef", ".asmref" };

        /// <summary>
        /// 静的コンストラクタ。ドメインリロード後に遅延実行でチェックを実行します。
        /// </summary>
        static ProjectStructureGuard()
        {
            EditorApplication.delayCall += () => CheckRuntimeLayout(true);
        }

        /// <summary>
        /// ランタイム配置違反を検出し、必要に応じてログ出力を行います。
        /// </summary>
        /// <param name="logToConsole">Unity コンソールにエラーを出力するかどうか。</param>
        private static void CheckRuntimeLayout(bool logToConsole)
        {
            var projectRoot = GetProjectRoot();
            if (string.IsNullOrEmpty(projectRoot))
            {
                return;
            }

            var forbiddenFullPath = Path.Combine(projectRoot, ForbiddenAssetRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(forbiddenFullPath))
            {
                return;
            }

            var offenders = Directory.EnumerateFiles(forbiddenFullPath, "*.*", SearchOption.AllDirectories)
                .Where(path => s_WatchedExtensions.Contains(Path.GetExtension(path)))
                .Select(path => ToAssetPath(path, projectRoot))
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList();

            if (offenders.Count == 0)
            {
                return;
            }

            if (logToConsole)
            {
                foreach (var assetPath in offenders)
                {
                    Debug.LogError($"[FUnity] Runtime code must live under 'Runtime/'. Found: {assetPath}");
                }

                Debug.LogError("[FUnity] Runtime フォルダへ手動で移動するか、競合を解消してください。");
            }
        }

        /// <summary>
        /// Unity プロジェクトのルートパスを返します。
        /// </summary>
        private static string GetProjectRoot()
        {
            var assetsPath = Application.dataPath;
            return string.IsNullOrEmpty(assetsPath) ? string.Empty : Path.GetDirectoryName(assetsPath);
        }

        /// <summary>
        /// 絶対パスをプロジェクト相対のアセットパスに変換します。
        /// </summary>
        /// <param name="fullPath">変換する絶対パス。</param>
        /// <param name="projectRoot">プロジェクトルート。</param>
        private static string ToAssetPath(string fullPath, string projectRoot)
        {
            var normalizedRoot = projectRoot.Replace(Path.DirectorySeparatorChar, '/');
            var normalizedFull = fullPath.Replace(Path.DirectorySeparatorChar, '/');
            if (!normalizedFull.StartsWith(normalizedRoot))
            {
                return string.Empty;
            }

            return normalizedFull.Substring(normalizedRoot.Length + 1);
        }
    }

    /// <summary>
    /// 旧フィールド (Portrait / PortraitSprite) を Sprites へ移行するための簡易マイグレーションウィンドウ。
    /// フィールド自体は 2025-05 時点で削除済みのため、古いアセットの確認・変換用途として維持する。
    /// </summary>
    public sealed class FUnityActorDataMigrationWindow : EditorWindow
    {
        /// <summary>ウィンドウタイトルに表示する文字列。</summary>
        private const string WindowTitle = "FUnity ActorData Migration";

        /// <summary>メニューに表示するコマンドパス。</summary>
        private const string MenuPath = "FUnity/Tools/Migrate ActorData Portrait -> Sprites";

        /// <summary>Sprites フィールドのシリアライズド名。</summary>
        private const string SpritesPropertyName = "m_sprites";

        /// <summary>旧 PortraitSprite フィールドのシリアライズド名。</summary>
        private const string PortraitSpritePropertyName = "m_portraitSprite";

        /// <summary>旧 Portrait(Texture2D) フィールドのシリアライズド名。</summary>
        private const string PortraitTexturePropertyName = "m_portrait";

        /// <summary>
        /// メニューコマンドからウィンドウを開くエントリーポイント。
        /// </summary>
        [MenuItem(MenuPath)]
        private static void Open()
        {
            GetWindow<FUnityActorDataMigrationWindow>(WindowTitle);
        }

        /// <summary>
        /// エディタウィンドウの GUI を描画し、マイグレーション処理を実行するボタンを提供する。
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Legacy Portrait フィールドに保存された Sprite を Sprites[0] へ移行します（現行バージョンではフィールドが削除されています）。", MessageType.Info);

            if (GUILayout.Button("Scan and Migrate All FUnityActorData"))
            {
                MigrateAll();
            }
        }

        /// <summary>
        /// プロジェクト内の FUnityActorData アセットを走査し、旧フィールドから Sprites へデータを移行する。
        /// </summary>
        private static void MigrateAll()
        {
            var guids = AssetDatabase.FindAssets("t:FUnityActorData");
            var migratedCount = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<FUnity.Runtime.Core.FUnityActorData>(path);
                if (data == null)
                {
                    continue;
                }

                Undo.RecordObject(data, "Migrate FUnityActorData");

                var serializedObject = new SerializedObject(data);
                var spritesProperty = serializedObject.FindProperty(SpritesPropertyName);
                var portraitSpriteProperty = serializedObject.FindProperty(PortraitSpritePropertyName);
                var portraitTextureProperty = serializedObject.FindProperty(PortraitTexturePropertyName);

                if (spritesProperty == null)
                {
                    Debug.LogWarning($"[FUnity] '{path}' の Sprites フィールドを取得できませんでした。スクリプト定義を確認してください。");
                    continue;
                }

                var hasSprites = spritesProperty.arraySize > 0;
                var hasPortraitSprite = portraitSpriteProperty != null && portraitSpriteProperty.objectReferenceValue != null;
                var hasPortraitTexture = portraitTextureProperty != null && portraitTextureProperty.objectReferenceValue != null;

                if (hasSprites || !hasPortraitSprite)
                {
                    if (hasPortraitTexture && !hasSprites)
                    {
                        Debug.LogWarning($"[FUnity] '{path}' は Texture2D Portrait のみ設定されています。Sprite に変換して Sprites に登録してください。");
                    }

                    continue;
                }

                spritesProperty.arraySize = 0;
                spritesProperty.InsertArrayElementAtIndex(0);
                spritesProperty.GetArrayElementAtIndex(0).objectReferenceValue = portraitSpriteProperty.objectReferenceValue;

                portraitSpriteProperty.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(data);
                migratedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[FUnity] Migrated {migratedCount} FUnityActorData asset(s).");
        }
    }
}
