#if UNITY_EDITOR
using UnityEngine;

namespace FUnity.EditorTools
{
    /// <summary>Simple inspector note component for Editor-only use.</summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class FUnityInspectorComment : MonoBehaviour
    {
        [SerializeField]
        private string m_title = "Setup Reminder";

        [SerializeField]
        [TextArea(2, 10)]
        private string m_comment;

        public string Title
        {
            get => m_title;
            set => m_title = value;
        }

        public string Comment
        {
            get => m_comment;
            set => m_comment = value;
        }
    }
}
#endif
