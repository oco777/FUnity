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
        private string displayName = "Sprite";

        [SerializeField]
        private Sprite? sprite;

        [SerializeField]
        private Vector2 size = new Vector2(128f, 128f);

        [SerializeField]
        private Vector2 initialPosition = new Vector2(120f, 120f);

        private VisualElement? _visualElement;
        private Vector2 _position;
        private StageRuntime? _runtime;

        /// <summary>
        /// Friendly name shown in the UI and Visual Scripting inspector.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>
        /// Current pixel position inside the stage (origin = top-left).
        /// </summary>
        public Vector2 Position => _position;

        internal StyleBackground CurrentBackground =>
            sprite != null
                ? new StyleBackground(sprite)
                : new StyleBackground { keyword = StyleKeyword.Null };

        internal bool HasSprite => sprite != null;

        private void Awake()
        {
            _position = initialPosition;
        }

        private void OnEnable()
        {
            _runtime = StageRuntime.Instance;
            if (_runtime != null)
            {
                _runtime.RegisterActor(this);
            }
        }

        private void OnDisable()
        {
            if (_runtime != null)
            {
                _runtime.UnregisterActor(this);
            }

            _runtime = null;
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

            displayName = definition.DisplayName;
            sprite = definition.Sprite;
            size = definition.Size;
            initialPosition = definition.InitialPosition;
        }

        /// <summary>
        /// Set a new sprite texture at runtime.
        /// </summary>
        public void SetSprite(Sprite? newSprite)
        {
            sprite = newSprite;
            if (_visualElement != null)
            {
                if (newSprite != null)
                {
                    _visualElement.style.backgroundImage = new StyleBackground(newSprite);
                    _visualElement.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                    _visualElement.style.backgroundColor = new StyleColor(Color.clear);
                }
                else
                {
                    _visualElement.style.backgroundImage = new StyleBackground { keyword = StyleKeyword.Null };
                    _visualElement.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
                }
            }
        }

        /// <summary>
        /// Directly move to a specific pixel coordinate.
        /// </summary>
        public void MoveTo(Vector2 position)
        {
            _position = position;
            if (_visualElement != null)
            {
                _visualElement.style.left = position.x;
                _visualElement.style.top = position.y;
            }
        }

        /// <summary>
        /// Move by the given delta (in pixels).
        /// </summary>
        public void MoveBy(Vector2 delta) => MoveTo(_position + delta);

        /// <summary>
        /// Resize the sprite visual.
        /// </summary>
        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            if (_visualElement != null)
            {
                _visualElement.style.width = newSize.x;
                _visualElement.style.height = newSize.y;
            }
        }

        internal void AttachToStage(VisualElement stageRoot)
        {
            if (stageRoot == null)
            {
                return;
            }

            _visualElement ??= CreateVisualElement();
            if (_visualElement.parent != stageRoot)
            {
                _visualElement.RemoveFromHierarchy();
                stageRoot.Add(_visualElement);
            }

            MoveTo(_position);
        }

        internal void DetachFromStage()
        {
            if (_visualElement != null)
            {
                _visualElement.RemoveFromHierarchy();
            }
        }

        private VisualElement CreateVisualElement()
        {
            var element = new VisualElement { name = DisplayName };
            element.style.position = Position.Absolute;
            element.style.width = size.x;
            element.style.height = size.y;
            element.style.left = _position.x;
            element.style.top = _position.y;
            element.style.borderRadius = 8f;
            element.style.overflow = Overflow.Hidden;
            element.style.borderBottomWidth = 1f;
            element.style.borderTopWidth = 1f;
            element.style.borderLeftWidth = 1f;
            element.style.borderRightWidth = 1f;
            element.style.borderBottomColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));
            element.style.borderTopColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));
            element.style.borderLeftColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));
            element.style.borderRightColor = new StyleColor(new Color(0.2f, 0.2f, 0.24f));

            if (sprite != null)
            {
                element.style.backgroundImage = new StyleBackground(sprite);
                element.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
            }
            else
            {
                element.style.backgroundColor = new StyleColor(new Color(0.9f, 0.4f, 0.4f));
            }

            var nameLabel = new Label(DisplayName);
            nameLabel.style.position = Position.Absolute;
            nameLabel.style.bottom = 4f;
            nameLabel.style.left = 4f;
            nameLabel.style.color = new StyleColor(Color.white);
            nameLabel.style.fontSize = 12f;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.unityTextOutlineColor = new StyleColor(Color.black);
            nameLabel.style.unityTextOutlineWidth = 0.2f;
            element.Add(nameLabel);

            _visualElement = element;
            return element;
        }
    }
}
