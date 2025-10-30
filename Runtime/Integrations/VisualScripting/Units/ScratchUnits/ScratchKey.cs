using UnityEngine;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch のブロックで指定できるキー種別を列挙で表現します。
    /// </summary>
    public enum ScratchKey
    {
        /// <summary>スペースキーです。</summary>
        Space,
        /// <summary>上矢印キーです。</summary>
        Up,
        /// <summary>下矢印キーです。</summary>
        Down,
        /// <summary>左矢印キーです。</summary>
        Left,
        /// <summary>右矢印キーです。</summary>
        Right,
        /// <summary>アルファベット A キーです。</summary>
        A,
        /// <summary>アルファベット B キーです。</summary>
        B,
        /// <summary>アルファベット C キーです。</summary>
        C,
        /// <summary>アルファベット D キーです。</summary>
        D,
        /// <summary>アルファベット E キーです。</summary>
        E,
        /// <summary>アルファベット F キーです。</summary>
        F,
        /// <summary>アルファベット G キーです。</summary>
        G,
        /// <summary>アルファベット H キーです。</summary>
        H,
        /// <summary>アルファベット I キーです。</summary>
        I,
        /// <summary>アルファベット J キーです。</summary>
        J,
        /// <summary>アルファベット K キーです。</summary>
        K,
        /// <summary>アルファベット L キーです。</summary>
        L,
        /// <summary>アルファベット M キーです。</summary>
        M,
        /// <summary>アルファベット N キーです。</summary>
        N,
        /// <summary>アルファベット O キーです。</summary>
        O,
        /// <summary>アルファベット P キーです。</summary>
        P,
        /// <summary>アルファベット Q キーです。</summary>
        Q,
        /// <summary>アルファベット R キーです。</summary>
        R,
        /// <summary>アルファベット S キーです。</summary>
        S,
        /// <summary>アルファベット T キーです。</summary>
        T,
        /// <summary>アルファベット U キーです。</summary>
        U,
        /// <summary>アルファベット V キーです。</summary>
        V,
        /// <summary>アルファベット W キーです。</summary>
        W,
        /// <summary>アルファベット X キーです。</summary>
        X,
        /// <summary>アルファベット Y キーです。</summary>
        Y,
        /// <summary>アルファベット Z キーです。</summary>
        Z,
        /// <summary>数字 0 キーです。</summary>
        Digit0,
        /// <summary>数字 1 キーです。</summary>
        Digit1,
        /// <summary>数字 2 キーです。</summary>
        Digit2,
        /// <summary>数字 3 キーです。</summary>
        Digit3,
        /// <summary>数字 4 キーです。</summary>
        Digit4,
        /// <summary>数字 5 キーです。</summary>
        Digit5,
        /// <summary>数字 6 キーです。</summary>
        Digit6,
        /// <summary>数字 7 キーです。</summary>
        Digit7,
        /// <summary>数字 8 キーです。</summary>
        Digit8,
        /// <summary>数字 9 キーです。</summary>
        Digit9
    }

    /// <summary>
    /// ScratchKey と UnityEngine.KeyCode の変換を補助するユーティリティです。
    /// </summary>
    public static class ScratchKeyUtil
    {
        /// <summary>
        /// ScratchKey に対応する Unity の KeyCode を返します。
        /// </summary>
        /// <param name="key">Scratch 形式で指定されたキー。</param>
        /// <returns>対応する KeyCode。該当しない場合は KeyCode.None。</returns>
        public static KeyCode ToKeyCode(ScratchKey key)
        {
            switch (key)
            {
                case ScratchKey.Space:
                    return KeyCode.Space;
                case ScratchKey.Up:
                    return KeyCode.UpArrow;
                case ScratchKey.Down:
                    return KeyCode.DownArrow;
                case ScratchKey.Left:
                    return KeyCode.LeftArrow;
                case ScratchKey.Right:
                    return KeyCode.RightArrow;
                case ScratchKey.A:
                    return KeyCode.A;
                case ScratchKey.B:
                    return KeyCode.B;
                case ScratchKey.C:
                    return KeyCode.C;
                case ScratchKey.D:
                    return KeyCode.D;
                case ScratchKey.E:
                    return KeyCode.E;
                case ScratchKey.F:
                    return KeyCode.F;
                case ScratchKey.G:
                    return KeyCode.G;
                case ScratchKey.H:
                    return KeyCode.H;
                case ScratchKey.I:
                    return KeyCode.I;
                case ScratchKey.J:
                    return KeyCode.J;
                case ScratchKey.K:
                    return KeyCode.K;
                case ScratchKey.L:
                    return KeyCode.L;
                case ScratchKey.M:
                    return KeyCode.M;
                case ScratchKey.N:
                    return KeyCode.N;
                case ScratchKey.O:
                    return KeyCode.O;
                case ScratchKey.P:
                    return KeyCode.P;
                case ScratchKey.Q:
                    return KeyCode.Q;
                case ScratchKey.R:
                    return KeyCode.R;
                case ScratchKey.S:
                    return KeyCode.S;
                case ScratchKey.T:
                    return KeyCode.T;
                case ScratchKey.U:
                    return KeyCode.U;
                case ScratchKey.V:
                    return KeyCode.V;
                case ScratchKey.W:
                    return KeyCode.W;
                case ScratchKey.X:
                    return KeyCode.X;
                case ScratchKey.Y:
                    return KeyCode.Y;
                case ScratchKey.Z:
                    return KeyCode.Z;
                case ScratchKey.Digit0:
                    return KeyCode.Alpha0;
                case ScratchKey.Digit1:
                    return KeyCode.Alpha1;
                case ScratchKey.Digit2:
                    return KeyCode.Alpha2;
                case ScratchKey.Digit3:
                    return KeyCode.Alpha3;
                case ScratchKey.Digit4:
                    return KeyCode.Alpha4;
                case ScratchKey.Digit5:
                    return KeyCode.Alpha5;
                case ScratchKey.Digit6:
                    return KeyCode.Alpha6;
                case ScratchKey.Digit7:
                    return KeyCode.Alpha7;
                case ScratchKey.Digit8:
                    return KeyCode.Alpha8;
                case ScratchKey.Digit9:
                    return KeyCode.Alpha9;
                default:
                    Debug.LogWarning($"未対応の ScratchKey が指定されました: {key}");
                    return KeyCode.None;
            }
        }
    }
}
