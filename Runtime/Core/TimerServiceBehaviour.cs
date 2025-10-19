// Updated: 2025-03-03
using System;
using System.Collections;
using UnityEngine;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// コルーチンを利用して遅延実行を行う <see cref="ITimerService"/> の標準実装。
    /// FUnity UI GameObject に常駐させ、Visual Scripting からも利用できるタイマーを提供する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimerServiceBehaviour : MonoBehaviour, ITimerService
    {
        /// <summary>遅延実行を識別するためのデバッグ名。ログ出力時に利用する。</summary>
        [SerializeField]
        private string m_DebugName = "FUnityTimer";

        /// <summary>
        /// 指定時間後に 1 回だけコールバックを実行する。負値や 0 の場合は即時実行する。
        /// </summary>
        /// <param name="delaySeconds">待機する秒数。</param>
        /// <param name="callback">実行する処理。</param>
        public void Invoke(float delaySeconds, Action callback)
        {
            if (callback == null)
            {
                Debug.LogWarning("[FUnity] TimerService: callback が null のため Invoke を中止しました。");
                return;
            }

            if (delaySeconds <= 0f)
            {
                callback();
                return;
            }

            StartCoroutine(RunTimer(delaySeconds, callback));
        }

        /// <summary>
        /// 内部的に WaitForSeconds を利用してコールバックを遅延実行する。
        /// </summary>
        /// <param name="delaySeconds">待機時間。</param>
        /// <param name="callback">呼び出す処理。</param>
        private IEnumerator RunTimer(float delaySeconds, Action callback)
        {
            yield return new WaitForSeconds(delaySeconds);

            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FUnity] TimerService ({m_DebugName}) callback で例外が発生しました: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
