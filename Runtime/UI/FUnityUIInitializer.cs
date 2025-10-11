using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
        private const string UxmlResourcePath = "UI/block";
        private const string PanelSettingsPath = "FUnityPanelSettings";

        private void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogWarning("[FUnityUIInitializer] UIDocument not found.");
                return;
            }

            var visualTree = Resources.Load<VisualTreeAsset>(UxmlResourcePath);
            if (visualTree == null)
            {
                Debug.LogWarning($"[FUnityUIInitializer] UXML not found at Resources/{UxmlResourcePath}.uxml");
                return;
            }

            uiDocument.visualTreeAsset = visualTree;

            var panel = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (panel != null)
            {
                uiDocument.panelSettings = panel;
            }
            else
            {
                Debug.LogWarning($"[FUnityUIInitializer] PanelSettings not found in Resources/{PanelSettingsPath}");
            }

            Debug.Log("[FUnityUIInitializer] UI initialized successfully.");
        }
    }
}
