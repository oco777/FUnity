// Updated: 2025-03-03
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// Unity Visual Scripting から Presenter 層の操作を受け付けるブリッジ。
    /// Custom Event で命名されたドメインイベントを受信し、Model 更新と View 反映を一元化する。
    /// </summary>
    public sealed class VSPresenterBridge : MonoBehaviour
    {
        /// <summary>Visual Scripting から操作する対象の俳優 Presenter。</summary>
        [SerializeField]
        private ActorPresenter m_Target;

        /// <summary>ステージ背景を制御するサービス。<see cref="FUnityManager"/> から供給される。</summary>
        private StageBackgroundService m_StageBackgroundService;

        /// <summary>遅延実行を提供するタイマーサービス。</summary>
        private ITimerService m_TimerService;

        /// <summary>シーン内で最後に有効化されたブリッジのシングルトン参照。</summary>
        public static VSPresenterBridge Instance { get; private set; }

        /// <summary>
        /// MonoBehaviour 初期化時にグローバル参照を登録し、重複時は警告を出す。
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: 複数インスタンスが有効化されています。最新のコンポーネントを Instance として上書きします。");
            }

            Instance = this;
        }

        /// <summary>
        /// 破棄時にシングルトン参照を解除する。
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Visual Scripting から操作可能な Presenter インスタンス。
        /// </summary>
        public ActorPresenter Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        /// <summary>
        /// ステージ背景サービスを注入する。null の場合は警告を出しつつ解除する。
        /// </summary>
        /// <param name="service">背景適用サービス。</param>
        public void SetStageBackgroundService(StageBackgroundService service)
        {
            m_StageBackgroundService = service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: StageBackgroundService が未設定です。");
            }
        }

        /// <summary>
        /// タイマーサービスを注入し、遅延実行を可能にする。
        /// </summary>
        /// <param name="service">遅延実行サービス。</param>
        public void SetTimerService(ITimerService service)
        {
            m_TimerService = service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ITimerService が未設定です。");
            }
        }

        /// <summary>
        /// 俳優を絶対座標へ移動させる。Custom Event "Actor/SetPosition" を想定。
        /// </summary>
        /// <param name="x">X 座標（px）。</param>
        /// <param name="y">Y 座標（px）。</param>
        public void OnActorSetPosition(float x, float y)
        {
            if (m_Target == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ActorPresenter が未設定のため位置を変更できません。");
                return;
            }

            m_Target.SetPosition(new Vector2(x, y));
        }

        /// <summary>
        /// 俳優を相対移動させる。Custom Event "Actor/MoveBy" を想定。
        /// </summary>
        /// <param name="dx">X 方向の変位。</param>
        /// <param name="dy">Y 方向の変位。</param>
        public void OnActorMoveBy(float dx, float dy)
        {
            if (m_Target == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ActorPresenter が未設定のため相対移動できません。");
                return;
            }

            m_Target.MoveBy(new Vector2(dx, dy));
        }

        /// <summary>
        /// 俳優の吹き出し表示を要求する。Custom Event "Actor/Say" を想定。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        /// <param name="seconds">表示継続時間（秒）。</param>
        public void OnActorSay(string message, float seconds = 2f)
        {
            if (m_Target == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ActorPresenter が未設定のため吹き出しを表示できません。");
                return;
            }

            m_Target.Say(message, seconds);
        }

        /// <summary>
        /// 俳優のサイズ（幅・高さ）を直接設定する。Custom Event "Actor/SetSizeWH" を想定。
        /// </summary>
        /// <param name="width">幅（px）。</param>
        /// <param name="height">高さ（px）。</param>
        public void OnActorSetSize(float width, float height)
        {
            if (m_Target == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ActorPresenter が未設定のためサイズを変更できません。");
                return;
            }

            m_Target.SetSize(new Vector2(width, height));
        }

        /// <summary>
        /// 俳優のスケールを一括変更する。Custom Event "Actor/SetSize" を想定。
        /// </summary>
        /// <param name="scale">等倍基準の拡大率。</param>
        public void OnActorSetScale(float scale)
        {
            if (m_Target == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ActorPresenter が未設定のためスケールを変更できません。");
                return;
            }

            m_Target.SetScale(scale);
        }

        /// <summary>
        /// 俳優の見た目を Scratch と同様に相対回転させる。Custom Event "Actor/TurnDegrees" を想定。
        /// </summary>
        /// <param name="degrees">加算する角度（度）。正で反時計回り。</param>
        /// <param name="actorId">将来的に複数俳優を識別するための任意 ID。null で既定俳優。</param>
        public void TurnDegrees(float degrees, string actorId = null)
        {
            var presenter = ResolveActorPresenter(actorId);
            if (presenter == null)
            {
                Debug.LogWarning($"[FUnity] VSPresenterBridge: ActorPresenter が未設定のため回転できません。(actorId={actorId ?? "<default>"})");
                return;
            }

            presenter.RotateBy(degrees);
        }

        /// <summary>
        /// ステージ背景色を変更する。Custom Event "Stage/SetBackgroundColor" を想定。
        /// </summary>
        /// <param name="color">適用する色。</param>
        public void OnStageSetBackgroundColor(Color color)
        {
            if (m_StageBackgroundService == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: StageBackgroundService が未設定のため背景色を変更できません。");
                return;
            }

            m_StageBackgroundService.SetBackgroundColor(color);
        }

        /// <summary>
        /// 背景テクスチャを直接設定する。Custom Event "Stage/SetBackground" を想定。
        /// </summary>
        /// <param name="texture">背景に用いるテクスチャ。</param>
        public void OnStageSetBackground(Texture2D texture)
        {
            if (m_StageBackgroundService == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: StageBackgroundService が未設定のため背景画像を変更できません。");
                return;
            }

            m_StageBackgroundService.SetBackground(texture, ScaleMode.ScaleAndCrop);
        }

        /// <summary>
        /// Resources パスを指定して背景画像を読み込む。Custom Event "Stage/SetBackgroundPath" を想定。
        /// </summary>
        /// <param name="resourcesPath">`Resources/` 配下のパス。</param>
        public void OnStageSetBackground(string resourcesPath)
        {
            if (m_StageBackgroundService == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: StageBackgroundService が未設定のため背景画像を変更できません。");
                return;
            }

            m_StageBackgroundService.SetBackground(resourcesPath);
        }

        /// <summary>
        /// 指定秒数後に Custom Event を発火する。Custom Event "Timer/Invoke" などから利用可能。
        /// </summary>
        /// <param name="delaySeconds">待機する秒数。</param>
        /// <param name="eventName">発火する Custom Event 名。</param>
        /// <param name="target">発火対象 GameObject。null の場合は自身。</param>
        /// <param name="argument">イベントに渡す追加引数。</param>
        public void InvokeCustomEventAfter(float delaySeconds, string eventName, GameObject target = null, object argument = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: eventName が空のため Custom Event を送信できません。");
                return;
            }

            if (m_TimerService == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ITimerService が未設定のため遅延実行できません。");
                return;
            }

            var targetObject = target != null ? target : gameObject;
            m_TimerService.Invoke(delaySeconds, () => CustomEvent.Trigger(targetObject, eventName, argument));
        }

        /// <summary>
        /// Visual Scripting から移動入力とデルタ時間を受け取り、従来の Tick 更新を実行するレガシー API。
        /// </summary>
        /// <param name="direction">正規化済みを想定した入力方向。</param>
        /// <param name="deltaTime">経過時間（秒）。</param>
        public void VS_Move(Vector2 direction, float deltaTime)
        {
            m_Target?.Tick(deltaTime, direction);
        }

        /// <summary>
        /// ActorPresenter を ID から解決する。現状は既定ターゲットのみ対応し、拡張余地を残す。
        /// </summary>
        /// <param name="actorId">識別子。null の場合は既定。</param>
        /// <returns>解決した Presenter。見つからない場合は null。</returns>
        private ActorPresenter ResolveActorPresenter(string actorId)
        {
            if (!string.IsNullOrEmpty(actorId))
            {
                Debug.LogWarning($"[FUnity] VSPresenterBridge: actorId フィルタリングは未実装です。actorId={actorId} の要求には既定ターゲットを返します。");
            }

            return m_Target;
        }
    }
}
