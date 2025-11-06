// Updated: 2025-10-19
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.View;
using Object = UnityEngine.Object;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch 系 Unit 共通の補助処理を提供し、Presenter アダプタの自動解決や方向ベクトル計算を行うユーティリティクラスです。
    /// </summary>
    internal static partial class ScratchUnitUtil
    {
        /// <summary>直近で解決したアダプタを保持する WeakReference です。破棄後は null になります。</summary>
        private static WeakReference<ActorPresenterAdapter> m_CachedAdapter;

        /// <summary>解決失敗時の警告を重複表示しないためのフラグです。</summary>
        private static bool m_HasLoggedResolutionFailure;

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

            return Variables.Object(host);
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
        /// マウスポインターのスクリーン座標から Scratch 論理座標を推定します。
        /// </summary>
        /// <param name="referenceAdapter">ステージ座標変換に使用する参照アダプタ。</param>
        /// <returns>推定した論理座標。失敗時はスクリーン座標から簡易変換した値。</returns>
        public static Vector2 GetMouseLogicalPosition(ActorPresenterAdapter referenceAdapter)
        {
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
        /// Scratch 方位（0°=右、90°=上、180°=左、270°=下）を UI 座標系（右=+X、下=+Y）へ変換した単位ベクトルを返します。
        /// </summary>
        /// <param name="degrees">変換する角度（度）。</param>
        /// <returns>UI 座標系における進行方向ベクトル。</returns>
        public static Vector2 DirFromDegrees(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
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

    /// <summary>
    /// Scratch 互換の当たり判定で利用する矩形・座標計算を提供する補助クラスです。
    /// </summary>
    internal static class ScratchHitTestUtil
    {
        /// <summary>Scratch ステージの既定サイズ（px）。座標取得に失敗した際のフォールバックとして利用します。</summary>
        private static readonly Vector2 s_FallbackStageSize = new Vector2(480f, 360f);

        /// <summary>
        /// ActorPresenterAdapter から俳優要素の世界座標矩形を取得する。worldBound が無効な場合は子要素や中心座標から補完する。
        /// </summary>
        /// <param name="adapter">対象となるアクターのアダプタ。</param>
        /// <param name="rect">推定した世界座標矩形。失敗時は default。</param>
        /// <returns>矩形の推定に成功した場合は <c>true</c>。</returns>
        public static bool TryGetActorWorldRect(ActorPresenterAdapter adapter, out Rect rect)
        {
            rect = default;
            if (adapter == null)
            {
                return false;
            }

            var view = adapter.ActorView;
            if (view != null && view.TryGetCachedWorldBound(out rect))
            {
                return true;
            }

            var root = ResolveActorRoot(adapter, view);
            var sprite = ResolveSpriteElement(root);
            if (TryResolveWorldBound(sprite, out rect))
            {
                return true;
            }

            if (TryResolveWorldBound(root, out rect))
            {
                return true;
            }

            var fallbackElement = view != null ? view.BoundElement : adapter.BoundElement;
            if (fallbackElement != null && fallbackElement != root && TryResolveWorldBound(fallbackElement, out rect))
            {
                return true;
            }

            var presenter = adapter.Presenter;
            if (presenter != null && view != null)
            {
                var logical = presenter.GetPosition();
                var center = presenter.ToUiPosition(logical);
                var sizePx = view.GetRootScaledSizePx();
                if (sizePx.x > 0f && sizePx.y > 0f)
                {
                    rect = new Rect(
                        center.x - sizePx.x * 0.5f,
                        center.y - sizePx.y * 0.5f,
                        sizePx.x,
                        sizePx.y);
                    return true;
                }
            }

            return false;
        }

        /// <summary>ActorView とアダプタから最も信頼できるルート要素を解決する。</summary>
        /// <param name="adapter">参照するアダプタ。</param>
        /// <param name="view">参照する ActorView。</param>
        /// <returns>矩形計算の基準とする要素。</returns>
        private static VisualElement ResolveActorRoot(ActorPresenterAdapter adapter, ActorView view)
        {
            if (view != null)
            {
                return view.ActorRoot ?? view.BoundElement;
            }

            return adapter != null ? adapter.BoundElement : null;
        }

        /// <summary>スプライトを描画している子要素を探索する。</summary>
        /// <param name="root">探索の起点。</param>
        /// <returns>スプライト描画要素。見つからない場合は null。</returns>
        private static VisualElement ResolveSpriteElement(VisualElement root)
        {
            if (root == null)
            {
                return null;
            }

            return root.Q<VisualElement>("sprite")
                ?? root.Q<VisualElement>(className: "sprite")
                ?? root.Q<VisualElement>(className: "actor-sprite")
                ?? root.Q<VisualElement>("portrait")
                ?? root.Q<VisualElement>(className: "portrait");
        }

        /// <summary>VisualElement の worldBound を取得し、正の幅・高さを持つかを検査する。</summary>
        /// <param name="element">検査対象の要素。</param>
        /// <param name="rect">取得した矩形。</param>
        /// <returns>幅・高さが正の場合は <c>true</c>。</returns>
        private static bool TryResolveWorldBound(VisualElement element, out Rect rect)
        {
            rect = default;
            if (element == null)
            {
                return false;
            }

            rect = element.worldBound;
            return rect.width > 0f && rect.height > 0f;
        }

        /// <summary>
        /// UI Toolkit の panel 座標系におけるマウスポインター位置を取得します。
        /// </summary>
        /// <param name="adapter">座標変換に利用するアクターのアダプタ。</param>
        /// <returns>panel 基準の座標。変換に失敗した場合はスクリーン座標を返します。</returns>
        public static Vector2 GetMousePanelPosition(ActorPresenterAdapter adapter)
        {
            var pointer = UnityEngine.Input.mousePosition;

            var referenceElement = adapter != null ? adapter.BoundElement : null;
            if (referenceElement == null && adapter != null && adapter.Presenter != null)
            {
                referenceElement = adapter.Presenter.StageRootElement;
            }

            if (referenceElement != null)
            {
                var panel = referenceElement.panel;
                if (panel != null)
                {
                    return RuntimePanelUtils.ScreenToPanel(panel, new Vector2(pointer.x, pointer.y));
                }
            }

            return new Vector2(pointer.x, pointer.y);
        }

        /// <summary>
        /// ステージ要素の worldBound を取得し、Fallback を含めた矩形を返します。
        /// </summary>
        /// <param name="adapter">ステージ参照の基準となるアダプタ。</param>
        /// <returns>ステージ領域の矩形。取得できない場合は (0,0,480,360) を返します。</returns>
        public static Rect GetStageWorldRect(ActorPresenterAdapter adapter)
        {
            var presenter = adapter != null ? adapter.Presenter : null;
            var stageRoot = presenter != null ? presenter.StageRootElement : null;
            if (stageRoot != null)
            {
                var rect = stageRoot.worldBound;
                if (rect.width > 0f && rect.height > 0f)
                {
                    return rect;
                }
            }

            return new Rect(0f, 0f, s_FallbackStageSize.x, s_FallbackStageSize.y);
        }

        /// <summary>
        /// ステージ矩形と俳優の半サイズから、中心座標が取り得る最小値・最大値を算出します。
        /// ステージより俳優が大きい場合は、ステージ中心へ収束させる安全値を返します。
        /// </summary>
        /// <param name="stageRect">判定に利用するステージの矩形（worldBound）。</param>
        /// <param name="halfSize">俳優の見た目半サイズ（px）。</param>
        /// <param name="minX">中心座標の許容最小値（world）。</param>
        /// <param name="maxX">中心座標の許容最大値（world）。</param>
        /// <param name="minY">中心座標の許容最小値（world）。</param>
        /// <param name="maxY">中心座標の許容最大値（world）。</param>
        private static void ResolveStageExtents(Rect stageRect, Vector2 halfSize, out float minX, out float maxX, out float minY, out float maxY)
        {
            var safeHalf = new Vector2(Mathf.Max(0f, halfSize.x), Mathf.Max(0f, halfSize.y));

            minX = stageRect.xMin + safeHalf.x;
            maxX = stageRect.xMax - safeHalf.x;
            minY = stageRect.yMin + safeHalf.y;
            maxY = stageRect.yMax - safeHalf.y;

            if (maxX < minX)
            {
                var mid = (stageRect.xMin + stageRect.xMax) * 0.5f;
                minX = mid;
                maxX = mid;
            }

            if (maxY < minY)
            {
                var mid = (stageRect.yMin + stageRect.yMax) * 0.5f;
                minY = mid;
                maxY = mid;
            }
        }

        /// <summary>
        /// 指定した中心座標がステージ範囲内に収まっているかを判定します。
        /// </summary>
        /// <param name="centerWorld">俳優中心座標（world）。</param>
        /// <param name="stageRect">ステージ矩形（worldBound）。</param>
        /// <param name="halfSize">俳優の見た目半サイズ（px）。</param>
        /// <returns>ステージ内に収まっている場合は <c>true</c>。</returns>
        public static bool IsCenterInsideStage(Vector2 centerWorld, Rect stageRect, Vector2 halfSize)
        {
            ResolveStageExtents(stageRect, halfSize, out var minX, out var maxX, out var minY, out var maxY);
            return centerWorld.x >= minX && centerWorld.x <= maxX && centerWorld.y >= minY && centerWorld.y <= maxY;
        }

        /// <summary>
        /// 中心座標が境界へ接触または越境しているかを判定します。
        /// </summary>
        /// <param name="centerWorld">俳優中心座標（world）。</param>
        /// <param name="stageRect">ステージ矩形（worldBound）。</param>
        /// <param name="halfSize">俳優の見た目半サイズ（px）。</param>
        /// <param name="epsilon">接触許容誤差（px）。</param>
        /// <returns>接触または越境している場合は <c>true</c>。</returns>
        public static bool IsTouchingStageEdge(Vector2 centerWorld, Rect stageRect, Vector2 halfSize, float epsilon)
        {
            ResolveStageExtents(stageRect, halfSize, out var minX, out var maxX, out var minY, out var maxY);

            if (centerWorld.x < minX || centerWorld.x > maxX || centerWorld.y < minY || centerWorld.y > maxY)
            {
                return true;
            }

            var safeEps = Mathf.Max(0f, epsilon);
            return (centerWorld.x - minX) <= safeEps
                || (maxX - centerWorld.x) <= safeEps
                || (centerWorld.y - minY) <= safeEps
                || (maxY - centerWorld.y) <= safeEps;
        }

        /// <summary>
        /// 現在位置から最初に到達する境界までの移動距離を算出します。
        /// </summary>
        /// <param name="centerWorld">俳優中心座標（world）。</param>
        /// <param name="directionUi">移動方向ベクトル（UI 座標系, 正規化済み想定）。</param>
        /// <param name="stageRect">ステージ矩形（worldBound）。</param>
        /// <param name="halfSize">俳優の見た目半サイズ（px）。</param>
        /// <returns>境界に衝突するまでの距離（px）。移動方向がゼロの場合は 0。</returns>
        public static float ComputeTravelToStageEdge(Vector2 centerWorld, Vector2 directionUi, Rect stageRect, Vector2 halfSize)
        {
            ResolveStageExtents(stageRect, halfSize, out var minX, out var maxX, out var minY, out var maxY);

            var travel = float.PositiveInfinity;
            var dirX = directionUi.x;
            var dirY = directionUi.y;

            if (Mathf.Abs(dirX) > Mathf.Epsilon)
            {
                var boundaryX = dirX > 0f ? maxX : minX;
                var distance = (boundaryX - centerWorld.x) / dirX;
                if (distance >= 0f)
                {
                    travel = Mathf.Min(travel, distance);
                }
            }

            if (Mathf.Abs(dirY) > Mathf.Epsilon)
            {
                var boundaryY = dirY > 0f ? maxY : minY;
                var distance = (boundaryY - centerWorld.y) / dirY;
                if (distance >= 0f)
                {
                    travel = Mathf.Min(travel, distance);
                }
            }

            if (float.IsPositiveInfinity(travel) || travel < 0f)
            {
                return 0f;
            }

            return Mathf.Max(0f, travel);
        }

        /// <summary>
        /// 境界へ衝突した際に進行方向を反射させ、ステージ内へ押し戻します。
        /// </summary>
        /// <param name="centerWorld">俳優中心座標（world）。結果は更新されます。</param>
        /// <param name="directionUi">移動方向ベクトル（UI 座標系）。結果は反射後の方向で上書きします。</param>
        /// <param name="stageRect">ステージ矩形（worldBound）。</param>
        /// <param name="halfSize">俳優の見た目半サイズ（px）。</param>
        /// <param name="epsilon">押し戻しに利用する余白（px）。</param>
        /// <returns>反射を行った場合は <c>true</c>。</returns>
        public static bool BounceDirectionAndClamp(ref Vector2 centerWorld, ref Vector2 directionUi, Rect stageRect, Vector2 halfSize, float epsilon)
        {
            ResolveStageExtents(stageRect, halfSize, out var minX, out var maxX, out var minY, out var maxY);

            var bounced = false;
            var push = Mathf.Max(0f, epsilon);

            if (centerWorld.x <= minX)
            {
                var adjusted = minX + push;
                centerWorld.x = maxX >= minX ? Mathf.Min(adjusted, maxX) : minX;
                directionUi.x = -directionUi.x;
                bounced = true;
            }
            else if (centerWorld.x >= maxX)
            {
                var adjusted = maxX - push;
                centerWorld.x = maxX >= minX ? Mathf.Max(adjusted, minX) : maxX;
                directionUi.x = -directionUi.x;
                bounced = true;
            }

            if (centerWorld.y <= minY)
            {
                var adjusted = minY + push;
                centerWorld.y = maxY >= minY ? Mathf.Min(adjusted, maxY) : minY;
                directionUi.y = -directionUi.y;
                bounced = true;
            }
            else if (centerWorld.y >= maxY)
            {
                var adjusted = maxY - push;
                centerWorld.y = maxY >= minY ? Mathf.Max(adjusted, minY) : maxY;
                directionUi.y = -directionUi.y;
                bounced = true;
            }

            if (!bounced)
            {
                return false;
            }

            if (directionUi.sqrMagnitude <= Mathf.Epsilon)
            {
                directionUi = Vector2.right;
            }
            else
            {
                directionUi.Normalize();
            }

            return true;
        }

        /// <summary>
        /// UI 座標系の方向ベクトルから Scratch 方位の角度を求めます。
        /// </summary>
        /// <param name="uiDirection">UI 座標系の方向ベクトル。</param>
        /// <returns>Scratch 方位の角度（度）。</returns>
        public static float DegreesFromUiDirection(Vector2 uiDirection)
        {
            if (uiDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return 90f;
            }

            var normalized = uiDirection.normalized;
            var radians = Mathf.Atan2(-normalized.y, normalized.x);
            var degrees = radians * Mathf.Rad2Deg;
            if (degrees < 0f)
            {
                degrees += 360f;
            }

            return degrees;
        }
    }
}
