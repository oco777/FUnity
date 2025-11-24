using UnityEngine;
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.Blocks
{
    /// <summary>
    /// Scratch の「〇から〇までの乱数」を再現する Value Unit です。
    ///
    /// - 両方の入力が整数値なら、整数乱数を返します（両端を含む）。
    /// - どちらかが小数なら、小数の乱数を返します。
    /// </summary>
    [UnitTitle("○から○までの乱数")]
    [UnitShortTitle("乱数")]
    [UnitSubtitle("演算")]
    [UnitCategory("FUnity/Blocks/演算")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class RandomFromToUnit : Unit
    {
        /// <summary>最小値入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Min;

        /// <summary>最大値入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Max;

        /// <summary>乱数出力ポート。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>最小値入力ポートへの参照。</summary>
        public ValueInput Min => m_Min;

        /// <summary>最大値入力ポートへの参照。</summary>
        public ValueInput Max => m_Max;

        /// <summary>乱数結果出力ポートへの参照。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// ポート定義。
        /// </summary>
        protected override void Definition()
        {
            // デフォルト引数は Scratch と同じく 1 〜 10 あたりを想定
            m_Min = ValueInput<float>("min", 1f);
            m_Max = ValueInput<float>("max", 10f);

            m_Result = ValueOutput<float>("result", GetRandom);
        }

        /// <summary>
        /// 入力範囲から乱数を生成して返します。
        /// </summary>
        private float GetRandom(Flow flow)
        {
            var rawMin = flow.GetValue<float>(m_Min);
            var rawMax = flow.GetValue<float>(m_Max);

            // min と max が逆転していたら入れ替え
            var min = rawMin;
            var max = rawMax;
            if (min > max)
            {
                var tmp = min;
                min = max;
                max = tmp;
            }

            // 範囲がほぼ 0 の場合は、その値を返す
            if (Mathf.Approximately(min, max))
            {
                return min;
            }

            // 「整数を入力したら整数乱数」を実現するため、
            // 入力が整数かどうかを「小数部がほぼ 0 か」で判定します。
            var isMinInt = Mathf.Approximately(min, Mathf.Round(min));
            var isMaxInt = Mathf.Approximately(max, Mathf.Round(max));

            if (isMinInt && isMaxInt)
            {
                // 整数モード: 両端を含む整数乱数
                var iMin = (int)Mathf.Round(min);
                var iMax = (int)Mathf.Round(max);

                if (iMin > iMax)
                {
                    var tmp = iMin;
                    iMin = iMax;
                    iMax = tmp;
                }

                // Unity の Random.Range(int, int) は [min, max) なので、
                // 上端も含めたい場合は +1 する。
                var resultInt = Random.Range(iMin, iMax + 1);
                return resultInt;
            }

            // 小数モード: float 乱数
            // Random.Range(float, float) は [min, max) だが、
            // Scratch に近い挙動として問題になりにくいのでそのまま利用。
            return Random.Range(min, max);
        }
    }
}
