// Updated: 2025-03-03
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Model;
using FUnity.Runtime.View;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 俳優の状態（Model）と UI 表示（View）を調停する Presenter。
    /// 入力から得た移動ベクトルを <see cref="ActorState"/> に反映し、View に一方向で通知する。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnityActorData"/>, <see cref="ActorState"/>, <see cref="IActorView"/>
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> が俳優生成時に <see cref="Initialize"/> を呼び出し、
    ///     以降は <see cref="Tick"/> をフレーム毎に実行する。Presenter 自体はステートレスであり、Model/View 間の同期のみを担当。
    /// </remarks>
    public sealed class ActorPresenter
    {
        /// <summary>Scratch 互換の「1 歩」をピクセル換算する係数（px/歩）。</summary>
        private const float StepToPixels = 10f;

        /// <summary>俳優の初期向き（度）。Scratch 互換で 90°=上。</summary>
        private const float DefaultDirectionDeg = 90f;

        /// <summary>拡大率として許容する最小値（%）。これ未満の値は 1% に丸め込む。</summary>
        private const float MinSizePercent = 1f;

        /// <summary>拡大率として許容する最大値（%）。これより大きい値は 300% に丸め込む。</summary>
        private const float MaxSizePercent = 300f;

        /// <summary>ランタイム状態を保持する Model。</summary>
        private ActorState m_State;

        /// <summary>UI Toolkit 側の描画を担当する View。</summary>
        private IActorView m_View;

        /// <summary>ポートレート表示用に生成したランタイムスプライト。</summary>
        private Sprite m_RuntimePortrait;

        /// <summary>この俳優専用にバインドされた Visual Scripting の ScriptMachine。</summary>
        private ScriptMachine m_ScriptMachine;

        /// <summary>初期サイズ（幅・高さ）。0 の場合は View 側の既定値を利用する。</summary>
        private Vector2 m_BaseSize = Vector2.zero;

        /// <summary>現在のスケール。等倍は 1。</summary>
        private float m_CurrentScale = 1f;

        /// <summary>Presenter が保持するステージ領域（左上原点座標系）。</summary>
        private Rect m_StageBoundsPx = new Rect(0f, 0f, 0f, 0f);

        /// <summary>俳優の左上座標をクランプするための矩形。</summary>
        private Rect m_PositionBoundsPx = new Rect(0f, 0f, 0f, 0f);

        /// <summary>直近で取得した俳優要素の描画サイズ（px）。</summary>
        private Vector2 m_LastMeasuredSizePx = Vector2.zero;

        /// <summary>境界計算が完了しているかどうか。</summary>
        private bool m_HasPositionBounds;

        /// <summary>View の境界イベントに購読済みかどうか。</summary>
        private bool m_IsSubscribedToBounds;

        /// <summary>ステージのピクセル境界（左上原点）。</summary>
        public Rect StageBoundsPx => m_StageBoundsPx;

        /// <summary>
        /// モデルとビューを初期化し、初期位置・速度・ポートレートを反映する。
        /// </summary>
        /// <param name="data">静的設定。null の場合は既定値を使用。</param>
        /// <param name="state">既存の状態。null の場合は内部で新規生成する。</param>
        /// <param name="view">表示先の View。</param>
        /// <example>
        /// <code>
        /// presenter.Initialize(actorData, new ActorState(), actorView);
        /// </code>
        /// </example>
        public void Initialize(FUnityActorData data, ActorState state, IActorView view)
        {
            var resolvedState = state ?? new ActorState();
            var shouldInitializeDirection = state == null;

            DetachViewEvents();

            m_State = resolvedState;
            m_View = view;

            if (m_State != null)
            {
                if (m_State.SizePercent <= 0f)
                {
                    m_State.SizePercent = 100f;
                }

                m_State.SizePercent = Mathf.Clamp(m_State.SizePercent, MinSizePercent, MaxSizePercent);
                m_CurrentScale = m_State.SizePercent / 100f;
            }
            else
            {
                m_CurrentScale = 1f;
            }

            var initialPosition = data != null ? data.InitialPosition : Vector2.zero;
            m_State.SetPositionUnchecked(initialPosition);
            m_State.Speed = Mathf.Max(0f, data != null ? GetConfiguredSpeed(data) : 300f);
            if (shouldInitializeDirection)
            {
                m_State.DirectionDeg = DefaultDirectionDeg;
                m_State.RotationDeg = 0f;
            }
            else
            {
                m_State.RotationDeg = Normalize0To360(m_State.RotationDeg);
            }

            m_BaseSize = data != null ? data.Size : Vector2.zero;
            m_LastMeasuredSizePx = Vector2.zero;
            m_StageBoundsPx = new Rect(0f, 0f, 0f, 0f);
            m_PositionBoundsPx = new Rect(0f, 0f, 0f, 0f);
            m_HasPositionBounds = false;

            if (m_View != null)
            {
                if (m_RuntimePortrait == null && data?.Portrait != null)
                {
                    m_RuntimePortrait = Sprite.Create(
                        data.Portrait,
                        new Rect(0f, 0f, data.Portrait.width, data.Portrait.height),
                        new Vector2(0.5f, 0.5f));
                }

                if (m_RuntimePortrait != null)
                {
                    m_View.SetPortrait(m_RuntimePortrait);
                }

                if (m_BaseSize.x > 0f || m_BaseSize.y > 0f)
                {
                    m_View.SetSize(m_BaseSize);
                }

                m_View.SetRotationDegrees(m_State.RotationDeg);

                AttachViewEvents();
                RefreshStageBoundsFromView();
            }

            SetSizePercent(m_State.SizePercent);

            if (m_View != null)
            {
                m_View.SetPosition(m_State.Position);
            }
        }

        /// <summary>
        /// Visual Scripting の ScriptMachine に Presenter・View・UI 要素をバインドし、グラフが自分自身のみを操作するよう制限する。
        /// </summary>
        /// <param name="machine">紐付ける ScriptMachine。</param>
        /// <param name="uiElement">この俳優の UI ルート要素。</param>
        public void BindScriptMachine(ScriptMachine machine, VisualElement uiElement)
        {
            if (machine == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenter: ScriptMachine が null のため Graph Variables へ Self を登録できません。");
                return;
            }

            m_ScriptMachine = machine;

            var objectVariables = Variables.Object(machine);
            if (objectVariables == null)
            {
                objectVariables = Variables.Object(machine.gameObject);
            }
            if (objectVariables != null)
            {
                objectVariables.Set("presenter", this);
                objectVariables.Set("selfPresenter", this);
                objectVariables.Set(nameof(ActorPresenter), this);

                if (uiElement != null)
                {
                    objectVariables.Set("ui", uiElement);
                    objectVariables.Set("selfUI", uiElement);
                }

                if (m_View is Object viewComponent && viewComponent != null)
                {
                    objectVariables.Set("view", viewComponent);
                    objectVariables.Set("selfView", viewComponent);
                }
                else if (m_View != null)
                {
                    objectVariables.Set("view", m_View);
                    objectVariables.Set("selfView", m_View);
                }
            }
            else
            {
                Debug.LogWarning("[FUnity] ActorPresenter: ScriptMachine の Object Variables にアクセスできないため Self を登録できません。Variables.Object(flow.stack) から参照できるよう Runner の構成を確認してください。");
            }
        }

        /// <summary>
        /// 入力方向と経過時間をもとに Model を更新し、View へ最新座標を反映する。
        /// </summary>
        /// <param name="deltaTime">経過時間（秒）。0 以下の場合は位置更新をスキップ。</param>
        /// <param name="inputDir">入力方向。必要に応じて正規化される。</param>
        /// <example>
        /// <code>
        /// presenter.Tick(Time.deltaTime, move);
        /// </code>
        /// </example>
        public void Tick(float deltaTime, Vector2 inputDir)
        {
            if (m_State == null)
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                m_View?.SetPosition(m_State.Position);
                return;
            }

            var direction = inputDir;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            if (direction.sqrMagnitude > 0f && m_State.Speed > 0f)
            {
                var delta = direction * (m_State.Speed * deltaTime);
                ApplyDelta(delta);
                return;
            }

            m_View?.SetPosition(m_State.Position);
        }

        /// <summary>
        /// 絶対座標を直接指定して俳優を移動させる。
        /// Visual Scripting の Custom Event "Actor/SetPosition" から利用することを想定。
        /// </summary>
        /// <param name="position">適用する座標。</param>
        public void SetPosition(Vector2 position)
        {
            ApplyPosition(position);
        }

        /// <summary>
        /// 現在の座標を取得する。Presenter 未初期化時は原点を返す。
        /// </summary>
        /// <returns>現在の座標（px）。</returns>
        public Vector2 GetPosition()
        {
            if (m_State == null)
            {
                return Vector2.zero;
            }

            return m_State.Position;
        }

        /// <summary>
        /// 現在位置からピクセル単位の差分を加算し、即座に View へ反映する。
        /// Visual Scripting の「〇歩動かす」ブロックからの呼び出しを想定し、Presenter 経由で Model を更新する。
        /// </summary>
        /// <param name="deltaPx">加算する移動量（px）。右+ / 下+ の座標系を想定。</param>
        public void MoveByPixels(Vector2 deltaPx)
        {
            ApplyDelta(deltaPx);
        }

        /// <summary>
        /// 現在位置から相対移動させる。
        /// 既存の API 互換性を維持しつつ <see cref="MoveByPixels"/> に処理を委譲する。
        /// </summary>
        /// <param name="delta">移動量。</param>
        public void MoveBy(Vector2 delta)
        {
            ApplyDelta(delta);
        }

        /// <summary>
        /// 指定した座標を現在のクランプ矩形に合わせて補正する。境界未設定時は入力値を返す。
        /// </summary>
        /// <param name="positionPx">補正対象の座標（px）。</param>
        /// <param name="clampedPx">クランプ後の座標。</param>
        /// <returns>クランプを実行した場合は <c>true</c>。</returns>
        public bool TryClampPosition(Vector2 positionPx, out Vector2 clampedPx)
        {
            if (!m_HasPositionBounds)
            {
                clampedPx = positionPx;
                return false;
            }

            var minX = m_PositionBoundsPx.xMin;
            var minY = m_PositionBoundsPx.yMin;
            var maxX = m_PositionBoundsPx.xMax;
            var maxY = m_PositionBoundsPx.yMax;

            var clampedX = Mathf.Clamp(positionPx.x, minX, maxX);
            var clampedY = Mathf.Clamp(positionPx.y, minY, maxY);

            clampedPx = new Vector2(clampedX, clampedY);
            return true;
        }

        /// <summary>
        /// 吹き出しを表示する。View 実装側で時間管理を行う。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        /// <param name="seconds">表示時間。</param>
        public void Say(string message, float seconds)
        {
            if (m_View == null)
            {
                return;
            }

            m_View.ShowSpeech(message, Mathf.Max(0.1f, seconds));
        }

        /// <summary>
        /// スケールを設定し、Scratch 互換の 100% 基準へ変換して <see cref="SetSizePercent(float)"/> を呼び出す。
        /// </summary>
        /// <param name="scale">等倍=1 としたスケール値。</param>
        public void SetScale(float scale)
        {
            SetSizePercent(scale * 100f);
        }

        /// <summary>
        /// 拡大率（%）を絶対値で設定し、View 側へ等比スケールを反映する。
        /// </summary>
        /// <param name="percent">100 で等倍となる拡大率（%）。</param>
        public void SetSizePercent(float percent)
        {
            var clamped = Mathf.Clamp(percent, MinSizePercent, MaxSizePercent);

            if (m_State != null)
            {
                m_State.SizePercent = clamped;
            }

            m_CurrentScale = clamped / 100f;

            if (m_View != null)
            {
                m_View.SetSizePercent(clamped);
            }

            UpdateRenderedSizeFromView();
            ClampStateToBounds();
        }

        /// <summary>
        /// 拡大率（%）を相対値で変更し、現在値に加算した結果を即時反映する。
        /// </summary>
        /// <param name="deltaPercent">加算する差分（%）。正で拡大、負で縮小。</param>
        public void ChangeSizeByPercent(float deltaPercent)
        {
            var currentPercent = m_State != null ? m_State.SizePercent : m_CurrentScale * 100f;
            SetSizePercent(currentPercent + deltaPercent);
        }

        /// <summary>
        /// 現在角度に相対加算で回転させ、View へ style.rotate を適用する。
        /// </summary>
        /// <param name="deltaDeg">加算する角度（度）。正で反時計回り。</param>
        public void RotateBy(float deltaDeg)
        {
            if (m_State == null)
            {
                return;
            }

            var normalized = Normalize0To360(m_State.RotationDeg + deltaDeg);
            m_State.RotationDeg = normalized;

            m_View?.SetRotationDegrees(normalized);
        }

        /// <summary>
        /// Visual Scripting の「自分自身を回す」命令を受け取り、内部の <see cref="RotateBy(float)"/> を経由して反映する。
        /// </summary>
        /// <param name="deltaDegrees">加算する角度（度）。</param>
        public void TurnSelf(float deltaDegrees)
        {
            RotateBy(deltaDegrees);
        }

        /// <summary>
        /// 絶対角度を指定して回転状態を更新し、View に即時反映する。
        /// </summary>
        /// <param name="degrees">適用する角度（度）。</param>
        public void SetRotation(float degrees)
        {
            if (m_State == null)
            {
                return;
            }

            var normalized = Normalize0To360(degrees);
            m_State.RotationDeg = normalized;

            m_View?.SetRotationDegrees(normalized);
        }

        /// <summary>
        /// 幅と高さを直接指定して View のサイズを変更する。
        /// </summary>
        /// <param name="size">幅・高さ（px）。負値の場合は無視される。</param>
        public void SetSize(Vector2 size)
        {
            var clamped = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
            m_BaseSize = clamped;

            if (m_View != null)
            {
                m_View.SetSize(clamped);
            }

            UpdateRenderedSizeFromView();
            ClampStateToBounds();
        }

        /// <summary>
        /// <see cref="FUnityActorData.MoveSpeed"/> をクランプして返す。
        /// </summary>
        /// <param name="data">俳優設定。</param>
        /// <returns>0 以上の移動速度。</returns>
        private static float GetConfiguredSpeed(FUnityActorData data)
        {
            if (data == null)
            {
                return 300f;
            }

            return Mathf.Max(0f, data.MoveSpeed);
        }

        /// <summary>
        /// VisualTreeAsset を指定して俳優 UI を構築し、必要な PanelSettings を適用する。Runner 側に UIDocument を直接付与せず、
        /// Presenter が責務を担う。
        /// </summary>
        /// <param name="owner">UIDocument を保持する GameObject。null の場合は処理を中止する。</param>
        /// <param name="template">俳優のルート要素を記述した UXML。null の場合は警告を出して既存レイアウトを維持する。</param>
        /// <param name="panelSettings">使用する PanelSettings。null の場合はシーン内から既定を探索する。</param>
        public void EnsureActorUI(GameObject owner, VisualTreeAsset template, PanelSettings panelSettings = null)
        {
            if (owner == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenter: owner GameObject が null のため UI を生成できません。");
                return;
            }

            var document = owner.GetComponent<UIDocument>() ?? owner.AddComponent<UIDocument>();

            if (panelSettings != null)
            {
                document.panelSettings = panelSettings;
            }
            else if (document.panelSettings == null)
            {
                var fallback = Object.FindFirstObjectByType<PanelSettings>();
                if (fallback != null)
                {
                    document.panelSettings = fallback;
                }
                else
                {
                    Debug.LogWarning("[FUnity] ActorPresenter: PanelSettings が見つからないため UIDocument に適用できません。");
                }
            }

            if (template != null)
            {
                document.visualTreeAsset = template;
            }
            else
            {
                Debug.LogWarning("[FUnity] ActorPresenter: VisualTreeAsset が null のため UIDocument に適用しません。");
            }
        }

        /// <summary>
        /// Scratch の「〇歩動かす」に相当する距離を現在向きに沿って移動させる。
        /// </summary>
        /// <param name="steps">移動する歩数。負値の場合は逆方向へ移動する。</param>
        public void MoveSteps(float steps)
        {
            if (m_State == null)
            {
                return;
            }

            if (Mathf.Approximately(steps, 0f))
            {
                return;
            }

            var radians = m_State.DirectionDeg * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians));
            var delta = direction * (steps * StepToPixels);

            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            ApplyDelta(delta);
        }

        /// <summary>
        /// 絶対座標をピクセル単位で設定し、View に反映する。
        /// </summary>
        /// <param name="positionPx">適用する座標（px）。右=+X、下=+Y。</param>
        public void SetPositionPixels(Vector2 positionPx)
        {
            ApplyPosition(positionPx);
        }

        /// <summary>
        /// 相対的な座標差分をピクセル単位で加算する。
        /// </summary>
        /// <param name="deltaPx">加算する差分（px）。</param>
        public void AddPositionPixels(Vector2 deltaPx)
        {
            ApplyDelta(deltaPx);
        }

        /// <summary>
        /// 現在の向きを取得する。Presenter 未初期化時は既定値の 90 度を返す。
        /// </summary>
        /// <returns>現在の向き（度）。</returns>
        public float GetDirection()
        {
            if (m_State == null)
            {
                return DefaultDirectionDeg;
            }

            return m_State.DirectionDeg;
        }

        /// <summary>
        /// 現在の向きを設定する。View 側は方向を保持しないため、主に移動ベクトル計算に利用する。
        /// </summary>
        /// <param name="degrees">設定する角度（度）。</param>
        public void SetDirection(float degrees)
        {
            if (m_State == null)
            {
                return;
            }

            m_State.DirectionDeg = Normalize0To360(degrees);
        }

        /// <summary>
        /// View の StageBoundsChanged へ購読し、境界更新時に Model のクランプを再計算する。
        /// </summary>
        private void AttachViewEvents()
        {
            if (m_View == null || m_IsSubscribedToBounds)
            {
                return;
            }

            m_View.StageBoundsChanged += OnStageBoundsChanged;
            m_IsSubscribedToBounds = true;
        }

        /// <summary>
        /// View の StageBoundsChanged から購読解除し、Presenter が不要なイベントを受け取らないようにする。
        /// </summary>
        private void DetachViewEvents()
        {
            if (m_View != null && m_IsSubscribedToBounds)
            {
                m_View.StageBoundsChanged -= OnStageBoundsChanged;
            }

            m_IsSubscribedToBounds = false;
        }

        /// <summary>
        /// View から報告されたステージ境界を受け取り、クランプ矩形を再計算する。
        /// </summary>
        /// <param name="boundsPx">左上原点で表現されたステージ境界。</param>
        private void OnStageBoundsChanged(Rect boundsPx)
        {
            m_StageBoundsPx = NormalizeRect(boundsPx);
            UpdatePositionBoundsInternal();
            ClampStateToBounds();
        }

        /// <summary>
        /// View からステージ境界を再取得する。未取得の場合はクランプを無効化する。
        /// </summary>
        private void RefreshStageBoundsFromView()
        {
            if (m_View != null && m_View.TryGetStageBounds(out var bounds))
            {
                OnStageBoundsChanged(bounds);
            }
            else
            {
                m_HasPositionBounds = false;
            }
        }

        /// <summary>
        /// View 側の現在サイズを取得し、クランプ矩形の再計算に利用する。
        /// </summary>
        private void UpdateRenderedSizeFromView()
        {
            if (m_View != null && m_View.TryGetVisualSize(out var measured) && measured.sqrMagnitude > 0f)
            {
                m_LastMeasuredSizePx = measured;
            }
            else if (m_BaseSize.x > 0f || m_BaseSize.y > 0f)
            {
                m_LastMeasuredSizePx = m_BaseSize * m_CurrentScale;
            }
            else
            {
                m_LastMeasuredSizePx = Vector2.zero;
            }

            UpdatePositionBoundsInternal();
        }

        /// <summary>
        /// ステージ境界と俳優サイズからクランプ矩形を計算する。
        /// </summary>
        private void UpdatePositionBoundsInternal()
        {
            if (m_StageBoundsPx.width <= 0f && m_StageBoundsPx.height <= 0f)
            {
                m_HasPositionBounds = false;
                return;
            }

            var size = m_LastMeasuredSizePx;
            var width = Mathf.Max(0f, size.x);
            var height = Mathf.Max(0f, size.y);

            var minX = m_StageBoundsPx.xMin;
            var minY = m_StageBoundsPx.yMin;
            var maxX = Mathf.Max(minX, m_StageBoundsPx.xMax - width);
            var maxY = Mathf.Max(minY, m_StageBoundsPx.yMax - height);

            m_PositionBoundsPx = Rect.MinMaxRect(minX, minY, maxX, maxY);
            m_HasPositionBounds = true;
        }

        /// <summary>
        /// 現在の境界設定を用いて Model の位置をクランプし、View に同期する。
        /// </summary>
        private void ClampStateToBounds()
        {
            if (m_State == null || !m_HasPositionBounds)
            {
                return;
            }

            m_State.SetPositionClamped(m_State.Position, m_PositionBoundsPx);
            m_View?.SetPosition(m_State.Position);
        }

        /// <summary>
        /// 指定座標を Model へ適用し、必要に応じてクランプして View に反映する。
        /// </summary>
        /// <param name="positionPx">適用する座標。</param>
        private void ApplyPosition(Vector2 positionPx)
        {
            if (m_State == null)
            {
                return;
            }

            if (m_HasPositionBounds)
            {
                m_State.SetPositionClamped(positionPx, m_PositionBoundsPx);
            }
            else
            {
                m_State.SetPositionUnchecked(positionPx);
            }

            m_View?.SetPosition(m_State.Position);
        }

        /// <summary>
        /// 指定差分を Model へ加算し、必要に応じてクランプした結果を View に反映する。
        /// </summary>
        /// <param name="deltaPx">加算する差分。</param>
        private void ApplyDelta(Vector2 deltaPx)
        {
            if (m_State == null)
            {
                return;
            }

            if (m_HasPositionBounds)
            {
                m_State.AddPositionClamped(deltaPx, m_PositionBoundsPx);
            }
            else
            {
                m_State.SetPositionUnchecked(m_State.Position + deltaPx);
            }

            m_View?.SetPosition(m_State.Position);
        }

        /// <summary>
        /// Rect の最小最大を正規化し、計算時のマイナス幅を回避する。
        /// </summary>
        /// <param name="rect">正規化対象の矩形。</param>
        /// <returns>Min/Max が昇順となる矩形。</returns>
        private static Rect NormalizeRect(Rect rect)
        {
            var minX = Mathf.Min(rect.xMin, rect.xMax);
            var minY = Mathf.Min(rect.yMin, rect.yMax);
            var maxX = Mathf.Max(rect.xMin, rect.xMax);
            var maxY = Mathf.Max(rect.yMin, rect.yMax);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// 渡された角度を 0～360 度の範囲へ丸め込む。
        /// </summary>
        /// <param name="degrees">正規化対象の角度（度）。</param>
        /// <returns>0～360 度に収めた角度。</returns>
        private static float Normalize0To360(float degrees)
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
