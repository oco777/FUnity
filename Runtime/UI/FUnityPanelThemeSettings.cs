using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    /// <summary>
    /// PanelSettings がテーマ参照を保持できない環境向けに、フォールバック用テーマを永続化する ScriptableObject。
    /// CreateProjectData から自動生成され、PanelSettingsInitializer がランタイム適用に利用する。
    /// </summary>
    public sealed class FUnityPanelThemeSettings : ScriptableObject
    {
        /// <summary>フォールバック適用時に追加するテーマ。null の場合は何もしない。</summary>
        [SerializeField]
        [Tooltip("PanelSettings で利用できない場合に rootVisualElement へ注入するテーマ。")]
        private StyleSheet m_theme;

        /// <summary>フォールバックで適用するテーマの参照。null の場合は何も適用しない。</summary>
        public StyleSheet Theme => m_theme;
    }
}
