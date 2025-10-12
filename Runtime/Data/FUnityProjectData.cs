using System.Collections.Generic;
using UnityEngine;
#if UNITY_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif

namespace FUnity.Runtime.Core
{
    [CreateAssetMenu(menuName = "FUnity/Project Data", fileName = "FUnityProjectData")]
    public sealed class FUnityProjectData : ScriptableObject
    {
        [System.Serializable]
        public class RunnerEntry
        {
            public string name = "FUnity VS Runner";
#if UNITY_VISUAL_SCRIPTING
            public ScriptGraphAsset macro;
#endif
            [System.Serializable]
            public class ObjectVar
            {
                public string key;
                public Object value;
            }

            public List<ObjectVar> objectVariables = new List<ObjectVar>();
        }

        [Header("Runtime setup")]
        public bool ensureFUnityUI = true;

        [Header("Visual Scripting Runners")]
        public List<RunnerEntry> runners = new List<RunnerEntry>();

        [Header("Stage & Actors")]
        [SerializeField] private FUnityStageData m_stage;
        [SerializeField] private List<FUnityActorData> m_actors = new List<FUnityActorData>();

        public FUnityStageData Stage => m_stage;
        public List<FUnityActorData> Actors => m_actors;
    }
}
