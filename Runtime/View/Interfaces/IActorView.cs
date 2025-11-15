// Updated: 2025-03-03
using System;
using UnityEngine;
using FUnity.Runtime.Model;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.View
{
    /// <summary>
    /// Presenter 層からの命令を受け取り、UI Toolkit 上に俳優の状態を反映する View の抽象契約。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnity.Runtime.Presenter.ActorPresenter"/>
    /// 想定ライフサイクル: Presenter 初期化時に具象 View がバインドされ、以後は Presenter からの呼び出しのみ。
    /// 実装者は表示更新以外の責務を持たない（ロジックは Presenter に集約）。
    /// </remarks>
    public interface IActorView
    {
        /// <summary>
        /// ステージ領域（UI Toolkit パネル内の有効描画範囲）が更新された際に発火するイベント。
        /// Presenter はこのイベントを購読し、境界更新後に Model 側のクランプ値を調整する。
        /// </summary>
        event Action<Rect> StageBoundsChanged;

        /// <summary>
        /// 俳優画像の中心座標（px）を UI 上に反映する。
        /// </summary>
        /// <param name="center">ステージ左上を原点とした中心座標。Presenter 側でクランプ済みであることを想定します。</param>
        /// <example>
        /// <code>
        /// actorView.SetCenterPosition(centerPx);
        /// </code>
        /// </example>
        void SetCenterPosition(Vector2 center);

        /// <summary>
        /// 俳優のポートレート画像を View に設定する。Sprite 優先で描画し、未設定時は Texture2D をフォールバックとして利用する。
        /// </summary>
        /// <param name="sprite">UI に表示するスプライト。null の場合は Texture2D へフォールバックする。</param>
        /// <param name="fallbackTexture">Sprite 未設定時に使用する従来の Texture2D。</param>
        /// <example>
        /// <code>
        /// actorView.SetPortrait(sprite, portraitTexture);
        /// </code>
        /// </example>
        void SetPortrait(Sprite sprite, Texture2D fallbackTexture);

        /// <summary>
        /// 幅と高さを指定し、俳優要素のサイズを更新する。
        /// </summary>
        /// <param name="size">幅・高さ（px）。負値は 0 として扱う。</param>
        void SetSize(Vector2 size);

        /// <summary>
        /// 等倍基準のスケール値を適用する。
        /// </summary>
        /// <param name="scale">適用するスケール。0 より大きい値を想定。</param>
        void SetScale(float scale);

        /// <summary>
        /// 拡大率（%）を適用し、#root 要素を左上原点のまま等比スケールさせる。
        /// 実装は内部的に <see cref="SetScale(float)"/> へフォワードしてもよい。
        /// </summary>
        /// <param name="percent">100 で等倍となる拡大率（%）。</param>
        void SetSizePercent(float percent);

        /// <summary>
        /// 回転角度（度）を UI に適用する。中心ピボットで回転させることを想定する。
        /// </summary>
        /// <param name="degrees">0～360 度に正規化済みの回転角度。</param>
        void SetRotationDegrees(float degrees);

        /// <summary>
        /// 左右反転の符号を指定し、見た目の向きを即時更新する。
        /// </summary>
        /// <param name="sign">+1 で通常、-1 で反転。0 近傍は +1 として扱う。</param>
        void SetHorizontalFlipSign(float sign);

        /// <summary>
        /// 俳優要素の表示/非表示を切り替える。style.display を利用し、アニメーションを伴わず即時に反映する。
        /// </summary>
        /// <param name="visible">true で表示、false で非表示。</param>
        void SetVisible(bool visible);

        /// <summary>
        /// モデルで保持している描画効果（色相など）を UI に適用する。
        /// </summary>
        /// <param name="effects">適用する描画効果の状態。</param>
        void ApplyGraphicEffects(ActorState.GraphicEffectsState effects);

        /// <summary>
        /// 描画効果を初期状態に戻し、Tint を無効化する。
        /// </summary>
        void ResetEffects();

        /// <summary>
        /// 吹き出しを表示し、発言か思考かに応じてスタイルを切り替える。
        /// </summary>
        /// <param name="text">表示する本文。null の場合は空文字。</param>
        /// <param name="isThought">思考吹き出しなら true。</param>
        void ShowSpeech(string text, bool isThought);

        /// <summary>
        /// 表示中の吹き出しを即座に非表示へ戻す。
        /// </summary>
        void HideSpeech();

        /// <summary>
        /// 現在のステージ境界（px）を取得する。取得に成功した場合のみ <c>true</c> を返す。
        /// </summary>
        /// <param name="boundsPx">左上原点ベースの境界矩形。</param>
        /// <returns>境界を取得できた場合は <c>true</c>。</returns>
        bool TryGetStageBounds(out Rect boundsPx);

        /// <summary>
        /// 表示中の俳優要素のピクセルサイズを取得する。レイアウト未確定時は既定値を返す。
        /// </summary>
        /// <param name="sizePx">幅・高さ（px）。</param>
        /// <returns>取得できた場合は <c>true</c>。</returns>
        bool TryGetVisualSize(out Vector2 sizePx);

        /// <summary>
        /// スケール適用後の #root サイズ（px）を取得する。Scratch モードのクランプや表示計算に利用する。
        /// </summary>
        /// <returns>
        /// #root のレイアウトサイズへ scale を掛け合わせた実寸（px）。未解決時は <see cref="Vector2.zero"/>。
        /// </returns>
        Vector2 GetRootScaledSizePx();

        /// <summary>
        /// 旧互換 API。現在は <see cref="GetRootScaledSizePx"/> を呼び出す薄いラッパーとして提供する。
        /// </summary>
        /// <returns>#root のスケール後サイズ（px）。</returns>
        Vector2 GetScaledSizePx();

        /// <summary>
        /// Presenter 参照を View 側へ伝達し、旧 API 経由のフォールバック呼び出しを可能にする。
        /// </summary>
        /// <param name="presenter">紐付ける <see cref="ActorPresenter"/>。</param>
        void SetActorPresenter(ActorPresenter presenter);
    }
}
