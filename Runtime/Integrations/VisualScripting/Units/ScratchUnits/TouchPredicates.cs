using Unity.VisualScripting;
using UnityEngine;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「マウスポインターに触れた？」ブロックを再現し、俳優矩形にマウス座標が含まれるかを返す Unit です。
    /// </summary>
    [UnitTitle("Touching Mouse Pointer?")]
    [UnitCategory("Scratch/Sensing")]
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
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Touching Mouse Pointer?: ActorPresenterAdapter が未解決のため判定できません。");
                return false;
            }

            if (!ScratchHitTestUtil.TryGetActorWorldRect(adapter, out var actorRect))
            {
                return false;
            }

            var pointer = ScratchHitTestUtil.GetMousePanelPosition(adapter);
            return actorRect.Contains(pointer);
        }
    }

    /// <summary>
    /// Scratch の「端に触れた？」ブロックを再現し、俳優矩形がステージ境界へ接触しているかを返す Unit です。
    /// </summary>
    [UnitTitle("Touching Edge?")]
    [UnitCategory("Scratch/Sensing")]
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

}
