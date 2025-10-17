// Updated: 2025-02-14
using UnityEngine;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 入力デバイスから移動ベクトルを読み取り、Presenter 層へ供給する入力アダプタ。
    /// </summary>
    /// <remarks>
    /// 依存関係: 旧 Input Manager（<see cref="UnityEngine.Input"/>）。新 Input System への移行を想定しており、
    ///     抽象化レイヤーとして実装している。
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> が常駐させ、<see cref="ActorPresenter"/> から毎フレーム参照。
    /// </remarks>
    public sealed class InputPresenter
    {
        /// <summary>
        /// 水平方向・垂直方向の入力を読み取り、UI 左上原点に合わせて Y 軸を反転したベクトルを返す。
        /// </summary>
        /// <returns>正規化前の入力ベクトル。値域は -1～1 を想定。</returns>
        /// <example>
        /// <code>
        /// var move = m_InputPresenter.ReadMove();
        /// </code>
        /// </example>
        public Vector2 ReadMove()
        {
            var x = UnityEngine.Input.GetAxisRaw("Horizontal");
            var y = -UnityEngine.Input.GetAxisRaw("Vertical");
            return new Vector2(x, y);
        }
    }
}
