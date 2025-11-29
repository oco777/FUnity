// Updated: 2025-11-05
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「背景を (背景の番号) にする」に対応し、ステージ背景を番号指定で切り替える Unit です。
    /// </summary>
    [UnitTitle("背景を〇にする")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetBackgroundByIndexUnit : Unit
    {
        /// <summary>フロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>フロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Output;

        /// <summary>背景番号（1 始まり）を受け取る入力ポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Index;

        /// <summary>入力ポートへの参照です。</summary>
        public ControlInput Input => m_Input;

        /// <summary>出力ポートへの参照です。</summary>
        public ControlOutput Output => m_Output;

        /// <summary>背景番号ポートへの参照です。</summary>
        public ValueInput Index => m_Index;

        /// <summary>
        /// ポート定義を行い、背景番号入力と制御フローを登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("enter", OnEnter);
            m_Output = ControlOutput("exit");
            m_Index = ValueInput<int>("index", 1);

            Succession(m_Input, m_Output);
            Requirement(m_Index, m_Input);
        }

        /// <summary>
        /// フロー入力時に背景番号を取得し、FUnityManager へ切り替えを依頼します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ続く ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var index = flow.GetValue<int>(m_Index);
            FUnityManager.SetBackgroundByNumber(index);
            return m_Output;
        }
    }

    /// <summary>
    /// Scratch の「背景を (背景の名前) にする」に対応し、表示名でステージ背景を切り替える Unit です。
    /// </summary>
    [UnitTitle("背景を (名前) にする")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetBackgroundByNameUnit : Unit
    {
        /// <summary>フロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>フロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Output;

        /// <summary>背景名を受け取る入力ポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Name;

        /// <summary>入力ポートへの参照です。</summary>
        public ControlInput Input => m_Input;

        /// <summary>出力ポートへの参照です。</summary>
        public ControlOutput Output => m_Output;

        /// <summary>背景名ポートへの参照です。</summary>
        public ValueInput Name => m_Name;

        /// <summary>
        /// ポート定義を行い、背景名入力と制御フローを登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("enter", OnEnter);
            m_Output = ControlOutput("exit");
            m_Name = ValueInput<string>("name", string.Empty);

            Succession(m_Input, m_Output);
            Requirement(m_Name, m_Input);
        }

        /// <summary>
        /// フロー入力時に背景名を取得し、FUnityManager へ切り替えを依頼します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ続く ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var name = flow.GetValue<string>(m_Name);
            FUnityManager.SetBackgroundByName(name);
            return m_Output;
        }
    }

    /// <summary>
    /// Scratch の「次の背景にする」に対応し、背景を循環切り替えする Unit です。
    /// </summary>
    [UnitTitle("次の背景にする")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class NextBackgroundUnit : Unit
    {
        /// <summary>フロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>フロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Output;

        /// <summary>入力ポートへの参照です。</summary>
        public ControlInput Input => m_Input;

        /// <summary>出力ポートへの参照です。</summary>
        public ControlOutput Output => m_Output;

        /// <summary>
        /// ポート定義を行い、制御フローのみを登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("enter", OnEnter);
            m_Output = ControlOutput("exit");

            Succession(m_Input, m_Output);
        }

        /// <summary>
        /// フロー入力時に次の背景へ進めます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ続く ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            FUnityManager.NextBackground();
            return m_Output;
        }
    }

    /// <summary>
    /// Scratch の「背景の番号」に対応し、現在の背景番号を返すレポーターユニットです。
    /// </summary>
    [UnitTitle("背景の番号")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class GetBackgroundIndexUnit : Unit
    {
        /// <summary>背景番号を出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_Index;

        /// <summary>背景番号ポートへの参照です。</summary>
        public ValueOutput Index => m_Index;

        /// <summary>
        /// ポート定義を行い、背景番号出力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Index = ValueOutput<int>("index", GetBackgroundNumber);
        }

        /// <summary>
        /// 現在の背景番号（1 始まり）を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>背景番号。背景未設定時は 0。</returns>
        private int GetBackgroundNumber(Flow flow)
        {
            return FUnityManager.GetCurrentBackgroundNumber();
        }
    }

    /// <summary>
    /// Scratch の「背景の名前」に対応し、現在の背景名を返すレポーターユニットです。
    /// </summary>
    [UnitTitle("背景の名前")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class GetBackgroundNameUnit : Unit
    {
        /// <summary>背景名を出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_Name;

        /// <summary>背景名ポートへの参照です。</summary>
        public ValueOutput Name => m_Name;

        /// <summary>
        /// ポート定義を行い、背景名出力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Name = ValueOutput<string>("name", GetBackgroundName);
        }

        /// <summary>
        /// 現在の背景名を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>背景名。未設定時は空文字列。</returns>
        private string GetBackgroundName(Flow flow)
        {
            return FUnityManager.GetCurrentBackgroundName();
        }
    }
}
