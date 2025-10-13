using UnityEngine;

namespace FUnity.Runtime.Core
{
    [CreateAssetMenu(menuName = "FUnity/Stage Data", fileName = "FUnityStageData")]
    public sealed class FUnityStageData : ScriptableObject
    {
        [SerializeField] private string m_stageName = "Default Stage";
        [SerializeField] private Color m_backgroundColor = Color.black;
        // TODO: BGM, 背景画像など将来拡張

        [Header("Background")]
        [SerializeField] private Texture2D m_backgroundImage;
        [SerializeField] private ScaleMode m_backgroundScale = ScaleMode.ScaleAndCrop;

        public string StageName => m_stageName;
        public Color BackgroundColor => m_backgroundColor;
        public Texture2D BackgroundImage => m_backgroundImage;
        public ScaleMode BackgroundScale => m_backgroundScale;
    }
}
