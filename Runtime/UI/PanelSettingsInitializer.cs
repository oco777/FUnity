// Updated: 2025-02-14
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    /// <summary>
    /// UI Toolkit の <see cref="PanelSettings"/> アセットを初期化し、ランタイム/エディタ双方で利用できるよう保証するユーティリティ。
    /// </summary>
    /// <remarks>
    /// 依存関係: Resources/FUnityPanelSettings.asset, UnityEditor（テーマ割り当て時）
    /// 想定ライフサイクル: ランタイム起動前 (<see cref="RuntimeInitializeOnLoadMethodAttribute"/>) に 1 度実行。
    /// View 層の基盤構築に該当し、ビジネスロジックは含まない。
    /// </remarks>
    public static class PanelSettingsInitializer
    {
        /// <summary>検索・生成対象となる PanelSettings アセット名。</summary>
        private const string ResourceName = "FUnityPanelSettings";
        /// <summary>フォールバックテーマ設定のリソース名。</summary>
        private const string ThemeSettingsResourceName = "FUnityPanelThemeSettings";

        /// <summary>キャッシュ済みのフォールバックテーマ。</summary>
        private static StyleSheet s_cachedFallbackTheme;

        /// <summary>
        /// 実行前に PanelSettings をロードし、存在しない場合は生成のみで一時利用する。
        /// Editor 環境でも Assets/Resources への保存は行わず、UI Document への割り当てに専念する。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsurePanelSettings()
        {
            var panelSettings = Resources.Load<PanelSettings>(ResourceName);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

                // ランタイム生成時に PanelSettings の標準値を整え、後続処理の想定を満たす。
                panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                panelSettings.referenceDpi = 96f;
                panelSettings.targetDisplay = 0;
                panelSettings.clearDepthStencil = true;
                panelSettings.clearColor = true;
                panelSettings.colorClearValue = Color.clear;
            }

            // テーマ未設定による警告を防ぐため、確実に Theme Style Sheet を割り当てる。
            EnsureThemeStyleSheet(panelSettings);
            var fallbackTheme = LoadFallbackTheme();

            var uiDocuments = Object.FindObjectsOfType<UIDocument>();
            if (uiDocuments == null || uiDocuments.Length == 0)
            {
                // UI Document がシーンに無い場合は配布処理が不要なため終了する。
                return;
            }

            foreach (var uiDocument in uiDocuments)
            {
                if (uiDocument != null && uiDocument.panelSettings == null)
                {
                    // PanelSettings を持たない UI Document のみに付与し、ユーザー設定を上書きしない。
                    uiDocument.panelSettings = panelSettings;
                }

                ApplyFallbackTheme(uiDocument, fallbackTheme);
            }
        }

        /// <summary>
        /// PanelSettings に Theme Style Sheet を最低 1 つ割り当て、UI Toolkit の標準スタイルを保証する。
        /// </summary>
        /// <param name="panelSettings">割り当て対象となる PanelSettings。null の場合は処理しない。</param>
        private static void EnsureThemeStyleSheet(PanelSettings panelSettings)
        {
            if (panelSettings == null)
            {
                return;
            }

            // ランタイムでは PanelSettings が既にテーマを保持している前提で一切操作しない。
#if UNITY_EDITOR
            const string PackageThemePath = "Packages/com.papacoder.funity/Runtime/UI/UnityDefaultRuntimeTheme.tss";
            const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

            // 1. パッケージ側に常備したテーマを最優先で割り当てる。
            var packageTheme = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageThemePath);
            if (packageTheme != null)
            {
                EditorAssignTheme(panelSettings, packageTheme);
                return;
            }

            // 2. Resources 配下の FUnityPanelThemeSettings を経由してフォールバックを試す。
            var fallbackTheme = LoadFallbackTheme();
            if (fallbackTheme != null)
            {
                EditorAssignTheme(panelSettings, fallbackTheme);
                return;
            }

            // 3. 互換目的で、ユーザープロジェクト直下のレガシー配置も検索する。
            var legacyTheme = AssetDatabase.LoadAssetAtPath<StyleSheet>(LegacyThemePath);
            if (legacyTheme != null)
            {
                EditorAssignTheme(panelSettings, legacyTheme);
                return;
            }

            var dark = Resources.Load<ThemeStyleSheet>("UIThemes/DefaultCommonDark");
            var light = Resources.Load<ThemeStyleSheet>("UIThemes/DefaultCommonLight");

            var picked = (StyleSheet)(dark != null ? dark : light);
            if (picked != null)
            {
                EditorAssignTheme(panelSettings, picked);
            }
            else
            {
                Debug.LogWarning("[FUnity.UI] Theme Style Sheet が見つかりません。UI 表示が崩れる可能性があります。");
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// PanelSettings のシリアライズプロパティへテーマを登録し、Unity バージョン差異を吸収する。
        /// </summary>
        /// <param name="panelSettings">割り当て対象となる PanelSettings。null は無視する。</param>
        /// <param name="themeOrThemeListElement">登録したい StyleSheet もしくは ThemeStyleSheet。</param>
        /// <returns>割り当てが実際に行われた場合は true。</returns>
        private static bool EditorAssignTheme(PanelSettings panelSettings, StyleSheet themeOrThemeListElement)
        {
            if (panelSettings == null || themeOrThemeListElement == null)
            {
                return false;
            }

            var serializedPanelSettings = new SerializedObject(panelSettings);
            var themeProperty =
                  serializedPanelSettings.FindProperty("themeStyleSheet")
               ?? serializedPanelSettings.FindProperty("m_ThemeStyleSheet")
               ?? serializedPanelSettings.FindProperty("themeUss")
               ?? serializedPanelSettings.FindProperty("themeStyleSheets");

            if (themeProperty == null)
            {
                return false;
            }

            var assigned = false;
            if (themeProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (themeProperty.objectReferenceValue != themeOrThemeListElement)
                {
                    themeProperty.objectReferenceValue = themeOrThemeListElement;
                    assigned = true;
                }
            }
            else if (themeProperty.isArray)
            {
                var exists = false;
                for (var i = 0; i < themeProperty.arraySize; i++)
                {
                    var element = themeProperty.GetArrayElementAtIndex(i);
                    if (element != null && element.objectReferenceValue == themeOrThemeListElement)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var insertIndex = themeProperty.arraySize;
                    themeProperty.InsertArrayElementAtIndex(insertIndex);
                    var newElement = themeProperty.GetArrayElementAtIndex(insertIndex);
                    if (newElement != null)
                    {
                        newElement.objectReferenceValue = themeOrThemeListElement;
                    }
                    assigned = true;
                }
            }

            if (assigned)
            {
                serializedPanelSettings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(panelSettings);
                AssetDatabase.SaveAssets();
            }

            return assigned;
        }
#endif

        /// <summary>
        /// Resources からフォールバック用テーマ設定を読み込み、StyleSheet をキャッシュして返す。
        /// Runtime/Resources 配下に常備されるパッケージ同梱アセットを前提とし、ユーザープロジェクトでの生成を必要としない。
        /// </summary>
        private static StyleSheet LoadFallbackTheme()
        {
            if (s_cachedFallbackTheme != null)
            {
                return s_cachedFallbackTheme;
            }

            var themeSettings = Resources.Load<FUnityPanelThemeSettings>(ThemeSettingsResourceName);
            if (themeSettings == null)
            {
                return null;
            }

            s_cachedFallbackTheme = themeSettings.Theme;
            return s_cachedFallbackTheme;
        }

        /// <summary>
        /// PanelSettings にテーマが設定できない場合のフォールバックとして、UIDocument の rootVisualElement に StyleSheet を追加する。
        /// </summary>
        private static void ApplyFallbackTheme(UIDocument uiDocument, StyleSheet fallbackTheme)
        {
            if (uiDocument == null || fallbackTheme == null)
            {
                return;
            }

            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var styleSheetSet = root.styleSheets;
            if (ContainsStyleSheet(styleSheetSet, fallbackTheme))
            {
                return;
            }

            styleSheetSet.Add(fallbackTheme);
        }

        /// <summary>
        /// 指定の StyleSheet が既に VisualElement に適用されているか確認する。
        /// </summary>
        private static bool HasStyleSheet(VisualElement visualElement, StyleSheet styleSheet)
        {
            if (visualElement == null || styleSheet == null)
            {
                return false;
            }

            return ContainsStyleSheet(visualElement.styleSheets, styleSheet);
        }

        /// <summary>
        /// <see cref="VisualElementStyleSheetSet"/> 内に対象の StyleSheet が含まれるかを確認する。
        /// </summary>
        /// <param name="set">検索対象の StyleSheet セット。</param>
        /// <param name="target">存在確認したい StyleSheet。</param>
        /// <returns>対象が含まれていれば true。</returns>
        private static bool ContainsStyleSheet(VisualElementStyleSheetSet set, StyleSheet target)
        {
            if (target == null)
            {
                return false;
            }

            for (int i = 0, count = set.count; i < count; i++)
            {
                if (set[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <see cref="VisualElementStyleSheetSet"/> から対象の StyleSheet を削除する。
        /// </summary>
        /// <param name="set">削除対象を含む StyleSheet セット。</param>
        /// <param name="target">削除したい StyleSheet。</param>
        private static void RemoveStyleSheet(VisualElementStyleSheetSet set, StyleSheet target)
        {
            if (target == null)
            {
                return;
            }

            set.Remove(target);
        }

        /// <summary>
        /// <see cref="VisualElementStyleSheetSet"/> に登録されている StyleSheet を全てクリアする。
        /// </summary>
        /// <param name="set">クリア対象の StyleSheet セット。</param>
        private static void ClearStyleSheets(VisualElementStyleSheetSet set)
        {
            set.Clear();
        }
    }
}
