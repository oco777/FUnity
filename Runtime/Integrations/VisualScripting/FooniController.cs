// Updated: 2025-02-14
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// UI Toolkit の <see cref="FooniElement"/> を Unity Visual Scripting から制御する View 向けブリッジ。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="UIDocument"/>, <see cref="FooniElement"/>, Unity Visual Scripting <see cref="CustomEvent"/>
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> によって UI GameObject に付与され、Awake で UIDocument を解決
    ///     した後に Presenter から <see cref="BindActorElement"/> が呼び出される。ビジネスロジックは保持せず、View 操作とイベント発行のみに徹する。
    /// </remarks>
    public class FooniController : MonoBehaviour
    {
        /// <summary>「1 歩」をピクセル換算するためのスケール（px/歩）。</summary>
        private const float StepToPixels = 10f;

        /// <summary>
        /// 制御対象の <see cref="UIDocument"/>。null の場合は <see cref="Awake"/> で同一 GameObject から補完する。
        /// </summary>
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiDocument")]
        private UIDocument m_UIDocument;

        /// <summary>Visual Scripting からの移動命令を委譲する Presenter。未設定時は <see cref="MoveSteps"/> を拒否する。</summary>
        [SerializeField]
        private ActorPresenter m_ActorPresenter;

        /// <summary>現在の向き（度）。0=右, 90=上, 180=左, 270=下。</summary>
        [SerializeField]
        private float m_DirectionDeg = 90f;

        /// <summary>
        /// 制御対象の UIDocument を外部から差し替える。
        /// </summary>
        /// <param name="doc">UI Toolkit ドキュメント。null を渡すと後続の Awake で再解決する。</param>
        /// <example>
        /// <code>
        /// controller.SetUIDocument(GetComponent&lt;UIDocument&gt;());
        /// </code>
        /// </example>
        public void SetUIDocument(UIDocument doc)
        {
            m_UIDocument = doc;
        }

        // Cached references and animation state
        private VisualElement m_BoundElement;
        private bool m_FloatEnabled = true;
        private float m_Amplitude = 10f;   // px
        private float m_PeriodSec = 3f;    // seconds
        private float m_Phase;
        private Vector3 m_BaseTranslation;
        private float m_CurrentOffset;

        private IVisualElementScheduledItem m_Ticker;

        /// <summary>
        /// 現在バインドされている UI Toolkit 要素。
        /// </summary>
        public VisualElement BoundElement => m_BoundElement;

        /// <summary>
        /// UIDocument と既存の FooniElement を探索し、未バインド時は Presenter からの通知を待つ。
        /// </summary>
        private void Awake()
        {
            // Ensure UIDocument and root
            if (!m_UIDocument) m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument ? m_UIDocument.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("[FUnity] FooniController: UIDocument / rootVisualElement is missing.");
                return;
            }

            if (m_BoundElement == null)
            {
                var existing = root.Q<FooniElement>();
                if (existing != null)
                {
                    BindActorElement(existing);
                    Debug.LogWarning("[FUnity] FooniController: auto-bound existing FooniElement (legacy path). Use FUnityManager-driven actors instead.");
                }
                else
                {
                    Debug.LogWarning("[FUnity] FooniController: no actor element bound. Awaiting BindActorElement from FUnityManager.");
                }
            }
        }

        /// <summary>
        /// 浮遊アニメーションが有効な場合にスケジューラを起動する。
        /// </summary>
        private void OnEnable()
        {
            if (!m_FloatEnabled || m_BoundElement == null)
            {
                return;
            }

            if (m_Ticker == null)
            {
                StartTicker();
            }
            else
            {
                m_Ticker.Resume();
            }
        }

        /// <summary>
        /// 無効化時にはスケジューラを一時停止する。
        /// </summary>
        private void OnDisable()
        {
            m_Ticker?.Pause();
        }

        // ===== Visual Scripting APIs (auto-exposed as units) =====

        /// <summary>
        /// Presenter や Visual Scripting から俳優要素を結び付ける。
        /// </summary>
        /// <param name="ve">制御対象の VisualElement。</param>
        /// <example>
        /// <code>
        /// controller.BindActorElement(actorElement);
        /// </code>
        /// </example>
        public void BindActorElement(VisualElement ve)
        {
            if (ve == null)
            {
                Debug.LogWarning("[FUnity] FooniController: BindActorElement called with null element.");
                return;
            }

            if (m_BoundElement != ve)
            {
                m_Ticker?.Pause();
                m_Ticker = null;
            }

            m_BoundElement = ve;
            m_BaseTranslation = ResolveBaseTranslation(m_BoundElement);
            m_CurrentOffset = 0f;
            ApplyTranslation();

            if (m_FloatEnabled && isActiveAndEnabled)
            {
                StartTicker();
            }
        }

        /// <summary>
        /// Visual Scripting 経由で移動を指示する Presenter を設定する。
        /// </summary>
        /// <param name="presenter">俳優の Model と View を調停する Presenter。</param>
        public void SetActorPresenter(ActorPresenter presenter)
        {
            m_ActorPresenter = presenter;
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] FooniController: ActorPresenter が未設定のため移動命令を転送できません。");
            }
        }

        /// <summary>
        /// 現在の向きを度単位で設定する。0=右、90=上、180=左、270=下 として扱う。
        /// </summary>
        /// <param name="degrees">設定する角度（度）。</param>
        public void SetDirection(float degrees)
        {
            m_DirectionDeg = degrees;
        }

        /// <summary>
        /// 現在の向きを度単位で取得する。0=右、90=上、180=左、270=下 を想定する。
        /// </summary>
        /// <returns>現在の向き（度）。</returns>
        public float GetDirection()
        {
            return m_DirectionDeg;
        }

        /// <summary>
        /// Scratch の「〇歩動かす」に相当する移動を実行する。現在の向きに沿って steps×10px 分だけ瞬間的に移動させる。
        /// </summary>
        /// <param name="steps">移動する歩数。負値を指定すると逆方向へ移動する。</param>
        public void MoveSteps(float steps)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] FooniController: ActorPresenter が未設定のため MoveSteps を実行できません。");
                return;
            }

            var radians = m_DirectionDeg * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians));
            var deltaPx = direction * (steps * StepToPixels);

            if (deltaPx.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            m_ActorPresenter.MoveByPixels(deltaPx);
        }

        /// <summary>
        /// 俳優の絶対座標をピクセル単位で取得する。
        /// </summary>
        /// <returns>現在の座標（px）。Presenter が未接続の場合は <see cref="Vector2.zero"/> を返す。</returns>
        public Vector2 GetPositionPixels()
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] FooniController: ActorPresenter が未設定のため GetPositionPixels を実行できません。");
                return Vector2.zero;
            }

            return m_ActorPresenter.GetPosition();
        }

        /// <summary>
        /// 俳優の絶対座標をピクセル単位で設定する。
        /// </summary>
        /// <param name="positionPx">適用する座標（px）。右=+X、下=+Y。</param>
        public void SetPositionPixels(Vector2 positionPx)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] FooniController: ActorPresenter が未設定のため SetPositionPixels を実行できません。");
                return;
            }

            m_ActorPresenter.SetPosition(positionPx);
        }

        /// <summary>
        /// 俳優の座標にピクセル単位の差分を加算する。
        /// </summary>
        /// <param name="deltaPx">加算する座標差分（px）。右=+X、下=+Y。</param>
        public void AddPositionPixels(Vector2 deltaPx)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] FooniController: ActorPresenter が未設定のため AddPositionPixels を実行できません。");
                return;
            }

            if (deltaPx.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            m_ActorPresenter.MoveByPixels(deltaPx);
        }

        /// <summary>
        /// 浮遊アニメーションの有効/無効を切り替える。
        /// </summary>
        /// <param name="enabled">true で有効化。</param>
        /// <example>
        /// <code>
        /// controller.EnableFloat(false);
        /// </code>
        /// </example>
        public void EnableFloat(bool enabled)
        {
            m_FloatEnabled = enabled;

            if (!m_FloatEnabled)
            {
                if (m_BoundElement != null)
                {
                    // Reset translation when disabled (visual reset)
                    m_CurrentOffset = 0f;
                    ApplyTranslation();
                }
                m_Ticker?.Pause();
                return;
            }

            if (m_BoundElement == null)
            {
                Debug.LogWarning("[FUnity] FooniController: EnableFloat called without a bound actor element.");
                return;
            }

            if (m_Ticker == null)
            {
                StartTicker();
            }
            m_Ticker?.Resume();
        }

        /// <summary>
        /// 浮遊の振幅（px）を設定する。負値は 0 に切り上げる。
        /// </summary>
        /// <param name="amplitudePx">上下方向の変位量（px）。</param>
        /// <example>
        /// <code>
        /// controller.SetFloatAmplitude(20f);
        /// </code>
        /// </example>
        public void SetFloatAmplitude(float amplitudePx)
        {
            m_Amplitude = Mathf.Max(0f, amplitudePx);
        }

        /// <summary>
        /// 浮遊周期（秒）を設定する。0.1 秒より短い値は 0.1 秒に制限する。
        /// </summary>
        /// <param name="periodSeconds">1 周あたりの時間。</param>
        /// <example>
        /// <code>
        /// controller.SetFloatPeriod(2.5f);
        /// </code>
        /// </example>
        public void SetFloatPeriod(float periodSeconds)
        {
            m_PeriodSec = Mathf.Max(0.1f, periodSeconds);

            if (m_FloatEnabled && m_BoundElement != null && m_Ticker != null)
            {
                StartTicker();
            }
        }

        /// <summary>
        /// Y 軸方向に即時的なオフセットを加算する。
        /// </summary>
        /// <param name="deltaPx">加算するピクセル量。</param>
        /// <example>
        /// <code>
        /// controller.NudgeY(-8f);
        /// </code>
        /// </example>
        public void NudgeY(float deltaPx)
        {
            if (m_BoundElement == null) return;

            m_BaseTranslation.y += deltaPx;
            ApplyTranslation();
        }

        /// <summary>
        /// Visual Scripting グラフへカスタムイベント <c>Fooni/Say</c> を発行する。
        /// </summary>
        /// <param name="message">イベントに添付するメッセージ。</param>
        /// <example>
        /// <code>
        /// controller.Say("Hello!");
        /// </code>
        /// </example>
        public void Say(string message)
        {
            CustomEvent.Trigger(gameObject, "Fooni/Say", message);
            Debug.Log($"Fooni says: {message}");
        }

        // ===== Internal: schedule-based floating =====
        /// <summary>
        /// UI Toolkit のスケジューラを使用して浮遊アニメーションを更新する。
        /// </summary>
        private void StartTicker()
        {
            m_Ticker?.Pause();
            if (m_BoundElement == null) return;

            m_Phase = 0f;
            m_CurrentOffset = 0f;
            m_Ticker = m_BoundElement.schedule.Execute(() =>
            {
                if (!m_FloatEnabled || m_PeriodSec <= 0f) return;

                m_Phase += Time.deltaTime / m_PeriodSec;
                if (m_Phase > 1f) m_Phase -= 1f;

                float offset = Mathf.Sin(m_Phase * Mathf.PI * 2f) * m_Amplitude;
                m_CurrentOffset = offset;
                ApplyTranslation();
            }).Every(16); // ~60 FPS
        }

        /// <summary>
        /// 現在のベース位置と浮遊オフセットを <see cref="VisualElement.style"/> に反映する。
        /// </summary>
        private void ApplyTranslation()
        {
            if (m_BoundElement == null)
            {
                return;
            }

            m_BoundElement.style.translate = new Translate(m_BaseTranslation.x, m_BaseTranslation.y + m_CurrentOffset, m_BaseTranslation.z);
        }

        /// <summary>
        /// 要素が既に保持している translate 値を初期値として取得する。
        /// </summary>
        /// <param name="element">対象要素。</param>
        /// <returns>translate のベース値。</returns>
        private static Vector3 ResolveBaseTranslation(VisualElement element)
        {
            if (element == null)
            {
                return Vector3.zero;
            }

            var translate = element.style.translate;
            if (translate.keyword == StyleKeyword.Undefined)
            {
                var value = translate.value;
                return new Vector3(value.x.value, value.y.value, value.z);
            }

            var resolved = element.resolvedStyle.translate;
            return new Vector3(resolved.x, resolved.y, resolved.z);
        }
    }
}
