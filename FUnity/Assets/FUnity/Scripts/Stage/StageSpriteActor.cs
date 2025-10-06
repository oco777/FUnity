using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Stage
{
    /// <summary>
    /// Represents a sprite that is rendered inside the UI Toolkit stage.
    /// Public API is intentionally simple so it can be consumed directly from Visual Scripting graphs.
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
        /// Friendly name shown in the UI and Visual Scripting inspector.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(m_displayName) ? name : m_displayName;

        /// <summary>
        /// Current pixel position inside the stage (origin = top-left).
        /// </summary>
        public Vector2 Position => m_position;

        internal StyleBackground CurrentBackground =>
            m_sprite != null
                ? new StyleBackground(m_sprite)
                : new StyleBackground { keyword = StyleKeyword.Null };

        internal bool HasSprite => m_sprite != null;

        /// <summary>
        /// Initializes the cached position so the sprite starts at its configured coordinates.
        /// </summary>
        private void Awake()
        {
            m_position = m_initialPosition;
        }

        /// <summary>
        /// Registers the actor with the active stage runtime when the component becomes enabled.
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
        /// Unregisters the actor from the runtime to avoid lingering references when disabled.
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
        /// Apply values from a definition ScriptableObject. Useful when spawned at runtime.
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
        /// Set a new sprite texture at runtime.
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
        /// Directly move to a specific pixel coordinate.
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
        /// Move by the given delta (in pixels).
        /// </summary>
        public void MoveBy(Vector2 delta) => MoveTo(m_position + delta);

        /// <summary>
        /// Resize the sprite visual.
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
        /// Ensures the actor has a visual element and attaches it to the provided stage root.
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
        /// Removes the visual element from the hierarchy without destroying the component.
        /// </summary>
        internal void DetachFromStage()
        {
            if (m_visualElement != null)
            {
                m_visualElement.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// Creates the UI Toolkit visual representation for the sprite actor.
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
