// Updated: 2025-10-19
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇度回す」ブロックを再現し、方向を相対的に更新するとともに Presenter 経由で UI 回転を適用するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により Unit 内で自動解決します。
    /// </summary>
    [UnitTitle("○度回す")]
    [UnitCategory("FUnity/Blocks/動き")]
    [UnitSubtitle("funity scratch 動き turn rotate degrees 回す 向き")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class TurnDegreesUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>加算する角度（度）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaDegrees;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>deltaDegrees ポートへの参照を公開します。</summary>
        public ValueInput DeltaDegrees => m_DeltaDegrees;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と deltaDegrees の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_DeltaDegrees = ValueInput<float>("deltaDegrees", 15f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に <see cref="ActorPresenterAdapter"/> を自動解決し、現在角度へ増減を加えます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var deltaScratchDeg = flow.GetValue<float>(m_DeltaDegrees);

            var presenterObj = ScratchUnitUtil.GetPresenter(flow);
            if (presenterObj != null)
            {
                VSPresenterBridge.TurnSelf(presenterObj, deltaScratchDeg);
            }
            else
            {
                var bridge = VSPresenterBridge.Instance;
                if (bridge != null)
                {
                    bridge.TurnDegrees(deltaScratchDeg);
                }
                else
                {
                    Debug.LogWarning("[FUnity] Scratch/Turn Degrees: VSPresenterBridge.Instance が未設定のため UI 回転を適用できません。FUnityManager がブリッジを初期化しているか確認してください。");
                }
            }

            var controller = ScratchUnitUtil.ResolveAdapter(flow);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Turn Degrees: ActorPresenterAdapter が未解決のため向きを更新できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            var currentInternal = controller.GetDirection();
            var currentForMode = FUnityModeUtil.IsScratchMode
                ? ScratchAngleUtil.InternalToScratch(currentInternal)
                : currentInternal;
            var nextForMode = currentForMode + deltaScratchDeg;
            var nextInternal = FUnityModeUtil.IsScratchMode
                ? ScratchAngleUtil.ScratchToInternal(nextForMode)
                : nextForMode;
            controller.SetDirection(nextInternal);
            yield return m_Exit;
        }

    }

    /// <summary>
    /// Scratch の「〇度に向ける」ブロックを再現し、絶対角度を設定するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により Unit 内で自動解決します。
    /// </summary>
    [UnitTitle("○度に向ける")]
    [UnitCategory("FUnity/Blocks/動き")]
    [UnitSubtitle("funity scratch 動き point direction degrees 向ける 向き")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class PointDirectionUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>設定する角度（度）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Degrees;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>degrees ポートへの参照を公開します。</summary>
        public ValueInput Degrees => m_Degrees;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と degrees の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Degrees = ValueInput<float>("degrees", 90f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter を受信した際に <see cref="ActorPresenterAdapter"/> を自動解決し、角度を絶対値で設定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveAdapter(flow);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Point Direction: ActorPresenterAdapter が未解決のため向きを設定できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            var scratchDeg = flow.GetValue<float>(m_Degrees);
            var internalDeg = FUnityModeUtil.IsScratchMode
                ? ScratchAngleUtil.ScratchToInternal(scratchDeg)
                : scratchDeg;
            controller.SetDirection(internalDeg);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「マウスポインターへ向ける」ブロックを再現し、俳優の向きをカーソル方向へ更新する Unit です。
    /// </summary>
    [UnitTitle("マウスポインターへ向ける")]
    [UnitCategory("FUnity/Blocks/動き")]
    [UnitSubtitle("funity scratch 動き point towards mouse pointer 向ける マウス")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class PointTowardsMousePointerUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// マウス位置と現在位置の差分から角度を算出し、俳優の向きを更新します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var provider = ScratchUnitUtil.ResolveMouseProvider();
            if (provider == null)
            {
                yield return m_Exit;
                yield break;
            }

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Point Towards Mouse Pointer: ActorPresenterAdapter が未解決のため向きを更新できません。");
                yield return m_Exit;
                yield break;
            }

            var presenter = adapter.Presenter;
            if (presenter == null)
            {
                yield return m_Exit;
                yield break;
            }

            var center = ScratchUnitUtil.ClampToStageBounds(presenter.GetPosition());
            var mouse = ScratchUnitUtil.ClampToStageBounds(provider.StagePosition);
            var delta = mouse - center;
            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                yield return m_Exit;
                yield break;
            }

            var directionForMode = ScratchUnitUtil.GetDirectionDegreesForCurrentMode(delta);
            var internalDeg = FUnityModeUtil.IsScratchMode
                ? ScratchAngleUtil.ScratchToInternal(directionForMode)
                : directionForMode;
            adapter.SetDirection(internalDeg);
            yield return m_Exit;
        }
    }
}
