using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    /// <summary>
    /// Binds UI Toolkit elements to expose simple runtime controls.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class WorkspaceHUD : MonoBehaviour
    {
        private UIDocument document;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = document?.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[FUnity] WorkspaceHUD missing UIDocument root.");
                return;
            }

            var runButton = root.Q<Button>("run-button");
            if (runButton != null)
            {
                runButton.clicked += () => Debug.Log("[FUnity] Run workspace requested.");
            }
        }
    }
}
