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
    internal static class ScratchUnitUtil
    {
        /// <summary>直近で解決したアダプタを保持する WeakReference です。破棄後は null になります。</summary>
        private static WeakReference<ActorPresenterAdapter> m_CachedAdapter;

        /// <summary>解決失敗時の警告を重複表示しないためのフラグです。</summary>
        private static bool m_HasLoggedResolutionFailure;

        /// <summary>Scratch の 1 歩をピクセルへ換算する倍率です。</summary>
        private const float StepToPixels = 1f;

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
        /// Scratch 方位（0°=上、90°=右、180°=下、-90°=左）を UI 座標系（右=+X、下=+Y）へ変換した単位ベクトルを返します。
        /// </summary>
        /// <param name="degrees">変換する角度（度）。</param>
        /// <returns>UI 座標系における進行方向ベクトル。</returns>
        public static Vector2 DirFromDegrees(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));
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
        /// UI 座標系の方向ベクトルから Scratch の方位（度）を計算します。
        /// </summary>
        /// <param name="dirPx">UI 座標系の方向ベクトル。</param>
        /// <returns>Scratch 方位の角度（度）。</returns>
        public static float DegreesFromUiDirection(Vector2 dirPx)
        {
            if (dirPx.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            var normalized = dirPx.normalized;
            var radians = Mathf.Atan2(normalized.x, -normalized.y);
            var degrees = radians * Mathf.Rad2Deg;
            return degrees;
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
