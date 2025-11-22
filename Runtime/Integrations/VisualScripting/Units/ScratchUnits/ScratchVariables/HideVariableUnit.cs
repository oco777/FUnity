// Updated: 2025-03-18
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「変数（変数）を隠す」に対応し、変数モニターを非表示にします。
    /// </summary>
    [UnitTitle("変数（変数）を隠す")]
    [UnitCategory("FUnity/Blocks/変数")]
    [UnitSubtitle("変数")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class HideVariableUnit : Unit
    {
        /// <summary>制御フロー入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フロー出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>対象変数名ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_VariableName;

        /// <summary>enter ポート参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポート参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>ポート定義を行います。</summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");

            m_VariableName = ValueInput("variableName", string.Empty);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>入力を受け取り、変数モニターを非表示にします。</summary>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = ScratchVariableUnitUtility.ResolveService("変数（変数）を隠す");
            if (service == null)
            {
                return m_Exit;
            }

            var name = flow.GetValue<string>(m_VariableName);
            var actor = ScratchVariableUnitUtility.ResolveActorPresenter(flow);

            service.SetVisible(name, false, actor);
            return m_Exit;
        }
    }
}
