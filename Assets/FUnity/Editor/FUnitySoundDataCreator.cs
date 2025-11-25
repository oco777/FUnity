#if UNITY_EDITOR
using System.IO;
using FUnity.Runtime.Audio;
using FUnity.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace FUnity.EditorTools
{
    /// <summary>
    /// 現在の FUnityProjectData と同じフォルダに FUnitySoundData を 1 つだけ生成する Editor メニュー実装。
    /// 既に存在する場合は新規作成せずに既存アセットを選択する。
    /// </summary>
    public static class FUnitySoundDataCreator
    {
        /// <summary>サウンドデータ生成用メニュー。ProjectData が見つからない場合はダイアログで案内する。</summary>
        [MenuItem("FUnity/Create/FUnitySoundData")]
        public static void CreateSoundData()
        {
            var project = FindCurrentProjectData();
            if (project == null)
            {
                EditorUtility.DisplayDialog(
                    "FUnitySoundData",
                    "現在の FUnityProjectData が見つかりません。\nシーン内の FUnityManager に割り当てるか、プロジェクトアセットを選択してください。",
                    "OK");
                return;
            }

            var projectPath = AssetDatabase.GetAssetPath(project);
            if (string.IsNullOrEmpty(projectPath))
            {
                EditorUtility.DisplayDialog(
                    "FUnitySoundData",
                    "FUnityProjectData のパスを取得できませんでした。アセットが保存済みか確認してください。",
                    "OK");
                return;
            }

            var folderPath = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(folderPath))
            {
                EditorUtility.DisplayDialog(
                    "FUnitySoundData",
                    "FUnityProjectData のフォルダを特定できませんでした。",
                    "OK");
                return;
            }

            var existingGuids = AssetDatabase.FindAssets("t:FUnitySoundData", new[] { folderPath });
            FUnitySoundData soundData = null;

            if (existingGuids != null && existingGuids.Length > 0)
            {
                var existingPath = AssetDatabase.GUIDToAssetPath(existingGuids[0]);
                soundData = AssetDatabase.LoadAssetAtPath<FUnitySoundData>(existingPath);

                EditorUtility.DisplayDialog(
                    "FUnitySoundData",
                    "このフォルダには既に FUnitySoundData.asset が存在します。既存アセットを選択します。",
                    "OK");
            }
            else
            {
                var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPath, "FUnitySoundData.asset"));
                soundData = ScriptableObject.CreateInstance<FUnitySoundData>();
                AssetDatabase.CreateAsset(soundData, assetPath);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayDialog(
                    "FUnitySoundData",
                    "新しい FUnitySoundData を作成し、現在のプロジェクトに設定しました。",
                    "OK");
            }

            if (soundData != null)
            {
                var isProjectSoundDataChanged = project.soundData != soundData;
                project.soundData = soundData;

                if (isProjectSoundDataChanged)
                {
                    EditorUtility.SetDirty(project);
                    AssetDatabase.SaveAssets();
                }

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = soundData;
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "FUnitySoundData",
                    "FUnitySoundData の取得または作成に失敗しました。",
                    "OK");
            }
        }

        /// <summary>
        /// 選択状態やシーン内の FUnityManager から現在の ProjectData を推定して返す。
        /// </summary>
        /// <returns>見つかった FUnityProjectData。取得できない場合は null。</returns>
        private static FUnityProjectData FindCurrentProjectData()
        {
            if (Selection.activeObject is FUnityProjectData selectedProject)
            {
                return selectedProject;
            }

            var manager = Object.FindObjectOfType<FUnityManager>();
            if (manager != null && manager.ProjectData != null)
            {
                return manager.ProjectData;
            }

            var guids = AssetDatabase.FindAssets("t:FUnityProjectData");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<FUnityProjectData>(path);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }
    }
}
#endif
