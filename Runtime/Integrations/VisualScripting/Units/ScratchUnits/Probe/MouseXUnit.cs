// Updated: 2025-03-20
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Input;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe
{
    /// <summary>
    /// Scratch の「マウスのx座標」ブロックを再現し、ステージ中心原点でのマウス x 座標を返す Value Unit です。
    /// </summary>
    [UnitTitle("マウスのx座標")]
    [UnitShortTitle("マウスx")]
    [UnitCategory("FUnity/Blocks/調べる")]
    [UnitSubtitle("funity scratch 調べる mouse x position 座標")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class MouseXUnit : Unit
    {
        /// <summary>現在の x 座標を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_X;

        /// <summary>x 座標の出力ポートへの参照。</summary>
        public ValueOutput X => m_X;

        /// <summary>
        /// 出力ポートを定義し、フロー評価時にマウス座標サービスから値を取得します。
        /// </summary>
        protected override void Definition()
        {
            m_X = ValueOutput<float>(nameof(X), GetX);
        }

        /// <summary>
        /// マウス座標サービスから x 座標を取得し、未初期化時は 0 を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>マウスの x 座標（ステージ中心原点基準）。</returns>
        private float GetX(Flow flow)
        {
            var provider = MouseProvider ?? FUnityServices.MousePosition;
            return provider != null ? provider.X : 0f;
        }

        /// <summary>静的プロパティへのアクセスを簡略化するためのヘルパー。</summary>
        private static IMousePositionProvider MouseProvider => FUnityManager.MouseProvider;
    }
}
