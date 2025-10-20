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

            if (macro != null)
            {
                var declarations = EnsureGraphVariables(macro);
                if (declarations != null)
                {
                    declarations.Set("adapter", adapter);
                    EditorUtility.SetDirty(macro);
                }
            }

            var objectVariables = Variables.Object(runner);
            if (objectVariables != null)
            {
                objectVariables.Set("adapter", adapter);
            }

            EditorUtility.SetDirty(runner);
        }

        /// <summary>
        /// ScriptGraphAsset 内に保持される FlowGraph の Variables を安全に初期化します。
        /// Unity のバージョンによっては <see cref="FlowGraph.variables"/> が読み取り専用のため、
        /// SerializedObject 経由で managed reference を生成します。
        /// </summary>
        /// <param name="macro">対象の ScriptGraphAsset。</param>
        /// <returns>利用可能な <see cref="VariableDeclarations"/>。生成に失敗した場合は null。</returns>
        private static VariableDeclarations EnsureGraphVariables(ScriptGraphAsset macro)
        {
            if (macro == null)
            {
                Debug.LogWarning("[FUnity] ScriptGraphAsset が null のため Variables を初期化できません。");
                return null;
            }

            if (!(macro.graph is FlowGraph flowGraph))
            {
                flowGraph = new FlowGraph();
                macro.graph = flowGraph;
                EditorUtility.SetDirty(macro);
            }

            var existing = flowGraph.variables;
            if (existing != null)
            {
                return existing;
            }

            using (var serializedMacro = new SerializedObject(macro))
            {
                serializedMacro.Update();

                var graphProperty = serializedMacro.FindProperty("graph");
                if (graphProperty == null)
                {
                    Debug.LogWarning("[FUnity] ScriptGraphAsset.graph プロパティが見つからず、Variables を生成できませんでした。");
                    return null;
                }

                var variablesProperty = graphProperty.FindPropertyRelative("variables");
                if (variablesProperty == null)
                {
                    Debug.LogWarning("[FUnity] FlowGraph.variables プロパティが見つからず、Variables を生成できませんでした。");
                    return null;
                }

                var newDeclarations = new VariableDeclarations();
                variablesProperty.managedReferenceValue = newDeclarations;
                serializedMacro.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(macro);

                return flowGraph.variables ?? newDeclarations;
            }

            return null;
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
