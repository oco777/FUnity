using System.Linq;
using FUnity.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.EditorTools
{
    /// <summary>
    /// Rebinds the scene's "FUnity UI" GameObject by removing missing MonoBehaviours,
    /// ensuring UIDocument and FUnityUIInitializer are attached, and optionally assigning
    /// a UILoadProfile if one exists in the project.
    /// </summary>
    public static class RebindFUnityUIInitializer
    {
        [MenuItem("FUnity/Fix/Rebind FUnity UI Initializer")]
        public static void Rebind()
        {
            // 1) Find target GameObject
            var go = GameObject.Find("FUnity UI");
            if (!go)
            {
                Debug.LogError("❌ GameObject 'FUnity UI' not found in the current scene.");
                return;
            }

            // 2) Remove all missing MonoBehaviours on the GameObject
            int before = go.GetComponents<Component>().Count(c => c == null);
            if (before > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }
            int after = go.GetComponents<Component>().Count(c => c == null);
            int removed = before - after;

            // 3) Ensure UIDocument
            var doc = go.GetComponent<UIDocument>();
            if (doc == null)
            {
                doc = go.AddComponent<UIDocument>();
            }

            // 4) Ensure FUnityUIInitializer
            var init = go.GetComponent<FUnityUIInitializer>();
            if (init == null)
            {
                init = go.AddComponent<FUnityUIInitializer>();
            }

            // 5) Optionally assign a UILoadProfile if found
            var profileGuid = AssetDatabase.FindAssets("t:UILoadProfile").FirstOrDefault();
            bool profileAssigned = false;
            if (!string.IsNullOrEmpty(profileGuid))
            {
                var path = AssetDatabase.GUIDToAssetPath(profileGuid);
                var profile = AssetDatabase.LoadAssetAtPath<UILoadProfile>(path);
                if (profile != null)
                {
                    var soInit = new SerializedObject(init);
                    var prop = soInit.FindProperty("profile");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = profile;
                        soInit.ApplyModifiedPropertiesWithoutUndo();
                        profileAssigned = true;
                    }
                }
            }

            // Mark scene dirty for saving if needed
            EditorUtility.SetDirty(go);
            Debug.Log($"✅ Rebound FUnity UI. Removed missing: {removed}. Profile assigned: {profileAssigned}");
            Selection.activeObject = go;
        }
    }
}
