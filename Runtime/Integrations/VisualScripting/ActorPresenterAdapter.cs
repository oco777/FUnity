// Updated: 2025-02-14
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;
using FUnity.Runtime.Presenter;
using UnityEngine.UIElements;

namespace FUnity.Runtime.Integrations.VisualScripting
{
    /// <summary>
    /// UI Toolkit 上の俳優要素を Presenter 経由で制御する MonoBehaviour アダプタです。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FooniElement"/>, Unity Visual Scripting <see cref="CustomEvent"/>, <see cref="ActorPresenter"/>
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> によって UI GameObject や Runner に付与され、Presenter から
    ///     <see cref="BindActorElement"/> および <see cref="SetActorPresenter"/> が呼び出される。ビジネスロジックは保持せず、Presenter
    ///     を経由した命令転送と View 操作に限定する。
    /// </remarks>
    public class ActorPresenterAdapter : MonoBehaviour
    {
        /// <summary>Visual Scripting からの移動命令を委譲する Presenter。未設定時は <see cref="MoveSteps"/> を拒否する。</summary>
        [SerializeField]
        private ActorPresenter m_ActorPresenter;

        /// <summary>Presenter 未接続時に保持する向き（度）。0=右, 90=上, 180=左, 270=下。</summary>
        [SerializeField]
        private float m_LocalDirectionDeg = 90f;

        // Cached references and animation state
        /// <summary>Presenter へ状態を伝える際の参照元となる VisualElement。</summary>
        private VisualElement m_BoundSource;

        /// <summary>UI への書き込み中かどうかを示す再入防止フラグ。</summary>
        private bool m_Applying;

        /// <summary>GeometryChangedEvent を購読済みかどうか。</summary>
        private bool m_IsGeometrySubscribed;

        /// <summary>GeometryChangedEvent を 1 度だけ処理したか。</summary>
        private bool m_SourceGeometryNotified;

        /// <summary>浮遊アニメーションが有効か。</summary>
        private bool m_FloatEnabled = true;

        /// <summary>上下方向の振幅（px）。</summary>
        private float m_Amplitude = 10f;

        /// <summary>1 周の周期（秒）。</summary>
        private float m_PeriodSec = 3f;

        /// <summary>周期計算用の内部フェーズ。</summary>
        private float m_Phase;

        /// <summary>現在の浮遊オフセット（UI 基準, px）。Presenter へ論理座標として伝える。</summary>
        private Vector2 m_FloatOffset = Vector2.zero;

        /// <summary>NudgeY などで調整した静的オフセット（UI 基準, px）。</summary>
        private Vector2 m_StaticOffset = Vector2.zero;

        /// <summary>UI Toolkit の schedule.Execute を保持し、再利用する。</summary>
        private IVisualElementScheduledItem m_Ticker;

        /// <summary>
        /// 現在バインドされている UI Toolkit 要素。Presenter への参照入力専用であり、直接スタイルは変更しない。
        /// </summary>
        public VisualElement BoundElement => m_BoundSource;

        /// <summary>現在の座標原点。Presenter 未設定時は TopLeft。</summary>
        public CoordinateOrigin CoordinateOrigin
        {
            get
            {
                return m_ActorPresenter != null ? m_ActorPresenter.CoordinateOrigin : CoordinateOrigin.TopLeft;
            }
        }

        /// <summary>
        /// 浮遊アニメーションが有効な場合にスケジューラを起動する。
        /// </summary>
        private void OnEnable()
        {
            if (m_BoundSource != null)
            {
                SubscribeSourceGeometry();
                ApplyPresenterTransforms();
                ApplyPresenterOffsets();
            }

            if (!m_FloatEnabled || m_BoundSource == null)
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
            UnsubscribeSourceGeometry();
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
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: BindActorElement called with null element.");
                return;
            }

            if (m_BoundSource != ve)
            {
                m_Ticker?.Pause();
                m_Ticker = null;
                UnsubscribeSourceGeometry();
            }

