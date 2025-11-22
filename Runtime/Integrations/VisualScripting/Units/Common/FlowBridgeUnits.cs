using System.Collections;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.Common
{
    /// <summary>
    /// 同期フローを Visual Scripting のコルーチン実行へ橋渡しするユニットです。
    /// 同期チェーンのままコルーチン専用ポートへ接続すると例外になるため、事前にこのユニットを挟みます。
    /// </summary>
    [UnitTitle("コルーチンに切り替える")]
    [UnitShortTitle("コルーチン")]
    [UnitCategory("FUnity/Blocks/拡張")]
    [UnitSubtitle("拡張")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ToCoroutineUnit : Unit
    {
        /// <summary>同期フローを受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>コルーチンとして後続へ接続する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ControlInputCoroutine で enter ポートを登録し、同フレームで exit を返します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", RunCoroutine);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// 即座に exit ポートを返して後続フローをコルーチンパイプラインへ切り替えます。
        /// </summary>
        /// <param name="flow">現在処理中のフロー情報。</param>
        /// <returns>後続にバトンを渡す列挙子を返します。</returns>
        private IEnumerator RunCoroutine(Flow flow)
        {
            // 必要に応じて次フレームへ持ち越す場合は以下を有効化してください。
            // yield return null;
            yield return m_Exit;
        }
    }
}
