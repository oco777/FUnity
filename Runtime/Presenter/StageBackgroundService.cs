// Updated: 2025-03-10
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// ステージ背景を UI Toolkit のルート要素へ反映するための軽量サービス。
    /// Presenter や Visual Scripting から呼び出し可能な安全な窓口を提供する。
    /// </summary>
    public sealed class StageBackgroundService
    {
        /// <summary>背景を敷き詰める親の UI ルート要素。null の場合はすべての処理を無視する。</summary>
        private VisualElement m_TargetRoot;

        /// <summary>背景画像と背景色を描画する専用レイヤー。常に UI ルートの最背面に配置する。</summary>
        private VisualElement m_BackgroundLayer;

        /// <summary>最後に適用した背景色。`Configure` 呼び出し時の即時再適用に利用する。</summary>
        private Color m_LastColor = Color.black;

        /// <summary>最後に適用した背景画像。null の場合は単色背景のみを表示する。</summary>
        private Texture2D m_LastTexture;

        /// <summary>最後に適用した背景画像のスケールモード。テクスチャ未指定時は ScaleAndCrop を既定とする。</summary>
        private ScaleMode m_LastScaleMode = ScaleMode.ScaleAndCrop;

        /// <summary>直近で Resources から読み込んだ背景名。再読み込み時のログ抑制に利用する。</summary>
        private string m_LastResourceKey;

        /// <summary>背景用コンテナの名前。UI ビルダー上で識別しやすいよう定数化する。</summary>
        private const string BackgroundLayerName = "FUnityBackgroundLayer";

        /// <summary>Resources/Backgrounds 配下の既定ファイル名を示す定数。</summary>
        private const string DefaultBackgroundResource = "Background_01";

        /// <summary>Resources.Load で探索する背景フォルダー名。</summary>
        private const string ResourceFolderName = "Backgrounds";

        /// <summary>
        /// 背景レイヤーを初期化し、必要に応じて既定の背景画像を読み込む。
        /// </summary>
        /// <param name="panelRoot">背景を貼り付ける UI Toolkit ルート要素。</param>
        /// <param name="backgroundName">初期適用する背景名。null を渡すと直前の状態を再適用する。</param>
        /// <param name="scale">背景画像を読み込む際に使用するスケールモード。</param>
        public void Initialize(VisualElement panelRoot, string backgroundName = DefaultBackgroundResource, ScaleMode scale = ScaleMode.ScaleAndCrop)
        {
            if (panelRoot == null)
            {
                Debug.LogError("[FUnity.BG] panelRoot が null のため初期化できません。");
                return;
            }

            if (m_TargetRoot != panelRoot)
            {
                if (m_BackgroundLayer != null)
                {
                    m_BackgroundLayer.RemoveFromHierarchy();
                }
                m_TargetRoot = panelRoot;
                m_BackgroundLayer = null;
            }

            if (!EnsureBackgroundLayer())
            {
                return;
            }

            ApplyColor(m_LastColor);

            if (!string.IsNullOrEmpty(backgroundName))
            {
                SetBackgroundFromResources(backgroundName, scale);
                return;
            }

            ApplyTexture(m_LastTexture, m_LastScaleMode);
        }

        /// <summary>
        /// 背景を適用する対象ルートを登録し、既知の背景設定があれば即時反映する。
        /// </summary>
        /// <param name="root">UI Document の <see cref="VisualElement.rootVisualElement"/>。</param>
        public void Configure(VisualElement root)
        {
            Initialize(root, null, m_LastScaleMode);
        }

        /// <summary>
        /// ステージ設定全体を一括適用するユーティリティ。色→画像の順で反映する。
        /// </summary>
        /// <param name="stage">背景色・テクスチャを保持する <see cref="FUnityStageData"/>。</param>
        public void ApplyStage(FUnityStageData stage)
        {
            if (stage == null)
            {
                return;
            }

            SetBackgroundColor(stage.BackgroundColor);

            if (stage.BackgroundImage != null)
            {
                SetBackground(stage.BackgroundImage, stage.BackgroundScale);
                return;
            }

            SetBackgroundFromResources(DefaultBackgroundResource, stage.BackgroundScale);
        }

        /// <summary>
        /// 背景色を更新し、背景レイヤーへ即座に適用する。
        /// </summary>
        /// <param name="color">適用する色。</param>
        public void SetBackgroundColor(Color color)
        {
            m_LastColor = color;
            ApplyColor(color);
        }

        /// <summary>
        /// 指定したテクスチャを背景に設定する。null を渡すと背景画像をクリアする。
        /// </summary>
        /// <param name="texture">背景に使用するテクスチャ。</param>
        /// <param name="scale">UI Toolkit の background scale mode。</param>
        /// <param name="preserveResourceKey">Resources 由来のキーを保持したい場合は true。</param>
        public void SetBackground(Texture2D texture, ScaleMode scale, bool preserveResourceKey = false)
        {
            m_LastTexture = texture;
            m_LastScaleMode = scale;
            if (!preserveResourceKey)
            {
                m_LastResourceKey = null;
            }
            ApplyTexture(texture, scale);
        }

        /// <summary>
        /// Resources/Backgrounds 配下のテクスチャを読み込み、背景へ適用する。
        /// </summary>
        /// <param name="backgroundName">拡張子なしのファイル名。null/空文字は無視する。</param>
        /// <param name="scale">テクスチャ適用時のスケールモード。</param>
        public void SetBackgroundFromResources(string backgroundName, ScaleMode scale)
        {
            if (string.IsNullOrEmpty(backgroundName))
            {
                Debug.LogWarning("[FUnity.BG] 背景名が空のため Resources から読み込めません。");
                return;
            }

            var resourcePath = $"{ResourceFolderName}/{backgroundName}";
            if (string.Equals(m_LastResourceKey, resourcePath) && m_LastTexture != null)
            {
                SetBackground(m_LastTexture, scale, true);
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[FUnity.BG] Resources/{resourcePath} が見つからず背景を変更できません。");
                m_LastScaleMode = scale;
                m_LastTexture = null;
                m_LastResourceKey = null;
                ApplyTexture(null, scale);
                return;
            }

            m_LastResourceKey = resourcePath;
            SetBackground(texture, scale, true);
        }

        /// <summary>
        /// 旧 API 互換の Resources パス指定で背景画像を設定する。
        /// </summary>
        /// <param name="resourcesPath">`Resources/` 直下からのパス。</param>
        public void SetBackground(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                Debug.LogWarning("[FUnity.BG] resourcesPath が空のため背景を変更できません。");
                return;
            }

            if (!resourcesPath.Contains("/"))
            {
                SetBackgroundFromResources(resourcesPath, m_LastScaleMode);
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture == null)
            {
                Debug.LogWarning($"[FUnity.BG] Resources/{resourcesPath} が見つからず背景を変更できません。");
                m_LastTexture = null;
                m_LastResourceKey = null;
                ApplyTexture(null, m_LastScaleMode);
                return;
            }

            m_LastResourceKey = resourcesPath;
            SetBackground(texture, m_LastScaleMode, true);
        }

        /// <summary>
        /// 内部状態に保存された色を背景レイヤーへ適用する。
        /// </summary>
        /// <param name="color">適用する色。</param>
        private void ApplyColor(Color color)
        {
            if (!EnsureBackgroundLayer())
            {
                return;
            }

            m_BackgroundLayer.style.backgroundColor = color;
        }

        /// <summary>
        /// テクスチャを背景に適用し、null 時は背景画像を解除する。
        /// </summary>
        /// <param name="texture">背景画像。</param>
        /// <param name="scale">スケールモード。</param>
        private void ApplyTexture(Texture2D texture, ScaleMode scale)
        {
            if (!EnsureBackgroundLayer())
            {
                return;
            }

            if (texture != null)
            {
                m_BackgroundLayer.style.backgroundImage = new StyleBackground(texture);
                m_BackgroundLayer.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                m_BackgroundLayer.style.unityBackgroundScaleMode = scale;
            }
            else
            {
                m_BackgroundLayer.style.backgroundImage = StyleKeyword.None;
                m_BackgroundLayer.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                m_BackgroundLayer.style.unityBackgroundScaleMode = scale;
            }
        }

        /// <summary>
        /// 背景レイヤーを生成し、UI ルートの最背面に挿入する。既に生成済みなら再利用する。
        /// </summary>
        /// <returns>レイヤーの確保に成功した場合は <c>true</c>。</returns>
        private bool EnsureBackgroundLayer()
        {
            if (m_TargetRoot == null)
            {
                return false;
            }

            if (m_BackgroundLayer == null)
            {
                m_BackgroundLayer = new VisualElement
                {
                    name = BackgroundLayerName,
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };

                m_BackgroundLayer.style.position = Position.Absolute;
                m_BackgroundLayer.style.left = 0f;
                m_BackgroundLayer.style.top = 0f;
                m_BackgroundLayer.style.right = 0f;
                m_BackgroundLayer.style.bottom = 0f;
                m_BackgroundLayer.style.flexGrow = 0f;
                m_BackgroundLayer.style.flexShrink = 0f;
            }

            if (m_BackgroundLayer.parent != m_TargetRoot)
            {
                m_BackgroundLayer.RemoveFromHierarchy();
                m_TargetRoot.Insert(0, m_BackgroundLayer);
            }

            return true;
        }
    }
}
