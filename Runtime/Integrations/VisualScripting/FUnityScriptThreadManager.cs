using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting
{
    /// <summary>
    /// Visual Scripting の ScriptGraph をスレッド（コルーチン）単位で追跡し、停止制御を提供するマネージャです。
    /// </summary>
    [AddComponentMenu("")]
    public sealed class FUnityScriptThreadManager : MonoBehaviour
    {
        /// <summary>シングルトンインスタンスを保持する静的フィールドです。</summary>
        private static FUnityScriptThreadManager s_Instance;

        /// <summary>スレッドに付随する情報を保持するテーブルです。</summary>
        private readonly Dictionary<Guid, ScriptThreadInfo> m_Threads = new Dictionary<Guid, ScriptThreadInfo>();

        /// <summary>Scratch 用スレッドを管理するテーブルです。俳優 ID とスレッド ID の組み合わせで一意に追跡します。</summary>
        private readonly Dictionary<(string actorId, string threadId), ScratchThreadInfo> m_ScratchThreads
            = new Dictionary<(string actorId, string threadId), ScratchThreadInfo>();

        /// <summary>
        /// シングルトンインスタンスを検索または生成し、呼び出し元へ返します。
        /// EventUnit やユーティリティから参照される想定です。
        /// </summary>
        /// <returns>シーン上で有効な <see cref="FUnityScriptThreadManager"/>。</returns>
        public static FUnityScriptThreadManager FindOrCreate()
        {
            return Instance;
        }

        /// <summary>
        /// シングルトンインスタンスを参照します。見つからない場合はシーンから検索し、存在しなければ自動生成します。
        /// </summary>
        public static FUnityScriptThreadManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindObjectOfType<FUnityScriptThreadManager>();

                    if (s_Instance == null)
                    {
                        var go = new GameObject("FUnityScriptThreadManager");
                        s_Instance = go.AddComponent<FUnityScriptThreadManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return s_Instance;
            }
            private set
            {
                s_Instance = value;
            }
        }

        /// <summary>
        /// Visual Scripting のスレッド情報を表すデータクラスです。
        /// </summary>
        [Serializable]
        public sealed class ScriptThreadInfo
        {
            /// <summary>属している俳優（Runner）の識別子です。</summary>
            public string ActorId;

            /// <summary>スレッド固有の ID です。</summary>
            public Guid ThreadId;

            /// <summary>実行中のコルーチン参照です。</summary>
            public Coroutine Coroutine;

            /// <summary>実行している ScriptGraph アセットです。</summary>
            public ScriptGraphAsset Graph;
        }

        /// <summary>
        /// Scratch イベント由来のスレッド情報を表すデータクラスです。
        /// Unity の Coroutine ではなく、Visual Scripting の Flow を直接追跡します。
        /// </summary>
        [Serializable]
        private sealed class ScratchThreadInfo
        {
            /// <summary>スレッド固有の ID です（Guid 文字列）。</summary>
            public string ThreadId;

            /// <summary>属している俳優（Runner）の識別子です。</summary>
            public string ActorId;

            /// <summary>実行している ScriptGraph アセットです。</summary>
            public ScriptGraphAsset Graph;

            /// <summary>実行中の Flow 参照です。</summary>
            public Flow Flow;
        }

        /// <summary>
        /// シーンに配置されたインスタンスを優先的に登録し、重複があれば破棄します。
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[FUnity] FUnityScriptThreadManager already exists. Destroying duplicate.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (gameObject.scene.rootCount > 0)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// 指定したスレッドを登録し、停止管理の対象にします。
        /// </summary>
        /// <param name="actorId">スレッドが属する俳優の識別子。</param>
        /// <param name="coroutine">実行中のコルーチン。</param>
        /// <param name="graph">対応する ScriptGraph アセット。</param>
        /// <returns>登録したスレッド情報。</returns>
        public ScriptThreadInfo RegisterThread(string actorId, Coroutine coroutine, ScriptGraphAsset graph)
        {
            var info = new ScriptThreadInfo
            {
                ActorId = string.IsNullOrEmpty(actorId) ? "(Unknown Actor)" : actorId,
                ThreadId = Guid.NewGuid(),
                Coroutine = coroutine,
                Graph = graph
            };

            m_Threads[info.ThreadId] = info;
            return info;
        }

        /// <summary>
        /// 指定した ID のスレッド登録を解除します。
        /// </summary>
        /// <param name="threadId">解除するスレッド ID。</param>
        public void UnregisterThread(Guid threadId)
        {
            m_Threads.Remove(threadId);
        }

        /// <summary>
        /// 登録済みのすべてのスレッドを停止し、テーブルを初期化します。
        /// </summary>
        public void StopAllThreads()
        {
            foreach (var entry in m_Threads.ToList())
            {
                var info = entry.Value;
                if (info?.Coroutine != null)
                {
                    StopCoroutine(info.Coroutine);
                }
            }

            m_Threads.Clear();
        }

        /// <summary>
        /// 指定した ID のスレッドを停止します。
        /// </summary>
        /// <param name="threadId">停止対象のスレッド ID。</param>
        public void StopThread(Guid threadId)
        {
            if (!m_Threads.TryGetValue(threadId, out var info))
            {
                return;
            }

            if (info.Coroutine != null)
            {
                StopCoroutine(info.Coroutine);
            }

            m_Threads.Remove(threadId);
        }

        /// <summary>
        /// 既存スレッドにコルーチン参照を紐付けます。RegisterThread 後に StartCoroutine で取得した参照を設定する際に使用します。
        /// </summary>
        /// <param name="threadId">更新するスレッド ID。</param>
        /// <param name="coroutine">紐付けるコルーチン。</param>
        public void UpdateCoroutine(Guid threadId, Coroutine coroutine)
        {
            if (!m_Threads.TryGetValue(threadId, out var info))
            {
                return;
            }

            info.Coroutine = coroutine;
        }

        /// <summary>
        /// 同一俳優に属するスレッドのうち、指定した ID 以外を停止します。
        /// </summary>
        /// <param name="actorId">対象俳優の識別子。</param>
        /// <param name="exceptThreadId">停止から除外するスレッド ID。</param>
        public void StopThreadsOfActorExcept(string actorId, Guid exceptThreadId)
        {
            var normalized = string.IsNullOrEmpty(actorId) ? string.Empty : actorId;
            foreach (var entry in m_Threads.ToList())
            {
                var info = entry.Value;
                if (info == null)
                {
                    continue;
                }

                if (!string.Equals(info.ActorId, normalized, StringComparison.Ordinal))
                {
                    continue;
                }

                if (info.ThreadId == exceptThreadId)
                {
                    continue;
                }

                if (info.Coroutine != null)
                {
                    StopCoroutine(info.Coroutine);
                }

                m_Threads.Remove(info.ThreadId);
            }
        }

        /// <summary>
        /// Scratch のイベント Unit から開始されたスレッドを登録します。
        /// </summary>
        /// <param name="actorId">スレッドを所有する俳優の識別子。</param>
        /// <param name="graph">実行している ScriptGraph アセット。</param>
        /// <param name="flow">実行中の Flow。</param>
        /// <returns>登録に成功した場合は生成したスレッド ID。Flow が null の場合は null。</returns>
        public void RegisterScratchThread(string actorId, ScriptGraphAsset graph, Flow flow, string threadId)
        {
            if (flow == null)
            {
                Debug.LogWarning("[FUnity.Thread] RegisterScratchThread: flow is null.");
                return;
            }

            if (string.IsNullOrEmpty(actorId))
            {
                Debug.LogWarning("[FUnity.Thread] RegisterScratchThread: actorId is null or empty.");
                return;
            }

            if (string.IsNullOrEmpty(threadId))
            {
                Debug.LogWarning("[FUnity.Thread] RegisterScratchThread: threadId is null or empty.");
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"[FUnity.Thread] Register actor={actorId}, thread={threadId}");
#endif

            var info = new ScratchThreadInfo
            {
                ThreadId = threadId,
                ActorId = actorId ?? string.Empty,
                Graph = graph,
                Flow = flow,
            };

            var key = (info.ActorId, info.ThreadId);
            m_ScratchThreads[key] = info;
        }

        /// <summary>
        /// Scratch 用に登録されたスレッドを登録解除します。
        /// </summary>
        /// <param name="threadId">解除するスレッド ID。</param>
        public void UnregisterScratchThread(string threadId)
        {
            if (string.IsNullOrEmpty(threadId))
            {
                return;
            }

            var targetKey = m_ScratchThreads.Keys
                .FirstOrDefault(key => string.Equals(key.threadId, threadId, StringComparison.Ordinal));

            if (!string.IsNullOrEmpty(targetKey.threadId))
            {
                m_ScratchThreads.Remove(targetKey);
            }
        }

        /// <summary>
        /// すべての Scratch スレッドを停止します。
        /// </summary>
        public void StopAllScratchThreads()
        {
            if (m_ScratchThreads.Count == 0)
            {
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"[FUnity.Thread] StopAll requested count={m_ScratchThreads.Count}");
#endif

            foreach (var info in m_ScratchThreads.Values)
            {
                TryStopFlow(info?.Flow);
            }

            m_ScratchThreads.Clear();
        }

        /// <summary>
        /// 指定した Scratch スレッドだけを停止します。
        /// </summary>
        /// <param name="actorId">停止対象の俳優 ID。</param>
        /// <param name="threadId">停止対象のスレッド ID。</param>
        public void StopScratchThread(string actorId, string threadId)
        {
            if (string.IsNullOrEmpty(actorId) || string.IsNullOrEmpty(threadId))
            {
                return;
            }

            var key = (actorId, threadId);
            if (!m_ScratchThreads.TryGetValue(key, out var info))
            {
                return;
            }

            TryStopFlow(info?.Flow);
            m_ScratchThreads.Remove(key);
        }

        /// <summary>
        /// 指定俳優に属する Scratch スレッドのうち、指定したスレッド以外を停止します。
        /// </summary>
        /// <param name="actorId">対象となる俳優の識別子。</param>
        /// <param name="exceptThreadId">停止対象から除外するスレッド ID。</param>
        public void StopOtherScratchThreadsOfActor(string actorId, string exceptThreadId)
        {
            if (string.IsNullOrEmpty(actorId))
            {
                return;
            }

            var keysToStop = new List<(string actorId, string threadId)>();

            foreach (var entry in m_ScratchThreads)
            {
                var key = entry.Key;
                var info = entry.Value;
                if (info == null)
                {
                    continue;
                }

                if (!string.Equals(info.ActorId, actorId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(exceptThreadId) && string.Equals(info.ThreadId, exceptThreadId, StringComparison.Ordinal))
                {
                    continue;
                }

                keysToStop.Add(key);
            }

            foreach (var key in keysToStop)
            {
                if (m_ScratchThreads.TryGetValue(key, out var info))
                {
                    TryStopFlow(info?.Flow);
                    m_ScratchThreads.Remove(key);
                }
            }
        }

        /// <summary>
        /// Flow 停止処理を共通化します。例外は警告ログに変換します。
        /// </summary>
        /// <param name="flow">停止対象のフロー。</param>
        private static void TryStopFlow(Flow flow)
        {
            if (flow == null)
            {
                return;
            }

            try
            {
                flow.StopCoroutine(false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FUnity] ScratchThreadManager: Failed to stop flow: {ex}");
            }
        }

        /// <summary>
        /// 指定したスレッド ID の情報を取得します。
        /// </summary>
        /// <param name="threadId">検索するスレッド ID。</param>
        /// <param name="info">見つかったスレッド情報。</param>
        /// <returns>見つかった場合は true。</returns>
        public bool TryGetThreadInfo(Guid threadId, out ScriptThreadInfo info)
        {
            return m_Threads.TryGetValue(threadId, out info);
        }
    }
}
