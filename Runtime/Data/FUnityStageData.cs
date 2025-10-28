// Updated: 2025-03-18
using System;
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

        /// <summary>背景スケール既定値を示す定数。</summary>
        private const string BackgroundScaleContain = "contain";

        /// <summary>背景スケール "cover" を示す定数。</summary>
        private const string BackgroundScaleCover = "cover";

        /// <summary>背景のスケール。"contain" または "cover" のみ受け付ける。</summary>
        [Tooltip("背景のスケール。\"contain\" または \"cover\" のみ")] 
        [FormerlySerializedAs("m_backgroundScaleMode")]
        [FormerlySerializedAs("m_backgroundScale")]
        [SerializeField] private string m_backgroundScale = BackgroundScaleContain;

        /// <summary>UI などに表示するステージ名。</summary>
        public string StageName => m_stageName;

        /// <summary>背景色（RGBA）。</summary>
        public Color BackgroundColor => m_backgroundColor;

        /// <summary>背景画像。null の場合は背景色のみ適用する。</summary>
        public Texture2D BackgroundImage => m_backgroundImage;

        /// <summary>背景画像のスケール種別（"contain" / "cover"）。</summary>
        public string BackgroundScale => NormalizeBackgroundScale(m_backgroundScale);

        /// <summary>
        /// シリアライズされた背景スケール値を正規化し、許可された語のみ保持する。
        /// </summary>
        private void OnValidate()
        {
            m_backgroundScale = NormalizeBackgroundScale(m_backgroundScale);
        }

        /// <summary>
        /// 背景スケール文字列を正規化し、"cover" 以外は "contain" へ丸める。
        /// </summary>
        /// <param name="raw">検証対象の文字列。</param>
        /// <returns>"cover" を除き常に "contain" を返す安全な文字列。</returns>
        private static string NormalizeBackgroundScale(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return BackgroundScaleContain;
            }

            var normalized = raw.Trim().ToLowerInvariant();
            return normalized == BackgroundScaleCover ? BackgroundScaleCover : BackgroundScaleContain;
        }
    }
}
