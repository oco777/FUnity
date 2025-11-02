using UnityEngine;
using UnityEngine.Serialization;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// ステージ背景などの静的設定を保持する Model レイヤーの ScriptableObject。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnity.Runtime.Core.FUnityManager"/>（Presenter 経由で View へ適用）
    /// 想定ライフサイクル: プロジェクト設定時に作成し、ランタイムでは読み取り専用。
    /// 背景画像は Resources/Art 等に配置し、UI Toolkit の `backgroundImage` プロパティへ <see cref="UnityEngine.UIElements.StyleBackground"/>
    ///     の Texture2D コンストラクタで割り当てる。
    /// </remarks>
    [CreateAssetMenu(menuName = "FUnity/Stage Data", fileName = "FUnityStageData")]
    public sealed class FUnityStageData : ScriptableObject
    {
        /// <summary>UI 上で表示するステージ名。</summary>
        [SerializeField] private string m_stageName = "Default Stage";

        /// <summary>ステージ横幅の既定値（px）。Scratch 互換の論理サイズを指す。</summary>
        public const int DefaultStageWidth = 480;

        /// <summary>ステージ縦幅の既定値（px）。Scratch デフォルト値と一致させる。</summary>
        public const int DefaultStageHeight = 360;

        /// <summary>背景の基調色。背景画像が無い場合は単色で塗りつぶす。</summary>
        [SerializeField] private Color m_backgroundColor = Color.black;
        // TODO: BGM, 背景画像など将来拡張

        [Header("Background")]
        /// <summary>
        /// 背景に使用するテクスチャ。`Runtime/Resources/Backgrounds` など Resources 参照可能なパスへ配置する想定。
        /// </summary>
        [SerializeField] private Texture2D m_backgroundImage;

        /// <summary>
        /// 背景スケール既定値を示す定数。Presenter からも共有するため public とする。
        /// </summary>
        public const string BackgroundScaleContain = "contain";

        /// <summary>
        /// 背景スケール "cover" を示す定数。USS 側のクラス判定にも利用する。
        /// </summary>
        public const string BackgroundScaleCover = "cover";

        /// <summary>背景のスケール。"contain" または "cover" のみ受け付ける。</summary>
        [Tooltip("背景のスケール。\"contain\" または \"cover\" のみ")]
        [FormerlySerializedAs("m_backgroundScaleMode")]
        [FormerlySerializedAs("m_backgroundScale")]
        [SerializeField]
        private string m_backgroundScale = BackgroundScaleContain;

        /// <summary>ステージ横幅（px）。0 以下の値は <see cref="DefaultStageWidth"/> へ補正する。</summary>
        [SerializeField] private int m_stageWidth = DefaultStageWidth;

        /// <summary>ステージ縦幅（px）。0 以下の値は <see cref="DefaultStageHeight"/> へ補正する。</summary>
        [SerializeField] private int m_stageHeight = DefaultStageHeight;

        /// <summary>UI などに表示するステージ名。</summary>
        public string StageName => m_stageName;

        /// <summary>背景色（RGBA）。</summary>
        public Color BackgroundColor => m_backgroundColor;

        /// <summary>背景画像。null の場合は背景色のみ適用する。</summary>
        public Texture2D BackgroundImage => m_backgroundImage;

        /// <summary>背景画像のスケール種別（"contain" / "cover"）。</summary>
        public string BackgroundScale => NormalizeBackgroundScale(m_backgroundScale);

        /// <summary>ステージ横幅（px）。1px 未満の値が入っている場合は既定値へ丸める。</summary>
        public int StageWidth => m_stageWidth > 0 ? m_stageWidth : DefaultStageWidth;

        /// <summary>ステージ縦幅（px）。1px 未満の値が入っている場合は既定値へ丸める。</summary>
        public int StageHeight => m_stageHeight > 0 ? m_stageHeight : DefaultStageHeight;

        /// <summary>ステージサイズをまとめて取得するユーティリティ。UI の等倍スケール計算などで利用する。</summary>
        public Vector2Int StageSize => new Vector2Int(StageWidth, StageHeight);

        /// <summary>
        /// シリアライズされた背景スケール値を正規化し、許可された語のみ保持する。
        /// </summary>
        private void OnValidate()
        {
            m_backgroundScale = NormalizeBackgroundScale(m_backgroundScale);

            if (m_stageWidth <= 0)
            {
                m_stageWidth = DefaultStageWidth;
            }

            if (m_stageHeight <= 0)
            {
                m_stageHeight = DefaultStageHeight;
            }
        }

        /// <summary>
        /// 背景スケール文字列を正規化し、"cover" 以外は "contain" へ丸める。
        /// </summary>
        /// <param name="raw">検証対象の文字列。</param>
        /// <returns>"cover" を除き常に "contain" を返す安全な文字列。</returns>
        internal static string NormalizeBackgroundScale(string raw)
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
