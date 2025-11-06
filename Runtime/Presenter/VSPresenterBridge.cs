// Updated: 2025-03-03
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;

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

        /// <summary>VS から俳優 Presenter を解決するための弱参照キャッシュ。</summary>
        private static readonly Dictionary<string, WeakReference<ActorPresenter>> s_ActorLookup = new Dictionary<string, WeakReference<ActorPresenter>>();

        /// <summary>弱参照が切れたキーを再利用せず整理するための一時バッファ。</summary>
        private static readonly List<string> s_PruningBuffer = new List<string>(8);

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
        /// VS グラフで利用する俳優キーを登録し、弱参照キャッシュへ記録します。
        /// </summary>
        /// <param name="presenter">登録対象の Presenter。</param>
        internal static void RegisterActor(ActorPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            lock (s_ActorLookup)
            {
                s_ActorLookup[presenter.ActorKey] = new WeakReference<ActorPresenter>(presenter);
            }
        }

        /// <summary>
        /// 弱参照キャッシュから俳優を除外します。Presenter が破棄された際の明示的な解除用です。
        /// </summary>
        /// <param name="presenter">解除対象の Presenter。</param>
        internal static void UnregisterActor(ActorPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            lock (s_ActorLookup)
            {
                s_ActorLookup.Remove(presenter.ActorKey);
            }
        }

        /// <summary>
        /// DisplayName で一致する俳優（本体＋クローン）を列挙し、可視状態に応じてフィルタリングします。
        /// </summary>
        /// <param name="displayName">検索する DisplayName。空白のみの場合は結果を返しません。</param>
        /// <param name="onlyVisible">true の場合は可視なインスタンスのみ返します。</param>
        /// <returns>条件に一致した俳優キーの列挙。</returns>
        public static IEnumerable<string> EnumerateActorKeysByDisplayName(string displayName, bool onlyVisible)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                yield break;
            }

            var normalized = displayName.Trim();
            var results = new List<string>();

            lock (s_ActorLookup)
            {
                s_PruningBuffer.Clear();

                foreach (var pair in s_ActorLookup)
                {
                    if (!pair.Value.TryGetTarget(out var presenter) || presenter == null)
                    {
                        s_PruningBuffer.Add(pair.Key);
                        continue;
                    }

                    if (!string.Equals(presenter.DisplayName, normalized, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (onlyVisible && !presenter.IsVisible)
                    {
                        continue;
                    }

                    results.Add(pair.Key);
                }

                if (s_PruningBuffer.Count > 0)
                {
                    for (var i = 0; i < s_PruningBuffer.Count; i++)
                    {
                        s_ActorLookup.Remove(s_PruningBuffer[i]);
                    }

                    s_PruningBuffer.Clear();
                }
            }

            for (var i = 0; i < results.Count; i++)
            {
                yield return results[i];
            }
        }

        /// <summary>
        /// 俳優キーから可視状態を取得します。未登録の場合は false を返します。
        /// </summary>
        /// <param name="actorKey">判定対象の俳優キー。</param>
        /// <returns>可視であれば true。</returns>
        public static bool IsVisible(string actorKey)
        {
            if (!TryResolvePresenter(actorKey, out var presenter))
            {
                return false;
            }

            return presenter.IsVisible;
        }

        /// <summary>
        /// 俳優キーから worldBound を取得します。矩形を取得できない場合は false を返します。
        /// </summary>
        /// <param name="actorKey">対象の俳優キー。</param>
        /// <param name="rect">取得した世界座標矩形。</param>
        /// <returns>矩形を取得できた場合は <c>true</c>。</returns>
        public static bool TryGetWorldRect(string actorKey, out Rect rect)
        {
            rect = default;
            if (!TryResolvePresenter(actorKey, out var presenter))
            {
                return false;
            }

            return presenter.TryGetWorldRect(out rect);
        }

        /// <summary>
        /// 俳優を絶対座標（中心）へ移動させる。Custom Event "Actor/SetPosition" を想定。
        /// </summary>
        /// <param name="x">中心の X 座標（px）。</param>
        /// <param name="y">中心の Y 座標（px）。</param>
        public void OnActorSetPosition(float x, float y)
        {
            if (m_Target == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ActorPresenter が未設定のため位置を変更できません。");
                return;
            }

            var logical = new Vector2(x, y);
            var uiPosition = m_Target.ToUiPosition(logical);
            m_Target.SetPositionPixels(uiPosition);
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

            var logicalDelta = new Vector2(dx, dy);
            var uiDelta = m_Target.ToUiDelta(logicalDelta);
            m_Target.MoveByPixels(uiDelta);
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

            m_Target.ShowSpeech(message, seconds, false);
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
        /// 俳優のスケールを直接指定し、Presenter 層に適用する。Scratch の Set Size などから使用される。
        /// </summary>
        /// <param name="scale">等倍基準のスケール。</param>
        /// <param name="actorId">対象俳優の識別子。null で既定俳優。</param>
        public void SetActorScale(float scale, string actorId = null)
        {
            var presenter = ResolveActorPresenter(actorId);
            if (presenter == null)
            {
                Debug.LogWarning($"[FUnity] VSPresenterBridge: ActorPresenter が未設定のためスケールを設定できません。(actorId={actorId ?? "<default>"})");
                return;
            }

            presenter.SetScale(scale);
        }

        /// <summary>
        /// 俳優の大きさを絶対値（%）で設定する。Scratch の「大きさを ◯ % にする」を想定する。
        /// </summary>
        /// <param name="percent">100 で等倍となる拡大率（%）。</param>
        /// <param name="actorId">将来的に複数俳優へ対応するための識別子。null で既定俳優。</param>
        public void SetActorSizePercent(float percent, string actorId = null)
        {
            var scale = percent / 100f;
            SetActorScale(scale, actorId);
        }

        /// <summary>
        /// DisplayName で指定した俳優のテンプレートからクローンを生成し、生成されたアダプタを返します。
        /// </summary>
        /// <param name="source">クローン生成を要求する実行中のアダプタ。</param>
        /// <param name="targetDisplayName">複製元となる俳優の DisplayName。</param>
        /// <returns>生成されたクローンの <see cref="ActorPresenterAdapter"/>。失敗時は null。</returns>
        public ActorPresenterAdapter RequestCloneByDisplayName(ActorPresenterAdapter source, string targetDisplayName)
        {
            if (source == null)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: RequestCloneByDisplayName に null アダプタが渡されたため処理を中止します。");
                return null;
            }

            if (string.IsNullOrWhiteSpace(targetDisplayName))
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: DisplayName が空のためクローンを生成できません。");
                return null;
            }

            var normalizedDisplayName = targetDisplayName.Trim();

            var manager = FUnityManager.Instance;
            // FUnityManager がシーンに存在しないケースを早期に検出するための防御コード。
            if (manager == null)
            {
                Debug.LogError("[FUnity] FUnityManager.Instance が見つかりません。サンプルシーンのセットアップを確認してください。");
                return null;
            }

            var project = manager.ProjectData;
            if (project?.Actors == null || project.Actors.Count == 0)
            {
                Debug.LogWarning("[FUnity] VSPresenterBridge: ProjectData に俳優が登録されていないためクローンを生成できません。");
                return null;
            }

            var template = project.Actors.FirstOrDefault(actor => actor != null && actor.DisplayName == normalizedDisplayName);
            if (template == null)
            {
                Debug.LogWarning($"[FUnity] VSPresenterBridge: DisplayName='{normalizedDisplayName}' に一致する俳優が見つかりません。");
                return null;
            }

            var cloneAdapter = manager.SpawnCloneFromTemplate(source, template);
            return cloneAdapter;
        }

        /// <summary>
        /// 俳優の大きさを相対値（%）で変更する。Scratch の「大きさを ◯ % ずつ変える」を想定する。
        /// </summary>
        /// <param name="deltaPercent">加算する拡大率（%）。正で拡大、負で縮小。</param>
        /// <param name="actorId">将来的な複数俳優対応用の識別子。null で既定俳優。</param>
        public void ChangeActorSizeByPercent(float deltaPercent, string actorId = null)
        {
            var presenter = ResolveActorPresenter(actorId);
            if (presenter == null)
            {
                Debug.LogWarning($"[FUnity] VSPresenterBridge: ActorPresenter が未設定のため大きさを変更できません。(actorId={actorId ?? "<default>"})");
                return;
            }

            presenter.ChangeSizeByPercent(deltaPercent);
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
        /// Graph Variables 等から渡された Presenter 参照に対し、自分自身のみの回転を適用する静的ヘルパー。
        /// </summary>
        /// <param name="presenterObj">Visual Scripting グラフから取得した Presenter 相当のオブジェクト。</param>
        /// <param name="degrees">加算する角度（度）。</param>
        public static void TurnSelf(object presenterObj, float degrees)
        {
            if (presenterObj is ActorPresenter presenter && presenter != null)
            {
                presenter.TurnSelf(degrees);
                return;
            }

            Debug.LogWarning("[FUnity] VSPresenterBridge: TurnSelf は ActorPresenter 参照を解決できず相対回転を適用できません。Runner の Object 変数に presenter/view/ui を設定し、Visual Scripting からは Variables.Object(flow.stack.gameObject) で参照してください。");
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

            m_StageBackgroundService.SetBackground(texture);
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

        /// <summary>
        /// 指定した Runner から ActorPresenter を解決する静的ヘルパー。
        /// </summary>
        /// <param name="runner">Visual Scripting Runner。</param>
        /// <returns>解決した Presenter。存在しない場合は null。</returns>
        public static ActorPresenter TryGetPresenterFromRunner(GameObject runner)
        {
            if (runner == null)
            {
                return null;
            }

            var variables = Variables.Object(runner);
            if (variables != null)
            {
                if (variables.IsDefined("presenter") && variables.Get("presenter") is ActorPresenter presenter && presenter != null)
                {
                    return presenter;
                }

                if (variables.IsDefined(nameof(ActorPresenter)) && variables.Get(nameof(ActorPresenter)) is ActorPresenter namedPresenter && namedPresenter != null)
                {
                    return namedPresenter;
                }
            }

            var adapter = runner.GetComponent<ActorPresenterAdapter>();
            return adapter != null ? adapter.Presenter : null;
        }

        /// <summary>
        /// 弱参照キャッシュから俳優 Presenter を解決します。見つからない場合は false を返します。
        /// </summary>
        /// <param name="actorKey">検索する俳優キー。</param>
        /// <param name="presenter">解決した Presenter。</param>
        /// <returns>解決に成功した場合は <c>true</c>。</returns>
        private static bool TryResolvePresenter(string actorKey, out ActorPresenter presenter)
        {
            presenter = null;
            if (string.IsNullOrEmpty(actorKey))
            {
                return false;
            }

            lock (s_ActorLookup)
            {
                if (!s_ActorLookup.TryGetValue(actorKey, out var weak) || weak == null)
                {
                    return false;
                }

                if (!weak.TryGetTarget(out presenter) || presenter == null)
                {
                    s_ActorLookup.Remove(actorKey);
                    presenter = null;
                    return false;
                }

                return true;
            }
        }
    }
}
