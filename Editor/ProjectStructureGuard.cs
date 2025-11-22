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

}
