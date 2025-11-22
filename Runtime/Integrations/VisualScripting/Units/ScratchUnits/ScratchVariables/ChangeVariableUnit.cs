// Updated: 2025-03-18
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「（変数）を○ずつ変える」に対応し、指定した変数へ加算を行うユニットです。
    /// </summary>
    [UnitTitle("（変数）を○ずつ変える")]
    [UnitCategory("FUnity/Blocks/変数")]
    [UnitSubtitle("funity scratch 変数 change variable add")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ChangeVariableUnit : Unit
    {
        /// <summary>制御フロー入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フロー出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>対象変数名を受け取るポート。</summary>
        [DoNotSerialize]
        private ValueInput m_VariableName;

        /// <summary>加算量を受け取るポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Delta;

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
            m_Delta = ValueInput("delta", 1f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>入力を受け取り、変数サービスへ加算を指示します。</summary>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = ScratchVariableUnitUtility.ResolveService("（変数）を○ずつ変える");
            if (service == null)
            {
                return m_Exit;
            }

            var name = flow.GetValue<string>(m_VariableName);
            var delta = flow.GetValue<float>(m_Delta);
            var actor = ScratchVariableUnitUtility.ResolveActorPresenter(flow);

            service.AddValue(name, delta, actor);
            return m_Exit;
        }
    }
}
