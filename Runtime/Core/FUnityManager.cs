using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.UI;
using FUnity.Runtime.Core;
#if UNITY_VISUAL_SCRIPTING
using Unity.VisualScripting;
#endif

namespace FUnity.Core
{
    /// <summary>
    /// 起動時に FUnity UI（UIDocument + FooniController）と VS Runner を動的生成。
    /// </summary>
    public sealed class FUnityManager : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDocument;

        [SerializeField]
        private FUnityProjectData project;

        private GameObject _fUnityUI;

        private void Awake()
        {
            if (project == null)
            {
                project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            }

            if (project == null || project.ensureFUnityUI)
            {
                EnsureFUnityUI();
            }

#if UNITY_VISUAL_SCRIPTING
            if (project != null && project.runners != null)
            {
                foreach (var runner in project.runners)
                {
                    if (runner == null)
                    {
                        continue;
                    }

                    CreateRunner(runner);
                }
            }
#endif
        }

        private void EnsureFUnityUI()
        {
            _fUnityUI = GameObject.Find("FUnity UI");
            if (_fUnityUI == null)
            {
                _fUnityUI = new GameObject("FUnity UI");
            }

            uiDocument = _fUnityUI.GetComponent<UIDocument>() ?? _fUnityUI.AddComponent<UIDocument>();

            var controller = _fUnityUI.GetComponent<FooniController>() ?? _fUnityUI.AddComponent<FooniController>();
            controller.SetUIDocument(uiDocument);
        }

#if UNITY_VISUAL_SCRIPTING
        private void CreateRunner(FUnityProjectData.RunnerEntry entry)
        {
            var go = new GameObject(string.IsNullOrEmpty(entry.name) ? "FUnity VS Runner" : entry.name);

            var machine = go.AddComponent<ScriptMachine>();
            machine.nest.source = GraphSource.Macro;
            machine.nest.macro = entry.macro;

            if (entry.objectVariables != null)
            {
                foreach (var variable in entry.objectVariables)
                {
                    if (string.IsNullOrEmpty(variable.key))
                    {
                        continue;
                    }

                    Variables.Object(go).Set(variable.key, variable.value);
                }
            }

            if (!Variables.Object(go).IsDefined("FUnityUI") && _fUnityUI != null)
            {
                Variables.Object(go).Set("FUnityUI", _fUnityUI);
            }
        }
#endif
    }
}
