using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    /// <summary>
    /// Scratch-like rounded block element that can be reused from UXML.
    /// </summary>
    public class BlockElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BlockElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _textAttribute = new()
            {
                name = "text",
                defaultValue = string.Empty
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is BlockElement block)
                {
                    block.Text = _textAttribute.GetValueFromBag(bag, cc);
                }
            }
        }

        public static readonly string UssClassName = "block";
        public static readonly string LabelUssClassName = "block__label";

        private readonly Label _label;

        public string Text
        {
            get => _label?.text ?? string.Empty;
            set
            {
                if (_label != null)
                {
                    _label.text = value ?? string.Empty;
                }
            }
        }

        public BlockElement()
        {
            AddToClassList(UssClassName);

            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;

            _label = new Label
            {
                pickingMode = PickingMode.Ignore
            };
            _label.AddToClassList(LabelUssClassName);
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.style.flexGrow = 1f;

            Add(_label);
        }
    }
}
