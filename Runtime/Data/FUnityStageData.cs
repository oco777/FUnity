// Updated: 2025-02-14
using UnityEngine;
using UnityEngine.Serialization;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// ステージ背景などの静的設定を保持する Model レイヤーの ScriptableObject。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnity.Core.FUnityManager"/>（Presenter 経由で View へ適用）
    /// 想定ライフサイクル: プロジェクト設定時に作成し、ランタイムでは読み取り専用。
    /// 背景画像は Resources/Art 等に配置し、UI Toolkit の `backgroundImage` プロパティへ <see cref="UnityEngine.UIElements.StyleBackground"/>
    ///     の Texture2D コンストラクタで割り当てる。
    /// </remarks>
    [CreateAssetMenu(menuName = "FUnity/Stage Data", fileName = "FUnityStageData")]
    public sealed class FUnityStageData : ScriptableObject
    {
        /// <summary>UI 上で表示するステージ名。</summary>
        [SerializeField] private string m_stageName = "Default Stage";

        /// <summary>背景の基調色。背景画像が無い場合は単色で塗りつぶす。</summary>
        [SerializeField] private Color m_backgroundColor = Color.black;
        // TODO: BGM, 背景画像など将来拡張

        [Header("Background")]
        /// <summary>
        /// 背景に使用するテクスチャ。`Runtime/Resources/Backgrounds` など Resources 参照可能なパスへ配置する想定。
        /// </summary>
        [SerializeField] private Texture2D m_backgroundImage;

        /// <summary>背景画像の拡大率。x/y それぞれ 1.0f で原寸（100%）を意味する。</summary>
        [SerializeField] private Vector2 m_backgroundScale = Vector2.one;

        /// <summary>UI Toolkit の `unityBackgroundScaleMode` に適用するスケーリング方式。</summary>
        [FormerlySerializedAs("m_backgroundScale")]
        [SerializeField] private ScaleMode m_backgroundScaleMode = ScaleMode.ScaleAndCrop;

        /// <summary>UI などに表示するステージ名。</summary>
        public string StageName => m_stageName;

        /// <summary>背景色（RGBA）。</summary>
        public Color BackgroundColor => m_backgroundColor;

        /// <summary>背景画像。null の場合は背景色のみ適用する。</summary>
        public Texture2D BackgroundImage => m_backgroundImage;

        /// <summary>背景画像の拡大率。</summary>
        public Vector2 BackgroundScale => m_backgroundScale;

        /// <summary>背景画像のスケーリング方式。</summary>
        public ScaleMode BackgroundScaleMode => m_backgroundScaleMode;
    }
}
