using System.Collections.Generic;
using UnityEngine;

namespace FUnity.Core
{
    /// <summary>
    /// Represents a workspace that manages scripts and variables authored with blocks.
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Workspace", fileName = "FUnityWorkspace")]
    public class FUnityWorkspace : ScriptableObject
    {
        [SerializeField]
        private string m_Title = "My Project";

        [SerializeField]
        private List<BlockCollection> m_Collections = new();

        /// <summary>
        /// Display title for the workspace window.
        /// </summary>
        public string Title => m_Title;

        /// <summary>
        /// All block collections registered to this workspace.
        /// </summary>
        public IReadOnlyList<BlockCollection> Collections => m_Collections;

        /// <summary>
        /// Registers a new block collection at runtime.
        /// </summary>
        /// <param name="collection">The collection to add.</param>
        public void RegisterCollection(BlockCollection collection)
        {
            if (collection == null || m_Collections.Contains(collection))
            {
                return;
            }

            m_Collections.Add(collection);
        }

        /// <summary>
        /// Replaces the current block collections with the provided list.
        /// </summary>
        public void Configure(string title, IEnumerable<BlockCollection> collections)
        {
            m_Title = title;
            m_Collections = new List<BlockCollection>(collections);
        }
    }
}
