// Updated: 2025-10-19
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「x座標を～に、y座標を～にする」ブロックを再現し、絶対座標を設定するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により Unit 内で自動解決します。
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

        /// <summary>x ポートへの参照を公開します。</summary>
        public ValueInput X => m_X;

        /// <summary>y ポートへの参照を公開します。</summary>
        public ValueInput Y => m_Y;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と x/y の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_X = ValueInput<float>("x", 0f);
            m_Y = ValueInput<float>("y", 0f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に <see cref="ActorPresenterAdapter"/> を自動解決して絶対座標を設定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveAdapter(flow);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Go To X,Y: ActorPresenterAdapter が未解決のため座標を設定できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            var logical = new Vector2(flow.GetValue<float>(m_X), flow.GetValue<float>(m_Y));
            var uiPosition = controller.ToUiPosition(logical);
            // Scratch モードでは Presenter 側でスケール済みサイズを考慮したクランプが適用される。
            controller.SetPositionPixels(uiPosition);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「x座標を〇ずつ変える」ブロックを再現し、X 座標へ差分を加算するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により Unit 内で自動解決します。
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

        /// <summary>X 座標の差分（px）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaX;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>dx ポートへの参照を公開します。</summary>
        public ValueInput DeltaX => m_DeltaX;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と dx の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_DeltaX = ValueInput<float>("dx", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に <see cref="ActorPresenterAdapter"/> を自動解決し、X 座標へ差分を加算します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveAdapter(flow);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Change X By: ActorPresenterAdapter が未解決のため移動できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            var delta = flow.GetValue<float>(m_DeltaX);
            var uiDelta = controller.ToUiDelta(new Vector2(delta, 0f));
            // Scratch モードでは Presenter 側で中心座標をクランプするため、ここではそのまま転送する。
            controller.AddPositionPixels(uiDelta);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「y座標を〇ずつ変える」ブロックを再現し、Y 座標へ差分を加算するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により Unit 内で自動解決します。
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

        /// <summary>Y 座標の差分（px）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaY;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>dy ポートへの参照を公開します。</summary>
        public ValueInput DeltaY => m_DeltaY;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と dy の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_DeltaY = ValueInput<float>("dy", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に <see cref="ActorPresenterAdapter"/> を自動解決し、Y 座標へ差分を加算します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveAdapter(flow);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Change Y By: ActorPresenterAdapter が未解決のため移動できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            var delta = flow.GetValue<float>(m_DeltaY);
            var uiDelta = controller.ToUiDelta(new Vector2(0f, delta));
            // Scratch モードでは Presenter が移動後に中心座標をクランプする。
            controller.AddPositionPixels(uiDelta);
            yield return m_Exit;
        }
    }
}
