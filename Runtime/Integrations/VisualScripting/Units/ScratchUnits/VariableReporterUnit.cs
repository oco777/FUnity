using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「(変数)」ブロックに対応し、指定した変数の現在値を返すユニットです。
    /// </summary>
    [UnitTitle("（変数）")]
    [UnitCategory("FUnity/Blocks/変数")]
    [UnitSubtitle("変数")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class VariableReporterUnit : Unit
    {
        /// <summary>対象変数名を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_VariableName;

        /// <summary>変数の現在値を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>variableName ポートへの参照を公開します。</summary>
        public ValueInput VariableName => m_VariableName;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 入出力ポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_VariableName = ValueInput<string>("variableName", string.Empty);
            m_Result = ValueOutput<float>("result", GetVariableValue);

            Requirement(m_VariableName, m_Result);
        }

        /// <summary>
        /// 指定された変数の値を変数サービスから取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>取得した変数値。サービス未初期化時は 0 を返します。</returns>
        private float GetVariableValue(Flow flow)
        {
            var service = ScratchVariableUnitUtility.ResolveService("（変数）");
            if (service == null)
            {
                return 0f;
            }

            var name = flow.GetValue<string>(m_VariableName);
            var actor = ScratchVariableUnitUtility.ResolveActorPresenter(flow);
            return service.GetValue(name, actor);
        }
    }
}
