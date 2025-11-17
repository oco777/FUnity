using System.Collections;
using System;
using System.Collections.Generic;
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
        /// <summary>GraphReference ごとに EventBus へ登録したハンドラを保持します。</summary>
        private static readonly Dictionary<GraphReference, Action<EmptyEventArgs>> s_Handlers
            = new Dictionary<GraphReference, Action<EmptyEventArgs>>();

        /// <summary>Visual Scripting 側でイベント登録を行うかどうかを制御します。</summary>
        protected override bool register => true;

        /// <summary>イベントデータを生成します。</summary>
        /// <returns>EventUnit 既定のデータ構造。</returns>
        public override IGraphElementData CreateData()
        {
            return new Data();
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

        /// <summary>
        /// グラフ有効化時に EventBus へリッスンを登録し、Scratch スレッド登録経由でフローを実行します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StartListening(GraphStack stack)
        {
            if (stack == null)
            {
                return;
            }

            var reference = stack.ToReference();

            if (s_Handlers.ContainsKey(reference))
            {
                return;
            }

            var hook = GetHook(reference);
            Action<EmptyEventArgs> handler = args => TriggerWithThreadRegistration(reference, args);

            if (hook.name != null && handler != null)
            {
                EventBus.Register<EmptyEventArgs>(hook, handler);
                s_Handlers[reference] = handler;
            }
        }

        /// <summary>
        /// グラフ無効化時に EventBus から登録を解除します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StopListening(GraphStack stack)
        {
            if (stack == null)
            {
                return;
            }

            var reference = stack.ToReference();

            if (!s_Handlers.TryGetValue(reference, out var handler) || handler == null)
            {
                return;
            }

            var hook = GetHook(reference);
            if (hook.name != null)
            {
                EventBus.Unregister(hook, handler);
            }

            s_Handlers.Remove(reference);
        }

        /// <summary>
        /// 緑の旗イベントでグラフを発火し、開始したコルーチンを Scratch スレッドとして登録します。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <param name="args">空のイベント引数。</param>
        private void TriggerWithThreadRegistration(GraphReference reference, EmptyEventArgs args)
        {
            var flow = Flow.New(reference);
            var routine = RunEventCoroutine(flow, args);
            ScratchUnitUtil.StartScratchCoroutine(flow, routine);
        }

        /// <summary>
        /// EventUnit 標準のフロー実行をコルーチンでラップし、Flow のライフサイクルは Visual Scripting 側に委ねます。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <param name="args">イベント引数。</param>
        /// <returns>実行完了までの列挙子。</returns>
        private IEnumerator RunEventCoroutine(Flow flow, EmptyEventArgs args)
        {
            if (!ShouldTrigger(flow, args))
            {
                yield break;
            }

            AssignArguments(flow, args);

            flow.StartCoroutine(trigger);
            yield break;
        }
    }
}
