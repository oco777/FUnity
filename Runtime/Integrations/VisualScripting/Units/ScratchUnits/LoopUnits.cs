// Updated: 2025-10-19
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇回繰り返す」ブロックを再現し、指定回数だけ本体ポートを実行するカスタム Unit です。
    /// </summary>
    [UnitTitle("○回繰り返す")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class RepeatNUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>ループ終了後に制御を返す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>本体処理へ接続する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Body;

        /// <summary>繰り返し回数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Count;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>body ポートへの参照を公開します。</summary>
        public ControlOutput Body => m_Body;

        /// <summary>count ポートへの参照を公開します。</summary>
        public ValueInput Count => m_Count;

        /// <summary>
        /// ポート定義を行い、コルーチン入力と body/exit を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);
            m_Exit = ControlOutput("exit");
            m_Body = ControlOutput("body");
            m_Count = ValueInput<int>("count", 10);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 指定回数だけ body ポートを順次実行し、最後に exit へ制御を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>ループ処理を行う列挙子。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            var iterations = Mathf.Max(0, flow.GetValue<int>(m_Count));
            for (var i = 0; i < iterations; i++)
            {
                yield return m_Body;
            }

            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「ずっと」ブロックを再現し、常に本体ポートを繰り返すカスタム Unit です。
    /// </summary>
    [UnitTitle("ずっと")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ForeverUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>ループ継続中に繰り返し呼び出す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Body;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>body ポートへの参照を公開します。</summary>
        public ControlOutput Body => m_Body;

        /// <summary>
        /// ポート定義を行い、Visual Scripting 標準のコルーチン入力と body 出力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);
            m_Body = ControlOutput("body");

            Succession(m_Enter, m_Body);
        }

        /// <summary>
        /// 「ずっと」ブロックの本体で、Flow コルーチンとして body を毎フレーム実行します。
        /// 各反復の後には 1 フレーム待機して描画更新へ制御を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>無限ループを行う列挙子。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            while (true)
            {
                // body に接続されたノード群を実行し、後続ユニットへ制御を渡します。
                yield return m_Body;

                // 1 フレーム待機してから次のループを開始し、Scratch の「ずっと」相当の周期を再現します。
                yield return null;
            }
        }
    }

    /// <summary>
    /// Scratch の「〇まで繰り返す」ブロックを再現し、条件が成立するまで本体を反復実行するカスタム Unit です。
    /// </summary>
    [UnitTitle("○まで繰り返す")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class RepeatUntilUnit : ScratchCoroutineUnitBase
    {
        /// <summary>ループ開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>各反復で実行する本体ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Body;

        /// <summary>ループ終了後に後続へ進む ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>終了条件を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Condition;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>body ポートへの参照を公開します。</summary>
        public ControlOutput Body => m_Body;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>condition ポートへの参照を公開します。</summary>
        public ValueInput Condition => m_Condition;

        /// <summary>
        /// ポート定義を行い、条件成立までコルーチンで body を繰り返す設定を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Exit = ControlOutput("exit");
            m_Body = ControlOutput("body");
            m_Condition = ValueInput("condition", false);
            m_Enter = CreateScratchCoroutineInput("enter", RunCoroutine);

            Succession(m_Enter, m_Body);
            Succession(m_Body, m_Body);
            Succession(m_Enter, m_Exit);
            Requirement(m_Condition, m_Enter);
        }

        /// <summary>
        /// 条件が真になるまで body を実行し、毎反復の終わりに 1 フレーム待機します。
        /// 条件成立後は exit ポートへ制御を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>ループ処理を行う列挙子。</returns>
        private IEnumerator RunCoroutine(Flow flow)
        {
            while (!flow.GetValue<bool>(m_Condition))
            {
                yield return m_Body;
                yield return null;
            }

            yield return m_Exit;
        }
    }
}
