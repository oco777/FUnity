// Updated: 2025-10-21
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「大きさを ◯ % にする」ブロックに対応し、Presenter 経由で俳優の拡大率を絶対値で適用するカスタム Unit です。
    /// Presenter 側で #root 要素へスケールを反映するため、左上座標がスケール変更時も維持されます。
    /// </summary>
    [UnitTitle("大きさを○%にする")]
    [UnitCategory("FUnity/Scratch/見た目")]
    [UnitSubtitle("funity scratch 見た目 size scale 大きさ percent")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetSizePercentUnit : Unit
    {
        /// <summary>フローの開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>適用する拡大率（%）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Percent;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>percent ポートへの参照を公開します。</summary>
        public ValueInput Percent => m_Percent;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と percent の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Percent = ValueInput<float>("percent", 100f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 受信時に Presenter とアダプタを解決し、拡大率を絶対値で適用します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var percent = flow.GetValue<float>(m_Percent);

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Set Size %: ActorPresenterAdapter が未解決のため拡大率を Presenter に転送できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            adapter.SetSizePercent(percent);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「大きさを ◯ % ずつ変える」ブロックに対応し、Presenter 経由で拡大率を相対変更するカスタム Unit です。
    /// 拡縮対象は Presenter から View へ伝搬され、#root 要素が左上原点のまま拡縮されます。
    /// </summary>
    [UnitTitle("大きさを○%ずつ変える")]
    [UnitCategory("FUnity/Scratch/見た目")]
    [UnitSubtitle("funity scratch 見た目 size scale change 大きさ percent")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ChangeSizeByPercentUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>加算する拡大率（%）を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaPercent;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>deltaPercent ポートへの参照を公開します。</summary>
        public ValueInput DeltaPercent => m_DeltaPercent;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と deltaPercent の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_DeltaPercent = ValueInput<float>("deltaPercent", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter を受信した際に Presenter とアダプタを解決し、現在の拡大率に差分を加算します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var delta = flow.GetValue<float>(m_DeltaPercent);

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Change Size by %: ActorPresenterAdapter が未解決のため拡大率の差分を Presenter に転送できません。VSPresenterBridge などでアダプタを供給してください。");
                yield return m_Exit;
                yield break;
            }

            adapter.ChangeSizeByPercent(delta);
            yield return m_Exit;
        }
    }
}
