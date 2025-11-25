// Updated: 2025-11-12
using System;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.Blocks
{
    /// <summary>
    /// 質問用の入力フォームを表示し、回答が確定するまでフローを停止する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("〇と聞いて待つ")]
    [UnitShortTitle("聞いて待つ")]
    [UnitCategory("FUnity/Blocks/調べる")]
    [UnitSubtitle("調べる")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class AskAndWaitUnit : Unit
    {
        /// <summary>制御フローの入力ポート。</summary>
        [DoNotSerialize]
        private ControlInput m_TriggerIn;

        /// <summary>制御フローの出力ポート。</summary>
        [DoNotSerialize]
        private ControlOutput m_TriggerOut;

        /// <summary>質問文を受け取る入力ポート。</summary>
        [DoNotSerialize]
        private ValueInput m_QuestionInput;

        /// <summary>入力ポートの参照。</summary>
        public ControlInput TriggerIn => m_TriggerIn;

        /// <summary>出力ポートの参照。</summary>
        public ControlOutput TriggerOut => m_TriggerOut;

        /// <summary>質問文入力ポートの参照。</summary>
        public ValueInput QuestionInput => m_QuestionInput;

        /// <summary>
        /// ポート定義を行い、質問入力とフロー入出力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_QuestionInput = ValueInput<string>("question", string.Empty);
            m_TriggerOut = ControlOutput("trigger");
            m_TriggerIn = ControlInputCoroutine("trigger", OnTrigger);

            Succession(m_TriggerIn, m_TriggerOut);
        }

        /// <summary>
        /// フォームを表示して回答完了まで待機し、終了後に後続フローへ進みます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>回答完了後にフローを再開する列挙子。</returns>
        private IEnumerator OnTrigger(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            var stageRoot = adapter?.Presenter != null ? adapter.Presenter.StageRootElement : null;
            if (stageRoot == null)
            {
                Debug.LogWarning("[FUnity] AskAndWaitUnit: UI を配置するルートが見つからないため、質問を表示できません。");
                yield return m_TriggerOut;
                yield break;
            }

            var question = flow.GetValue<string>(m_QuestionInput) ?? string.Empty;
            AnswerStore.LastAnswer = string.Empty;
            var isAnswered = false;

            AskPromptService.Show(stageRoot, question, answer =>
            {
                AnswerStore.LastAnswer = answer ?? string.Empty;
                isAnswered = true;
            });

            while (!isAnswered)
            {
                yield return null;
            }

            yield return m_TriggerOut;
        }
    }

    /// <summary>
    /// 直近の回答文字列を取得する Value Unit です。
    /// </summary>
    [UnitTitle("答え")]
    [UnitShortTitle("答え")]
    [UnitCategory("FUnity/Blocks/調べる")]
    [UnitSubtitle("調べる")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class AnswerUnit : Unit
    {
        /// <summary>回答文字列を出力するポート。</summary>
        [DoNotSerialize]
        private ValueOutput m_AnswerOutput;

        /// <summary>回答を数値として出力するポート（float）。</summary>
        [DoNotSerialize]
        [PortLabel("答え(float)")]
        private ValueOutput m_AnswerFloatOutput;

        /// <summary>回答を整数として出力するポート（int）。</summary>
        [DoNotSerialize]
        [PortLabel("答え(int)")]
        private ValueOutput m_AnswerIntOutput;

        /// <summary>出力ポートの参照。</summary>
        public ValueOutput AnswerOutput => m_AnswerOutput;

        /// <summary>float 変換済み回答ポートの参照。</summary>
        public ValueOutput AnswerFloatOutput => m_AnswerFloatOutput;

        /// <summary>int 変換済み回答ポートの参照。</summary>
        public ValueOutput AnswerIntOutput => m_AnswerIntOutput;

        /// <summary>
        /// ポート定義を行い、回答文字列を出力します。
        /// </summary>
        protected override void Definition()
        {
            m_AnswerOutput = ValueOutput<string>("answer", GetAnswerString);
            m_AnswerFloatOutput = ValueOutput<float>("answerFloat", GetAnswerFloat);
            m_AnswerIntOutput = ValueOutput<int>("answerInt", GetAnswerInt);
        }

        /// <summary>
        /// 回答文字列を返します。未回答の場合は空文字列を返却します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>直近の回答文字列。</returns>
        private string GetAnswerString(Flow flow)
        {
            return AnswerStore.LastAnswer ?? string.Empty;
        }

        /// <summary>
        /// 回答文字列を float に変換して返します。失敗時は 0f を返却します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>数値変換した回答。</returns>
        private float GetAnswerFloat(Flow flow)
        {
            var text = GetAnswerString(flow) ?? string.Empty;
            if (float.TryParse(text, out var value))
            {
                return value;
            }

            return 0f;
        }

        /// <summary>
        /// 回答文字列を四捨五入した整数として返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>四捨五入後の整数値。</returns>
        private int GetAnswerInt(Flow flow)
        {
            var floatValue = GetAnswerFloat(flow);
            return Mathf.RoundToInt(floatValue);
        }
    }
}
