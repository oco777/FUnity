using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Integrations.VisualScripting;
using UnityEngine;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「イベント/このスプライトが押されたとき」を
    /// Visual Scripting 上で再現する EventUnit です。
    ///
    /// 対象の Actor（スプライト）がクリックされた際に、
    /// その Actor 用の ScriptGraph を起動します。
    /// </summary>
    [UnitTitle("このスプライトが押されたとき")]
    [UnitShortTitle("スプライトクリック")]
    [UnitCategory("Events/FUnity/Blocks/イベント")]
    [UnitSubtitle("イベント")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class WhenThisSpriteClickedUnit : EventUnit<EmptyEventArgs>
    {
        /// <summary>
        /// GraphReference ごとに EventBus へ登録したハンドラを保持します。
        ///
        /// ・キー: GraphReference
        /// ・値: EventBus から呼び出されるコールバック
        /// </summary>
        private static readonly Dictionary<GraphReference, Action<EmptyEventArgs>> s_Handlers
            = new Dictionary<GraphReference, Action<EmptyEventArgs>>();

        /// <summary>
        /// EventUnit による EventBus への自動登録を有効にします。
        /// </summary>
        protected override bool register => true;

        /// <summary>
        /// EventUnit 用のデータ構造を生成します。
        /// </summary>
        public override IGraphElementData CreateData()
        {
            return new Data();
        }

        /// <summary>
        /// Runner（self）をターゲットとする EventHook を返し、
        /// 同一 Runner からの発火のみ受け付けるようにします。
        ///
        /// ここでは FUnity 側から
        /// EventBus.Trigger(new EventHook(FUnityEventNames.OnSpriteClicked, target), args);
        /// のように呼ばれることを想定しています。
        /// </summary>
        /// <param name="reference">現在処理中のグラフ参照。</param>
        /// <returns>「このスプライトが押されたとき」イベント用の EventHook。</returns>
        public override EventHook GetHook(GraphReference reference)
        {
            // クリックされた Actor（スプライト）が self に一致する場合のみ発火
            return new EventHook(FUnityEventNames.OnSpriteClicked, reference.self);
        }

        /// <summary>
        /// EventUnit 側で定義済みの trigger ポートを利用するため、
        /// 基底実装のみを呼び出します。
        /// </summary>
        protected override void Definition()
        {
            base.Definition();
        }

        /// <summary>
        /// 「このスプライトが押されたとき」には引数を特に利用しないため、
        /// ここでは何も行いません。
        /// </summary>
        /// <param name="flow">実行中のフロー。</param>
        /// <param name="args">空のイベント引数。</param>
        protected override void AssignArguments(Flow flow, EmptyEventArgs args)
        {
            // Scratch と同様、特に追加の引数は持たない
        }

        /// <summary>
        /// 発火条件は FUnity 側の EventBus.Trigger 側で制御されるため、
        /// ここでは常に true を返します。
        /// </summary>
        /// <param name="flow">実行中のフロー。</param>
        /// <param name="args">空のイベント引数。</param>
        /// <returns>常に true。</returns>
        protected override bool ShouldTrigger(Flow flow, EmptyEventArgs args)
        {
            return true;
        }

        /// <summary>
        /// グラフ有効化時に EventBus へリッスンを登録し、
        /// クリックイベントを Scratch スレッドとして処理するようにします。
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
        /// 「このスプライトが押されたとき」のイベントでグラフを発火し、
        /// 起動した Flow を Scratch スレッドとして登録します。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <param name="args">空のイベント引数。</param>
        private void TriggerWithThreadRegistration(GraphReference reference, EmptyEventArgs args)
        {
            var flow = Flow.New(reference);
            if (!ShouldTrigger(flow, args))
            {
                flow.Dispose();
                return;
            }

            AssignArguments(flow, args);

            // Scratch 互換のコルーチンとして実行し、スレッド管理へ登録
            flow.StartCoroutine(trigger);
            ScratchUnitUtil.RegisterScratchFlow(flow);
        }
    }
}
