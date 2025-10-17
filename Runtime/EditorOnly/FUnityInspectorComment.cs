#if UNITY_EDITOR
// Updated: 2025-02-14
using UnityEngine;

namespace FUnity.EditorTools
{
    /// <summary>
    /// エディタ上でメモを残すための MonoBehaviour。MVP の枠外にある補助ツールとして設置する。
    /// </summary>
    /// <remarks>
    /// 依存関係: UnityEditor のインスペクター
    /// 想定ライフサイクル: エディタのみで常駐（<see cref="ExecuteAlwaysAttribute"/>）。ランタイムビルドには含まれない。
    /// </remarks>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class FUnityInspectorComment : MonoBehaviour
    {
        /// <summary>
        /// インスペクターに表示するタイトル。ワークフロー上の注意点などを記載する。
        /// </summary>
        [SerializeField]
        private string m_title = "Setup Reminder";

        /// <summary>
        /// 表示する本文テキスト。リッチテキストは非対応。
        /// </summary>
        [SerializeField]
        [TextArea(2, 10)]
        private string m_comment;

        /// <summary>
        /// 表示中タイトルを取得・更新する。
        /// </summary>
        public string Title
        {
            get => m_title;
            set => m_title = value;
        }

        /// <summary>
        /// 表示中コメントを取得・更新する。
        /// </summary>
        public string Comment
        {
            get => m_comment;
            set => m_comment = value;
        }
    }
}
#endif
