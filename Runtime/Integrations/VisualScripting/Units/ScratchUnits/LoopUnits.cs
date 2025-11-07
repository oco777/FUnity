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
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 repeat loop 回数 繰り返す")]
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
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 forever loop ずっと")]
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
        /// ポート定義を行い、コルーチン入力と body 出力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);
            m_Body = ControlOutput("body");

            Succession(m_Enter, m_Body);
        }

        /// <summary>
        /// 停止条件が提供されるまで body ポートを無限に呼び出します。
        /// 各反復の後には 1 フレーム待機して描画更新へ制御を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>無限ループを行う列挙子。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            while (true)
            {
                yield return m_Body;
                yield return null;
            }
        }
    }
}
