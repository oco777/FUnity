// Path: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs
// Summary: Scratch メッセージ関連（送信／送信して待つ／受信イベント）の Visual Scripting Unit 群です。
using System.Collections;
using Unity.VisualScripting;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「メッセージを送る」に相当し、指定メッセージを即時配信する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Broadcast Message")]
    [UnitCategory("Scratch/Events")]
    public sealed class BroadcastMessageUnit : Unit
    {
        /// <summary>入力フロー。呼び出し時にメッセージを配信します。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>出力フロー。Publish 後に次ノードへ進みます。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>送信するメッセージ名を受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Message;

        /// <summary>受け渡す任意ペイロードを受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Payload;

        /// <summary>
        /// ポート定義を行い、入力から出力への制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Message = ValueInput<string>("message", "message");
            m_Payload = ValueInput<object>("payload", null);

            Succession(m_Enter, m_Exit);
            Requirement(m_Message, m_Enter);
        }

        /// <summary>
        /// 入力フローを受け取り、MessageBus 経由でメッセージを配信します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>後続ノードへ進める ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var message = flow.GetValue<string>(m_Message);
            var payload = flow.GetValue<object>(m_Payload);
            MessageBus.Publish(message, payload);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「メッセージを送って待つ」に相当し、受信側の処理が終わるまで待機する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Broadcast And Wait")]
    [UnitCategory("Scratch/Events")]
    public sealed class BroadcastAndWaitUnit : Unit
    {
        /// <summary>入力フロー。コルーチンで待機処理を行います。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>出力フロー。全ハンドラ完了後に発火します。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>送信するメッセージ名を受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Message;

        /// <summary>受け渡す任意ペイロードを受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Payload;

        /// <summary>
        /// ポート定義を行い、入力から出力への制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);
            m_Exit = ControlOutput("exit");
            m_Message = ValueInput<string>("message", "message");
            m_Payload = ValueInput<object>("payload", null);

            Succession(m_Enter, m_Exit);
            Requirement(m_Message, m_Enter);
        }

        /// <summary>
        /// メッセージを配信し、対象ハンドラがすべて完了するまでフローを保留します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>待機処理を表す IEnumerator。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            var message = flow.GetValue<string>(m_Message);
            var payload = flow.GetValue<object>(m_Payload);
            MessageBus.Publish(message, payload);

            while (MessageBus.GetActiveHandlerCount(message) > 0)
            {
                yield return null;
            }

            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「メッセージを受け取ったとき」に相当し、指定メッセージ受信でフローを発火する EventUnit です。
    /// </summary>
    [UnitTitle("Scratch/When I Receive")]
    [UnitCategory("Scratch/Events")]
    public sealed class WhenIReceiveMessageUnit : EventUnit<WhenIReceiveMessageUnit.Args>
    {
        /// <summary>イベント発火時の引数をまとめる構造体です。</summary>
        public struct Args
        {
            /// <summary>受信したペイロードを保持します。</summary>
            public object Payload;
        }

        /// <summary>EventUnit の自動登録を有効にします。</summary>
        protected override bool register => true;

        /// <summary>対象メッセージ名を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Message;

        /// <summary>受信したペイロードを参照できる ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Payload;

        /// <summary>現在登録中のメッセージ名を保持します。</summary>
        private string m_CurrentMessage;

        /// <summary>直近に受信したイベント引数を保持します。</summary>
        private Args m_LastArgs;

        /// <summary>Trigger 呼び出し時に利用する GraphReference です。</summary>
        private GraphReference m_Reference;

        /// <summary>
        /// ポート定義と EventUnit 基底の初期化を行います。
        /// </summary>
        protected override void Definition()
        {
            base.Definition();
            m_Message = ValueInput<string>("message", "message");
            m_Payload = ValueOutput<object>("payload", flow => m_LastArgs.Payload);
        }

        /// <summary>
        /// グラフが有効化された際にメッセージ購読を開始します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StartListening(GraphStack stack)
        {
            base.StartListening(stack);
            m_Reference = stack.ToReference();
            using (var flow = Flow.New(m_Reference))
            {
                m_CurrentMessage = flow.GetValue<string>(m_Message);
            }

            MessageBus.Subscribe(m_CurrentMessage, OnMessage);
        }

        /// <summary>
        /// グラフが無効化された際にメッセージ購読を解除します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StopListening(GraphStack stack)
        {
            MessageBus.Unsubscribe(m_CurrentMessage, OnMessage);
            m_CurrentMessage = null;
            m_LastArgs = default;
            m_Reference = null;
            base.StopListening(stack);
        }

        /// <summary>
        /// MessageBus から呼び出され、イベントを発火させます。
        /// </summary>
        /// <param name="payload">受信したペイロード。</param>
        private void OnMessage(object payload)
        {
            m_LastArgs = new Args { Payload = payload };
            if (m_Reference != null)
            {
                Trigger(m_Reference, m_LastArgs);
            }
        }
    }
}
