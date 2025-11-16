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
        /// Scratch 用に登録されたすべてのスレッドを停止し、後続フローには進めません。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>常に null を返し、フローを終端させます。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var manager = FUnityScriptThreadManager.Instance;
            if (manager == null)
            {
                return null;
            }

            manager.StopAllScratchThreads();
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
        /// 現在のフローが紐づく Scratch スレッドを停止します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>常に null を返し、フローを終端させます。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            if (ScratchUnitUtil.TryGetThreadContext(flow, out string _, out string threadId) &&
                !string.IsNullOrEmpty(threadId))
            {
                var manager = FUnityScriptThreadManager.Instance;
                if (manager != null)
                {
                    manager.StopScratchThread(threadId);
                }
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
        /// 同一俳優に属する Scratch スレッドのうち、自身以外を停止します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>常に後続へ継続する ControlOutput を返します。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            if (ScratchUnitUtil.TryGetThreadContext(flow, out string actorId, out string threadId) &&
                !string.IsNullOrEmpty(actorId))
            {
                var manager = FUnityScriptThreadManager.Instance;
                if (manager != null)
                {
                    manager.StopOtherScratchThreadsOfActor(actorId, threadId);
                }
            }

            return m_Output;
        }
    }
}
