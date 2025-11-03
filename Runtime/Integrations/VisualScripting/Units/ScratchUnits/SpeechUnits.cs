// Updated: 2025-10-21
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇と〇秒言う」に対応し、指定秒数だけ発言吹き出しを表示する Visual Scripting Unit です。
    /// ActorPresenterAdapter を自動解決し、Presenter 経由で吹き出しを制御します。
    /// </summary>
    [UnitTitle("Scratch/Say For Seconds")]
    [UnitCategory("FUnity/Scratch/Looks")]
    public sealed class SayForSecondsUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>表示する本文を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Text;

        /// <summary>表示継続秒数を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>text ポートへの参照。</summary>
        public ValueInput Text => m_Text;

        /// <summary>seconds ポートへの参照。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と text / seconds の入力を登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Exit = ControlOutput("exit");
            m_Text = ValueInput<string>("text", string.Empty);
            m_Seconds = ValueInput<float>("seconds", 2f);
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、指定秒数だけ吹き出しを表示した後に非表示へ戻すコルーチンを実行する。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>処理完了後に exit ポートへ遷移する列挙子。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Say For Seconds: ActorPresenterAdapter が見つからないため吹き出しを表示できません。VSPresenterBridge などでアダプタを登録してください。");
                yield break;
            }

            var text = flow.GetValue<string>(m_Text) ?? string.Empty;
            var seconds = Mathf.Max(0f, flow.GetValue<float>(m_Seconds));

            adapter.ShowSpeech(text, seconds, false);

            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }

            adapter.HideSpeech();
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇と言う」に対応し、無期限の発言吹き出しを表示する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("Scratch/Say")]
    [UnitCategory("FUnity/Scratch/Looks")]
    public sealed class SayUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>表示する本文を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Text;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>text ポートへの参照。</summary>
        public ValueInput Text => m_Text;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と text 入力を登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Text = ValueInput<string>("text", string.Empty);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、ActorPresenterAdapter を介して吹き出し表示を実行する。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Say: ActorPresenterAdapter が見つからないため吹き出しを表示できません。VSPresenterBridge などでアダプタを登録してください。");
                yield return m_Exit;
                yield break;
            }

            var text = flow.GetValue<string>(m_Text) ?? string.Empty;
            adapter.ShowSpeech(text, 0f, false);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇と〇秒考える」に対応し、指定秒数だけ思考吹き出しを表示する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("Scratch/Think For Seconds")]
    [UnitCategory("FUnity/Scratch/Looks")]
    public sealed class ThinkForSecondsUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>表示する本文を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Text;

        /// <summary>表示継続秒数を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>text ポートへの参照。</summary>
        public ValueInput Text => m_Text;

        /// <summary>seconds ポートへの参照。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と text / seconds を登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Exit = ControlOutput("exit");
            m_Text = ValueInput<string>("text", string.Empty);
            m_Seconds = ValueInput<float>("seconds", 2f);
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、指定秒数だけ思考吹き出しを表示してから非表示に戻すコルーチンを実行する。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>処理完了後に exit ポートへ遷移する列挙子。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Think For Seconds: ActorPresenterAdapter が見つからないため吹き出しを表示できません。VSPresenterBridge などでアダプタを登録してください。");
                yield break;
            }

            var text = flow.GetValue<string>(m_Text) ?? string.Empty;
            var seconds = Mathf.Max(0f, flow.GetValue<float>(m_Seconds));

            adapter.ShowSpeech(text, seconds, true);

            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }

            adapter.HideSpeech();
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇と考える」に対応し、無期限の思考吹き出しを表示する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("Scratch/Think")]
    [UnitCategory("FUnity/Scratch/Looks")]
    public sealed class ThinkUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>表示する本文を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_Text;

        /// <summary>enter ポートへの参照。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>text ポートへの参照。</summary>
        public ValueInput Text => m_Text;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と text 入力を登録する。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Text = ValueInput<string>("text", string.Empty);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 入力フローを受け取り、ActorPresenterAdapter を介して思考吹き出しを表示する。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Think: ActorPresenterAdapter が見つからないため吹き出しを表示できません。VSPresenterBridge などでアダプタを登録してください。");
                yield return m_Exit;
                yield break;
            }

            var text = flow.GetValue<string>(m_Text) ?? string.Empty;
            adapter.ShowSpeech(text, 0f, true);
            yield return m_Exit;
        }
    }
}
