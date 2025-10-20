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
        private const string RunnerHelpMessage = "Assign an ActorPresenterAdapter (旧 FooniController) to a GameObject used by your VS graph before running.";

        [MenuItem("FUnity/VS/Create Fooni Macros & Runner")]
        public static void CreateMacrosAndRunners()
        {
            Directory.CreateDirectory(MacroDir);

            var floatMacro = LoadOrCreateMacro(FloatMacroPath,
                "[How to wire]\nOn Start  →  Get Component (ActorPresenterAdapter)  →  EnableFloat(true)\n" +
                "                                     ↘ SetFloatAmplitude(10)\n" +
                "                                     ↘ SetFloatPeriod(3)");

            var sayMacro = LoadOrCreateMacro(SayMacroPath,
                "[How to wire]\nOn Keyboard (Space) → Get Component (ActorPresenterAdapter) → Say(\"やっほー！\")\n" +
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
                "Assign an ActorPresenterAdapter (旧 FooniController) to the runner or another referenced GameObject.");
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

        /// <summary>
        /// ランナー用 GameObject を取得・生成し、アダプタと ScriptMachine の設定を適用します。
        /// </summary>
        /// <param name="name">対象 GameObject 名。</param>
        /// <param name="macro">割り当てる ScriptGraphAsset。</param>
        /// <returns>準備済みの GameObject。</returns>
        private static GameObject EnsureRunner(string name, ScriptGraphAsset macro)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Create Fooni Runner");
            }

            EnsureInspectorHelp(go);

            var adapter = ConfigureActorPresenterAdapter(go);
            ConfigureScriptMachine(go, macro, adapter);

            EditorUtility.SetDirty(go);
            return go;
        }

        /// <summary>
        /// 指定 GameObject に <see cref="ActorPresenterAdapter"/>（旧称 FooniController）を確実にアタッチします。
        /// </summary>
        /// <param name="runner">対象の GameObject。</param>
        /// <returns>発見または新規追加したアダプタ。</returns>
        private static ActorPresenterAdapter ConfigureActorPresenterAdapter(GameObject runner)
        {
            if (runner == null)
            {
                return null;
            }

            var adapter = runner.GetComponent<ActorPresenterAdapter>();
            if (adapter != null)
            {
                return adapter;
            }

            adapter = Undo.AddComponent<ActorPresenterAdapter>(runner);
            EditorUtility.SetDirty(adapter);
            Debug.Log("[FUnity] Added ActorPresenterAdapter to runner: " + runner.name);
            return adapter;
        }

        /// <summary>
        /// ScriptMachine を設定し、ScriptGraphAsset の Variables および Object Variables にアダプタ参照を保存します。
        /// </summary>
        /// <param name="runner">対象 GameObject。</param>
        /// <param name="macro">割り当てる ScriptGraphAsset。</param>
        /// <param name="adapter">保存するアダプタ参照。</param>
        private static void ConfigureScriptMachine(GameObject runner, ScriptGraphAsset macro, ActorPresenterAdapter adapter)
        {
            if (runner == null)
            {
                return;
            }

            var machine = runner.GetComponent<ScriptMachine>();
            if (machine == null)
            {
                machine = Undo.AddComponent<ScriptMachine>(runner);
            }

            var machineDirty = false;

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

            if (macro != null && macro.graph is FlowGraph flowGraph)
            {
                var declarations = flowGraph.variables;
                if (declarations == null)
                {
                    declarations = new VariableDeclarations();
                    flowGraph.variables = declarations;
                }

                declarations.Set("adapter", adapter);
                EditorUtility.SetDirty(macro);
            }

            var objectVariables = Variables.Object(runner);
            if (objectVariables != null)
            {
                objectVariables.Set("adapter", adapter);
            }

            EditorUtility.SetDirty(runner);
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
