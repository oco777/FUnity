using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string UxmlPath = "Packages/com.papacoder.funity/UXML/block.uxml";
#endif
        private const string PanelSettingsPath = "Assets/FUnity/Resources/FUnityPanelSettings.asset";

        private void OnEnable()
        {
            Debug.Log("[FUnityUIInitializer] OnEnable called");

            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogWarning("[FUnityUIInitializer] UIDocument not found on this GameObject.");
                return;
            }

#if UNITY_EDITOR
            var visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (visualTree == null)
            {
                Debug.LogWarning($"[FUnityUIInitializer] UXML not found at {UxmlPath}");
                return;
            }
            uiDocument.visualTreeAsset = visualTree;
#endif

            var panel = Resources.Load<PanelSettings>("FUnityPanelSettings");
            if (panel != null)
            {
                uiDocument.panelSettings = panel;
            }
            else
            {
                Debug.LogWarning("[FUnityUIInitializer] PanelSettings not found in Resources/FUnityPanelSettings.asset");
            }
        }
    }
}
