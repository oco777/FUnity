using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Stage
{
    /// <summary>
    /// Unity UI Toolkit を用いてスクラッチ風ワークスペースを構築し管理します。
    /// ランタイムは左にステージ、右に補助パネルを持つ二列レイアウトを生成します。
    /// ステージ上のスプライトは Visual Scripting から操作できる <see cref="StageSpriteActor"/> インスタンスで管理されます。
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
        /// 現在のステージ表示領域の幅と高さを返します。
        /// </summary>
        public Vector2 StageSize => m_stageRoot?.contentRect.size ?? new Vector2(960f, 540f);

        /// <summary>
        /// ランタイムを初期化し、レイアウトアセットの読み込みと主要参照のキャッシュを行います。
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
        /// コンポーネントが有効化された際に既存のアクターを登録し、画面上のスプライト一覧を更新します。
        /// </summary>
        private void OnEnable()
        {
            RegisterExistingActors();
            RefreshSpriteList();
        }

        /// <summary>
        /// コンポーネントが無効化されたときに登録済みアクターを後処理し、シングルトンを解放します。
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
        /// ランタイム中にステージへ新しいスプライトアクターを生成します。
        /// Visual Scripting のグラフから呼び出せるよう設計されています。
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
        /// アクターをランタイムへ登録し、ステージで描画・追跡できるようにします。
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
        /// アクターをランタイムから外し、ビジュアルツリーから確実に切り離します。
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
        /// ステージのレイアウトとスタイルを定義する UXML/USS リソースを読み込みます。
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
        /// ステージ操作に必要な主要ビジュアル要素を検索して保持します。
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
        /// 階層内に既に存在する StageSpriteActor コンポーネントを見つけて登録します。
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
        /// 登録されているスプライトアクターを反映した UI リストを再構築します。
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
