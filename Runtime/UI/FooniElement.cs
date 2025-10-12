using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// Displays the Fooni character using a UI Toolkit custom element.
    /// </summary>
    [UxmlElement]
    public partial class FooniElement : VisualElement
    {
        // NOTE:
        // FooniElement は見た目専用の UI 要素です。
        // アニメーションや時間変化は FooniController に集約しました（Visual Scripting から制御）。
        private const string VisualTreeResourcePath = "UI/FooniElement";
        private const string StyleResourcePath = "UI/FooniElement";
        private const string FooniTexturePath = "Characters/Fooni";
        private const string FooniImageName = "fooni-image";
        private Image _image;

        // Initializes the element layout and animation.
        public FooniElement()
        {
            InitializeLayout();
            CacheImageElement();
            LoadCharacter();
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
    }
}
