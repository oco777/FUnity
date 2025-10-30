using System;
using Unity.VisualScripting;
using UnityEngine;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇キーが押されたとき」ブロックを再現し、押下瞬間にフローを発火する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Events/On Key Pressed")]
    [UnitShortTitle("On Key Pressed")]
    [UnitCategory("Scratch/Events/OnKeyPressed")]
    public sealed class OnKeyPressedUnit : Unit, IGraphElementWithData
    {
        /// <summary>押下時のフローを送出する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Trigger;

        /// <summary>監視対象のキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>押下時フローの出力ポートを公開します。</summary>
        public ControlOutput Trigger => m_Trigger;

        /// <summary>監視対象キーの入力ポートを公開します。</summary>
        public ValueInput Key => m_Key;

        /// <summary>
        /// Update フックで利用するランタイムデータを保持します。
        /// </summary>
        private sealed class Data : IGraphElementData
        {
            /// <summary>対象グラフへの参照です。</summary>
            public GraphReference m_Reference;

            /// <summary>Update フックに登録するデリゲートです。</summary>
            public Action<EmptyEventArgs> m_UpdateHandler;
        }

        /// <summary>
        /// ポート定義を行い、キー入力を条件とするイベントを登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Trigger = ControlOutput("trigger");
            m_Key = ValueInput("key", ScratchKey.Space);

            Requirement(m_Key, m_Trigger);
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

            var data = stack.GetElementData<Data>(this);
            data.m_Reference = stack.ToReference();

            if (data.m_UpdateHandler == null)
            {
                data.m_UpdateHandler = args => Poll(data);
            }

            EventBus.Register<EmptyEventArgs>(this, EventHooks.Update, data.m_UpdateHandler);
        }

        /// <summary>
        /// グラフ停止時に Update フックから登録を解除します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StopListening(GraphStack stack)
        {
            base.StopListening(stack);

            var data = stack.GetElementData<Data>(this);
            if (data.m_UpdateHandler != null)
            {
                EventBus.Unregister<EmptyEventArgs>(this, EventHooks.Update, data.m_UpdateHandler);
            }
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
                return;
            }

            if (UnityEngine.Input.GetKeyDown(keyCode))
            {
                flow.Invoke(m_Trigger);
            }
        }
    }
}
