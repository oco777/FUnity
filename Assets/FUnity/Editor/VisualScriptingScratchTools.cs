#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;

namespace FUnity.EditorTools
{
    /// <summary>
    /// Visual Scripting を Scratch 本向けに整備するためのエディタユーティリティ集。
    /// Unit オプションの再構築と、代表的なマクロ雛形の自動生成を提供する。
    /// </summary>
    public static class VisualScriptingScratchTools
    {
        /// <summary>Scratch 学習用マクロを配置するフォルダ。</summary>
        private const string MacroFolder = "Assets/FUnity/VisualScripting/Macros";

        /// <summary>生成するマクロの一覧（ファイル名、タイトル、説明テキスト）。</summary>
        private static readonly (string fileName, string title, string instructions)[] ScratchMacros =
        {
            ("SayOnKey.asset", "Say On Key",
                "[概要]\nスペースキーで Actor/Say を呼び出し、吹き出しを表示します。\n" +
                "[手順]\n1. On Keyboard Input (Space) → Variables.Object(\"VSPresenterBridge\")\n" +
                "2. InvokeMember ノードで OnActorSay(message, seconds) を呼ぶ\n" +
                "3. message には好みの文字列、seconds には 2.0 を指定"),
            ("FloatUpDown.asset", "Float Up Down",
                "[概要]\nサイン波で上下移動し続けます。\n" +
                "[手順]\n1. Update イベントから Time.time を取得\n" +
                "2. Mathf.Sin(Time.time * 速度) * 振幅 → 変化量dy\n" +
                "3. VSPresenterBridge.OnActorMoveBy(0, dy) を呼び出す"),
            ("MoveWithArrow.asset", "Move With Arrow",
                "[概要]\n矢印キーの入力で Actor を移動させます。\n" +
                "[手順]\n1. Update イベントで Input.GetAxisRaw(\"Horizontal\"), (\"Vertical\") を取得\n" +
                "2. ベクトル(dx, dy) と Time.deltaTime を VSPresenterBridge.VS_Move に渡す"),
            ("WhenClickedMoveTo.asset", "When Clicked Move To",
                "[概要]\nクリック位置へキャラクターを移動させます。\n" +
                "[手順]\n1. On Pointer Click → グラフ入力 position(Vector2) を取得\n" +
                "2. VSPresenterBridge.OnActorSetPosition(position.x, position.y) を呼び出す"),
            ("SizePulse.asset", "Size Pulse",
                "[概要]\nスケールを一定周期で拡大・縮小します。\n" +
                "[手順]\n1. Update で Time.time を利用し、1 + Mathf.Sin(Time.time * 速度) * 0.2 を算出\n" +
                "2. VSPresenterBridge.OnActorSetScale(scale) を呼ぶ")
        };

        [MenuItem("FUnity/VS/Rebuild Unit Options")]
        /// <summary>
        /// Visual Scripting の Unit オプションデータベースを再生成する。エディタ上でノードが表示されない場合に利用する。
        /// </summary>
        public static void RebuildUnitOptions()
        {
            var typeNames = new[]
            {
                "Unity.VisualScripting.Editor.UnitOptionsUtility, Unity.VisualScripting.Core.Editor",
                "Unity.VisualScripting.Editor.UnitOptionsUtility, Unity.VisualScripting.Flow.Editor",
                "Unity.VisualScripting.Editor.UnitOptionsUtility, Unity.VisualScripting.Editor"
            };

            foreach (var typeName in typeNames)
            {
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                var method = type.GetMethod("Update", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool) }, null)
                             ?? type.GetMethod("Update", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    continue;
                }

                try
                {
                    if (method.GetParameters().Length == 1)
                    {
                        method.Invoke(null, new object[] { true });
                    }
                    else
                    {
                        method.Invoke(null, null);
                    }

                    Debug.Log("[FUnity] Visual Scripting unit options were rebuilt successfully.");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FUnity] Failed to rebuild unit options via {typeName}: {ex.Message}");
                }
            }

            Debug.LogWarning("[FUnity] Unit options utility type was not found. Check that Visual Scripting editor assemblies are imported.");
        }

        [MenuItem("FUnity/VS/Create Scratch Macros")]
        /// <summary>
        /// Scratch の定番レシピを再現するマクロ雛形を作成し、利用手順をコンソールに案内する。
        /// </summary>
        public static void CreateScratchMacros()
        {
            Directory.CreateDirectory(MacroFolder);

            foreach (var (fileName, title, instructions) in ScratchMacros)
            {
                var path = Path.Combine(MacroFolder, fileName);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<ScriptGraphAsset>();
                    asset.graph = new FlowGraph();
                    AssetDatabase.CreateAsset(asset, path);
                }

                if (asset.graph == null)
                {
                    asset.graph = new FlowGraph();
                }

                asset.graph.title = $"{title} (Inspector & Console Guide)";
                asset.graph.units.Clear();

                EditorUtility.SetDirty(asset);

                LogScratchMacroInstructions(path, title, instructions);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FUnity] Scratch macro templates were created/updated. 各マクロの使い方は Console ログおよびランナーの Inspector コメントを参照してください。");
        }

        /// <summary>
        /// マクロの生成経路と配線手順を Console ログに書き出す。
        /// </summary>
        /// <param name="assetPath">生成・更新したマクロアセットのパス。</param>
        /// <param name="title">マクロのタイトル。</param>
        /// <param name="instructions">配線に関する手順テキスト。</param>
        private static void LogScratchMacroInstructions(string assetPath, string title, string instructions)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(title))
            {
                Debug.LogWarning("[FUnity] Scratch macro log skipped because title or path was empty.");
                return;
            }

            var detail = string.IsNullOrEmpty(instructions)
                ? "（手順テキストが定義されていません）"
                : instructions;

            Debug.Log($"[FUnity] Macro created: {title}\nAsset Path: {assetPath}\n配線ヒント:\n{detail}\n※ ランナーの Inspector には FUnityInspectorComment で同じ案内を表示してください。");
        }
    }
}
#endif
