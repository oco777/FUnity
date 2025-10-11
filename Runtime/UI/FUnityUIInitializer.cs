using FUnity.Runtime.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
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

            // Replace the current root content with a new Fooni element instance.
            var root = uiDocument.rootVisualElement;
            root.Clear();
            var fooniElement = new FooniElement();
            root.Add(fooniElement);

            ApplyPanelSettings();
            Debug.Log("🌈 FUnityUIInitializer: FooniElement をコードから生成・表示しました。");
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
