#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.UI;

namespace FUnity.Editor.Diagnostics
{
    /// <summary>
    /// シーン内の UIDocument に RootLayoutBootstrapper を一括付与する診断メニューを提供するユーティリティクラス。
    /// </summary>
    public static class AttachRootLayoutBootstrapper
    {
        /// <summary>
        /// メニューから実行された際にアクティブシーン内の UIDocument を探索し、未付与の GameObject に RootLayoutBootstrapper を追加する。
        /// </summary>
        [MenuItem("FUnity/Diagnostics/Attach RootLayoutBootstrapper")]
        private static void Attach()
        {
            var documents = Object.FindObjectsOfType<UIDocument>(true);
            if (documents == null || documents.Length == 0)
            {
                Debug.LogWarning("[FUnity.Diagnostics] UIDocument がシーンに存在しないため、RootLayoutBootstrapper を付与できません。");
                return;
            }

            var attachedCount = 0;
            foreach (var document in documents)
            {
                if (document == null)
                {
                    continue;
                }

                var targetObject = document.gameObject;
                if (targetObject == null)
                {
                    continue;
                }

                if (targetObject.GetComponent<RootLayoutBootstrapper>() != null)
                {
                    continue;
                }

                Undo.AddComponent<RootLayoutBootstrapper>(targetObject);
                attachedCount++;
                Debug.Log($"[FUnity.Diagnostics] RootLayoutBootstrapper を付与しました: {targetObject.name}");
            }

            if (attachedCount == 0)
            {
                Debug.Log("[FUnity.Diagnostics] すべての UIDocument に RootLayoutBootstrapper が既に付与されています。");
            }
        }
    }
}
#endif
