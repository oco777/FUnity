#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.EditorTools
{
    public static class CreateFooniMacros
    {
        private const string MacroDir = "Assets/FUnity/VisualScripting/Macros";
        private const string FloatMacroPath = MacroDir + "/Fooni_FloatSetup.asset";
        private const string SayMacroPath = MacroDir + "/Fooni_SayOnKey.asset";
        private const string RunnerHelpMessage = "Ensure 'FUnity UI' contains a FooniController component before running.";

        [MenuItem("FUnity/VS/Create Fooni Macros & Runner")]
        public static void CreateMacrosAndRunners()
        {
            Directory.CreateDirectory(MacroDir);

            var floatMacro = LoadOrCreateMacro(FloatMacroPath,
                "[How to wire]\nOn Start  →  Get Component (FooniController)  →  EnableFloat(true)\n" +
                "                                     ↘ SetFloatAmplitude(10)\n" +
                "                                     ↘ SetFloatPeriod(3)");

            var sayMacro = LoadOrCreateMacro(SayMacroPath,
                "[How to wire]\nOn Keyboard (Space) → Get Component (FooniController) → Say(\"やっほー！\")\n" +
                "または\nOn Custom Event (\"Fooni/Say\") → 吹き出しUIの表示処理へ");

            var floatRunner = EnsureRunner("Fooni VS Float Runner", floatMacro);
            var sayRunner = EnsureRunner("Fooni VS Say Runner", sayMacro);

            EditorSceneManager.MarkSceneDirty(floatRunner.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[FUnity] Created or updated Fooni macros and runners.\n" +
                $"- Macro: {FloatMacroPath}\n" +
                $"- Macro: {SayMacroPath}\n" +
                $"- Runner objects: '{floatRunner.name}', '{sayRunner.name}'\n" +
                "Ensure a FooniController exists under 'FUnity UI' in the scene.");
        }

        private static ScriptGraphAsset LoadOrCreateMacro(string path, string _)
        {
            var macro = AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(path);
            if (macro == null)
            {
                macro = ScriptableObject.CreateInstance<ScriptGraphAsset>();
                macro.graph = new FlowGraph();
                AssetDatabase.CreateAsset(macro, path);
            }

            if (macro.graph == null)
            {
                macro.graph = new FlowGraph();
            }

            return macro;
        }

        private static GameObject EnsureRunner(string name, ScriptGraphAsset macro)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create Fooni Runner");
            }

            var machine = go.GetComponent<ScriptMachine>();
            if (machine == null)
            {
                machine = Undo.AddComponent<ScriptMachine>(go);
            }

            bool machineDirty = false;

            if (machine.nest.source != GraphSource.Macro)
            {
                machine.nest.source = GraphSource.Macro;
                machineDirty = true;
            }

            if (machine.nest.macro != macro)
            {
                machine.nest.macro = macro;
                machineDirty = true;
            }

            if (machineDirty)
            {
                EditorUtility.SetDirty(machine);
            }

            if (name == "Fooni VS Float Runner")
            {
                var controller = go.GetComponent<FooniController>();
                if (controller == null)
                {
                    controller = Undo.AddComponent<FooniController>(go);
                    EditorUtility.SetDirty(controller);
                    Debug.Log("[FUnity] Added FooniController to runner: " + go.name);
                }
            }

            EnsureInspectorHelp(go);

            EditorUtility.SetDirty(go);
            return go;
        }

        private static void EnsureInspectorHelp(GameObject go)
        {
            var comment = go.GetComponent<FUnityInspectorComment>();
            if (comment == null)
            {
                comment = Undo.AddComponent<FUnityInspectorComment>(go);
            }

            var changed = false;
            if (comment.Title != "Setup Reminder")
            {
                comment.Title = "Setup Reminder";
                changed = true;
            }

            if (comment.Comment != RunnerHelpMessage)
            {
                comment.Comment = RunnerHelpMessage;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(comment);
            }
        }
    }
}
#endif
