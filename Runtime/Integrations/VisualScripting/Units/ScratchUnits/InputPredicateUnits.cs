using Unity.VisualScripting;
using UnityEngine;
using UInput = UnityEngine.Input;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇キーが押された？」ブロックを再現し、指定キーが押されている間は true を返す Unit です。
    /// </summary>
    [UnitTitle("Key Pressed?")]
    [UnitShortTitle("Key?")]
    [UnitCategory("Scratch/Sensing")]
    public sealed class KeyIsPressedUnit : Unit
    {
        /// <summary>押下状態を監視するキーを指定する ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Key;

        /// <summary>押下状態を bool で返す ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Result;

        /// <summary>監視キーの入力ポートを公開します。</summary>
        public ValueInput Key => m_Key;

        /// <summary>押下判定結果の出力ポートを公開します。</summary>
        public ValueOutput Result => m_Result;

        /// <summary>
        /// ポートを定義し、監視キーの入力値に応じて押下判定を行う設定を構築します。
        /// </summary>
        protected override void Definition()
        {
            m_Key = ValueInput<ScratchKey>(nameof(Key), ScratchKey.Space);
            m_Result = ValueOutput<bool>(nameof(Result), Evaluate);

            Requirement(m_Key, m_Result);
        }

        /// <summary>
        /// 指定されたキーが現在押されているかどうかを判定します。
        /// </summary>
        /// <param name="flow">現在処理しているフロー。</param>
        /// <returns>押されている場合は true、それ以外は false。</returns>
        private bool Evaluate(Flow flow)
        {
            var scratchKey = flow.GetValue<ScratchKey>(m_Key);
            var keyCode = ScratchKeyUtil.ToKeyCode(scratchKey);
            if (keyCode == KeyCode.None)
            {
                return false;
            }

            return UInput.GetKey(keyCode);
        }
    }
}
