using System.Linq;
using FUnity.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.EditorTools
{
    /// <summary>
    /// Rebinds the scene's "FUnity UI" GameObject by removing missing MonoBehaviours,
    /// ensuring UIDocument and FUnityUIInitializer (from package) are attached,
    /// and optionally assigning a UILoadProfile if one exists.
    /// </summary>
    public static class RebindFUnityUIInitializer
    {
        [MenuItem("FUnity/Fix/Rebind FUnity UI Initializer")]
        public static void Rebind()
        {
            var go = GameObject.Find("FUnity UI");
            if (!go)
            {
                Debug.LogError("❌ GameObject 'FUnity UI' not found in scene.");
                return;
            }

            // Remove all missing components
            int before = go.GetComponents<Component>().Count(c => c == null);
            if (before > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            int after = go.GetComponents<Component>().Count(c => c == null);
            int removed = before - after;

            // Ensure UIDocument
            var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();

            // Ensure FUnityUIInitializer (package type)
            var init = go.GetComponent<FUnityUIInitializer>() ?? go.AddComponent<FUnityUIInitializer>();

            // Try assign any UILoadProfile
            var guid = AssetDatabase.FindAssets("t:UILoadProfile").FirstOrDefault();
            bool profileAssigned = false;
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<UILoadProfile>(path);
                if (profile != null)
                {
                    var so = new SerializedObject(init);
                    var p = so.FindProperty("profile");
                    if (p != null)
                    {
                        p.objectReferenceValue = profile;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        profileAssigned = true;
                    }
                }
            }

            EditorUtility.SetDirty(go);
            Debug.Log($"✅ Rebound FUnity UI. Removed missing: {removed}. Profile assigned: {profileAssigned}");
            Selection.activeObject = go;
        }
    }
}
