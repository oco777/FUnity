// Updated: 2025-02-14
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using System.IO;
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

        /// <summary>Resources 配下に常駐させるため、Assets/Resources を基準に正規パスを構築する。</summary>
        private static readonly string ResourceDirectory = Path.Combine("Assets", "Resources");
        private static readonly string AssetPath = Path.Combine(ResourceDirectory, ResourceName + ".asset");

        /// <summary>
        /// 実行前に PanelSettings をロードし、存在しない場合は生成・保存まで行う。
        /// Editor 環境では UI Builder 既定テーマを割り当て、UI Document へ一括配布する。
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
#if UNITY_EDITOR
                if (!Directory.Exists(ResourceDirectory))
                {
                    // 既存プロジェクトに Resources が無いケースを想定し、書き込み前に生成する。
                    Directory.CreateDirectory(ResourceDirectory);
                }

                AssetDatabase.CreateAsset(panelSettings, AssetPath);
                AssetDatabase.SaveAssets();
#endif
            }

            // テーマ未設定による警告を防ぐため、確実に Theme Style Sheet を割り当てる。
            EnsureThemeStyleSheet(panelSettings);

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
            const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";
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
    }
}
