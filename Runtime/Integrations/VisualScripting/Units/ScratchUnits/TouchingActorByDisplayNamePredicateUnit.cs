using System;
using Unity.VisualScripting;
using UnityEngine;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「◯◯に触れた？」ブロックを再現し、Graph のコンテキストから取得した自分自身と、指定 DisplayName の可視俳優（本体＋クローン）との接触を判定する Unit です。
    /// </summary>
    [UnitTitle("○○に触れた？")]
    [UnitCategory("FUnity/Scratch/調べる")]
    [UnitSubtitle("funity scratch 調べる touch actor クローン display name 触れた")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class TouchingActorByDisplayNamePredicateUnit : Unit
    {
        /// <summary>当たり判定対象となる俳優 DisplayName を受け取る入力ポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_TargetDisplayName;

        /// <summary>判定結果を出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>Graph から提供された DisplayName 入力ポートへの参照です。</summary>
        public ValueInput TargetDisplayName => m_TargetDisplayName;

        /// <summary>接触判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// displayName 入力のみを持つノードを定義し、評価時に当たり判定を実行するよう設定します。
        /// </summary>
        protected override void Definition()
        {
            m_TargetDisplayName = ValueInput<string>("displayName", string.Empty);
            m_Result = ValueOutput<bool>("result", Evaluate);

            Requirement(m_TargetDisplayName, m_Result);
        }

        /// <summary>
        /// 自身が可視であることを確認した上で、指定 DisplayName の可視俳優と矩形的に接触しているかを判定します。
        /// </summary>
        /// <param name="flow">現在評価中のフロー情報。</param>
        /// <returns>接触している場合は true。条件を満たさない場合は false。</returns>
        private bool Evaluate(Flow flow)
        {
            if (!TryGetSelfActorKey(flow, out var selfKey))
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
        /// Flow/Graph のコンテキストから自身の俳優キーを推定します。
        /// Object Variables に保存されたキーや Presenter 参照、ActorPresenterAdapter から取得を試み、最後に Bridge のフォールバックを利用します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="actorKey">取得した俳優キー。失敗時は空文字列。</param>
        /// <returns>俳優キーを解決できた場合は true。</returns>
        private bool TryGetSelfActorKey(Flow flow, out string actorKey)
        {
            actorKey = string.Empty;

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
                    var presenterKey = presenterFromVars.ActorKey;
                    if (!string.IsNullOrEmpty(presenterKey))
                    {
                        actorKey = presenterKey;
                        return true;
                    }
                }

                if (objectVariables.IsDefined("selfPresenter") && objectVariables.Get("selfPresenter") is ActorPresenter selfPresenter && selfPresenter != null)
                {
                    var presenterKey = selfPresenter.ActorKey;
                    if (!string.IsNullOrEmpty(presenterKey))
                    {
                        actorKey = presenterKey;
                        return true;
                    }
                }
            }

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter != null)
            {
                var presenter = adapter.Presenter;
                if (presenter != null && !string.IsNullOrEmpty(presenter.ActorKey))
                {
                    actorKey = presenter.ActorKey;
                    return true;
                }
            }

            var fallbackKey = VSPresenterBridge.GetSelfActorKey(flow);
            if (!string.IsNullOrEmpty(fallbackKey))
            {
                actorKey = fallbackKey;
                return true;
            }

            return false;
        }
    }
}
