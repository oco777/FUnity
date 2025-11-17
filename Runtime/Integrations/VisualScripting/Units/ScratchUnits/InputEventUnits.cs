using System;
using System.Collections;
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
    [UnitCategory("Events/FUnity/Scratch/イベント")]
    [UnitSubtitle("funity scratch イベント key keyboard press 押された when")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class OnKeyPressedUnit : EventUnit<EmptyEventArgs>
    {
        /// <summary>イベントリッスン状態を保持するデータ構造です。</summary>
        private sealed class ScratchEventListenerData : IGraphElementData
        {
            /// <summary>現在リッスン中かどうか。</summary>
            public bool m_IsListening;

            /// <summary>EventBus 登録に利用するフック。</summary>
            public EventHook m_Hook;

            /// <summary>発火時に呼び出すハンドラ。</summary>
            public Action<EmptyEventArgs> m_Handler;

            /// <summary>GraphReference をキャッシュします。</summary>
            public GraphReference m_Reference;
        }

        /// <summary>監視対象のキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>
        /// この EventUnit を EventBus に登録するかどうかを指定します。
        /// true にしておくことで、Visual Scripting がこのイベントをリッスンします。
        /// </summary>
        protected override bool register => true;

        /// <summary>イベントデータを生成します。</summary>
        /// <returns>Scratch 用リスナー状態。</returns>
        public override IGraphElementData CreateData()
        {
            return new ScratchEventListenerData();
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

            var data = stack.GetElementData<ScratchEventListenerData>(this);
            if (data.m_IsListening)
            {
                return;
            }

            var reference = stack.ToReference();
            data.m_Reference = reference;
            data.m_Hook = GetHook(reference);
            data.m_Handler = args => TriggerWithThreadRegistration(reference, args);

            EventBus.Register(data.m_Hook, data.m_Handler);
            data.m_IsListening = true;
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

            var data = stack.GetElementData<ScratchEventListenerData>(this);
            if (!data.m_IsListening)
            {
                return;
            }

            if (data.m_Hook != null && data.m_Handler != null)
            {
                EventBus.Unregister(data.m_Hook, data.m_Handler);
            }

            data.m_IsListening = false;
            data.m_Reference = null;
            data.m_Hook = null;
            data.m_Handler = null;
        }

        /// <summary>
        /// イベントをコルーチン化し、Scratch 用スレッドに登録してから処理を実行します。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <param name="args">空のイベント引数。</param>
        private void TriggerWithThreadRegistration(GraphReference reference, EmptyEventArgs args)
        {
            using (var flow = Flow.New(reference))
            {
                var routine = RunEventCoroutine(flow, args);
                ScratchUnitUtil.StartScratchCoroutine(flow, routine);
            }
        }

        /// <summary>
        /// ShouldTrigger と AssignArguments を走らせた後、trigger ポートを実行します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <param name="args">空のイベント引数。</param>
        /// <returns>実行完了までの列挙子。</returns>
        private IEnumerator RunEventCoroutine(Flow flow, EmptyEventArgs args)
        {
            using (flow)
            {
                if (!ShouldTrigger(flow, args))
                {
                    yield break;
                }

                AssignArguments(flow, args);
                flow.Invoke(trigger);
            }
        }
    }
}
