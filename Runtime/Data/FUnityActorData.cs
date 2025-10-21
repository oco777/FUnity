// Updated: 2025-02-14
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// 俳優（キャラクター）の静的設定を保持する Model レイヤーの ScriptableObject。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnity.Core.FUnityManager"/>（初期化）、<see cref="FUnity.Runtime.Presenter.ActorPresenter"/>
    /// 想定ライフサイクル: プロジェクト設定時に作成し、ランタイムでは読み取り専用。
    /// `Size` は生成時に UI 要素へ適用する設計であり、動的変更が必要な場合は Presenter を通じて再適用する。
    /// </remarks>
    [CreateAssetMenu(menuName = "FUnity/Actor Data", fileName = "FUnityActorData")]
    public sealed class FUnityActorData : ScriptableObject
    {
        /// <summary>UI 上に表示する名前。空の場合は要素名のみ設定される。</summary>
        [SerializeField] private string m_displayName = "Fooni";

        /// <summary>ポートレート画像。UI Toolkit の `StyleBackground(Texture2D)` で使用する。</summary>
        [SerializeField] private Texture2D m_portrait;          // UI表示用など

        /// <summary>初期座標（px）。Presenter が <see cref="FUnity.Runtime.Model.ActorState.Position"/> へ反映する。</summary>
        [SerializeField] private Vector2 m_initialPosition = new Vector2(0, 0);

        /// <summary>移動速度（px/s）。0 未満の場合も Presenter 側でクランプされる。</summary>
        [SerializeField] private float m_moveSpeed = 300f;

        /// <summary>初期状態で ActorPresenterAdapter の浮遊アニメーションを有効にするか。</summary>
        [SerializeField] private bool m_floatAnimation = true;   // ふわふわの初期状態

        [Header("UI Template (optional)")]
        /// <summary>俳優の表示に使用する任意の UXML テンプレート。</summary>
        [SerializeField] private VisualTreeAsset m_ElementUxml;

        /// <summary>テンプレートに追加で適用する USS。</summary>
        [SerializeField] private StyleSheet m_ElementStyle;

        [Header("Layout")]
        [Tooltip("Actor element size in pixels. Set 0 or negative to defer to USS/UXML.")]
        /// <summary>
        /// UI 要素の幅・高さ（px）。0 以下の場合は USS/UXML 側の値を尊重する。
        /// </summary>
        [SerializeField] private Vector2 m_size = new Vector2(128, 128);

        [Header("Visual Scripting")]
        /// <summary>俳優個別の Visual Scripting グラフ。<see cref="ScriptMachine"/> へ割り当てられる。</summary>
        [SerializeField] private ScriptGraphAsset m_scriptGraph;

        /// <summary>表示名。</summary>
        public string DisplayName => m_displayName;

        /// <summary>UI 表示に使用するポートレート。</summary>
        public Texture2D Portrait => m_portrait;

        /// <summary>初期座標（px）。</summary>
        public Vector2 InitialPosition => m_initialPosition;

        /// <summary>移動速度（px/s）。</summary>
        public float MoveSpeed => m_moveSpeed;

        /// <summary>浮遊アニメーションを有効にするか。</summary>
        public bool FloatAnimation => m_floatAnimation;

        /// <summary>俳優要素の UXML テンプレート。</summary>
        public VisualTreeAsset ElementUxml => m_ElementUxml;

        /// <summary>俳優要素に追加適用する USS。</summary>
        public StyleSheet ElementStyle => m_ElementStyle;

        /// <summary>UI 要素サイズ（px）。0 以下で USS/UXML に委譲。</summary>
        public Vector2 Size => m_size;

        /// <summary>俳優専用の Visual Scripting グラフ。</summary>
        public ScriptGraphAsset ScriptGraph => m_scriptGraph;
    }
}
