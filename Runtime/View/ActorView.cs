// Updated: 2025-02-14
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Input;

namespace FUnity.Runtime.View
{
    /// <summary>
    /// 俳優の見た目と座標を UI Toolkit 上に投影する View レイヤーの薄いアダプタ。
    /// Presenter が指示する座標と画像のみを反映し、状態やロジックは保持しない。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FooniUIBridge"/>（UI Toolkit 要素への操作ラッパー）
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> が生成した UI GameObject にアタッチされ、<see cref="FUnity.Runtime.Presenter.ActorPresenter"/>
    ///     の初期化時に <see cref="Configure(FooniUIBridge, VisualElement)"/> で結線される。
    /// ビューは常に Presenter からの一方向更新のみを許容し、ユーザー入力は受け付けない。
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ActorView : MonoBehaviour, IActorView
    {
        /// <summary>
        /// UI Toolkit 要素へ座標・背景画像を反映するためのブリッジ。
        /// <see cref="Configure(FooniUIBridge, VisualElement)"/> で差し替え可能。
        /// </summary>
        [SerializeField]
        private FooniUIBridge m_Bridge;

        /// <summary>
        /// Presenter から指定された UI Toolkit 要素。`Configure` 呼び出し後に <see cref="SetPortrait"/> が利用する。
        /// </summary>
        private VisualElement m_BoundElement;

        /// <summary>
        /// コンポーネントがリセットされた際に既存の <see cref="FooniUIBridge"/> を再取得する。
        /// </summary>
        private void Reset()
        {
            if (m_Bridge == null)
            {
                m_Bridge = GetComponent<FooniUIBridge>();
            }
        }

        /// <summary>
        /// Presenter から受け取ったブリッジと UI 要素を紐付け、以降の描画命令を受け付ける。
        /// </summary>
        /// <param name="bridge">座標更新を委譲する <see cref="FooniUIBridge"/>。null 時は自身に付随するブリッジを利用する。</param>
        /// <param name="element">UI Toolkit 側で描画対象となる要素。</param>
        /// <example>
        /// <code>
        /// var view = gameObject.AddComponent<ActorView>();
        /// view.Configure(existingBridge, actorElement);
        /// </code>
        /// </example>
        public void Configure(FooniUIBridge bridge, VisualElement element)
        {
            if (bridge != null)
            {
                m_Bridge = bridge;
            }
            else if (m_Bridge == null)
            {
                m_Bridge = GetComponent<FooniUIBridge>();
                if (m_Bridge == null)
                {
                    m_Bridge = gameObject.AddComponent<FooniUIBridge>();
                }
            }

            m_BoundElement = element;
            if (m_Bridge != null && element != null)
            {
                m_Bridge.BindElement(element);
            }
        }

        /// <summary>
        /// Presenter から通知された座標を UI Toolkit 要素へ適用する。
        /// </summary>
        /// <param name="pos">左上原点基準（px）のワールド内 UI 座標。</param>
        /// <remarks>
        /// <see cref="FooniUIBridge"/> 経由で `style.left/top` を更新する。要素未バインド時は静かに無視する。
        /// </remarks>
        /// <example>
        /// <code>
        /// m_ActorView.SetPosition(model.Position);
        /// </code>
        /// </example>
        public void SetPosition(Vector2 pos)
        {
            if (m_Bridge == null)
            {
                return;
            }

            if (!m_Bridge.HasBoundElement && !m_Bridge.TryBind())
            {
                return;
            }

            m_Bridge.SetPosition(pos);
        }

        /// <summary>
        /// 俳優のポートレート画像を UI Toolkit の `portrait` 要素へ設定する。
        /// </summary>
        /// <param name="sprite">`Sprite.Create` 等で生成済みのスプライト。</param>
        /// <remarks>
        /// `portrait` 要素には UI Toolkit の仕様上 <see cref="StyleBackground"/> を `new StyleBackground(Texture2D)` で渡す必要がある。
        /// `StyleBackground.none` は使用できないため、null 時は既存の背景を保持する。
        /// </remarks>
        /// <example>
        /// <code>
        /// var portraitSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
        /// m_ActorView.SetPortrait(portraitSprite);
        /// </code>
        /// </example>
        public void SetPortrait(Sprite sprite)
        {
            if (sprite == null || m_BoundElement == null)
            {
                return;
            }

            var portrait = m_BoundElement.Q<VisualElement>("portrait") ?? m_BoundElement.Q<VisualElement>(className: "portrait");
            if (portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(sprite);
            }
        }
    }
}
