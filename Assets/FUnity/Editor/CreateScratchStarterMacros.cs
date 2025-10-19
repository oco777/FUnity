#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits;

namespace FUnity.Editor.VisualScripting
{
    /// <summary>
    /// Scratch 風の移動サンプルグラフをエディタメニューから生成する補助クラスです。
    /// </summary>
    public static class CreateScratchStarterMacros
    {
        /// <summary>作成先ディレクトリのパスです。</summary>
        private const string MacroDirectory = "Assets/FUnity/VisualScripting/Macros";

        /// <summary>矢印キー移動マクロの保存先パスです。</summary>
        private const string MoveMacroPath = MacroDirectory + "/Scratch_Move_ArrowKeys.asset";

        /// <summary>
        /// エディタメニューから Scratch 風の移動マクロを生成します。
        /// </summary>
        [MenuItem("FUnity/VS/Create Macro/Scratch: Arrow Move")]
        public static void CreateArrowMoveMacro()
        {
            Directory.CreateDirectory(MacroDirectory);

            var macro = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            macro.graph = new FlowGraph();
            macro.name = "Scratch_Move_ArrowKeys";

            var onStart = new Start();
            var setDirRight = new PointDirectionUnit();
            var setDirUp = new PointDirectionUnit();
            var setDirLeft = new PointDirectionUnit();
            var setDirDown = new PointDirectionUnit();
            var move = new MoveStepsUnit();

            macro.graph.units.Add(onStart);
            macro.graph.units.Add(setDirRight);
            macro.graph.units.Add(setDirUp);
            macro.graph.units.Add(setDirLeft);
            macro.graph.units.Add(setDirDown);
            macro.graph.units.Add(move);

            onStart.position = new Vector2(0f, 0f);
            setDirRight.position = new Vector2(300f, -150f);
            setDirUp.position = new Vector2(300f, 0f);
            setDirLeft.position = new Vector2(300f, 150f);
            setDirDown.position = new Vector2(300f, 300f);
            move.position = new Vector2(600f, 0f);

            if (File.Exists(MoveMacroPath))
            {
                AssetDatabase.DeleteAsset(MoveMacroPath);
            }

            AssetDatabase.CreateAsset(macro, MoveMacroPath);
            EditorUtility.SetDirty(macro);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FUnity] Scratch macro created: " + MoveMacroPath);
        }
    }
}
#endif
