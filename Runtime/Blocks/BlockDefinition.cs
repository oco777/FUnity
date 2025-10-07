using UnityEngine;

namespace FUnity.Core
{
    /// <summary>
    /// Describes a Scratch-like block and how it should be rendered and executed.
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Block Definition", fileName = "BlockDefinition")]
    public class BlockDefinition : ScriptableObject
    {
        [SerializeField]
        private string m_DisplayName = "Move";

        [SerializeField, TextArea]
        private string m_Description = "Moves the sprite forward.";

        [SerializeField]
        private string m_Category = "Motion";

        [SerializeField]
        private Color m_Color = new(0.2f, 0.6f, 1.0f);

        /// <summary>
        /// Display label for the block in the UI.
        /// </summary>
        public string DisplayName => m_DisplayName;

        /// <summary>
        /// Text description shown in the inspector or tooltips.
        /// </summary>
        public string Description => m_Description;

        /// <summary>
        /// Group name used to find the matching palette.
        /// </summary>
        public string Category => m_Category;

        /// <summary>
        /// Color applied to the block body in the UI.
        /// </summary>
        public Color Color => m_Color;

        /// <summary>
        /// Allows runtime configuration when creating sample data.
        /// </summary>
        public void Configure(string displayName, string description, string category, Color color)
        {
            m_DisplayName = displayName;
            m_Description = description;
            m_Category = category;
            m_Color = color;
        }
    }
}
