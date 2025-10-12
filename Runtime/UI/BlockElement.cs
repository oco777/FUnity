using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    /// <summary>
    /// Scratch-like rounded block element that can be reused from UXML.
    /// </summary>
    [UxmlElement]
    public partial class BlockElement : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BlockElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription m_TextAttribute = new()
            {
                name = "text",
                defaultValue = string.Empty
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is BlockElement block)
                {
                    block.Text = m_TextAttribute.GetValueFromBag(bag, cc);
                }
            }
        }

        public static readonly string UssClassName = "block";
        public static readonly string LabelUssClassName = "block__label";

        private readonly Label m_Label;

        public string Text
        {
            get => m_Label?.text ?? string.Empty;
            set
            {
                if (m_Label != null)
                {
                    m_Label.text = value ?? string.Empty;
                }
            }
        }

        public BlockElement()
        {
            AddToClassList(UssClassName);

            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;

            m_Label = new Label
            {
                pickingMode = PickingMode.Ignore
            };
            m_Label.AddToClassList(LabelUssClassName);
            m_Label.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_Label.style.flexGrow = 1f;

            Add(m_Label);
        }
    }
}
