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

    /// <summary>
    /// Scratch の「◯◯に触れた？」ブロックを再現し、DisplayName で指定した俳優との矩形重なりを判定する Unit です。
    /// </summary>
    [UnitTitle("Touching Actor By DisplayName?")]
    [UnitCategory("Scratch/Sensing")]
    public sealed class TouchingActorByDisplayNamePredicateUnit : Unit
    {
        /// <summary>対象俳優の DisplayName を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DisplayName;

        /// <summary>接触判定結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>DisplayName 入力ポートを公開します。</summary>
        public ValueInput DisplayName => m_DisplayName;

        /// <summary>判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 入出力ポートを構築し、DisplayName の取得を必須とする依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_DisplayName = ValueInput<string>(nameof(DisplayName), string.Empty);
            m_Result = ValueOutput<bool>(nameof(Result), Evaluate);

            Requirement(m_DisplayName, m_Result);
        }

        /// <summary>
        /// 指定された俳優と自身の俳優が矩形的に接触しているかを判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>接触している場合は true。</returns>
        private bool Evaluate(Flow flow)
        {
            var selfAdapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (selfAdapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Touching Actor By DisplayName?: ActorPresenterAdapter が未解決のため判定できません。");
                return false;
            }

            var displayName = flow.GetValue<string>(m_DisplayName);
            if (string.IsNullOrEmpty(displayName))
            {
                return false;
            }

            if (!ScratchHitTestUtil.TryGetActorWorldRect(selfAdapter, out var selfRect))
            {
                return false;
            }

            if (!ScratchUnitUtil.TryFindActorByDisplayName(displayName, out var targetAdapter) || targetAdapter == null)
            {
                Debug.LogWarning($"[FUnity] Scratch/Touching Actor By DisplayName?: '{displayName}' に一致する俳優が見つかりません。");
                return false;
            }

            if (!ScratchHitTestUtil.TryGetActorWorldRect(targetAdapter, out var targetRect))
            {
                return false;
            }

            const float inflate = 0.001f;
            selfRect.xMin -= inflate;
            selfRect.yMin -= inflate;
            selfRect.xMax += inflate;
            selfRect.yMax += inflate;
            targetRect.xMin -= inflate;
            targetRect.yMin -= inflate;
            targetRect.xMax += inflate;
            targetRect.yMax += inflate;

            return selfRect.Overlaps(targetRect, true);
        }
    }
}
