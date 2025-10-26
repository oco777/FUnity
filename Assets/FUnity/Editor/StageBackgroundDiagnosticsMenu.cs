#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Presenter;

namespace FUnity.Editor
{
    /// <summary>
    /// エディタ上から背景レイヤ診断を起動するメニューを提供します。
    /// </summary>
    public static class StageBackgroundDiagnosticsMenu
    {
        /// <summary>
        /// 選択中の UIDocument から背景レイヤ診断を起動します。
        /// </summary>
        [MenuItem("FUnity/Diagnostics/Run Background Diagnostics")]
        public static void Run()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[FUnity.BGDiag] Selection is null");
                return;
            }

            var doc = go.GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null)
            {
                Debug.LogWarning("[FUnity.BGDiag] UIDocument が見つかりません。UI の GameObject を選択して実行してください。");
                return;
            }

            StageBackgroundDiagnostics.RunBackgroundDiagnostics(doc.rootVisualElement, "Background_01", tryTemporaryFix: true);
        }
    }
}
#endif
