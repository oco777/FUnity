// Updated: 2025-03-20
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe
{
    /// <summary>
    /// Scratch の「マウスポインターまでの距離」ブロックを再現し、自身の中心座標とマウス座標との差を返す Value Unit です。
    /// </summary>
    [UnitTitle("マウスポインターまでの距離")]
    [UnitCategory("FUnity/Scratch/調べる")]
    [UnitSubtitle("funity scratch 調べる distance mouse pointer 距離")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class DistanceToMousePointerUnit : Unit
    {
        /// <summary>距離（px）を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_Distance;

        /// <summary>距離出力ポートへの参照を公開します。</summary>
        public ValueOutput Distance => m_Distance;

        /// <summary>
        /// 出力ポートを構築し、評価時に距離を計算するよう設定します。
        /// </summary>
        protected override void Definition()
        {
            m_Distance = ValueOutput<float>(nameof(Distance), Evaluate);
        }

        /// <summary>
        /// 現在の俳優中心とマウスポインターとの距離を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>距離（px）。必要な情報が不足している場合は 0。</returns>
        private float Evaluate(Flow flow)
        {
            var provider = ScratchUnitUtil.ResolveMouseProvider();
            if (provider == null)
            {
                return 0f;
            }

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                return 0f;
            }

            var presenter = adapter.Presenter;
            if (presenter == null)
            {
                return 0f;
            }

            var center = ScratchUnitUtil.ClampToStageBounds(presenter.GetPosition());
            var mouse = ScratchUnitUtil.ClampToStageBounds(provider.StagePosition);
            return Vector2.Distance(center, mouse);
        }
    }
}
