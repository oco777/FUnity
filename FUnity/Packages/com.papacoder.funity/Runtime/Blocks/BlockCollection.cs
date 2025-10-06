using System.Collections.Generic;
using UnityEngine;

namespace FUnity.Core
{
    /// <summary>
    /// Holds a named list of block definitions that appear in the palette.
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Block Collection", fileName = "BlockCollection")]
    public class BlockCollection : ScriptableObject
    {
        [SerializeField]
        private string m_Category = "Motion";

        [SerializeField]
        private List<BlockDefinition> m_Definitions = new();

        /// <summary>
        /// Name of the block category displayed in the palette.
        /// </summary>
        public string Category => m_Category;

        /// <summary>
        /// Available blocks in this collection.
        /// </summary>
        public IReadOnlyList<BlockDefinition> Definitions => m_Definitions;

        /// <summary>
        /// Sets the category and replaces the definitions list.
        /// </summary>
        public void Configure(string category, IEnumerable<BlockDefinition> definitions)
        {
            m_Category = category;
            m_Definitions = new List<BlockDefinition>(definitions);
        }
    }
}
