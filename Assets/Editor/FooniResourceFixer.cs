using System.IO;
using UnityEditor;
using UnityEngine;

namespace FUnity.EditorTools
{
    public static class FooniResourceFixer
    {
        private const string SourcePath = "Assets/FUnity/Art/Characters/Fooni.png";
        private const string TargetDirectory = "Assets/FUnity/Runtime/Resources/Characters";
        private const string TargetPath = "Assets/FUnity/Runtime/Resources/Characters/Fooni.png";

        [MenuItem("FUnity/Fix Fooni Resource")]
        public static void CopyFooniTextureToResources()
        {
            if (!File.Exists(SourcePath))
            {
                Debug.LogError("❌ Fooni.png が見つかりません。");
                return;
            }

            if (!Directory.Exists(TargetDirectory))
            {
                Directory.CreateDirectory(TargetDirectory);
            }

            if (File.Exists(TargetPath))
            {
                Debug.LogWarning("⚠️ すでにコピー済みです。");
                return;
            }

            File.Copy(SourcePath, TargetPath, overwrite: false);
            AssetDatabase.Refresh();
            Debug.Log("✅ Fooni.png を Resources/Characters にコピーしました。");

            // Ensure that Resources.Load<Texture2D>("Characters/Fooni") succeeds after the copy.
        }
    }
}
