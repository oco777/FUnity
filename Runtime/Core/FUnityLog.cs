// Updated: 2025-03-10
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FUnity.Core
{
    /// <summary>
    /// FUnity の初期化処理に関するログ出力を集約するユーティリティ。
    /// 共通のプレフィックスとエディタ専用詳細ログを提供し、原因究明を容易にする。
    /// </summary>
    internal static class FUnityLog
    {
        /// <summary>初期化ログに付与するプレフィックス。</summary>
        private const string Prefix = "[FUnity.Init]";

        /// <summary>
        /// 重大な問題を表すエラーログを出力する。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        public static void LogError(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }

        /// <summary>
        /// 実行継続可能な警告を出力する。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        /// <summary>
        /// 情報レベルのログを出力する。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        public static void LogInfo(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        /// <summary>
        /// Resources 内で見つからなかったアセットをエディタ専用の詳細ログとして出力する。
        /// </summary>
        /// <param name="description">不足しているアセットの説明。</param>
        /// <param name="triedPath">探索した Resources パス。</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogMissingResource(string description, string triedPath)
        {
            Debug.LogWarning($"{Prefix} {description} が見つかりません (Resources/{triedPath})。");
        }

        /// <summary>
        /// フォールバック生成をエディタ専用ログとして記録する。
        /// </summary>
        /// <param name="description">生成したフォールバックの説明。</param>
        [Conditional("UNITY_EDITOR")]
        public static void LogCreateFallback(string description)
        {
            Debug.Log($"{Prefix} {description} をフォールバックとして生成しました。");
        }
    }
}
