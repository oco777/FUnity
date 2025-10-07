using UnityEngine;

namespace FUnity.Core
{
    /// <summary>
    /// FUnity ランタイムシステムの初期化を行うエントリーポイントを提供します。
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
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
