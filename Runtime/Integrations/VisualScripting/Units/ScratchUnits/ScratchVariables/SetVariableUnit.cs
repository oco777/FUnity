// Updated: 2025-03-18
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「（変数）を○にする」に対応し、指定した変数へ絶対値を書き込むユニットです。
    /// </summary>
    [UnitTitle("（変数）を○にする")]
    [UnitCategory("FUnity/Scratch/変数")]
    [UnitSubtitle("funity scratch 変数 set variable")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetVariableUnit : Unit
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

        /// <summary>設定する値を受け取るポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>ポート定義を行い、enter→exit の制御線を登録します。</summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");

            m_VariableName = ValueInput("variableName", string.Empty);
            m_Value = ValueInput("value", 0f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>入力を受け取り、変数サービスへ値を設定します。</summary>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = ScratchVariableUnitUtility.ResolveService("（変数）を○にする");
            if (service == null)
            {
                return m_Exit;
            }

            var name = flow.GetValue<string>(m_VariableName);
            var value = flow.GetValue<float>(m_Value);
            var actor = ScratchVariableUnitUtility.ResolveActorPresenter(flow);

            service.SetValue(name, value, actor);
            return m_Exit;
        }
    }
}
