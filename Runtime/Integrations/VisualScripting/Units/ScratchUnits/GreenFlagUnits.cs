using System.Collections;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;
using UnityEngine;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「イベント/緑の旗が押されたとき」を Visual Scripting 上で再現する EventUnit です。
    /// </summary>
    [UnitTitle("緑の旗が押されたとき")]
    [UnitShortTitle("緑の旗")]
    [UnitCategory("Events/FUnity/Scratch/イベント")]
    [UnitSubtitle("funity scratch イベント green flag start 押された when")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class WhenGreenFlagClickedUnit : EventUnit<EmptyEventArgs>
    {
        /// <summary>Visual Scripting 側でイベント登録を行うかどうかを制御します。</summary>
        protected override bool register => true;

        /// <summary>
        /// GreenFlag イベントをコルーチンとして実行し、スクリプトスレッドを登録します。
        /// ScriptMachine が見つからない場合は既存の実装にフォールバックします。
        /// </summary>
        /// <param name="reference">現在処理中のグラフ参照。</param>
        /// <param name="args">空のイベント引数。</param>
        public new void Trigger(GraphReference reference, EmptyEventArgs args)
        {
            var machine = reference?.machine as ScriptMachine;
            if (machine == null)
            {
                base.Trigger(reference, args);
                return;
            }

            var flow = Flow.New(reference);
            AssignArguments(flow, args);
            if (!ShouldTrigger(flow, args))
            {
                flow.Dispose();
                return;
            }

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            var graph = machine.nest?.source as ScriptGraphAsset;

            IEnumerator Routine()
            {
                using (flow)
                {
                    flow.Invoke(trigger);
                }
            }

            var coroutine = machine.StartCoroutine(Routine());
            ScratchUnitUtil.EnsureScratchThreadRegistered(flow, adapter, graph, coroutine);
        }

        /// <summary>
        /// Runner（self）をターゲットとする EventHook を返し、同一 Runner からの発火のみ受け付けます。
        /// </summary>
        /// <param name="reference">現在処理中のグラフ参照。</param>
        /// <returns>緑の旗イベント用の EventHook。</returns>
        public override EventHook GetHook(GraphReference reference)
        {
            return new EventHook(FUnityEventNames.OnGreenFlag, reference.self);
        }

        /// <summary>
        /// EventUnit 側で定義済みの trigger ポートを活用するため、基底実装のみを呼び出します。
        /// </summary>
        protected override void Definition()
        {
            base.Definition();
        }

        /// <summary>
        /// イベントに付随する引数は存在しないため、何も行いません。
        /// </summary>
        /// <param name="flow">実行中のフロー。</param>
        /// <param name="args">空のイベント引数。</param>
        protected override void AssignArguments(Flow flow, EmptyEventArgs args)
        {
        }

        /// <summary>
        /// 発火条件はトリガー側で制御するため、常に true を返します。
        /// </summary>
        /// <param name="flow">実行中のフロー。</param>
        /// <param name="args">空のイベント引数。</param>
        /// <returns>常に true。</returns>
        protected override bool ShouldTrigger(Flow flow, EmptyEventArgs args)
        {
            return true;
        }
    }
}
