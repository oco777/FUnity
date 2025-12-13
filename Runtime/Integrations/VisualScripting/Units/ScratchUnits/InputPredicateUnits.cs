using Unity.VisualScripting;
using UnityEngine;
using FUnity.Runtime.Integrations.VisualScripting;
using UInput = UnityEngine.Input;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇キーが押された？」ブロックを再現し、指定キーが押されている間は true を返す Unit です。
    /// </summary>
    [UnitTitle("○キーが押された？")]
    [UnitShortTitle("○キー？")]
    [UnitCategory("FUnity/Blocks/調べる")]
    [UnitSubtitle("調べる")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
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

#if ENABLE_INPUT_SYSTEM
            return IsPressedByInputSystem(keyCode);
#else
            return UInput.GetKey(keyCode);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Input System が有効な環境でキー押下状態を確認し、押されていなければ false を返します。
        /// Keyboard.current が取得できないケースにも対応し、例外を防ぎます。
        /// </summary>
        /// <param name="keyCode">判定対象の KeyCode。</param>
        /// <returns>押されている場合は true。それ以外は false。</returns>
        private static bool IsPressedByInputSystem(KeyCode keyCode)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (!TryMapKeyCodeToInputSystemKey(keyCode, out var mappedKey))
            {
                return false;
            }

            return keyboard[mappedKey].isPressed;
        }

        /// <summary>
        /// KeyCode を Input System の Key へ変換します。対応していないキーの場合は false を返します。
        /// Scratch で重視される A-Z / 0-9 / 矢印キー / スペース / エンター / エスケープを網羅します。
        /// </summary>
        /// <param name="keyCode">変換元の KeyCode。</param>
        /// <param name="inputSystemKey">変換に成功した場合の出力先。</param>
        /// <returns>変換できた場合は true。それ以外は false。</returns>
        private static bool TryMapKeyCodeToInputSystemKey(KeyCode keyCode, out Key inputSystemKey)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                inputSystemKey = (Key)((int)Key.A + ((int)keyCode - (int)KeyCode.A));
                return true;
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                inputSystemKey = (Key)((int)Key.Digit0 + ((int)keyCode - (int)KeyCode.Alpha0));
                return true;
            }

            if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
            {
                inputSystemKey = (Key)((int)Key.Numpad0 + ((int)keyCode - (int)KeyCode.Keypad0));
                return true;
            }

            switch (keyCode)
            {
                case KeyCode.Space:
                    inputSystemKey = Key.Space;
                    return true;
                case KeyCode.LeftArrow:
                    inputSystemKey = Key.LeftArrow;
                    return true;
                case KeyCode.RightArrow:
                    inputSystemKey = Key.RightArrow;
                    return true;
                case KeyCode.UpArrow:
                    inputSystemKey = Key.UpArrow;
                    return true;
                case KeyCode.DownArrow:
                    inputSystemKey = Key.DownArrow;
                    return true;
                case KeyCode.Return:
                    inputSystemKey = Key.Enter;
                    return true;
                case KeyCode.KeypadEnter:
                    inputSystemKey = Key.NumpadEnter;
                    return true;
                case KeyCode.Escape:
                    inputSystemKey = Key.Escape;
                    return true;
                default:
                    inputSystemKey = default;
                    return false;
            }
        }
#endif
    }
}
