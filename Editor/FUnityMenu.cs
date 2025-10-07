#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FUnity.Editor
{
    /// <summary>
    /// Provides convenience menu items for configuring FUnity in the editor.
    /// </summary>
    public static class FUnityMenu
    {
        [MenuItem("FUnity/Create Sample Workspace")]
        private static void CreateSampleWorkspace()
        {
            Debug.Log("[FUnity] Sample workspace creation triggered from menu.");
        }
    }
}
#endif
