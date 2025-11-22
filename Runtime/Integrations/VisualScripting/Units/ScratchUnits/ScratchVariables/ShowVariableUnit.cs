// Updated: 2025-03-18
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「変数（変数）を表示する」に対応し、変数モニターを可視化します。
    /// </summary>
    [UnitTitle("変数（変数）を表示する")]
    [UnitCategory("FUnity/Blocks/変数")]
    [UnitSubtitle("funity scratch 変数 show variable monitor")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ShowVariableUnit : Unit
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

        /// <summary>入力を受け取り、変数モニターを表示状態にします。</summary>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = ScratchVariableUnitUtility.ResolveService("変数（変数）を表示する");
            if (service == null)
            {
                return m_Exit;
            }

            var name = flow.GetValue<string>(m_VariableName);
            var actor = ScratchVariableUnitUtility.ResolveActorPresenter(flow);

            service.SetVisible(name, true, actor);
            return m_Exit;
        }
    }
}
