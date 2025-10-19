// Updated: 2025-03-03
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// ステージ背景を UI Toolkit のルート要素へ反映するための軽量サービス。
    /// Presenter や Visual Scripting から呼び出し可能な安全な窓口を提供する。
    /// </summary>
    public sealed class StageBackgroundService
    {
        /// <summary>背景適用先の UI Toolkit ルート要素。null の場合は処理を無視する。</summary>
        private VisualElement m_TargetRoot;

        /// <summary>最後に適用した背景色。`Configure` 後の即時反映に利用する。</summary>
        private Color m_LastColor = Color.black;

        /// <summary>最後に適用した背景画像のスケールモード。テクスチャ未指定時は ScaleAndCrop。</summary>
        private ScaleMode m_LastScaleMode = ScaleMode.ScaleAndCrop;

        /// <summary>
        /// 背景を適用する対象ルートを登録し、既知の背景設定があれば即時反映する。
        /// </summary>
        /// <param name="root">UI Document の <see cref="VisualElement.rootVisualElement"/>。</param>
        public void Configure(VisualElement root)
        {
            m_TargetRoot = root;
            ApplyColor(m_LastColor);
        }

        /// <summary>
        /// ステージ設定全体を一括適用するユーティリティ。色→画像の順で反映する。
        /// </summary>
        /// <param name="stage">背景色・テクスチャを保持する <see cref="FUnityStageData"/>。</param>
        public void ApplyStage(FUnityStageData stage)
        {
            if (stage == null)
            {
                return;
            }

            SetBackgroundColor(stage.BackgroundColor);
            SetBackground(stage.BackgroundImage, stage.BackgroundScale);
        }

        /// <summary>
        /// 背景色を更新し、ルート要素へ即座に適用する。
        /// </summary>
        /// <param name="color">適用する色。</param>
        public void SetBackgroundColor(Color color)
        {
            m_LastColor = color;
            ApplyColor(color);
        }

        /// <summary>
        /// 指定したテクスチャを背景に設定する。null を渡すと背景画像をクリアする。
        /// </summary>
        /// <param name="texture">背景に使用するテクスチャ。</param>
        /// <param name="scale">UI Toolkit の background scale mode。</param>
        public void SetBackground(Texture2D texture, ScaleMode scale)
        {
            m_LastScaleMode = scale;
            ApplyTexture(texture, scale);
        }

        /// <summary>
        /// Resources パスをもとにテクスチャを読み込み、背景へ適用する。
        /// </summary>
        /// <param name="resourcesPath">`Resources.Load` 互換のパス。拡張子不要。</param>
        public void SetBackground(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                Debug.LogWarning("[FUnity] StageBackgroundService: resourcesPath が空のため背景を変更できません。");
                return;
            }

            var texture = Resources.Load<Texture2D>(resourcesPath);
            if (texture == null)
            {
                Debug.LogWarning($"[FUnity] StageBackgroundService: Resources/{resourcesPath} が見つからず背景を変更できません。");
                return;
            }

            SetBackground(texture, m_LastScaleMode);
        }

        /// <summary>
        /// 内部状態に保存された色をルート要素へ適用する。
        /// </summary>
        /// <param name="color">適用する色。</param>
        private void ApplyColor(Color color)
        {
            if (m_TargetRoot == null)
            {
                return;
            }

            m_TargetRoot.style.backgroundColor = color;
        }

        /// <summary>
        /// テクスチャを背景に適用し、null 時は背景画像を解除する。
        /// </summary>
        /// <param name="texture">背景画像。</param>
        /// <param name="scale">スケールモード。</param>
        private void ApplyTexture(Texture2D texture, ScaleMode scale)
        {
            if (m_TargetRoot == null)
            {
                return;
            }

            if (texture != null)
            {
                m_TargetRoot.style.backgroundImage = new StyleBackground(texture);
                m_TargetRoot.style.unityBackgroundScaleMode = scale;
            }
            else
            {
                m_TargetRoot.style.backgroundImage = new StyleBackground();
            }
        }
    }
}
