// Updated: 2025-02-14
using UnityEngine;

namespace FUnity.Runtime.Model
{
    /// <summary>
    /// Scratch 互換の回転スタイルを表現する列挙体です。
    /// </summary>
    public enum RotationStyle
    {
        /// <summary>全方向へ自由に回転します。</summary>
        AllAround = 0,

        /// <summary>左右反転のみで見た目の向きを表現します。</summary>
        LeftRight = 1,

        /// <summary>見た目を常に直立させ、回転を行いません。</summary>
        DontRotate = 2
    }

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
        /// 俳優に適用する見た目の効果量を保持する構造体です。
        /// Scratch の Looks カテゴリに存在する「色の効果」などを数値で表現し、Presenter が View へ渡します。
        /// </summary>
        public struct GraphicEffectsState
        {
            /// <summary>
            /// 色相回転量（Scratch の「色の効果」相当）。
            /// 200 で一周とみなし、負値は 0～200 の範囲に正規化して扱います。
            /// </summary>
            public float ColorEffect;
        }

        /// <summary>
        /// 論理座標での現在位置（px）。原点や軸方向は <see cref="FUnityModeConfig"/> の設定に従い、Presenter が一方向に更新します。
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
        /// 現在の回転角度（度）。UI 上のポートレートを中心ピボットで回転させる際に利用し、0～360 度へ正規化して保持する。
        /// </summary>
        public float RotationDeg;

        /// <summary>
        /// 現在の拡大率（%）。100 を等倍とし、Presenter が 1～300 % の範囲へクランプして管理する。
        /// </summary>
        public float SizePercent = 100f;

        /// <summary>
        /// 現在参照している SpriteList のインデックス。Sprite 未使用時は 0 を保持する。
        /// </summary>
        public int SpriteIndex;

        /// <summary>
        /// 現在のコスチュームを内部 0 始まりインデックスで保持するフィールドです。
        /// Scratch の UI に表示される「コスチューム番号」が 1 始まりであるのに対し、内部では 0 始まりで統一管理します。
        /// </summary>
        private int m_CostumeIndex;

        /// <summary>
        /// 現在のコスチュームを 0 始まりインデックスで表す値。
        /// Scratch の「コスチューム番号」が 1 始まりであるのに対して、内部では 0 始まりのインデックスとして統一管理する。
        /// </summary>
        public int CostumeIndex
        {
            get => m_CostumeIndex;
            set => m_CostumeIndex = value;
        }

        /// <summary>
        /// 見た目の回転挙動を指定する Scratch 互換の回転スタイルです。
        /// </summary>
        public RotationStyle RotationStyle = RotationStyle.AllAround;

        /// <summary>
        /// 現在の描画効果（色相など）を保持します。
        /// Presenter から View へ転送する際に参照され、UI Toolkit の Tint へ変換されます。
        /// </summary>
        public GraphicEffectsState Effects;

        /// <summary>
        /// コスチューム数に応じてインデックスを 0..(count-1) の範囲にクランプ/ラップする。
        /// count が 0 以下のときは 0 に固定する。
        /// </summary>
        /// <param name="count">利用可能なコスチューム数。</param>
        public void NormalizeCostumeIndex(int count)
        {
            if (count <= 0)
            {
                m_CostumeIndex = 0;
                return;
            }

            var mod = m_CostumeIndex % count;
            if (mod < 0)
            {
                mod += count;
            }

            m_CostumeIndex = mod;
        }

        /// <summary>
        /// 次のコスチュームに進めるヘルパー。
        /// count が 0 の場合は何もしない。
        /// </summary>
        /// <param name="count">利用可能なコスチューム数。</param>
        public void AdvanceCostume(int count)
        {
            if (count <= 0)
            {
                return;
            }

            m_CostumeIndex++;
            NormalizeCostumeIndex(count);
        }

        /// <summary>
        /// 外部から与えられた座標を指定された範囲内にクランプして保持する。
        /// </summary>
        /// <param name="pos">設定したい座標（px）。</param>
        /// <param name="boundsPx">許容される座標範囲（px）。</param>
        public void SetPositionClamped(Vector2 pos, Rect boundsPx)
        {
            Position = Clamp(pos, boundsPx);
        }

        /// <summary>
        /// 現在位置に差分を加算し、指定範囲内にクランプして保持する。
        /// </summary>
        /// <param name="delta">加算する座標差分（px）。</param>
        /// <param name="boundsPx">許容される座標範囲（px）。</param>
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
