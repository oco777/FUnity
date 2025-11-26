using Unity.VisualScripting;
using UnityEngine;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇色に触れた？」ブロックを再現し、背景色との接触を判定する Unit です。
    /// 俳優以外の要素は対象とせず、ステージ背景（単色・画像）のみをサンプリングします。
    /// </summary>
    [UnitTitle("〇色に触れた？")]
    [UnitCategory("FUnity/Blocks/調べる")]
    [UnitSubtitle("調べる")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class TouchingColorPredicateUnit : Unit
    {
        /// <summary>判定する目標色を受け取る入力ポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_TargetColor;

        /// <summary>許容する色差を受け取る入力ポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Tolerance;

        /// <summary>判定結果を出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>判定対象の色入力ポートを公開します。</summary>
        public ValueInput TargetColor => m_TargetColor;

        /// <summary>許容度入力ポートを公開します。</summary>
        public ValueInput Tolerance => m_Tolerance;

        /// <summary>背景色との接触判定結果を出力するポートです。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 入出力ポートを構築し、評価時に背景色との接触判定を実行します。
        /// </summary>
        protected override void Definition()
        {
            m_TargetColor = ValueInput("color", Color.magenta);
            m_Tolerance = ValueInput("tolerance", 0.08f);
            m_Result = ValueOutput<bool>("result", Evaluate);

            Requirement(m_TargetColor, m_Result);
            Requirement(m_Tolerance, m_Result);
        }

        /// <summary>
        /// Flow から俳優 Presenter を解決し、背景色が入力色へ近い点が存在するかを判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>背景色が目標色に近い場合は true。</returns>
        private bool Evaluate(Flow flow)
        {
            if (!ScratchUnitUtil.TryGetActorPresenter(flow, out ActorPresenter presenter))
            {
                return false;
            }

            var targetColor = flow.GetValue<Color>(m_TargetColor);
            var tolerance = Mathf.Max(0f, flow.GetValue<float>(m_Tolerance));
            return TouchPredicates.IsTouchingBackgroundColor(presenter, targetColor, tolerance);
        }
    }
}
