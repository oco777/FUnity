using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Authoring;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// UI Toolkit座標（左上原点, 下+Y）とFUnity論理座標の相互変換を行います。
    /// モード設定（Origin）に応じて変換式を切り替えます。
    /// </summary>
    public static class CoordinateConverter
    {
        /// <summary>UIピクセル→論理座標。</summary>
        /// <param name="uiPx">UI Toolkit座標（左上原点）。</param>
        /// <param name="stageRoot">ステージのルートVisualElement。</param>
        /// <param name="origin">原点種別（Scratch=Center, unityroom=TopLeft）。</param>
        public static Vector2 UIToLogical(Vector2 uiPx, VisualElement stageRoot, CoordinateOrigin origin)
        {
            if (stageRoot == null) return uiPx;

            var rs = stageRoot.resolvedStyle;
            float w = rs.width;
            float h = rs.height;

            if (origin == CoordinateOrigin.Center)
            {
                // Scratch: 中央原点。右+X, 上+Y
                float x = uiPx.x - (w * 0.5f);
                float y = (h * 0.5f) - uiPx.y;
                return new Vector2(x, y);
            }
            else
            {
                // TopLeft: 左上原点。右+X, 下+Y（UI Toolkit と一致）
                return uiPx;
            }
        }

        /// <summary>論理座標→UIピクセル。</summary>
        public static Vector2 LogicalToUI(Vector2 logical, VisualElement stageRoot, CoordinateOrigin origin)
        {
            if (stageRoot == null) return logical;

            var rs = stageRoot.resolvedStyle;
            float w = rs.width;
            float h = rs.height;

            if (origin == CoordinateOrigin.Center)
            {
                float x = logical.x + (w * 0.5f);
                float y = (h * 0.5f) - logical.y;
                return new Vector2(x, y);
            }
            else
            {
                return logical;
            }
        }

        /// <summary>現在のモード設定から原点種別を取得。</summary>
        public static CoordinateOrigin GetActiveOrigin(FUnityModeConfig activeConfig)
        {
            return (activeConfig != null) ? activeConfig.Origin : CoordinateOrigin.TopLeft;
        }
    }
}
