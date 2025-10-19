// Updated: 2025-03-03
using System;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// 指定秒数後にコールバックを実行するためのサービス契約。
    /// Presenter や Visual Scripting ブリッジから利用し、コルーチン実装の差異を隠蔽する。
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// 指定時間経過後にコールバックを 1 度だけ呼び出す。
        /// </summary>
        /// <param name="delaySeconds">待機する時間（秒）。0 以下の場合は即時実行。</param>
        /// <param name="callback">実行する処理。null の場合は安全に無視される。</param>
        void Invoke(float delaySeconds, Action callback);
    }
}
