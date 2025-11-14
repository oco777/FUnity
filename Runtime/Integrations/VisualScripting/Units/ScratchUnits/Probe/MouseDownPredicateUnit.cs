// Updated: 2025-03-20
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe
{
    /// <summary>
    /// Scratch の「マウスが押された」ブロックを再現し、左ボタン押下状態を返す Unit です。
    /// </summary>
    [UnitTitle("マウスが押された")]
    [UnitCategory("FUnity/Scratch/調べる")]
    [UnitSubtitle("funity scratch 調べる mouse down pressed ボタン")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class MouseDownPredicateUnit : Unit
    {
        /// <summary>押下状態を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Pressed;

        /// <summary>押下状態出力ポートへの参照を公開します。</summary>
        public ValueOutput Pressed => m_Pressed;

        /// <summary>
        /// 出力ポートを構築し、評価時に押下状態を取得します。
        /// </summary>
        protected override void Definition()
        {
            m_Pressed = ValueOutput<bool>(nameof(Pressed), Evaluate);
        }

        /// <summary>
        /// 現在のマウス押下状態を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。未使用。</param>
        /// <returns>左ボタンが押されていれば true。</returns>
        private bool Evaluate(Flow flow)
        {
            var provider = ScratchUnitUtil.ResolveMouseProvider();
            return provider != null && provider.IsPressed;
        }
    }
}
