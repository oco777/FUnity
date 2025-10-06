using System.Collections.Generic;
using FUnity.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    /// <summary>
    /// UI Toolkit based workspace visual that binds to a <see cref="FUnityWorkspace"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WorkspaceView : MonoBehaviour
    {
        [SerializeField]
        private FUnityWorkspace m_Workspace;

        [SerializeField]
        private VisualTreeAsset m_WorkspaceLayout;

        [SerializeField]
        private StyleSheet m_WorkspaceStyles;

        private UIDocument m_Document;

        private VisualElement m_PaletteContainer;

        private void Awake()
        {
            m_Document = GetComponent<UIDocument>();
            BuildInterface();
        }

        private void BuildInterface()
        {
            if (m_Document == null)
            {
                m_Document = GetComponent<UIDocument>();
            }

            VisualElement root = m_Document.rootVisualElement;
            root.Clear();

            if (m_WorkspaceLayout != null)
            {
                m_Document.visualTreeAsset = m_WorkspaceLayout;
                root = m_Document.rootVisualElement;
            }
            else
            {
                root = CreateFallbackLayout(root);
            }

            if (m_WorkspaceStyles != null)
            {
                root.styleSheets.Add(m_WorkspaceStyles);
            }

            m_PaletteContainer = root.Q("palette-list");
            RenderPalette();
        }

        private VisualElement CreateFallbackLayout(VisualElement root)
        {
            VisualElement workspace = new()
            {
                name = "funity-workspace"
            };
            workspace.AddToClassList("funity-workspace");
            workspace.style.flexGrow = 1f;
            workspace.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            workspace.style.color = Color.white;

            VisualElement layout = new()
            {
                name = "workspace-root"
            };
            layout.AddToClassList("funity-layout");
            layout.style.flexDirection = FlexDirection.Row;
            layout.style.flexGrow = 1f;

            VisualElement palette = new()
            {
                name = "palette"
            };
            palette.AddToClassList("funity-palette");
            palette.style.width = 280f;
            palette.style.backgroundColor = new Color(0.15f, 0.16f, 0.19f);
            palette.style.paddingLeft = palette.style.paddingRight = palette.style.paddingTop = palette.style.paddingBottom = 8;

            ScrollView paletteList = new()
            {
                name = "palette-list"
            };
            palette.Add(paletteList);

            VisualElement stage = new()
            {
                name = "stage"
            };
            stage.AddToClassList("funity-stage");
            stage.style.flexGrow = 1f;
            stage.style.backgroundColor = new Color(0.1f, 0.11f, 0.14f);
            stage.style.paddingLeft = stage.style.paddingRight = stage.style.paddingTop = stage.style.paddingBottom = 12;
            stage.Add(new Label("Stage Preview") { name = "stage-label" });

            layout.Add(palette);
            layout.Add(stage);
            workspace.Add(layout);
            root.Add(workspace);

            return root;
        }

        public void SetWorkspace(FUnityWorkspace workspace)
        {
            m_Workspace = workspace;
            RenderPalette();
        }

        public void SetLayout(VisualTreeAsset layout)
        {
            m_WorkspaceLayout = layout;
            BuildInterface();
        }

        public void SetStyles(StyleSheet styles)
        {
            m_WorkspaceStyles = styles;
            BuildInterface();
        }

        private void RenderPalette()
        {
            if (m_PaletteContainer == null || m_Workspace == null)
            {
                return;
            }

            m_PaletteContainer.Clear();
            foreach (BlockCollection collection in m_Workspace.Collections)
            {
                VisualElement foldout = CreateCollectionElement(collection);
                m_PaletteContainer.Add(foldout);
            }
        }

        private VisualElement CreateCollectionElement(BlockCollection collection)
        {
            Foldout foldout = new()
            {
                text = collection.Category,
                value = true
            };

            IReadOnlyList<BlockDefinition> blocks = collection.Definitions;
            foreach (BlockDefinition block in blocks)
            {
                Label blockLabel = new(block.DisplayName)
                {
                    name = $"block-{block.DisplayName.ToLowerInvariant().Replace(' ', '-')}"
                };

                blockLabel.style.backgroundColor = block.Color;
                blockLabel.tooltip = block.Description;
                blockLabel.AddToClassList("funity-block");
                foldout.Add(blockLabel);
            }

            return foldout;
        }
    }
}
