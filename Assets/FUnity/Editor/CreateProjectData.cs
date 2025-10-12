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
            const string dir = "Assets/Resources";
            Directory.CreateDirectory(dir);

            var project = ScriptableObject.CreateInstance<FUnityProjectData>();
            var projectPath = $"{dir}/FUnityProjectData.asset";
            AssetDatabase.CreateAsset(project, projectPath);

            var stage = ScriptableObject.CreateInstance<FUnityStageData>();
            var stagePath = $"{dir}/FUnityStageData.asset";
            AssetDatabase.CreateAsset(stage, stagePath);

            var actor = ScriptableObject.CreateInstance<FUnityActorData>();
            var actorPath = $"{dir}/FUnityActorData_Fooni.asset";
            AssetDatabase.CreateAsset(actor, actorPath);

            var so = new SerializedObject(project);
            so.FindProperty("m_stage").objectReferenceValue = stage;

            var actorsProp = so.FindProperty("m_actors");
            actorsProp.arraySize = 1;
            actorsProp.GetArrayElementAtIndex(0).objectReferenceValue = actor;
            so.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            Selection.activeObject = project;

            Debug.Log("✅ Created & linked: FUnityProjectData + Stage/Actor assets under Assets/Resources");
        }
    }
}
#endif
