using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// Displays the Fooni character using a UI Toolkit custom element.
    /// </summary>
    [UxmlElement]
    public partial class FooniElement : VisualElement
    {
        private const string VisualTreeResourcePath = "UI/FooniElement";
        private const string StyleResourcePath = "UI/FooniElement";
        private const string FooniTexturePath = "Characters/Fooni";
        private const string FooniImageName = "fooni-image";
        private Image _image;
        private IValueAnimation _floatAnimation;

        // Initializes the element layout and animation.
        public FooniElement()
        {
            InitializeLayout();
            CacheImageElement();
            LoadCharacter();
            StartFloatingAnimation();
        }

        // Loads the UXML and USS resources for the visual layout.
        private void InitializeLayout()
        {
            var visualTree = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (visualTree != null)
            {
                visualTree.CloneTree(this);
            }
            else
            {
                Debug.LogWarning($"FooniElement: Unable to load VisualTreeAsset at Resources/{VisualTreeResourcePath}.");
            }

            var styleSheet = Resources.Load<StyleSheet>(StyleResourcePath);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning($"FooniElement: Unable to load StyleSheet at Resources/{StyleResourcePath}.");
            }
        }

        // Caches the Fooni image element for later updates.
        private void CacheImageElement()
        {
            _image = this.Q<Image>(name: FooniImageName) ?? this.Q<Image>(className: FooniImageName);
            if (_image == null)
            {
                Debug.LogWarning("FooniElement: Unable to find Image element with name or class 'fooni-image'.");
            }
        }

        // Loads the character texture from the Resources folder.
        private void LoadCharacter()
        {
            if (_image == null)
            {
                return;
            }

            var texture = Resources.Load<Texture2D>(FooniTexturePath);
            if (texture == null)
            {
                Debug.LogWarning("⚠️ Fooni image not found in Resources/Characters/Fooni.png");
                return;
            }

            _image.image = texture;
        }

        // Starts the floating animation that makes Fooni gently move up and down.
        private void StartFloatingAnimation()
        {
            const int durationMs = 3000;
            const float amplitude = 10f;

            _floatAnimation?.Stop();

            _floatAnimation = this.experimental.animation.Start(0f, 1f, durationMs, (element, t) =>
            {
                float offset = Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
                element.style.translate = new Translate(0f, offset, 0f);
            });

            if (_floatAnimation == null)
            {
                return;
            }

            schedule.Execute(StartFloatingAnimation).StartingIn(durationMs);
        }
    }
}
