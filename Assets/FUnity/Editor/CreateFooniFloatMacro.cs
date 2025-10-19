#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting.Units;

/// <summary>
/// FooniController の浮遊設定を行う Visual Scripting マクロをエディター上で生成するメニューを提供します。
/// カスタム Unit を直列に配置し、制御線と値を接続した状態で ScriptGraphAsset を保存します。
/// </summary>
public static class CreateFooniFloatMacro
{
    /// <summary>マクロを保存するフォルダーのパスです。</summary>
    private const string kAssetDirectory = "Assets/FUnity/VisualScripting/Macros";

    /// <summary>生成する ScriptGraphAsset の保存パスです。</summary>
    private const string kAssetPath = kAssetDirectory + "/Fooni_FloatSetup.asset";

    /// <summary>
    /// FUnity → VS → Create Macro → Fooni Float Setup を実行してマクロを生成します。
    /// 既存のアセットがある場合は削除し、新しく生成した Graph を保存します。
    /// </summary>
    [MenuItem("FUnity/VS/Create Macro/Fooni Float Setup")]
    public static void CreateMacro()
    {
        try
        {
            EnsureAssetDirectory();

            var macro = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            var graph = new FlowGraph();
            macro.graph = graph;
            macro.name = "Fooni_FloatSetup";

            var onStart = new Start();
            var enableUnit = new Fooni_EnableFloatUnit();
            var amplitudeUnit = new Fooni_SetFloatAmplitudeUnit();
            var periodUnit = new Fooni_SetFloatPeriodUnit();

            graph.units.Add(onStart);
            graph.units.Add(enableUnit);
            graph.units.Add(amplitudeUnit);
            graph.units.Add(periodUnit);

            onStart.position = new Vector2(0f, 0f);
            enableUnit.position = new Vector2(300f, 0f);
            amplitudeUnit.position = new Vector2(600f, 0f);
            periodUnit.position = new Vector2(900f, 0f);

            graph.controlConnections.Add(new ControlConnection(onStart.trigger, enableUnit.Enter));
            graph.controlConnections.Add(new ControlConnection(enableUnit.Exit, amplitudeUnit.Enter));
            graph.controlConnections.Add(new ControlConnection(amplitudeUnit.Exit, periodUnit.Enter));

            OverwriteExistingAsset();

            AssetDatabase.CreateAsset(macro, kAssetPath);
            EditorUtility.SetDirty(macro);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FUnity] Macro created: " + kAssetPath);
        }
        catch (Exception exception)
        {
            Debug.LogError("[FUnity] マクロ生成に失敗しました: " + exception);
        }
    }

    /// <summary>
    /// マクロを保存するフォルダーを作成し、存在しない場合は新規作成します。
    /// </summary>
    private static void EnsureAssetDirectory()
    {
        if (AssetDatabase.IsValidFolder(kAssetDirectory))
        {
            return;
        }

        Directory.CreateDirectory(kAssetDirectory);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 既存のマクロアセットが存在する場合は削除して上書き保存できるようにします。
    /// </summary>
    private static void OverwriteExistingAsset()
    {
        if (!File.Exists(kAssetPath))
        {
            return;
        }

        AssetDatabase.DeleteAsset(kAssetPath);
    }
}
#endif
