// Updated: 2025-02-14
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// Fooni キャラクターを表示する UI Toolkit カスタム要素。View レイヤーの再利用可能な部品。
    /// </summary>
    /// <remarks>
    /// 依存関係: Resources/UI/FooniElement (UXML/USS), Resources/Characters/Fooni (Texture2D)
    /// 想定ライフサイクル: UI ドキュメントの生成時にインスタンス化され、<see cref="FUnity.Core.FUnityManager"/> から俳優要素とし
    ///     て利用される。UI Toolkit の仕様上、<c>[UxmlElement]</c> を付与するクラスは <c>partial</c> 修飾子が必須である。
    /// 背景画像の割当には <see cref="StyleBackground"/> の `new StyleBackground(Texture2D)` コンストラクタを使用する。
    /// </remarks>
    [UxmlElement]
    public partial class FooniElement : VisualElement
    {
        // NOTE:
        // FooniElement は見た目専用の UI 要素です。
        // アニメーションや時間変化は FooniController に集約しました（Visual Scripting から制御）。

        /// <summary>複製する UXML アセットの Resources 内パス。</summary>
        private const string VisualTreeResourcePath = "UI/FooniElement";

        /// <summary>適用する USS アセットの Resources 内パス。</summary>
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
            var visualTree = Resources.Load<VisualTreeAsset>(VisualTreeResourcePath);
            if (visualTree != null)
            {
                visualTree.CloneTree(this);
            }
            else
            {
                Debug.LogWarning($"FooniElement: Unable to load VisualTreeAsset at Resources/{VisualTreeResourcePath}.");
            }

            var styleSheet = Resources.Load<StyleSheet>(StyleResourcePath);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning($"FooniElement: Unable to load StyleSheet at Resources/{StyleResourcePath}.");
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
