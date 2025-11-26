using Unity.VisualScripting;
using UnityEngine;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.View;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「マウスポインターに触れた？」ブロックを再現し、俳優矩形にマウス座標が含まれるかを返す Unit です。
    /// </summary>
    [UnitTitle("マウスポインターに触れた？")]
    [UnitCategory("FUnity/Blocks/調べる")]
    [UnitSubtitle("調べる")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class TouchingMousePointerPredicateUnit : Unit
    {
        /// <summary>判定結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 出力ポートを構築し、フロー評価時に当たり判定を実行する設定を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Result = ValueOutput<bool>(nameof(Result), Evaluate);
        }

        /// <summary>
        /// マウスポインターと俳優矩形の接触判定を行います。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>接触している場合は true。</returns>
        private bool Evaluate(Flow flow)
        {
            var provider = ScratchUnitUtil.ResolveMouseProvider();
            if (provider == null)
            {
                return false;
            }

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Touching Mouse Pointer?: ActorPresenterAdapter が未解決のため判定できません。");
                return false;
            }

            var presenter = adapter.Presenter;
            if (presenter == null || !presenter.IsVisible)
            {
                return false;
            }

            if (!ScratchUnitUtil.TryGetActorHalfSizeLogical(adapter, out var halfSize))
            {
                return false;
            }

            var center = ScratchUnitUtil.ClampToStageBounds(presenter.GetPosition());
            var mouse = ScratchUnitUtil.ClampToStageBounds(provider.StagePosition);
            var min = center - halfSize;
            var max = center + halfSize;

            return mouse.x >= min.x && mouse.x <= max.x && mouse.y >= min.y && mouse.y <= max.y;
        }
    }

    /// <summary>
    /// Scratch の「端に触れた？」ブロックを再現し、俳優矩形がステージ境界へ接触しているかを返す Unit です。
    /// </summary>
    [UnitTitle("端に触れた？")]
    [UnitCategory("FUnity/Blocks/調べる")]
    [UnitSubtitle("調べる")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class TouchingEdgePredicateUnit : Unit
    {
        /// <summary>判定結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 出力ポートを構築し、評価時にステージ境界との接触を確認します。
        /// </summary>
        protected override void Definition()
        {
            m_Result = ValueOutput<bool>(nameof(Result), Evaluate);
        }

        /// <summary>
        /// ステージ矩形と俳優矩形の境界接触を判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>接触している場合は true。</returns>
        private bool Evaluate(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Touching Edge?: ActorPresenterAdapter が未解決のため判定できません。");
                return false;
            }

            if (!ScratchHitTestUtil.TryGetActorWorldRect(adapter, out var actorRect))
            {
                return false;
            }

            var stageRect = ScratchHitTestUtil.GetStageWorldRect(adapter);
            const float epsilon = 0.001f;

            var touchesLeft = actorRect.xMin <= stageRect.xMin + epsilon;
            var touchesRight = actorRect.xMax >= stageRect.xMax - epsilon;
            var touchesTop = actorRect.yMin <= stageRect.yMin + epsilon;
            var touchesBottom = actorRect.yMax >= stageRect.yMax - epsilon;

            return touchesLeft || touchesRight || touchesTop || touchesBottom;
        }
    }

    /// <summary>
    /// 背景色との接触を判定する静的ヘルパー群です。
    /// </summary>
    internal static class TouchPredicates
    {
        /// <summary>背景サンプリング用のグリッド分割数。</summary>
        private const int SampleGrid = 3;

        /// <summary>
        /// 俳優矩形内の複数点をサンプリングし、背景色が目標色に近似しているかを判定します。
        /// </summary>
        /// <param name="presenter">判定対象の俳優 Presenter。</param>
        /// <param name="targetColor">接触判定したい色。</param>
        /// <param name="tolerance">許容する色差（RGB ユークリッド距離）。</param>
        /// <returns>背景色が近似している点が存在すれば true。</returns>
        public static bool IsTouchingBackgroundColor(ActorPresenter presenter, Color targetColor, float tolerance)
        {
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] TouchPredicates: presenter が null のため背景色判定を行えません。");
                return false;
            }

            if (!presenter.IsVisible)
            {
                return false;
            }

            var view = presenter.ActorViewComponent;
            if (view == null)
            {
                Debug.LogWarning("[FUnity] TouchPredicates: ActorView が未設定のため背景色判定を行えません。");
                return false;
            }

            var backgroundService = presenter.StageBackgroundService;
            if (backgroundService == null)
            {
                Debug.LogWarning("[FUnity] TouchPredicates: StageBackgroundService が未設定のため背景色を取得できません。");
                return false;
            }

            var root = view.RootElement;
            if (root == null)
            {
                return false;
            }

            var worldRect = root.worldBound;
            if (worldRect.width <= 0f || worldRect.height <= 0f)
            {
                return false;
            }

            var clampedTolerance = Mathf.Max(0f, tolerance);

            for (var iy = 0; iy < SampleGrid; iy++)
            {
                for (var ix = 0; ix < SampleGrid; ix++)
                {
                    var tx = (ix + 0.5f) / SampleGrid;
                    var ty = (iy + 0.5f) / SampleGrid;

                    var worldPos = new Vector2(
                        worldRect.xMin + worldRect.width * tx,
                        worldRect.yMin + worldRect.height * ty);

                    if (!backgroundService.TryGetBackgroundColorAtWorldPosition(worldPos, out var sampledColor))
                    {
                        continue;
                    }

                    if (IsNearColor(sampledColor, targetColor, clampedTolerance))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 2 色間の距離が許容範囲内かを判定します。
        /// </summary>
        /// <param name="a">比較する色 A。</param>
        /// <param name="b">比較する色 B。</param>
        /// <param name="tolerance">許容する距離。</param>
        /// <returns>距離が閾値以下なら true。</returns>
        private static bool IsNearColor(in Color a, in Color b, float tolerance)
        {
            var dr = a.r - b.r;
            var dg = a.g - b.g;
            var db = a.b - b.b;
            var dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);
            return dist <= tolerance;
        }
    }

}
