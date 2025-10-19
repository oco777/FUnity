// Updated: 2025-02-14
using System; // for ObsoleteAttribute
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.Input
{
    /// <summary>
    /// UI Toolkit 上の俳優要素を操作する View 向けヘルパー。Presenter から渡された要素に対し座標や移動を適用する。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="UIDocument"/>（ルート取得）、<see cref="VisualElement"/> API
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> が UI GameObject に追加し、<see cref="FUnity.Runtime.View.ActorView"/>
    ///     や Visual Scripting から座標更新メソッドが呼び出される。
    /// 本クラスは View であり、Model/Presenter のロジックを保持しない。背景画像の設定には `StyleBackground(Texture2D)` を利用する。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class FooniUIBridge : MonoBehaviour
    {
        /// <summary>
        /// ルート要素として検索する UXML 名称。空の場合はクラス検索へフォールバックする。
        /// </summary>
        public string rootName = "root";

        /// <summary>
        /// ルート要素として検索する USS クラス名。<see cref="rootName"/> で見つからなかった場合のみ使用する。
        /// </summary>
        public string rootClass = "actor-root";

        /// <summary>
        /// <see cref="NudgeByDefaultSpeed"/> が使用する既定移動速度（px/s）。
        /// </summary>
        public float defaultSpeed = 300f;

        /// <summary>
        /// `true` の場合、スクリーン全体の範囲に座標をクランプする。
        /// </summary>
        public bool clampToPanel = true;

        private UIDocument _ui;
        private VisualElement _actorRoot;

        /// <summary>Presenter ベースのクランプへフォワードする対象。</summary>
        private ActorPresenter m_ActorPresenter;

        /// <summary>非推奨 API の警告を既に表示したかどうか。</summary>
        private bool m_HasLoggedClampWarning;

        /// <summary>
        /// <see cref="UIDocument"/> をキャッシュする。
        /// </summary>
        private void Awake()
        {
            _ui = GetComponent<UIDocument>();
        }

        /// <summary>
        /// 実行開始時に既定のルート要素をバインドする。
        /// </summary>
        private void Start()
        {
            TryBind();
        }

        /// <summary>
        /// 現在ルート要素がバインド済みかを返す。
        /// </summary>
        public bool HasBoundElement => _actorRoot != null;

        /// <summary>
        /// バインド済みの UI Toolkit 要素。
        /// </summary>
        public VisualElement BoundElement => _actorRoot;

        /// <summary>
        /// UIDocument から俳優要素を探索し、見つかれば内部にバインドする。
        /// </summary>
        /// <returns>要素をバインドできた場合は <c>true</c>。</returns>
        /// <example>
        /// <code>
        /// if (!bridge.HasBoundElement) {
        ///     bridge.TryBind();
        /// }
        /// </code>
        /// </example>
        public bool TryBind()
        {
            if (_ui == null)
            {
                _ui = GetComponent<UIDocument>();
            }

            if (_ui == null || _ui.rootVisualElement == null)
            {
                return false;
            }

            var root = _ui.rootVisualElement;
            _actorRoot = null;
            if (!string.IsNullOrEmpty(rootName))
            {
                BindElement(root.Q<VisualElement>(rootName));
            }

            if (_actorRoot == null && !string.IsNullOrEmpty(rootClass))
            {
                BindElement(root.Query<VisualElement>(className: rootClass).First());
            }

            return _actorRoot != null;
        }

        /// <summary>
        /// 外部から指定された要素をバインドし、初期化する。
        /// </summary>
        /// <param name="element">俳優表示用の UI Toolkit 要素。</param>
        public void BindElement(VisualElement element)
        {
            _actorRoot = element;
            ConfigureActorRoot(_actorRoot);
        }

        /// <summary>
        /// 座標クランプを担う Presenter を設定し、旧 API 経由のフォワード先として保持する。
        /// </summary>
        /// <param name="presenter">割り当てる <see cref="ActorPresenter"/>。</param>
        public void SetActorPresenter(ActorPresenter presenter)
        {
            m_ActorPresenter = presenter;
        }

        /// <summary>
        /// 現在の描画位置を UI Toolkit の解決済みスタイルから読み取る。
        /// </summary>
        /// <returns>左上原点基準（px）の座標。</returns>
        public Vector2 GetPosition()
        {
            if (_actorRoot == null)
            {
                return Vector2.zero;
            }

            return new Vector2(_actorRoot.resolvedStyle.left, _actorRoot.resolvedStyle.top);
        }

        /// <summary>
        /// 指定座標へ移動する。必要に応じてパネルサイズ内にクランプする。
        /// </summary>
        /// <param name="pos">左上原点（px）での目標座標。</param>
        public void SetPosition(Vector2 pos)
        {
            if (_actorRoot == null)
            {
                return;
            }

            _actorRoot.style.left = pos.x;
            _actorRoot.style.top = pos.y;
        }

        /// <summary>
        /// パネル全体の描画範囲を取得する。左上原点（px）で表現され、右方向と下方向を正とする。
        /// </summary>
        /// <param name="boundsPx">取得したパネル境界。</param>
        /// <returns>パネルが有効で境界を算出できた場合は <c>true</c>。</returns>
        public bool TryGetPanelBounds(out Rect boundsPx)
        {
            boundsPx = default;

            if (_actorRoot == null)
            {
                return false;
            }

            var panel = _actorRoot.panel;
            if (panel == null)
            {
                return false;
            }

            var panelOrigin = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            var panelScreenMax = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));

            var minX = Mathf.Min(panelOrigin.x, panelScreenMax.x);
            var minY = Mathf.Min(panelOrigin.y, panelScreenMax.y);
            var maxX = Mathf.Max(panelOrigin.x, panelScreenMax.x);
            var maxY = Mathf.Max(panelOrigin.y, panelScreenMax.y);

            boundsPx = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        /// <summary>
        /// 現在の俳優要素のピクセルサイズを取得する。レイアウトが未解決の場合は解決済みスタイルを参照する。
        /// </summary>
        /// <param name="sizePx">幅・高さ（px）。</param>
        /// <returns>要素がバインドされている場合は <c>true</c>。</returns>
        public bool TryGetElementPixelSize(out Vector2 sizePx)
        {
            if (_actorRoot == null)
            {
                sizePx = Vector2.zero;
                return false;
            }

            sizePx = GetElementPixelSize(_actorRoot);
            return true;
        }

        /// <summary>
        /// 要素のピクセルサイズを取得する。レイアウト未確定の場合は解決済みスタイル値を使用し、最低 1px を保証する。
        /// </summary>
        /// <param name="element">サイズを測定する UI 要素。</param>
        /// <returns>幅・高さを格納したピクセル単位のベクトル。</returns>
        private Vector2 GetElementPixelSize(VisualElement element)
        {
            if (element == null)
            {
                return Vector2.one;
            }

            var world = element.worldBound;
            if (world.width > 0f && world.height > 0f)
            {
                return new Vector2(world.width, world.height);
            }

            var width = element.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f)
            {
                width = 1f;
            }

            var height = element.resolvedStyle.height;
            if (float.IsNaN(height) || height <= 0f)
            {
                height = 1f;
            }

            return new Vector2(width, height);
        }

        /// <summary>
        /// 現在位置にオフセットを加算する。
        /// </summary>
        /// <param name="delta">加算する差分ベクトル（px）。</param>
        public void Nudge(Vector2 delta)
        {
            if (_actorRoot == null)
            {
                return;
            }

            SetPosition(GetPosition() + delta);
        }

        /// <summary>
        /// 速度（px/s）と経過時間から移動量を算出し、現在位置に加算する。
        /// </summary>
        /// <param name="dir">正規化済みを想定した方向ベクトル。</param>
        /// <param name="speed">移動速度（px/s）。</param>
        /// <param name="deltaTime">経過時間（秒）。</param>
        public void NudgeBySpeed(Vector2 dir, float speed, float deltaTime)
        {
            Nudge(dir * speed * deltaTime);
        }

        /// <summary>
        /// <see cref="defaultSpeed"/> を使用して移動する簡易 API。
        /// </summary>
        /// <param name="dir">正規化済みを想定した方向ベクトル。</param>
        /// <param name="deltaTime">経過時間（秒）。</param>
        public void NudgeByDefaultSpeed(Vector2 dir, float deltaTime)
        {
            NudgeBySpeed(dir, defaultSpeed, deltaTime);
        }

        /// <summary>
        /// バインド時に UI 要素へ初期スタイルを適用し、未設定値を補正する。
        /// </summary>
        /// <param name="element">初期化対象の要素。</param>
        private void ConfigureActorRoot(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.focusable = true;

            if (float.IsNaN(element.resolvedStyle.left))
            {
                element.style.left = 0f;
            }

            if (float.IsNaN(element.resolvedStyle.top))
            {
                element.style.top = 0f;
            }
        }

        /// <summary>
        /// 旧 API 互換のクランプ処理。Presenter 層へ処理を委譲し、将来の削除に備える。
        /// </summary>
        /// <param name="posPx">左上原点座標系での希望座標（px）。</param>
        /// <returns>Presenter によってクランプされた座標。</returns>
        [Obsolete("Deprecated: Clamp moved to state mutation in Presenter/Model path. Use ActorPresenter.SetActorPositionClamped(...) or VSPresenterBridge route instead.")]
        public Vector2 ClampToPanel(Vector2 posPx)
        {
            if (!m_HasLoggedClampWarning)
            {
                Debug.LogWarning("[FUnity] FooniUIBridge.ClampToPanel is obsolete. Clamping has moved to Presenter/Model path.");
                m_HasLoggedClampWarning = true;
            }

            return ForwardClampToPresenter(posPx);
        }

        /// <summary>
        /// Presenter 側にフォワードしてクランプ済み座標を取得する。Presenter 未設定時は入力値を返す。
        /// </summary>
        /// <param name="posPx">左上原点座標系での希望座標（px）。</param>
        /// <returns>Presenter が算出したクランプ済み座標。Presenter 未設定時は入力値。</returns>
        private Vector2 ForwardClampToPresenter(Vector2 posPx)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] FooniUIBridge.ClampToPanel のフォワード先 Presenter が未設定です。");
                return posPx;
            }

            return m_ActorPresenter.TryClampPosition(posPx, out var clamped) ? clamped : posPx;
        }
    }
}
