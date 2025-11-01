// Updated: 2025-03-03
using System;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Input;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.View
{
    /// <summary>
    /// 俳優の見た目と座標を UI Toolkit 上に投影する View レイヤーの薄いアダプタ。
    /// Presenter が指示する座標と画像のみを反映し、状態やロジックは保持しない。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FooniUIBridge"/>（UI Toolkit 要素への操作ラッパー）
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> が生成した UI GameObject にアタッチされ、<see cref="FUnity.Runtime.Presenter.ActorPresenter"/>
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
        /// Presenter から指定された UI Toolkit 要素。`Configure` 呼び出し後に <see cref="SetPortrait"/> が利用する。
        /// </summary>
        private VisualElement m_BoundElement;

        /// <summary>サイズ・吹き出し適用対象となるルート要素。</summary>
        private VisualElement m_RootElement;

        /// <summary>回転および背景差し替えに利用するポートレート要素。</summary>
        private VisualElement m_PortraitElement;

        /// <summary>現在の回転角度（度）。中心ピボットを基準に UI へ適用する。</summary>
        private float m_CurrentRotationDeg;

        /// <summary>現在の中心座標（px）。左上原点で右+ / 下+。</summary>
        private Vector2 m_CurrentCenterPx = Vector2.zero;

        /// <summary>吹き出しテキストを表示するラベル。</summary>
        private Label m_SpeechLabel;

        /// <summary>吹き出し非表示を遅延実行するスケジュール項目。</summary>
        private IVisualElementScheduledItem m_SpeechHideItem;

        /// <summary>パネル全体の GeometryChangedEvent を購読する際に利用するルート要素。</summary>
        private VisualElement m_PanelRoot;

        /// <summary>GeometryChangedEvent の登録済みかどうかを示すフラグ。</summary>
        private bool m_GeometryCallbacksRegistered;

        /// <summary>俳優ルート要素のジオメトリが確定済みかを示すフラグ。</summary>
        private bool m_GeometryReady;

        /// <summary>現在適用している等倍基準のスケール値。</summary>
        private float m_CurrentScale = 1f;

        /// <summary>transform-origin を中央 (50%/50%) へ固定するためのパーセント指定。</summary>
        private static readonly Length CenterPivotPercent = new Length(50f, LengthUnit.Percent);

        /// <summary>transform-origin を中央へ固定するためのスタイル値。</summary>
        private static readonly StyleTransformOrigin CenterTransformOrigin = new StyleTransformOrigin(
            new TransformOrigin(CenterPivotPercent, CenterPivotPercent, 0f));

        /// <summary>ポートレートのスケールを初期化する際に使用する等倍スタイル。</summary>
        private static readonly StyleScale IdentityScale = new StyleScale(new Scale(Vector3.one));

        /// <summary>UI Toolkit の scale に適用する下限値。</summary>
        private const float MinimumScale = 0.01f;

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
            m_GeometryReady = false;
            if (m_RootElement != null)
            {
                ApplyCenterPivot(m_RootElement);
            }
            m_SpeechLabel = searchBase?.Q<Label>("speech") ?? element?.Q<Label>("speech");
            if (m_SpeechLabel != null)
            {
                m_SpeechLabel.style.display = DisplayStyle.None;
            }

            m_PortraitElement = null;
            ResetPortraitScaleToIdentity();

            RegisterGeometryCallbacks();
            ApplyAllTransforms();
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
        /// Presenter から通知された中心座標を UI Toolkit 要素へ適用する。
        /// </summary>
        /// <param name="centerPx">左上原点基準（px）の中心座標。</param>
        /// <remarks>
        /// 要素の自然サイズを参照して左上座標へ変換し、<see cref="FooniUIBridge"/> 経由で `style.left/top` を更新する。
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
        /// 現在適用されているスケールを反映した俳優要素の見た目サイズ（px）を返す。
        /// </summary>
        /// <returns>スケール反映後の幅・高さ（px）。要素未バインド時は <see cref="Vector2.zero"/> を返す。</returns>
        public Vector2 GetScaledSizePx()
        {
            if (m_RootElement == null && m_BoundElement == null)
            {
                return Vector2.zero;
            }

            return GetScaledRootSize();
        }

        /// <summary>
        /// 俳優のポートレート画像を UI Toolkit の `portrait` 要素へ設定する。
        /// </summary>
        /// <param name="sprite">`Sprite.Create` 等で生成済みのスプライト。</param>
        /// <remarks>
        /// `portrait` 要素には UI Toolkit の仕様上 <see cref="StyleBackground"/> を `new StyleBackground(Texture2D)` で渡す必要がある。
        /// `StyleBackground.none` は使用できないため、null 時は既存の背景を保持する。
        /// </remarks>
        /// <example>
        /// <code>
        /// var portraitSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
        /// m_ActorView.SetPortrait(portraitSprite);
        /// </code>
        /// </example>
        public void SetPortrait(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            var portrait = ResolvePortraitElement();
            if (portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(sprite);
            }
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
        /// 現在角度に相対加算で回転させる。Presenter からの「自分を回す」命令を UI に伝える際に利用する。
        /// </summary>
        /// <param name="deltaDegrees">加算する角度（度）。正で反時計回り。</param>
        public void AddRotationDegrees(float deltaDegrees)
        {
            var next = NormalizeDegrees(m_CurrentRotationDeg + deltaDegrees);
            SetRotationDegrees(next);
        }

        /// <summary>
        /// 吹き出しを表示し、指定秒数後に自動で非表示へ戻す。
        /// </summary>
        /// <param name="message">表示するテキスト。</param>
        /// <param name="seconds">表示時間。</param>
        public void ShowSpeech(string message, float seconds)
        {
            if (m_SpeechLabel == null)
            {
                return;
            }

            m_SpeechLabel.text = message ?? string.Empty;
            m_SpeechLabel.style.display = DisplayStyle.Flex;

            m_SpeechHideItem?.Pause();
            m_SpeechHideItem = m_SpeechLabel.schedule.Execute(() =>
            {
                m_SpeechLabel.style.display = DisplayStyle.None;
            });
            // UI Toolkit スケジューラはミリ秒 long を要求するため、秒 float を ms long に変換
            m_SpeechHideItem.StartingIn(SecToMs(Mathf.Max(0.1f, seconds)));
        }

        /// <summary>
        /// UI Toolkit のスケジューラへ渡す時間を秒からミリ秒へ変換するヘルパー。
        /// 負数が渡された場合でも 0 以上に正規化した上で long 型を返す。
        /// </summary>
        /// <param name="seconds">秒単位で指定された時間。</param>
        /// <returns>ミリ秒単位へ変換した値。</returns>
        private static long SecToMs(float seconds)
        {
            return (long)(Mathf.Max(0f, seconds) * 1000f);
        }

        /// <summary>
        /// MonoBehaviour の破棄時に GeometryChangedEvent の購読を解除する。
        /// </summary>
        private void OnDestroy()
        {
            UnregisterGeometryCallbacks();
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
            if (evt != null)
            {
                m_GeometryReady = evt.newRect.width > 0f && evt.newRect.height > 0f;
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
        /// ポートレート要素を遅延解決し、キャッシュを維持する。存在しない場合は null を返す。
        /// </summary>
        /// <returns>回転・画像差し替え対象の VisualElement。</returns>
        private VisualElement ResolvePortraitElement()
        {
            if (m_PortraitElement != null)
            {
                return m_PortraitElement;
            }

            if (m_BoundElement == null)
            {
                return null;
            }

            m_PortraitElement = m_BoundElement.Q<VisualElement>("portrait")
                ?? m_BoundElement.Q<VisualElement>(className: "portrait")
                ?? m_BoundElement.Q<VisualElement>(className: "actor-portrait");

            return m_PortraitElement;
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
        /// スケール・回転・座標適用対象となる #root 要素を解決する。
        /// </summary>
        /// <returns>俳優のルート VisualElement。</returns>
        private VisualElement GetRootElement()
        {
            return m_RootElement ?? m_ActorRoot ?? m_BoundElement;
        }

        /// <summary>
        /// 中心座標と拡大後サイズから left/top を算出し、#root 要素へ反映する。
        /// 描画揺れを抑えるため整数ピクセルへ丸めて適用する。
        /// </summary>
        private void UpdateLayoutForCenter()
        {
            var root = GetRootElement();
            if (root == null)
            {
                return;
            }

            if (!m_GeometryReady)
            {
                var fallbackLeft = Mathf.RoundToInt(m_CurrentCenterPx.x);
                var fallbackTop = Mathf.RoundToInt(m_CurrentCenterPx.y);
                root.style.left = fallbackLeft;
                root.style.top = fallbackTop;
                return;
            }

            var scaledSize = GetScaledSizePx();
            if (scaledSize.sqrMagnitude <= 0f)
            {
                var centerLeft = Mathf.RoundToInt(m_CurrentCenterPx.x);
                var centerTop = Mathf.RoundToInt(m_CurrentCenterPx.y);
                root.style.left = centerLeft;
                root.style.top = centerTop;
                return;
            }

            var halfWidth = scaledSize.x * 0.5f;
            var halfHeight = scaledSize.y * 0.5f;
            var left = Mathf.RoundToInt(m_CurrentCenterPx.x - halfWidth);
            var top = Mathf.RoundToInt(m_CurrentCenterPx.y - halfHeight);

            root.style.left = left;
            root.style.top = top;
        }

        /// <summary>
        /// ルート要素の自然サイズ（スケール適用前）を取得する。
        /// </summary>
        /// <returns>幅・高さ（px）。未確定時は 0。</returns>
        private Vector2 GetUnscaledRootSize()
        {
            var root = GetRootElement();
            if (root == null)
            {
                return Vector2.zero;
            }

            var width = root.layout.width;
            var height = root.layout.height;

            if (float.IsNaN(width) || width <= 0f)
            {
                width = root.resolvedStyle.width;
            }

            if (float.IsNaN(height) || height <= 0f)
            {
                height = root.resolvedStyle.height;
            }

            if (float.IsNaN(width) || width < 0f)
            {
                width = 0f;
            }

            if (float.IsNaN(height) || height < 0f)
            {
                height = 0f;
            }

            return new Vector2(width, height);
        }

        /// <summary>
        /// スケール適用後の俳優ルート要素サイズを計算し、中心アンカーでの配置計算に利用する。
        /// </summary>
        /// <returns>スケール済み幅・高さ（px）。未確定時は <see cref="Vector2.zero"/>。</returns>
        private Vector2 GetScaledRootSize()
        {
            var baseSize = GetUnscaledRootSize();
            if (baseSize.sqrMagnitude <= 0f)
            {
                return Vector2.zero;
            }

#if UNITY_2022_3_OR_NEWER
            var root = GetRootElement();
            var scaleValue = ResolveScaleXY(root, 1f);
            return new Vector2(baseSize.x * scaleValue.x, baseSize.y * scaleValue.y);
#else
            return baseSize * m_CurrentScale;
#endif
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
            ApplyScaleXY(root, m_CurrentScale, m_CurrentScale);
#endif
        }

        /// <summary>
        /// 現在保持している回転角を #root 要素へ適用する。
        /// </summary>
        private void ApplyRotationToRoot()
        {
            var root = GetRootElement();
            if (root == null)
            {
                return;
            }

            ApplyCenterPivot(root);
            root.style.rotate = new Rotate(Angle.Degrees(m_CurrentRotationDeg));
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
