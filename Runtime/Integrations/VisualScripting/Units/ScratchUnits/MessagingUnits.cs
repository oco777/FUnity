// Path: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs
// Summary: Scratch メッセージ関連（送信／送信して待つ／受信イベント）の Visual Scripting Unit 群です。
using System;
using System.Collections;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch 互換のメッセージ配送で共通利用する定数と引数構造体をまとめます。
    /// </summary>
    public static class MessagingCommon
    {
        /// <summary>EventBus 経由で流通させるイベント名です。</summary>
        public const string EventName = "FUnity.Message";

        /// <summary>EventBus.Trigger で配送する引数一式です。</summary>
        [Serializable]
        public struct Args
        {
            /// <summary>メッセージ名です。未指定時は空文字列に正規化します。</summary>
            public string Message;
        }
    }

    /// <summary>
    /// Scratch の「メッセージを送る」に相当し、指定メッセージを即時配信する Unit です。
    /// </summary>
    [UnitTitle("メッセージを送る")]
    [UnitCategory("FUnity/Scratch/イベント")]
    [UnitSubtitle("funity scratch イベント broadcast message send メッセージ")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
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

        /// <summary>
        /// ポート定義を行い、入力から出力への制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Message = ValueInput<string>("message", string.Empty);

            Succession(m_Enter, m_Exit);
            Requirement(m_Message, m_Enter);
        }

        /// <summary>
        /// 入力フローを受け取り、EventBus 経由でメッセージを配信します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>後続ノードへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var message = flow.GetValue<string>(m_Message) ?? string.Empty;
            var args = new MessagingCommon.Args
            {
                Message = message
            };

            EventBus.Trigger(new EventHook(MessagingCommon.EventName), args);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「メッセージを送って待つ」に相当し、受信側の処理が終わるまで同フレームで待機する Unit です。
    /// </summary>
    [UnitTitle("メッセージを送って待つ")]
    [UnitCategory("FUnity/Scratch/イベント")]
    [UnitSubtitle("funity scratch イベント broadcast wait message メッセージ")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
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

        /// <summary>
        /// ポート定義を行い、入力から出力への制御線を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);
            m_Exit = ControlOutput("exit");
            m_Message = ValueInput<string>("message", string.Empty);

            Succession(m_Enter, m_Exit);
            Requirement(m_Message, m_Enter);
        }

        /// <summary>
        /// メッセージを配信し、EventBus が処理を完了するまでフローを保留します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>待機処理を表す IEnumerator。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            var message = flow.GetValue<string>(m_Message) ?? string.Empty;
            var args = new MessagingCommon.Args
            {
                Message = message
            };

            EventBus.Trigger(new EventHook(MessagingCommon.EventName), args);

            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「メッセージを受け取ったとき」に相当し、指定メッセージ受信でフローを発火する EventUnit です。
    /// </summary>
    [UnitTitle("メッセージを受け取ったとき")]
    [UnitCategory("Events/FUnity/Scratch/イベント")]
    [UnitSubtitle("funity scratch イベント receive message broadcast 受信")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class WhenIReceiveMessageUnit : EventUnit<MessagingCommon.Args>
    {
        /// <summary>EventUnit の自動登録を有効にします。</summary>
        protected override bool register => true;

        /// <summary>受信したいメッセージ名を指定するフィルタ入力です。</summary>
        [DoNotSerialize]
        private ValueInput m_Filter;

        /// <summary>実際に受信したメッセージ名を提供する出力です。</summary>
        [DoNotSerialize]
        private ValueOutput m_OutMessage;

        /// <summary>
        /// イベントバスで購読する EventHook を返します。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <returns>メッセージイベント用の EventHook。</returns>
        public override EventHook GetHook(GraphReference reference)
        {
            return new EventHook(MessagingCommon.EventName);
        }

        /// <summary>
        /// メッセージ受信イベントをコルーチンで実行し、Scratch 用スレッドとして登録します。
        /// ScriptMachine を取得できない場合は既存の同期処理にフォールバックします。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <param name="args">メッセージイベント引数。</param>
        public new void Trigger(GraphReference reference, MessagingCommon.Args args)
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
        /// ポート定義と EventUnit 基底の初期化を行います。
        /// </summary>
        protected override void Definition()
        {
            base.Definition();
            m_Filter = ValueInput<string>("filter", string.Empty);
            m_OutMessage = ValueOutput<string>("message");
        }

        /// <summary>
        /// イベント発火時に ValueOutput へ値を割り当てます。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <param name="args">受信したメッセージ引数。</param>
        protected override void AssignArguments(Flow flow, MessagingCommon.Args args)
        {
            flow.SetValue(m_OutMessage, args.Message ?? string.Empty);
        }

        /// <summary>
        /// 指定されたフィルタに一致するメッセージのみを発火させるかを判定します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <param name="args">受信したメッセージ引数。</param>
        /// <returns>トリガーを許可する場合は true。</returns>
        protected override bool ShouldTrigger(Flow flow, MessagingCommon.Args args)
        {
            var filter = flow.GetValue<string>(m_Filter) ?? string.Empty;
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            var message = args.Message ?? string.Empty;
            return string.Equals(filter, message, StringComparison.Ordinal);
        }
    }
}
