using UnityEngine;

namespace FUnity.Blocks
{
    /// <summary>
    /// FUnity ワークスペースで利用できるブロックの種類を記述します。
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Block Definition", fileName = "BlockDefinition")]
    public sealed class BlockDefinition : ScriptableObject
    {
        [SerializeField]
        private string m_displayName = "New Block";

        [TextArea]
        [SerializeField]
        private string m_description = "Describe what this block does.";

        /// <summary>
        /// ブロックの表示名を取得します。
        /// </summary>
        public string DisplayName => m_displayName;

        /// <summary>
        /// ブロックの説明文を取得します。
        /// </summary>
        public string Description => m_description;
    }
}
