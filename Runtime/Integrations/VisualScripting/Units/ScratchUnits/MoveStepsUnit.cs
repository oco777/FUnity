// Updated: 2025-10-19
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇歩動かす」ブロックに対応し、現在の向きに沿ってピクセル移動を指示するカスタム Unit です。
    /// </summary>
    [UnitTitle("Scratch/Move Steps")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class MoveStepsUnit : Unit
    {
        /// <summary>フローの開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>操作対象の <see cref="ActorPresenterAdapter"/>（旧称 FooniController）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>移動する歩数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Steps;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>steps ポートへの参照を公開します。</summary>
        public ValueInput Steps => m_Steps;

        /// <summary>
        /// ポートの定義を行い、enter→exit の制御線と target/steps の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Target = ValueInput<ActorPresenterAdapter>("target", (ActorPresenterAdapter)null);
            m_Steps = ValueInput<float>("steps", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter を受け取った際にターゲットを解決し、指定歩数分の移動を実行します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートを返し、後続へ制御を渡します。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveTarget(flow.GetValue<ActorPresenterAdapter>(m_Target));
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Move Steps: ActorPresenterAdapter (FooniController) が見つかりません。");
                return m_Exit;
            }

            var steps = flow.GetValue<float>(m_Steps);
            controller.MoveSteps(steps);
            return m_Exit;
        }
    }
}
