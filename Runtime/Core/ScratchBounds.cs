using UnityEngine;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// Scratch モードにおける中心座標クランプ処理を集約するユーティリティです。
    /// ステージの半幅/半高さ (240/180) を基準に、スプライトの実サイズを考慮した移動範囲を提供します。
    /// </summary>
    public static class ScratchBounds
    {
        /// <summary>Scratch ステージの半幅（px）。</summary>
        public const float StageHalfW = 240f;

        /// <summary>Scratch ステージの半高さ（px）。</summary>
        public const float StageHalfH = 180f;

        /// <summary>
        /// 拡大率適用後のスプライトサイズを考慮し、中心座標を許容範囲へクランプします。
        /// 幅・高さの半分をステージ半径に足し引きした境界で判定し、回転は中心回転を前提とします。
        /// </summary>
        /// <param name="center">中心座標（Scratch 論理座標系）。</param>
        /// <param name="scaledSize">拡大率適用後の幅・高さ（px）。</param>
        /// <returns>指定ルールでクランプした中心座標。</returns>
        public static Vector2 ClampCenter(Vector2 center, Vector2 scaledSize)
        {
            var width = Mathf.Max(0f, scaledSize.x);
            var height = Mathf.Max(0f, scaledSize.y);
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;

            var minX = -StageHalfW - halfWidth;
            var maxX = StageHalfW + halfWidth;
            var minY = -StageHalfH - halfHeight;
            var maxY = StageHalfH + halfHeight;

            var clampedX = Mathf.Clamp(center.x, minX, maxX);
            var clampedY = Mathf.Clamp(center.y, minY, maxY);
            return new Vector2(clampedX, clampedY);
        }
    }
}
