namespace FUnity.Stage
{
    /// <summary>
    /// Visual Scripting グラフからステージを参照しやすくする軽量なヘルパーです。
    /// </summary>
    public static class StageVisualScripting
    {
        /// <summary>
        /// アクティブなステージランタイムのインスタンスがあれば返します。
        /// </summary>
        public static StageRuntime? GetStage() => StageRuntime.Instance;

        /// <summary>
        /// 定義用 ScriptableObject を使ってスプライトを出現させます。
        /// Visual Scripting の「Invoke Member」ノードから直接呼び出せる便利なラッパーです。
        /// </summary>
        public static StageSpriteActor? Spawn(StageSpriteDefinition definition) => StageRuntime.Instance?.SpawnSprite(definition);
    }
}
