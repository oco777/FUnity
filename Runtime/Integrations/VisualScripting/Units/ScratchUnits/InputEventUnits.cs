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
    [UnitSubtitle("イベント")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class OnKeyPressedUnit : EventUnit<EmptyEventArgs>
    {
        /// <summary>GraphReference 単位のキー入力リスナーを保持します。</summary>
        private static readonly Dictionary<GraphReference, Action<EmptyEventArgs>> s_Handlers
            = new Dictionary<GraphReference, Action<EmptyEventArgs>>();

        /// <summary>現在ひとつでもキー入力リスナーが登録されているかを返します。</summary>
        internal static bool HasAnyListeners => s_Handlers.Count > 0;

        /// <summary>
        /// FUnityManager から毎フレーム呼び出され、登録済みのキー入力ハンドラを実行します。
        /// </summary>
        internal static void ProcessUpdate()
        {
            if (s_Handlers.Count == 0)
            {
                return;
            }

            var snapshot = new List<Action<EmptyEventArgs>>(s_Handlers.Values);
            var args = default(EmptyEventArgs);

            foreach (var handler in snapshot)
            {
                if (handler == null)
                {
                    continue;
                }

                try
                {
                    handler(args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>監視対象のキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>
        /// Visual Scripting 標準の EventBus を使用しないため、登録は行いません。
        /// </summary>
        protected override bool register => false;

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
        /// FUnity 専用のディスパッチャへ登録し、Scratch スレッド登録経由で実行します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StartListening(GraphStack stack)
        {
            if (stack == null || !stack.HasReference)
            {
                return;
            }

            var reference = stack.ToReference();

            if (s_Handlers.ContainsKey(reference))
            {
                return;
            }

            Action<EmptyEventArgs> handler = args => TriggerWithThreadRegistration(reference, args);

            s_Handlers.Add(reference, handler);
        }

        /// <summary>
        /// FUnity 専用ディスパッチャからキー入力監視の登録を解除します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StopListening(GraphStack stack)
        {
            if (stack == null || !stack.HasReference)
            {
                return;
            }

            var reference = stack.ToReference();

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

namespace FUnity.Runtime.Integrations.VisualScripting
{
    /// <summary>
    /// FUnityManager から Visual Scripting の入力イベントを駆動するディスパッチャです。
    /// 今後の拡張でマウスやゲームパッド入力も集約できるよう、現時点ではキー入力のみを委譲します。
    /// </summary>
    internal static class ScratchInputEventDispatcher
    {
        /// <summary>
        /// 毎フレーム呼び出され、Scratch 互換の入力イベントを順次処理します。
        /// </summary>
        public static void Tick()
        {
            Units.ScratchUnits.OnKeyPressedUnit.ProcessUpdate();
        }
    }
}
