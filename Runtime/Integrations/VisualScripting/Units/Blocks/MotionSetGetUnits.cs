// Updated: 2025-11-14
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.Blocks
{
    /// <summary>
    /// Scratch の「x座標を〇にする」ブロックを再現し、X 座標を絶対値で設定する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("x座標を〇にする")]
    [UnitShortTitle("x座標を〇にする")]
    [UnitSubtitle("動き")]
    [UnitCategory("FUnity/Blocks/動き")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetXToUnit : Unit
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

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>x ポートへの参照を公開します。</summary>
        public ValueInput X => m_X;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と x の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_X = ValueInput<float>("x", 0f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に ActorPresenter およびアダプタを解決し、X 座標を絶対値で設定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] SetXToUnit: ActorPresenterAdapter が未解決のため座標を設定できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            var presenter = ScratchUnitUtil.ResolveActorPresenter(flow, adapter);
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] SetXToUnit: ActorPresenter を取得できなかったため座標を設定できません。");
                yield return m_Exit;
                yield break;
            }

            var logical = presenter.GetPosition();
            logical.x = flow.GetValue<float>(m_X);
            var uiPosition = adapter.ToUiPosition(logical);
            adapter.SetPositionPixels(uiPosition);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「y座標を〇にする」ブロックを再現し、Y 座標を絶対値で設定する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("y座標を〇にする")]
    [UnitShortTitle("y座標を〇にする")]
    [UnitSubtitle("動き")]
    [UnitCategory("FUnity/Blocks/動き")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetYToUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>Y 座標（px）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Y;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>y ポートへの参照を公開します。</summary>
        public ValueInput Y => m_Y;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と y の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Y = ValueInput<float>("y", 0f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に ActorPresenter およびアダプタを解決し、Y 座標を絶対値で設定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] SetYToUnit: ActorPresenterAdapter が未解決のため座標を設定できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            var presenter = ScratchUnitUtil.ResolveActorPresenter(flow, adapter);
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] SetYToUnit: ActorPresenter を取得できなかったため座標を設定できません。");
                yield return m_Exit;
                yield break;
            }

            var logical = presenter.GetPosition();
            logical.y = flow.GetValue<float>(m_Y);
            var uiPosition = adapter.ToUiPosition(logical);
            adapter.SetPositionPixels(uiPosition);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「x座標」ブロックを再現し、現在の X 座標を取得する Value Unit です。
    /// </summary>
    [UnitTitle("x座標")]
    [UnitShortTitle("x座標")]
    [UnitSubtitle("動き")]
    [UnitCategory("FUnity/Blocks/動き")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class GetXPositionUnit : Unit
    {
        /// <summary>X 座標を出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_X;

        /// <summary>x 出力ポートへの参照を公開します。</summary>
        public ValueOutput X => m_X;

        /// <summary>
        /// ポート定義を行い、X 座標を返す ValueOutput を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_X = ValueOutput<float>("x", GetX);
        }

        /// <summary>
        /// 現在の論理座標から X 値を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>現在の X 座標。</returns>
        private float GetX(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            var presenter = ScratchUnitUtil.ResolveActorPresenter(flow, adapter);
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] GetXPositionUnit: ActorPresenter を取得できなかったため 0 を返します。");
                return 0f;
            }

            return presenter.GetPosition().x;
        }
    }

    /// <summary>
    /// Scratch の「y座標」ブロックを再現し、現在の Y 座標を取得する Value Unit です。
    /// </summary>
    [UnitTitle("y座標")]
    [UnitShortTitle("y座標")]
    [UnitSubtitle("動き")]
    [UnitCategory("FUnity/Blocks/動き")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class GetYPositionUnit : Unit
    {
        /// <summary>Y 座標を出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_Y;

        /// <summary>y 出力ポートへの参照を公開します。</summary>
        public ValueOutput Y => m_Y;

        /// <summary>
        /// ポート定義を行い、Y 座標を返す ValueOutput を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Y = ValueOutput<float>("y", GetY);
        }

        /// <summary>
        /// 現在の論理座標から Y 値を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>現在の Y 座標。</returns>
        private float GetY(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            var presenter = ScratchUnitUtil.ResolveActorPresenter(flow, adapter);
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] GetYPositionUnit: ActorPresenter を取得できなかったため 0 を返します。");
                return 0f;
            }

            return presenter.GetPosition().y;
        }
    }

    /// <summary>
    /// Scratch の「向き」ブロックを再現し、現在の向きを取得する Value Unit です。
    /// </summary>
    [UnitTitle("向き")]
    [UnitShortTitle("向き")]
    [UnitSubtitle("動き")]
    [UnitCategory("FUnity/Blocks/動き")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class GetDirectionUnit : Unit
    {
        /// <summary>向きを出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_Direction;

        /// <summary>direction 出力ポートへの参照を公開します。</summary>
        public ValueOutput Direction => m_Direction;

        /// <summary>
        /// ポート定義を行い、向きを返す ValueOutput を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Direction = ValueOutput<float>("direction", GetDirection);
        }

        /// <summary>
        /// 現在の向きを Scratch 互換の角度で取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>Scratch の向きルールに基づく角度（度）。</returns>
        private float GetDirection(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] GetDirectionUnit: ActorPresenterAdapter が未解決のため 0 を返します。");
                return 0f;
            }

            var internalDirection = adapter.GetDirection();
            return FUnityModeUtil.IsScratchMode
                ? ScratchAngleUtil.InternalToScratch(internalDirection)
                : internalDirection;
        }
    }
}
