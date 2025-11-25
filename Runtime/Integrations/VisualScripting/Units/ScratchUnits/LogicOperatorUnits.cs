using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// 2 つの数値を比較し、左辺が右辺より大きいかを判定する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 > 〇")]
    [UnitShortTitle("〇 > 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(bool))]
    public sealed class GreaterThanNumbersUnit : Unit
    {
        /// <summary>左辺の数値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>右辺の数値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>大小比較の結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 比較に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 0f);
            m_Result = ValueOutput<bool>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得し、大きいかどうかを判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>左辺が右辺より大きければ true。</returns>
        private bool Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue > bValue;
        }
    }

    /// <summary>
    /// 2 つの数値を比較し、左辺が右辺より小さいかを判定する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 < 〇")]
    [UnitShortTitle("〇 < 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(bool))]
    public sealed class LessThanNumbersUnit : Unit
    {
        /// <summary>左辺の数値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>右辺の数値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>大小比較の結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 比較に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 0f);
            m_Result = ValueOutput<bool>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得し、小さいかどうかを判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>左辺が右辺より小さければ true。</returns>
        private bool Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue < bValue;
        }
    }

    /// <summary>
    /// 2 つの数値が等しいかどうかを判定する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 = 〇")]
    [UnitShortTitle("〇 = 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(bool))]
    public sealed class EqualNumbersUnit : Unit
    {
        /// <summary>左辺の数値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>右辺の数値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>等価比較の結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 比較に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 0f);
            m_Result = ValueOutput<bool>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得し、等しいかどうかを判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>2 つの値が等しければ true。</returns>
        private bool Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue == bValue;
        }
    }

    /// <summary>
    /// 2 つの bool 値の論理積を計算する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 かつ 〇")]
    [UnitShortTitle("〇 かつ 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(bool))]
    public sealed class AndBooleanUnit : Unit
    {
        /// <summary>論理積の左辺を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>論理積の右辺を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>論理積の結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 論理積に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput("a", false);
            m_B = ValueInput("b", false);
            m_Result = ValueOutput<bool>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの bool 値を取得し、論理積を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>両方が true のときのみ true。</returns>
        private bool Operation(Flow flow)
        {
            var aValue = flow.GetValue<bool>(m_A);
            var bValue = flow.GetValue<bool>(m_B);
            return aValue && bValue;
        }
    }

    /// <summary>
    /// 2 つの bool 値の論理和を計算する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 または 〇")]
    [UnitShortTitle("〇 または 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(bool))]
    public sealed class OrBooleanUnit : Unit
    {
        /// <summary>論理和の左辺を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>論理和の右辺を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>論理和の結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 論理和に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput("a", false);
            m_B = ValueInput("b", false);
            m_Result = ValueOutput<bool>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの bool 値を取得し、論理和を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>どちらかが true であれば true。</returns>
        private bool Operation(Flow flow)
        {
            var aValue = flow.GetValue<bool>(m_A);
            var bValue = flow.GetValue<bool>(m_B);
            return aValue || bValue;
        }
    }

    /// <summary>
    /// 1 つの bool 値を反転する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 ではない")]
    [UnitShortTitle("〇 ではない")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(bool))]
    public sealed class NotBooleanUnit : Unit
    {
        /// <summary>反転する対象の bool 値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>反転後の bool 値を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>value ポートへの参照を公開します。</summary>
        public ValueInput Value => m_Value;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// NOT 演算に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Value = ValueInput("value", false);
            m_Result = ValueOutput<bool>("result", Operation);

            Requirement(m_Value, m_Result);
        }

        /// <summary>
        /// Flow から bool 値を取得し、反転した結果を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>入力が true なら false、false なら true。</returns>
        private bool Operation(Flow flow)
        {
            var value = flow.GetValue<bool>(m_Value);
            return !value;
        }
    }
}
