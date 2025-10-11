#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.VisualScripting;

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
            Debug.Log($"✅ Created/updated Fooni macros and runners.\n" +
                      $"- Macro: {FloatMacroPath}\n" +
                      $"- Macro: {SayMacroPath}\n" +
                      $"- Runner objects: '{floatRunner.name}', '{sayRunner.name}'\n" +
                      "Note: Ensure a FooniController exists under 'FUnity UI' in the scene.");
        }

        private static ScriptGraphAsset LoadOrCreateMacro(string path, string stickyText)
        {
            var macro = AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(path);
            if (macro == null)
            {
                macro = ScriptableObject.CreateInstance<ScriptGraphAsset>();
                macro.graph = new FlowGraph();
                AssetDatabase.CreateAsset(macro, path);
                AssetDatabase.SaveAssets();
            }

            var graph = macro.graph as FlowGraph;
            if (graph == null)
            {
                graph = new FlowGraph();
                macro.graph = graph;
            }

            bool updated = EnsureStickyNote(graph, stickyText);

            if (updated)
            {
                EditorUtility.SetDirty(macro);
                AssetDatabase.SaveAssets();
            }

            return macro;
        }

        private static bool EnsureStickyNote(FlowGraph graph, string stickyText)
        {
            Note existingNote = null;
            foreach (var element in graph.elements)
            {
                if (element is Note note)
                {
                    existingNote = note;
                    break;
                }
            }

            if (existingNote == null)
            {
                existingNote = new Note
                {
                    title = "Fooni Macro Guide",
                    text = stickyText
                };
                graph.elements.Add(existingNote);
                return true;
            }

            if (existingNote.text != stickyText)
            {
                existingNote.title = "Fooni Macro Guide";
                existingNote.text = stickyText;
                return true;
            }

            return false;
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

            EnsureInspectorHelp(go);

            EditorUtility.SetDirty(go);
            return go;
        }

        private static void EnsureInspectorHelp(GameObject go)
        {
            var comment = go.GetComponent<InspectorComment>();
            if (comment == null)
            {
                comment = Undo.AddComponent<InspectorComment>(go);
            }

            bool changed = false;
            if (comment.title != "Setup Reminder")
            {
                comment.title = "Setup Reminder";
                changed = true;
            }

            if (comment.comment != RunnerHelpMessage)
            {
                comment.comment = RunnerHelpMessage;
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
