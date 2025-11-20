// Updated: 2025-03-03
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Core;
using FUnity.Runtime.Input;
using FUnity.Runtime.Model;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Rendering;

namespace FUnity.Runtime.View
{
    /// <summary>
    /// 俳優の見た目と座標を UI Toolkit 上に投影する View レイヤーの薄いアダプタ。
    /// Presenter が指示する座標と画像のみを反映し、状態やロジックは保持しない。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FooniUIBridge"/>（UI Toolkit 要素への操作ラッパー）
    /// 想定ライフサイクル: <see cref="FUnity.Runtime.Core.FUnityManager"/> が生成した UI GameObject にアタッチされ、<see cref="FUnity.Runtime.Presenter.ActorPresenter"/>
    ///     の初期化時に <see cref="Configure(FooniUIBridge, VisualElement)"/> で結線される。
    /// ビューは常に Presenter からの一方向更新のみを許容し、ユーザー入力は受け付けない。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ActorView : MonoBehaviour, IActorView
    {
        /// <summary>
        /// ステージ境界が更新された際に Presenter へ通知するイベント。<see cref="Configure"/> 直後とジオメトリ変化時に発火する。
        /// </summary>
        public event Action<Rect> StageBoundsChanged;

        /// <summary>
        /// UI Toolkit 要素へ座標・背景画像を反映するためのブリッジ。
        /// <see cref="Configure(FooniUIBridge, VisualElement)"/> で差し替え可能。
        /// </summary>
        [SerializeField]
        private FooniUIBridge m_Bridge;

        /// <summary>
        /// 俳優 UI をホストする <see cref="UIDocument"/>。ドキュメント直下のレイアウト初期化に利用する。
        /// </summary>
        [SerializeField]
        private UIDocument m_Document;

        /// <summary>InitialPosition を適用する俳優コンテナ（#root / .actor-root）。</summary>
        private VisualElement m_ActorRoot;

        /// <summary>
        /// Presenter から指定された UI Toolkit 要素。`Configure` 呼び出し後に <see cref="SetSprite"/> が利用する。
        /// </summary>
        private VisualElement m_BoundElement;

        /// <summary>サイズ・吹き出し適用対象となるルート要素。</summary>
        private VisualElement m_RootElement;

        /// <summary>回転および背景差し替えに利用するポートレート要素。</summary>
        private VisualElement m_PortraitElement;

        /// <summary>ポートレート表示専用の Image 要素。Sprite を直接描画するために使用する。</summary>
        private Image m_PortraitImage;

        /// <summary>色相回転を適用する際に使用するレンダリングパイプライン。</summary>
        private HueShiftPipeline m_HuePipeline;

        /// <summary>描画効果をかける前の元テクスチャ。Image.image や backgroundImage から取得してキャッシュする。</summary>
        private Texture m_SourceTexture;

        /// <summary>元テクスチャをすでに取得できているかどうかのフラグ。</summary>
        private bool m_SourceCaptured;

        /// <summary>現在の回転角度（度）。中心ピボットを基準に UI へ適用する。</summary>
        private float m_CurrentRotationDeg;

        /// <summary>現在の中心座標（px）。左上原点で右+ / 下+。</summary>
        private Vector2 m_CurrentCenterPx = Vector2.zero;

        /// <summary>吹き出し全体を包む VisualElement。</summary>
        private VisualElement m_SpeechBubble;

        /// <summary>吹き出しテキストを表示するラベル。</summary>
        private Label m_SpeechLabel;

        /// <summary>パネル全体の GeometryChangedEvent を購読する際に利用するルート要素。</summary>
        private VisualElement m_PanelRoot;

        /// <summary>GeometryChangedEvent の登録済みかどうかを示すフラグ。</summary>
        private bool m_GeometryCallbacksRegistered;

        /// <summary>現在適用している等倍基準のスケール値。</summary>
        private float m_CurrentScale = 1f;

        /// <summary>左右反転のためのスケール符号。+1 で通常、-1 で反転。</summary>
        private float m_FlipSignX = 1f;

        /// <summary>GeometryChangedEvent で観測した直近の worldBound。</summary>
        private Rect m_WorldBoundCache = Rect.zero;

        /// <summary>有効な worldBound をキャッシュできているかどうか。</summary>
        private bool m_HasWorldBoundCache;

        /// <summary>worldBound キャッシュ更新のために監視している要素群。</summary>
        private readonly List<VisualElement> m_WorldBoundSources = new List<VisualElement>();

        /// <summary>直近の可視状態。style.display から評価できない場合のフォールバックに利用します。</summary>
        private bool m_IsVisible = true;

        /// <summary>transform-origin を中央 (50%/50%) へ固定するためのパーセント指定。</summary>
        private static readonly Length CenterPivotPercent = new Length(50f, LengthUnit.Percent);

        /// <summary>transform-origin を中央へ固定するためのスタイル値。</summary>
        private static readonly StyleTransformOrigin CenterTransformOrigin = new StyleTransformOrigin(
            new TransformOrigin(CenterPivotPercent, CenterPivotPercent, 0f));

        /// <summary>ポートレートのスケールを初期化する際に使用する等倍スタイル。</summary>
        private static readonly StyleScale IdentityScale = new StyleScale(new Scale(Vector3.one));

        /// <summary>UI Toolkit の scale に適用する下限値。</summary>
        private const float MinimumScale = 0.01f;

        /// <summary>吹き出しの最大幅（px）。レイアウト初期化時に利用する。</summary>
        private const float SpeechMaxWidthPx = 240f;

        /// <summary>吹き出し文字の基準フォントサイズ（px）。</summary>
        private const float SpeechBaseFontPx = 14f;

#if UNITY_2022_3_OR_NEWER
        /// <summary>
        /// UI Toolkit の `style.scale` を参照し、XY 成分のみを Vector2 として安全に取得するヘルパー。
        /// 未解決・未設定時はフォールバック値を返し、0 近傍の値は厳密に 0 として扱う。
        /// </summary>
        /// <param name="element">スケールを取得する対象要素。</param>
        /// <param name="fallback">要素や値が未設定だった場合に使用する等倍スケール。</param>
        /// <returns>XY 成分を正規化したスケール値。</returns>
        private static Vector2 ResolveScaleXY(VisualElement element, float fallback = 1f)
        {
            var resolved = element != null
                ? element.resolvedStyle.scale.value
                : new Vector3(fallback, fallback, 1f);
            var resolvedX = float.IsNaN(resolved.x) ? fallback : resolved.x;
            var resolvedY = float.IsNaN(resolved.y) ? fallback : resolved.y;
            var scaleX = Mathf.Approximately(resolvedX, 0f) ? 0f : resolvedX;
            var scaleY = Mathf.Approximately(resolvedY, 0f) ? 0f : resolvedY;
            return new Vector2(scaleX, scaleY);
        }

        /// <summary>
        /// UI Toolkit の `style.scale` へ XY 成分を Vector3(x, y, 1) として適用するヘルパー。
        /// Z 成分は 1 固定とし、2D UI における期待どおりの拡縮を保証する。
        /// </summary>
        /// <param name="element">スケールを適用する対象要素。</param>
        /// <param name="scaleX">X 方向のスケール値。</param>
        /// <param name="scaleY">Y 方向のスケール値。</param>
        private static void ApplyScaleXY(VisualElement element, float scaleX, float scaleY)
        {
            if (element == null)
            {
                return;
            }

            element.style.scale = new StyleScale(new Scale(new Vector3(scaleX, scaleY, 1f)));
        }
#else
        /// <summary>
        /// Unity 2021.x 以前では resolvedStyle.scale が未提供のため、常にフォールバック値を返すヘルパー。
        /// </summary>
        /// <param name="element">未使用。</param>
        /// <param name="fallback">返却する等倍スケール。</param>
        /// <returns>フォールバックとして渡された等倍スケール値。</returns>
        private static Vector2 ResolveScaleXY(VisualElement element, float fallback = 1f)
        {
            return new Vector2(fallback, fallback);
        }

        /// <summary>
        /// Unity 2021.x 以前では UI Toolkit の style.scale が利用できないため、ダミー実装として何も行わない。
        /// </summary>
        /// <param name="element">未使用。</param>
        /// <param name="scaleX">未使用。</param>
        /// <param name="scaleY">未使用。</param>
        private static void ApplyScaleXY(VisualElement element, float scaleX, float scaleY)
        {
        }
#endif

        /// <summary>
        /// Presenter からバインドされた VisualElement。Visual Scripting の Graph Variables へ登録する際に利用する。
        /// </summary>
        public VisualElement BoundElement => m_BoundElement;

        /// <summary>
        /// 俳優コンテナのルート要素。未解決の場合は <see cref="BoundElement"/> を返す。
        /// </summary>
        public VisualElement ActorRoot => m_ActorRoot ?? m_BoundElement;

        /// <summary>
        /// 現在の可視状態を返します。style.display が None の場合は false を返します。
        /// </summary>
        public bool IsVisible
        {
            get
            {
                var target = m_RootElement ?? m_BoundElement;
                if (target == null)
                {
                    return m_IsVisible;
                }

                return target.resolvedStyle.display != DisplayStyle.None;
            }
        }

        /// <summary>
        /// 直近のレイアウト確定時に観測した worldBound を返す。幅・高さが正の場合のみ有効とみなす。
        /// </summary>
        /// <param name="rect">キャッシュ済みの worldBound。</param>
        /// <returns>有効なキャッシュが存在する場合は <c>true</c>。</returns>
        public bool TryGetCachedWorldBound(out Rect rect)
        {
            rect = m_WorldBoundCache;
            return m_HasWorldBoundCache && rect.width > 0f && rect.height > 0f;
        }

        /// <summary>
        /// UIDocument をキャッシュし、ドキュメント直下のルートを相対配置へ戻す。
        /// </summary>
        private void Awake()
        {
            if (m_Document == null)
            {
                m_Document = GetComponent<UIDocument>();
            }

            var docRoot = m_Document != null ? m_Document.rootVisualElement : null;
            if (docRoot != null)
            {
                docRoot.style.position = Position.Relative;
                docRoot.style.left = StyleKeyword.Auto;
                docRoot.style.top = StyleKeyword.Auto;
            }
        }

        /// <summary>
        /// コンポーネントがリセットされた際に既存の <see cref="FooniUIBridge"/> や <see cref="UIDocument"/> を再取得する。
        /// </summary>
        private void Reset()
        {
            if (m_Bridge == null)
            {
                m_Bridge = GetComponent<FooniUIBridge>();
            }

            if (m_Document == null)
            {
                m_Document = GetComponent<UIDocument>();
            }
        }

        /// <summary>
        /// Presenter から受け取ったブリッジと UI 要素を紐付け、以降の描画命令を受け付ける。
        /// </summary>
        /// <param name="bridge">座標更新を委譲する <see cref="FooniUIBridge"/>。null 時は自身に付随するブリッジを利用する。</param>
        /// <param name="element">UI Toolkit 側で描画対象となる要素。</param>
        /// <example>
        /// <code>
        /// var view = gameObject.AddComponent<ActorView>();
        /// view.Configure(existingBridge, actorElement);
        /// </code>
        /// </example>
        public void Configure(FooniUIBridge bridge, VisualElement element)
        {
            if (bridge != null)
            {
                m_Bridge = bridge;
            }
            else if (m_Bridge == null)
            {
                m_Bridge = GetComponent<FooniUIBridge>();
                if (m_Bridge == null)
                {
                    m_Bridge = gameObject.AddComponent<FooniUIBridge>();
                }
            }

            UnregisterGeometryCallbacks();
            UnregisterWorldBoundCacheSources();

            m_ActorRoot = FindActorRoot(element);
            if (m_ActorRoot == null && element != null)
            {
                Debug.LogWarning("[FUnity.ActorView] 俳優コンテナ (#root / .actor-root) を特定できなかったため、初期座標の適用をスキップします。");
            }

            if (m_ActorRoot != null)
            {
                m_ActorRoot.style.position = Position.Absolute;
                ApplyCenterPivot(m_ActorRoot);
            }

            if (m_Bridge != null)
            {
                if (m_ActorRoot != null)
                {
                    m_Bridge.BindElement(m_ActorRoot);
                }
                else
                {
                    m_Bridge.BindElement(null);
                }
            }

            var searchBase = m_ActorRoot ?? element;
            m_BoundElement = searchBase;
            m_RootElement = searchBase;
            if (m_RootElement != null)
            {
                ApplyCenterPivot(m_RootElement);
                EnsurePortraitImage();
                EnsureSpeechElements();
            }
            else
            {
                m_PortraitImage = null;
                m_SpeechBubble = null;
                m_SpeechLabel = null;
            }

            InvalidateSourceTexture();
            HideSpeech();
            ResetEffects();

            RegisterWorldBoundCacheSources();

            m_PortraitElement = null;
            ResetPortraitScaleToIdentity();

            RegisterGeometryCallbacks();
            ApplyAllTransforms();
        }

        /// <summary>
        /// 俳優設定を View へ適用し、Sprites に登録された Sprite を描画する。
        /// Sprites が空の場合は画像を解除し、旧フィールドは参照しない。
        /// </summary>
        /// <param name="actorData">表示対象となる俳優設定。null の場合は画像を全て解除する。</param>
        public void ApplyActorData(FUnityActorData actorData)
        {
            ApplyActorData(actorData, 0);
        }

        /// <summary>
        /// 俳優設定とインデックスを指定して View へ適用する。Sprites[index] を描画し、範囲外の場合は丸め込む。
        /// </summary>
        /// <param name="actorData">表示対象となる俳優設定。</param>
        /// <param name="spriteIndex">表示したい Sprite のインデックス。</param>
        public void ApplyActorData(FUnityActorData actorData, int spriteIndex)
        {
            EnsurePortraitImage();

            var root = GetRootElement();
            if (root != null)
            {
                root.style.backgroundImage = new StyleBackground();
            }

            if (actorData == null)
            {
                SetSprite(null);
                return;
            }

            var sprites = actorData.Sprites;
            if (sprites == null || sprites.Count == 0)
            {
                SetSprite(null);
                return;
            }

            var safeIndex = Mathf.Clamp(spriteIndex, 0, sprites.Count - 1);
            SetSprite(sprites[safeIndex]);
        }

        /// <summary>
        /// Presenter 参照を UI ブリッジに伝達し、旧 API 互換のフォールバック経路を確保する。
        /// </summary>
        /// <param name="presenter">紐付ける <see cref="ActorPresenter"/>。</param>
        public void SetActorPresenter(ActorPresenter presenter)
        {
            if (m_Bridge == null)
            {
                return;
            }

            m_Bridge.SetActorPresenter(presenter);
        }

        /// <summary>
        /// Presenter から通知された中心座標（px）を受け取り、#root の left/top を中心基準で配置する。
        /// </summary>
        /// <param name="centerPx">UI ステージ左上を原点とした中心座標（px）。</param>
        /// <remarks>
        /// スケール適用後の見た目サイズを取得し、<c>center − scaledSize / 2</c> で left/top を算出する。
        /// これにより、中心がステージ境界へ移動した際にちょうど画像が見切れる。
        /// </remarks>
        /// <example>
        /// <code>
        /// m_ActorView.SetCenterPosition(centerPx);
        /// </code>
        /// </example>
        public void SetCenterPosition(Vector2 centerPx)
        {
            m_CurrentCenterPx = centerPx;
            UpdateLayoutForCenter();
        }

        /// <summary>
        /// 現在推定できるステージ境界を返す。UI 要素とパネルがバインドされていない場合は失敗する。
        /// </summary>
        /// <param name="boundsPx">左上原点ベースの境界。</param>
        /// <returns>取得できた場合は <c>true</c>。</returns>
        public bool TryGetStageBounds(out Rect boundsPx)
        {
            boundsPx = default;
            if (m_Bridge == null)
            {
                return false;
            }

            return m_Bridge.TryGetPanelBounds(out boundsPx);
        }

        /// <summary>
        /// 現在の俳優要素のピクセルサイズを返す。レイアウト前は既定値を返す。
        /// </summary>
        /// <param name="sizePx">幅・高さ（px）。</param>
        /// <returns>取得できた場合は <c>true</c>。</returns>
        public bool TryGetVisualSize(out Vector2 sizePx)
        {
            sizePx = default;
            if (m_Bridge == null)
            {
                return false;
            }

            return m_Bridge.TryGetElementPixelSize(out sizePx);
        }

        /// <summary>
        /// #root 要素のレイアウトサイズへ scale を掛け合わせ、見た目上の実寸（px）を算出する。
        /// Scratch クランプおよび表示位置の決定に利用するため、ポートレート側のサイズ/スケールは参照しない。
        /// </summary>
        /// <returns>#root のスケール適用後サイズ（px）。未解決時は <see cref="Vector2.zero"/>。</returns>
        public Vector2 GetRootScaledSizePx()
        {
            var root = GetRootElement();
            if (root == null)
            {
                return Vector2.zero;
            }

            var layoutSize = root.layout.size;
            var width = NormalizeLength(layoutSize.x, root.resolvedStyle.width);
            var height = NormalizeLength(layoutSize.y, root.resolvedStyle.height);

#if UNITY_2022_3_OR_NEWER
            var scaleValue = ResolveScaleXY(root, 1f);
            var scaleX = Mathf.Abs(scaleValue.x);
            var scaleY = Mathf.Abs(scaleValue.y);
            return new Vector2(width * scaleX, height * scaleY);
#else
            var scaledWidth = width * m_CurrentScale;
            var scaledHeight = height * m_CurrentScale;
            return new Vector2(scaledWidth, scaledHeight);
#endif
        }

        /// <summary>
        /// 旧 API 互換のために残すラッパー。内部的には <see cref="GetRootScaledSizePx"/> を呼び出す。
        /// </summary>
        /// <returns>#root のスケール適用後サイズ（px）。</returns>
        public Vector2 GetScaledSizePx()
        {
            return GetRootScaledSizePx();
        }

        /// <summary>
        /// Sprite を UI Toolkit Image へ設定し、Texture2D 互換フィールドへは依存しない描画を行う。
        /// </summary>
        /// <param name="sprite">表示したい Sprite。null の場合は画像を解除する。</param>
        public void SetSprite(Sprite sprite)
        {
            EnsurePortraitImage();

            if (m_PortraitImage != null)
            {
                if (sprite != null)
                {
                    m_PortraitImage.sprite = sprite;
                    m_PortraitImage.image = null;
                }
                else
                {
                    m_PortraitImage.sprite = null;
                    m_PortraitImage.image = null;
                }
            }

            var portrait = ResolvePortraitElement();
            if (portrait != null && portrait != m_PortraitImage)
            {
                portrait.style.backgroundImage = new StyleBackground();
                portrait.style.backgroundColor = StyleKeyword.Null;
            }

            var root = GetRootElement();
            if (root != null)
            {
                root.style.backgroundImage = new StyleBackground();
            }

            InvalidateSourceTexture();
        }

        /// <summary>
        /// 指定したサイズをルート要素へ適用する。
        /// </summary>
        /// <param name="size">幅・高さ（px）。</param>
        public void SetSize(Vector2 size)
        {
            if (m_RootElement == null)
            {
                return;
            }

            if (size.x > 0f)
            {
                m_RootElement.style.width = size.x;
            }
            else
            {
                m_RootElement.style.width = StyleKeyword.Auto;
            }

            if (size.y > 0f)
            {
                m_RootElement.style.height = size.y;
            }
            else
            {
                m_RootElement.style.height = StyleKeyword.Auto;
            }

            UpdateLayoutForCenter();
            NotifyStageBounds();
        }

        /// <summary>
        /// スケールを UI Toolkit の `style.scale` で適用する。
        /// </summary>
        /// <param name="scale">適用するスケール。</param>
        public void SetScale(float scale)
        {
            var safeScale = Mathf.Max(MinimumScale, scale);
            m_CurrentScale = safeScale;
            ApplyScaleToRoot();
            UpdateLayoutForCenter();
            NotifyStageBounds();
        }

        /// <summary>
        /// 拡大率（%）を等倍スケールへ変換し、#root 要素に対して拡縮を適用する。
        /// </summary>
        /// <param name="percent">100 で等倍となる拡大率（%）。</param>
        public void SetSizePercent(float percent)
        {
            var safeScale = Mathf.Max(MinimumScale, percent / 100f);
            SetScale(safeScale);
        }

        /// <summary>
        /// 指定された角度でポートレート要素を回転させる。中心ピボットを固定し、UITK の rotate プロパティを使用する。
        /// </summary>
        /// <param name="degrees">適用する角度（度）。0～360 度の範囲を想定する。</param>
        public void SetRotationDegrees(float degrees)
        {
            m_CurrentRotationDeg = NormalizeDegrees(degrees);
            ApplyRotationToRoot();
        }

        /// <summary>
        /// 左右反転の符号を指定し、UI Toolkit の scale.x に即時反映する。
        /// </summary>
        /// <param name="sign">+1 で通常、-1 で反転。0 近傍は +1 として扱う。</param>
        public void SetHorizontalFlipSign(float sign)
        {
            var normalized = Mathf.Approximately(sign, 0f) ? 1f : Mathf.Sign(sign);
            if (Mathf.Approximately(normalized, m_FlipSignX))
            {
                return;
            }

            m_FlipSignX = normalized;
            ApplyScaleToRoot();
        }

        /// <summary>
        /// 俳優要素の可視状態を切り替える。style.display を直接操作し、Flex/None で表示を制御する。
        /// </summary>
        /// <param name="visible">true で表示、false で非表示。</param>
        public void SetVisible(bool visible)
        {
            var target = m_RootElement ?? m_BoundElement;
            if (target == null)
            {
                m_IsVisible = visible;
                return;
            }

            target.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            m_IsVisible = visible;
        }

        /// <summary>
        /// モデルで保持している描画効果を UI Toolkit の背景 Tint へ変換して適用する。
        /// </summary>
        /// <param name="effects">適用する描画効果の状態。</param>
        public void ApplyGraphicEffects(ActorState.GraphicEffectsState effects)
        {
            var normalized = Mathf.Repeat(effects.ColorEffect, 200f);
            if (Mathf.Approximately(normalized, 0f))
            {
                ResetEffects();
                return;
            }

            var sourceTexture = GetSourceTexture();
            if (sourceTexture == null)
            {
                Debug.LogWarning("[FUnity.ActorView] 色相回転の元テクスチャを取得できなかったため、描画効果をスキップします。");
                return;
            }

            m_HuePipeline ??= new HueShiftPipeline();
            if (m_HuePipeline == null)
            {
                return;
            }

            var hueDegrees = normalized * (360f / 200f);
            var renderTexture = m_HuePipeline.Render(sourceTexture, hueDegrees);
            if (renderTexture == null)
            {
                return;
            }

            var drawable = GetDrawableElement();
            if (drawable == null)
            {
                return;
            }

            if (drawable is Image imageElement)
            {
                imageElement.image = renderTexture;
                imageElement.tintColor = Color.white;
            }
            else
            {
                drawable.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(renderTexture));
                drawable.style.unityBackgroundImageTintColor = Color.white;
            }
        }

