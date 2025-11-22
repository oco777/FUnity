using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using FUnity.Runtime.Integrations.VisualScripting;
using UInput = UnityEngine.Input;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇キーが押されたとき」ブロックを再現し、
    /// 対象キーが押されたフレームでフローを発火する EventUnit です。
    /// </summary>
    [UnitTitle("○キーが押されたとき")]
    [UnitShortTitle("○キー")]
    [UnitCategory("Events/FUnity/Blocks/イベント")]
    [UnitSubtitle("funity scratch イベント key keyboard press 押された when")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class OnKeyPressedUnit : EventUnit<EmptyEventArgs>
    {
        /// <summary>GraphReference 単位のキー入力リスナーを保持します。</summary>
        private static readonly Dictionary<GraphReference, Action<EmptyEventArgs>> s_Handlers
            = new Dictionary<GraphReference, Action<EmptyEventArgs>>();

        /// <summary>監視対象のキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>
        /// この EventUnit を EventBus に登録するかどうかを指定します。
        /// true にしておくことで、Visual Scripting がこのイベントをリッスンします。
        /// </summary>
        protected override bool register => true;

        /// <summary>イベントデータを生成します。</summary>
        /// <returns>EventUnit 既定のデータ構造。</returns>
        public override IGraphElementData CreateData()
        {
            return new Data();
        }

        /// <summary>
        /// Visual Scripting の組み込み Update イベントを使用します。
        /// </summary>
        public override EventHook GetHook(GraphReference reference)
        {
            // UnityOnUpdateMessageListener から発火される標準の Update イベント
            return EventHooks.Update;
        }

        /// <summary>
        /// ポート定義を行い、キー入力を条件とするイベントを登録します。
        /// </summary>
        protected override void Definition()
        {
            base.Definition();

            // 監視対象キー (デフォルトは Space)
            m_Key = ValueInput<ScratchKey>("key", ScratchKey.Space);
        }

        /// <summary>
        /// Update ごとに「このフレームでキーが押されたか（GetKeyDown）」を判定します。
        /// true を返したフレームでのみ trigger が実行されます。
        /// </summary>
        protected override bool ShouldTrigger(Flow flow, EmptyEventArgs args)
        {
            var scratchKey = flow.GetValue<ScratchKey>(m_Key);
            var keyCode = ScratchKeyUtil.ToKeyCode(scratchKey);
            if (keyCode == KeyCode.None)
            {
                return false;
            }

            // 押下瞬間のみを拾う
            return UInput.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Update 監視イベントを EventBus に登録し、Scratch スレッド登録経由で実行します。
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
        /// Update 監視イベントの登録を解除します。
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
        /// キー入力イベントで Flow を開始し、Scratch 用スレッドに登録してから処理を実行します。
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

            flow.StartCoroutine(trigger);
            ScratchUnitUtil.RegisterScratchFlow(flow);
        }
    }
}
