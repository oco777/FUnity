#if UNITY_EDITOR
using System.IO;
using FUnity.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace FUnity.EditorTools
{
    public static class CreateProjectData
    {
        [MenuItem("FUnity/Create/Default Project Data")]
        public static void CreateDefault()
        {
            const string directory = "Assets/Resources";
            const string assetPath = directory + "/FUnityProjectData.asset";
            Directory.CreateDirectory(directory);

            var asset = ScriptableObject.CreateInstance<FUnityProjectData>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;

            Debug.Log($"✅ Created: {assetPath}");
        }
    }
}
#endif
