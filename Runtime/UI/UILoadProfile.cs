using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    [CreateAssetMenu(menuName = "FUnity/UI Load Profile", fileName = "UIProfile_Default")]
    public class UILoadProfile : ScriptableObject
    {
        [Header("Layout")]
        public VisualTreeAsset uxml;
        public List<StyleSheet> uss = new();
        public PanelSettings panelSettings;

        [Header("Dynamic Elements")]
        public bool spawnFooni = true;
        public bool spawnBlocks = false;

        [Header("Block Palette")]
        [SerializeField]
        private List<string> m_BlockPalette = new();

        /// <summary>
        /// Gets the list of block labels that will be spawned when <see cref="spawnBlocks"/> is enabled.
        /// </summary>
        public IReadOnlyList<string> BlockPalette => m_BlockPalette;
    }
}
