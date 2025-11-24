using System.Collections;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「もし〇〇なら」ブロックを再現し、条件が真のときに内部フローを 1 回だけ実行する Unit です。
    /// </summary>
    [UnitTitle("もし○なら")]
    [UnitShortTitle("もし○なら")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class IfThenUnit : Unit
    {
        /// <summary>フロー入力を受け付ける ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>条件が真のときに発火させる ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Body;

        /// <summary>処理完了後に後続へ接続する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>評価する条件値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Condition;

        /// <summary>フロー入力ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>条件が真のときに実行されるポートを公開します。</summary>
        public ControlOutput Body => m_Body;

        /// <summary>終了後のフロー出力ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>条件値の入力ポートを公開します。</summary>
        public ValueInput Condition => m_Condition;

        /// <summary>
        /// ポート定義を行い、条件評価後に Body と Exit を順次呼び出すセットアップを行います。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Body = ControlOutput("body");
            m_Exit = ControlOutput("exit");
            m_Condition = ValueInput("condition", false);

            Succession(m_Enter, m_Body);
            Succession(m_Enter, m_Exit);
            Requirement(m_Condition, m_Enter);
        }

        /// <summary>
        /// フローが入ってきた際に条件を評価し、真であれば Body を 1 回実行します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>条件に応じて body/exit を順に返す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var condition = flow.GetValue<bool>(m_Condition);
            if (condition)
            {
                yield return m_Body;
            }

            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「もし〇なら でなければ」ブロックを再現し、条件が真偽のどちらでも後続を分岐させる Unit です。
    /// </summary>
    [UnitTitle("もし ○ なら でなければ")]
    [UnitShortTitle("もし〜なら/でなければ")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class IfElseUnit : Unit
    {
        /// <summary>制御フローの入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>条件が真のときに発火する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_IfTrue;

        /// <summary>条件が偽のときに発火する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_IfFalse;

        /// <summary>判定に使用する条件値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Condition;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>条件が真だった場合に進むポートへの参照を公開します。</summary>
        public ControlOutput IfTrue => m_IfTrue;

        /// <summary>条件が偽だった場合に進むポートへの参照を公開します。</summary>
        public ControlOutput IfFalse => m_IfFalse;

        /// <summary>条件入力ポートへの参照を公開します。</summary>
        public ValueInput Condition => m_Condition;

        /// <summary>
        /// ポート定義を行い、条件評価後に真偽それぞれの出力へ遷移できるよう設定します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_IfTrue = ControlOutput("ifTrue");
            m_IfFalse = ControlOutput("ifFalse");
            m_Condition = ValueInput("condition", false);

            Succession(m_Enter, m_IfTrue);
            Succession(m_Enter, m_IfFalse);
            Requirement(m_Condition, m_Enter);
        }

        /// <summary>
        /// 条件を評価し、真なら ifTrue、偽なら ifFalse へ制御を分岐させます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>分岐先の ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var condition = flow.GetValue<bool>(m_Condition);
            return condition ? m_IfTrue : m_IfFalse;
        }
    }
}
