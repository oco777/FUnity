// Updated: 2025-10-19
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇度回す」ブロックを再現し、方向を相対的に更新するカスタム Unit です。
    /// </summary>
    [UnitTitle("Scratch/Turn Degrees")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class TurnDegreesUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>操作対象の <see cref="ActorPresenterAdapter"/>（旧称 FooniController）を受け取る ValueInput です。未接続でも自動解決されます。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>加算する角度（度）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaDegrees;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>deltaDegrees ポートへの参照を公開します。</summary>
        public ValueInput DeltaDegrees => m_DeltaDegrees;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と target/deltaDegrees を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Target = ValueInput<ActorPresenterAdapter>("target", (ActorPresenterAdapter)null);
            m_DeltaDegrees = ValueInput<float>("deltaDegrees", 15f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時にターゲットを解決し、現在角度へ増減を加えます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var explicitAdapter = flow.HasValue(m_Target) ? flow.GetValue<ActorPresenterAdapter>(m_Target) : null;
            var controller = ScratchUnitUtil.ResolveAdapter(flow, explicitAdapter);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Turn Degrees: ActorPresenterAdapter (FooniController) が見つかりません。");
                return m_Exit;
            }

            var delta = flow.GetValue<float>(m_DeltaDegrees);
            var current = controller.GetDirection();
            controller.SetDirection(current + delta);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇度に向ける」ブロックを再現し、絶対角度を設定するカスタム Unit です。
    /// </summary>
    [UnitTitle("Scratch/Point Direction")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class PointDirectionUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>操作対象の <see cref="ActorPresenterAdapter"/>（旧称 FooniController）を受け取る ValueInput です。未接続でも自動解決されます。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>設定する角度（度）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Degrees;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>degrees ポートへの参照を公開します。</summary>
        public ValueInput Degrees => m_Degrees;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と target/degrees を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Target = ValueInput<ActorPresenterAdapter>("target", (ActorPresenterAdapter)null);
            m_Degrees = ValueInput<float>("degrees", 90f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter を受信した際に角度を絶対値で設定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var explicitAdapter = flow.HasValue(m_Target) ? flow.GetValue<ActorPresenterAdapter>(m_Target) : null;
            var controller = ScratchUnitUtil.ResolveAdapter(flow, explicitAdapter);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Point Direction: ActorPresenterAdapter (FooniController) が見つかりません。");
                return m_Exit;
            }

            var degrees = flow.GetValue<float>(m_Degrees);
            controller.SetDirection(degrees);
            return m_Exit;
        }
    }
}
