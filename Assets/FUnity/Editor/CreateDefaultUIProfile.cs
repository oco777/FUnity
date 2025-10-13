#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using FUnity.Runtime.UI;

namespace FUnity.EditorTools
{
    public static class CreateDefaultUIProfile
    {
        private const string TargetDirectory = "Assets/FUnity/Configs/UIProfiles";
        private const string AssetPath = TargetDirectory + "/UIProfile_Default.asset";

        [MenuItem("FUnity/Create/Default UI Load Profile")]
        public static void CreateDefaultProfile()
        {
            Directory.CreateDirectory(TargetDirectory);

            var profile = AssetDatabase.LoadAssetAtPath<UILoadProfile>(AssetPath);
            if (profile != null)
            {
                Debug.LogWarning($"[FUnity] Default UI Load Profile already exists at {AssetPath}.");
                Selection.activeObject = profile;
                return;
            }

            profile = ScriptableObject.CreateInstance<UILoadProfile>();
            profile.spawnFooni = true;
            profile.spawnBlocks = false;

            AssetDatabase.CreateAsset(profile, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FUnity] Default UI Load Profile created at {AssetPath}.");
            Selection.activeObject = profile;
        }
    }
}
#endif
