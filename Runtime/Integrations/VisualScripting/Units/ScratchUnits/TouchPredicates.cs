using System;
using Unity.VisualScripting;
using UnityEngine;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「マウスポインターに触れた？」ブロックを再現し、俳優矩形にマウス座標が含まれるかを返す Unit です。
    /// </summary>
    [UnitTitle("Touching Mouse Pointer?")]
    [UnitCategory("Scratch/Sensing")]
    public sealed class TouchingMousePointerPredicateUnit : Unit
    {
        /// <summary>判定結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 出力ポートを構築し、フロー評価時に当たり判定を実行する設定を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Result = ValueOutput<bool>(nameof(Result), Evaluate);
        }

        /// <summary>
        /// マウスポインターと俳優矩形の接触判定を行います。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>接触している場合は true。</returns>
        private bool Evaluate(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Touching Mouse Pointer?: ActorPresenterAdapter が未解決のため判定できません。");
                return false;
            }

            if (!ScratchHitTestUtil.TryGetActorWorldRect(adapter, out var actorRect))
            {
                return false;
            }

            var pointer = ScratchHitTestUtil.GetMousePanelPosition(adapter);
            return actorRect.Contains(pointer);
        }
    }

    /// <summary>
    /// Scratch の「端に触れた？」ブロックを再現し、俳優矩形がステージ境界へ接触しているかを返す Unit です。
    /// </summary>
    [UnitTitle("Touching Edge?")]
    [UnitCategory("Scratch/Sensing")]
    public sealed class TouchingEdgePredicateUnit : Unit
    {
        /// <summary>判定結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 出力ポートを構築し、評価時にステージ境界との接触を確認します。
        /// </summary>
        protected override void Definition()
        {
            m_Result = ValueOutput<bool>(nameof(Result), Evaluate);
        }

        /// <summary>
        /// ステージ矩形と俳優矩形の境界接触を判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>接触している場合は true。</returns>
        private bool Evaluate(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Touching Edge?: ActorPresenterAdapter が未解決のため判定できません。");
                return false;
            }

            if (!ScratchHitTestUtil.TryGetActorWorldRect(adapter, out var actorRect))
            {
                return false;
            }

            var stageRect = ScratchHitTestUtil.GetStageWorldRect(adapter);
            const float epsilon = 0.001f;

            var touchesLeft = actorRect.xMin <= stageRect.xMin + epsilon;
            var touchesRight = actorRect.xMax >= stageRect.xMax - epsilon;
            var touchesTop = actorRect.yMin <= stageRect.yMin + epsilon;
            var touchesBottom = actorRect.yMax >= stageRect.yMax - epsilon;

            return touchesLeft || touchesRight || touchesTop || touchesBottom;
        }
    }

    /// <summary>
    /// Scratch の「◯◯に触れた？」ブロックを再現し、DisplayName で指定した俳優（本体＋クローン）のうち可視なものとの矩形重なりを判定する Unit です。
    /// </summary>
    [UnitTitle("Touching Actor By DisplayName?")]
    [UnitCategory("Scratch/Sensing")]
    public sealed class TouchingActorByDisplayNamePredicateUnit : Unit
    {
        /// <summary>Self を表す俳優キーを受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_SelfActorKey;

        /// <summary>対象俳優の DisplayName を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_TargetDisplayName;

        /// <summary>接触判定結果を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// 入出力ポートを構築し、Self と DisplayName の取得を必須とする依存関係を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_SelfActorKey = ValueInput<string>("self", "Self");
            m_TargetDisplayName = ValueInput<string>("displayName", string.Empty);
            m_Result = ValueOutput<bool>("result", Evaluate);

            Requirement(m_SelfActorKey, m_Result);
            Requirement(m_TargetDisplayName, m_Result);
        }

        /// <summary>
        /// 指定された俳優群と自分自身が矩形的に接触しているかを判定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>接触している場合は true。</returns>
        private bool Evaluate(Flow flow)
        {
            var selfKeyRaw = flow.GetValue<string>(m_SelfActorKey);
            if (!TryResolveActorKey(flow, selfKeyRaw, out var selfKey))
            {
                return false;
            }

            if (!ScratchHitTestUtil.TryGetVisibleRect(selfKey, out var selfRect))
            {
                return false;
            }

            var targetName = flow.GetValue<string>(m_TargetDisplayName);
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return false;
            }

            foreach (var otherKey in ScratchHitTestUtil.EnumerateVisibleActorKeysByDisplayName(targetName))
            {
                if (string.Equals(otherKey, selfKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!ScratchHitTestUtil.TryGetVisibleRect(otherKey, out var otherRect))
                {
                    continue;
                }

                if (ScratchHitTestUtil.OverlapsAABB(selfRect, otherRect))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Flow から俳優キーを解決します。Self 指定時は Runner の Object Variables やアダプタを参照します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="inputKey">ポートから受け取ったキー文字列。</param>
        /// <param name="actorKey">解決した俳優キー。</param>
        /// <returns>解決に成功した場合は <c>true</c>。</returns>
        private static bool TryResolveActorKey(Flow flow, string inputKey, out string actorKey)
        {
            actorKey = null;
            if (!string.IsNullOrWhiteSpace(inputKey))
            {
                var normalized = inputKey.Trim();
                if (!string.Equals(normalized, "Self", StringComparison.OrdinalIgnoreCase))
                {
                    actorKey = normalized;
                    return true;
                }
            }

            var objectVariables = ScratchUnitUtil.GetObjectVars(flow);
            if (objectVariables != null)
            {
                if (objectVariables.IsDefined("actorKey"))
                {
                    var value = objectVariables.Get("actorKey");
                    if (value is string key && !string.IsNullOrEmpty(key))
                    {
                        actorKey = key;
                        return true;
                    }
                }

                if (objectVariables.IsDefined("selfActorKey"))
                {
                    var value = objectVariables.Get("selfActorKey");
                    if (value is string key && !string.IsNullOrEmpty(key))
                    {
                        actorKey = key;
                        return true;
                    }
                }

                if (objectVariables.IsDefined("presenter") && objectVariables.Get("presenter") is ActorPresenter presenterFromVars && presenterFromVars != null)
                {
                    actorKey = presenterFromVars.ActorKey;
                    return true;
                }

                if (objectVariables.IsDefined("selfPresenter") && objectVariables.Get("selfPresenter") is ActorPresenter selfPresenter && selfPresenter != null)
                {
                    actorKey = selfPresenter.ActorKey;
                    return true;
                }
            }

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter != null)
            {
                var presenter = adapter.Presenter;
                if (presenter != null)
                {
                    actorKey = presenter.ActorKey;
                    return true;
                }
            }

            return false;
        }
    }
}
