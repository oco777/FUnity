using UnityEngine;

namespace FUnity.Runtime.Core
{
    [CreateAssetMenu(menuName = "FUnity/Actor Data", fileName = "FUnityActorData")]
    public sealed class FUnityActorData : ScriptableObject
    {
        [SerializeField] private string m_displayName = "Fooni";
        [SerializeField] private Texture2D m_portrait;          // UI表示用など
        [SerializeField] private Vector2 m_initialPosition = new Vector2(0, 0);
        [SerializeField] private bool m_floatAnimation = true;   // ふわふわの初期状態

        public string DisplayName => m_displayName;
        public Texture2D Portrait => m_portrait;
        public Vector2 InitialPosition => m_initialPosition;
        public bool FloatAnimation => m_floatAnimation;
    }
}
