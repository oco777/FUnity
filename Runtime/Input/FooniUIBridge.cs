// Updated: 2025-02-14
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

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
        /// `true` の場合、UI ドキュメントの描画領域内に座標をクランプする。
        /// </summary>
        public bool clampToPanel = true;

        private UIDocument _ui;
        private VisualElement _actorRoot;

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

            var size = new Vector2(_actorRoot.resolvedStyle.width, _actorRoot.resolvedStyle.height);
            var clamped = ClampToPanel(pos, size);
            _actorRoot.style.left = clamped.x;
            _actorRoot.style.top = clamped.y;
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
        /// パネル境界内に収まるよう座標を制限する。
        /// </summary>
        /// <param name="pos">希望座標。</param>
        /// <param name="size">要素サイズ（px）。</param>
        /// <returns>クランプ後の座標。</returns>
        private Vector2 ClampToPanel(Vector2 pos, Vector2 size)
        {
            if (!clampToPanel || _ui == null || _ui.rootVisualElement == null)
            {
                return pos;
            }

            var panel = _ui.rootVisualElement.resolvedStyle;
            var maxX = Mathf.Max(0, panel.width - size.x);
            var maxY = Mathf.Max(0, panel.height - size.y);
            pos.x = Mathf.Clamp(pos.x, 0, maxX);
            pos.y = Mathf.Clamp(pos.y, 0, maxY);
            return pos;
        }
    }
}
