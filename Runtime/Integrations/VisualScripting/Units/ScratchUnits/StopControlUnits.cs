using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「すべてを止める」に対応し、全俳優の全スクリプトを停止するユニットです。
    /// </summary>
    [UnitTitle("Scratch/すべてを止める")]
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 stop all すべてを止める")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class StopAllUnit : Unit
    {
        /// <summary>入力フローを受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>
        /// フロー入力のみを受け付け、出力は作成しません。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("in", OnEnter);
        }

        /// <summary>
        /// すべてのスレッドを停止し、後続フローには進めません。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>常に null を返し、フローを終端させます。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            FUnityScriptThreadManager.Instance.StopAllThreads();
            return null;
        }
    }

    /// <summary>
    /// Scratch の「このスクリプトを止める」に対応し、実行中のスレッドのみ停止するユニットです。
    /// </summary>
    [UnitTitle("Scratch/このスクリプトを止める")]
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 stop this script このスクリプトを止める")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class StopThisScriptUnit : Unit
    {
        /// <summary>入力フローを受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>
        /// フロー入力のみを定義し、出力は作成しません。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("in", OnEnter);
        }

        /// <summary>
        /// 現在のフローが紐づくスレッドを停止します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>常に null を返し、フローを終端させます。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            if (ScratchUnitUtil.TryGetThreadContext(flow, out _, out var threadId))
            {
                FUnityScriptThreadManager.Instance.StopThread(threadId);
            }

            return null;
        }
    }

    /// <summary>
    /// Scratch の「スプライトの他のスクリプトを止める」に対応し、同一俳優の他スレッドを停止します。
    /// </summary>
    [UnitTitle("Scratch/スプライトの他のスクリプトを止める")]
    [UnitCategory("FUnity/Scratch/制御")]
    [UnitSubtitle("funity scratch 制御 stop other scripts 他のスクリプトを止める")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class StopOtherScriptsInSpriteUnit : Unit
    {
        /// <summary>入力フローを受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Input;

        /// <summary>停止後もフローを継続する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Output;

        /// <summary>
        /// 入出力ポートを定義します。
        /// </summary>
        protected override void Definition()
        {
            m_Input = ControlInput("in", OnEnter);
            m_Output = ControlOutput("out");
            Succession(m_Input, m_Output);
        }

        /// <summary>
        /// 同一俳優に属するスレッドのうち、自身以外を停止します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>常に後続へ継続する ControlOutput を返します。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            if (ScratchUnitUtil.TryGetThreadContext(flow, out var actorId, out var threadId))
            {
                FUnityScriptThreadManager.Instance.StopThreadsOfActorExcept(actorId, threadId);
            }

            return m_Output;
        }
    }
}
