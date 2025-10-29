#if UNITY_EDITOR
using FUnity.Runtime.Core;
using UnityEngine;
using UE = UnityEditor;

namespace FUnity.Editor.Inspectors
{
    /// <summary>
    /// <see cref="FUnityStageData"/> の Inspector を拡張し、背景スケールを Popup で選択できるようにするエディタ拡張クラスです。
    /// </summary>
    [UE.CustomEditor(typeof(FUnityStageData))]
    public sealed class FUnityStageDataEditor : UE.Editor
    {
        /// <summary>背景スケールのシリアライズドプロパティ参照。</summary>
        private UE.SerializedProperty m_propertyBackgroundScale;

        /// <summary>Popup 表示に使用する背景スケール候補。</summary>
        private static readonly string[] s_backgroundScaleOptions =
        {
            FUnityStageData.BackgroundScaleContain,
            FUnityStageData.BackgroundScaleCover,
        };

        /// <summary>
        /// エディタが有効化されたタイミングでシリアライズドプロパティをキャッシュします。
        /// </summary>
        private void OnEnable()
        {
            m_propertyBackgroundScale = serializedObject.FindProperty("m_backgroundScale");
        }

        /// <summary>
        /// Inspector 上の描画処理を実行します。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBackgroundScalePopup();

            DrawPropertiesExcluding(serializedObject, "m_Script", "m_backgroundScale");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 背景スケールを contain/cover から選択する Popup を描画します。
        /// </summary>
        private void DrawBackgroundScalePopup()
        {
            if (m_propertyBackgroundScale == null)
            {
                UE.EditorGUILayout.HelpBox("背景スケールのプロパティが見つかりません。ScriptableObject の定義を確認してください。", UE.MessageType.Warning);
                return;
            }

            var rawValue = (m_propertyBackgroundScale.stringValue ?? string.Empty).Trim().ToLowerInvariant();
            var currentIndex = rawValue == FUnityStageData.BackgroundScaleCover ? 1 : 0;

            UE.EditorGUI.BeginChangeCheck();
            var nextIndex = UE.EditorGUILayout.Popup(new GUIContent("Background Scale"), currentIndex, s_backgroundScaleOptions);
            if (UE.EditorGUI.EndChangeCheck())
            {
                var clampedIndex = Mathf.Clamp(nextIndex, 0, s_backgroundScaleOptions.Length - 1);
                m_propertyBackgroundScale.stringValue = s_backgroundScaleOptions[clampedIndex];
            }
        }
    }
}
#endif
