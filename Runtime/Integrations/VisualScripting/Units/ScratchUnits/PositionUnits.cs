// Updated: 2025-10-19
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「x座標を～に、y座標を～にする」ブロックを再現し、絶対座標を設定するカスタム Unit です。
    /// </summary>
    [UnitTitle("Scratch/Go To X,Y")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GoToXYUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>操作対象の <see cref="FooniController"/> を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>X 座標（px）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_X;

        /// <summary>Y 座標（px）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Y;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>x ポートへの参照を公開します。</summary>
        public ValueInput X => m_X;

        /// <summary>y ポートへの参照を公開します。</summary>
        public ValueInput Y => m_Y;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と target/x/y を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Target = ValueInput<FooniController>("target", (FooniController)null);
            m_X = ValueInput<float>("x", 0f);
            m_Y = ValueInput<float>("y", 0f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に絶対座標を設定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveTarget(flow.GetValue<FooniController>(m_Target));
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Go To X,Y: FooniController が見つかりません。");
                return m_Exit;
            }

            var position = new Vector2(flow.GetValue<float>(m_X), flow.GetValue<float>(m_Y));
            controller.SetPositionPixels(position);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「x座標を〇ずつ変える」ブロックを再現し、X 座標へ差分を加算するカスタム Unit です。
    /// </summary>
    [UnitTitle("Scratch/Change X By")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class ChangeXByUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>操作対象の <see cref="FooniController"/> を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>X 座標の差分（px）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaX;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>dx ポートへの参照を公開します。</summary>
        public ValueInput DeltaX => m_DeltaX;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と target/dx を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Target = ValueInput<FooniController>("target", (FooniController)null);
            m_DeltaX = ValueInput<float>("dx", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に X 座標へ差分を加算します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveTarget(flow.GetValue<FooniController>(m_Target));
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Change X By: FooniController が見つかりません。");
                return m_Exit;
            }

            var delta = flow.GetValue<float>(m_DeltaX);
            controller.AddPositionPixels(new Vector2(delta, 0f));
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「y座標を〇ずつ変える」ブロックを再現し、Y 座標へ差分を加算するカスタム Unit です。
    /// </summary>
    [UnitTitle("Scratch/Change Y By")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class ChangeYByUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>操作対象の <see cref="FooniController"/> を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>Y 座標の差分（px）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaY;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>dy ポートへの参照を公開します。</summary>
        public ValueInput DeltaY => m_DeltaY;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と target/dy を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Target = ValueInput<FooniController>("target", (FooniController)null);
            m_DeltaY = ValueInput<float>("dy", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に Y 座標へ差分を加算します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveTarget(flow.GetValue<FooniController>(m_Target));
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Change Y By: FooniController が見つかりません。");
                return m_Exit;
            }

            var delta = flow.GetValue<float>(m_DeltaY);
            controller.AddPositionPixels(new Vector2(0f, delta));
            return m_Exit;
        }
    }
}
