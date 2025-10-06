using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Stage
{
    /// <summary>
    /// Builds and manages the Scratch-like workspace using Unity UI Toolkit.
    /// The runtime creates a two-column layout: a stage on the left and helper panels on the right.
    /// Stage sprites are managed via <see cref="StageSpriteActor"/> instances that Visual Scripting can control.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StageRuntime : MonoBehaviour
    {
        private readonly List<StageSpriteActor> m_actors = new();

        private const string LayoutResourcePath = "Stage/StageLayout";
        private const string StyleResourcePath = "Stage/StageStyles";

        private UIDocument m_document = default!;
        private VisualElement? m_stageRoot;
        private ScrollView? m_spriteList;

        public static StageRuntime? Instance { get; private set; }

        /// <summary>
        /// Width/height of the current stage content rectangle.
        /// </summary>
        public Vector2 StageSize => m_stageRoot?.contentRect.size ?? new Vector2(960f, 540f);

        /// <summary>
        /// Initializes the runtime instance, loads the layout assets and caches key references.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("FUnity stage runtime already exists. Destroying duplicate instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            m_document = GetComponent<UIDocument>();
            LoadLayout();
            CacheLayoutReferences();
        }

        /// <summary>
        /// Hooks up existing actors and refreshes the on-screen sprite list when the component activates.
        /// </summary>
        private void OnEnable()
        {
            RegisterExistingActors();
            RefreshSpriteList();
        }

        /// <summary>
        /// Cleans up registered actors and releases the singleton instance when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            foreach (var actor in m_actors)
            {
                actor.DetachFromStage();
            }

            m_actors.Clear();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Instantiate a new sprite actor on the stage at runtime.
        /// This method is designed to be called from Visual Scripting graphs.
        /// </summary>
        public StageSpriteActor? SpawnSprite(StageSpriteDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("SpawnSprite called with a null definition.");
                return null;
            }

            var spriteObject = new GameObject(definition.DisplayName);
            spriteObject.transform.SetParent(transform, false);

            var actor = spriteObject.AddComponent<StageSpriteActor>();
            actor.ApplyDefinition(definition);
            RegisterActor(actor);
            actor.MoveTo(definition.InitialPosition);
            return actor;
        }

        /// <summary>
        /// Adds an actor to the runtime so it can be rendered and tracked by the stage.
        /// </summary>
        internal void RegisterActor(StageSpriteActor actor)
        {
            if (m_actors.Contains(actor))
            {
                return;
            }

            m_actors.Add(actor);
            if (m_stageRoot == null)
            {
                Debug.LogError("Stage root is missing. Ensure StageLayout.uxml is loaded correctly.");
                return;
            }

            actor.AttachToStage(m_stageRoot);
            RefreshSpriteList();
        }

        /// <summary>
        /// Removes an actor from the runtime, ensuring it is detached from the visual tree.
        /// </summary>
        internal void UnregisterActor(StageSpriteActor actor)
        {
            if (m_actors.Remove(actor))
            {
                actor.DetachFromStage();
                RefreshSpriteList();
            }
        }

        /// <summary>
        /// Loads the UXML/USS resources that define the stage layout and styling.
        /// </summary>
        private void LoadLayout()
        {
            var root = m_document.rootVisualElement;
            root.Clear();

            var layout = Resources.Load<VisualTreeAsset>(LayoutResourcePath);
            if (layout == null)
            {
                Debug.LogError($"Unable to load stage layout UXML at Resources/{LayoutResourcePath}.");
                return;
            }

            layout.CloneTree(root);

            var styleSheet = Resources.Load<StyleSheet>(StyleResourcePath);
            if (styleSheet == null)
            {
                Debug.LogWarning($"Unable to load stage stylesheet USS at Resources/{StyleResourcePath}. Using inline defaults.");
                return;
            }

            root.styleSheets.Add(styleSheet);
        }

        /// <summary>
        /// Queries and stores the key visual elements required for stage interaction.
        /// </summary>
        private void CacheLayoutReferences()
        {
            var root = m_document.rootVisualElement;
            m_stageRoot = root.Q<VisualElement>("funity-stage");
            m_spriteList = root.Q<ScrollView>("funity-sprite-list");
            if (m_stageRoot == null)
            {
                Debug.LogError("Stage root element not found in loaded UXML. Stage functionality will be limited.");
            }

            if (m_spriteList == null)
            {
                Debug.LogError("Sprite list element not found in loaded UXML. Sprite overview will be disabled.");
            }
        }

        /// <summary>
        /// Finds StageSpriteActor components already present in the hierarchy and registers them.
        /// </summary>
        private void RegisterExistingActors()
        {
            var existingActors = GetComponentsInChildren<StageSpriteActor>(true);
            foreach (var actor in existingActors)
            {
                RegisterActor(actor);
            }
        }

        /// <summary>
        /// Rebuilds the UI list that mirrors the currently registered sprite actors.
        /// </summary>
        private void RefreshSpriteList()
        {
            if (m_spriteList == null)
            {
                return;
            }

            m_spriteList.contentContainer.Clear();
            foreach (var actor in m_actors)
            {
                var entry = new VisualElement();
                entry.AddToClassList("funity-sprite-entry");

                var swatch = new VisualElement();
                swatch.AddToClassList("funity-sprite-swatch");
                swatch.style.backgroundImage = actor.CurrentBackground;
                swatch.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                if (!actor.HasSprite)
                {
                    swatch.style.backgroundColor = new StyleColor(new Color(0.45f, 0.45f, 0.5f));
                }
                entry.Add(swatch);

                var label = new Label(actor.DisplayName);
                label.AddToClassList("funity-sprite-label");
                entry.Add(label);

                m_spriteList.contentContainer.Add(entry);
            }
        }
    }
}
