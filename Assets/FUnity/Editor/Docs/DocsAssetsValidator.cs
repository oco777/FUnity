#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FUnity.Editor.Docs
{
    /// <summary>
    /// ドキュメント内の画像参照とヒーロー画像の実体を検証するユーティリティです。
    /// README.md と Docs 配下の Markdown を走査し、パスの揺れや空ファイルを検出します。
    /// </summary>
    [InitializeOnLoad]
    public static class DocsAssetsValidator
    {
        /// <summary>初回バリデーション実行が済んだかどうかを示すフラグです。</summary>
        private static bool m_hasExecuted;

        /// <summary>許可された画像パスに含まれるべきディレクトリ名です。</summary>
        private const string DocsImagesFragment = "Docs/images/";

        /// <summary>ヒーロー画像の相対パスです。リポジトリ直下の Docs/images に配置します。</summary>
        private const string HeroImageRelativePath = "Docs/images/readme-hero.png";

        /// <summary>Markdown の画像記法を抽出するための正規表現です。</summary>
        private static readonly Regex m_imagePattern = new Regex(@"!\[[^\]]*\]\(([^\)]+)\)", RegexOptions.Compiled);

        /// <summary>監視対象となる Markdown ファイルの追加候補です。</summary>
        private static readonly IReadOnlyList<string> m_explicitMarkdown = new[] { "README.md" };

        /// <summary>
        /// クラスがロードされたタイミングでバリデーションを予約します。
        /// </summary>
        static DocsAssetsValidator()
        {
            EditorApplication.delayCall += RunValidationOnce;
        }

        /// <summary>
        /// Unity メニューから手動でバリデーションを実行するためのエントリです。
        /// </summary>
        [MenuItem("FUnity/Diagnostics/Validate Docs Assets")]
        public static void ValidateFromMenu()
        {
            ExecuteValidation();
        }

        /// <summary>
        /// EditorApplication.delayCall に登録された一度きりのバリデーション実行です。
        /// </summary>
        private static void RunValidationOnce()
        {
            EditorApplication.delayCall -= RunValidationOnce;
            if (m_hasExecuted)
            {
                return;
            }

            m_hasExecuted = true;
            ExecuteValidation();
        }

        /// <summary>
        /// 実際にドキュメントファイルを走査し、警告を必要に応じて出力します。
        /// </summary>
        private static void ExecuteValidation()
        {
            try
            {
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var markdownFiles = CollectMarkdownFiles(projectRoot).ToList();

                foreach (var filePath in markdownFiles)
                {
                    ValidateMarkdown(projectRoot, filePath);
                }

                ValidateHeroImage(projectRoot);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FUnity.Docs] ドキュメント検証中に例外が発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// プロジェクトルートから対象の Markdown ファイルを列挙します。
        /// </summary>
        /// <param name="projectRoot">Unity プロジェクトのルートパス。</param>
        /// <returns>検証対象 Markdown ファイルの絶対パス。</returns>
        private static IEnumerable<string> CollectMarkdownFiles(string projectRoot)
        {
            foreach (var explicitFile in m_explicitMarkdown)
            {
                var candidate = Path.Combine(projectRoot, explicitFile);
                if (File.Exists(candidate))
                {
                    yield return candidate;
                }
            }

            var docsRoot = Path.Combine(projectRoot, "Docs");
            if (!Directory.Exists(docsRoot))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        /// <summary>
        /// 単一 Markdown ファイル内の画像参照を検査し、Docs/images/ 配下への統一を促します。
        /// </summary>
        /// <param name="projectRoot">Unity プロジェクトのルートパス。</param>
        /// <param name="absolutePath">検証対象ファイルの絶対パス。</param>
        private static void ValidateMarkdown(string projectRoot, string absolutePath)
        {
            var markdown = File.ReadAllText(absolutePath);
            var imageLinks = ExtractImageLinks(markdown);
            foreach (var link in imageLinks)
            {
                if (!link.Contains("readme-hero", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!link.Contains(DocsImagesFragment, StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[FUnity.Docs] {Path.GetRelativePath(projectRoot, absolutePath)} のヒーロー画像参照が Docs/images/ 以外になっています: {link}");
                }
            }
        }

        /// <summary>
        /// ヒーロー画像自体の存在とサイズをチェックします。
        /// </summary>
        /// <param name="projectRoot">Unity プロジェクトのルートパス。</param>
        private static void ValidateHeroImage(string projectRoot)
        {
            var heroPath = BuildProjectPath(projectRoot, HeroImageRelativePath);
            if (!File.Exists(heroPath))
            {
                Debug.LogWarning($"[FUnity.Docs] ヒーロー画像が見つかりません。{HeroImageRelativePath} に PNG を配置してください。");
                return;
            }

            var info = new FileInfo(heroPath);
            if (info.Length <= 0)
            {
                Debug.LogWarning($"[FUnity.Docs] {HeroImageRelativePath} のファイルサイズが 0 バイトです。正しい PNG をアップロードしてください。");
            }
        }

        /// <summary>
        /// Markdown から画像リンクを抽出します。タイトル付き書式にも対応するよう空白以降を切り捨てます。
        /// </summary>
        /// <param name="markdown">解析対象の Markdown テキスト。</param>
        /// <returns>画像リンクの一覧。</returns>
        private static IEnumerable<string> ExtractImageLinks(string markdown)
        {
            foreach (Match match in m_imagePattern.Matches(markdown))
            {
                var raw = match.Groups[1].Value.Trim();
                var spaceIndex = raw.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    raw = raw.Substring(0, spaceIndex);
                }

                yield return raw;
            }
        }

        /// <summary>
        /// プロジェクトルートと相対パスから OS 依存性のない絶対パスを構築します。
        /// </summary>
        /// <param name="projectRoot">Unity プロジェクトのルートパス。</param>
        /// <param name="relativePath">スラッシュ区切りの相対パス。</param>
        /// <returns>絶対パス。</returns>
        private static string BuildProjectPath(string projectRoot, string relativePath)
        {
            var segments = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var parts = new List<string> { projectRoot };
            parts.AddRange(segments);
            return Path.Combine(parts.ToArray());
        }
    }
}
#endif
