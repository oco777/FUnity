using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    [CreateAssetMenu(menuName = "FUnity/UI Load Profile", fileName = "UIProfile_Default")]
    public class UILoadProfile : ScriptableObject
    {
        public VisualTreeAsset uxml;
        public List<StyleSheet> uss = new();
        public PanelSettings panelSettings;

        [Header("Dynamic Elements")]
        public bool spawnFooni = true;
        public bool spawnBlocks = false;
    }
}
