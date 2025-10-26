using System.Collections.Generic;
using UnityEngine;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Authoring
{
    /// <summary>
    /// FUnity の制作モードに応じた推奨設定を保持する ScriptableObject です。
    /// ステージ解像度やピクセル密度、利用可能な機能フラグを集約し、Editor から読み出して環境構築を補助します。
    /// </summary>
    [CreateAssetMenu(fileName = "FUnityModeConfig", menuName = "FUnity/Authoring/Mode Config")]
    public sealed class FUnityModeConfig : ScriptableObject
    {
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
    }
}
