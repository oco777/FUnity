#if UNITY_EDITOR
using System.Linq;
using FUnity.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.EditorTools
{
    /// <summary>
    /// Safely removes FUnityUIInitializer from the active scene and ensures a minimal setup:
    /// UIDocument + FooniController. It creates "FUnity UI" if missing and wires references.
    /// </summary>
    public static class RemoveFUnityUIInitializer
    {
        private const string TargetGoName = "FUnity UI";

        [MenuItem("FUnity/VS/Remove FUnityUIInitializer (Safe)")]
        public static void RemoveInitializerAndSetupController()
        {
            // 1) Active scene
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("❌ No valid active scene. Open a scene and re-run.");
                return;
            }

            // 2) Ensure "FUnity UI" GO
            var go = GameObject.Find(TargetGoName);
            if (go == null)
            {
                go = new GameObject(TargetGoName);
                Debug.Log($"ℹ️ Created GameObject '{TargetGoName}' in scene '{scene.name}'.");
            }

            // 3) Remove all FUnityUIInitializer in the scene (safe cleanup)
            int removed = 0;
            var initializers = Object.FindObjectsByType<FUnityUIInitializer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var init in initializers)
            {
                Undo.DestroyObjectImmediate(init);
                removed++;
            }

            // 4) Ensure UIDocument
            var doc = go.GetComponent<UIDocument>() ?? Undo.AddComponent<UIDocument>(go);

            // 5) Ensure FooniController
            var controller = go.GetComponent<FooniController>() ?? Undo.AddComponent<FooniController>(go);

            // 6) Wire controller.uiDocument = doc (SerializedObject is safer for private field)
            var so = new SerializedObject(controller);
            var prop = so.FindProperty("uiDocument");
            if (prop != null && prop.objectReferenceValue != doc)
            {
                prop.objectReferenceValue = doc;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 7) Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"✅ Removed FUnityUIInitializer (count: {removed}). " +
                      $"Ensured UIDocument + FooniController on '{TargetGoName}' in scene '{scene.name}'.");
            Selection.activeObject = go;
        }
    }
}
#endif
