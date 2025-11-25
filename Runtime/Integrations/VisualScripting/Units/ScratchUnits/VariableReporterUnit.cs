using Unity.VisualScripting;
using UnityEngine;

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

        /// <summary>変数の現在値を float として出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>変数の現在値を int として出力する ValueOutput です。</summary>
        [DoNotSerialize]
        [PortLabel("値(int)")]
        private ValueOutput m_ResultInt;

        /// <summary>変数の現在値を string として出力する ValueOutput です。</summary>
        [DoNotSerialize]
        [PortLabel("値(string)")]
        private ValueOutput m_ResultString;

        /// <summary>variableName ポートへの参照を公開します。</summary>
        public ValueInput VariableName => m_VariableName;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>resultInt ポートへの参照を公開します。</summary>
        public ValueOutput ResultInt => m_ResultInt;

        /// <summary>resultString ポートへの参照を公開します。</summary>
        public ValueOutput ResultString => m_ResultString;

        /// <summary>
        /// 入出力ポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_VariableName = ValueInput<string>("variableName", string.Empty);
            m_Result = ValueOutput<float>("result", GetVariableFloat);
            m_ResultInt = ValueOutput<int>("resultInt", GetVariableInt);
            m_ResultString = ValueOutput<string>("resultString", GetVariableString);

            Requirement(m_VariableName, m_Result);
            Requirement(m_VariableName, m_ResultInt);
            Requirement(m_VariableName, m_ResultString);
        }

        /// <summary>
        /// 指定された変数の値を変数サービスから float として取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>取得した変数値。サービス未初期化時は 0 を返します。</returns>
        private float GetVariableFloat(Flow flow)
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

        /// <summary>
        /// 変数の値を int に変換して返却します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>Mathf.RoundToInt で四捨五入した整数。</returns>
        private int GetVariableInt(Flow flow)
        {
            var floatValue = GetVariableFloat(flow);
            return Mathf.RoundToInt(floatValue);
        }

        /// <summary>
        /// 変数の値を string に変換して返却します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>float 値を ToString した文字列。</returns>
        private string GetVariableString(Flow flow)
        {
            var floatValue = GetVariableFloat(flow);
            return floatValue.ToString();
        }
    }
}
