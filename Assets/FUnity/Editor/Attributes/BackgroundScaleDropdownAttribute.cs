#if UNITY_EDITOR
using System;
using UnityEngine;

namespace FUnity.Editor.Attributes
{
    /// <summary>
    /// FUnityStageData の背景スケール設定を Inspector 上でドロップダウン表示させるための属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class BackgroundScaleDropdownAttribute : PropertyAttribute
    {
    }
}
#endif
