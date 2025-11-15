// Updated: 2025-10-23
using UnityEngine;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// Scratch角度（0°=上）と内部角度（0°=右）を相互変換するユーティリティです。
    /// Visual Scripting の Scratch 用ユニットでは Scratch角度で計算し、
    /// Presenter や ActorState に渡す直前で内部角度へ揃えるために利用します。
    /// </summary>
    public static class ScratchAngleUtil
    {
        /// <summary>
        /// Scratch角度（0°=上）を内部角度（0°=右）へ変換します。
        /// </summary>
        /// <param name="scratchDeg">Scratch角度（度）。</param>
        /// <returns>内部角度（度）。</returns>
        public static float ScratchToInternal(float scratchDeg)
        {
            return Mathf.Repeat(90f - scratchDeg, 360f);
        }

        /// <summary>
        /// 内部角度（0°=右）を Scratch角度（0°=上）へ変換します。
        /// </summary>
        /// <param name="internalDeg">内部角度（度）。</param>
        /// <returns>Scratch角度（度）。</returns>
        public static float InternalToScratch(float internalDeg)
        {
            return Mathf.Repeat(90f - internalDeg, 360f);
        }
    }
}
