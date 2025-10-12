#if UNITY_EDITOR
using UnityEngine;

namespace FUnity.EditorTools
{
    /// <summary>Simple inspector note component for Editor-only use.</summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class FUnityInspectorComment : MonoBehaviour
    {
        public string title = "Setup Reminder";
        [TextArea(2, 10)]
        public string comment;
    }
}
#endif
