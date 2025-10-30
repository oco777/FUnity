using System;
using Unity.VisualScripting;
using UnityEngine;
using UInput = UnityEngine.Input;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇キーが押されたとき」ブロックを再現し、押下瞬間にフローを発火する Unit です。
    /// </summary>
    [UnitTitle("On Key Pressed")]
    [UnitShortTitle("Key Pressed")]
    [UnitCategory("Scratch/Events")]
    public sealed class OnKeyPressedUnit : EventUnit<EmptyEventArgs>, IGraphElementWithData
    {
        /// <summary>監視対象のキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>自動登録を行わず、自前で EventBus を利用する設定値です。</summary>
        protected override bool register => false;

        /// <summary>
        /// Update フックで利用するランタイムデータを保持します。
        /// </summary>
        private sealed class Data : IGraphElementData
        {
            /// <summary>フロー生成に使用するグラフ参照です。</summary>
            public GraphReference m_Reference;

            /// <summary>前フレームでキーが押下されていたかどうかの状態です。</summary>
            public bool m_WasDown;

            /// <summary>Update フックから呼び出されるデリゲートです。</summary>
            public Action<EmptyEventArgs> m_Callback;
        }

        /// <summary>
        /// Update フックを利用してキー状態をポーリングします。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <returns>利用する EventHook。</returns>
        protected override EventHook GetHook(GraphReference reference)
        {
            return EventHooks.Update;
        }

        /// <summary>
        /// ポート定義を行い、キー入力を条件とするイベントを登録します。
        /// </summary>
        protected override void Definition()
        {
            base.Definition();
            m_Key = ValueInput<ScratchKey>("key", ScratchKey.Space);
            Requirement(m_Key, trigger);
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
        /// グラフ開始時に Update フックへ登録し、キー入力を監視します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);

            var data = (Data)stack.GetElementData(this);
            var reference = stack.ToReference();
            data.m_Reference = reference;
            data.m_WasDown = false;

            if (data.m_Callback != null)
            {
                return;
            }

            data.m_Callback = args => Poll(data);
            EventBus.Register(this, GetHook(reference), data.m_Callback);
        }

        /// <summary>
        /// グラフ停止時に Update フックから登録を解除します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);

            var data = (Data)stack.GetElementData(this);
            if (data.m_Callback != null && data.m_Reference != null)
            {
                EventBus.Unregister(this, GetHook(data.m_Reference), data.m_Callback);
            }

            data.m_Callback = null;
            data.m_WasDown = false;
            data.m_Reference = null;
        }

        /// <summary>
        /// 毎フレーム呼び出され、指定キーの押下瞬間を検出してフローを発火します。
        /// </summary>
        /// <param name="data">監視に用いるランタイムデータ。</param>
        private void Poll(Data data)
        {
            if (data == null)
            {
                return;
            }

            if (data.m_Reference == null)
            {
                return;
            }

            using var flow = Flow.New(data.m_Reference);
            var scratchKey = flow.GetValue<ScratchKey>(m_Key);
            var keyCode = ScratchKeyUtil.ToKeyCode(scratchKey);
            if (keyCode == KeyCode.None)
            {
                data.m_WasDown = false;
                return;
            }

            var isDown = UInput.GetKey(keyCode);
            var isPressed = isDown && !data.m_WasDown;
            data.m_WasDown = isDown;

            if (isPressed)
            {
                flow.Invoke(trigger);
            }
        }
    }
}
