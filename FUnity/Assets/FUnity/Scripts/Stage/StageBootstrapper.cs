using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Stage
{
    /// <summary>
    /// シーンがロードされるたびに UI Toolkit ベースのステージが利用可能になるよう保証します。
    /// ブートストラッパーは（プレイモードとビルドの両方で）自動実行され、
    /// UIDocument と StageRuntime を備えた専用の GameObject を生成します。
    /// </summary>
    public static class StageBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        /// <summary>
        /// シーンの読み込み完了後にステージ用 GameObject が存在することを保証します。
        /// </summary>
        private static void EnsureStage()
        {
            if (Object.FindObjectOfType<StageRuntime>() != null)
            {
                return;
            }

            var root = new GameObject("FUnity Stage");
            Object.DontDestroyOnLoad(root);

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "FUnity Stage Panel Settings (Runtime)";
            panelSettings.clearColor = true;
            panelSettings.colorClearValue = Color.clear;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;

            var document = root.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            root.AddComponent<StageRuntime>();
        }
    }
}
