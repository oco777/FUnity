using System;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// FUnity が提供する制作モードの区分を表す列挙体です。
    /// Scratch 互換重視か unityroom 公開最適化かを明示し、各種設定の切り替えに利用します。
    /// </summary>
    [Serializable]
    public enum FUnityAuthoringMode
    {
        /// <summary>
        /// Scratch と同じステージ解像度・座標系・ブロック互換を目指すモードです。
        /// </summary>
        Scratch = 0,

        /// <summary>
        /// unityroom での WebGL 公開を前提に、16:9 解像度と Unity 2D 拡張機能を解放するモードです。
        /// </summary>
        Unityroom = 1
    }
}
