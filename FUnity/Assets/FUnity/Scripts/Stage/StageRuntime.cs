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
        private readonly List<StageSpriteActor> _actors = new();

        private const string LayoutResourcePath = "Stage/StageLayout";
        private const string StyleResourcePath = "Stage/StageStyles";

        private UIDocument _document = default!;
        private VisualElement? _stageRoot;
        private ScrollView? _spriteList;

        public static StageRuntime? Instance { get; private set; }

        /// <summary>
        /// Width/height of the current stage content rectangle.
        /// </summary>
        public Vector2 StageSize => _stageRoot?.contentRect.size ?? new Vector2(960f, 540f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("FUnity stage runtime already exists. Destroying duplicate instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _document = GetComponent<UIDocument>();
            LoadLayout();
            CacheLayoutReferences();
        }

        private void OnEnable()
        {
            RegisterExistingActors();
            RefreshSpriteList();
        }

        private void OnDisable()
        {
            foreach (var actor in _actors)
            {
                actor.DetachFromStage();
            }

            _actors.Clear();
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

        internal void RegisterActor(StageSpriteActor actor)
        {
            if (_actors.Contains(actor))
            {
                return;
            }

            _actors.Add(actor);
            if (_stageRoot == null)
            {
                Debug.LogError("Stage root is missing. Ensure StageLayout.uxml is loaded correctly.");
                return;
            }

            actor.AttachToStage(_stageRoot);
            RefreshSpriteList();
        }

        internal void UnregisterActor(StageSpriteActor actor)
        {
            if (_actors.Remove(actor))
            {
                actor.DetachFromStage();
                RefreshSpriteList();
            }
        }

        private void LoadLayout()
        {
            var root = _document.rootVisualElement;
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

        private void CacheLayoutReferences()
        {
            var root = _document.rootVisualElement;
            _stageRoot = root.Q<VisualElement>("funity-stage");
            _spriteList = root.Q<ScrollView>("funity-sprite-list");
            if (_stageRoot == null)
            {
                Debug.LogError("Stage root element not found in loaded UXML. Stage functionality will be limited.");
            }

            if (_spriteList == null)
            {
                Debug.LogError("Sprite list element not found in loaded UXML. Sprite overview will be disabled.");
            }
        }

        private void RegisterExistingActors()
        {
            var existingActors = GetComponentsInChildren<StageSpriteActor>(true);
            foreach (var actor in existingActors)
            {
                RegisterActor(actor);
            }
        }

        private void RefreshSpriteList()
        {
            if (_spriteList == null)
            {
                return;
            }

            _spriteList.contentContainer.Clear();
            foreach (var actor in _actors)
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

                _spriteList.contentContainer.Add(entry);
            }
        }
    }
}
