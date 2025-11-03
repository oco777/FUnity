using System.Collections;
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.Common
{
    /// <summary>
    /// 従来の同期処理を <see cref="ExecuteSync"/> に集約し、最後に exit ポートをコルーチンで返すための共通基底クラスです。
    /// Visual Scripting のコルーチン専用ポートへ安全に接続することを目的としており、待機を挟まず同フレーム内で後続へ制御を渡します。
    /// </summary>
    public abstract class AutoCoroutineUnit : Unit
    {
        /// <summary>コルーチンの入り口となる ControlInput です。</summary>
        protected ControlInput m_In;

        /// <summary>処理完了後に制御を渡す ControlOutput です。</summary>
        protected ControlOutput m_Out;

        /// <summary>
        /// enter / exit ポートを定義し、サブクラスの同期処理を実行後にコルーチンとして exit を返します。
        /// </summary>
        protected override void Definition()
        {
            DefinePorts();
        }

        /// <summary>
        /// enter / exit ポートの定義を行います。サブクラスが追加ポートを宣言する際は base 呼び出し後に処理してください。
        /// </summary>
        protected virtual void DefinePorts()
        {
            m_In = ControlInputCoroutine("enter", Run);
            m_Out = ControlOutput("exit");
            Succession(m_In, m_Out);
        }

        /// <summary>
        /// 既存の同期ロジックを実装するための抽象メソッドです。ここで ValueInput から値を取得し、必要な副作用を実行します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        protected abstract void ExecuteSync(Flow flow);

        /// <summary>
        /// <see cref="ExecuteSync"/> の実行後に exit ポートを返すコルーチン実装です。待機は行わず同フレーム内で後続へ進みます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            ExecuteSync(flow);
            yield return m_Out;
        }
    }
}
