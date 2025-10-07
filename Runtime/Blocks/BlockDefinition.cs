using UnityEngine;

namespace FUnity.Blocks
{
    /// <summary>
    /// Describes a block type available in the FUnity workspace.
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Block Definition", fileName = "BlockDefinition")]
    public sealed class BlockDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName = "New Block";

        [TextArea]
        [SerializeField]
        private string description = "Describe what this block does.";

        public string DisplayName => displayName;
        public string Description => description;
    }
}
