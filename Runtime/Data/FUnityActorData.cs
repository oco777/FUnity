// Updated: 2025-02-14
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// 俳優画像をステージ上に配置する際のアンカー（基準点）を定義する列挙体。
    /// </summary>
    public enum ActorAnchor
    {
        /// <summary>左上隅をアンカーとします。UI Toolkit の style.left/top に一致します。</summary>
        TopLeft,

        /// <summary>画像の中心点をアンカーとします。Scratch に近い見た目で配置されます。</summary>
        Center
    }

    /// <summary>
    /// 俳優（キャラクター）の静的設定を保持する Model レイヤーの ScriptableObject。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnity.Runtime.Core.FUnityManager"/>（初期化）、<see cref="FUnity.Runtime.Presenter.ActorPresenter"/>
    /// 想定ライフサイクル: プロジェクト設定時に作成し、ランタイムでは読み取り専用。
    /// `Size` は生成時に UI 要素へ適用する設計であり、動的変更が必要な場合は Presenter を通じて再適用する。
    /// </remarks>
    [CreateAssetMenu(menuName = "FUnity/Actor Data", fileName = "FUnityActorData")]
    public sealed class FUnityActorData : ScriptableObject
    {
        /// <summary>UI 上に表示する名前。空の場合は要素名のみ設定される。</summary>
        [SerializeField] private string m_displayName = "Fooni";

        /// <summary>
        /// 旧実装で利用していたポートレート画像。互換目的で保持し、新規アセットでは Sprite 利用を推奨する。
        /// </summary>
        [SerializeField] private Texture2D m_portrait;          // UI表示用など

        /// <summary>
        /// 俳優の見た目として最優先で表示するメイン Sprite。Sprite Editor で切り出した画像を直接指定する。
        /// </summary>
        [SerializeField] private Sprite m_portraitSprite;

        /// <summary>
        /// アニメーションや差分表示に利用する追加 Sprite 一覧。Inspector 上で任意枚数を登録できる。
        /// </summary>
        [SerializeField] private List<Sprite> m_sprites = new List<Sprite>();

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

        /// <summary>
        /// 俳優の座標がどの点を指すかを決めるアンカー設定。既定は画像中心。
        /// </summary>
        [SerializeField] private ActorAnchor m_anchor = ActorAnchor.Center;

        [Header("Visual Scripting")]
        /// <summary>俳優個別の Visual Scripting グラフ。<see cref="ScriptMachine"/> へ割り当てられる。</summary>
        [SerializeField] private ScriptGraphAsset m_scriptGraph;

        [Header("Variables")]
        /// <summary>
        /// この俳優専用の変数定義リスト。クローンを含む各インスタンスが独立した値を保持します。
        /// </summary>
        [SerializeField] private List<FUnityVariableDefinition> m_actorVariables = new List<FUnityVariableDefinition>();

        /// <summary>表示名。</summary>
        public string DisplayName => m_displayName;

        /// <summary>UI 表示に使用するポートレート（Texture2D）。Sprite 未使用時のフォールバックに限定する。</summary>
        public Texture2D Portrait => m_portrait;

        /// <summary>UI 表示に利用するメイン Sprite。null の場合は <see cref="Portrait"/> や <see cref="Sprites"/> が利用される。</summary>
        public Sprite PortraitSprite => m_portraitSprite;

        /// <summary>追加 Sprite の一覧。読み取り専用で公開し、Presenter から差分切り替えに利用する。</summary>
        public IReadOnlyList<Sprite> Sprites => m_sprites;

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

        /// <summary>この俳優に紐付くローカル変数の定義一覧。</summary>
        public List<FUnityVariableDefinition> ActorVariables => m_actorVariables;

        /// <summary>俳優座標が指すアンカー位置。</summary>
        public ActorAnchor Anchor => m_anchor;
    }
}
