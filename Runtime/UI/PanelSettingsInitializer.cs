// Updated: 2025-02-14
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
            // UI Builder 旧テーマ（.tss）が残っている場合のみ Resources 側へコピーする。
            const string LegacyThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";
            var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(LegacyThemePath);
            if (theme != null && panelSettings != null)
            {
                var so = new SerializedObject(panelSettings);
                /* Unity バージョン差異対策。themeStyleSheet → m_ThemeStyleSheet → themeUss の順で探索し、
                   最終手段として再度 themeStyleSheet を参照する（重複回避） */
                var themeProp =
                      so.FindProperty("themeStyleSheet")
                   ?? so.FindProperty("m_ThemeStyleSheet")
                   ?? so.FindProperty("themeUss")
                   ?? so.FindProperty("themeStyleSheet");

                bool assigned = false;
                if (themeProp != null)
                {
                    if (themeProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (themeProp.objectReferenceValue != theme)
                        {
                            themeProp.objectReferenceValue = theme;
                            assigned = true;
                        }
                    }
                    else if (themeProp.isArray)
                    {
                        bool exists = false;
                        for (int i = 0; i < themeProp.arraySize; i++)
                        {
                            var e = themeProp.GetArrayElementAtIndex(i);
                            if (e != null && e.objectReferenceValue == theme)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            int idx = themeProp.arraySize;
                            themeProp.InsertArrayElementAtIndex(idx);
                            var newElement = themeProp.GetArrayElementAtIndex(idx);
                            if (newElement != null)
                            {
                                newElement.objectReferenceValue = theme;
                            }
                            assigned = true;
                        }
                    }
                }

                if (assigned)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(panelSettings);
                    AssetDatabase.SaveAssets();
                }
            }
#endif

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
    }
}
