using UnityEngine;

namespace FUnity.Stage
{
    /// <summary>
    /// ステージ上に出現させるスプライト情報を表す ScriptableObject です。
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Stage Sprite", fileName = "StageSpriteDefinition")]
    public sealed class StageSpriteDefinition : ScriptableObject
    {
        [SerializeField]
        private string m_displayName = "Sprite";

        [SerializeField]
        private Sprite? m_sprite;

        [SerializeField]
        private Vector2 m_size = new Vector2(128f, 128f);

        [SerializeField]
        private Vector2 m_initialPosition = new Vector2(120f, 120f);

        public string DisplayName => string.IsNullOrWhiteSpace(m_displayName) ? name : m_displayName;
        public Sprite? Sprite => m_sprite;
        public Vector2 Size => m_size;
        public Vector2 InitialPosition => m_initialPosition;

        /// <summary>
        /// エディターツールが新しい定義を生成するときに呼び出すユーティリティメソッドです。
        /// </summary>
        public void Initialize(Sprite newSprite, string friendlyName)
        {
            m_sprite = newSprite;
            m_displayName = string.IsNullOrWhiteSpace(friendlyName) ? newSprite.name : friendlyName;
            m_size = new Vector2(newSprite.rect.width, newSprite.rect.height);
        }
    }
}
