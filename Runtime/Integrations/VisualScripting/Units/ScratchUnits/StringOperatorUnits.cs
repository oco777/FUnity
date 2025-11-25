using UnityEngine;
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// 2 つの文字列を受け取り、連結した結果を返す Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("A と B")]
    [UnitShortTitle("A と B")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(string))]
    public sealed class JoinStringsUnit : Unit
    {
        /// <summary>1 つ目の文字列を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_A;

        /// <summary>2 つ目の文字列を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_B;

        /// <summary>連結結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>A ポートへの参照を公開します。</summary>
        public ValueInput A => m_A;

        /// <summary>B ポートへの参照を公開します。</summary>
        public ValueInput B => m_B;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 必要な入力ポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_A = ValueInput<string>("a", string.Empty);
            m_B = ValueInput<string>("b", string.Empty);
            m_Result = ValueOutput<string>("result", Operation);

            Requirement(m_A, m_Result);
            Requirement(m_B, m_Result);
        }

        /// <summary>
        /// Flow から 2 つの文字列を取得して連結結果を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>連結後の文字列。</returns>
        private string Operation(Flow flow)
        {
            var aValue = flow.GetValue<string>(m_A) ?? string.Empty;
            var bValue = flow.GetValue<string>(m_B) ?? string.Empty;
            return aValue + bValue;
        }
    }

    /// <summary>
    /// 文字列の指定した位置にある 1 文字を返す Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("S の N 番目の文字")]
    [UnitShortTitle("S の N 番目の文字")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(string))]
    public sealed class LetterOfStringUnit : Unit
    {
        /// <summary>対象の文字列を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Text;

        /// <summary>取得したい文字位置を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Index;

        /// <summary>取得した 1 文字を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>text ポートへの参照を公開します。</summary>
        public ValueInput Text => m_Text;

        /// <summary>index ポートへの参照を公開します。</summary>
        public ValueInput Index => m_Index;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 入力ポートと出力ポートを定義し、必要な依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Text = ValueInput<string>("text", string.Empty);
            m_Index = ValueInput<float>("index", 1f);
            m_Result = ValueOutput<string>("result", Operation);

            Requirement(m_Text, m_Result);
            Requirement(m_Index, m_Result);
        }

        /// <summary>
        /// 1 始まりのインデックスで指定された文字を返します。範囲外の場合は空文字列を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>指定位置の文字、または空文字列。</returns>
        private string Operation(Flow flow)
        {
            var textValue = flow.GetValue<string>(m_Text) ?? string.Empty;
            var rawIndex = flow.GetValue<float>(m_Index);
            var indexValue = Mathf.RoundToInt(rawIndex);

            if (string.IsNullOrEmpty(textValue))
            {
                return string.Empty;
            }

            if (indexValue < 1 || indexValue > textValue.Length)
            {
                return string.Empty;
            }

            return textValue[indexValue - 1].ToString();
        }
    }

    /// <summary>
    /// 指定した文字列の長さを返す Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("S の長さ")]
    [UnitShortTitle("S の長さ")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(string))]
    public sealed class LengthOfStringUnit : Unit
    {
        /// <summary>長さを測定する文字列を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Text;

        /// <summary>測定した長さを出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Length;

        /// <summary>text ポートへの参照を公開します。</summary>
        public ValueInput Text => m_Text;

        /// <summary>length ポートへの参照を公開します。</summary>
        public ValueOutput Length => m_Length;

        /// <summary>
        /// 入出力ポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Text = ValueInput<string>("text", string.Empty);
            m_Length = ValueOutput<float>("length", Operation);

            Requirement(m_Text, m_Length);
        }

        /// <summary>
        /// 文字列の長さを取得し、float として返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>文字列の長さ。空または null の場合は 0。</returns>
        private float Operation(Flow flow)
        {
            var textValue = flow.GetValue<string>(m_Text) ?? string.Empty;
            return textValue.Length;
        }
    }

    /// <summary>
    /// 文字列が指定した部分文字列を含むかを判定する Scratch 互換の演算ユニットです。
    /// </summary>
    [UnitTitle("S に SUB が含まれる")]
    [UnitShortTitle("S に SUB が含まれる")]
    [UnitCategory("FUnity/Blocks/演算")]
    [UnitSubtitle("演算")]
    [TypeIcon(typeof(bool))]
    public sealed class StringContainsUnit : Unit
    {
        /// <summary>検索対象の文字列を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Text;

        /// <summary>検索する部分文字列を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Sub;

        /// <summary>包含判定結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>text ポートへの参照を公開します。</summary>
        public ValueInput Text => m_Text;

        /// <summary>sub ポートへの参照を公開します。</summary>
        public ValueInput Sub => m_Sub;

        /// <summary>result ポートへの参照を公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 入出力ポートを定義し、依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Text = ValueInput<string>("text", string.Empty);
            m_Sub = ValueInput<string>("sub", string.Empty);
            m_Result = ValueOutput<bool>("result", Operation);

            Requirement(m_Text, m_Result);
            Requirement(m_Sub, m_Result);
        }

        /// <summary>
        /// 文字列が部分文字列を含むかを判定します。部分文字列が空の場合は true を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>包含していれば true。</returns>
        private bool Operation(Flow flow)
        {
            var textValue = flow.GetValue<string>(m_Text) ?? string.Empty;
            var subValue = flow.GetValue<string>(m_Sub) ?? string.Empty;

            if (string.IsNullOrEmpty(subValue))
            {
                return true;
            }

            return textValue.Contains(subValue);
        }
    }
}
