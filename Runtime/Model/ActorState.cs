// Updated: 2025-02-14
using UnityEngine;

namespace FUnity.Runtime.Model
{
    /// <summary>
    /// 俳優（キャラクター）のランタイム状態を保持する Model レイヤーの値オブジェクト。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnity.Runtime.Presenter.ActorPresenter"/>（Presenter が唯一の更新窓口）
    /// 想定ライフサイクル: プレイ中に <see cref="FUnity.Runtime.Presenter.ActorPresenter"/> が生成・保持し、フレームごとに座標を更新する。
    /// スレッド/GC: Unity メインスレッドでのみ利用することを前提とした軽量なクラス。
    /// </remarks>
    public sealed class ActorState
    {
        /// <summary>
        /// UI Toolkit の左上原点座標系での現在位置（px）。Presenter が一方向に更新し View は読み取りのみを行う。
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// 1 秒あたりの移動速度（px/s）。<see cref="FUnity.Runtime.Presenter.ActorPresenter"/> が <see cref="FUnity.Runtime.Core.FUnityActorData.MoveSpeed"/> を反映して管理する。
        /// </summary>
        public float Speed;
    }
}
