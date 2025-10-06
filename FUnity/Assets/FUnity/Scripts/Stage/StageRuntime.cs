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

        private UIDocument _document = default!;
        private VisualElement _stageRoot = default!;
        private ScrollView _spriteList = default!;
        private Label _instructionsLabel = default!;

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
            BuildLayout();
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

        private void BuildLayout()
        {
            var root = _document.rootVisualElement;
            root.style.flexGrow = 1f;
            root.style.flexDirection = FlexDirection.Row;
            root.style.backgroundColor = new StyleColor(new Color(0.11f, 0.11f, 0.13f));
            root.style.paddingLeft = 12f;
            root.style.paddingRight = 12f;
            root.style.paddingTop = 12f;
            root.style.paddingBottom = 12f;
            //root.style.gap = 12f;

            var leftColumn = new VisualElement { name = "funity-left" };
            leftColumn.style.flexGrow = 1f;
            leftColumn.style.flexDirection = FlexDirection.Column;
            //leftColumn.style.gap = 8f;
            root.Add(leftColumn);

            var stageTitle = new Label("ステージ");
            stageTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            stageTitle.style.fontSize = 20f;
            stageTitle.style.color = new StyleColor(Color.white);
            leftColumn.Add(stageTitle);

            _stageRoot = new VisualElement { name = "funity-stage" };
            _stageRoot.style.flexGrow = 1f;
            _stageRoot.style.minHeight = 360f;
            _stageRoot.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.2f));
            _stageRoot.style.borderBottomWidth = 2f;
            _stageRoot.style.borderTopWidth = 2f;
            _stageRoot.style.borderLeftWidth = 2f;
            _stageRoot.style.borderRightWidth = 2f;
            _stageRoot.style.borderBottomColor = new StyleColor(new Color(0.34f, 0.34f, 0.38f));
            _stageRoot.style.borderTopColor = new StyleColor(new Color(0.34f, 0.34f, 0.38f));
            _stageRoot.style.borderLeftColor = new StyleColor(new Color(0.34f, 0.34f, 0.38f));
            _stageRoot.style.borderRightColor = new StyleColor(new Color(0.34f, 0.34f, 0.38f));
            _stageRoot.style.position = Position.Relative;
            _stageRoot.style.overflow = Overflow.Hidden;
            _stageRoot.style.flexDirection = FlexDirection.Row;
            _stageRoot.style.justifyContent = Justify.Center;
            _stageRoot.style.alignItems = Align.Center;
            leftColumn.Add(_stageRoot);

            var spritePanelTitle = new Label("スプライト");
            spritePanelTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            spritePanelTitle.style.fontSize = 16f;
            spritePanelTitle.style.color = new StyleColor(Color.white);
            leftColumn.Add(spritePanelTitle);

            _spriteList = new ScrollView { name = "funity-sprite-list" };
            _spriteList.style.flexGrow = 0f;
            _spriteList.style.height = 120f;
            _spriteList.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.17f));
            _spriteList.style.borderBottomWidth = 1f;
            _spriteList.style.borderTopWidth = 1f;
            _spriteList.style.borderLeftWidth = 1f;
            _spriteList.style.borderRightWidth = 1f;
            _spriteList.style.borderBottomColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            _spriteList.style.borderTopColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            _spriteList.style.borderLeftColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            _spriteList.style.borderRightColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            leftColumn.Add(_spriteList);

            var rightColumn = new VisualElement { name = "funity-right" };
            rightColumn.style.width = 360f;
            rightColumn.style.flexShrink = 0f;
            rightColumn.style.flexDirection = FlexDirection.Column;
            //rightColumn.style.gap = 8f;
            root.Add(rightColumn);

            var programmingTitle = new Label("Visual Scripting");
            programmingTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            programmingTitle.style.fontSize = 20f;
            programmingTitle.style.color = new StyleColor(Color.white);
            rightColumn.Add(programmingTitle);

            _instructionsLabel = new Label(
                "Visual Scripting Graph ウィンドウを開いて、Flow Graph でスプライトを操作してください。\n" +
                "StageRuntime.SpawnSprite や StageSpriteActor.MoveBy などのメソッドはそのままブロックとして利用できます。");
            _instructionsLabel.style.whiteSpace = WhiteSpace.Normal;
            _instructionsLabel.style.fontSize = 13f;
            _instructionsLabel.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.9f));
            _instructionsLabel.style.unityTextAlign = TextAnchor.UpperLeft;
            _instructionsLabel.style.backgroundColor = new StyleColor(new Color(0.14f, 0.14f, 0.17f));
            _instructionsLabel.style.paddingLeft = 12f;
            _instructionsLabel.style.paddingRight = 12f;
            _instructionsLabel.style.paddingTop = 12f;
            _instructionsLabel.style.paddingBottom = 12f;
            _instructionsLabel.style.borderBottomWidth = 1f;
            _instructionsLabel.style.borderTopWidth = 1f;
            _instructionsLabel.style.borderLeftWidth = 1f;
            _instructionsLabel.style.borderRightWidth = 1f;
            _instructionsLabel.style.borderBottomColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            _instructionsLabel.style.borderTopColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            _instructionsLabel.style.borderLeftColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            _instructionsLabel.style.borderRightColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
            _instructionsLabel.style.borderTopLeftRadius = 4f;
            _instructionsLabel.style.borderTopRightRadius = 4f;
            _instructionsLabel.style.borderBottomLeftRadius = 4f;
            _instructionsLabel.style.borderBottomRightRadius = 4f;
            rightColumn.Add(_instructionsLabel);
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
                entry.style.flexDirection = FlexDirection.Row;
                entry.style.alignItems = Align.Center;
                //entry.style.gap = 8f;
                entry.style.paddingLeft = 8f;
                entry.style.paddingRight = 8f;
                entry.style.paddingTop = 4f;
                entry.style.paddingBottom = 4f;

                var swatch = new VisualElement();
                swatch.style.width = 32f;
                swatch.style.height = 32f;
                swatch.style.borderBottomWidth = 1f;
                swatch.style.borderTopWidth = 1f;
                swatch.style.borderLeftWidth = 1f;
                swatch.style.borderRightWidth = 1f;
                swatch.style.borderBottomColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
                swatch.style.borderTopColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
                swatch.style.borderLeftColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
                swatch.style.borderRightColor = new StyleColor(new Color(0.27f, 0.27f, 0.32f));
                swatch.style.backgroundImage = actor.CurrentBackground;
                swatch.style.unityBackgroundImageTintColor = new StyleColor(Color.white);
                if (!actor.HasSprite)
                {
                    swatch.style.backgroundColor = new StyleColor(new Color(0.45f, 0.45f, 0.5f));
                }
                entry.Add(swatch);

                var label = new Label(actor.DisplayName);
                label.style.color = new StyleColor(Color.white);
                label.style.fontSize = 14f;
                entry.Add(label);

                _spriteList.contentContainer.Add(entry);
            }
        }
    }
}
