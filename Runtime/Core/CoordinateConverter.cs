using UnityEngine;
using FUnity.Runtime.Authoring;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// UI Toolkit の左上原点座標と FUnity の論理座標（Scratch 時は中央原点）を相互変換するユーティリティです。
    /// Scratch では常に 480×360 の論理サイズを想定し、中心を (0,0) として計算します。
    /// </summary>
    public static class CoordinateConverter
    {
        /// <summary>
        /// 座標系の原点種別を示す列挙体です。UI と論理座標の変換方式を統一的に扱うために使用します。
        /// </summary>
        public enum OriginMode
        {
            /// <summary>左上を原点とする座標系です。unityroom モードなどで採用されます。</summary>
            TopLeft,

            /// <summary>中央を原点とし、右を +X / 上を +Y とする座標系です。Scratch 互換モードで使用します。</summary>
            Center
        }

        /// <summary>Scratch 論理座標の幅（px）。</summary>
        private const float ScratchLogicalWidth = 480f;

        /// <summary>Scratch 論理座標の高さ（px）。</summary>
        private const float ScratchLogicalHeight = 360f;

        /// <summary>Scratch 論理座標の半幅（px）。</summary>
        private const float ScratchHalfWidth = ScratchLogicalWidth * 0.5f;

        /// <summary>Scratch 論理座標の半高さ（px）。</summary>
        private const float ScratchHalfHeight = ScratchLogicalHeight * 0.5f;

        /// <summary>
        /// 現在のモード設定から原点種別を決定します。Scratch モードでは中央原点を強制し、
        /// それ以外は設定済みの Origin を優先します。
        /// </summary>
        /// <param name="activeConfig">アクティブなモード設定。</param>
        /// <returns>使用する座標原点。</returns>
        public static CoordinateOrigin GetActiveOrigin(FUnityModeConfig activeConfig)
        {
            var originMode = GetActiveOriginMode(activeConfig);
            return ToCoordinateOrigin(originMode);
        }

        /// <summary>
        /// 現在のモード設定から原点種別を <see cref="OriginMode"/> として決定します。
        /// </summary>
        /// <param name="activeConfig">アクティブなモード設定。</param>
        /// <returns>使用する座標原点。</returns>
        public static OriginMode GetActiveOriginMode(FUnityModeConfig activeConfig)
        {
            if (activeConfig == null)
            {
                return OriginMode.TopLeft;
            }

            if (activeConfig.Mode == FUnityAuthoringMode.Scratch)
            {
                return OriginMode.Center;
            }

            return ToOriginMode(activeConfig.Origin);
        }

        /// <summary>
        /// <see cref="CoordinateOrigin"/> から <see cref="OriginMode"/> への変換を行います。
        /// </summary>
        /// <param name="origin">変換対象の原点列挙値。</param>
        /// <returns>対応する <see cref="OriginMode"/>。</returns>
        public static OriginMode ToOriginMode(CoordinateOrigin origin)
        {
            return origin == CoordinateOrigin.Center ? OriginMode.Center : OriginMode.TopLeft;
        }

        /// <summary>
        /// <see cref="OriginMode"/> から <see cref="CoordinateOrigin"/> への変換を行います。
        /// </summary>
        /// <param name="originMode">変換対象の原点列挙値。</param>
        /// <returns>対応する <see cref="CoordinateOrigin"/>。</returns>
        public static CoordinateOrigin ToCoordinateOrigin(OriginMode originMode)
        {
            return originMode == OriginMode.Center ? CoordinateOrigin.Center : CoordinateOrigin.TopLeft;
        }

        /// <summary>
        /// UI 座標（左上原点／下+Y）を論理座標（Scratch では中央原点／上+Y）へ変換します。
        /// </summary>
        /// <param name="uiPx">UI Toolkit ベースの座標。</param>
        /// <param name="mode">モード設定。null の場合は左上原点扱い。</param>
        /// <returns>論理座標（px）。</returns>
        public static Vector2 UIToLogical(Vector2 uiPx, FUnityModeConfig mode)
        {
            var origin = GetActiveOrigin(mode);
            return UIToLogical(uiPx, origin);
        }

        /// <summary>
        /// 原点種別を直接指定して UI 座標から論理座標へ変換します。
        /// </summary>
        /// <param name="uiPx">UI Toolkit ベースの座標。</param>
        /// <param name="origin">使用する原点。</param>
        /// <returns>論理座標（px）。</returns>
        public static Vector2 UIToLogical(Vector2 uiPx, CoordinateOrigin origin)
        {
            var mode = ToOriginMode(origin);
            return UIToLogical(uiPx, mode);
        }

        /// <summary>
        /// 原点種別を直接指定して UI 座標から論理座標へ変換します。
        /// </summary>
        /// <param name="uiPx">UI Toolkit ベースの座標。</param>
        /// <param name="origin">使用する原点。</param>
        /// <returns>論理座標（px）。</returns>
        public static Vector2 UIToLogical(Vector2 uiPx, OriginMode origin)
        {
            if (origin == OriginMode.Center)
            {
                var logicalX = uiPx.x - ScratchHalfWidth;
                var logicalY = ScratchHalfHeight - uiPx.y;
                return new Vector2(logicalX, logicalY);
            }

            return uiPx;
        }

        /// <summary>
        /// 論理座標（Scratch では中央原点／上+Y）を UI 座標（左上原点／下+Y）へ変換します。
        /// </summary>
        /// <param name="logical">論理座標。</param>
        /// <param name="mode">モード設定。null の場合は左上原点扱い。</param>
        /// <returns>UI Toolkit ベースの座標。</returns>
        public static Vector2 LogicalToUI(Vector2 logical, FUnityModeConfig mode)
        {
            var origin = GetActiveOrigin(mode);
            return LogicalToUI(logical, origin);
        }

        /// <summary>
        /// 原点種別を直接指定して論理座標から UI 座標へ変換します。
        /// </summary>
        /// <param name="logical">論理座標。</param>
        /// <param name="origin">使用する原点。</param>
        /// <returns>UI Toolkit ベースの座標。</returns>
        public static Vector2 LogicalToUI(Vector2 logical, CoordinateOrigin origin)
        {
            var mode = ToOriginMode(origin);
            return LogicalToUI(logical, mode);
        }

        /// <summary>
        /// 原点種別を直接指定して論理座標から UI 座標へ変換します。
        /// </summary>
        /// <param name="logical">論理座標。</param>
        /// <param name="origin">使用する原点。</param>
        /// <returns>UI Toolkit ベースの座標。</returns>
        public static Vector2 LogicalToUI(Vector2 logical, OriginMode origin)
        {
            if (origin == OriginMode.Center)
            {
                var uiX = logical.x + ScratchHalfWidth;
                var uiY = ScratchHalfHeight - logical.y;
                return new Vector2(uiX, uiY);
            }

            return logical;
        }
    }
}
