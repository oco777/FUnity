// Updated: 2025-10-19
using UnityEngine;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch 系 Unit 共通の補助処理を提供するユーティリティクラスです。
    /// </summary>
    internal static class ScratchUnitUtil
    {
        /// <summary>
        /// 明示的に指定された <see cref="ActorPresenterAdapter"/>（旧称 FooniController）が存在しない場合、シーン内から最初に見つかったインスタンスを返します。
        /// </summary>
        /// <param name="explicitTarget">ポートに接続されたターゲット。null の場合は自動探索を行います。</param>
        /// <returns>解決された <see cref="ActorPresenterAdapter"/>。見つからなかった場合は null を返します。</returns>
        public static ActorPresenterAdapter ResolveTarget(ActorPresenterAdapter explicitTarget)
        {
            if (explicitTarget != null)
            {
                return explicitTarget;
            }

            return Object.FindFirstObjectByType<ActorPresenterAdapter>();
        }

        /// <summary>
        /// Scratch 方位（0°=右、90°=上、180°=左、270°=下）を UI 座標系（右=+X、下=+Y）へ変換した単位ベクトルを返します。
        /// </summary>
        /// <param name="degrees">変換する角度（度）。</param>
        /// <returns>UI 座標系における進行方向ベクトル。</returns>
        public static Vector2 DirFromDegrees(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
        }
    }
}
