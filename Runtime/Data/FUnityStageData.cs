using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// ステージ背景 1 枚分の表示情報を保持するデータクラス。
    /// </summary>
    [System.Serializable]
    public class StageBackgroundInfo
    {
        /// <summary>
        /// 背景の番号（1, 2, 3,...）。ブロックなどから指定する ID として使用します。
        /// </summary>
        [SerializeField]
        private int m_Id;

        /// <summary>
        /// UI 上に表示する背景の名前。
        /// </summary>
        [SerializeField]
        private string m_DisplayName;

        /// <summary>
        /// 表示に使用する背景 Sprite。
        /// </summary>
        [SerializeField]
        private Sprite m_Sprite;

        /// <summary>
        /// デフォルトコンストラクタ。Unity のシリアライズで利用する。
        /// </summary>
        public StageBackgroundInfo()
        {
        }

        /// <summary>
        /// 背景情報を初期化するコンストラクタ。背景番号・表示名・Sprite を一括設定する。
        /// </summary>
        /// <param name="id">背景の番号。</param>
        /// <param name="displayName">インスペクターや UI で表示する背景名。</param>
        /// <param name="sprite">背景として使用する Sprite。</param>
        public StageBackgroundInfo(int id, string displayName, Sprite sprite)
        {
            m_Id = id;
            m_DisplayName = displayName;
            m_Sprite = sprite;
        }

        /// <summary>背景の番号。</summary>
        public int Id => m_Id;

        /// <summary>背景の名前。</summary>
        public string DisplayName => m_DisplayName;

        /// <summary>表示に使用する背景 Sprite。</summary>
        public Sprite Sprite => m_Sprite;
    }

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
        /// 登録されているステージ背景候補の一覧。
        /// </summary>
        [SerializeField]
        private List<StageBackgroundInfo> m_backgrounds = new List<StageBackgroundInfo>();

        /// <summary>デフォルトで表示する背景のインデックス。</summary>
        [SerializeField]
        private int m_defaultBackgroundIndex = 0;

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

        /// <summary>
        /// 背景画像。デフォルト背景が設定されていればその Sprite の texture を返し、
        /// 登録が何もない場合は null を返します。
        /// </summary>
        public Texture2D BackgroundImage
        {
            get
            {
                var defaultBackground = GetDefaultBackground();
                if (defaultBackground?.Sprite != null)
                {
                    return defaultBackground.Sprite.texture;
                }

                // 登録が何もない場合は null（＝背景色のみ）
                return null;
            }
        }

        /// <summary>背景画像のスケール種別（"contain" / "cover"）。</summary>
        public string BackgroundScale => NormalizeBackgroundScale(m_backgroundScale);

        /// <summary>登録されているステージ背景候補の一覧。null の場合でも空リストを返す。</summary>
        public IReadOnlyList<StageBackgroundInfo> Backgrounds => m_backgrounds ??= new List<StageBackgroundInfo>();

        /// <summary>デフォルトで表示する背景のインデックス。</summary>
        public int DefaultBackgroundIndex => m_defaultBackgroundIndex;

        /// <summary>ステージ横幅（px）。1px 未満の値が入っている場合は既定値へ丸める。</summary>
        public int StageWidth => m_stageWidth > 0 ? m_stageWidth : DefaultStageWidth;

        /// <summary>ステージ縦幅（px）。1px 未満の値が入っている場合は既定値へ丸める。</summary>
        public int StageHeight => m_stageHeight > 0 ? m_stageHeight : DefaultStageHeight;

        /// <summary>ステージサイズをまとめて取得するユーティリティ。UI の等倍スケール計算などで利用する。</summary>
        public Vector2Int StageSize => new Vector2Int(StageWidth, StageHeight);

        /// <summary>
        /// デフォルト背景情報を取得する。インデックスが範囲外の場合は最初の要素を返す。
        /// </summary>
        /// <returns>デフォルト設定された背景情報。背景が未登録の場合は null。</returns>
        public StageBackgroundInfo GetDefaultBackground()
        {
            if (m_backgrounds == null || m_backgrounds.Count == 0)
            {
                return null;
            }

            if (m_defaultBackgroundIndex < 0 || m_defaultBackgroundIndex >= m_backgrounds.Count)
            {
                return m_backgrounds[0];
            }

            return m_backgrounds[m_defaultBackgroundIndex];
        }

        /// <summary>
        /// 背景番号から対応する背景情報を検索します。
        /// 指定された番号と一致する StageBackgroundInfo が存在しない場合は null を返します。
        /// </summary>
        /// <param name="id">取得したい背景の番号。</param>
        /// <returns>一致する背景情報。見つからない場合は null。</returns>
        public StageBackgroundInfo GetBackgroundByNumber(int id)
        {
            if (m_backgrounds == null)
            {
                return null;
            }

            return m_backgrounds.Find(background => background != null && background.Id == id);
        }

        /// <summary>
        /// 旧 API。文字列の ID から背景を取得します。
        /// 可能であれば整数に変換して <see cref="GetBackgroundByNumber"/> に委譲します。
        /// </summary>
        /// <param name="id">検索対象の背景 ID。null または空文字の場合は null を返す。</param>
        /// <returns>一致する背景情報。見つからない場合は null。</returns>
        public StageBackgroundInfo GetBackgroundById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (int.TryParse(id, out var number))
            {
                return GetBackgroundByNumber(number);
            }

            return null;
        }

        /// <summary>
        /// インデックス指定で背景情報を取得する。範囲外の場合は null を返す。
        /// </summary>
        /// <param name="index">取得したい背景のインデックス。</param>
        /// <returns>インデックスに対応する背景情報。範囲外は null。</returns>
        public StageBackgroundInfo GetBackgroundAt(int index)
        {
            if (m_backgrounds == null || index < 0 || index >= m_backgrounds.Count)
            {
                return null;
            }

            return m_backgrounds[index];
        }

        /// <summary>
        /// シリアライズされた背景スケール値を正規化し、許可された語のみ保持する。
        /// </summary>
        private void OnValidate()
        {
            m_backgroundScale = NormalizeBackgroundScale(m_backgroundScale);

            EnsureBackgroundListAllocated();
            ClampDefaultBackgroundIndex();

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

        /// <summary>
        /// 背景リストが null になっている場合に空リストへ初期化する。
        /// </summary>
        private void EnsureBackgroundListAllocated()
        {
            if (m_backgrounds == null)
            {
                m_backgrounds = new List<StageBackgroundInfo>();
            }
        }

        /// <summary>
        /// 背景リストの範囲に収まるようデフォルトインデックスを丸める。
        /// </summary>
        private void ClampDefaultBackgroundIndex()
        {
            if (m_backgrounds.Count == 0)
            {
                m_defaultBackgroundIndex = 0;
                return;
            }

            m_defaultBackgroundIndex = Mathf.Clamp(m_defaultBackgroundIndex, 0, m_backgrounds.Count - 1);
        }
    }
}
