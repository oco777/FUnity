// Updated: 2025-10-19
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇歩動かす」ブロックに対応し、現在の向きに沿ってピクセル移動を指示するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により Unit 内で自動解決します。
    /// </summary>
    [UnitTitle("Scratch/Move Steps")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class MoveStepsUnit : Unit
    {
        /// <summary>同一フレームで反射を再試行する最大回数です。</summary>
        private const int MaxBounceIterations = 4;

        /// <summary>フローの開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>移動する歩数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Steps;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>steps ポートへの参照を公開します。</summary>
        public ValueInput Steps => m_Steps;

        /// <summary>
        /// ポートの定義を行い、enter→exit の制御線と steps の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Steps = ValueInput<float>("steps", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter を受け取った際に <see cref="ActorPresenterAdapter"/> を自動解決し、指定歩数分の移動を実行します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var controller = ScratchUnitUtil.ResolveAdapter(flow);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Move Steps: ActorPresenterAdapter が見つからないため移動できません。VSPresenterBridge などからアダプタを登録してください。");
                yield return m_Exit;
                yield break;
            }

            if (controller.Presenter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Move Steps: ActorPresenter が未設定のため移動できません。");
                yield return m_Exit;
                yield break;
            }

            var steps = flow.GetValue<float>(m_Steps);
            var movePixels = ScratchUnitUtil.StepsToPixels(steps);
            if (Mathf.Approximately(movePixels, 0f))
            {
                yield return m_Exit;
                yield break;
            }

            var presenter = controller.Presenter;
            var view = controller.ActorView;
            var rootScaledSize = view != null ? view.GetRootScaledSizePx() : Vector2.zero;
            var currentCenter = presenter.GetPosition();
            var directionDeg = controller.GetDirection();
            var uiDirection = ScratchUnitUtil.DirFromDegrees(directionDeg);
            if (uiDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                uiDirection = new Vector2(0f, -1f);
            }

            uiDirection.Normalize();
            if (movePixels < 0f)
            {
                uiDirection = -uiDirection;
                movePixels = -movePixels;
            }

            var remaining = movePixels;
            var hasBounced = false;
            var nextDirectionDeg = directionDeg;
            var logicalDirection = new Vector2(uiDirection.x, -uiDirection.y);
            logicalDirection.Normalize();

            for (var i = 0; i < MaxBounceIterations && remaining > Mathf.Epsilon; i++)
            {
                var targetCenter = currentCenter + logicalDirection * remaining;
                if (ScratchHitTestUtil.IsCenterInsideStage(targetCenter))
                {
                    currentCenter = targetCenter;
                    remaining = 0f;
                    break;
                }

                var travelLogical = ScratchUnitUtil.ComputeTravelToStageEdge(currentCenter, uiDirection * remaining, rootScaledSize, out _);
                if (travelLogical.sqrMagnitude <= Mathf.Epsilon)
                {
                    break;
                }

                var traveled = travelLogical.magnitude;
                currentCenter += travelLogical;
                remaining = Mathf.Max(0f, remaining - traveled);

                var bouncedDirection = ScratchUnitUtil.BounceDirectionAndClamp(currentCenter, uiDirection, rootScaledSize, out var clampedCenter);
                var directionChanged = (bouncedDirection - uiDirection).sqrMagnitude > 1e-4f;
                currentCenter = clampedCenter;

                if (directionChanged)
                {
                    hasBounced = true;
                    uiDirection = bouncedDirection;
                    logicalDirection = new Vector2(uiDirection.x, -uiDirection.y);
                    logicalDirection.Normalize();
                    nextDirectionDeg = ScratchUnitUtil.DegreesFromUiDirection(uiDirection);
                    continue;
                }

                break;
            }

            var finalUi = controller.ToUiPosition(currentCenter);
            controller.SetPositionPixels(finalUi);

            if (hasBounced)
            {
                controller.SetDirection(nextDirectionDeg);
            }

            yield return m_Exit;
        }
    }
}
