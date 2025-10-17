using UnityEngine;

namespace FUnity.Runtime.Model
{
    /// <summary>
    /// プレイ中に変化する俳優の状態を保持します。
    /// </summary>
    public sealed class ActorState
    {
        /// <summary>UI座標系での現在位置（px）。</summary>
        public Vector2 Position;

        /// <summary>1秒あたりの移動速度（px/s）。</summary>
        public float Speed;
    }
}
