// Updated: 2025-03-18
using System;
using UnityEngine;
using FUnity.Runtime.Core;
using FUnity.Runtime.UI;
using UnityEngine.UIElements;

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

        /// <summary>最後に適用した背景色。Configure 呼び出し時の即時再適用に利用する。</summary>
        private Color m_LastColor = Color.black;

        /// <summary>最後に適用した背景画像。null の場合は単色背景のみを表示する。</summary>
        private Texture2D m_LastTexture;

        /// <summary>最後に適用した背景スケール（"contain" / "cover"）。</summary>
        private string m_LastScaleKeyword = FUnityStageData.BackgroundScaleContain;

        /// <summary>直近で Resources から読み込んだ背景名。再読み込み時のログ抑制に利用する。</summary>
        private string m_LastResourceKey;

        /// <summary>背景用コンテナの名前。UI ビルダー上で識別しやすいよう定数化する。</summary>
        private const string BackgroundLayerName = StageElement.BackgroundLayerName;

        /// <summary>Resources/Backgrounds 配下の既定ファイル名を示す定数。</summary>
        private const string DefaultBackgroundResource = "Background_01";

        /// <summary>Resources.Load で探索する背景フォルダー名。</summary>
        private const string ResourceFolderName = "Backgrounds";

        /// <summary>背景スケール contain 用 USS クラス名。</summary>
        private const string BackgroundContainClass = "bg--contain";

        /// <summary>背景スケール cover 用 USS クラス名。</summary>
        private const string BackgroundCoverClass = "bg--cover";

        /// <summary>背景スケールを定義した USS の Resources パス。</summary>
        private const string BackgroundStyleSheetResourcePath = "UI/StageBackground";

        /// <summary>背景スケール USS のキャッシュ。</summary>
        private static StyleSheet s_BackgroundStyleSheet;

        /// <summary>USS の読み込みを一度でも試みたかどうか。</summary>
        private static bool s_BackgroundStyleSheetLoaded;

        /// <summary>USS 未適用を警告したかどうか。</summary>
        private static bool s_BackgroundStyleSheetWarningEmitted;

        /// <summary>inline background-size の自動解除状態を保持するための PropertyName。</summary>
        private static readonly PropertyName InlineGuardPropertyName = new PropertyName("FUnity.StageBackgroundService.InlineGuard");

        /// <summary>
        /// background-size をクリアするためのライフサイクル登録情報を保持するヘルパークラスです。
        /// </summary>
        private sealed class BackgroundInlineGuard
        {
            /// <summary>AttachToPanelEvent の重複登録を避けるためのフラグ。</summary>
            public bool AttachRegistered;

            /// <summary>GeometryChangedEvent の重複登録を避けるためのフラグ。</summary>
            public bool GeometryRegistered;

            /// <summary>AttachToPanelEvent 登録時に再利用するコールバック。</summary>
            public EventCallback<AttachToPanelEvent> AttachCallback;

            /// <summary>GeometryChangedEvent 登録時に再利用するコールバック。</summary>
            public EventCallback<GeometryChangedEvent> GeometryCallback;
        }

        /// <summary>
        /// 指定した背景要素の inline background-size を確実に未設定へ戻し、ライフサイクルでも再適用されないよう監視します。
        /// </summary>
        /// <param name="background">監視対象となる背景レイヤーの VisualElement。</param>
        public static void ForceClearInlineBackgroundSize(VisualElement background)
        {
            if (background == null)
            {
                return;
            }

            ClearInlineBackgroundSizeNow(background);

            background.schedule.Execute(() =>
            {
                ClearInlineBackgroundSizeNow(background);
            }).StartingIn(0);

            var guard = GetOrCreateInlineGuard(background);
            if (guard == null)
            {
                return;
            }

            if (!guard.AttachRegistered)
            {
                guard.AttachCallback = _ => ClearInlineBackgroundSizeNow(background);
                background.RegisterCallback(guard.AttachCallback, TrickleDown.NoTrickleDown);
                guard.AttachRegistered = true;
            }

            if (!guard.GeometryRegistered)
            {
                guard.GeometryCallback = _ => ClearInlineBackgroundSizeNow(background);
                background.RegisterCallback(guard.GeometryCallback, TrickleDown.NoTrickleDown);
                guard.GeometryRegistered = true;
            }
        }

        /// <summary>
        /// VisualElement に保存した inline クリア用のガード情報を取得または生成します。
        /// </summary>
        /// <param name="background">背景レイヤー要素。</param>
        /// <returns>既存または新規生成したガード情報。取得に失敗した場合は null。</returns>
        private static BackgroundInlineGuard GetOrCreateInlineGuard(VisualElement background)
        {
            if (background == null)
            {
                return null;
            }

            var stored = background.GetProperty(InlineGuardPropertyName);
            if (stored is BackgroundInlineGuard guard)
            {
                return guard;
            }

            guard = new BackgroundInlineGuard();
            background.SetProperty(InlineGuardPropertyName, guard);
            return guard;
        }

        /// <summary>
        /// 即時に inline の background-size / unityBackgroundScaleMode を未設定へ戻します。
        /// </summary>
        /// <param name="background">対象となる背景レイヤー。</param>
        private static void ClearInlineBackgroundSizeNow(VisualElement background)
        {
            if (background == null)
            {
                return;
            }

            background.style.backgroundSize = StyleKeyword.Null;
            background.style.unityBackgroundScaleMode = StyleKeyword.Null;
        }

        /// <summary>
        /// 任意の背景要素へステージデータを直接適用するユーティリティ。Presenter を経由しない呼び出し向けの簡易窓口です。
        /// </summary>
        /// <param name="background">適用対象となる背景レイヤー。</param>
        /// <param name="data">背景画像・色を含むステージ設定。</param>
        public static void Apply(VisualElement background, FUnityStageData data)
        {
            if (background == null || data == null)
            {
                return;
            }

            ForceClearInlineBackgroundSize(background);
            EnsureBackgroundStyleSheet(background);

            background.style.backgroundColor = data.BackgroundColor;

            if (data.BackgroundImage != null)
            {
                background.style.backgroundImage = new StyleBackground(data.BackgroundImage);
            }
            else
            {
                background.style.backgroundImage = StyleKeyword.None;
            }

            background.RemoveFromClassList(BackgroundContainClass);
            background.RemoveFromClassList(BackgroundCoverClass);

            var className = data.BackgroundScale == FUnityStageData.BackgroundScaleCover
                ? BackgroundCoverClass
                : BackgroundContainClass;
            background.AddToClassList(className);

            ForceClearInlineBackgroundSize(background);
        }

        /// <summary>
        /// 背景レイヤーを初期化し、必要に応じて既定の背景画像を読み込む。
        /// </summary>
        /// <param name="panelRoot">背景を貼り付ける UI Toolkit ルート要素。</param>
        /// <param name="backgroundName">初期適用する背景名。null を渡すと直前の状態を再適用する。</param>
        /// <param name="backgroundScale">背景スケールのキーワード。null なら直前の値を引き継ぐ。</param>
        public void Initialize(VisualElement panelRoot, string backgroundName = DefaultBackgroundResource, string backgroundScale = null)
        {
            if (panelRoot == null)
            {
                Debug.LogError("[FUnity.BG] panelRoot が null のため初期化できません。");
                return;
            }

            var normalizedScale = NormalizeScaleKeyword(backgroundScale ?? m_LastScaleKeyword);
            m_LastScaleKeyword = normalizedScale;

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
            ApplyBackgroundScaleClass(normalizedScale);

            if (!string.IsNullOrEmpty(backgroundName))
            {
                SetBackgroundFromResources(backgroundName, normalizedScale);
                return;
            }

            ApplyTexture(m_LastTexture);
        }

        /// <summary>
        /// 背景を適用する対象ルートを登録し、既知の背景設定があれば即時反映する。
        /// </summary>
        /// <param name="root">UI Document の <see cref="VisualElement.rootVisualElement"/>。</param>
        public void Configure(VisualElement root)
        {
            Initialize(root, null, m_LastScaleKeyword);
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
        /// <param name="backgroundScale">背景スケールのキーワード。null なら直前の値を再利用する。</param>
        /// <param name="preserveResourceKey">Resources 由来のキーを保持したい場合は true。</param>
        public void SetBackground(Texture2D texture, string backgroundScale = null, bool preserveResourceKey = false)
        {
            var normalizedScale = NormalizeScaleKeyword(backgroundScale ?? m_LastScaleKeyword);
            m_LastScaleKeyword = normalizedScale;
            m_LastTexture = texture;

            if (!preserveResourceKey)
            {
                m_LastResourceKey = null;
            }

            ApplyBackgroundScaleClass(normalizedScale);
            ApplyTexture(texture);
        }

        /// <summary>
        /// Resources/Backgrounds 配下のテクスチャを読み込み、背景へ適用する。
        /// </summary>
        /// <param name="backgroundName">拡張子なしのファイル名。null/空文字は無視する。</param>
        /// <param name="backgroundScale">背景スケールのキーワード。null なら直前の値を再利用する。</param>
        public void SetBackgroundFromResources(string backgroundName, string backgroundScale = null)
        {
            var normalizedScale = NormalizeScaleKeyword(backgroundScale ?? m_LastScaleKeyword);
            m_LastScaleKeyword = normalizedScale;
            ApplyBackgroundScaleClass(normalizedScale);

            if (string.IsNullOrEmpty(backgroundName))
            {
                Debug.LogWarning("[FUnity.BG] 背景名が空のため Resources から読み込めません。");
                return;
            }

            var resourcePath = $"{ResourceFolderName}/{backgroundName}";
            if (string.Equals(m_LastResourceKey, resourcePath, StringComparison.Ordinal) && m_LastTexture != null)
            {
                ApplyTexture(m_LastTexture);
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"[FUnity.BG] Resources/{resourcePath} が見つからず背景を変更できません。");
                m_LastTexture = null;
                m_LastResourceKey = null;
                ApplyTexture(null);
                return;
            }

            m_LastResourceKey = resourcePath;
            SetBackground(texture, normalizedScale, true);
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

            if (resourcesPath.IndexOf('/') < 0)
            {
                SetBackgroundFromResources(resourcesPath);
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture == null)
            {
                Debug.LogWarning($"[FUnity.BG] Resources/{resourcesPath} が見つからず背景を変更できません。");
                m_LastTexture = null;
                m_LastResourceKey = null;
                ApplyTexture(null);
                return;
            }

            m_LastResourceKey = resourcesPath;
            SetBackground(texture, null, true);
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
        private void ApplyTexture(Texture2D texture)
        {
            if (!EnsureBackgroundLayer())
            {
                return;
            }

            ForceClearInlineBackgroundSize(m_BackgroundLayer);
            m_BackgroundLayer.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);

            if (texture != null)
            {
                m_BackgroundLayer.style.backgroundImage = new StyleBackground(texture);
            }
            else
            {
                m_BackgroundLayer.style.backgroundImage = StyleKeyword.None;
            }

            ForceClearInlineBackgroundSize(m_BackgroundLayer);
            Debug.Log($"[FUnity.BGDiag] background scale='{m_LastScaleKeyword}', texture={(texture != null ? texture.name : "null")}");
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

            if (m_BackgroundLayer != null && m_BackgroundLayer.parent != m_TargetRoot)
            {
                m_BackgroundLayer.RemoveFromHierarchy();
                m_BackgroundLayer = null;
            }

            if (m_BackgroundLayer == null)
            {
                if (m_TargetRoot is StageElement stageElement)
                {
                    var stageBackground = stageElement.BackgroundLayer;
                    if (stageBackground != null)
                    {
                        m_BackgroundLayer = stageBackground;
                    }
                }

                if (m_BackgroundLayer == null)
                {
                    var existing = m_TargetRoot.Q<VisualElement>(BackgroundLayerName);
                    if (existing != null)
                    {
                        m_BackgroundLayer = existing;
                    }
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
            }

            m_BackgroundLayer.style.position = Position.Absolute;
            m_BackgroundLayer.style.left = 0f;
            m_BackgroundLayer.style.top = 0f;
            m_BackgroundLayer.style.right = 0f;
            m_BackgroundLayer.style.bottom = 0f;
            m_BackgroundLayer.style.flexGrow = 1f;
            m_BackgroundLayer.style.flexShrink = 0f;

            EnsureBackgroundStyleSheet(m_BackgroundLayer);

            if (m_BackgroundLayer.parent != m_TargetRoot)
            {
                m_BackgroundLayer.RemoveFromHierarchy();
                m_TargetRoot.Insert(0, m_BackgroundLayer);
            }
            else
            {
                var currentIndex = m_TargetRoot.IndexOf(m_BackgroundLayer);
                if (currentIndex > 0)
                {
                    m_TargetRoot.Remove(m_BackgroundLayer);
                    m_TargetRoot.Insert(0, m_BackgroundLayer);
                }
            }

            // USS 側の background-size 指定を活かすため、ここでも inline 値を念のため解除する。
            ForceClearInlineBackgroundSize(m_BackgroundLayer);

            return true;
        }

        /// <summary>
        /// 背景スケール用の USS クラスを適用する。
        /// </summary>
        /// <param name="scaleKeyword">適用するスケール種別。</param>
        private void ApplyBackgroundScaleClass(string scaleKeyword)
        {
            if (!EnsureBackgroundLayer())
            {
                return;
            }

            var styleSheetAvailable = EnsureBackgroundStyleSheet(m_BackgroundLayer);

            // USS に記述された background-size を有効にするため、毎回 inline 値を未設定へ戻す。
            ForceClearInlineBackgroundSize(m_BackgroundLayer);

            m_BackgroundLayer.RemoveFromClassList(BackgroundContainClass);
            m_BackgroundLayer.RemoveFromClassList(BackgroundCoverClass);

            var className = scaleKeyword == FUnityStageData.BackgroundScaleCover
                ? BackgroundCoverClass
                : BackgroundContainClass;
            m_BackgroundLayer.AddToClassList(className);

            ForceClearInlineBackgroundSize(m_BackgroundLayer);
            if (!styleSheetAvailable && !s_BackgroundStyleSheetWarningEmitted)
            {
                Debug.LogWarning($"[FUnity.BG] 背景スケール USS が適用されていないため、'{className}' の効果が反映されない可能性があります。");
                s_BackgroundStyleSheetWarningEmitted = true;
            }
        }

        /// <summary>
        /// 拡大率として扱える安全な背景スケール文字列へ正規化する。
        /// </summary>
        /// <param name="scale">正規化対象のスケール文字列。</param>
        /// <returns>"cover" 以外を "contain" へ丸めた結果。</returns>
        private static string NormalizeScaleKeyword(string scale)
        {
            return FUnityStageData.NormalizeBackgroundScale(scale);
        }

        /// <summary>
        /// 背景レイヤーに USS を追加し、未ロード時は Resources から読み込む。
        /// </summary>
        /// <param name="backgroundLayer">対象の背景レイヤー。</param>
        /// <returns>USS が適用できた場合は true。</returns>
        private static bool EnsureBackgroundStyleSheet(VisualElement backgroundLayer)
        {
            if (backgroundLayer == null)
            {
                return false;
            }

            if (s_BackgroundStyleSheet == null && !s_BackgroundStyleSheetLoaded)
            {
                s_BackgroundStyleSheet = Resources.Load<StyleSheet>(BackgroundStyleSheetResourcePath);
                s_BackgroundStyleSheetLoaded = true;

                if (s_BackgroundStyleSheet == null)
                {
                    Debug.LogWarning($"[FUnity.BG] Resources/{BackgroundStyleSheetResourcePath}.uss が見つからず、背景スケール USS を適用できません。");
                }
            }

            if (s_BackgroundStyleSheet == null)
            {
                return false;
            }

            var styleSheets = backgroundLayer.styleSheets;
            if (!ContainsStyleSheet(styleSheets, s_BackgroundStyleSheet))
            {
                styleSheets.Add(s_BackgroundStyleSheet);
            }

            return true;
        }

        /// <summary>
        /// 指定した StyleSheet が要素に既に登録されているか判定する。
        /// </summary>
        /// <param name="styleSheets">確認対象のスタイルシート集合。</param>
        /// <param name="styleSheet">調べるスタイルシート。</param>
        /// <returns>含まれていれば true。</returns>
        private static bool ContainsStyleSheet(VisualElementStyleSheetSet styleSheets, StyleSheet styleSheet)
        {
            if (styleSheets == null || styleSheet == null)
            {
                return false;
            }

            for (var i = 0; i < styleSheets.count; i++)
            {
                if (styleSheets[i] == styleSheet)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
