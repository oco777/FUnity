using UnityEngine;

namespace FUnity.Core
{
    /// <summary>
    /// Provides an entry point for initializing FUnity runtime systems in a scene.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField]
        private string workspaceName = "Default Workspace";

        private void Awake()
        {
            Debug.Log($"[FUnity] Initializing workspace '{workspaceName}'.");
        }
    }
}
