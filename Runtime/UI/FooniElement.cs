// Updated: 2025-03-03
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// Fooni キャラクターを表示する UI Toolkit カスタム要素。View レイヤーの再利用可能な部品。
    /// </summary>
    /// <remarks>
    /// 依存関係: Resources/UI/ActorElement (UXML/USS), Resources/UI/FooniElement (追加装飾), Resources/Characters/Fooni (Texture2D)
    /// 想定ライフサイクル: UI ドキュメントの生成時にインスタンス化され、<see cref="FUnity.Core.FUnityManager"/> から俳優要素として利用される。
    /// UI Toolkit の仕様上、<c>[UxmlElement]</c> を付与するクラスは <c>partial</c> 修飾子が必須である。
    /// 背景画像の割当には <see cref="StyleBackground"/> の `new StyleBackground(Texture2D)` コンストラクタを使用する。
    /// </remarks>
    [UxmlElement]
    public partial class FooniElement : VisualElement
    {
        /// <summary>共通俳優レイアウトの Resources 内パス。</summary>
        private const string BaseVisualTreeResourcePath = "UI/ActorElement";

        /// <summary>共通俳優スタイルの Resources 内パス。</summary>
        private const string BaseStyleResourcePath = "UI/ActorElement";

        /// <summary>Fooni 固有レイアウトの Resources 内パス。</summary>
        private const string VisualTreeResourcePath = null;

        /// <summary>Fooni 固有スタイルの Resources 内パス。</summary>
        private const string StyleResourcePath = "UI/FooniElement";

        /// <summary>標準ポートレート画像の Resources 内パス（拡張子不要）。</summary>
        private const string FooniTexturePath = "Characters/Fooni";

        /// <summary>`Q` 検索で使用する画像要素の名前/クラス。</summary>
        private const string FooniImageName = "fooni-image";

        /// <summary>ポートレート描画対象の <see cref="Image"/> 要素。</summary>
        private Image m_Image;

        /// <summary>
        /// レイアウトと画像を読み込み、要素を即座に利用可能な状態へ初期化する。
        /// </summary>
        public FooniElement()
        {
            InitializeLayout();
            CacheImageElement();
            LoadCharacter();
        }

        /// <summary>
        /// UXML と USS を読み込み、要素の構造とスタイルを構築する。
        /// </summary>
        private void InitializeLayout()
        {
            CloneTree(BaseVisualTreeResourcePath);
            ApplyStyle(BaseStyleResourcePath);
            CloneTree(VisualTreeResourcePath);
            ApplyStyle(StyleResourcePath);
        }

        /// <summary>
        /// Resources から VisualTreeAsset を読み込み、自身に複製する。
        /// </summary>
        /// <param name="resourcePath">Resources 内のパス。</param>
        private void CloneTree(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return;
            }

            var visualTree = Resources.Load<VisualTreeAsset>(resourcePath);
            if (visualTree != null)
            {
                visualTree.CloneTree(this);
            }
            else
            {
                Debug.LogWarning($"FooniElement: Unable to load VisualTreeAsset at Resources/{resourcePath}.");
            }
        }

        /// <summary>
        /// Resources から StyleSheet を読み込み、要素へ追加する。
        /// </summary>
        /// <param name="resourcePath">Resources 内のパス。</param>
        private void ApplyStyle(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return;
            }

            var styleSheet = Resources.Load<StyleSheet>(resourcePath);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning($"FooniElement: Unable to load StyleSheet at Resources/{resourcePath}.");
            }
        }

        /// <summary>
        /// ポートレート描画用の <see cref="Image"/> 要素をキャッシュする。
        /// </summary>
        private void CacheImageElement()
        {
            m_Image = this.Q<Image>(name: FooniImageName) ?? this.Q<Image>(className: FooniImageName);
            if (m_Image == null)
            {
                Debug.LogWarning("FooniElement: Unable to find Image element with name or class 'fooni-image'.");
            }
        }

        /// <summary>
        /// Resources からキャラクターテクスチャを読み込み、<see cref="Image.image"/> へ割り当てる。
        /// </summary>
        /// <remarks>
        /// UI Toolkit では <see cref="Image.image"/> へ直接 <see cref="Texture2D"/> を設定する。StyleBackground.none は利用できない。
        /// </remarks>
        private void LoadCharacter()
        {
            if (m_Image == null)
            {
                return;
            }

            var texture = Resources.Load<Texture2D>(FooniTexturePath);
            if (texture == null)
            {
                Debug.LogWarning("⚠️ Fooni image not found in Resources/Characters/Fooni.png");
                return;
            }

            m_Image.image = texture;
        }
    }
}
