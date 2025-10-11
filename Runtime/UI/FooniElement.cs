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
        private const string VisualTreeResourcePath = "UI/FooniElement";
        private const string StyleResourcePath = "UI/FooniElement";
        private const string FooniTexturePath = "Characters/Fooni";
        private const string FooniImageClass = "fooni-image";

        public FooniElement()
        {
            InitializeLayout();
            ApplyTexture();
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

        private void ApplyTexture()
        {
            var texture = Resources.Load<Texture2D>(FooniTexturePath);
            if (texture == null)
            {
                Debug.LogWarning($"FooniElement: Unable to load texture at Resources/{FooniTexturePath}.");
                return;
            }

            var image = this.Q<Image>(className: FooniImageClass);
            if (image == null)
            {
                Debug.LogWarning("FooniElement: Unable to find Image element with class 'fooni-image'.");
                return;
            }

            image.image = texture;
        }
    }
}
