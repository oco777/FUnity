using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;

namespace FUnity.Runtime.Core
{
    [CreateAssetMenu(menuName = "FUnity/Actor Data", fileName = "FUnityActorData")]
    public sealed class FUnityActorData : ScriptableObject
    {
        [SerializeField] private string m_displayName = "Fooni";
        [SerializeField] private Texture2D m_portrait;          // UI表示用など
        [SerializeField] private Vector2 m_initialPosition = new Vector2(0, 0);
        [SerializeField] private float m_moveSpeed = 300f;
        [SerializeField] private bool m_floatAnimation = true;   // ふわふわの初期状態

        [Header("UI Template (optional)")]
        [SerializeField] private VisualTreeAsset m_ElementUxml;
        [SerializeField] private StyleSheet m_ElementStyle;

        [Header("Layout")]
        [Tooltip("Actor element size in pixels. Set 0 or negative to defer to USS/UXML.")]
        [SerializeField] private Vector2 m_size = new Vector2(128, 128);

        [Header("Visual Scripting")]
        [SerializeField] private ScriptGraphAsset m_scriptGraph;

        public string DisplayName => m_displayName;
        public Texture2D Portrait => m_portrait;
        public Vector2 InitialPosition => m_initialPosition;
        public float MoveSpeed => m_moveSpeed;
        public bool FloatAnimation => m_floatAnimation;
        public VisualTreeAsset ElementUxml => m_ElementUxml;
        public StyleSheet ElementStyle => m_ElementStyle;
        public Vector2 Size => m_size;
        public ScriptGraphAsset ScriptGraph => m_scriptGraph;
    }
}
