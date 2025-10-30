using System.Collections.Generic;
using UnityEngine;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Authoring
{
    /// <summary>
    /// 座標原点の種類を表す列挙体です。UI Toolkit との変換方式を切り替える際に利用します。
    /// </summary>
    public enum CoordinateOrigin
    {
        /// <summary>左上を原点とする座標系です。UI Toolkit 標準であり、unityroom モードの既定となります。</summary>
        TopLeft,

        /// <summary>中央を原点とし、右を +X / 上を +Y とする座標系です。Scratch 互換モードで使用します。</summary>
        Center
    }

    /// <summary>
    /// FUnity の制作モードに応じた推奨設定を保持する ScriptableObject です。
    /// ステージ解像度やピクセル密度、利用可能な機能フラグを集約し、Editor から読み出して環境構築を補助します。
    /// </summary>
    [CreateAssetMenu(fileName = "FUnityModeConfig", menuName = "FUnity/Authoring/Mode Config")]
    public sealed partial class FUnityModeConfig : ScriptableObject
    {
        /// <summary>Scratch 固定ステージ幅の既定値。</summary>
        private const int DefaultScratchStageWidth = 480;

        /// <summary>Scratch 固定ステージ高さの既定値。</summary>
        private const int DefaultScratchStageHeight = 360;

        [SerializeField]
        [Tooltip("制作モードの種類。Scratch 互換か unityroom 公開かを選びます。")]
        private FUnityAuthoringMode m_Mode = FUnityAuthoringMode.Scratch;

        [SerializeField]
        [Tooltip("ステージの基準ピクセルサイズ。左から幅、縦は高さです。")]
        private Vector2Int m_StagePixels = new Vector2Int(480, 360);

        [SerializeField]
        [Tooltip("1 ユニットあたりのピクセル数。物理と画面上の座標系を接続します。")]
        private float m_PixelsPerUnit = 100f;

        [SerializeField]
        [Tooltip("Unity の Physics2D 機能を利用可能にするかどうか。Scratch 互換を重視する場合は無効化します。")]
        private bool m_AllowUnityPhysics2D;

        [SerializeField]
        [Tooltip("カスタムシェーダーやポストエフェクトなどの高度な表現を許可するかどうか。")]
        private bool m_AllowCustomShaders;

        [SerializeField]
        [Tooltip("Scratch 形式 (.sb3) のインポート機能を有効化するかどうか。")]
        private bool m_EnableScratchImport = true;

        [SerializeField]
        [Tooltip("利用を許可する拡張機能の識別子一覧。Scratch 拡張や Unity 拡張を列挙します。")]
        private List<string> m_EnabledExtensions = new List<string>();

        [SerializeField]
        [Tooltip("Scratch モード時にステージサイズを 480x360 に固定するかどうか。")]
        private bool m_UseScratchFixedStage = true;

        [SerializeField]
        [Tooltip("Scratch 固定ステージ適用時の幅（px）。")] 
        private int m_ScratchStageWidth = DefaultScratchStageWidth;

        [SerializeField]
        [Tooltip("Scratch 固定ステージ適用時の高さ（px）。")]
        private int m_ScratchStageHeight = DefaultScratchStageHeight;

        [SerializeField]
        [Tooltip("論理座標の原点を指定します。unityroom では TopLeft、Scratch では Center を推奨します。")]
        private CoordinateOrigin m_Origin = CoordinateOrigin.TopLeft;

        /// <summary>制作モードの種類。UI やビルド設定の切り替え条件として参照します。</summary>
        public FUnityAuthoringMode Mode => m_Mode;

        /// <summary>ステージの幅・高さ（px）。</summary>
        public Vector2Int StagePixels => m_StagePixels;

        /// <summary>1 ユニットあたりのピクセル数。</summary>
        public float PixelsPerUnit => m_PixelsPerUnit;

        /// <summary>Unity の Physics2D 機能を利用できるかどうか。</summary>
        public bool AllowUnityPhysics2D => m_AllowUnityPhysics2D;

        /// <summary>カスタムシェーダーやポストエフェクトを許可するかどうか。</summary>
        public bool AllowCustomShaders => m_AllowCustomShaders;

        /// <summary>Scratch (.sb3) インポート機能を有効化するかどうか。</summary>
        public bool EnableScratchImport => m_EnableScratchImport;

        /// <summary>有効化する拡張機能の識別子一覧。読み取り専用コレクションとして公開します。</summary>
        public IReadOnlyList<string> EnabledExtensions => m_EnabledExtensions;

        /// <summary>Scratch モード時に固定ステージサイズを適用するかどうか。</summary>
        public bool UseScratchFixedStage => m_UseScratchFixedStage;

        /// <summary>Scratch 固定ステージの幅（px）。0 以下の場合は既定値を返します。</summary>
        public int ScratchStageWidth => m_ScratchStageWidth > 0 ? m_ScratchStageWidth : DefaultScratchStageWidth;

        /// <summary>Scratch 固定ステージの高さ（px）。0 以下の場合は既定値を返します。</summary>
        public int ScratchStageHeight => m_ScratchStageHeight > 0 ? m_ScratchStageHeight : DefaultScratchStageHeight;

        /// <summary>論理座標の原点設定。UI Toolkit との座標変換で参照されます。</summary>
        public CoordinateOrigin Origin => m_Origin;

        /// <summary>
        /// 他のモード設定から値を複製し、アクティブ設定を更新します。
        /// </summary>
        /// <param name="source">複製元となる設定。null の場合は処理を行いません。</param>
        public void ApplyFrom(FUnityModeConfig source)
        {
            if (source == null)
            {
                Debug.LogWarning("FUnityModeConfig: 複製元が null のため、ApplyFrom をスキップしました。");
                return;
            }

            m_Mode = source.m_Mode;
            m_StagePixels = source.m_StagePixels;
            m_PixelsPerUnit = source.m_PixelsPerUnit;
            m_AllowUnityPhysics2D = source.m_AllowUnityPhysics2D;
            m_AllowCustomShaders = source.m_AllowCustomShaders;
            m_EnableScratchImport = source.m_EnableScratchImport;
            m_UseScratchFixedStage = source.m_UseScratchFixedStage;
            m_ScratchStageWidth = source.m_ScratchStageWidth;
            m_ScratchStageHeight = source.m_ScratchStageHeight;
            m_Origin = source.m_Origin;

            if (m_EnabledExtensions == null)
            {
                m_EnabledExtensions = new List<string>();
            }

            m_EnabledExtensions.Clear();
            if (source.m_EnabledExtensions != null)
            {
                m_EnabledExtensions.AddRange(source.m_EnabledExtensions);
            }
        }

        /// <summary>
        /// Scratch モード用の固定ステージ設定に欠損があれば補完する。Editor/Runtime 双方から呼び出し可能。
        /// </summary>
        /// <returns>値を変更した場合は true。</returns>
        public bool EnsureScratchStageDefaults()
        {
            if (m_Mode != FUnityAuthoringMode.Scratch)
            {
                return false;
            }

            var changed = false;

            if (!m_UseScratchFixedStage)
            {
                m_UseScratchFixedStage = true;
                changed = true;
            }

            if (m_ScratchStageWidth <= 0)
            {
                m_ScratchStageWidth = DefaultScratchStageWidth;
                changed = true;
            }

            if (m_ScratchStageHeight <= 0)
            {
                m_ScratchStageHeight = DefaultScratchStageHeight;
                changed = true;
            }

            return changed;
        }
    }
}
