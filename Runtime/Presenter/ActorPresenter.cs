// Updated: 2025-03-03
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using FUnity.Runtime.Authoring;
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

        /// <summary>使用中の座標原点。Scratch では Center、unityroom では TopLeft。</summary>
        private CoordinateOrigin m_CoordinateOrigin = CoordinateOrigin.TopLeft;

        /// <summary>アクティブなモードが Scratch 互換かどうか。</summary>
        private bool m_IsScratchMode;

        /// <summary>俳優座標が指し示すアンカー種別。既定は画像中心。</summary>
        private ActorAnchor m_Anchor = ActorAnchor.Center;

        /// <summary>Presenter が保持するステージ領域（UI 座標系）。TopLeft 原点時のみクランプ計算に使用する。</summary>
        private Rect m_StageBoundsUi = new Rect(0f, 0f, 0f, 0f);

        /// <summary>論理座標でのクランプ矩形。中央原点時は [-W/2, +W/2] を保持する。</summary>
        private Rect m_PositionBoundsLogical = new Rect(0f, 0f, 0f, 0f);

        /// <summary>直近で取得した俳優要素の描画サイズ（px）。</summary>
        private Vector2 m_LastMeasuredSizePx = Vector2.zero;

        /// <summary>境界計算が完了しているかどうか。</summary>
        private bool m_HasPositionBounds;

        /// <summary>View の境界イベントに購読済みかどうか。</summary>
        private bool m_IsSubscribedToBounds;

        /// <summary>ステージ座標変換に使用するルート要素。Scratch の中央原点変換で必要になる。</summary>
        private VisualElement m_StageCoordinateRoot;

        /// <summary>ステージジオメトリの購読状態。多重登録を避けるために利用する。</summary>
        private bool m_IsStageGeometrySubscribed;

        /// <summary>アンカー補正の再計算が必要な際に監視する俳優要素。</summary>
        private VisualElement m_AnchorGeometryElement;

        /// <summary>アンカー補正用 GeometryChangedEvent を登録済みかを示す。</summary>
        private bool m_IsAnchorGeometrySubscribed;

        /// <summary>モード設定から取得したステージサイズのフォールバック値。</summary>
        private Vector2 m_ModeStagePixelsFallback = Vector2.zero;

        /// <summary>View に伝える追加の縦方向オフセット（px）。浮遊アニメーションなどの視覚効果に利用する。</summary>
        private float m_VisualYOffset;

        /// <summary>ステージのピクセル境界（左上原点）。</summary>
        public Rect StageBoundsPx => m_StageBoundsUi;

        /// <summary>使用中の座標原点。外部から参照する際はこのプロパティを利用します。</summary>
        public CoordinateOrigin CoordinateOrigin => m_CoordinateOrigin;

        /// <summary>座標変換に利用するステージ要素。null の場合は中央原点変換を利用できません。</summary>
        public VisualElement StageRootElement => m_StageCoordinateRoot;

        /// <summary>
        /// モデルとビューを初期化し、初期位置・速度・ポートレートを反映する。
        /// </summary>
        /// <param name="data">静的設定。null の場合は既定値を使用。</param>
        /// <param name="state">既存の状態。null の場合は内部で新規生成する。</param>
        /// <param name="view">表示先の View。</param>
        /// <param name="modeConfig">アクティブなモード設定。原点やステージサイズを決定します。</param>
        /// <param name="stageRoot">ステージ座標の基準となる要素。Scratch での中央原点変換に利用します。</param>
        /// <example>
        /// <code>
        /// presenter.Initialize(actorData, new ActorState(), actorView, modeConfig, stageElement.ActorContainer);
        /// </code>
        /// </example>
        public void Initialize(FUnityActorData data, ActorState state, IActorView view, FUnityModeConfig modeConfig, VisualElement stageRoot)
        {
            var resolvedState = state ?? new ActorState();
            var shouldInitializeDirection = state == null;

            DetachViewEvents();
            DetachStageGeometryCallbacks();
            DetachAnchorGeometryCallback();

            m_State = resolvedState;
            m_View = view;
            m_CoordinateOrigin = CoordinateConverter.GetActiveOrigin(modeConfig);
            m_IsScratchMode = modeConfig != null && modeConfig.Mode == FUnityAuthoringMode.Scratch;
            m_ModeStagePixelsFallback = ResolveModeStagePixels(modeConfig);
            AttachStageGeometryCallback(stageRoot);

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

            m_Anchor = data != null ? data.Anchor : ActorAnchor.Center;
            m_BaseSize = data != null ? data.Size : Vector2.zero;
            m_LastMeasuredSizePx = Vector2.zero;
            m_VisualYOffset = 0f;

            var initialCenterUi = data != null ? data.InitialPosition : Vector2.zero;
            var anchorInitialUi = ConvertCenterToAnchor(initialCenterUi);
            var logicalInitial = ConvertUiToLogical(anchorInitialUi);
            m_State.SetPositionUnchecked(logicalInitial);
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

            m_StageBoundsUi = new Rect(0f, 0f, 0f, 0f);
            m_PositionBoundsLogical = new Rect(0f, 0f, 0f, 0f);
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
            UpdatePositionBoundsInternal();
            ClampStateToBounds();
            UpdateViewPosition();
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
                UpdateViewPosition();
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

            UpdateViewPosition();
        }

        /// <summary>
        /// 絶対座標を直接指定して俳優を移動させる。
        /// Visual Scripting の Custom Event "Actor/SetPosition" から利用することを想定。
        /// </summary>
        /// <param name="position">UI 座標（px）。俳優画像の中心座標。中央原点モードでは <see cref="ToUiPosition(Vector2)"/> で変換してから渡します。</param>
        public void SetPosition(Vector2 position)
        {
            ApplyPosition(position);
        }

        /// <summary>
        /// 現在の論理座標を取得する。Presenter 未初期化時は原点を返す。
        /// </summary>
        /// <returns>論理座標（px）。中央原点の場合は右+X/上+Y。</returns>
        public Vector2 GetPosition()
        {
            if (m_State == null)
            {
                return Vector2.zero;
            }

            return m_State.Position;
        }

        /// <summary>
        /// 論理座標を UI 座標へ変換し、中心座標を返すヘルパー。
        /// Visual Scripting アダプタ等から利用する。
        /// </summary>
        /// <param name="logical">論理座標。</param>
        /// <returns>アンカー適用後の中心座標。</returns>
        public Vector2 ToUiPosition(Vector2 logical)
        {
            var anchorUi = ConvertLogicalToUi(logical);
            return ConvertAnchorToCenter(anchorUi);
        }

        /// <summary>
        /// UI 座標（left/top）をアンカー逆補正して論理座標へ変換するヘルパー。入力デバイス座標の解釈に利用する。
        /// </summary>
        /// <param name="ui">中心座標の UI 値。</param>
        /// <returns>アンカー逆補正後の論理座標。</returns>
        public Vector2 ToLogicalPosition(Vector2 ui)
        {
            var anchorUi = ConvertCenterToAnchor(ui);
            return ConvertUiToLogical(anchorUi);
        }

        /// <summary>
        /// UI 座標系での差分を論理座標系の差分へ変換する。
        /// </summary>
        /// <param name="uiDelta">UI 座標系での差分。</param>
        /// <returns>論理座標系の差分。</returns>
        public Vector2 ToLogicalDelta(Vector2 uiDelta)
        {
            return ConvertDeltaToLogical(uiDelta);
        }

        /// <summary>
        /// 論理座標系の差分を UI 座標系の差分へ変換する。
        /// </summary>
        /// <param name="logicalDelta">論理座標系での差分。</param>
        /// <returns>UI 座標系での差分。</returns>
        public Vector2 ToUiDelta(Vector2 logicalDelta)
        {
            return ConvertLogicalDeltaToUi(logicalDelta);
        }

        /// <summary>
        /// 現在位置からピクセル単位の差分を加算し、即座に View へ反映する。
        /// Visual Scripting の「〇歩動かす」ブロックからの呼び出しを想定し、Presenter 経由で Model を更新する。
        /// </summary>
        /// <param name="deltaPx">加算する移動量（px）。中央原点モードでは内部で Y 方向の符号を調整します。</param>
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
        /// 指定した UI 座標を現在のクランプ矩形に合わせて補正する。境界未設定時は入力値を返す。
        /// </summary>
        /// <param name="positionPx">補正対象の中心座標（px）。</param>
        /// <param name="clampedPx">クランプ後の UI 座標。</param>
        /// <returns>クランプを実行した場合は <c>true</c>。</returns>
        public bool TryClampPosition(Vector2 positionPx, out Vector2 clampedPx)
        {
            if (!m_HasPositionBounds)
            {
                clampedPx = positionPx;
                return false;
            }

            var anchorUi = ConvertCenterToAnchor(positionPx);
            var logical = ConvertUiToLogical(anchorUi);

            var minX = m_PositionBoundsLogical.xMin;
            var minY = m_PositionBoundsLogical.yMin;
            var maxX = m_PositionBoundsLogical.xMax;
            var maxY = m_PositionBoundsLogical.yMax;

            var clampedLogical = new Vector2(
                Mathf.Clamp(logical.x, minX, maxX),
                Mathf.Clamp(logical.y, minY, maxY));

            var clampedAnchor = ConvertLogicalToUi(clampedLogical);
            clampedPx = ConvertAnchorToCenter(clampedAnchor);
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
        /// 拡大率（%）を絶対値で設定し、View 側で #root に対する等比スケールとして反映する。
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
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            if (m_CoordinateOrigin != CoordinateOrigin.Center)
            {
                direction.y = -direction.y;
            }
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
        /// <param name="positionPx">UI 座標（px）。俳優画像の中心座標。Scratch など中央原点の場合は <see cref="ToUiPosition(Vector2)"/> で変換してから渡してください。</param>
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
        /// 視覚効果として中心座標へ加算する縦方向のオフセットを設定する。Model の座標値は変更しない。
        /// </summary>
        /// <param name="offsetPx">加算するピクセル量。正で下方向、負で上方向。</param>
        public void SetVisualYOffset(float offsetPx)
        {
            m_VisualYOffset = offsetPx;

            if (m_View == null)
            {
                return;
            }

            UpdateViewPosition();
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
            DetachAnchorGeometryCallback();
        }

        /// <summary>
        /// ステージ要素の GeometryChangedEvent を購読し、サイズ変化時にクランプを更新する。
        /// </summary>
        /// <param name="stageRoot">購読対象の要素。</param>
        private void AttachStageGeometryCallback(VisualElement stageRoot)
        {
            if (stageRoot == null)
            {
                DetachStageGeometryCallbacks();
                return;
            }

            if (m_StageCoordinateRoot == stageRoot && m_IsStageGeometrySubscribed)
            {
                return;
            }

            DetachStageGeometryCallbacks();

            m_StageCoordinateRoot = stageRoot;
            m_StageCoordinateRoot.RegisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);
            m_IsStageGeometrySubscribed = true;
        }

        /// <summary>
        /// ステージ要素の GeometryChangedEvent 購読を解除する。
        /// </summary>
        private void DetachStageGeometryCallbacks()
        {
            if (m_StageCoordinateRoot != null && m_IsStageGeometrySubscribed)
            {
                m_StageCoordinateRoot.UnregisterCallback<GeometryChangedEvent>(OnStageGeometryChanged);
            }

            m_IsStageGeometrySubscribed = false;
            m_StageCoordinateRoot = null;
        }

        /// <summary>
        /// ステージ要素のサイズが更新された際に呼び出され、クランプと描画座標を再評価する。
        /// </summary>
        /// <param name="evt">UI Toolkit が通知するジオメトリイベント。</param>
        private void OnStageGeometryChanged(GeometryChangedEvent evt)
        {
            UpdatePositionBoundsInternal();
            ClampStateToBounds();
            UpdateViewPosition();
        }

        /// <summary>
        /// View から報告されたステージ境界を受け取り、クランプ矩形を再計算する。
        /// </summary>
        /// <param name="boundsPx">左上原点で表現されたステージ境界。</param>
        private void OnStageBoundsChanged(Rect boundsPx)
        {
            m_StageBoundsUi = NormalizeRect(boundsPx);
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
            else if (m_CoordinateOrigin != CoordinateOrigin.Center)
            {
                m_HasPositionBounds = false;
            }
            else
            {
                UpdatePositionBoundsInternal();
            }
        }

        /// <summary>
        /// View 側の現在サイズを取得し、クランプ矩形の再計算に利用する。
        /// </summary>
        private void UpdateRenderedSizeFromView()
        {
            var actorElement = ResolveActorVisualElement();
            ResolveCurrentVisualSizeForAnchor(actorElement);
            UpdatePositionBoundsInternal();
        }

        /// <summary>
        /// Scratch モード時に中心座標を許容範囲へクランプする。
        /// </summary>
        /// <param name="centerLogical">Scratch 論理座標系の中心座標。</param>
        /// <returns>必要に応じてクランプした中心座標。</returns>
        private Vector2 ClampToScratchIfNeeded(Vector2 centerLogical)
        {
            if (!m_IsScratchMode || m_CoordinateOrigin != CoordinateOrigin.Center)
            {
                return centerLogical;
            }

            var scaledSize = m_View != null ? m_View.GetScaledSizePx() : Vector2.zero;
            return ScratchBounds.ClampCenter(centerLogical, scaledSize);
        }

        /// <summary>
        /// ステージ境界と俳優サイズからクランプ矩形を計算する。
        /// </summary>
        private void UpdatePositionBoundsInternal()
        {
            if (m_CoordinateOrigin == CoordinateOrigin.Center)
            {
                var stageSize = ResolveStageSize();
                if (stageSize.x <= 0f || stageSize.y <= 0f)
                {
                    m_HasPositionBounds = false;
                    return;
                }

                var halfWidth = stageSize.x * 0.5f;
                var halfHeight = stageSize.y * 0.5f;
                m_PositionBoundsLogical = Rect.MinMaxRect(-halfWidth, -halfHeight, halfWidth, halfHeight);
                m_HasPositionBounds = true;
                return;
            }

            if (m_StageBoundsUi.width <= 0f && m_StageBoundsUi.height <= 0f)
            {
                m_HasPositionBounds = false;
                return;
            }

            var size = m_LastMeasuredSizePx;
            var width = Mathf.Max(0f, size.x);
            var height = Mathf.Max(0f, size.y);
            var anchorOffset = CalculateAnchorOffset(size);

            var minX = m_StageBoundsUi.xMin + anchorOffset.x;
            var minY = m_StageBoundsUi.yMin + anchorOffset.y;

            var tailWidth = Mathf.Max(0f, width - anchorOffset.x);
            var tailHeight = Mathf.Max(0f, height - anchorOffset.y);

            var maxX = Mathf.Max(minX, m_StageBoundsUi.xMax - tailWidth);
            var maxY = Mathf.Max(minY, m_StageBoundsUi.yMax - tailHeight);

            m_PositionBoundsLogical = Rect.MinMaxRect(minX, minY, maxX, maxY);
            m_HasPositionBounds = true;
        }

        /// <summary>
        /// 現在の境界設定を用いて Model の位置をクランプし、View に同期する。
        /// </summary>
        private void ClampStateToBounds()
        {
            if (m_State == null)
            {
                return;
            }

            if (m_IsScratchMode && m_CoordinateOrigin == CoordinateOrigin.Center)
            {
                var clamped = ClampToScratchIfNeeded(m_State.Position);
                m_State.SetPositionUnchecked(clamped);
                UpdateViewPosition();
                return;
            }

            if (!m_HasPositionBounds)
            {
                return;
            }

            m_State.SetPositionClamped(m_State.Position, m_PositionBoundsLogical);
            UpdateViewPosition();
        }

        /// <summary>
        /// Model に保持された論理座標を UI 座標へ変換し、View に反映する。
        /// </summary>
        private void UpdateViewPosition()
        {
            if (m_View == null || m_State == null)
            {
                return;
            }

            var actorElement = ResolveActorVisualElement();
            var anchorUi = ConvertLogicalToUi(m_State.Position);
            var visualSize = ResolveCurrentVisualSizeForAnchor(actorElement);
            var centerUi = ConvertAnchorToCenter(anchorUi, visualSize);
            if (Mathf.Abs(m_VisualYOffset) > Mathf.Epsilon)
            {
                centerUi.y += m_VisualYOffset;
            }
            var requiresGeometryWatch = NeedsGeometryResolution(actorElement);

            EnsureAnchorGeometrySubscription(actorElement, requiresGeometryWatch);

            m_View.SetCenterPosition(centerUi);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ValidateCenterAlignment(centerUi);
#endif
        }

        /// <summary>
        /// 現在保持しているスケール・回転・座標を View に再適用し、外部要因で崩れたスタイルを復元する。
        /// </summary>
        public void ApplyAllTransforms()
        {
            if (m_View == null)
            {
                return;
            }

            if (m_State != null)
            {
                var percent = Mathf.Clamp(m_State.SizePercent, MinSizePercent, MaxSizePercent);
                m_CurrentScale = percent / 100f;
                m_View.SetSizePercent(percent);
                m_View.SetRotationDegrees(m_State.RotationDeg);
                UpdateRenderedSizeFromView();
                ClampStateToBounds();
            }
            else
            {
                m_View.SetSizePercent(m_CurrentScale * 100f);
                m_View.SetRotationDegrees(0f);
            }

            UpdateViewPosition();
        }

        /// <summary>
        /// アンカー基準の UI 座標を中心座標へ変換する。
        /// </summary>
        /// <param name="anchorUi">アンカー基準の UI 座標。</param>
        /// <returns>俳優画像の中心座標。</returns>
        private Vector2 ConvertAnchorToCenter(Vector2 anchorUi)
        {
            var actorElement = ResolveActorVisualElement();
            var size = ResolveCurrentVisualSizeForAnchor(actorElement);
            return ConvertAnchorToCenter(anchorUi, size);
        }

        /// <summary>
        /// 指定サイズを用いてアンカー基準の UI 座標を中心座標へ変換する。
        /// </summary>
        /// <param name="anchorUi">アンカー基準の UI 座標。</param>
        /// <param name="visualSize">俳優要素の描画サイズ。</param>
        /// <returns>俳優画像の中心座標。</returns>
        private Vector2 ConvertAnchorToCenter(Vector2 anchorUi, Vector2 visualSize)
        {
            if (m_Anchor != ActorAnchor.TopLeft)
            {
                return anchorUi;
            }

            var offset = CalculateAnchorToCenterOffset(visualSize);
            if (offset.sqrMagnitude <= 0f)
            {
                return anchorUi;
            }

            return anchorUi + offset;
        }

        /// <summary>
        /// 中心座標をアンカー基準の UI 座標へ変換する。
        /// </summary>
        /// <param name="centerUi">俳優画像の中心座標。</param>
        /// <returns>アンカー基準の UI 座標。</returns>
        private Vector2 ConvertCenterToAnchor(Vector2 centerUi)
        {
            if (m_Anchor != ActorAnchor.TopLeft)
            {
                return centerUi;
            }

            var offset = CalculateAnchorToCenterOffset();
            if (offset.sqrMagnitude <= 0f)
            {
                return centerUi;
            }

            return centerUi - offset;
        }

        /// <summary>
        /// 現在の俳優サイズに基づくアンカーオフセットを算出する。
        /// </summary>
        /// <returns>左上原点からアンカー位置までのオフセット。</returns>
        private Vector2 CalculateAnchorOffset()
        {
            var actorElement = ResolveActorVisualElement();
            var size = ResolveCurrentVisualSizeForAnchor(actorElement);
            return CalculateAnchorOffset(size);
        }

        /// <summary>
        /// 指定されたサイズを元にアンカーオフセットを計算する。
        /// </summary>
        /// <param name="size">俳優要素の幅・高さ（px）。</param>
        /// <returns>左上原点からアンカー位置までのオフセット。</returns>
        private Vector2 CalculateAnchorOffset(Vector2 size)
        {
            if (m_Anchor != ActorAnchor.Center)
            {
                return Vector2.zero;
            }

            var width = Mathf.Max(0f, size.x);
            var height = Mathf.Max(0f, size.y);

            if (width <= 0f && height <= 0f)
            {
                return Vector2.zero;
            }

            var offsetX = width > 0f ? width * 0.5f : 0f;
            var offsetY = height > 0f ? height * 0.5f : 0f;
            return new Vector2(offsetX, offsetY);
        }

        /// <summary>
        /// アンカーが左上の場合に中心までのオフセットを計算する。
        /// </summary>
        /// <returns>アンカー（左上）から中心までのベクトル。</returns>
        private Vector2 CalculateAnchorToCenterOffset()
        {
            var actorElement = ResolveActorVisualElement();
            var size = ResolveCurrentVisualSizeForAnchor(actorElement);
            return CalculateAnchorToCenterOffset(size);
        }

        /// <summary>
        /// 指定したサイズからアンカー→中心のオフセットを計算する。
        /// </summary>
        /// <param name="size">俳優要素の幅・高さ。</param>
        /// <returns>アンカー（左上）から中心までのベクトル。</returns>
        private Vector2 CalculateAnchorToCenterOffset(Vector2 size)
        {
            if (m_Anchor != ActorAnchor.TopLeft)
            {
                return Vector2.zero;
            }

            var width = Mathf.Max(0f, size.x);
            var height = Mathf.Max(0f, size.y);

            if (width <= 0f && height <= 0f)
            {
                return Vector2.zero;
            }

            var offsetX = width > 0f ? width * 0.5f : 0f;
            var offsetY = height > 0f ? height * 0.5f : 0f;
            return new Vector2(offsetX, offsetY);
        }

        /// <summary>
        /// アンカー計算に利用できる最新の俳優サイズを取得する。
        /// </summary>
        /// <returns>幅・高さ（px）。未取得時は 0。</returns>
        private Vector2 ResolveCurrentVisualSizeForAnchor(VisualElement actorElement = null)
        {
            if (actorElement != null)
            {
                var extracted = ExtractUnscaledSize(actorElement);
                if (extracted.sqrMagnitude > 0f)
                {
                    m_LastMeasuredSizePx = extracted;
                    return extracted;
                }
            }

            if (m_View != null && m_View.TryGetVisualSize(out var measured) && measured.sqrMagnitude > 0f)
            {
                m_LastMeasuredSizePx = measured;
                return measured;
            }

            if (m_LastMeasuredSizePx.sqrMagnitude > 0f)
            {
                return m_LastMeasuredSizePx;
            }

            if (m_BaseSize.x > 0f || m_BaseSize.y > 0f)
            {
                var scaled = m_BaseSize * m_CurrentScale;
                if (scaled.sqrMagnitude > 0f)
                {
                    m_LastMeasuredSizePx = scaled;
                    return scaled;
                }
            }

            m_LastMeasuredSizePx = Vector2.zero;
            return Vector2.zero;
        }

        /// <summary>
        /// VisualElement から UIScale 非依存の描画サイズを抽出する。
        /// </summary>
        /// <param name="element">測定対象の UI 要素。</param>
        /// <returns>幅・高さ（px）。未確定時は 0。</returns>
        private static Vector2 ExtractUnscaledSize(VisualElement element)
        {
            if (element == null)
            {
                return Vector2.zero;
            }

            var rect = element.contentRect;
            var width = rect.width;
            var height = rect.height;

            if (float.IsNaN(width) || width <= 0f)
            {
                width = element.resolvedStyle.width;
            }

            if (float.IsNaN(height) || height <= 0f)
            {
                height = element.resolvedStyle.height;
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
        /// 俳優 View が保持する VisualElement を解決する。ActorView 専用処理に限定し、NullActorView 時は null を返す。
        /// </summary>
        /// <returns>俳優要素。取得できない場合は null。</returns>
        private VisualElement ResolveActorVisualElement()
        {
            if (m_View is ActorView actorView)
            {
                return actorView.ActorRoot ?? actorView.BoundElement;
            }

            return null;
        }

        /// <summary>
        /// ジオメトリ未確定かどうかを判定し、GeometryChangedEvent 監視が必要かを返す。
        /// </summary>
        /// <param name="element">判定対象。</param>
        /// <returns>幅または高さが未確定の場合は <c>true</c>。</returns>
        private static bool NeedsGeometryResolution(VisualElement element)
        {
            if (element == null)
            {
                return false;
            }

            var size = ExtractUnscaledSize(element);
            return size.x <= 0f || size.y <= 0f;
        }

        /// <summary>
        /// アンカー補正のために GeometryChangedEvent を必要に応じて登録・解除する。
        /// </summary>
        /// <param name="element">監視対象。</param>
        /// <param name="shouldSubscribe">登録が必要かどうか。</param>
        private void EnsureAnchorGeometrySubscription(VisualElement element, bool shouldSubscribe)
        {
            if (!shouldSubscribe || element == null)
            {
                DetachAnchorGeometryCallback();
                return;
            }

            if (m_IsAnchorGeometrySubscribed && m_AnchorGeometryElement == element)
            {
                return;
            }

            DetachAnchorGeometryCallback();
            element.RegisterCallback<GeometryChangedEvent>(OnActorGeometryChanged);
            m_AnchorGeometryElement = element;
            m_IsAnchorGeometrySubscribed = true;
        }

        /// <summary>
        /// GeometryChangedEvent の購読を解除し、不要な参照を破棄する。
        /// </summary>
        private void DetachAnchorGeometryCallback()
        {
            if (m_AnchorGeometryElement != null && m_IsAnchorGeometrySubscribed)
            {
                m_AnchorGeometryElement.UnregisterCallback<GeometryChangedEvent>(OnActorGeometryChanged);
            }

            m_IsAnchorGeometrySubscribed = false;
            m_AnchorGeometryElement = null;
        }

        /// <summary>
        /// 俳優要素のジオメトリ確定時に呼び出され、サイズキャッシュと座標を再評価する。
        /// </summary>
        /// <param name="evt">UI Toolkit から渡されるジオメトリイベント。</param>
        private void OnActorGeometryChanged(GeometryChangedEvent evt)
        {
            DetachAnchorGeometryCallback();
            UpdateRenderedSizeFromView();
            UpdateViewPosition();
        }

        /// <summary>
        /// 中央原点×アンカー中心の整合性をデバッグモードで確認し、ズレが大きい場合は警告する。
        /// </summary>
        /// <param name="computedCenter">現在計算された中心座標。</param>
        private void ValidateCenterAlignment(Vector2 computedCenter)
        {
            if (!Debug.isDebugBuild && !Application.isEditor)
            {
                return;
            }

            if (m_CoordinateOrigin != CoordinateOrigin.Center || m_Anchor != ActorAnchor.Center)
            {
                return;
            }

            if (m_State == null)
            {
                return;
            }

            if (Mathf.Abs(m_State.Position.x) > 0.001f || Mathf.Abs(m_State.Position.y) > 0.001f)
            {
                return;
            }

            var stageSize = ResolveStageSize();
            if (stageSize.x <= 0f || stageSize.y <= 0f)
            {
                return;
            }

            var expectedCenter = new Vector2(stageSize.x * 0.5f, stageSize.y * 0.5f);

            if (Mathf.Abs(computedCenter.x - expectedCenter.x) >= 0.5f || Mathf.Abs(computedCenter.y - expectedCenter.y) >= 0.5f)
            {
                Debug.LogWarning(
                    $"[FUnity] ActorPresenter: Scratch 中央原点での中心座標が想定外です。centerX={computedCenter.x:F2} (expected {expectedCenter.x:F2}), centerY={computedCenter.y:F2} (expected {expectedCenter.y:F2}).");
            }
        }

        /// <summary>
        /// 指定座標を Model へ適用し、必要に応じてクランプして View に反映する。
        /// </summary>
        /// <param name="centerPx">適用する中心座標。</param>
        private void ApplyPosition(Vector2 centerPx)
        {
            if (m_State == null)
            {
                return;
            }

            var anchorUi = ConvertCenterToAnchor(centerPx);
            var logical = ConvertUiToLogical(anchorUi);

            if (m_IsScratchMode && m_CoordinateOrigin == CoordinateOrigin.Center)
            {
                var clampedCenter = ClampToScratchIfNeeded(logical);
                m_State.SetPositionUnchecked(clampedCenter);
            }
            else if (m_HasPositionBounds)
            {
                m_State.SetPositionClamped(logical, m_PositionBoundsLogical);
            }
            else
            {
                m_State.SetPositionUnchecked(logical);
            }

            UpdateViewPosition();
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

            var logicalDelta = ConvertDeltaToLogical(deltaPx);
            var targetLogical = m_State.Position + logicalDelta;

            if (m_IsScratchMode && m_CoordinateOrigin == CoordinateOrigin.Center)
            {
                var clampedCenter = ClampToScratchIfNeeded(targetLogical);
                m_State.SetPositionUnchecked(clampedCenter);
            }
            else if (m_HasPositionBounds)
            {
                m_State.AddPositionClamped(logicalDelta, m_PositionBoundsLogical);
            }
            else
            {
                m_State.SetPositionUnchecked(targetLogical);
            }

            UpdateViewPosition();
        }

        /// <summary>
        /// 論理座標を UI 座標へ変換する。中央原点時はステージサイズを利用して中心を原点に写像する。
        /// </summary>
        /// <param name="logical">論理座標。</param>
        /// <returns>アンカー基準の UI 座標。</returns>
        private Vector2 ConvertLogicalToUi(Vector2 logical)
        {
            return CoordinateConverter.LogicalToUI(logical, m_CoordinateOrigin);
        }

        /// <summary>
        /// UI 座標を論理座標へ変換する。中央原点時は中心を原点とする座標へ写像する。
        /// </summary>
        /// <param name="ui">アンカー基準の UI 座標。</param>
        /// <returns>論理座標。</returns>
        private Vector2 ConvertUiToLogical(Vector2 ui)
        {
            return CoordinateConverter.UIToLogical(ui, m_CoordinateOrigin);
        }

        /// <summary>
        /// UI ベースの差分ベクトルを論理座標の差分へ変換する。中央原点時は Y 軸の正負を反転させる。
        /// </summary>
        /// <param name="delta">UI 座標系での差分。</param>
        /// <returns>論理座標系での差分。</returns>
        private Vector2 ConvertDeltaToLogical(Vector2 delta)
        {
            if (m_CoordinateOrigin != CoordinateOrigin.Center)
            {
                return delta;
            }

            return new Vector2(delta.x, -delta.y);
        }

        /// <summary>
        /// 論理座標系の差分を UI 座標系へ変換する。
        /// </summary>
        /// <param name="delta">論理座標系の差分。</param>
        /// <returns>UI 座標系での差分。</returns>
        private Vector2 ConvertLogicalDeltaToUi(Vector2 delta)
        {
            if (m_CoordinateOrigin != CoordinateOrigin.Center)
            {
                return delta;
            }

            return new Vector2(delta.x, -delta.y);
        }

        /// <summary>
        /// ステージ要素から現在のサイズを取得する。未確定の場合はモード設定のフォールバックを返す。
        /// </summary>
        /// <returns>ステージ幅・高さ（px）。取得できない場合は <see cref="Vector2.zero"/>。</returns>
        private Vector2 ResolveStageSize()
        {
            if (m_StageCoordinateRoot != null)
            {
                var rs = m_StageCoordinateRoot.resolvedStyle;
                if (rs.width > 0f && rs.height > 0f)
                {
                    return new Vector2(rs.width, rs.height);
                }
            }

            if (m_ModeStagePixelsFallback.x > 0f && m_ModeStagePixelsFallback.y > 0f)
            {
                return m_ModeStagePixelsFallback;
            }

            return Vector2.zero;
        }

        /// <summary>
        /// モード設定からステージサイズのフォールバック値を抽出する。
        /// </summary>
        /// <param name="config">参照するモード設定。</param>
        /// <returns>幅・高さ（px）。取得できない場合は <see cref="Vector2.zero"/>。</returns>
        private static Vector2 ResolveModeStagePixels(FUnityModeConfig config)
        {
            if (config == null)
            {
                return Vector2.zero;
            }

            var stagePixels = config.StagePixels;
            if (stagePixels.x > 0 && stagePixels.y > 0)
            {
                return new Vector2(stagePixels.x, stagePixels.y);
            }

            if (config.UseScratchFixedStage)
            {
                var width = Mathf.Max(0, config.ScratchStageWidth);
                var height = Mathf.Max(0, config.ScratchStageHeight);
                if (width > 0 && height > 0)
                {
                    return new Vector2(width, height);
                }
            }

            return Vector2.zero;
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
