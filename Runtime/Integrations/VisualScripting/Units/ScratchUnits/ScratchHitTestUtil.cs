using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.View;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch 互換ユニットのために、俳優矩形の推定やステージ境界判定を提供する当たり判定ユーティリティです。
    /// </summary>
    internal static class ScratchHitTestUtil
    {
        /// <summary>ステージ矩形を取得できなかった場合に利用する既定サイズ（px）。</summary>
        private static readonly Vector2 s_FallbackStageSize = new Vector2(480f, 360f);

        /// <summary>
        /// アダプタに紐づく俳優の worldBound を推定し、矩形情報を返します。
        /// </summary>
        /// <param name="adapter">対象の <see cref="ActorPresenterAdapter"/>。</param>
        /// <param name="rect">推定された世界座標矩形。</param>
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
                var centerUi = presenter.ToUiPosition(logical);
                var sizePx = view.GetRootScaledSizePx();
                if (sizePx.x > 0f && sizePx.y > 0f)
                {
                    rect = new Rect(
                        centerUi.x - sizePx.x * 0.5f,
                        centerUi.y - sizePx.y * 0.5f,
                        sizePx.x,
                        sizePx.y);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ステージ境界の worldBound を取得し、0,0 原点での Fallback を含めた矩形を返します。
        /// </summary>
        /// <param name="adapter">ステージ参照の基準となるアダプタ。</param>
        /// <returns>取得したステージ矩形。情報が無い場合は 480x360 を返します。</returns>
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
        /// panel 座標系でのマウスポインター位置を返します。panel が無い場合はスクリーン座標を返します。
        /// </summary>
        /// <param name="adapter">座標変換の基準となるアダプタ。</param>
        /// <returns>panel 基準の座標。fallback 時はスクリーン座標。</returns>
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
        /// 中心原点（0,0）基準で、俳優中心がステージ境界内に収まっているか判定します。
        /// </summary>
        /// <param name="center">俳優中心座標（Scratch 論理座標系）。</param>
        /// <returns>ステージ範囲内であれば <c>true</c>。</returns>
        public static bool IsCenterInsideStage(Vector2 center)
        {
            return center.x >= -ScratchBounds.StageHalfW
                && center.x <= ScratchBounds.StageHalfW
                && center.y >= -ScratchBounds.StageHalfH
                && center.y <= ScratchBounds.StageHalfH;
        }

        /// <summary>
        /// 俳優の中心とサイズを考慮し、ステージ境界へ接触または越境しているかを判定します。
        /// </summary>
        /// <param name="center">俳優中心座標（Scratch 論理座標系）。</param>
        /// <param name="rootScaledSize">#root のスケール適用後サイズ（px）。</param>
        /// <param name="hitNormal">接触境界の外向き法線。未接触の場合は <see cref="Vector2.zero"/>。</param>
        /// <returns>境界へ接触していれば <c>true</c>。</returns>
        public static bool IsTouchingStageEdge(Vector2 center, Vector2 rootScaledSize, out Vector2 hitNormal)
        {
            ResolveLogicalExtents(rootScaledSize, out var minX, out var maxX, out var minY, out var maxY);

            const float edgeEpsilon = 0.5f;
            var normal = Vector2.zero;
            var touching = false;

            var leftPenetration = minX - center.x;
            if (leftPenetration >= -edgeEpsilon)
            {
                normal.x -= 1f;
                touching = true;
            }

            var rightPenetration = center.x - maxX;
            if (rightPenetration >= -edgeEpsilon)
            {
                normal.x += 1f;
                touching = true;
            }

            var bottomPenetration = minY - center.y;
            if (bottomPenetration >= -edgeEpsilon)
            {
                normal.y -= 1f;
                touching = true;
            }

            var topPenetration = center.y - maxY;
            if (topPenetration >= -edgeEpsilon)
            {
                normal.y += 1f;
                touching = true;
            }

            if (touching && normal.sqrMagnitude > Mathf.Epsilon)
            {
                hitNormal = normal.normalized;
                return true;
            }

            hitNormal = Vector2.zero;
            return false;
        }

        /// <summary>俳優のルート要素を決定し、矩形推定の基準を返します。</summary>
        /// <param name="adapter">参照するアダプタ。</param>
        /// <param name="view">参照する ActorView。</param>
        /// <returns>矩形推定の基準となる VisualElement。</returns>
        private static VisualElement ResolveActorRoot(ActorPresenterAdapter adapter, ActorView view)
        {
            if (view != null)
            {
                return view.ActorRoot ?? view.BoundElement;
            }

            return adapter != null ? adapter.BoundElement : null;
        }

        /// <summary>スプライト描画要素を探索し、矩形推定に利用します。</summary>
        /// <param name="root">探索を開始する要素。</param>
        /// <returns>見つかったスプライト描画要素。</returns>
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

        /// <summary>指定要素の worldBound を取得し、有効サイズかを検査します。</summary>
        /// <param name="element">検査対象の要素。</param>
        /// <param name="rect">取得した矩形。</param>
        /// <returns>正の幅・高さを持てば <c>true</c>。</returns>
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
        /// ステージ境界と俳優サイズから、中心座標の許容範囲（最小・最大）を計算します。
        /// </summary>
        /// <param name="rootScaledSize">俳優のスケール適用後サイズ（px）。</param>
        /// <param name="minX">中心 X 座標の許容最小値。</param>
        /// <param name="maxX">中心 X 座標の許容最大値。</param>
        /// <param name="minY">中心 Y 座標の許容最小値。</param>
        /// <param name="maxY">中心 Y 座標の許容最大値。</param>
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
    }
}
