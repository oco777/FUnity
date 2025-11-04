using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FUnity.Editor
{
    /// <summary>
    /// パッケージ内のランタイムコード配置を監視し、`Assets/FUnity/Runtime/` に C# や asmdef が混入した場合に警告・自動修正を行うエディタガードです。
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectStructureGuard
    {
        /// <summary>`Assets/FUnity/Runtime` 配下で検査する相対ルート。</summary>
        private const string ForbiddenAssetRoot = "Assets/FUnity/Runtime";

        /// <summary>自動移動先となるパッケージ内 Runtime フォルダを検出する際に参照するキー。</summary>
        private const string GuardScriptName = "ProjectStructureGuard.cs";

        /// <summary>監視対象とするファイル拡張子一覧。</summary>
        private static readonly string[] s_WatchedExtensions = { ".cs", ".asmdef", ".asmref" };

        /// <summary>検出済みの違反ファイル一覧をキャッシュします。</summary>
        private static readonly List<string> s_LastDetected = new List<string>();

        /// <summary>メニュー表示用のキャッシュ済みパッケージルート。</summary>
        private static string s_PackageRoot;

        /// <summary>
        /// 静的コンストラクタ。ドメインリロード後に遅延実行でチェックを実行します。
        /// </summary>
        static ProjectStructureGuard()
        {
            EditorApplication.delayCall += () => CheckRuntimeLayout(true, false);
        }

        /// <summary>
        /// ランタイム配置違反を検出し、必要に応じて自動移動を試みます。
        /// </summary>
        /// <param name="logToConsole">Unity コンソールにエラーを出力するかどうか。</param>
        /// <param name="autoFix">検出したファイルを自動移動するかどうか。</param>
        private static void CheckRuntimeLayout(bool logToConsole, bool autoFix)
        {
            s_LastDetected.Clear();

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

            s_LastDetected.AddRange(offenders);

            if (logToConsole)
            {
                foreach (var assetPath in offenders)
                {
                    Debug.LogError($"[FUnity] Runtime code must live under 'Runtime/'. Found: {assetPath}");
                }

                Debug.LogError("[FUnity] FUnity/Tools/Fix Runtime Layout で自動移動を試せます。競合がある場合は手動で整理してください。");
            }

            if (!autoFix)
            {
                return;
            }

            var packageRoot = ResolvePackageRoot();
            if (string.IsNullOrEmpty(packageRoot))
            {
                Debug.LogError("[FUnity] ProjectStructureGuard: パッケージルートを特定できず、自動移動を中止しました。");
                return;
            }

            var movedMessages = new List<string>();
            foreach (var assetPath in offenders)
            {
                TryMoveAsset(assetPath, packageRoot, movedMessages);
            }

            if (movedMessages.Count > 0)
            {
                foreach (var message in movedMessages)
                {
                    Debug.Log(message);
                }

                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// `FUnity/Tools/Fix Runtime Layout` メニューの有効状態を判定します。
        /// </summary>
        /// <returns>違反ファイルが存在する場合は true。</returns>
        [MenuItem("FUnity/Tools/Fix Runtime Layout", true)]
        private static bool ValidateFixRuntimeLayout()
        {
            var projectRoot = GetProjectRoot();
            if (string.IsNullOrEmpty(projectRoot))
            {
                return false;
            }

            if (s_LastDetected.Count > 0)
            {
                return true;
            }

            var forbiddenFullPath = Path.Combine(projectRoot, ForbiddenAssetRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(forbiddenFullPath))
            {
                return false;
            }

            return Directory.EnumerateFiles(forbiddenFullPath, "*.*", SearchOption.AllDirectories)
                .Any(path => s_WatchedExtensions.Contains(Path.GetExtension(path)));
        }

        /// <summary>
        /// 配置違反を検出した際に、Runtime フォルダへ一括移動します。
        /// </summary>
        [MenuItem("FUnity/Tools/Fix Runtime Layout", priority = 500)]
        private static void FixRuntimeLayout()
        {
            CheckRuntimeLayout(true, true);
        }

        /// <summary>
        /// アセットパスを Runtime フォルダへ移動します。既に存在する場合はスキップして警告します。
        /// </summary>
        /// <param name="assetPath">移動元のアセットパス。</param>
        /// <param name="packageRoot">パッケージルートの相対パス。</param>
        /// <param name="messages">ログ出力用のメッセージ蓄積先。</param>
        private static void TryMoveAsset(string assetPath, string packageRoot, List<string> messages)
        {
            if (!assetPath.StartsWith(ForbiddenAssetRoot))
            {
                return;
            }

            var relative = assetPath.Length > ForbiddenAssetRoot.Length
                ? assetPath.Substring(ForbiddenAssetRoot.Length).TrimStart('/')
                : string.Empty;

            if (string.IsNullOrEmpty(relative))
            {
                messages.Add($"[FUnity] Skip moving '{assetPath}' because the relative path is empty.");
                return;
            }

            var targetPath = $"{packageRoot}/Runtime/{relative}";
            if (AssetDatabase.LoadAssetAtPath<Object>(targetPath) != null)
            {
                messages.Add($"[FUnity] Skip moving '{assetPath}' because '{targetPath}' already exists.");
                return;
            }

            var result = AssetDatabase.MoveAsset(assetPath, targetPath);
            if (!string.IsNullOrEmpty(result))
            {
                messages.Add($"[FUnity] Failed to move '{assetPath}' -> '{targetPath}': {result}");
            }
            else
            {
                messages.Add($"[FUnity] Moved '{assetPath}' -> '{targetPath}'.");
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

        /// <summary>
        /// パッケージルートのアセットパスを特定します。見つからない場合は既定値を返します。
        /// </summary>
        private static string ResolvePackageRoot()
        {
            if (!string.IsNullOrEmpty(s_PackageRoot))
            {
                return s_PackageRoot;
            }

            var guids = AssetDatabase.FindAssets($"{Path.GetFileNameWithoutExtension(GuardScriptName)} t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(GuardScriptName))
                {
                    var directory = Path.GetDirectoryName(path);
                    if (string.IsNullOrEmpty(directory))
                    {
                        continue;
                    }

                    var packageRoot = Path.GetDirectoryName(directory);
                    if (!string.IsNullOrEmpty(packageRoot))
                    {
                        s_PackageRoot = packageRoot.Replace("\\", "/");
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(s_PackageRoot))
            {
                s_PackageRoot = "Packages/com.papacoder.funity";
            }

            return s_PackageRoot;
        }
    }
}
