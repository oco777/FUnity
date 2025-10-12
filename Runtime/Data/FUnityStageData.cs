using UnityEngine;

namespace FUnity.Runtime.Core
{
    [CreateAssetMenu(menuName = "FUnity/Stage Data", fileName = "FUnityStageData")]
    public sealed class FUnityStageData : ScriptableObject
    {
        [SerializeField] private string m_stageName = "Default Stage";
        [SerializeField] private Color m_backgroundColor = Color.black;
        // TODO: BGM, 背景画像など将来拡張

        public string StageName => m_stageName;
        public Color BackgroundColor => m_backgroundColor;
    }
}
