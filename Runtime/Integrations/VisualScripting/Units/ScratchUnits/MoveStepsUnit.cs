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
    /// Scratch の「〇歩動かす」ブロックに対応し、現在の向きに沿ってピクセル移動を指示するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により Unit 内で自動解決します。
    /// </summary>
    [UnitTitle("○歩動かす")]
    [UnitCategory("FUnity/Scratch/動き")]
    [UnitSubtitle("funity scratch 動き move steps 歩 進む")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
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
            if (!ScratchUnitUtil.TryGetActorPresenter(flow, out var presenter) || presenter == null)
            {
                Debug.LogWarning("[FUnity] MoveStepsUnit: ActorPresenter を取得できませんでした。");
                yield return m_Exit;
                yield break;
            }

            var controller = ScratchUnitUtil.ResolveAdapter(flow);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] MoveStepsUnit: ActorPresenterAdapter を解決できませんでした。");
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

            var view = controller.ActorView;
            var rootScaledSize = view != null ? view.GetRootScaledSizePx() : Vector2.zero;
            var currentCenter = presenter.GetPosition();
            var directionInternal = controller.GetDirection();
            var directionForMode = FUnityModeUtil.IsScratchMode
                ? ScratchAngleUtil.InternalToScratch(directionInternal)
                : directionInternal;
            var uiDirection = ScratchUnitUtil.DirFromDegrees(directionForMode);
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
            var nextDirectionInternal = directionInternal;
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
                    var nextDirectionForMode = ScratchUnitUtil.DegreesFromUiDirection(uiDirection);
                    nextDirectionInternal = FUnityModeUtil.IsScratchMode
                        ? ScratchAngleUtil.ScratchToInternal(nextDirectionForMode)
                        : nextDirectionForMode;
                    continue;
                }

                break;
            }

            var finalUi = controller.ToUiPosition(currentCenter);
            controller.SetPositionPixels(finalUi);

            if (hasBounced)
            {
                controller.SetDirection(nextDirectionInternal);
            }

            yield return m_Exit;
        }
    }
}
