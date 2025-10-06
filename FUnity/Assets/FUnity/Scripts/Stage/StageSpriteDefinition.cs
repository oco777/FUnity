using UnityEngine;

namespace FUnity.Stage
{
    /// <summary>
    /// ScriptableObject used to describe a sprite entry that can be spawned on the stage.
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Stage Sprite", fileName = "StageSpriteDefinition")]
    public sealed class StageSpriteDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Sprite";

        [SerializeField]
        private Sprite? sprite;

        [SerializeField]
        private Vector2 size = new Vector2(128f, 128f);

        [SerializeField]
        private Vector2 initialPosition = new Vector2(120f, 120f);

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Sprite? Sprite => sprite;
        public Vector2 Size => size;
        public Vector2 InitialPosition => initialPosition;

        /// <summary>
        /// Utility method called by editor tools when generating new definitions.
        /// </summary>
        public void Initialize(Sprite newSprite, string friendlyName)
        {
            sprite = newSprite;
            displayName = string.IsNullOrWhiteSpace(friendlyName) ? newSprite.name : friendlyName;
            size = new Vector2(newSprite.rect.width, newSprite.rect.height);
        }
    }
}
