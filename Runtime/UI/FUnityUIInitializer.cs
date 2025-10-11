using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UILoadProfile profile;

        public UILoadProfile Profile => profile;

        private void Awake()
        {
            if (!TryResolveDocument())
            {
                Debug.LogError("[FUnityUIInitializer] UIDocument component is missing.");
                return;
            }

            if (profile != null)
            {
                ApplyProfile(profile);
            }
            else
            {
                ApplyFallback();
            }
        }

        private bool TryResolveDocument()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            return uiDocument != null;
        }

        private void ApplyProfile(UILoadProfile loadProfile)
        {
            if (loadProfile.panelSettings != null)
            {
                uiDocument.panelSettings = loadProfile.panelSettings;
            }

            if (loadProfile.uxml != null)
            {
                uiDocument.visualTreeAsset = loadProfile.uxml;
            }

            var root = uiDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            if (loadProfile.uxml != null)
            {
                loadProfile.uxml.CloneTree(root);
            }
            else if (uiDocument.visualTreeAsset != null)
            {
                uiDocument.visualTreeAsset.CloneTree(root);
            }

            AppendStyleSheets(root, loadProfile.uss);

            if (loadProfile.spawnFooni)
            {
                root.Add(new FooniElement());
            }

            if (loadProfile.spawnBlocks)
            {
                Debug.LogWarning("[FUnityUIInitializer] Block UI spawning is not implemented yet.");
            }

            Debug.Log("✅ FUnityUIInitializer: UI loaded via UILoadProfile.");
        }

        private static void AppendStyleSheets(VisualElement root, List<StyleSheet> sheets)
        {
            if (sheets == null)
            {
                return;
            }

            foreach (var sheet in sheets)
            {
                if (sheet != null)
                {
                    root.styleSheets.Add(sheet);
                }
            }
        }

        private void ApplyFallback()
        {
            var root = uiDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            root.Add(new FooniElement());
            Debug.Log("🌈 FUnityUIInitializer: FooniElement spawned via fallback.");
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
