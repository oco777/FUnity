using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
        private const string FooniElementResourcePath = "UI/FooniElement";
        private const string PanelSettingsResourcePath = "FUnityPanelSettings";

        [SerializeField]
        private UIDocument uiDocument;

        private void Awake()
        {
            // Ensure the UIDocument reference is available.
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                Debug.LogError("UIDocumentが見つかりません。");
                return;
            }

            // Load the Fooni element layout.
            var visualTree = Resources.Load<VisualTreeAsset>(FooniElementResourcePath);
            if (visualTree == null)
            {
                Debug.LogError("FooniElement.uxml が見つかりません。");
                return;
            }

            // Replace the current root content with Fooni.
            var root = uiDocument.rootVisualElement;
            root.Clear();
            var fooniElement = visualTree.Instantiate();
            root.Add(fooniElement);

            ApplyPanelSettings();
            Debug.Log("🌈 FUnityUIInitializer: FooniElement を表示しました。");
        }

        // Apply panel settings if available in Resources.
        private void ApplyPanelSettings()
        {
            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (panelSettings != null)
            {
                uiDocument.panelSettings = panelSettings;
            }
            else
            {
                Debug.LogWarning($"[FUnityUIInitializer] PanelSettings not found in Resources/{PanelSettingsResourcePath}");
            }
        }
    }
}
