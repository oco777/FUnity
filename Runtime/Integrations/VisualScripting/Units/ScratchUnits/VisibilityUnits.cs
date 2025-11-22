// Updated: 2025-03-07
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「表示する」ブロックに対応し、俳優の可視状態を Presenter 経由で有効化する Visual Scripting Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> を用いて自動的に解決します。
    /// </summary>
    [UnitTitle("表示する")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("funity scratch 見た目 show visible 表示")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ShowActorUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線のみを登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、Presenter を自動解決して俳優を表示状態にする。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning(
                    "[FUnity] Scratch/Looks/Show: ActorPresenterAdapter を自動解決できません。ScriptMachine の Variables 設定を確認してください。");
                yield return m_Exit;
                yield break;
            }

            var presenter = adapter.Presenter;
            if (presenter == null)
            {
                Debug.LogWarning(
                    "[FUnity] Scratch/Looks/Show: ActorPresenter が未接続のため表示状態を変更できません。Adapter と Presenter の紐付けを確認してください。");
                yield return m_Exit;
                yield break;
            }

            presenter.SetVisible(true);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「隠す」ブロックに対応し、俳優の可視状態を Presenter 経由で無効化する Visual Scripting Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> を用いて自動的に解決します。
    /// </summary>
    [UnitTitle("隠す")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("funity scratch 見た目 hide invisible 隠す")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class HideActorUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線のみを登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、Presenter を自動解決して俳優を非表示にする。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning(
                    "[FUnity] Scratch/Looks/Hide: ActorPresenterAdapter を自動解決できません。ScriptMachine の Variables 設定を確認してください。");
                yield return m_Exit;
                yield break;
            }

            var presenter = adapter.Presenter;
            if (presenter == null)
            {
                Debug.LogWarning(
                    "[FUnity] Scratch/Looks/Hide: ActorPresenter が未接続のため表示状態を変更できません。Adapter と Presenter の紐付けを確認してください。");
                yield return m_Exit;
                yield break;
            }

            presenter.SetVisible(false);
            yield return m_Exit;
        }
    }
}
