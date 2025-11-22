// Updated: 2025-05-30
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「コスチュームを〇にする」ブロックに対応し、Presenter 経由で指定番号のコスチュームへ切り替える Unit です。
    /// </summary>
    [UnitTitle("コスチュームを〇にする")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("funity scratch 見た目 costume set コスチューム")] 
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetCostumeNumberUnit : Unit
    {
        /// <summary>フロー入力を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>次のノードへ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Output;

        /// <summary>Scratch コスチューム番号（1 始まり）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_CostumeNumber;

        /// <summary>入力ポートへの参照を公開します。</summary>
        public ControlInput Input => m_Input;

        /// <summary>出力ポートへの参照を公開します。</summary>
        public ControlOutput Output => m_Output;

        /// <summary>costumeNumber ポートへの参照を公開します。</summary>
        public ValueInput CostumeNumber => m_CostumeNumber;

        /// <summary>
        /// ポート定義を行い、フロー入出力とコスチューム番号の入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("in", OnEnter);
            m_Output = ControlOutput("out");

            m_CostumeNumber = ValueInput<int>("costumeNumber", 1);

            Succession(m_Input, m_Output);
        }

        /// <summary>
        /// フロー入力時にアダプタを解決し、コスチューム番号を Presenter へ転送します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続の ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            if (ScratchUnitUtil.TryGetActorAdapter(flow, null, out var adapter))
            {
                var costumeNumber = flow.GetValue<int>(m_CostumeNumber);
                adapter.SetCostumeNumber(costumeNumber);
            }

            return m_Output;
        }
    }

    /// <summary>
    /// Scratch の「次のコスチュームにする」ブロックに対応し、Presenter 経由でコスチュームを循環切り替えする Unit です。
    /// </summary>
    [UnitTitle("次のコスチュームにする")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("funity scratch 見た目 costume next 次へ")] 
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class NextCostumeUnit : Unit
    {
        /// <summary>フロー入力を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>次のノードへ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Output;

        /// <summary>入力ポートへの参照です。</summary>
        public ControlInput Input => m_Input;

        /// <summary>出力ポートへの参照です。</summary>
        public ControlOutput Output => m_Output;

        /// <summary>
        /// ポート定義を行い、フロー入出力のみを登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("in", OnEnter);
            m_Output = ControlOutput("out");

            Succession(m_Input, m_Output);
        }

        /// <summary>
        /// フロー入力時にアダプタを解決し、Presenter へ「次のコスチューム」を指示します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続の ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            if (ScratchUnitUtil.TryGetActorAdapter(flow, null, out var adapter))
            {
                adapter.NextCostume();
            }

            return m_Output;
        }
    }

    /// <summary>
    /// Scratch の「コスチュームの番号」ブロックに対応し、現在のコスチューム番号（1 始まり）を取得する Unit です。
    /// </summary>
    [UnitTitle("コスチュームの番号")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("funity scratch 見た目 costume number 番号")] 
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class CostumeNumberUnit : Unit
    {
        /// <summary>現在のコスチューム番号を返す ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_CostumeNumber;

        /// <summary>costumeNumber ポートへの参照です。</summary>
        public ValueOutput CostumeNumber => m_CostumeNumber;

        /// <summary>
        /// ポート定義を行い、コスチューム番号の出力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_CostumeNumber = ValueOutput<int>("costumeNumber", GetCostumeNumber);
        }

        /// <summary>
        /// Presenter アダプタを解決して現在の Scratch コスチューム番号を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>1 始まりのコスチューム番号。利用可能な Sprite が無い場合は 0。</returns>
        private int GetCostumeNumber(Flow flow)
        {
            if (ScratchUnitUtil.TryGetActorAdapter(flow, null, out var adapter))
            {
                return adapter.GetCostumeNumber();
            }

            return 0;
        }
    }
}
