// Updated: 2025-03-03
using UnityEngine;

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
        /// 左上原点（px）での座標を UI 上に反映する。
        /// </summary>
        /// <param name="pos">UI Toolkit の描画領域内座標。Presenter 側でクランプ済みであることを想定。</param>
        /// <example>
        /// <code>
        /// actorView.SetPosition(model.Position);
        /// </code>
        /// </example>
        void SetPosition(Vector2 pos);

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
        /// 吹き出しテキストを表示する。指定時間経過後は自動で非表示にする。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        /// <param name="seconds">表示継続時間（秒）。</param>
        void ShowSpeech(string message, float seconds);
    }
}
