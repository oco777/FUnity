// Updated: 2025-10-21
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Core;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇のクローンを作る」に対応し、現在の俳優 Presenter を複製してランタイムに登録する Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により自動解決されます。
    /// </summary>
    [UnitTitle("Scratch/Control/Create Clone of Self")]
    [UnitCategory("Scratch/Control")]
    public sealed class CreateCloneOfSelfUnit : Unit
    {
        /// <summary>制御フローの入口。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出口。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter→exit の制御線を定義します。</summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// アダプタから Presenter を取得し、FUnityManager にクローン生成を依頼します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>常に exit ポートを返します。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null || adapter.Presenter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Create Clone of Self: ActorPresenterAdapter を解決できないためクローンを生成できません。");
                return m_Exit;
            }

            FUnityManager.CloneActor(adapter.Presenter);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「クローンされたとき」イベントを表現し、新しく生成されたクローンごとにトリガーを発火します。
    /// </summary>
    [UnitTitle("Scratch/Control/When I Start as a Clone")]
    [UnitCategory("Scratch/Control")]
    public sealed class WhenIStartAsCloneUnit : EventUnit<CloneEventArgs>
    {
        /// <summary>EventBus に登録するかどうか。</summary>
        protected override bool register => true;

        /// <summary>クローン開始イベントに使用する EventHook を返します。</summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <returns>クローン開始イベントの EventHook。</returns>
        public override EventHook GetHook(GraphReference reference)
        {
            return new EventHook(FUnityEventNames.OnCloneStart, reference.self);
        }

        /// <summary>EventUnit 基底の定義を呼び出します。</summary>
        protected override void Definition()
        {
            base.Definition();
        }

        /// <summary>クローン開始イベントには追加引数が無いため何も行いません。</summary>
        protected override void AssignArguments(Flow flow, CloneEventArgs args)
        {
        }

        /// <summary>常に発火を許可します。</summary>
        protected override bool ShouldTrigger(Flow flow, CloneEventArgs args)
        {
            return true;
        }
    }

    /// <summary>
    /// Scratch の「このクローンを削除する」に対応し、現在の Presenter がクローンであれば安全に破棄します。
    /// </summary>
    [UnitTitle("Scratch/Control/Delete This Clone")]
    [UnitCategory("Scratch/Control")]
    public sealed class DeleteThisCloneUnit : Unit
    {
        /// <summary>制御フローの入口。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出口。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter→exit の制御線を定義します。</summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// クローンであれば FUnityManager に削除を依頼し、本体であれば警告のみを表示します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>常に exit ポートを返します。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            var presenter = adapter != null ? adapter.Presenter : null;
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Delete This Clone: ActorPresenter を解決できないため削除できません。");
                return m_Exit;
            }

            if (!presenter.IsClone)
            {
                Debug.LogWarning("[FUnity] Scratch/Delete This Clone: 本体は削除対象外のため処理をスキップします。");
                return m_Exit;
            }

            FUnityManager.DeleteActorClone(presenter);
            return m_Exit;
        }
    }
}
