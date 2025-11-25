using UnityEngine;
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// 2 つの数値を受け取り、和を計算する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 + 〇")]
    [UnitShortTitle("〇 + 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(float))]
    public sealed class AddNumbersUnit : Unit
    {
        /// <summary>1 つ目の入力値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>2 つ目の入力値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>加算結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 加算に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 0f);
            m_Result = ValueOutput<float>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得して和を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>加算結果。</returns>
        private float Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue + bValue;
        }
    }

    /// <summary>
    /// 2 つの数値を受け取り、差を計算する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 - 〇")]
    [UnitShortTitle("〇 - 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(float))]
    public sealed class SubtractNumbersUnit : Unit
    {
        /// <summary>被減数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>減数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>減算結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 減算に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 0f);
            m_Result = ValueOutput<float>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得して差を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>減算結果。</returns>
        private float Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue - bValue;
        }
    }

    /// <summary>
    /// 2 つの数値を受け取り、積を計算する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 * 〇")]
    [UnitShortTitle("〇 * 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(float))]
    public sealed class MultiplyNumbersUnit : Unit
    {
        /// <summary>乗算する 1 つ目の値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>乗算する 2 つ目の値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>乗算結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 乗算に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 0f);
            m_Result = ValueOutput<float>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得して積を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>乗算結果。</returns>
        private float Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue * bValue;
        }
    }

    /// <summary>
    /// 2 つの数値を受け取り、商を計算する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 / 〇")]
    [UnitShortTitle("〇 / 〇")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(float))]
    public sealed class DivideNumbersUnit : Unit
    {
        /// <summary>被除数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>除数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>除算結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 除算に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 1f);
            m_Result = ValueOutput<float>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得して商を返します。
        /// C# の float 除算仕様に従い、0 除算時もそのまま返却します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>除算結果。</returns>
        private float Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue / bValue;
        }
    }

    /// <summary>
    /// 2 つの数値を受け取り、余りを計算する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 を 〇 で割った余り")]
    [UnitShortTitle("〇 を 〇 で割った余り")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(float))]
    public sealed class ModuloNumbersUnit : Unit
    {
        /// <summary>割られる値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>割る値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>剰余結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 余り計算に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<float>("a", 0f);
            m_B = ValueInput<float>("b", 1f);
            m_Result = ValueOutput<float>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの値を取得して剰余を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>剰余結果。</returns>
        private float Operation(Flow flow)
        {
            var aValue = flow.GetValue<float>(m_A);
            var bValue = flow.GetValue<float>(m_B);
            return aValue % bValue;
        }
    }

    /// <summary>
    /// 1 つの数値を受け取り、四捨五入した結果を返す Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("〇 を四捨五入")]
    [UnitShortTitle("〇 を四捨五入")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(float))]
    public sealed class RoundNumberUnit : Unit
    {
        /// <summary>丸め対象の値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Number;

        /// <summary>丸め結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>number ポートへの参照を公開します。</summary>
        public ValueInput Number => m_Number;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 四捨五入に必要なポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Number = ValueInput<float>("number", 0f);
            m_Result = ValueOutput<float>("result", Operation);

            Requirement(m_Number, m_Result);
        }

        /// <summary>
        /// Flow から入力値を取得し、Mathf.Round を用いて四捨五入結果を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>四捨五入後の値。</returns>
        private float Operation(Flow flow)
        {
            var value = flow.GetValue<float>(m_Number);
            return Mathf.Round(value);
        }
    }
}
