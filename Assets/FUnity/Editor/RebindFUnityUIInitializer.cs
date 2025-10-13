#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using FUnity.Runtime.UI;

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
            if (go == null)
            {
                Debug.LogError("[FUnity] GameObject 'FUnity UI' not found in scene.");
                return;
            }

            // Remove all missing components
            var before = go.GetComponents<Component>().Count(component => component == null);
            if (before > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            }

            var after = go.GetComponents<Component>().Count(component => component == null);
            var removed = before - after;

            // Ensure UIDocument
            var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();

            // Ensure FUnityUIInitializer (package type)
            var init = go.GetComponent<FUnityUIInitializer>() ?? go.AddComponent<FUnityUIInitializer>();

            // Try assign any UILoadProfile
            var guid = AssetDatabase.FindAssets("t:UILoadProfile").FirstOrDefault();
            var profileAssigned = false;
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<UILoadProfile>(path);
                if (profile != null)
                {
                    var serializedInitializer = new SerializedObject(init);
                    var profileProperty = serializedInitializer.FindProperty("m_Profile");
                    if (profileProperty != null && profileProperty.objectReferenceValue != profile)
                    {
                        profileProperty.objectReferenceValue = profile;
                        serializedInitializer.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(init);
                        profileAssigned = true;
                    }
                }
            }

            EditorUtility.SetDirty(go);
            Debug.Log(
                $"[FUnity] Rebound FUnity UI. Removed missing components: {removed}. Profile assigned: {profileAssigned}.");
            Selection.activeObject = go;
        }
    }
}
#endif
