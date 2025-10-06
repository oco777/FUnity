using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Stage
{
    /// <summary>
    /// UI Toolkit のステージ内で描画されるスプライトを表します。
    /// 公開 API は Visual Scripting のグラフから直接扱えるよう意図的にシンプルにしています。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageSpriteActor : MonoBehaviour
    {
        [SerializeField]
        private string m_displayName = "Sprite";

        [SerializeField]
        private Sprite? m_sprite;

        [SerializeField]
        private Vector2 m_size = new Vector2(128f, 128f);

        [SerializeField]
        private Vector2 m_initialPosition = new Vector2(120f, 120f);

        private VisualElement? m_visualElement;
        private Vector2 m_position;
        private StageRuntime? m_runtime;

        /// <summary>
        /// UI と Visual Scripting インスペクターに表示されるわかりやすい名称です。
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(m_displayName) ? name : m_displayName;

        /// <summary>
        /// ステージ内での現在のピクセル座標（原点は左上）を返します。
        /// </summary>
        public Vector2 Position => m_position;

        internal StyleBackground CurrentBackground =>
            m_sprite != null
                ? new StyleBackground(m_sprite)
                : new StyleBackground { keyword = StyleKeyword.Null };

        internal bool HasSprite => m_sprite != null;

        /// <summary>
        /// キャッシュされた位置を初期化し、設定済み座標からスプライトを開始させます。
        /// </summary>
        private void Awake()
        {
            m_position = m_initialPosition;
        }

        /// <summary>
        /// コンポーネントが有効化されたときにアクティブなステージランタイムへ登録します。
        /// </summary>
        private void OnEnable()
        {
            m_runtime = StageRuntime.Instance;
            if (m_runtime != null)
            {
                m_runtime.RegisterActor(this);
            }
        }

        /// <summary>
        /// コンポーネントが無効化された際にランタイムから登録を解除し、参照の残存を防ぎます。
        /// </summary>
        private void OnDisable()
        {
            if (m_runtime != null)
            {
                m_runtime.UnregisterActor(this);
            }

            m_runtime = null;
        }

        /// <summary>
        /// 定義用 ScriptableObject の値を適用します。ランタイム生成時に便利です。
        /// </summary>
        public void ApplyDefinition(StageSpriteDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            m_displayName = definition.DisplayName;
            m_sprite = definition.Sprite;
            m_size = definition.Size;
            m_initialPosition = definition.InitialPosition;
        }

        /// <summary>
        /// ランタイム中に新しいスプライトテクスチャを設定します。
        /// </summary>
        public void SetSprite(Sprite? newSprite)
        {
            m_sprite = newSprite;
            if (m_visualElement != null)
            {
                if (newSprite != null)
                {
                    m_visualElement.style.backgroundImage = new StyleBackground(newSprite);
                    m_visualElement.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                    m_visualElement.style.backgroundColor = new StyleColor(Color.clear);
                }
                else
                {
                    m_visualElement.style.backgroundImage = new StyleBackground { keyword = StyleKeyword.Null };
                    m_visualElement.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
                }
            }
        }

        /// <summary>
        /// 指定したピクセル座標へ直接移動します。
        /// </summary>
        public void MoveTo(Vector2 position)
        {
            m_position = position;
            if (m_visualElement != null)
            {
                m_visualElement.style.left = position.x;
                m_visualElement.style.top = position.y;
            }
        }

        /// <summary>
        /// 指定されたピクセル量だけ移動します。
        /// </summary>
        public void MoveBy(Vector2 delta) => MoveTo(m_position + delta);

        /// <summary>
        /// スプライトの見た目をリサイズします。
        /// </summary>
        public void SetSize(Vector2 newSize)
        {
            m_size = newSize;
            if (m_visualElement != null)
            {
                m_visualElement.style.width = newSize.x;
                m_visualElement.style.height = newSize.y;
            }
        }

        /// <summary>
        /// アクターがビジュアル要素を保持していることを確認し、指定されたステージルートにアタッチします。
        /// </summary>
        internal void AttachToStage(VisualElement stageRoot)
        {
            if (stageRoot == null)
            {
                return;
            }

            m_visualElement ??= CreateVisualElement();
            if (m_visualElement.parent != stageRoot)
            {
                m_visualElement.RemoveFromHierarchy();
                stageRoot.Add(m_visualElement);
            }

            MoveTo(m_position);
        }

        /// <summary>
        /// コンポーネントを破棄せずにビジュアル要素を階層から取り外します。
        /// </summary>
        internal void DetachFromStage()
        {
            if (m_visualElement != null)
            {
                m_visualElement.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// スプライトアクター用の UI Toolkit ビジュアルを生成します。
        /// </summary>
        private VisualElement CreateVisualElement()
        {
            var element = new VisualElement { name = DisplayName };
            element.style.position = UnityEngine.UIElements.Position.Absolute;
            element.style.width = m_size.x;
            element.style.height = m_size.y;
            element.style.left = m_position.x;
            element.style.top = m_position.y;
            element.style.borderTopLeftRadius = 8f;
            element.style.borderTopRightRadius = 8f;
            element.style.borderBottomLeftRadius = 8f;
            element.style.borderBottomRightRadius = 8f;
            element.style.overflow = Overflow.Hidden;
            element.style.borderBottomWidth = 1f;
            element.style.borderTopWidth = 1f;
            element.style.borderLeftWidth = 1f;
            element.style.borderRightWidth = 1f;
            element.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));
            element.style.borderTopColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));
            element.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));
            element.style.borderRightColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));

            if (m_sprite != null)
            {
                element.style.backgroundImage = new StyleBackground(m_sprite);
                element.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
            }
            else
            {
                element.style.backgroundColor = new StyleColor(new Color(0.9f, 0.4f, 0.4f));
            }

            var nameLabel = new Label(DisplayName);
            nameLabel.style.position = UnityEngine.UIElements.Position.Absolute;
            nameLabel.style.bottom = 4f;
            nameLabel.style.left = 4f;
            nameLabel.style.color = new StyleColor(Color.white);
            nameLabel.style.fontSize = 12f;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.unityTextOutlineColor = new StyleColor(Color.black);
            nameLabel.style.unityTextOutlineWidth = 0.2f;
            element.Add(nameLabel);

            m_visualElement = element;
            return element;
        }
    }
}
