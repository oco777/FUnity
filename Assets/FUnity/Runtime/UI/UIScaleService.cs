using System.Collections.Generic;
using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// Scratch モード時に UI ルート要素へ表示スケールを適用するユーティリティです。
    /// 論理座標は 480x360 に固定し、実ウィンドウが大きい場合のみ拡大します。
    /// </summary>
    public static class UIScaleService
    {
        /// <summary>Scratch モードの論理幅（px）。</summary>
        private const float LogicalWidth = 480f;

        /// <summary>Scratch モードの論理高さ（px）。</summary>
        private const float LogicalHeight = 360f;

        /// <summary>GeometryChangedEvent を登録済みのルート要素集合。</summary>
        private static readonly HashSet<VisualElement> m_HookedElements = new HashSet<VisualElement>();

        /// <summary>
        /// Scratch モードであればルート要素へ表示用スケールを適用し、必要に応じて GeometryChangedEvent を監視します。
        /// </summary>
        /// <param name="root">スケールを適用する対象ルート要素。</param>
        /// <param name="activeMode">現在のアクティブモード設定。Scratch 以外では何も行いません。</param>
        public static void ApplyScaleIfScratchMode(VisualElement root, FUnityModeConfig activeMode)
        {
            if (root == null)
            {
                Debug.LogWarning("[FUnity.UI] UIScaleService: ルート要素が null のためスケール適用を中止します。");
                return;
            }

            if (activeMode == null)
            {
                Debug.LogWarning("[FUnity.UI] UIScaleService: アクティブモード設定が null のため Scratch 拡大を適用できません。");
                return;
            }

            if (activeMode.Mode != FUnityAuthoringMode.Scratch)
            {
                return;
            }

            ApplyScale(root);

            if (m_HookedElements.Contains(root))
            {
                return;
            }

            m_HookedElements.Add(root);
            root.RegisterCallback<GeometryChangedEvent>(_ => ApplyScale(root));
        }

        /// <summary>
        /// 現在のパネルサイズから等倍以上となる最大スケールを計算し、ルート要素へ適用します。
        /// </summary>
        /// <param name="root">スケール対象のルート要素。</param>
        private static void ApplyScale(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            var width = root.worldBound.width;
            var height = root.worldBound.height;
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            var scaleX = width / LogicalWidth;
            var scaleY = height / LogicalHeight;
            var scale = Mathf.Max(1f, Mathf.Min(scaleX, scaleY));

            var currentScale = root.transform.scale;
            if (Mathf.Approximately(currentScale.x, scale) && Mathf.Approximately(currentScale.y, scale))
            {
                return;
            }

            root.transform.scale = new Vector3(scale, scale, 1f);
        }
    }
}
