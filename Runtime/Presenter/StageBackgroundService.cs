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

        /// <summary>最後に適用した背景画像の拡大率。1.0f で 100% 表示を意味する。</summary>
        private Vector2 m_LastBackgroundScale = Vector2.one;

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
        /// <param name="backgroundScale">背景画像の拡大率。null を渡すと直前の値、未適用なら 100% を使用する。</param>
        public void Initialize(VisualElement panelRoot, string backgroundName = DefaultBackgroundResource, ScaleMode scale = ScaleMode.ScaleAndCrop, Vector2? backgroundScale = null)
        {
            if (panelRoot == null)
            {
                Debug.LogError("[FUnity.BG] panelRoot が null のため初期化できません。");
                return;
            }

            var effectiveScale = NormalizeScale(backgroundScale ?? m_LastBackgroundScale);
            m_LastBackgroundScale = effectiveScale;

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
                SetBackgroundFromResources(backgroundName, scale, m_LastBackgroundScale);
                return;
            }

            ApplyTexture(m_LastTexture, m_LastScaleMode, m_LastBackgroundScale);
        }

        /// <summary>
        /// 背景を適用する対象ルートを登録し、既知の背景設定があれば即時反映する。
        /// </summary>
        /// <param name="root">UI Document の <see cref="VisualElement.rootVisualElement"/>。</param>
        public void Configure(VisualElement root)
        {
            Initialize(root, null, m_LastScaleMode, m_LastBackgroundScale);
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
                SetBackground(stage.BackgroundImage, stage.BackgroundScaleMode, stage.BackgroundScale);
                return;
            }

            SetBackgroundFromResources(DefaultBackgroundResource, stage.BackgroundScaleMode, stage.BackgroundScale);
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
        /// <param name="backgroundScale">背景画像の拡大率。1.0f で 100% を意味する。</param>
        /// <param name="preserveResourceKey">Resources 由来のキーを保持したい場合は true。</param>
        public void SetBackground(Texture2D texture, ScaleMode scale, Vector2 backgroundScale, bool preserveResourceKey = false)
        {
            m_LastTexture = texture;
            m_LastScaleMode = scale;
            m_LastBackgroundScale = NormalizeScale(backgroundScale);
            if (!preserveResourceKey)
            {
                m_LastResourceKey = null;
            }
            ApplyTexture(texture, scale, m_LastBackgroundScale);
        }

        /// <summary>
        /// 旧 API 互換の背景設定。拡大率は 100% として扱う。
        /// </summary>
        /// <param name="texture">背景画像。</param>
        /// <param name="scale">スケールモード。</param>
        /// <param name="preserveResourceKey">Resources キーを保持するかどうか。</param>
        public void SetBackground(Texture2D texture, ScaleMode scale, bool preserveResourceKey = false)
        {
            SetBackground(texture, scale, Vector2.one, preserveResourceKey);
        }

        /// <summary>
        /// Resources/Backgrounds 配下のテクスチャを読み込み、背景へ適用する。
        /// </summary>
        /// <param name="backgroundName">拡張子なしのファイル名。null/空文字は無視する。</param>
        /// <param name="scale">テクスチャ適用時のスケールモード。</param>
        /// <param name="backgroundScale">背景画像の拡大率。</param>
        public void SetBackgroundFromResources(string backgroundName, ScaleMode scale, Vector2 backgroundScale)
        {
            if (string.IsNullOrEmpty(backgroundName))
            {
                Debug.LogWarning("[FUnity.BG] 背景名が空のため Resources から読み込めません。");
                return;
            }

            m_LastBackgroundScale = NormalizeScale(backgroundScale);

            var resourcePath = $"{ResourceFolderName}/{backgroundName}";
            if (string.Equals(m_LastResourceKey, resourcePath) && m_LastTexture != null)
            {
                SetBackground(m_LastTexture, scale, m_LastBackgroundScale, true);
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[FUnity.BG] Resources/{resourcePath} が見つからず背景を変更できません。");
                m_LastScaleMode = scale;
                m_LastTexture = null;
                m_LastResourceKey = null;
                ApplyTexture(null, scale, m_LastBackgroundScale);
                return;
            }

            m_LastResourceKey = resourcePath;
            SetBackground(texture, scale, m_LastBackgroundScale, true);
        }

        /// <summary>
        /// 旧 API 互換の Resources 背景読み込み。拡大率は最後に適用した値を再利用する。
        /// </summary>
        /// <param name="backgroundName">背景名。</param>
        /// <param name="scale">スケールモード。</param>
        public void SetBackgroundFromResources(string backgroundName, ScaleMode scale)
        {
            SetBackgroundFromResources(backgroundName, scale, m_LastBackgroundScale);
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
                SetBackgroundFromResources(resourcesPath, m_LastScaleMode, m_LastBackgroundScale);
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture == null)
            {
                Debug.LogWarning($"[FUnity.BG] Resources/{resourcesPath} が見つからず背景を変更できません。");
                m_LastTexture = null;
                m_LastResourceKey = null;
                ApplyTexture(null, m_LastScaleMode, m_LastBackgroundScale);
                return;
            }

            m_LastResourceKey = resourcesPath;
            SetBackground(texture, m_LastScaleMode, m_LastBackgroundScale, true);
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
        /// <param name="backgroundScale">背景画像の拡大率。</param>
        private void ApplyTexture(Texture2D texture, ScaleMode scale, Vector2 backgroundScale)
        {
            if (!EnsureBackgroundLayer())
            {
                return;
            }

            var normalizedScale = NormalizeScale(backgroundScale);
            var widthLength = new Length(normalizedScale.x * 100f, LengthUnit.Percent);
            var heightLength = new Length(normalizedScale.y * 100f, LengthUnit.Percent);
            m_BackgroundLayer.style.backgroundSize = new BackgroundSize(widthLength, heightLength);

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

            Debug.Log($"[FUnity.BGDiag] backgroundSize=({widthLength.value}%, {heightLength.value}%), scaleMode={scale}, texture={(texture != null ? texture.name : "null")}");
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
            }

            // ルート側のスタイルリセットや UI Toolkit バージョン差分に備え、都度絶対配置を再適用する。
            m_BackgroundLayer.style.position = Position.Absolute;
            m_BackgroundLayer.style.left = 0f;
            m_BackgroundLayer.style.top = 0f;
            m_BackgroundLayer.style.right = 0f;
            m_BackgroundLayer.style.bottom = 0f;
            m_BackgroundLayer.style.flexGrow = 1f;
            m_BackgroundLayer.style.flexShrink = 0f;

            if (m_BackgroundLayer.parent != m_TargetRoot)
            {
                m_BackgroundLayer.RemoveFromHierarchy();
                m_TargetRoot.Insert(0, m_BackgroundLayer);
            }

            return true;
        }

        /// <summary>
        /// 拡大率として扱える安全な値へ正規化します。
        /// </summary>
        /// <param name="scale">正規化対象のスケール。</param>
        /// <returns>負値を 0 に丸め、0 ベクトルの場合は (1,1) を返却した結果。</returns>
        private static Vector2 NormalizeScale(Vector2 scale)
        {
            var x = Mathf.Max(0f, scale.x);
            var y = Mathf.Max(0f, scale.y);
            if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
            {
                return Vector2.one;
            }

            return new Vector2(x, y);
        }
    }
}
