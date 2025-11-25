using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「◯ 秒待つ」ブロックを再現し、指定秒数経過後に後続フローへ進めるユニットです。
    /// </summary>
    [UnitTitle("○秒待つ")]
    [UnitShortTitle("○秒待つ")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class WaitSecondsUnit : Unit
    {
        /// <summary>フロー入力を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>待機完了時に後続へ接続する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>待機秒数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>seconds ポートを公開します。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ControlInputCoroutine を使用し、待機処理後に exit へ遷移するポートを定義します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", RunCoroutine);
            m_Exit = ControlOutput("exit");
            m_Seconds = ValueInput<float>("seconds", 1f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Seconds, m_Enter);
        }

        /// <summary>
        /// 指定秒数だけ待機し、終了後に exit ポートを発火させます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>待機処理を行う列挙子を返します。</returns>
        private IEnumerator RunCoroutine(Flow flow)
        {
            var seconds = Mathf.Max(0f, flow.GetValue<float>(m_Seconds));

            //Debug.Log($"[FUnity.Wait] WaitSecondsUnit start seconds={seconds}");

            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }

            ////Debug.Log("[FUnity.Wait] WaitSecondsUnit exit");

            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇まで待つ」ブロックを再現し、条件が成立するまで毎フレーム待機するユニットです。
    /// </summary>
    [UnitTitle("○まで待つ")]
    [UnitShortTitle("○まで待つ")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class WaitUntilUnit : ScratchCoroutineUnitBase
    {
        /// <summary>待機を開始する ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>条件成立後に後続へ進む ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>待機判定に使用する条件値を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Condition;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>condition ポートへの参照を公開します。</summary>
        public ValueInput Condition => m_Condition;

        /// <summary>
        /// ポート定義を行い、条件成立までコルーチンで待機する入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Exit = ControlOutput("exit");
            m_Condition = ValueInput("condition", false);
            m_Enter = CreateScratchCoroutineInput("enter", RunCoroutine);

            Succession(m_Enter, m_Exit);
            Requirement(m_Condition, m_Enter);
        }

        /// <summary>
        /// 条件が true になるまで 1 フレームずつ待機し、成立後に exit へ遷移します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>待機処理を行う列挙子。</returns>
        private IEnumerator RunCoroutine(Flow flow)
        {
            while (!flow.GetValue<bool>(m_Condition))
            {
                yield return null;
            }

            yield return m_Exit;
        }
    }
}
