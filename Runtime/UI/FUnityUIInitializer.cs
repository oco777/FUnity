using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using FUnity.UI;

namespace FUnity.Runtime.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
        // Provides fallback block labels when a profile does not define any.
        private static readonly string[] DefaultBlockPalette =
        {
            "when green flag clicked",
            "move 10 steps",
            "say \"Hello!\""
        };

        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiDocument")]
        private UIDocument m_UIDocument;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("profile")]
        private UILoadProfile m_Profile;

        public UILoadProfile Profile => m_Profile;

        private void Awake()
        {
            if (!TryResolveDocument())
            {
                Debug.LogError("[FUnityUIInitializer] UIDocument component is missing.");
                return;
            }

            if (m_Profile != null)
            {
                ApplyProfile(m_Profile);
            }
            else
            {
                ApplyFallback();
            }
        }

        private bool TryResolveDocument()
        {
            if (m_UIDocument == null)
            {
                m_UIDocument = GetComponent<UIDocument>();
            }

            return m_UIDocument != null;
        }

        private void ApplyProfile(UILoadProfile loadProfile)
        {
            if (loadProfile.panelSettings != null)
            {
                m_UIDocument.panelSettings = loadProfile.panelSettings;
            }

            if (loadProfile.uxml != null)
            {
                m_UIDocument.visualTreeAsset = loadProfile.uxml;
            }

            var root = m_UIDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            if (loadProfile.uxml != null)
            {
                loadProfile.uxml.CloneTree(root);
            }
            else if (m_UIDocument.visualTreeAsset != null)
            {
                m_UIDocument.visualTreeAsset.CloneTree(root);
            }

            AppendStyleSheets(root, loadProfile.uss);

            if (loadProfile.spawnFooni)
            {
                root.Add(new FooniElement());
            }

            if (loadProfile.spawnBlocks)
            {
                SpawnBlocks(root, loadProfile);
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
            var root = m_UIDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            root.Add(new FooniElement());
            Debug.Log("🌈 FUnityUIInitializer: FooniElement spawned via fallback.");
        }

        private static void SpawnBlocks(VisualElement root, UILoadProfile loadProfile)
        {
            // Spawn a lightweight block palette so profiles can show sample blocks immediately.
            if (root == null)
            {
                return;
            }

            var paletteContainer = new ScrollView
            {
                name = "funity-block-palette",
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };
            paletteContainer.AddToClassList("funity-block-palette");
            paletteContainer.style.paddingLeft = 12f;
            paletteContainer.style.paddingRight = 12f;
            paletteContainer.style.paddingTop = 8f;
            paletteContainer.style.paddingBottom = 8f;
            paletteContainer.contentContainer.style.flexDirection = FlexDirection.Column;
            SetChildrenGap(paletteContainer.contentContainer, 6f);

            var paletteSource = loadProfile != null && loadProfile.BlockPalette != null && loadProfile.BlockPalette.Count > 0
                ? loadProfile.BlockPalette
                : DefaultBlockPalette;

            foreach (var blockLabel in paletteSource)
            {
                if (string.IsNullOrWhiteSpace(blockLabel))
                {
                    continue;
                }

                var block = new BlockElement();
                block.Text = blockLabel.Trim();
                paletteContainer.Add(block);
            }

            if (paletteContainer.contentContainer.childCount == 0)
            {
                var fallbackBlock = new BlockElement();
                fallbackBlock.Text = "Block";
                paletteContainer.Add(fallbackBlock);
            }

            root.Add(paletteContainer);
            Debug.Log("🧱 FUnityUIInitializer: Block palette spawned.");
        }

        // NOTE: We don't call IStyle.gap at all to avoid compile-time dependency across versions.
        private static void SetChildrenGap(VisualElement parent, float gap)
        {
            if (parent == null)
            {
                return;
            }

            foreach (var child in parent.Children())
            {
                child.style.marginLeft = 0f;
                child.style.marginRight = 0f;
                child.style.marginTop = 0f;
                child.style.marginBottom = 0f;
            }

            var direction = parent.style.flexDirection.value;
            bool isRow = direction == FlexDirection.Row || direction == FlexDirection.RowReverse;

            int index = 0;
            int childCount = parent.childCount;
            foreach (var child in parent.Children())
            {
                bool isLast = index == childCount - 1;
                if (isRow)
                {
                    if (!isLast)
                    {
                        child.style.marginRight = gap;
                    }
                }
                else if (!isLast)
                {
                    child.style.marginBottom = gap;
                }

                index++;
            }
        }

        private void OnValidate()
        {
            if (m_UIDocument == null)
            {
                m_UIDocument = GetComponent<UIDocument>();
            }
        }
    }
}