            m_BoundSource = ve;
            m_SourceGeometryNotified = false;
            m_StaticOffset = Vector2.zero;
            m_FloatOffset = Vector2.zero;
            SubscribeSourceGeometry();
            ApplyPresenterTransforms();

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
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため移動命令を転送できません。");
                return;
            }

            m_ActorPresenter.SetDirection(m_LocalDirectionDeg);
            ApplyPresenterOffsets();
        }

        /// <summary>
        /// 現在の向きを度単位で設定する。0=右、90=上、180=左、270=下 として扱う。
        /// </summary>
        /// <param name="degrees">設定する角度（度）。</param>
        public void SetDirection(float degrees)
        {
            m_LocalDirectionDeg = degrees;

            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため SetDirection を転送できません。");
                return;
            }

            m_ActorPresenter.SetDirection(degrees);
        }

        /// <summary>
        /// 現在の向きを度単位で取得する。0=右、90=上、180=左、270=下 を想定する。
        /// </summary>
        /// <returns>現在の向き（度）。</returns>
        public float GetDirection()
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため GetDirection はローカル値を返します。");
                return m_LocalDirectionDeg;
            }

            var direction = m_ActorPresenter.GetDirection();
            m_LocalDirectionDeg = direction;
            return direction;
        }

        /// <summary>
        /// Scratch の「〇歩動かす」に相当する移動を実行する。現在の向きに沿って steps×10px 分だけ瞬間的に移動させる。
        /// </summary>
        /// <param name="steps">移動する歩数。負値を指定すると逆方向へ移動する。</param>
        public void MoveSteps(float steps)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため MoveSteps を実行できません。");
                return;
            }

            m_ActorPresenter.MoveSteps(steps);
        }

        /// <summary>
        /// 俳優の中心座標をピクセル単位で取得する。
        /// </summary>
        /// <returns>現在の中心座標（px）。Presenter が未接続の場合は <see cref="Vector2.zero"/> を返す。</returns>
        public Vector2 GetPositionPixels()
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため GetPositionPixels を実行できません。");
                return Vector2.zero;
            }

            var logical = m_ActorPresenter.GetPosition();
            return m_ActorPresenter.ToUiPosition(logical);
        }

        /// <summary>
        /// 俳優の絶対座標（中心）をピクセル単位で設定する。
        /// </summary>
        /// <param name="positionPx">適用する中心座標（px）。右=+X、下=+Y。</param>
        public void SetPositionPixels(Vector2 positionPx)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため SetPositionPixels を実行できません。");
                return;
            }

            m_ActorPresenter.SetPositionPixels(positionPx);
        }

        /// <summary>
        /// 論理座標を UI 座標へ変換する。
        /// </summary>
        /// <param name="logical">論理座標。</param>
        /// <returns>俳優画像の中心座標（UI）。</returns>
        public Vector2 ToUiPosition(Vector2 logical)
        {
            if (m_ActorPresenter != null)
            {
                return m_ActorPresenter.ToUiPosition(logical);
            }

            return CoordinateConverter.LogicalToUI(logical, CoordinateOrigin);
        }

        /// <summary>
        /// UI 座標（中心）を論理座標へ変換する。
        /// </summary>
        /// <param name="ui">俳優画像の中心座標。</param>
        /// <returns>論理座標。</returns>
        public Vector2 ToLogicalPosition(Vector2 ui)
        {
            if (m_ActorPresenter != null)
            {
                return m_ActorPresenter.ToLogicalPosition(ui);
            }

            return CoordinateConverter.UIToLogical(ui, CoordinateOrigin);
        }

        /// <summary>
        /// UI 座標系の差分を論理座標系の差分へ変換する。
        /// </summary>
        /// <param name="uiDelta">UI 座標系での差分。</param>
        /// <returns>論理座標系の差分。</returns>
        public Vector2 ToLogicalDelta(Vector2 uiDelta)
        {
            if (CoordinateOrigin == CoordinateOrigin.Center)
            {
                return new Vector2(uiDelta.x, -uiDelta.y);
            }

            return uiDelta;
        }

        /// <summary>
        /// 論理座標系の差分を UI 座標系へ変換する。
        /// </summary>
        /// <param name="logicalDelta">論理座標系での差分。</param>
        /// <returns>UI 座標系での差分。</returns>
        public Vector2 ToUiDelta(Vector2 logicalDelta)
        {
            if (m_ActorPresenter != null)
            {
                return m_ActorPresenter.ToUiDelta(logicalDelta);
            }

            if (CoordinateOrigin == CoordinateOrigin.Center)
            {
                return new Vector2(logicalDelta.x, -logicalDelta.y);
            }

            return logicalDelta;
        }

        /// <summary>
        /// 座標変換で使用するステージ要素を解決する。Presenter 未設定時はバインド要素の親を返す。
        /// </summary>
        /// <returns>変換の基準に用いる <see cref="VisualElement"/>。</returns>
        private VisualElement ResolveStageRootForConversion()
        {
            if (m_ActorPresenter != null && m_ActorPresenter.StageRootElement != null)
            {
                return m_ActorPresenter.StageRootElement;
            }

            return m_BoundSource != null ? m_BoundSource.parent : null;
        }

        /// <summary>
        /// 等倍基準のスケール値を直接指定し、Presenter に転送する。
        /// </summary>
        /// <param name="scale">適用するスケール。1 で等倍。</param>
        public void SetScale(float scale)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため SetScale を実行できません。");
                return;
            }

            m_ActorPresenter.SetScale(scale);
        }

        /// <summary>
        /// 拡大率（%）を指定し、内部的にスケールへ変換して <see cref="SetScale(float)"/> に委譲する。
        /// </summary>
        /// <param name="percent">100 で等倍となる拡大率（%）。</param>
        public void SetSizePercent(float percent)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため SetSizePercent を実行できません。");
                return;
            }

            SetScale(percent / 100f);
        }

        /// <summary>
        /// 拡大率（%）を相対値で変更し、Presenter を通じて現在の値へ加算する。
        /// </summary>
        /// <param name="deltaPercent">加算する拡大率（%）。正で拡大、負で縮小。</param>
        public void ChangeSizeByPercent(float deltaPercent)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため ChangeSizeByPercent を実行できません。");
                return;
            }

            m_ActorPresenter.ChangeSizeByPercent(deltaPercent);
        }

        /// <summary>
        /// 俳優の座標にピクセル単位の差分を加算する。
        /// </summary>
        /// <param name="deltaPx">加算する座標差分（px）。右=+X、下=+Y。</param>
        public void AddPositionPixels(Vector2 deltaPx)
        {
            if (m_ActorPresenter == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: ActorPresenter が未設定のため AddPositionPixels を実行できません。");
                return;
            }

            if (deltaPx.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            m_ActorPresenter.AddPositionPixels(deltaPx);
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
                m_FloatOffset = Vector2.zero;
                ApplyPresenterOffsets();
                m_Ticker?.Pause();
                return;
            }

            if (m_BoundSource == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenterAdapter: EnableFloat called without a bound actor element.");
                return;
            }

            if (m_Ticker == null)
            {
                StartTicker();
            }
            m_Ticker?.Resume();
            ApplyPresenterOffsets();
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

            if (m_FloatEnabled && m_BoundSource != null && m_Ticker != null)
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
            m_StaticOffset.y += deltaPx;
            ApplyPresenterOffsets();
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
            Debug.Log($"ActorPresenterAdapter says: {message}");
        }

        // ===== Internal: schedule-based floating =====
        /// <summary>
        /// UI Toolkit のスケジューラを使用して浮遊アニメーションを更新する。
        /// </summary>
        private void StartTicker()
        {
            m_Ticker?.Pause();
            if (m_BoundSource == null) return;

            m_Phase = 0f;
            m_FloatOffset = Vector2.zero;
            ApplyPresenterOffsets();
            m_Ticker = m_BoundSource.schedule.Execute(() =>
            {
                if (!m_FloatEnabled || m_PeriodSec <= 0f) return;

                m_Phase += Time.deltaTime / m_PeriodSec;
                if (m_Phase > 1f) m_Phase -= 1f;

                float offset = Mathf.Sin(m_Phase * Mathf.PI * 2f) * m_Amplitude;
                m_FloatOffset = new Vector2(0f, offset);
                ApplyPresenterOffsets();
            }).Every(16); // ~60 FPS
        }

        /// <summary>
        /// Presenter へ現在のオフセットを伝え、View を更新させる。
        /// </summary>
        private void ApplyPresenterOffsets()
        {
            if (m_ActorPresenter == null)
            {
                return;
            }

            if (m_Applying)
            {
                return;
            }

            m_Applying = true;
            try
            {
                var uiOffset = m_StaticOffset + m_FloatOffset;
                var logicalOffset = ConvertUiOffsetToLogical(uiOffset);
                m_ActorPresenter.SetFloatOffset(logicalOffset);
            }
            finally
            {
                m_Applying = false;
            }
        }

        /// <summary>
        /// UI 基準の中心オフセットを現在の座標原点に合わせた論理座標へ変換する。
        /// </summary>
        /// <param name="uiOffset">UI Toolkit 基準でのオフセット（px）。</param>
        /// <returns>論理座標系でのオフセット。</returns>
        private Vector2 ConvertUiOffsetToLogical(Vector2 uiOffset)
        {
            if (CoordinateOrigin == CoordinateOrigin.Center)
            {
                return new Vector2(uiOffset.x, -uiOffset.y);
            }

            return uiOffset;
        }

        /// <summary>
        /// Presenter が保持している座標・角度・スケールを再適用させる。
        /// </summary>
        private void ApplyPresenterTransforms()
        {
            if (m_ActorPresenter == null)
            {
                return;
            }

            if (m_Applying)
            {
                return;
            }

            m_Applying = true;
            try
            {
                m_ActorPresenter.ApplyAllTransforms();
            }
            finally
            {
                m_Applying = false;
            }
        }

        /// <summary>
        /// バインド元要素の GeometryChangedEvent を監視し、初回レイアウト確定で Presenter を起動する。
        /// </summary>
        private void SubscribeSourceGeometry()
        {
            if (m_BoundSource == null)
            {
                return;
            }

            if (m_IsGeometrySubscribed || m_SourceGeometryNotified)
            {
                return;
            }

            m_BoundSource.RegisterCallback<GeometryChangedEvent>(OnSourceGeometryChanged);
            m_IsGeometrySubscribed = true;
        }

        /// <summary>
        /// GeometryChangedEvent の購読を解除し、ループを防止する。
        /// </summary>
        private void UnsubscribeSourceGeometry()
        {
            if (m_BoundSource == null)
            {
                return;
            }

            if (!m_IsGeometrySubscribed)
            {
                return;
            }

            m_BoundSource.UnregisterCallback<GeometryChangedEvent>(OnSourceGeometryChanged);
            m_IsGeometrySubscribed = false;
        }

        /// <summary>
        /// バインド元のレイアウトが確定した際に Presenter へ再適用を要求する。
        /// </summary>
        /// <param name="evt">UI Toolkit が通知するジオメトリ情報。</param>
        private void OnSourceGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_SourceGeometryNotified)
            {
                return;
            }

            m_SourceGeometryNotified = true;
            UnsubscribeSourceGeometry();
            ApplyPresenterTransforms();
            ApplyPresenterOffsets();
        }
    }
}
