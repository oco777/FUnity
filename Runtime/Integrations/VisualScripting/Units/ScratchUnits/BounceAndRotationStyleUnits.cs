using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Model;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「もし端に着いたら、跳ね返る」を再現し、端接触時に進行方向を反射させる制御ユニットです。
    /// </summary>
    [UnitTitle("Scratch/Bounce If On Edge")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class BounceIfOnEdgeUnit : Unit
    {
        /// <summary>反射後に中心座標を押し戻す余白（px）です。</summary>
        private const float BounceEpsilon = 0.5f;

        /// <summary>enter ポートを受け取る制御入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す制御出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 端接触を検知して進行方向を反射させ、ステージ内へ押し戻します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit へ制御を返す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Bounce If On Edge: ActorPresenterAdapter が未解決のため反射できません。");
                yield return m_Exit;
                yield break;
            }

            if (!ScratchUnitUtil.TryGetActorWorldRect(adapter, out var actorRect))
            {
                yield return null;

                if (!ScratchUnitUtil.TryGetActorWorldRect(adapter, out actorRect))
                {
                    Debug.LogWarning("[FUnity] Scratch/Bounce If On Edge: worldBound が未確定のため、判定をスキップしました。");
                    yield return m_Exit;
                    yield break;
                }
            }

            var stageRect = ScratchHitTestUtil.GetStageWorldRect(adapter);
            if (stageRect.width <= 0f || stageRect.height <= 0f)
            {
                var fallback = ScratchBounds.GetStageRect();
                stageRect = new Rect(stageRect.position, fallback.size);
            }

            var halfSize = new Vector2(Mathf.Max(0f, actorRect.width * 0.5f), Mathf.Max(0f, actorRect.height * 0.5f));
            var centerWorld = actorRect.center;
            if (!ScratchUnitUtil.IsTouchingStageEdge(centerWorld, stageRect, halfSize, BounceEpsilon))
            {
                yield return m_Exit;
                yield break;
            }

            var uiDirection = ScratchUnitUtil.DirFromDegrees(adapter.GetDirection());
            if (uiDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                uiDirection = Vector2.right;
            }
            else
            {
                uiDirection.Normalize();
            }

            if (!ScratchUnitUtil.BounceDirectionAndClamp(ref centerWorld, ref uiDirection, stageRect, halfSize, BounceEpsilon))
            {
                yield return m_Exit;
                yield break;
            }

            var reflectedDeg = ScratchUnitUtil.DegreesFromUiDirection(uiDirection);
            var stageOrigin = stageRect.position;
            var finalLocal = centerWorld - stageOrigin;

            adapter.SetDirection(reflectedDeg);
            adapter.SetPositionPixels(finalLocal);

            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「回転方向を左右のみにする」を再現し、Presenter の回転スタイルを左右反転限定へ切り替える設定ユニットです。
    /// </summary>
    [UnitTitle("Scratch/Set Rotation Style: Left-Right")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class SetRotationStyleLeftRightUnit : Unit
    {
        /// <summary>enter ポートを受け取る制御入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す制御出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力を受け取り、対象俳優の回転スタイルを左右反転のみへ切り替えます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Set Rotation Style: Left-Right: ActorPresenterAdapter が未解決のため設定できません。");
                return m_Exit;
            }

            adapter.SetRotationStyle(RotationStyle.LeftRight);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「回転方向を回転しないにする」を再現し、俳優を常に直立表示へ切り替える設定ユニットです。
    /// </summary>
    [UnitTitle("Scratch/Set Rotation Style: Don't Rotate")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class SetRotationStyleDontRotateUnit : Unit
    {
        /// <summary>enter ポートを受け取る制御入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す制御出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力を受け取り、対象俳優を常に直立表示する回転スタイルへ切り替えます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Set Rotation Style: Don't Rotate: ActorPresenterAdapter が未解決のため設定できません。");
                return m_Exit;
            }

            adapter.SetRotationStyle(RotationStyle.DontRotate);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「回転方向を自由に回転にする」を再現し、俳優が任意の角度で回転できる状態へ戻す設定ユニットです。
    /// </summary>
    [UnitTitle("Scratch/Set Rotation Style: All Around")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class SetRotationStyleAllAroundUnit : Unit
    {
        /// <summary>enter ポートを受け取る制御入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す制御出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力を受け取り、俳優の回転スタイルを全方向回転可能な状態へ戻します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Set Rotation Style: All Around: ActorPresenterAdapter が未解決のため設定できません。");
                return m_Exit;
            }

            adapter.SetRotationStyle(RotationStyle.AllAround);
            return m_Exit;
        }
    }
}
