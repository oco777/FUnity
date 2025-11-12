// Updated: 2025-03-20
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Input;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe
{
    /// <summary>
    /// Scratch の「マウスのy座標」ブロックを再現し、ステージ中心原点でのマウス y 座標を返す Value Unit です。
    /// </summary>
    [UnitTitle("マウスのy座標")]
    [UnitShortTitle("マウスy")]
    [UnitCategory("FUnity/Scratch/調べる")]
    [UnitSubtitle("funity scratch 調べる mouse y position 座標")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class MouseYUnit : Unit
    {
        /// <summary>現在の y 座標を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Y;

        /// <summary>y 座標の出力ポートへの参照。</summary>
        public ValueOutput Y => m_Y;

        /// <summary>
        /// 出力ポートを定義し、フロー評価時にマウス座標サービスから値を取得します。
        /// </summary>
        protected override void Definition()
        {
            m_Y = ValueOutput<float>(nameof(Y), GetY);
        }

        /// <summary>
        /// マウス座標サービスから y 座標を取得し、未初期化時は 0 を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>マウスの y 座標（ステージ中心原点基準）。</returns>
        private float GetY(Flow flow)
        {
            var provider = MouseProvider ?? FUnityServices.MousePosition;
            return provider != null ? provider.Y : 0f;
        }

        /// <summary>静的プロパティへのアクセスを簡略化するためのヘルパー。</summary>
        private static IMousePositionProvider MouseProvider => FUnityManager.MouseProvider;
    }
}
