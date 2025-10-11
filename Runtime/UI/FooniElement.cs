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
        private const float FloatAmplitude = 10f;
        private const int FloatDurationMs = 3000;

        private Image _image;
        private IValueAnimation<float> _floatingAnimation;

        public FooniElement()
        {
            InitializeLayout();
            CacheImageElement();
            LoadCharacter();
            StartFloatingAnimation();
        }

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

        private void CacheImageElement()
        {
            _image = this.Q<Image>(name: FooniImageName) ?? this.Q<Image>(className: FooniImageName);
            if (_image == null)
            {
                Debug.LogWarning("FooniElement: Unable to find Image element with name or class 'fooni-image'.");
            }
        }

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

        private void StartFloatingAnimation()
        {
            _floatingAnimation?.Stop();

            var animation = this.experimental.animation.Start(0f, 1f, FloatDurationMs, (element, value) =>
            {
                var offset = Mathf.Sin(value * Mathf.PI * 2f) * FloatAmplitude;
                element.style.translate = new Translate(0f, offset, 0f);
            });

            if (animation == null)
            {
                return;
            }

            animation.onAnimationCompleted += () =>
            {
                if (panel != null)
                {
                    StartFloatingAnimation();
                }
            };

            _floatingAnimation = animation;
        }
    }
}
