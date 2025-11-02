// Path: Runtime/Core/MessageBus.cs
// Summary: シンプルなメッセージ配信（Pub/Sub）とハンドラ完了待ちカウントを提供します。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// 文字列キーでメッセージを配信する軽量メッセージバスです。
    /// 「送って待つ」機能向けに、ハンドラ実行中カウントも追跡します。
    /// </summary>
    public static class MessageBus
    {
        /// <summary>メッセージごとの登録ハンドラ一覧を保持します。</summary>
        private static readonly Dictionary<string, List<Action<object>>> s_Subscribers
            = new Dictionary<string, List<Action<object>>>();

        /// <summary>メッセージごとの実行中ハンドラ数を保持します。</summary>
        private static readonly Dictionary<string, int> s_ActiveHandlerCount
            = new Dictionary<string, int>();

        /// <summary>
        /// メッセージへハンドラを登録します。
        /// message が null/空の場合や handler が null の場合は何もしません。
        /// </summary>
        public static void Subscribe(string message, Action<object> handler)
        {
            if (string.IsNullOrEmpty(message) || handler == null)
            {
                return;
            }

            if (!s_Subscribers.TryGetValue(message, out var list))
            {
                list = new List<Action<object>>();
                s_Subscribers[message] = list;
            }

            if (!list.Contains(handler))
            {
                list.Add(handler);
            }
        }

        /// <summary>
        /// メッセージからハンドラ登録を解除します。
        /// message が null/空の場合や handler が null の場合は何もしません。
        /// </summary>
        public static void Unsubscribe(string message, Action<object> handler)
        {
            if (string.IsNullOrEmpty(message) || handler == null)
            {
                return;
            }

            if (!s_Subscribers.TryGetValue(message, out var list))
            {
                return;
            }

            list.Remove(handler);
            if (list.Count == 0)
            {
                s_Subscribers.Remove(message);
            }
        }

        /// <summary>
        /// メッセージを配信します。登録ハンドラがあれば順に呼び出します。
        /// payload は任意のオブジェクトで null も許容します。
        /// ハンドラ実行中はカウントを増減させ、"送って待つ" での待機を可能にします。
        /// </summary>
        public static void Publish(string message, object payload = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (!s_Subscribers.TryGetValue(message, out var list) || list.Count == 0)
            {
                return;
            }

            if (!s_ActiveHandlerCount.ContainsKey(message))
            {
                s_ActiveHandlerCount[message] = 0;
            }

            foreach (var handler in list.ToArray())
            {
                try
                {
                    s_ActiveHandlerCount[message]++;
                    handler?.Invoke(payload);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[MessageBus] Handler error on '{message}': {exception}");
                }
                finally
                {
                    s_ActiveHandlerCount[message] = Math.Max(0, s_ActiveHandlerCount[message] - 1);
                }
            }
        }

        /// <summary>
        /// 指定メッセージの実行中ハンドラ数を返します。
        /// message が null/空の場合は 0 を返します。
        /// </summary>
        public static int GetActiveHandlerCount(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return 0;
            }

            return s_ActiveHandlerCount.TryGetValue(message, out var count) ? count : 0;
        }
    }
}
