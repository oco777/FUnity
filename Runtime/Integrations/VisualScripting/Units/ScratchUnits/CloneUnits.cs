// Updated: 2025-10-21
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇〇のクローンを作る」に対応し、現在の俳優 Presenter を複製してランタイムに登録する Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により自動解決されます。
    /// </summary>
    [UnitTitle("自分のクローンを作る")]
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 clone create self クローン")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
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
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// アダプタから Presenter を取得し、FUnityManager にクローン生成を依頼します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>常に exit ポートへ遷移する列挙子を返します。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null || adapter.Presenter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Create Clone of Self: ActorPresenterAdapter を解決できないためクローンを生成できません。");
                yield return m_Exit;
                yield break;
            }

            FUnityManager.CloneActor(adapter.Presenter);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「(名前)のクローンを作る」に対応し、DisplayName で指定した俳優 Presenter を複製する Unit です。
    /// 実行中のグラフから <see cref="ActorPresenterAdapter"/> を解決し、<see cref="VSPresenterBridge"/> 経由でクローン生成を依頼します。
    /// </summary>
    [UnitTitle("○のクローンを作る")]
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 clone create 指定 クローン display name")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class CreateCloneOfDisplayNameUnit : Unit
    {
        /// <summary>制御フローの入口。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出口。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>複製元を識別する DisplayName 入力。</summary>
        [DoNotSerialize]
        private ValueInput m_TargetDisplayName;

        /// <summary>生成したクローンのアダプタ出力。失敗時は null。</summary>
        [DoNotSerialize]
        private ValueOutput m_CloneAdapter;

        /// <summary>enter→exit の制御線および入出力ポートを定義します。</summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");

            m_TargetDisplayName = ValueInput<string>("TargetDisplayName", string.Empty);
            m_CloneAdapter = ValueOutput<ActorPresenterAdapter>("CloneAdapter", flow => null);

            Succession(m_Enter, m_Exit);
            Requirement(m_TargetDisplayName, m_Enter);
        }

        /// <summary>
        /// 実行中の Adapter と DisplayName を基にクローン生成を依頼します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>常に exit ポートへ遷移する列挙子を返します。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Create Clone Of (DisplayName): ActorPresenterAdapter を解決できないためクローンを生成できません。");
                flow.SetValue(m_CloneAdapter, null);
                yield return m_Exit;
                yield break;
            }

            var bridge = VSPresenterBridge.Instance;
            if (bridge == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Create Clone Of (DisplayName): VSPresenterBridge が未初期化のためクローン生成を依頼できません。");
                flow.SetValue(m_CloneAdapter, null);
                yield return m_Exit;
                yield break;
            }

            var displayName = flow.GetValue<string>(m_TargetDisplayName);
            var cloneAdapter = bridge.RequestCloneByDisplayName(adapter, displayName);
            flow.SetValue(m_CloneAdapter, cloneAdapter);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「クローンされたとき」イベントを表現し、新しく生成されたクローンごとにトリガーを発火します。
    /// </summary>
    [UnitTitle("クローンされたとき")]
    [UnitCategory("Events/FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 clone event when クローン")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class WhenIStartAsCloneUnit : EventUnit<CloneEventArgs>
    {
        /// <summary>GraphReference ごとに登録したクローン開始イベントのハンドラ管理。</summary>
        private static readonly Dictionary<GraphReference, Action<CloneEventArgs>> s_Handlers
            = new Dictionary<GraphReference, Action<CloneEventArgs>>();

        /// <summary>EventBus に登録するかどうか。</summary>
        protected override bool register => true;

        /// <summary>Scratch 用のリスナー状態を生成します。</summary>
        /// <returns>EventUnit 既定のデータ構造。</returns>
        public override IGraphElementData CreateData()
        {
            return new Data();
        }

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

        /// <summary>
        /// クローン開始イベントを EventBus に登録し、Scratch スレッドを経由してフローを開始します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StartListening(GraphStack stack)
        {
            if (stack == null)
            {
                return;
            }

            var reference = stack.ToReference();

            if (s_Handlers.ContainsKey(reference))
            {
                return;
            }

            var hook = GetHook(reference);
            Action<CloneEventArgs> handler = args => TriggerWithThreadRegistration(reference, args);

            if (hook.name != null && handler != null)
            {
                EventBus.Register<CloneEventArgs>(hook, handler);
                s_Handlers[reference] = handler;
            }
        }

        /// <summary>
        /// EventBus からクローン開始イベントの購読を解除します。
        /// </summary>
        /// <param name="stack">現在のグラフスタック。</param>
        public override void StopListening(GraphStack stack)
        {
            if (stack == null)
            {
                return;
            }

            var reference = stack.ToReference();

            if (!s_Handlers.TryGetValue(reference, out var handler) || handler == null)
            {
                return;
            }

            var hook = GetHook(reference);
            if (hook.name != null)
            {
                EventBus.Unregister(hook, handler);
            }

            s_Handlers.Remove(reference);
        }

        /// <summary>
        /// クローン開始イベントでフローを発火し、開始したコルーチンを Scratch スレッドとして登録します。
        /// </summary>
        /// <param name="reference">現在のグラフ参照。</param>
        /// <param name="args">クローンイベント引数。</param>
        private void TriggerWithThreadRegistration(GraphReference reference, CloneEventArgs args)
        {
            var flow = Flow.New(reference);
            var routine = RunEventCoroutine(flow, args);
            ScratchUnitUtil.StartScratchCoroutine(flow, routine);
        }

        /// <summary>
        /// EventUnit 標準のフロー実行をコルーチンでラップし、Flow のライフサイクルは Visual Scripting 側に委ねます。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <param name="args">クローンイベント引数。</param>
        /// <returns>実行完了までの列挙子。</returns>
        private IEnumerator RunEventCoroutine(Flow flow, CloneEventArgs args)
        {
            if (!ShouldTrigger(flow, args))
            {
                yield break;
            }

            AssignArguments(flow, args);

            flow.StartCoroutine(trigger);
            yield break;
        }
    }

    /// <summary>
    /// Scratch の「このクローンを削除する」に対応し、現在の Presenter がクローンであれば安全に破棄します。
    /// </summary>
    [UnitTitle("このクローンを削除する")]
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 clone delete remove クローン")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
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
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// クローンであれば FUnityManager に削除を依頼し、本体であれば警告のみを表示します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>常に exit ポートへ遷移する列挙子を返します。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            var presenter = adapter != null ? adapter.Presenter : null;
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Delete This Clone: ActorPresenter を解決できないため削除できません。");
                yield return m_Exit;
                yield break;
            }

            if (!presenter.IsClone)
            {
                Debug.LogWarning("[FUnity] Scratch/Delete This Clone: 本体は削除対象外のため処理をスキップします。");
                yield return m_Exit;
                yield break;
            }

            FUnityManager.DeleteActorClone(presenter);
            yield return m_Exit;
        }
    }
}
