// Updated: 2025-03-15
using System;
using UnityEngine;
using FUnity.Runtime.Model;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.View
{
    /// <summary>
    /// ActorView が確保できなかった際の安全なダミー実装。
    /// 例外や NullReference を避けるため、すべての操作を無視しつつ <see cref="IActorView"/> 契約を満たす。
    /// </summary>
    public sealed class NullActorView : IActorView
    {
        /// <summary>シングルトンインスタンス。状態を持たないため共有して利用する。</summary>
        public static NullActorView Instance { get; } = new NullActorView();

        /// <summary>無効化されたイベント。購読・解除を許可するが発火しない。</summary>
        public event Action<Rect> StageBoundsChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// 外部からの生成を禁止するため private コンストラクターとする。
        /// </summary>
        private NullActorView()
        {
        }

        /// <inheritdoc />
        public void SetCenterPosition(Vector2 center)
        {
        }

        /// <inheritdoc />
        public void SetPortrait(Sprite sprite, Texture2D fallbackTexture)
        {
        }

        /// <inheritdoc />
        public void SetSize(Vector2 size)
        {
        }

        /// <inheritdoc />
        public void SetScale(float scale)
        {
        }

        /// <inheritdoc />
        public void SetSizePercent(float percent)
        {
        }

        /// <inheritdoc />
        public void SetRotationDegrees(float degrees)
        {
        }

        /// <inheritdoc />
        public void SetHorizontalFlipSign(float sign)
        {
        }

        /// <inheritdoc />
        public void SetVisible(bool visible)
        {
        }

        /// <inheritdoc />
        public void ApplyGraphicEffects(ActorState.GraphicEffectsState effects)
        {
        }

        /// <inheritdoc />
        public void ResetEffects()
        {
        }

        /// <inheritdoc />
        public void ShowSpeech(string text, bool isThought)
        {
        }

        /// <inheritdoc />
        public void HideSpeech()
        {
        }

        /// <inheritdoc />
        public bool TryGetStageBounds(out Rect boundsPx)
        {
            boundsPx = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetVisualSize(out Vector2 sizePx)
        {
            sizePx = default;
            return false;
        }

        /// <inheritdoc />
        public Vector2 GetRootScaledSizePx()
        {
            return Vector2.zero;
        }

        /// <inheritdoc />
        public Vector2 GetScaledSizePx()
        {
            return Vector2.zero;
        }

        /// <inheritdoc />
        public void SetActorPresenter(ActorPresenter presenter)
        {
        }
    }
}
