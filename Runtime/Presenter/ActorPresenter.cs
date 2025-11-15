// Updated: 2025-03-03
using System.Threading;
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
    /// Visual Scripting から俳優を識別する際に使用する Presenter の共通インターフェースです。
    /// 変数サービスなどが俳優キーや静的設定へアクセスするために参照します。
    /// </summary>
    public interface IActorPresenter
    {
        /// <summary>Visual Scripting から一意に識別される俳優キー。</summary>
        string ActorKey { get; }

        /// <summary>紐付いている俳優設定。null の場合はフォールバック構成を意味します。</summary>
        FUnityActorData ActorData { get; }

        /// <summary>現在使用している SpriteList のインデックス。Sprite 未使用時は 0。</summary>
        int SpriteIndex { get; }

        /// <summary>ActorData に登録された Sprite 枚数。未設定時は 0。</summary>
        int SpriteCount { get; }

        /// <summary>SpriteList の表示インデックスを設定し、可能なら見た目を切り替える。</summary>
        /// <param name="index">使用したい Sprite のインデックス。</param>
        void SetSpriteIndex(int index);
    }

    /// <summary>
    /// 俳優の状態（Model）と UI 表示（View）を調停する Presenter。
    /// 入力から得た移動ベクトルを <see cref="ActorState"/> に反映し、View に一方向で通知する。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnityActorData"/>, <see cref="ActorState"/>, <see cref="IActorView"/>
    /// 想定ライフサイクル: <see cref="FUnity.Runtime.Core.FUnityManager"/> が俳優生成時に <see cref="Initialize"/> を呼び出し、
    ///     以降は <see cref="Tick"/> をフレーム毎に実行する。Presenter 自体はステートレスであり、Model/View 間の同期のみを担当。
    /// </remarks>
    public sealed class ActorPresenter : IActorPresenter
    {
        /// <summary>VS から俳優を一意に識別するための連番です。</summary>
        private static int s_ActorKeySequence;

        /// <summary>Scratch 互換の「1 歩」をピクセル換算する係数（px/歩）。</summary>
        private const float StepToPixels = 10f;

        /// <summary>俳優の初期向き（度）。Scratch 互換で 90°=上。</summary>
        private const float DefaultDirectionDeg = 90f;

        /// <summary>拡大率として許容する最小値（%）。これ未満の値は 1% に丸め込む。</summary>
        private const float MinSizePercent = 1f;

        /// <summary>拡大率として許容する最大値（%）。これより大きい値は 300% に丸め込む。</summary>
        private const float MaxSizePercent = 300f;

        /// <summary>Scratch の「色の効果」の一周を表す値。200 で 360 度に相当します。</summary>
        private const float ColorEffectCycle = 200f;

        /// <summary>ランタイム状態を保持する Model。</summary>
        private ActorState m_State;

        /// <summary>UI Toolkit 側の描画を担当する View。</summary>
        private IActorView m_View;

        /// <summary>SpriteList 切り替え時に利用する現在のインデックス。</summary>
        private int m_SpriteIndex;

        /// <summary>SpriteList のインデックス指定を優先するかどうか。</summary>
        private bool m_UseSpriteIndexOverride;

        /// <summary>この俳優専用にバインドされた Visual Scripting の ScriptMachine。</summary>
        private ScriptMachine m_ScriptMachine;

        /// <summary>Visual Scripting Runner（GameObject）への参照。</summary>
        private GameObject m_Runner;

        /// <summary>初期サイズ（幅・高さ）。0 の場合は View 側の既定値を利用する。</summary>
        private Vector2 m_BaseSize = Vector2.zero;

        /// <summary>現在のスケール。等倍は 1。</summary>
        private float m_CurrentScale = 1f;

        /// <summary>左右反転スタイル用に保持する最後の X 方向符号。+1 で右向き。</summary>
        private float m_LastFacingSignX = 1f;

        /// <summary>現在保持している拡大率（%）。Presenter ごとに独立して管理する。</summary>
        private float m_SizePercent = 100f;

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

        /// <summary>論理座標で扱う浮遊オフセット（px）。中心座標に加算して View に伝える。</summary>
        private Vector2 m_FloatOffset = Vector2.zero;

        /// <summary>モード設定で浮遊オフセットの適用を許可しているか。</summary>
        private bool m_EnableFloatOffset = true;

        /// <summary>現在表示している吹き出しの本文。null の場合は吹き出し非表示を意味する。</summary>
        private string m_SpeechText;

        /// <summary>吹き出しが残り何秒表示されるか。タイマー無効時は 0 以下を保持する。</summary>
        private float m_SpeechRemainSeconds;

        /// <summary>吹き出しが思考（考える）表現かどうか。true で思考、false で発言。</summary>
        private bool m_SpeechIsThought;

        /// <summary>吹き出しの表示時間をカウントダウンするかどうか。</summary>
        private bool m_SpeechUsesTimer;

        /// <summary>VS から参照されるユニークな俳優キー。</summary>
        private readonly string m_ActorKey;

        /// <summary>現在の表示名。DisplayName が空の場合は空文字を保持します。</summary>
        private string m_DisplayName = string.Empty;

        /// <summary>現在参照している俳優設定。null の場合はフォールバック構成を意味します。</summary>
        private FUnityActorData m_ActorData;

        /// <summary>最新の可視状態。View へ伝播した値をキャッシュします。</summary>
        private bool m_IsVisible = true;

        /// <summary>ステージのピクセル境界（左上原点）。</summary>
        public Rect StageBoundsPx => m_StageBoundsUi;

        /// <summary>使用中の座標原点。外部から参照する際はこのプロパティを利用します。</summary>
        public CoordinateOrigin CoordinateOrigin => m_CoordinateOrigin;

        /// <summary>現在使用している SpriteList のインデックス。Sprite 未使用時は 0 を返す。</summary>
        public int SpriteIndex => m_SpriteIndex;

        /// <summary>ActorData に登録されている Sprite 枚数。SpriteList 未設定時は 0。</summary>
        public int SpriteCount => m_ActorData?.Sprites?.Count ?? 0;

        /// <summary>現在の拡大率（%）。View へ適用されている値を返す。</summary>
        public float SizePercent => m_SizePercent;

        /// <summary>座標変換に利用するステージ要素。null の場合は中央原点変換を利用できません。</summary>
        public VisualElement StageRootElement => m_StageCoordinateRoot;

        /// <summary>現在バインドされている View を ActorView 型として取得する。ActorView 以外の場合は null。</summary>
        internal ActorView ActorViewComponent => m_View as ActorView;

        /// <summary>この Presenter がクローンかどうかを示します。</summary>
        public bool IsClone { get; internal set; }

        /// <summary>複製元の Presenter。クローンでない場合は null。</summary>
        public ActorPresenter Original { get; internal set; }

        /// <summary>紐付いている Visual Scripting Runner の GameObject。</summary>
        public GameObject Runner
        {
            get => m_Runner;
            internal set => m_Runner = value;
        }

        /// <summary>
        /// VS グラフから俳優を一意に参照するためのキーを返します。
        /// 生成時に採番され、ライフタイムを通じて不変です。
        /// </summary>
        public string ActorKey => m_ActorKey;

        /// <summary>
        /// 現在の DisplayName を返します。未設定の場合は空文字列です。
        /// クローンも元の DisplayName を共有します。
        /// </summary>
        public string DisplayName => m_DisplayName;

        /// <summary>紐付いている俳優設定。null の場合はフォールバック構成を意味します。</summary>
        public FUnityActorData ActorData => m_ActorData;

        /// <summary>
        /// 現在 View に適用されている可視状態を返します。
        /// Presenter から SetVisible を呼ぶたびに更新されます。
        /// </summary>
        public bool IsVisible => m_IsVisible;

        /// <summary>
        /// インスタンス生成時に俳優キーを採番し、VS ブリッジへ登録します。
        /// </summary>
        public ActorPresenter()
        {
            var sequence = Interlocked.Increment(ref s_ActorKeySequence);
            m_ActorKey = $"actor-{sequence:D6}";
            VSPresenterBridge.RegisterActor(this);
        }

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

            m_View?.HideSpeech();

            m_State = resolvedState;
            m_View = view;
            m_CoordinateOrigin = CoordinateConverter.GetActiveOrigin(modeConfig);
            m_IsScratchMode = modeConfig != null && modeConfig.Mode == FUnityAuthoringMode.Scratch;
            m_ModeStagePixelsFallback = ResolveModeStagePixels(modeConfig);
            m_EnableFloatOffset = ResolveFloatOffsetEnabled(modeConfig);
            AttachStageGeometryCallback(stageRoot);

            m_ActorData = data;
            m_DisplayName = data != null ? data.DisplayName ?? string.Empty : string.Empty;
            if (m_View is ActorView actorViewInstance)
            {
                m_IsVisible = actorViewInstance.IsVisible;
            }
            else
            {
                m_IsVisible = true;
            }

            if (m_State != null)
            {
                if (m_State.SizePercent <= 0f)
                {
                    m_State.SizePercent = 100f;
                }

                m_State.SizePercent = Mathf.Clamp(m_State.SizePercent, MinSizePercent, MaxSizePercent);
                m_SizePercent = m_State.SizePercent;
                m_CurrentScale = m_SizePercent / 100f;
            }
            else
            {
                m_SizePercent = 100f;
                m_CurrentScale = 1f;
            }

            m_Anchor = data != null ? data.Anchor : ActorAnchor.Center;
            m_BaseSize = data != null ? data.Size : Vector2.zero;
            m_LastMeasuredSizePx = Vector2.zero;
            m_FloatOffset = Vector2.zero;

            var initialCenterUi = data != null ? data.InitialPosition : Vector2.zero;
            var anchorInitialUi = ConvertCenterToAnchor(initialCenterUi);
            var logicalInitial = ConvertUiToLogical(anchorInitialUi);
            m_State.SetPositionUnchecked(logicalInitial);
            m_State.Speed = Mathf.Max(0f, data != null ? GetConfiguredSpeed(data) : 300f);
            m_State.SpriteIndex = 0;
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

            m_SpriteIndex = 0;
            m_UseSpriteIndexOverride = false;

            if (m_View != null)
            {
                ApplyCurrentSpriteToView();

                if (m_BaseSize.x > 0f || m_BaseSize.y > 0f)
                {
                    m_View.SetSize(m_BaseSize);
                }

                ApplyRotationStyleToView();

                AttachViewEvents();
                RefreshStageBoundsFromView();
            }

            SetScale(m_CurrentScale);
            UpdatePositionBoundsInternal();
            ClampStateToBounds();
            UpdateViewPosition();
            ApplyGraphicEffectsFromState();
            HideSpeech();
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
            m_Runner = machine.gameObject;

            var objectVariables = Unity.VisualScripting.Variables.Object(machine);
            if (objectVariables == null)
            {
                objectVariables = Unity.VisualScripting.Variables.Object(machine.gameObject);
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

                objectVariables.Set("actorKey", ActorKey);
                objectVariables.Set("selfActorKey", ActorKey);
            }
            else
            {
                Debug.LogWarning("[FUnity] ActorPresenter: ScriptMachine の Object Variables にアクセスできないため Self を登録できません。Variables.Object(flow.stack) から参照できるよう Runner の構成を確認してください。");
            }
        }

        /// <summary>
        /// 他の Presenter から状態をコピーし、位置・サイズ・吹き出しなどを同期します。
        /// </summary>
        /// <param name="source">コピー元となる Presenter。</param>
        public void CopyRuntimeStateFrom(ActorPresenter source)
        {
            if (source == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenter: CopyRuntimeStateFrom に null が渡されたためクローン状態を複製できません。");
                return;
            }

            m_DisplayName = source.m_DisplayName;

            if (m_State != null && source.m_State != null)
            {
                m_State.Speed = source.m_State.Speed;
                m_State.DirectionDeg = source.m_State.DirectionDeg;
                m_State.RotationDeg = source.m_State.RotationDeg;
                m_State.SizePercent = source.m_State.SizePercent;
                m_State.RotationStyle = source.m_State.RotationStyle;
                m_State.Effects = source.m_State.Effects;
            }

            m_LastFacingSignX = source.m_LastFacingSignX;

            m_EnableFloatOffset = source.m_EnableFloatOffset;
            m_FloatOffset = source.m_FloatOffset;
            m_ModeStagePixelsFallback = source.m_ModeStagePixelsFallback;
            m_CoordinateOrigin = source.m_CoordinateOrigin;
            m_IsScratchMode = source.m_IsScratchMode;
            m_Anchor = source.m_Anchor;
            m_BaseSize = source.m_BaseSize;

            if (source.m_State != null)
            {
                SetRotationStyle(source.m_State.RotationStyle);
            }
            else
            {
                ApplyRotationStyleToView();
            }

            SetSizePercent(source.SizePercent);
            SetDirection(source.GetDirection());
            SetRotation(source.GetRotationDegrees());
            SetFloatOffset(source.m_FloatOffset);

            var sourceLogical = source.GetPosition();
            var sourceUi = source.ToUiPosition(sourceLogical);
            SetPositionPixels(sourceUi);
            ApplyGraphicEffectsFromState();

            var sourceElement = source.ResolveActorVisualElement();
            if (sourceElement != null)
            {
                var isVisible = sourceElement.resolvedStyle.display != DisplayStyle.None;
                SetVisible(isVisible);
            }

            if (!string.IsNullOrEmpty(source.m_SpeechText))
            {
                ShowSpeech(source.m_SpeechText, source.m_SpeechUsesTimer ? source.m_SpeechRemainSeconds : 0f, source.m_SpeechIsThought);
            }
            else
            {
                HideSpeech();
            }
        }

        /// <summary>
        /// 現在の回転角度（度）を取得します。
        /// </summary>
        /// <returns>0～360 度の範囲に正規化された角度。</returns>
        public float GetRotationDegrees()
        {
            if (m_State == null)
            {
                return 0f;
            }

            return m_State.RotationDeg;
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

            UpdateSpeechTimer(deltaTime);

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
        /// 吹き出しを表示し、必要に応じて寿命タイマーを開始する。
        /// </summary>
        /// <param name="text">表示する本文。null の場合は空文字として扱う。</param>
        /// <param name="seconds">表示継続時間（秒）。0 以下で無期限表示。</param>
        /// <param name="isThought">思考吹き出しとして表示する場合は true。</param>
        public void ShowSpeech(string text, float seconds, bool isThought)
        {
            m_SpeechText = text ?? string.Empty;
            m_SpeechIsThought = isThought;
            var safeSeconds = Mathf.Max(0f, seconds);
            m_SpeechUsesTimer = safeSeconds > 0f;
            m_SpeechRemainSeconds = m_SpeechUsesTimer ? safeSeconds : 0f;

            m_View?.ShowSpeech(m_SpeechText, m_SpeechIsThought);
        }

        /// <summary>
        /// 表示中の吹き出しを即座に閉じ、タイマーも停止する。
        /// </summary>
        public void HideSpeech()
        {
            m_SpeechText = null;
            m_SpeechRemainSeconds = 0f;
            m_SpeechUsesTimer = false;
            m_SpeechIsThought = false;

            m_View?.HideSpeech();
        }

        /// <summary>
        /// 俳優の表示/非表示を切り替え、View へ style.display の更新を委譲する。
        /// </summary>
        /// <param name="visible">true で表示、false で非表示。</param>
        public void SetVisible(bool visible)
        {
            m_IsVisible = visible;
            m_View?.SetVisible(visible);
        }

        /// <summary>
        /// 現在の俳優を表す世界座標矩形を取得します。矩形が無効な場合は false を返します。
        /// </summary>
        /// <param name="rect">有効な worldBound。取得に失敗した場合は <see cref="Rect.zero"/>。</param>
        /// <returns>矩形を取得できた場合は <c>true</c>。</returns>
        public bool TryGetWorldRect(out Rect rect)
        {
            rect = default;

            var actorView = ActorViewComponent;
            if (actorView == null)
            {
                return false;
            }

            if (actorView.TryGetCachedWorldBound(out rect))
            {
                return rect.width > 0f && rect.height > 0f;
            }

            var root = actorView.ActorRoot;
            if (root == null)
            {
                return false;
            }

            var worldBound = root.worldBound;
            if (worldBound.width <= 0f || worldBound.height <= 0f)
            {
                return false;
            }

            rect = worldBound;
            return true;
        }

        /// <summary>
        /// 色の効果（Scratch の color）を絶対値で設定し、View へ反映する。
        /// </summary>
        /// <param name="value">設定する効果量。200 で 1 周とする。</param>
        public void SetColorEffect(float value)
        {
            var normalized = NormalizeColorEffect(value);
            var effects = m_State != null ? m_State.Effects : default;
            effects.ColorEffect = normalized;
            ApplyGraphicEffects(effects);
        }

        /// <summary>
        /// 色の効果に差分を加算し、正規化して View へ反映する。
        /// </summary>
        /// <param name="delta">加算する差分値。正で色相を進め、負で戻す。</param>
        public void ChangeColorEffect(float delta)
        {
            var current = m_State != null ? m_State.Effects.ColorEffect : 0f;
            SetColorEffect(current + delta);
        }

        /// <summary>
        /// すべての描画効果をリセットし、Tint を初期状態（白）へ戻す。
        /// </summary>
        public void ClearGraphicEffects()
        {
            ApplyGraphicEffects(default);
        }

        /// <summary>
        /// 指定された描画効果を状態に保存し、View へ適用する。
        /// </summary>
        /// <param name="effects">適用する描画効果。</param>
        public void ApplyGraphicEffects(ActorState.GraphicEffectsState effects)
        {
            if (m_State != null)
            {
                m_State.Effects = effects;
            }

            if (m_View == null)
            {
                return;
            }

            if (Mathf.Approximately(effects.ColorEffect, 0f))
            {
                m_View.ResetEffects();
                return;
            }

            m_View.ApplyGraphicEffects(effects);
        }

        /// <summary>
        /// 現在保持している描画効果を View へ再適用する。
        /// </summary>
        private void ApplyGraphicEffectsFromState()
        {
            if (m_View == null)
            {
                return;
            }

            if (m_State == null)
            {
                m_View.ResetEffects();
                return;
            }

            ApplyGraphicEffects(m_State.Effects);
        }

        /// <summary>
        /// 渡された色効果値を 0～200 の範囲に正規化する。
        /// </summary>
        /// <param name="value">正規化する値。</param>
        /// <returns>0～200 の範囲へ丸め込んだ値。</returns>
        private static float NormalizeColorEffect(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            var cycle = ColorEffectCycle;
            if (cycle <= 0f)
            {
                return 0f;
            }

            var normalized = value % cycle;
            if (normalized < 0f)
            {
                normalized += cycle;
            }

            return normalized;
        }

        /// <summary>
        /// 現在の設定に基づき、表示すべき Sprite を決定して View へ反映する。
        /// View 未接続時でも内部状態（SpriteIndex）は更新する。
        /// </summary>
        private void ApplyCurrentSpriteToView()
        {
            var sprite = ResolveCurrentSprite();

            if (m_View == null)
            {
                return;
            }

            m_View.SetSprite(sprite);
        }

        /// <summary>
        /// SpriteList の内容から現在使用する Sprite を解決する。
        /// </summary>
        /// <returns>表示に利用する Sprite。利用可能な Sprite が無ければ null。</returns>
        private Sprite ResolveCurrentSprite()
        {
            if (m_ActorData == null)
            {
                m_SpriteIndex = 0;
                if (m_State != null)
                {
                    m_State.SpriteIndex = 0;
                }

                return null;
            }

            Sprite resolved = null;

            if (m_UseSpriteIndexOverride)
            {
                resolved = ResolveSpriteFromList(m_SpriteIndex, out var overrideIndex);
                m_SpriteIndex = overrideIndex;

                if (m_State != null)
                {
                    m_State.SpriteIndex = overrideIndex;
                }

                if (resolved != null)
                {
                    return resolved;
                }
            }

            resolved = ResolveSpriteFromList(m_SpriteIndex, out var listIndex);
            m_SpriteIndex = listIndex;
            if (m_State != null)
            {
                m_State.SpriteIndex = listIndex;
            }

            if (resolved != null)
            {
                return resolved;
            }

            if (m_State != null)
            {
                m_State.SpriteIndex = SpriteCount > 0 ? Mathf.Clamp(m_SpriteIndex, 0, SpriteCount - 1) : 0;
            }

            return null;
        }

        /// <summary>
        /// SpriteList から指定インデックスの Sprite を取得し、null の場合は先頭から順に探索する。
        /// </summary>
        /// <param name="index">利用したいインデックス。</param>
        /// <param name="resolvedIndex">最終的に採用されたインデックス。Sprite が見つからなければ安全に丸められた値。</param>
        /// <returns>取得できた Sprite。全て null の場合は null。</returns>
        private Sprite ResolveSpriteFromList(int index, out int resolvedIndex)
        {
            resolvedIndex = 0;

            var sprites = m_ActorData?.Sprites;
            if (sprites == null || sprites.Count == 0)
            {
                return null;
            }

            var count = sprites.Count;
            var safeIndex = Mathf.Clamp(index, 0, count - 1);
            var candidate = sprites[safeIndex];
            if (candidate != null)
            {
                resolvedIndex = safeIndex;
                return candidate;
            }

            for (var i = 0; i < count; i++)
            {
                var fallback = sprites[i];
                if (fallback != null)
                {
                    resolvedIndex = i;
                    return fallback;
                }
            }

            resolvedIndex = safeIndex;
            return null;
        }

        /// <summary>
        /// 等倍基準のスケール値を直接設定し、内部的には拡大率（%）へ変換して統一ロジックに委譲する。
        /// </summary>
        /// <param name="scale">等倍=1 を基準としたスケール値。</param>
        public void SetScale(float scale)
        {
            var clampedScale = Mathf.Clamp(scale, MinSizePercent / 100f, MaxSizePercent / 100f);
            var percent = clampedScale * 100f;
            SetSizePercent(percent);
        }

        /// <summary>
        /// 拡大率（%）を絶対値で設定し、Model・View・境界情報を俳優ごとに更新する。
        /// </summary>
        /// <param name="percent">100 で等倍となる拡大率（%）。</param>
        public void SetSizePercent(float percent)
        {
            var clamped = Mathf.Clamp(percent, MinSizePercent, MaxSizePercent);

            if (!Mathf.Approximately(m_SizePercent, clamped))
            {
                m_SizePercent = clamped;
                m_CurrentScale = m_SizePercent / 100f;

                if (m_State != null)
                {
                    m_State.SizePercent = m_SizePercent;
                }
            }
            else if (m_State != null)
            {
                m_State.SizePercent = m_SizePercent;
            }

            m_View?.SetSizePercent(m_SizePercent);

            UpdateRenderedSizeFromView();
            ClampStateToBounds();
        }

        /// <summary>
        /// 拡大率（%）を相対値で変更し、現在値へ加算した結果をクランプのうえ反映する。
        /// </summary>
        /// <param name="deltaPercent">加算する差分（%）。正で拡大、負で縮小。</param>
        public void ChangeSizeByPercent(float deltaPercent)
        {
            var currentPercent = m_SizePercent;
            SetSizePercent(currentPercent + deltaPercent);
        }

        /// <summary>
        /// SpriteList のインデックスを設定し、利用可能な Sprite があれば即座に View へ反映する。
        /// </summary>
        /// <param name="index">使用したい Sprite のインデックス。範囲外の場合は自動的に丸め込まれる。</param>
        public void SetSpriteIndex(int index)
        {
            if (m_ActorData == null)
            {
                return;
            }

            var sprites = m_ActorData.Sprites;
            if (sprites != null && sprites.Count > 0)
            {
                m_SpriteIndex = Mathf.Clamp(index, 0, sprites.Count - 1);
                m_UseSpriteIndexOverride = true;

                if (m_State != null)
                {
                    m_State.SpriteIndex = m_SpriteIndex;
                }

                m_View?.SetSprite(sprites[m_SpriteIndex]);
                return;
            }

            m_UseSpriteIndexOverride = false;
            m_SpriteIndex = 0;
            if (m_State != null)
            {
                m_State.SpriteIndex = 0;
            }

            ApplyCurrentSpriteToView();
        }

        /// <summary>
        /// 現在の回転スタイルに基づき、View の回転角と左右反転を適用する。
        /// </summary>
        private void ApplyRotationStyleToView()
        {
            if (m_State == null)
            {
                m_LastFacingSignX = 1f;
                if (m_View != null)
                {
                    m_View.SetHorizontalFlipSign(1f);
                    m_View.SetRotationDegrees(0f);
                }

                return;
            }

            var style = m_State.RotationStyle;

            switch (style)
            {
                case RotationStyle.AllAround:
                    m_LastFacingSignX = 1f;
                    break;
                case RotationStyle.LeftRight:
                    var radians = m_State.DirectionDeg * Mathf.Deg2Rad;
                    var dirX = Mathf.Cos(radians);
                    if (Mathf.Abs(dirX) > 0.0001f)
                    {
                        m_LastFacingSignX = dirX >= 0f ? 1f : -1f;
                    }
                    break;
                case RotationStyle.DontRotate:
                    m_LastFacingSignX = 1f;
                    break;
            }

            if (m_View == null)
            {
                return;
            }

            switch (style)
            {
                case RotationStyle.AllAround:
                    m_View.SetHorizontalFlipSign(1f);
                    m_View.SetRotationDegrees(m_State.RotationDeg);
                    break;
                case RotationStyle.LeftRight:
                    m_View.SetHorizontalFlipSign(m_LastFacingSignX);
                    m_View.SetRotationDegrees(0f);
                    break;
                case RotationStyle.DontRotate:
                    m_View.SetHorizontalFlipSign(1f);
                    m_View.SetRotationDegrees(0f);
                    break;
            }
        }

        /// <summary>
        /// 吹き出しの残り表示時間を更新し、時間切れの場合は自動で非表示にする。
        /// </summary>
        /// <param name="deltaTime">経過時間（秒）。負値が渡された場合は 0 として扱う。</param>
        private void UpdateSpeechTimer(float deltaTime)
        {
            if (!m_SpeechUsesTimer || m_SpeechText == null)
            {
                return;
            }

            var safeDelta = Mathf.Max(0f, deltaTime);
            if (safeDelta <= 0f)
            {
                return;
            }

            m_SpeechRemainSeconds -= safeDelta;
            if (m_SpeechRemainSeconds <= 0f)
            {
                HideSpeech();
            }
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

            ApplyRotationStyleToView();
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

            ApplyRotationStyleToView();
        }

        /// <summary>
        /// Scratch 互換の回転スタイルを設定し、見た目へ即時反映する。
        /// </summary>
        /// <param name="style">適用する回転スタイル。</param>
        public void SetRotationStyle(RotationStyle style)
        {
            if (m_State == null)
            {
                return;
            }

            m_State.RotationStyle = style;
            ApplyRotationStyleToView();
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
        /// 浮遊演出に利用する論理座標オフセットを設定し、View へ即時反映する。モード設定で無効化されている場合は 0 扱いとする。
        /// </summary>
        /// <param name="offsetLogical">論理座標系での中心オフセット（px）。Scratch 中央原点では上方向が正。</param>
        public void SetFloatOffset(Vector2 offsetLogical)
        {
            m_FloatOffset = m_EnableFloatOffset ? offsetLogical : Vector2.zero;

            if (m_View == null)
            {
                return;
            }

            UpdateViewPosition();
        }

        /// <summary>
        /// 旧来の縦方向オフセット API を互換目的で残したラッパー。UI 基準の値を論理座標へ変換して <see cref="SetFloatOffset"/> に委譲する。
        /// </summary>
        /// <param name="offsetPx">UI Toolkit 基準での縦方向オフセット（px）。</param>
        [System.Obsolete("SetVisualYOffset は SetFloatOffset(Vector2) に置き換えられました。", false)]
        public void SetVisualYOffset(float offsetPx)
        {
            var logical = m_CoordinateOrigin == CoordinateOrigin.Center
                ? new Vector2(0f, -offsetPx)
                : new Vector2(0f, offsetPx);
            SetFloatOffset(logical);
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
            ApplyRotationStyleToView();
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

            var rootScaledSize = m_View != null ? m_View.GetRootScaledSizePx() : Vector2.zero;
            return ScratchBounds.ClampCenter(centerLogical, rootScaledSize);
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
            var visualSize = ResolveCurrentVisualSizeForAnchor(actorElement);
            var offsetLogical = m_EnableFloatOffset ? m_FloatOffset : Vector2.zero;
            var centerLogical = m_State.Position + offsetLogical;

            if (m_IsScratchMode && m_CoordinateOrigin == CoordinateOrigin.Center)
            {
                var rootScaledSize = m_View != null ? m_View.GetRootScaledSizePx() : Vector2.zero;
                centerLogical = ScratchBounds.ClampCenter(centerLogical, rootScaledSize);
            }

            var anchorUi = ConvertLogicalToUi(centerLogical);
            var centerUi = ConvertAnchorToCenter(anchorUi, visualSize);
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
                SetSizePercent(m_State.SizePercent);
                ApplyRotationStyleToView();
            }
            else
            {
                m_View.SetSizePercent(m_SizePercent);
                m_View.SetRotationDegrees(0f);
                m_View.SetHorizontalFlipSign(1f);
                UpdateRenderedSizeFromView();
                ClampStateToBounds();
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
        /// モード設定に応じて浮遊オフセット機能を有効化するかどうかを判断する。設定が無い場合は中央原点モードのみ無効化する。
        /// </summary>
        /// <param name="config">参照するモード設定。</param>
        /// <returns>浮遊オフセットを適用可能な場合は true。</returns>
        private bool ResolveFloatOffsetEnabled(FUnityModeConfig config)
        {
            if (config == null)
            {
                return m_CoordinateOrigin != CoordinateOrigin.Center;
            }

            return config.EnableFloatOffset;
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
