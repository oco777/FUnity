// Updated: 2025-10-19
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Input;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.View;
using Object = UnityEngine.Object;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch 系のコルーチン Unit が共有する、コルーチン入力生成のための基底クラスです。
    /// スレッド停止機能は Scratch の EventUnit 側でのみ扱います。
    /// </summary>
    public abstract class ScratchCoroutineUnitBase : Unit
    {
        /// <summary>
        /// Visual Scripting 標準の ControlInputCoroutine を使って、
        /// IEnumerator ベースのコルーチン Unit を定義するためのヘルパーです。
        /// </summary>
        /// <param name="key">ControlInput のキー名。</param>
        /// <param name="coroutineFactory">Flow から IEnumerator を生成するファクトリ。</param>
        /// <returns>生成された ControlInput。</returns>
        protected ControlInput CreateScratchCoroutineInput(
            string key,
            Func<Flow, IEnumerator> coroutineFactory)
        {
            // この入力は「コルーチン」として扱われる。
            return ControlInputCoroutine(key, flow =>
            {
                if (coroutineFactory == null)
                {
                    return null;
                }

                // Unit 側で定義した IEnumerator をそのまま Visual Scripting に渡す。
                return coroutineFactory(flow);
            });
        }
    }

    /// <summary>
    /// Scratch 系 Unit 共通の補助処理を提供し、Presenter アダプタの自動解決や方向ベクトル計算を行うユーティリティクラスです。
    /// </summary>
    internal static class ScratchUnitUtil
    {
        /// <summary>直近で解決したアダプタを保持する WeakReference です。破棄後は null になります。</summary>
        private static WeakReference<ActorPresenterAdapter> m_CachedAdapter;

        /// <summary>解決失敗時の警告を重複表示しないためのフラグです。</summary>
        private static bool m_HasLoggedResolutionFailure;

        /// <summary>Scratch の 1 歩をピクセルへ換算する倍率です。</summary>
        private const float StepToPixels = 1f;

        /// <summary>フロー変数に保存するスレッド ID のキーです。</summary>
        private const string ThreadIdKey = "FUNITY_SCRATCH_THREAD_ID";

        /// <summary>フロー変数に保存する俳優 ID のキーです。</summary>
        private const string ActorIdKey = "FUNITY_SCRATCH_ACTOR_ID";

        /// <summary>
        /// 現在のフローがホストしている Runner（Self）に紐づく Object 変数を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。null の場合は null を返します。</param>
        /// <returns>Runner 単位の Object 変数。取得できない場合は null。</returns>
        public static VariableDeclarations GetObjectVars(Flow flow)
        {
            if (flow == null || flow.stack == null)
            {
                return null;
            }

            var host = flow.stack.gameObject;
            if (host == null)
            {
                return null;
            }

            return Unity.VisualScripting.Variables.Object(host);
        }

        /// <summary>
        /// Runner に格納された presenter 参照を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>presenter 参照。未登録の場合は null。</returns>
        public static object GetPresenter(Flow flow)
        {
            VariableDeclarations objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            return objectVariables.IsDefined("presenter") ? objectVariables.Get("presenter") : null;
        }

        /// <summary>
        /// Runner に格納された view 参照を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>view 参照。未登録の場合は null。</returns>
        public static object GetView(Flow flow)
        {
            VariableDeclarations objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            return objectVariables.IsDefined("view") ? objectVariables.Get("view") : null;
        }

        /// <summary>
        /// Runner に格納された ui 参照を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>ui 参照。未登録の場合は null。</returns>
        public static object GetUI(Flow flow)
        {
            VariableDeclarations objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            return objectVariables.IsDefined("ui") ? objectVariables.Get("ui") : null;
        }

        /// <summary>
        /// Flow に保存されたスレッドコンテキストを取得し、俳優 ID とスレッド ID を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="actorId">取得した俳優 ID。</param>
        /// <param name="threadId">取得したスレッド ID。</param>
        /// <returns>必要な情報が揃っている場合は true。</returns>
        public static bool TryGetThreadContext(Flow flow, out string actorId, out string threadId)
        {
            actorId = null;
            threadId = null;

            if (flow == null)
            {
                return false;
            }

            var variables = flow.variables;
            if (variables == null)
            {
                return false;
            }

            if (!variables.IsDefined(ActorIdKey) || !variables.IsDefined(ThreadIdKey))
            {
                return false;
            }

            actorId = variables.Get<string>(ActorIdKey);
            threadId = variables.Get<string>(ThreadIdKey);

            if (string.IsNullOrEmpty(threadId))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Guid 形式での互換取得用ラッパーです。既存の停止系ユニットからの移行期間に利用します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="actorId">取得した俳優 ID。</param>
        /// <param name="threadId">取得したスレッド ID。</param>
        /// <returns>必要な情報が揃っており、Guid へ変換できた場合は true。</returns>
        public static bool TryGetThreadContext(Flow flow, out string actorId, out Guid threadId)
        {
            string threadIdString;
            var result = TryGetThreadContext(flow, out actorId, out threadIdString);
            if (!result)
            {
                threadId = Guid.Empty;
                return false;
            }

            if (!Guid.TryParse(threadIdString, out threadId))
            {
                threadId = Guid.Empty;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Flow に俳優 ID とスレッド ID を保存し、停止系ユニットから参照できるようにします。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="actorId">保存する俳優 ID。</param>
        /// <param name="threadId">保存するスレッド ID。</param>
        public static void SetThreadContext(Flow flow, string actorId, string threadId)
        {
            if (flow == null)
            {
                return;
            }

            var variables = flow.variables;
            if (variables == null)
            {
                return;
            }

            variables.Set(ActorIdKey, actorId ?? string.Empty);
            variables.Set(ThreadIdKey, threadId ?? string.Empty);
        }

        /// <summary>
        /// Guid 形式の ID を受け取り、内部的に文字列化して保存する互換ラッパーです。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="actorId">保存する俳優 ID。</param>
        /// <param name="threadId">保存するスレッド ID。</param>
        public static void SetThreadContext(Flow flow, string actorId, Guid threadId)
        {
            SetThreadContext(flow, actorId, threadId.ToString());
        }

        /// <summary>
        /// Scratch のイベント Unit から開始されたスレッドを FUnityScriptThreadManager に登録し、Flow.variables に俳優 ID とスレッド ID を保存します。
        /// すでにフロー側にスレッド ID が設定されている場合は再登録せず、その ID を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="adapter">俳優の Presenter 参照を解決するアダプタ。</param>
        /// <param name="graph">実行中の ScriptGraph アセット。</param>
        /// <param name="coroutine">登録対象のコルーチン。</param>
        /// <returns>登録済みまたは新規登録したスレッド ID。登録に失敗した場合は null。</returns>
        public static string EnsureScratchThreadRegistered(
            Flow flow,
            ActorPresenterAdapter adapter,
            ScriptGraphAsset graph,
            Coroutine coroutine)
        {
            if (flow == null || coroutine == null)
            {
                return null;
            }

            if (TryGetThreadContext(flow, out string existingActorId, out string existingThreadId) &&
                !string.IsNullOrEmpty(existingThreadId))
            {
                return existingThreadId;
            }

            var manager = FUnityScriptThreadManager.Instance;
            if (manager == null)
            {
                return null;
            }

            var actorId = existingActorId;
            if (string.IsNullOrEmpty(actorId) && adapter != null && adapter.Presenter != null)
            {
                actorId = adapter.Presenter.ActorKey;
            }

            var threadId = manager.RegisterScratchThread(actorId, graph, coroutine);
            if (!string.IsNullOrEmpty(threadId))
            {
                SetThreadContext(flow, actorId, threadId);
            }

            return threadId;
        }

        /// <summary>
        /// Flow 情報から <see cref="ActorPresenterAdapter"/> を自動的に解決します。
        /// 優先度: ScriptGraphAsset Variables → Graph Variables → Object Variables → 自身の GameObject → 静的キャッシュ → シーン検索。
        /// </summary>
        /// <param name="flow">現在のフロー情報。null の場合は静的キャッシュとシーン検索のみ利用します。</param>
        /// <returns>解決に成功した <see cref="ActorPresenterAdapter"/>。見つからない場合は null。</returns>
        public static ActorPresenterAdapter ResolveAdapter(Flow flow)
        {
            var declarations = (flow?.stack?.graph as FlowGraph)?.variables;
            if (declarations != null)
            {
                if (declarations.IsDefined("adapter"))
                {
                    if (declarations.Get("adapter") is ActorPresenterAdapter fromAsset && fromAsset != null)
                    {
                        CacheAdapter(fromAsset);
                        return fromAsset;
                    }
                }
            }

            var fromGraphVariables = TryGetFromGraphVariables(flow);
            if (fromGraphVariables != null)
            {
                CacheAdapter(fromGraphVariables);
                return fromGraphVariables;
            }

            var fromObjectVariables = TryGetFromObjectVariables(flow);
            if (fromObjectVariables != null)
            {
                CacheAdapter(fromObjectVariables);
                return fromObjectVariables;
            }

            var fromSelf = TryGetFromSelf(flow);
            if (fromSelf != null)
            {
                CacheAdapter(fromSelf);
                return fromSelf;
            }

            if (TryGetFromStaticCache(out var cached))
            {
                CacheAdapter(cached);
                return cached;
            }

            var fromScene = FindAdapterInScene();
            if (fromScene != null)
            {
                CacheAdapter(fromScene);
                return fromScene;
            }

            LogResolutionFailureOnce();
            return null;
        }

        /// <summary>
        /// GameObject 入力または既存の解決ロジックから <see cref="ActorPresenterAdapter"/> を取得します。
        /// GameObject が指定されていない場合は <see cref="ResolveAdapter(Flow)"/> を利用します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="actorInput">GameObject を受け取る ValueInput。null の場合は Runner から自動解決します。</param>
        /// <param name="adapter">解決に成功したアダプタ。失敗時は null。</param>
        /// <returns>解決に成功した場合は <c>true</c>。</returns>
        public static bool TryGetActorAdapter(Flow flow, ValueInput actorInput, out ActorPresenterAdapter adapter)
        {
            adapter = null;

            GameObject actorObject = null;
            if (actorInput != null && flow != null)
            {
                actorObject = flow.GetValue<GameObject>(actorInput);
            }

            if (actorObject != null)
            {
                adapter = actorObject.GetComponent<ActorPresenterAdapter>();
                if (adapter == null)
                {
                    adapter = actorObject.GetComponentInChildren<ActorPresenterAdapter>(true);
                }

                if (adapter != null)
                {
                    CacheAdapter(adapter);
                    return true;
                }
            }

            adapter = ResolveAdapter(flow);
            return adapter != null;
        }

        /// <summary>
        /// 表示名から <see cref="ActorPresenterAdapter"/> を探索し、見つかった場合に返します。
        /// </summary>
        /// <param name="displayName">検索する表示名。</param>
        /// <param name="adapter">見つかったアダプタ。見つからない場合は null。</param>
        /// <returns>検索に成功した場合は <c>true</c>。</returns>
        public static bool TryFindActorByDisplayName(string displayName, out ActorPresenterAdapter adapter)
        {
            adapter = null;
            if (string.IsNullOrEmpty(displayName))
            {
                return false;
            }

            var normalized = displayName.Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            var adapters = Object.FindObjectsByType<ActorPresenterAdapter>(FindObjectsSortMode.None);
            foreach (var candidate in adapters)
            {
                if (MatchesDisplayName(candidate, normalized))
                {
                    adapter = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 俳優の UI ルート要素から worldBound を取得します。レイアウトが未確定で有効サイズが得られない場合は false を返します。
        /// </summary>
        /// <param name="adapter">対象の <see cref="ActorPresenterAdapter"/>。null の場合は失敗します。</param>
        /// <param name="worldRect">取得した worldBound。失敗時は <see cref="Rect.zero"/>。</param>
        /// <returns>有効な worldBound を得られた場合は <c>true</c>。</returns>
        public static bool TryGetActorWorldRect(ActorPresenterAdapter adapter, out Rect worldRect)
        {
            worldRect = default;
            if (adapter == null)
            {
                return false;
            }

            var actorView = adapter.ActorView;
            if (actorView != null && actorView.TryGetCachedWorldBound(out var cachedRect))
            {
                worldRect = cachedRect;
                return true;
            }

            VisualElement rootElement = null;
            if (actorView != null)
            {
                rootElement = actorView.ActorRoot;
            }

            rootElement ??= adapter.BoundElement;
            if (rootElement == null)
            {
                return false;
            }

            var rect = rootElement.worldBound;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return false;
            }

            worldRect = rect;
            return true;
        }

        /// <summary>
        /// Scratch ステージの論理座標における中心座標をクランプします。
        /// </summary>
        /// <param name="logical">クランプ対象の論理座標。</param>
        /// <returns>ステージ範囲へ丸め込んだ論理座標。</returns>
        public static Vector2 ClampToStageBounds(Vector2 logical)
        {
            try
            {
                var clampedX = Mathf.Clamp(logical.x, -ScratchBounds.StageHalfW, ScratchBounds.StageHalfW);
                var clampedY = Mathf.Clamp(logical.y, -ScratchBounds.StageHalfH, ScratchBounds.StageHalfH);
                return new Vector2(clampedX, clampedY);
            }
            catch
            {
                return logical;
            }
        }

        /// <summary>
        /// 現在有効なマウス座標プロバイダを解決します。FUnityManager 優先で、未設定時はサービスロケータを参照します。
        /// </summary>
        /// <returns>利用可能な <see cref="IMousePositionProvider"/>。無ければ null。</returns>
        public static IMousePositionProvider ResolveMouseProvider()
        {
            var provider = FUnityManager.MouseProvider;
            if (provider != null)
            {
                return provider;
            }

            return FUnityServices.MousePosition;
        }

        /// <summary>
        /// マウスポインターのスクリーン座標から Scratch 論理座標を推定します。
        /// </summary>
        /// <param name="referenceAdapter">ステージ座標変換に使用する参照アダプタ。</param>
        /// <returns>推定した論理座標。失敗時はスクリーン座標から簡易変換した値。</returns>
        public static Vector2 GetMouseLogicalPosition(ActorPresenterAdapter referenceAdapter)
        {
            var provider = ResolveMouseProvider();
            if (provider != null)
            {
                return ClampToStageBounds(provider.StagePosition);
            }

            var pointer = UnityEngine.Input.mousePosition;
            if (referenceAdapter != null)
            {
                var presenter = referenceAdapter.Presenter;
                var stageRoot = presenter != null ? presenter.StageRootElement : null;
                if (stageRoot != null)
                {
                    var panel = stageRoot.panel;
                    if (panel != null)
                    {
                        var panelPoint = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(pointer.x, pointer.y));
                        var local = panelPoint - stageRoot.worldBound.position;
                        var logical = referenceAdapter.ToLogicalPosition(local);
                        return ClampToStageBounds(logical);
                    }
                }
            }

            var fallback = new Vector2(
                pointer.x - (Screen.width * 0.5f),
                (Screen.height * 0.5f) - pointer.y);
            return ClampToStageBounds(fallback);
        }

        /// <summary>
        /// 俳優の #root サイズをもとに、Scratch 論理座標系での半幅・半高を推定します。
        /// </summary>
        /// <param name="adapter">対象のアダプタ。</param>
        /// <param name="halfSize">取得した半幅・半高。失敗時は <see cref="Vector2.zero"/>。</param>
        /// <returns>推定に成功した場合は <c>true</c>。</returns>
        public static bool TryGetActorHalfSizeLogical(ActorPresenterAdapter adapter, out Vector2 halfSize)
        {
            halfSize = Vector2.zero;
            if (adapter == null)
            {
                return false;
            }

            var presenter = adapter.Presenter;
            var view = adapter.ActorView;
            if (presenter == null || view == null)
            {
                return false;
            }

            var rootSize = view.GetRootScaledSizePx();
            if (rootSize.x <= 0f || rootSize.y <= 0f)
            {
                return false;
            }

            var halfUi = rootSize * 0.5f;
            var logicalHalf = presenter.ToLogicalDelta(halfUi);
            halfSize = new Vector2(Mathf.Abs(logicalHalf.x), Mathf.Abs(logicalHalf.y));
            return halfSize.x > 0f && halfSize.y > 0f;
        }

        /// <summary>
        /// 指定アクターを目標座標へ滑らかに移動させます。
        /// </summary>
        /// <param name="adapter">移動させるアダプタ。</param>
        /// <param name="targetLogical">目標の論理座標。</param>
        /// <param name="duration">移動にかける秒数。0 以下の場合は即時移動。</param>
        /// <param name="exit">完了時に返す ControlOutput。</param>
        /// <returns>移動処理を行う列挙子。</returns>
        public static System.Collections.IEnumerator GlideActorTo(ActorPresenterAdapter adapter, Vector2 targetLogical, float duration, ControlOutput exit)
        {
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide: ActorPresenterAdapter が解決できませんでした。");
                yield return exit;
                yield break;
            }

            var presenter = adapter.Presenter;
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide: ActorPresenter が未割り当てのため移動できません。");
                yield return exit;
                yield break;
            }

            var clampedTarget = ClampToStageBounds(targetLogical);
            var startLogical = ClampToStageBounds(presenter.GetPosition());
            var safeDuration = Mathf.Max(0f, duration);
            if (safeDuration <= 0f)
            {
                var uiInstant = adapter.ToUiPosition(clampedTarget);
                adapter.SetPositionPixels(uiInstant);
                yield return exit;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.Clamp01(elapsed / safeDuration);
                var logical = Vector2.Lerp(startLogical, clampedTarget, ratio);
                var clampedStep = ClampToStageBounds(logical);
                var uiStep = adapter.ToUiPosition(clampedStep);
                adapter.SetPositionPixels(uiStep);
                yield return null;
            }

            var uiTarget = adapter.ToUiPosition(clampedTarget);
            adapter.SetPositionPixels(uiTarget);
            yield return exit;
        }

        /// <summary>
        /// ActorPresenterAdapter が保持する名称情報と Runner 名を確認し、DisplayName と一致するかを判定します。
        /// </summary>
        /// <param name="adapter">判定対象のアダプタ。</param>
        /// <param name="normalizedDisplayName">比較に用いる正規化済み DisplayName。</param>
        /// <returns>一致すると判断した場合は <c>true</c>。</returns>
        private static bool MatchesDisplayName(ActorPresenterAdapter adapter, string normalizedDisplayName)
        {
            if (adapter == null)
            {
                return false;
            }

            if (NameMatches(adapter.gameObject != null ? adapter.gameObject.name : null, normalizedDisplayName))
            {
                return true;
            }

            if (NameMatches(adapter.name, normalizedDisplayName))
            {
                return true;
            }

            var presenter = adapter.Presenter;
            if (presenter != null)
            {
                if (NameMatches(presenter.Runner != null ? presenter.Runner.name : null, normalizedDisplayName))
                {
                    return true;
                }

                var original = presenter.Original;
                if (original != null && NameMatches(original.Runner != null ? original.Runner.name : null, normalizedDisplayName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 名前文字列が DisplayName に相当すると判断できるかを判定します。
        /// </summary>
        /// <param name="source">比較対象の名前。</param>
        /// <param name="normalizedDisplayName">正規化済み DisplayName。</param>
        /// <returns>DisplayName に一致すると判断できる場合は <c>true</c>。</returns>
        private static bool NameMatches(string source, string normalizedDisplayName)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            if (string.Equals(source, normalizedDisplayName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (source.StartsWith(normalizedDisplayName + " ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (source.StartsWith(normalizedDisplayName + "(", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (source.EndsWith(" " + normalizedDisplayName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Flow 情報と任意のアダプタから <see cref="ActorPresenter"/> を解決する。
        /// </summary>
        /// <param name="flow">現在のフロー情報。null 許容。</param>
        /// <param name="adapter">優先的に使用する <see cref="ActorPresenterAdapter"/>。null 時は自動解決を試みる。</param>
        /// <returns>解決した <see cref="ActorPresenter"/>。取得できない場合は null。</returns>
        public static ActorPresenter ResolveActorPresenter(Flow flow, ActorPresenterAdapter adapter)
        {
            var resolvedAdapter = adapter ?? ResolveAdapter(flow);
            if (resolvedAdapter != null && resolvedAdapter.Presenter != null)
            {
                return resolvedAdapter.Presenter;
            }

            if (GetPresenter(flow) is ActorPresenter presenterFromVariables && presenterFromVariables != null)
            {
                return presenterFromVariables;
            }

            var bridge = VSPresenterBridge.Instance;
            if (bridge != null && bridge.Target != null)
            {
                return bridge.Target;
            }

            return null;
        }

        /// <summary>
        /// 差分ベクトルから現在のモードに適した向きの角度（度）を計算します。
        /// Scratch モード時は上=0°、それ以外は右=0° を返します。
        /// </summary>
        /// <param name="delta">対象までの差分ベクトル（論理座標系）。</param>
        /// <returns>現在のモード基準で正規化された角度（度）。</returns>
        public static float GetDirectionDegreesForCurrentMode(Vector2 delta)
        {
            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            var degreesFromRight = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (!FUnityModeUtil.IsScratchMode)
            {
                return NormalizeSignedAngle(degreesFromRight);
            }

            var degreesFromUp = 90f - degreesFromRight;
            return NormalizeSignedAngle(degreesFromUp);
        }

        /// <summary>
        /// 現在のモードに応じた角度（度）を UI 座標系（右=+X、下=+Y）の単位ベクトルへ変換します。
        /// Scratch モードでは上=0°、それ以外では右=0° を基準とします。
        /// </summary>
        /// <param name="degrees">変換する角度（度）。</param>
        /// <returns>UI 座標系における進行方向ベクトル。</returns>
        public static Vector2 DirFromDegrees(float degrees)
        {
            var normalized = NormalizeSignedAngle(degrees);
            var reference = FUnityModeUtil.IsScratchMode ? 90f - normalized : normalized;
            var rad = reference * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
        }

        /// <summary>
        /// Scratch の「〇歩」をピクセル量へ変換します。1 歩 = 1 px を固定とします。
        /// </summary>
        /// <param name="steps">変換する歩数。</param>
        /// <returns>ピクセルへ変換した移動量。</returns>
        public static float StepsToPixels(float steps)
        {
            return steps * StepToPixels;
        }

        /// <summary>
        /// 直近の解決結果を WeakReference に保存します。
        /// </summary>
        /// <param name="adapter">キャッシュ対象のアダプタ。</param>
        private static void CacheAdapter(ActorPresenterAdapter adapter)
        {
            if (adapter == null)
            {
                return;
            }

            if (m_CachedAdapter == null)
            {
                m_CachedAdapter = new WeakReference<ActorPresenterAdapter>(adapter);
            }
            else
            {
                m_CachedAdapter.SetTarget(adapter);
            }

            m_HasLoggedResolutionFailure = false;
        }

        /// <summary>
        /// 静的キャッシュからアダプタを取得します。
        /// </summary>
        /// <param name="adapter">取得したアダプタ。</param>
        /// <returns>取得に成功した場合 true。</returns>
        private static bool TryGetFromStaticCache(out ActorPresenterAdapter adapter)
        {
            adapter = null;
            if (m_CachedAdapter == null)
            {
                return false;
            }

            if (!m_CachedAdapter.TryGetTarget(out var target) || target == null)
            {
                return false;
            }

            adapter = target;
            return true;
        }

        /// <summary>
        /// Flow 情報から Object Variables（Graph をホストする GameObject 単位）を参照し、一般的なキーでアダプタを探します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter TryGetFromGraphVariables(Flow flow)
        {
            var objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            if (objectVariables.IsDefined("adapter"))
            {
                if (objectVariables.Get("adapter") is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            if (objectVariables.IsDefined(nameof(ActorPresenterAdapter)))
            {
                if (objectVariables.Get(nameof(ActorPresenterAdapter)) is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            return null;
        }

        /// <summary>
        /// Object 変数（GameObject 単位）からアダプタを取得します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter TryGetFromObjectVariables(Flow flow)
        {
            var objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            if (objectVariables.IsDefined("adapter"))
            {
                if (objectVariables.Get("adapter") is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            if (objectVariables.IsDefined(nameof(ActorPresenterAdapter)))
            {
                if (objectVariables.Get(nameof(ActorPresenterAdapter)) is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            return null;
        }

        /// <summary>
        /// Flow スタックが紐付く GameObject からアダプタを取得します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>取得したアダプタ。見つからない場合は null。</returns>
        private static ActorPresenterAdapter TryGetFromSelf(Flow flow)
        {
            var owner = flow?.stack?.gameObject;
            if (owner == null)
            {
                return null;
            }

            return owner.GetComponent<ActorPresenterAdapter>();
        }

        /// <summary>
        /// シーン全体から最初の <see cref="ActorPresenterAdapter"/> を検索します。
        /// </summary>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter FindAdapterInScene()
        {
            ActorPresenterAdapter found = null;
#if UNITY_6000_0_OR_NEWER
            found = UnityEngine.Object.FindFirstObjectByType<ActorPresenterAdapter>();
#else
            found = UnityEngine.Object.FindObjectOfType<ActorPresenterAdapter>();
#endif
            return found;
        }

        /// <summary>
        /// ステージ境界に到達するまでの移動量を計算し、最初に接触する法線を返します。
        /// </summary>
        /// <param name="center">現在の俳優中心座標（Scratch 論理座標系）。</param>
        /// <param name="directionPx">移動させたい UI 座標系の差分（px）。</param>
        /// <param name="rootScaledSize">#root のスケール適用後サイズ（px）。</param>
        /// <param name="hitNormal">最初に接触する境界の外向き法線。</param>
        /// <returns>ステージ内で移動できる論理座標の差分。</returns>
        public static Vector2 ComputeTravelToStageEdge(Vector2 center, Vector2 directionPx, Vector2 rootScaledSize, out Vector2 hitNormal)
        {
            var logicalDelta = ToLogicalDelta(directionPx);
            hitNormal = Vector2.zero;

            if (logicalDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            ResolveLogicalExtents(rootScaledSize, out var minX, out var maxX, out var minY, out var maxY);

            var smallestT = float.PositiveInfinity;
            var normal = Vector2.zero;

            if (Mathf.Abs(logicalDelta.x) > Mathf.Epsilon)
            {
                var boundary = logicalDelta.x > 0f ? maxX : minX;
                var t = (boundary - center.x) / logicalDelta.x;
                if (t >= 0f && t <= 1f && t < smallestT)
                {
                    smallestT = t;
                    normal = logicalDelta.x > 0f ? Vector2.right : Vector2.left;
                }
            }

            if (Mathf.Abs(logicalDelta.y) > Mathf.Epsilon)
            {
                var boundary = logicalDelta.y > 0f ? maxY : minY;
                var t = (boundary - center.y) / logicalDelta.y;
                if (t >= 0f && t <= 1f && t < smallestT)
                {
                    smallestT = t;
                    normal = logicalDelta.y > 0f ? Vector2.up : Vector2.down;
                }
            }

            if (float.IsPositiveInfinity(smallestT))
            {
                return logicalDelta;
            }

            if (smallestT > 1f)
            {
                return logicalDelta;
            }

            hitNormal = normal;
            var clampedT = Mathf.Max(0f, smallestT);
            return logicalDelta * clampedT;
        }

        /// <summary>
        /// ステージ境界で反射させつつ俳優をステージ内へ押し戻し、新しい進行方向を返します。
        /// </summary>
        /// <param name="center">境界に接触した後の俳優中心座標（Scratch 論理座標系）。</param>
        /// <param name="dirPx">直前の進行方向（UI 座標系）。</param>
        /// <param name="rootScaledSize">#root のスケール適用後サイズ（px）。</param>
        /// <param name="clampedCenter">クランプ後の中心座標（Scratch 論理座標系）。</param>
        /// <returns>反射後の進行方向（UI 座標系）。</returns>
        public static Vector2 BounceDirectionAndClamp(Vector2 center, Vector2 dirPx, Vector2 rootScaledSize, out Vector2 clampedCenter)
        {
            ResolveLogicalExtents(rootScaledSize, out var minX, out var maxX, out var minY, out var maxY);
            clampedCenter = ClampCenter(center, minX, maxX, minY, maxY);

            if (!ScratchHitTestUtil.IsTouchingStageEdge(center, rootScaledSize, out var hitNormal) || hitNormal.sqrMagnitude <= Mathf.Epsilon)
            {
                return NormalizeUiDirection(dirPx);
            }

            const float pushEpsilon = 0.5f;

            if (hitNormal.x > 0f && maxX > minX)
            {
                clampedCenter.x = Mathf.Min(clampedCenter.x, Mathf.Max(minX, maxX - pushEpsilon));
            }
            else if (hitNormal.x < 0f && maxX > minX)
            {
                clampedCenter.x = Mathf.Max(clampedCenter.x, Mathf.Min(maxX, minX + pushEpsilon));
            }

            if (hitNormal.y > 0f && maxY > minY)
            {
                clampedCenter.y = Mathf.Min(clampedCenter.y, Mathf.Max(minY, maxY - pushEpsilon));
            }
            else if (hitNormal.y < 0f && maxY > minY)
            {
                clampedCenter.y = Mathf.Max(clampedCenter.y, Mathf.Min(maxY, minY + pushEpsilon));
            }

            var logicalDir = ToLogicalDelta(dirPx);
            if (logicalDir.sqrMagnitude <= Mathf.Epsilon)
            {
                logicalDir = Vector2.up;
            }
            else
            {
                logicalDir.Normalize();
            }

            var normal = hitNormal.normalized;
            var reflectedLogical = Vector2.Reflect(logicalDir, normal);
            if (reflectedLogical.sqrMagnitude <= Mathf.Epsilon)
            {
                reflectedLogical = -logicalDir;
            }

            var reflectedUi = ToUiDelta(reflectedLogical);
            return NormalizeUiDirection(reflectedUi);
        }

        /// <summary>
        /// UI 座標系の方向ベクトルから現在のモードに応じた方位（度）を計算します。
        /// Scratch モードでは上=0°、それ以外では右=0° を返します。
        /// </summary>
        /// <param name="dirPx">UI 座標系の方向ベクトル。</param>
        /// <returns>現在のモード基準での角度（度）。</returns>
        public static float DegreesFromUiDirection(Vector2 dirPx)
        {
            if (dirPx.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            var logicalDelta = ToLogicalDelta(dirPx);
            return GetDirectionDegreesForCurrentMode(logicalDelta);
        }

        /// <summary>
        /// 角度を -180° ～ 180° の範囲へ正規化します。Scratch 互換の方向比較や UI 回転計算で一貫した値を得るために利用します。
        /// </summary>
        /// <param name="degrees">正規化対象の角度（度）。</param>
        /// <returns>-180° ～ 180° に収めた角度（度）。</returns>
        private static float NormalizeSignedAngle(float degrees)
        {
            var normalized = Mathf.Repeat(degrees + 180f, 360f) - 180f;
            if (Mathf.Approximately(normalized, -180f) && degrees > 0f)
            {
                return 180f;
            }

            return normalized;
        }

        /// <summary>
        /// UI 座標系の方向ベクトルを正規化し、ゼロベクトルの場合は上方向（0,-1）を返します。
        /// </summary>
        /// <param name="direction">正規化対象のベクトル。</param>
        /// <returns>正規化後の UI 座標系ベクトル。</returns>
        private static Vector2 NormalizeUiDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return new Vector2(0f, -1f);
            }

            return direction.normalized;
        }

        /// <summary>
        /// UI 座標系の差分を Scratch 論理座標系の差分へ変換します。
        /// </summary>
        /// <param name="uiDelta">UI 座標系の差分。</param>
        /// <returns>論理座標系の差分。</returns>
        private static Vector2 ToLogicalDelta(Vector2 uiDelta)
        {
            return new Vector2(uiDelta.x, -uiDelta.y);
        }

        /// <summary>
        /// Scratch 論理座標系の差分を UI 座標系へ変換します。
        /// </summary>
        /// <param name="logicalDelta">論理座標系での差分。</param>
        /// <returns>UI 座標系の差分。</returns>
        private static Vector2 ToUiDelta(Vector2 logicalDelta)
        {
            return new Vector2(logicalDelta.x, -logicalDelta.y);
        }

        /// <summary>
        /// 俳優サイズを考慮した中心座標の許容範囲を算出します。
        /// </summary>
        /// <param name="rootScaledSize">#root のスケール適用後サイズ（px）。</param>
        /// <param name="minX">中心 X 座標の最小値。</param>
        /// <param name="maxX">中心 X 座標の最大値。</param>
        /// <param name="minY">中心 Y 座標の最小値。</param>
        /// <param name="maxY">中心 Y 座標の最大値。</param>
        private static void ResolveLogicalExtents(Vector2 rootScaledSize, out float minX, out float maxX, out float minY, out float maxY)
        {
            var halfWidth = Mathf.Max(0f, rootScaledSize.x * 0.5f);
            var halfHeight = Mathf.Max(0f, rootScaledSize.y * 0.5f);

            minX = -ScratchBounds.StageHalfW + halfWidth;
            maxX = ScratchBounds.StageHalfW - halfWidth;
            minY = -ScratchBounds.StageHalfH + halfHeight;
            maxY = ScratchBounds.StageHalfH - halfHeight;

            if (minX > maxX)
            {
                minX = 0f;
                maxX = 0f;
            }

            if (minY > maxY)
            {
                minY = 0f;
                maxY = 0f;
            }
        }

        /// <summary>
        /// 許容範囲へ中心座標をクランプします。
        /// </summary>
        /// <param name="center">クランプ前の中心座標。</param>
        /// <param name="minX">許容最小 X。</param>
        /// <param name="maxX">許容最大 X。</param>
        /// <param name="minY">許容最小 Y。</param>
        /// <param name="maxY">許容最大 Y。</param>
        /// <returns>範囲内へ収めた中心座標。</returns>
        private static Vector2 ClampCenter(Vector2 center, float minX, float maxX, float minY, float maxY)
        {
            var clampedX = Mathf.Clamp(center.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
            var clampedY = Mathf.Clamp(center.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
            return new Vector2(clampedX, clampedY);
        }

        /// <summary>
        /// 解決失敗時に一度だけ警告を表示します。
        /// </summary>
        private static void LogResolutionFailureOnce()
        {
            if (m_HasLoggedResolutionFailure)
            {
                return;
            }

            Debug.LogWarning("[FUnity] ActorPresenterAdapter を自動解決できませんでした。ScriptGraphAsset の Variables もしくは対象 GameObject の Object/Graph Variables に adapter を設定してください。");
            m_HasLoggedResolutionFailure = true;
        }
    }
}