        /// <summary>
        /// 描画効果を初期状態に戻し、Tint を白へリセットする。
        /// </summary>
        public void ResetEffects()
        {
            var drawable = GetDrawableElement();
            var sourceTexture = GetSourceTexture();

            if (drawable is Image imageElement)
            {
                if (sourceTexture != null)
                {
                    imageElement.image = sourceTexture;
                }

                imageElement.tintColor = Color.white;
                return;
            }

            if (drawable != null)
            {
                if (sourceTexture is RenderTexture renderTexture)
                {
                    drawable.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(renderTexture));
                }
                else if (sourceTexture is Texture2D texture2D)
                {
                    drawable.style.backgroundImage = new StyleBackground(texture2D);
                }
                else if (sourceTexture != null)
                {
                    Debug.LogWarning("[FUnity.ActorView] 未対応のテクスチャ種別のため、背景復元をスキップします。");
                }

                drawable.style.unityBackgroundImageTintColor = Color.white;
                return;
            }

            var root = GetRootElement();
            if (root != null)
            {
                root.style.unityBackgroundImageTintColor = Color.white;
            }
        }

        /// <summary>
        /// 現在角度に相対加算で回転させる。Presenter からの「自分を回す」命令を UI に伝える際に利用する。
        /// </summary>
        /// <param name="deltaDegrees">加算する角度（度）。正で反時計回り。</param>
        public void AddRotationDegrees(float deltaDegrees)
        {
            var next = NormalizeDegrees(m_CurrentRotationDeg + deltaDegrees);
            SetRotationDegrees(next);
        }

        /// <summary>
        /// 吹き出しを表示し、発言か思考かに応じてクラスを切り替える。
        /// </summary>
        /// <param name="message">表示する本文。</param>
        /// <param name="isThought">思考吹き出しとして表示する場合は true。</param>
        public void ShowSpeech(string message, bool isThought)
        {
            EnsureSpeechElements();
            if (m_SpeechBubble == null || m_SpeechLabel == null)
            {
                return;
            }

            ResetSpeechStyle();
            m_SpeechLabel.text = message ?? string.Empty;
            m_SpeechBubble.EnableInClassList("speech-think", isThought);
            m_SpeechBubble.EnableInClassList("speech-say", !isThought);
            // 吹き出し表示のたびに視認性を最優先で担保するため、インライン style を強制適用する。
            // USS 側の競合を無視して文字色・背景色・フォントサイズを確実に反映する。
            m_SpeechLabel.style.color = Color.black;
            m_SpeechLabel.style.fontSize = SpeechBaseFontPx * 2f;

            // 背景はわずかに透過を抑えた白、境界線と余白を最低限付与して読みやすさを向上させる。
            m_SpeechBubble.style.backgroundColor = new Color(1f, 1f, 1f, 0.98f);
            m_SpeechBubble.style.paddingLeft = 6f;
            m_SpeechBubble.style.paddingRight = 6f;
            m_SpeechBubble.style.paddingTop = 4f;
            m_SpeechBubble.style.paddingBottom = 4f;
            m_SpeechBubble.style.borderTopWidth = 1f;
            m_SpeechBubble.style.borderRightWidth = 1f;
            m_SpeechBubble.style.borderBottomWidth = 1f;
            m_SpeechBubble.style.borderLeftWidth = 1f;
            var borderColor = new Color(0f, 0f, 0f, 0.25f);
            m_SpeechBubble.style.borderTopColor = borderColor;
            m_SpeechBubble.style.borderRightColor = borderColor;
            m_SpeechBubble.style.borderBottomColor = borderColor;
            m_SpeechBubble.style.borderLeftColor = borderColor;

            m_SpeechBubble.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 吹き出し表示のたびにサイズやスケールを初期化し、累積的な拡大を防ぐ。
        /// </summary>
        private void ResetSpeechStyle()
        {
            if (m_SpeechBubble != null)
            {
                m_SpeechBubble.style.width = StyleKeyword.Auto;
                m_SpeechBubble.style.height = StyleKeyword.Auto;
                m_SpeechBubble.style.maxWidth = SpeechMaxWidthPx;
                m_SpeechBubble.style.scale = IdentityScale;
                m_SpeechBubble.style.display = DisplayStyle.None;
            }

            if (m_SpeechLabel != null)
            {
                m_SpeechLabel.style.width = StyleKeyword.Auto;
                m_SpeechLabel.style.height = StyleKeyword.Auto;
                m_SpeechLabel.style.fontSize = SpeechBaseFontPx * 2f;
            }
        }

        /// <summary>
        /// 表示中の吹き出しを非表示にし、本文をリセットする。
        /// </summary>
        public void HideSpeech()
        {
            if (m_SpeechBubble == null)
            {
                return;
            }

            m_SpeechBubble.style.display = DisplayStyle.None;
            if (m_SpeechLabel != null)
            {
                m_SpeechLabel.text = string.Empty;
            }
        }

        /// <summary>
        /// worldBound キャッシュの更新に利用する GeometryChangedEvent を登録する。
        /// </summary>
        private void RegisterWorldBoundCacheSources()
        {
            var root = GetRootElement();
            TryRegisterWorldBoundSource(root);

            if (m_ActorRoot != null && m_ActorRoot != root)
            {
                TryRegisterWorldBoundSource(m_ActorRoot);
            }

            if (m_BoundElement != null && m_BoundElement != root && m_BoundElement != m_ActorRoot)
            {
                TryRegisterWorldBoundSource(m_BoundElement);
            }

            var sprite = ResolveSpriteElement(root ?? m_ActorRoot ?? m_BoundElement);
            TryRegisterWorldBoundSource(sprite);
        }

        /// <summary>
        /// worldBound キャッシュ用の GeometryChangedEvent を解除し、不要な購読を除去する。
        /// </summary>
        private void UnregisterWorldBoundCacheSources()
        {
            if (m_WorldBoundSources.Count == 0)
            {
                m_WorldBoundCache = Rect.zero;
                m_HasWorldBoundCache = false;
                return;
            }

            foreach (var source in m_WorldBoundSources)
            {
                source?.UnregisterCallback<GeometryChangedEvent>(OnWorldBoundSourceGeometryChanged);
            }

            m_WorldBoundSources.Clear();
            m_WorldBoundCache = Rect.zero;
            m_HasWorldBoundCache = false;
        }

        /// <summary>
        /// MonoBehaviour の破棄時に GeometryChangedEvent の購読を解除する。
        /// </summary>
        private void OnDestroy()
        {
            UnregisterGeometryCallbacks();
            UnregisterWorldBoundCacheSources();
            if (m_HuePipeline != null)
            {
                m_HuePipeline.Dispose();
                m_HuePipeline = null;
            }
        }

        /// <summary>
        /// GeometryChangedEvent を購読し、ステージ境界の再計算契機を確保する。
        /// </summary>
        private void RegisterGeometryCallbacks()
        {
            var geometryTarget = m_RootElement ?? m_BoundElement;
            if (geometryTarget != null && !m_GeometryCallbacksRegistered)
            {
                geometryTarget.RegisterCallback<GeometryChangedEvent>(OnElementGeometryChanged);
                m_GeometryCallbacksRegistered = true;
            }

            var panelRoot = geometryTarget?.panel?.visualTree;
            if (panelRoot == null || panelRoot == m_PanelRoot)
            {
                return;
            }

            if (m_PanelRoot != null)
            {
                m_PanelRoot.UnregisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
            }

            m_PanelRoot = panelRoot;
            m_PanelRoot.RegisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
        }

        /// <summary>
        /// 要素自体のジオメトリ変化を捕捉し、境界再計算を促す。
        /// </summary>
        /// <param name="evt">UI Toolkit が発行する GeometryChangedEvent。</param>
        private void OnElementGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt?.target is VisualElement element)
            {
                UpdateWorldBoundCache(element.worldBound);
            }

            ApplyAllTransforms();
        }

        /// <summary>
        /// パネル全体のジオメトリ変化を受け取り、境界通知を行う。
        /// </summary>
        /// <param name="evt">UI Toolkit から渡されるイベント。</param>
        private void OnPanelGeometryChanged(GeometryChangedEvent evt)
        {
            NotifyStageBounds();
        }

        /// <summary>
        /// 現在取得可能なステージ境界を Presenter へ通知する。
        /// </summary>
        private void NotifyStageBounds()
        {
            if (StageBoundsChanged == null)
            {
                return;
            }

            if (TryGetStageBounds(out var bounds))
            {
                StageBoundsChanged.Invoke(bounds);
            }
        }

        /// <summary>
        /// GeometryChangedEvent の購読を解除し、リソースリークを防ぐ。
        /// </summary>
        private void UnregisterGeometryCallbacks()
        {
            if (m_GeometryCallbacksRegistered)
            {
                var geometryTarget = m_RootElement ?? m_BoundElement;
                geometryTarget?.UnregisterCallback<GeometryChangedEvent>(OnElementGeometryChanged);
            }

            m_GeometryCallbacksRegistered = false;

            if (m_PanelRoot != null)
            {
                m_PanelRoot.UnregisterCallback<GeometryChangedEvent>(OnPanelGeometryChanged);
                m_PanelRoot = null;
            }
        }

        /// <summary>
        /// worldBound キャッシュ対象の要素へ GeometryChangedEvent を登録する。
        /// </summary>
        /// <param name="element">監視対象の要素。</param>
        private void TryRegisterWorldBoundSource(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (m_WorldBoundSources.Contains(element))
            {
                return;
            }

            element.RegisterCallback<GeometryChangedEvent>(OnWorldBoundSourceGeometryChanged, TrickleDown.TrickleDown);
            m_WorldBoundSources.Add(element);
            UpdateWorldBoundCache(element.worldBound);
        }

        /// <summary>
        /// GeometryChangedEvent の通知を受け取り、worldBound キャッシュを更新する。
        /// </summary>
        /// <param name="evt">UI Toolkit から渡されるイベント。</param>
        private void OnWorldBoundSourceGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt?.target is not VisualElement element)
            {
                return;
            }

            UpdateWorldBoundCache(element.worldBound);
        }

        /// <summary>
        /// スプライト要素を名前・クラス指定で探索し、見つかった場合に返す。
        /// </summary>
        /// <param name="root">探索を行う起点要素。</param>
        /// <returns>スプライトを描画している要素。存在しない場合は null。</returns>
        private static VisualElement ResolveSpriteElement(VisualElement root)
        {
            if (root == null)
            {
                return null;
            }

            return root.Q<VisualElement>("sprite")
                ?? root.Q<VisualElement>(className: "sprite")
                ?? root.Q<VisualElement>("portrait")
                ?? root.Q<VisualElement>(className: "portrait");
        }

        /// <summary>
        /// 新たに観測した worldBound を検証し、有効な場合はキャッシュを更新する。
        /// </summary>
        /// <param name="candidate">GeometryChangedEvent で取得した矩形。</param>
        private void UpdateWorldBoundCache(Rect candidate)
        {
            if (candidate.width <= 0f || candidate.height <= 0f)
            {
                if (!m_HasWorldBoundCache)
                {
                    m_WorldBoundCache = candidate;
                }

                return;
            }

            m_WorldBoundCache = candidate;
            m_HasWorldBoundCache = true;
        }

        /// <summary>
        /// ポートレート要素を遅延解決し、キャッシュを維持する。未定義の場合は #root 要素へフォールバックする。
        /// </summary>
        /// <returns>回転・画像差し替え対象の VisualElement。</returns>
        private VisualElement ResolvePortraitElement()
        {
            if (m_PortraitElement != null)
            {
                return m_PortraitElement;
            }

            var searchBase = GetRootElement();
            if (searchBase == null)
            {
                return null;
            }

            m_PortraitElement = searchBase.Q<VisualElement>("portrait")
                ?? searchBase.Q<VisualElement>(className: "portrait")
                ?? searchBase.Q<VisualElement>(className: "actor-portrait")
                ?? searchBase;

            return m_PortraitElement;
        }

        /// <summary>
        /// 回転適用専用の要素を解決する。優先順位は actor-visual → portrait → root の順とし、吹き出しを含まないツリーを選択する。
        /// </summary>
        /// <returns>回転・左右反転を適用する対象要素。</returns>
        private VisualElement ResolveRotationTarget()
        {
            var searchBase = GetRootElement();
            if (searchBase == null)
            {
                return null;
            }

            var visualRoot = searchBase.Q<VisualElement>("visual")
                ?? searchBase.Q<VisualElement>(className: "actor-visual");
            if (visualRoot != null)
            {
                return visualRoot;
            }

            var portrait = ResolvePortraitElement();
            if (portrait != null && portrait != searchBase)
            {
                return portrait;
            }

            return portrait ?? searchBase;
        }

        /// <summary>
        /// ポートレート要素を解決して専用 Image を 1 つだけ配置し、backgroundImage の直接使用を避ける。
        /// 既存の Image を極力再利用し、重複してしまった要素はクリーンアップする。
        /// </summary>
        private void EnsurePortraitImage()
        {
            if (m_PortraitElement == null)
            {
                m_PortraitElement = ResolvePortraitElement();
                if (m_PortraitElement == null)
                {
                    return;
                }
            }

            if (m_PortraitImage == null)
            {
                m_PortraitImage = m_PortraitElement.Q<Image>("portrait-image");
            }

            if (m_PortraitImage == null)
            {
                m_PortraitImage = new Image
                {
                    name = "portrait-image",
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore,
                };
                m_PortraitImage.style.flexGrow = 1f;
                m_PortraitElement.Add(m_PortraitImage);
            }
            else if (m_PortraitImage.parent != m_PortraitElement)
            {
                m_PortraitImage.RemoveFromHierarchy();
                m_PortraitElement.Add(m_PortraitImage);
            }

            var duplicates = m_PortraitElement.Children()
                .OfType<Image>()
                .Where(image => image != m_PortraitImage && image.name == "portrait-image")
                .ToList();
            foreach (var extra in duplicates)
            {
                extra.RemoveFromHierarchy();
            }

            // 旧スタイルが残らないように背景を明示クリアする
            m_PortraitElement.style.backgroundImage = StyleKeyword.Null;
            m_PortraitElement.style.backgroundColor = StyleKeyword.Null;
        }
        /// <summary>
        /// 描画効果の基準となる元テクスチャを再取得できるよう、キャッシュを無効化する。
        /// </summary>
        private void InvalidateSourceTexture()
        {
            m_SourceCaptured = false;
            m_SourceTexture = null;
        }

        /// <summary>
        /// 色相回転や背景差し替えを適用する対象となる描画用要素を解決する。
        /// </summary>
        /// <returns>ポートレート要素またはスプライト要素。見つからない場合は null。</returns>
        private VisualElement GetDrawableElement()
        {
            var portrait = ResolvePortraitElement();
            if (portrait != null)
            {
                return portrait;
            }

            var root = GetRootElement();
            var sprite = ResolveSpriteElement(root ?? m_ActorRoot ?? m_BoundElement);
            if (sprite != null)
            {
                return sprite;
            }

            return root ?? m_BoundElement;
        }

        /// <summary>
        /// 描画効果を適用する前の元テクスチャを取得し、必要に応じてキャッシュする。
        /// </summary>
        /// <returns>取得できた元テクスチャ。VectorImage 等で未対応の場合は null。</returns>
        private Texture GetSourceTexture()
        {
            if (m_SourceCaptured && m_SourceTexture != null)
            {
                return m_SourceTexture;
            }

            if (m_SourceCaptured && m_SourceTexture == null)
            {
                return null;
            }

            var drawable = GetDrawableElement();
            if (drawable == null)
            {
                m_SourceCaptured = false;
                return null;
            }

            Texture captured = null;
            if (drawable is Image imageElement)
            {
                if (imageElement.image != null)
                {
                    captured = imageElement.image;
                }
                else if (imageElement.sprite != null)
                {
                    captured = imageElement.sprite.texture;
                }
                else if (imageElement.vectorImage != null)
                {
                    Debug.LogWarning("[FUnity.ActorView] VectorImage からの色相回転には未対応です。テクスチャへ変換してください。");
                }
            }

            if (captured == null)
            {
                var background = drawable.resolvedStyle.backgroundImage;
                if (background.texture != null)
                {
                    captured = background.texture;
                }
                else if (background.renderTexture != null)
                {
                    captured = background.renderTexture;
                }
                else if (background.sprite != null)
                {
                    captured = background.sprite.texture;
                }
            }

            if (captured == null)
            {
                m_SourceCaptured = false;
                return null;
            }

            m_SourceTexture = captured;
            m_SourceCaptured = true;
            return m_SourceTexture;
        }

        /// <summary>
        /// UXML 内の俳優コンテナを名前優先で探索し、なければクラスで解決する。
        /// </summary>
        /// <param name="element">探索の起点となる VisualElement。</param>
        /// <returns>見つかった俳優コンテナ。存在しない場合は null。</returns>
        private static VisualElement FindActorRoot(VisualElement element)
        {
            if (element == null)
            {
                return null;
            }

            var byName = element.Q<VisualElement>("root");
            if (byName != null)
            {
                return byName;
            }

            return element.Q<VisualElement>(className: "actor-root");
        }

        /// <summary>
        /// 吹き出し用の VisualElement を生成し、俳優ルート要素へ追加する。
        /// </summary>
        private void EnsureSpeechElements()
        {
            if (m_RootElement == null)
            {
                return;
            }

            var speechLayer = m_RootElement.Q<VisualElement>("speech-layer");
            var speechLabel = speechLayer?.Q<Label>("speech");
            if (speechLayer != null)
            {
                m_SpeechBubble = speechLayer;
                m_SpeechLabel = speechLabel ?? m_SpeechLabel;
            }

            if (m_SpeechBubble != null && m_SpeechBubble.parent != m_RootElement)
            {
                m_SpeechBubble.RemoveFromHierarchy();
                m_SpeechBubble = null;
                m_SpeechLabel = null;
            }

            if (m_SpeechBubble == null)
            {
                m_SpeechBubble = new VisualElement
                {
                    name = "speech-bubble",
                    pickingMode = PickingMode.Ignore,
                };
                m_SpeechBubble.AddToClassList("speech-bubble");
                m_SpeechBubble.AddToClassList("speech-say");
                m_SpeechBubble.style.position = Position.Absolute;
                m_SpeechBubble.style.left = new Length(50f, LengthUnit.Percent);
                m_SpeechBubble.style.bottom = new Length(100f, LengthUnit.Percent);
                m_SpeechBubble.style.translate = new Translate(
                    new Length(-50f, LengthUnit.Percent),
                    new Length(-8f, LengthUnit.Pixel),
                    0f);
                m_SpeechBubble.style.display = DisplayStyle.None;

                m_SpeechLabel = new Label
                {
                    name = "speech-text",
                    pickingMode = PickingMode.Ignore,
                };
                m_SpeechLabel.AddToClassList("speech-text");
                m_SpeechBubble.Add(m_SpeechLabel);

                m_RootElement.Add(m_SpeechBubble);
            }
            else if (m_SpeechLabel == null)
            {
                m_SpeechLabel = new Label
                {
                    name = "speech-text",
                    pickingMode = PickingMode.Ignore,
                };
                m_SpeechLabel.AddToClassList("speech-text");
                m_SpeechBubble.Add(m_SpeechLabel);
            }

            if (m_SpeechBubble != null)
            {
                m_SpeechBubble.AddToClassList("speech-bubble");
                m_SpeechBubble.AddToClassList("speech-say");
                m_SpeechBubble.pickingMode = PickingMode.Ignore;
                m_SpeechBubble.style.position = Position.Absolute;
                m_SpeechBubble.style.left = new Length(50f, LengthUnit.Percent);
                m_SpeechBubble.style.bottom = new Length(100f, LengthUnit.Percent);
                m_SpeechBubble.style.translate = new Translate(
                    new Length(-50f, LengthUnit.Percent),
                    new Length(-8f, LengthUnit.Pixel),
                    0f);
            }

            if (m_SpeechLabel != null)
            {
                m_SpeechLabel.AddToClassList("speech-text");
                m_SpeechLabel.pickingMode = PickingMode.Ignore;
            }
        }

        /// <summary>
        /// スケール・回転・座標適用対象となる #root 要素を解決する。
        /// </summary>
        /// <returns>俳優のルート VisualElement。</returns>
        private VisualElement GetRootElement()
        {
            return m_RootElement ?? m_ActorRoot ?? m_BoundElement;
        }

        /// <summary>
        /// 指定した色を背景画像の Tint として適用する。
        /// </summary>
        /// <param name="color">乗算する色。</param>
        public void SetTintColor(Color color)
        {
            var drawable = GetDrawableElement();
            if (drawable is Image imageElement)
            {
                imageElement.tintColor = color;
                return;
            }

            if (drawable != null)
            {
                drawable.style.unityBackgroundImageTintColor = color;
                return;
            }

            var root = GetRootElement();
            if (root != null)
            {
                root.style.unityBackgroundImageTintColor = color;
            }
        }

        /// <summary>
        /// 中心座標とスケール前レイアウトサイズから left/top を算出し、#root 要素へ反映する。
        /// 描画揺れを抑えるため整数ピクセルへ丸めて適用する。
        /// </summary>
        private void UpdateLayoutForCenter()
        {
            var root = GetRootElement();
            if (root == null)
            {
                return;
            }

            var rootSize = root.layout.size;
            var halfWidth = rootSize.x * 0.5f;
            var halfHeight = rootSize.y * 0.5f;

            var left = Mathf.RoundToInt(m_CurrentCenterPx.x - halfWidth);
            var top = Mathf.RoundToInt(m_CurrentCenterPx.y - halfHeight);

            root.style.left = left;
            root.style.top = top;
        }

        /// <summary>
        /// layout.size もしくは resolvedStyle.width/height を正規化し、負値・NaN を 0 として扱う。
        /// </summary>
        /// <param name="layoutValue">layout から取得した値。</param>
        /// <param name="resolvedValue">resolvedStyle から取得したフォールバック値。</param>
        /// <returns>正規化済みの長さ（px）。</returns>
        private static float NormalizeLength(float layoutValue, float resolvedValue)
        {
            var hasLayout = !float.IsNaN(layoutValue) && layoutValue > 0f;
            if (hasLayout)
            {
                return layoutValue;
            }

            if (!float.IsNaN(resolvedValue) && resolvedValue > 0f)
            {
                return resolvedValue;
            }

            return 0f;
        }

        /// <summary>
        /// 現在保持しているスケール・回転・中心座標を再適用し、ジオメトリ変化後の揺らぎを抑制する。
        /// </summary>
        private void ApplyAllTransforms()
        {
            ApplyScaleToRoot();
            ApplyRotationToRoot();
            UpdateLayoutForCenter();
            NotifyStageBounds();
        }

        /// <summary>
        /// 現在のスケール値を #root 要素へ適用し、中心を基点に拡縮できるようにする。
        /// </summary>
        private void ApplyScaleToRoot()
        {
            var root = GetRootElement();
            if (root == null)
            {
                return;
            }

            ApplyCenterPivot(root);
#if UNITY_2022_3_OR_NEWER
            var scaleX = m_CurrentScale * m_FlipSignX;
            ApplyScaleXY(root, scaleX, m_CurrentScale);
#endif
        }

        /// <summary>
        /// 現在保持している回転角を #root 要素へ適用する。
        /// </summary>
        private void ApplyRotationToRoot()
        {
            var rotationTarget = ResolveRotationTarget();
            if (rotationTarget == null)
            {
                return;
            }

            ApplyCenterPivot(rotationTarget);
            rotationTarget.style.rotate = new Rotate(Angle.Degrees(m_CurrentRotationDeg));

            var root = GetRootElement();
            if (root != null && root != rotationTarget)
            {
                ApplyCenterPivot(root);
                root.style.rotate = new Rotate(Angle.Degrees(0f));
            }
        }

        /// <summary>
        /// 指定された要素の transform-origin を中央 (50% 50%) へ固定する。
        /// UITK の拡縮・回転を要素中心で行うための前提条件として用いる。
        /// </summary>
        /// <param name="element">ピボットを適用する対象要素。</param>
        private static void ApplyCenterPivot(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.style.transformOrigin = CenterTransformOrigin;
        }

        /// <summary>
        /// ポートレート要素の scale を等倍へ戻し、#root 側の拡縮へ責務を集約する。
        /// </summary>
        private void ResetPortraitScaleToIdentity()
        {
            var portrait = ResolvePortraitElement();
            if (portrait == null)
            {
                return;
            }

            portrait.style.scale = IdentityScale;
        }

        /// <summary>
        /// 指定角度を 0～360 度の範囲へ正規化し、UI の回転指定に利用しやすい形へ整える。
        /// </summary>
        /// <param name="degrees">正規化する角度（度）。</param>
        /// <returns>0～360 度の範囲に収まる角度。</returns>
        private static float NormalizeDegrees(float degrees)
        {
            var normalized = degrees % 360f;
            if (normalized < 0f)
            {
                normalized += 360f;
            }

            return normalized;
        }
    }
}
