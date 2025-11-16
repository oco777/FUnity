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
        /// <summary>監視対象のキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>
        /// この EventUnit を EventBus に登録するかどうかを指定します。
        /// true にしておくことで、Visual Scripting がこのイベントをリッスンします。
        /// </summary>
        protected override bool register => true;

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

        // EventUnit<EmptyEventArgs> の標準パイプラインをそのまま利用するので、
        // StartListening / StopListening / Trigger のオーバーライドは不要です。
    }
}
