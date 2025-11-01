// Updated: 2025-03-03
using System;
using UnityEngine;
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
        /// 俳優のポートレート画像を View に設定する。
        /// </summary>
        /// <param name="sprite">UI に表示するスプライト。null の場合は呼び出し側で未設定扱い。</param>
        /// <example>
        /// <code>
        /// actorView.SetPortrait(portraitSprite);
        /// </code>
        /// </example>
        void SetPortrait(Sprite sprite);

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
        /// </summary>
        /// <param name="percent">100 で等倍となる拡大率（%）。</param>
        void SetSizePercent(float percent);

        /// <summary>
        /// 回転角度（度）を UI に適用する。中心ピボットで回転させることを想定する。
        /// </summary>
        /// <param name="degrees">0～360 度に正規化済みの回転角度。</param>
        void SetRotationDegrees(float degrees);

        /// <summary>
        /// 吹き出しテキストを表示する。指定時間経過後は自動で非表示にする。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        /// <param name="seconds">表示継続時間（秒）。</param>
        void ShowSpeech(string message, float seconds);

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
        /// スケール適用後の俳優サイズ（px）を取得する。Scratch モードでのクランプ計算に利用する。
        /// </summary>
        /// <returns>拡大率を反映した幅・高さ（px）。要素未バインド時は <see cref="Vector2.zero"/>。</returns>
        Vector2 GetScaledSizePx();

        /// <summary>
        /// Presenter 参照を View 側へ伝達し、旧 API 経由のフォールバック呼び出しを可能にする。
        /// </summary>
        /// <param name="presenter">紐付ける <see cref="ActorPresenter"/>。</param>
        void SetActorPresenter(ActorPresenter presenter);
    }
}
