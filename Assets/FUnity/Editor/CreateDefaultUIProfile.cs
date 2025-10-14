#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using FUnity.Runtime.UI;

namespace FUnity.EditorTools
{
    // UI Profile をプロジェクトに常備させる Editor ユーティリティ。初期セットアップやサンプル確認前に実行する。UNITY_EDITOR 前提。
    public static class CreateDefaultUIProfile
    {
        // UI Profile を保存する正規フォルダ。Resources 経由のランタイムロードを単純化するため固定パスに集約する。
        private const string TargetDirectory = "Assets/FUnity/Configs/UIProfiles";
        private const string AssetPath = TargetDirectory + "/UIProfile_Default.asset";

        // Default UI Load Profile を生成するメニュー。既存があれば再利用し、新規生成時のみ ScriptableObject を作る。
        [MenuItem("FUnity/Create/Default UI Load Profile")]
        /// <summary>
        /// - フォルダを作成し、正規パスに既存アセットが無いか確認する。
        /// - 既存があれば警告のみ出して選択し、処理を終了する。
        /// - 無ければ ScriptableObject を生成し既定値を代入、AssetDatabase へ登録する。
        /// - Save/Refresh まで行い、作成物を Project ビューで選択する。
        /// </summary>
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

        // CreateProjectData と連携し、UI/Actor 生成時に参照されるロードプロファイルの基礎を整える役割を担う。
    }
}
#endif
