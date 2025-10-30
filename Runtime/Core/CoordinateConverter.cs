using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Authoring;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// UI Toolkit 座標（左上原点・下方向が正）と FUnity 論理座標の相互変換を提供するユーティリティです。
    /// 制作モードに応じて原点の種類を切り替え、Scratch と unityroom で異なる基準を安全に扱えるようにします。
    /// </summary>
    public static class CoordinateConverter
    {
        /// <summary>
        /// UI Toolkit 座標を論理座標へ変換します。原点が中央の場合は Y 軸を反転して上方向を正とします。
        /// </summary>
        /// <param name="uiPx">左上原点基準の UI 座標。</param>
        /// <param name="stageRoot">ステージの基準要素。null や未レイアウト時はフォールバックとして元値を返します。</param>
        /// <param name="origin">使用する原点種別。</param>
        /// <returns>論理座標。中央原点時は右が +X、上が +Y。</returns>
        public static Vector2 UIToLogical(Vector2 uiPx, VisualElement stageRoot, CoordinateOrigin origin)
        {
            if (origin != CoordinateOrigin.Center)
            {
                return uiPx;
            }

            if (stageRoot == null)
            {
                return uiPx;
            }

            var rs = stageRoot.resolvedStyle;
            var width = rs.width;
            var height = rs.height;

            if (width <= 0f || height <= 0f)
            {
                return uiPx;
            }

            var x = uiPx.x - (width * 0.5f);
            var y = (height * 0.5f) - uiPx.y;
            return new Vector2(x, y);
        }

        /// <summary>
        /// 論理座標を UI Toolkit の座標系へ変換します。中央原点の場合は上方向を負として UI 座標へ写像します。
        /// </summary>
        /// <param name="logical">論理座標。</param>
        /// <param name="stageRoot">ステージの基準要素。null や未レイアウト時は入力値をそのまま返します。</param>
        /// <param name="origin">使用する原点種別。</param>
        /// <returns>左上原点基準の UI 座標。</returns>
        public static Vector2 LogicalToUI(Vector2 logical, VisualElement stageRoot, CoordinateOrigin origin)
        {
            if (origin != CoordinateOrigin.Center)
            {
                return logical;
            }

            if (stageRoot == null)
            {
                return logical;
            }

            var rs = stageRoot.resolvedStyle;
            var width = rs.width;
            var height = rs.height;

            if (width <= 0f || height <= 0f)
            {
                return logical;
            }

            var x = logical.x + (width * 0.5f);
            var y = (height * 0.5f) - logical.y;
            return new Vector2(x, y);
        }

        /// <summary>
        /// アクティブなモード設定から原点種別を解決します。設定が null の場合は TopLeft を返します。
        /// </summary>
        /// <param name="activeConfig">現在利用中のモード設定。</param>
        /// <returns>設定された <see cref="CoordinateOrigin"/>。</returns>
        public static CoordinateOrigin GetActiveOrigin(FUnityModeConfig activeConfig)
        {
            return activeConfig != null ? activeConfig.Origin : CoordinateOrigin.TopLeft;
        }
    }
}
