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
        public Vector2 Position { get; private set; }

        /// <summary>
        /// 1 秒あたりの移動速度（px/s）。<see cref="FUnity.Runtime.Presenter.ActorPresenter"/> が <see cref="FUnity.Runtime.Core.FUnityActorData.MoveSpeed"/> を反映して管理する。
        /// </summary>
        public float Speed;

        /// <summary>
        /// 現在の向き（度）。0=右、90=上、180=左、270=下を想定し、<see cref="FUnity.Runtime.Presenter.ActorPresenter"/> が移動命令を解釈する際に利用する。
        /// </summary>
        public float DirectionDeg;

        /// <summary>
        /// 外部から与えられた座標を指定された範囲内にクランプして保持する。
        /// </summary>
        /// <param name="pos">設定したい座標（px）。</param>
        /// <param name="boundsPx">許容される左上座標の範囲（px）。</param>
        public void SetPositionClamped(Vector2 pos, Rect boundsPx)
        {
            Position = Clamp(pos, boundsPx);
        }

        /// <summary>
        /// 現在位置に差分を加算し、指定範囲内にクランプして保持する。
        /// </summary>
        /// <param name="delta">加算する座標差分（px）。</param>
        /// <param name="boundsPx">許容される左上座標の範囲（px）。</param>
        public void AddPositionClamped(Vector2 delta, Rect boundsPx)
        {
            var target = Position + delta;
            Position = Clamp(target, boundsPx);
        }

        /// <summary>
        /// クランプを行わずに座標を直接設定する。ステージ境界未初期化時のフォールバック用途。
        /// </summary>
        /// <param name="pos">設定する座標（px）。</param>
        public void SetPositionUnchecked(Vector2 pos)
        {
            Position = pos;
        }

        /// <summary>
        /// 渡された座標を矩形内に収める内部補助メソッド。
        /// </summary>
        /// <param name="pos">調整対象の座標。</param>
        /// <param name="boundsPx">クランプ対象の矩形。</param>
        /// <returns>クランプ済みの座標。</returns>
        private static Vector2 Clamp(Vector2 pos, Rect boundsPx)
        {
            if (boundsPx.width <= 0f && boundsPx.height <= 0f)
            {
                return pos;
            }

            var minX = boundsPx.xMin;
            var minY = boundsPx.yMin;
            var maxX = boundsPx.xMax;
            var maxY = boundsPx.yMax;

            var clampedX = Mathf.Clamp(pos.x, minX, maxX);
            var clampedY = Mathf.Clamp(pos.y, minY, maxY);

            return new Vector2(clampedX, clampedY);
        }
    }
}
