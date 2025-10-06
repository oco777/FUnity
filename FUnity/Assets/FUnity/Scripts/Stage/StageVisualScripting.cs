namespace FUnity.Stage
{
    /// <summary>
    /// Lightweight helper to make it easier to reference the stage from Visual Scripting graphs.
    /// </summary>
    public static class StageVisualScripting
    {
        /// <summary>
        /// Returns the active stage runtime instance (if any).
        /// </summary>
        public static StageRuntime? GetStage() => StageRuntime.Instance;

        /// <summary>
        /// Spawn a sprite using a definition ScriptableObject.
        /// Convenient wrapper that can be called directly from a Visual Scripting "Invoke Member" node.
        /// </summary>
        public static StageSpriteActor? Spawn(StageSpriteDefinition definition) => StageRuntime.Instance?.SpawnSprite(definition);
    }
}
