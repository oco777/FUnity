// Updated: 2025-10-19
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「〇秒待つ」ブロックを再現し、一定時間後に制御を返すカスタム Unit です。
    /// </summary>
    [UnitTitle("Scratch/Wait Seconds")]
    [UnitCategory("FUnity/Scratch/Control")]
    public sealed class WaitSecondsUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>一定時間経過後に制御を返す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>待機秒数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>seconds ポートへの参照を公開します。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ポート定義を行い、コルーチン入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", OnEnterCoroutine);
            m_Exit = ControlOutput("exit");
            m_Seconds = ValueInput<float>("seconds", 1f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 指定秒数だけ待機した後に exit へ制御を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>待機処理を行う列挙子を返します。</returns>
        private IEnumerator OnEnterCoroutine(Flow flow)
        {
            var seconds = Mathf.Max(0f, flow.GetValue<float>(m_Seconds));
            if (seconds <= 0f)
            {
                yield return m_Exit;
                yield break;
            }

#if UNITY_EDITOR
            var start = EditorApplication.timeSinceStartup;
            while (EditorApplication.timeSinceStartup - start < seconds)
            {
                yield return null;
            }
#else
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
#endif

            yield return m_Exit;
        }
    }
}
