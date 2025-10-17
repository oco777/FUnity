// Updated: 2025-02-14
using UnityEngine;

namespace FUnity.Core
{
    /// <summary>
    /// FUnity ランタイムのエントリーポイント。Presenter 群の生成前にログを出力する薄いブートストラップ。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnityManager"/>（同一シーンに配置）
    /// 想定ライフサイクル: シーン読み込み時に <see cref="Awake"/> が 1 度だけ呼び出される。
    /// </remarks>
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>
        /// 初期化ログに含めるワークスペース名。シーンごとに区別するためのメタ情報。
        /// </summary>
        [SerializeField]
        private string m_workspaceName = "Default Workspace";

        /// <summary>
        /// シーン読み込み時にワークスペースの初期化メッセージを出力します。
        /// </summary>
        private void Awake()
        {
            Debug.Log($"[FUnity] Initializing workspace '{m_workspaceName}'.");
        }
    }
}
