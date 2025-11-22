using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「すべてを止める」に対応し、全俳優の全スクリプトを停止するユニットです。
    /// </summary>
    [UnitTitle("すべてを止める")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
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
            var manager = FUnityScriptThreadManager.FindOrCreate();
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
    [UnitTitle("このスクリプトを止める")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
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
            if (ScratchUnitUtil.TryGetThreadContext(flow, out string actorId, out string threadId) &&
                !string.IsNullOrEmpty(actorId) && !string.IsNullOrEmpty(threadId))
            {
                var manager = FUnityScriptThreadManager.FindOrCreate();
                if (manager != null)
                {
                    manager.StopScratchThread(actorId, threadId);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Scratch の「スプライトの他のスクリプトを止める」に対応し、同一俳優の他スレッドを停止します。
    /// </summary>
    [UnitTitle("スプライトの他のスクリプトを止める")]
    [UnitCategory("FUnity/Blocks/制御")]
    [UnitSubtitle("制御")]
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
                var manager = FUnityScriptThreadManager.FindOrCreate();
                if (manager != null)
                {
                    manager.StopOtherScratchThreadsOfActor(actorId, threadId);
                }
            }

            return m_Output;
        }
    }
}
