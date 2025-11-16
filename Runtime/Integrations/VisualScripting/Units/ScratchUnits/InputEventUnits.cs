using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using FUnity.Runtime.Integrations.VisualScripting;
using UInput = UnityEngine.Input;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇キーが押されたとき」ブロックを再現し、押下瞬間にフローを発火する Unit です。
    /// </summary>
    [UnitTitle("○キーが押されたとき")]
    [UnitShortTitle("○キー")]
    [UnitCategory("Events/FUnity/Scratch/イベント")]
    [UnitSubtitle("funity scratch イベント key keyboard press 押された when")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class OnKeyPressedUnit : EventUnit<EmptyEventArgs>, IGraphElementWithData
    {
        /// <summary>監視対象のキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>EventUnit の自動登録機構を利用するための設定値です。</summary>
        protected override bool register => true;

        /// <summary>押下エッジを検出するためのランタイムデータを保持します。</summary>
        private sealed class Data : IGraphElementData
        {
            /// <summary>前フレームでキーが押されていたかどうかを表します。</summary>
            public bool m_WasDown;
        }

        /// <summary>
        /// Update フックを利用してキー状態をポーリングします。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <returns>利用する EventHook。</returns>
        public override EventHook GetHook(GraphReference reference)
        {
            return EventHooks.Update;
        }

        /// <summary>
        /// キー押下イベントをコルーチンとして起動し、Scratch 用スレッドとして登録します。
        /// ScriptMachine を取得できない場合は従来の同期実行にフォールバックします。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
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
            ScriptGraphAsset graph = null;
            if (machine.nest != null)
            {
                graph = machine.nest.macro as ScriptGraphAsset;
            }

            IEnumerator Routine()
            {
                using (flow)
                {
                    flow.Invoke(trigger);
                }

                yield break;
            }

            var coroutine = machine.StartCoroutine(Routine());
            ScratchUnitUtil.EnsureScratchThreadRegistered(flow, adapter, graph, coroutine);
        }

        /// <summary>
        /// ポート定義を行い、キー入力を条件とするイベントを登録します。
        /// </summary>
        protected override void Definition()
        {
            base.Definition();
            m_Key = ValueInput<ScratchKey>("key", ScratchKey.Space);
            // EventUnit では ControlInput を要求しないため Requirement は不要。
        }

        /// <summary>
        /// ランタイムデータを生成します。
        /// </summary>
        /// <returns>初期化済みの Data インスタンス。</returns>
        public IGraphElementData CreateData()
        {
            return new Data();
        }

        /// <summary>
        /// Update ごとに押下エッジを検出し、押された瞬間のみ発火させるかどうかを返します。
        /// </summary>
        /// <param name="flow">現在処理しているフロー。</param>
        /// <param name="args">イベント引数（未使用）。</param>
        /// <returns>押下エッジが検出された場合は true。</returns>
        protected override bool ShouldTrigger(Flow flow, EmptyEventArgs args)
        {
            var data = flow.stack.GetElementData<Data>(this);

            var scratchKey = flow.GetValue<ScratchKey>(m_Key);
            var keyCode = ScratchKeyUtil.ToKeyCode(scratchKey);
            if (keyCode == KeyCode.None)
            {
                data.m_WasDown = false;
                return false;
            }

            var isDown = UInput.GetKey(keyCode);
            var isPressed = isDown && !data.m_WasDown;
            data.m_WasDown = isDown;

            return isPressed;
        }
    }
}
