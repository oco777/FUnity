#if UNITY_EDITOR
using FUnity.Editor.Attributes;
using UnityEditor;
using UnityEngine;

namespace FUnity.Editor.Drawers
{
    /// <summary>
    /// 文字列フィールドを背景スケール用の contain/cover ポップアップとして描画する PropertyDrawer。
    /// </summary>
    [CustomPropertyDrawer(typeof(BackgroundScaleDropdownAttribute))]
    public sealed class BackgroundScaleDropdownDrawer : PropertyDrawer
    {
        /// <summary>Inspector に表示する選択肢一覧。将来の選択肢追加時はここを拡張する。</summary>
        private static readonly string[] s_displayOptions = { "contain", "cover" };

        /// <summary>
        /// Popup の描画処理。contain/cover 以外の値は contain として扱い、ユーザー操作時に正規化する。
        /// </summary>
        /// <param name="position">描画位置。</param>
        /// <param name="property">対象プロパティ。</param>
        /// <param name="label">表示ラベル。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var rawValue = (property.stringValue ?? string.Empty).Trim().ToLowerInvariant();
            var currentIndex = rawValue == "cover" ? 1 : 0;

            EditorGUI.BeginProperty(position, label, property);
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, s_displayOptions);
            EditorGUI.EndProperty();

            var normalizedValue = newIndex == 1 ? "cover" : "contain";
            if (property.stringValue != normalizedValue)
            {
                property.stringValue = normalizedValue;
            }
        }

        /// <summary>単一行分の高さを返す。</summary>
        /// <param name="property">対象プロパティ。</param>
        /// <param name="label">表示ラベル。</param>
        /// <returns>標準的な 1 行の高さ。</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
