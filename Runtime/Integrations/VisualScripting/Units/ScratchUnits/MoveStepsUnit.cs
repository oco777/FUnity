// Updated: 2025-10-19
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
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
        /// <summary>Scratch の 1 歩を UI ピクセルへ変換する倍率です。</summary>
        private const float StepToPixels = 1f;

        /// <summary>同一フレームで反射を再試行する最大回数です。</summary>
        private const int MaxBounceIterations = 4;

        /// <summary>反射後にステージ内へ押し戻す余白（px）です。</summary>
        private const float BounceEpsilon = 0.5f;

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

            if (!ScratchUnitUtil.TryGetActorWorldRect(controller, out var actorRect))
            {
                yield return null;

                if (!ScratchUnitUtil.TryGetActorWorldRect(controller, out actorRect))
                {
                    Debug.LogWarning("[FUnity] Scratch/Move Steps: worldBound が未確定のため移動をスキップしました。");
                    yield return m_Exit;
                    yield break;
                }
            }

            var stageRect = ScratchHitTestUtil.GetStageWorldRect(controller);
            if (stageRect.width <= 0f || stageRect.height <= 0f)
            {
                var fallbackStage = ScratchBounds.GetStageRect();
                stageRect = new Rect(stageRect.position, fallbackStage.size);
            }

            var steps = flow.GetValue<float>(m_Steps);
            var movePixels = steps * StepToPixels;
            if (Mathf.Approximately(movePixels, 0f))
            {
                yield return m_Exit;
                yield break;
            }

            var halfSize = new Vector2(Mathf.Max(0f, actorRect.width * 0.5f), Mathf.Max(0f, actorRect.height * 0.5f));
            var stageOrigin = stageRect.position;
            var currentCenter = actorRect.center;
            var directionDeg = controller.GetDirection();
            var uiDirection = ScratchUnitUtil.DirFromDegrees(directionDeg);
            if (uiDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                uiDirection = Vector2.right;
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

            for (var i = 0; i < MaxBounceIterations && remaining > Mathf.Epsilon; i++)
            {
                var targetCenter = currentCenter + uiDirection * remaining;
                if (ScratchUnitUtil.IsCenterInsideStage(targetCenter, stageRect, halfSize))
                {
                    currentCenter = targetCenter;
                    remaining = 0f;
                    break;
                }

                var travel = ScratchUnitUtil.ComputeTravelToStageEdge(currentCenter, uiDirection, stageRect, halfSize);
                if (travel <= 0f)
                {
                    break;
                }

                travel = Mathf.Min(travel, remaining);
                currentCenter += uiDirection * travel;
                remaining -= travel;

                if (ScratchUnitUtil.BounceDirectionAndClamp(ref currentCenter, ref uiDirection, stageRect, halfSize, BounceEpsilon))
                {
                    hasBounced = true;
                    nextDirectionDeg = ScratchUnitUtil.DegreesFromUiDirection(uiDirection);
                    continue;
                }

                break;
            }

            var finalLocal = currentCenter - stageOrigin;
            controller.SetPositionPixels(finalLocal);

            if (hasBounced)
            {
                controller.SetDirection(nextDirectionDeg);
            }

            yield return m_Exit;
        }
    }
}
