// Updated: 2025-03-07
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「表示する」ブロックに対応し、俳優の可視状態を Presenter 経由で有効化する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("Scratch/Looks/Show")]
    [UnitCategory("FUnity/Scratch/Looks")]
    public sealed class ShowActorUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>可視状態を切り替える対象俳優を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Actor;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>actor ポートへの参照。</summary>
        public ValueInput Actor => m_Actor;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と actor 入力を登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Exit = ControlOutput("exit");
            m_Actor = ValueInput<ActorPresenterAdapter>("actor", null);
            m_Enter = ControlInput("enter", OnEnter);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、Presenter を解決して俳優を表示状態にする。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートを返し、後続へ制御を渡す。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var adapter = flow.GetValue<ActorPresenterAdapter>(m_Actor);
            ActorPresenter presenter = ScratchUnitUtil.ResolveActorPresenter(flow, adapter);
            if (presenter == null)
            {
                Debug.LogWarning(
                    "[FUnity] Scratch/Looks/Show: ActorPresenter を解決できなかったため表示状態を変更できません。" +
                    "アダプタや VSPresenterBridge の設定を確認してください。");
                return m_Exit;
            }

            presenter.SetVisible(true);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「隠す」ブロックに対応し、俳優の可視状態を Presenter 経由で無効化する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("Scratch/Looks/Hide")]
    [UnitCategory("FUnity/Scratch/Looks")]
    public sealed class HideActorUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>可視状態を切り替える対象俳優を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Actor;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>actor ポートへの参照。</summary>
        public ValueInput Actor => m_Actor;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と actor 入力を登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Exit = ControlOutput("exit");
            m_Actor = ValueInput<ActorPresenterAdapter>("actor", null);
            m_Enter = ControlInput("enter", OnEnter);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、Presenter を解決して俳優を非表示にする。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートを返し、後続へ制御を渡す。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var adapter = flow.GetValue<ActorPresenterAdapter>(m_Actor);
            ActorPresenter presenter = ScratchUnitUtil.ResolveActorPresenter(flow, adapter);
            if (presenter == null)
            {
                Debug.LogWarning(
                    "[FUnity] Scratch/Looks/Hide: ActorPresenter を解決できなかったため表示状態を変更できません。" +
                    "アダプタや VSPresenterBridge の設定を確認してください。");
                return m_Exit;
            }

            presenter.SetVisible(false);
            return m_Exit;
        }
    }
}
