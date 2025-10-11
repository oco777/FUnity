using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
        private const string PanelSettingsResourcePath = "FUnityPanelSettings";

        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UILoadProfile profile;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                Debug.LogError("[FUnityUIInitializer] UIDocument component is missing.");
                return;
            }

            if (profile != null)
            {
                LoadFromProfile();
            }
            else
            {
                ApplyFallbackUI();
            }
        }

        private void LoadFromProfile()
        {
            if (profile.panelSettings != null)
            {
                uiDocument.panelSettings = profile.panelSettings;
            }
            else if (uiDocument.panelSettings == null)
            {
                ApplyPanelSettings();
            }

            uiDocument.visualTreeAsset = profile.uxml;

            var root = uiDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            if (profile.uxml != null)
            {
                profile.uxml.CloneTree(root);
            }

            List<StyleSheet> styleSheets = profile.uss;
            if (styleSheets != null)
            {
                foreach (var styleSheet in styleSheets)
                {
                    if (styleSheet != null)
                    {
                        root.styleSheets.Add(styleSheet);
                    }
                }
            }

            if (profile.spawnFooni)
            {
                root.Add(new FooniElement());
            }

            if (profile.spawnBlocks)
            {
                Debug.LogWarning("[FUnityUIInitializer] TODO: Block UI spawning is not implemented yet.");
            }

            Debug.Log("✅ FUnityUIInitializer: UI loaded via UILoadProfile.");
        }

        private void ApplyFallbackUI()
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            var fooniElement = new FooniElement();
            root.Add(fooniElement);

            ApplyPanelSettings();
            Debug.Log("🌈 FUnityUIInitializer: FooniElement をコードから生成・表示しました。");
        }

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

        private void OnValidate()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }
    }
}
