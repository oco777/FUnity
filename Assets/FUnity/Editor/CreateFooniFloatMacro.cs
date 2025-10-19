#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;

/// <summary>
/// FooniController の浮遊設定マクロをエディター上で生成するメニューを提供します。
/// Visual Scripting グラフ内に必要なユニットを並べて接続します。
/// </summary>
public static class CreateFooniFloatMacro
{
    /// <summary>マクロを保存するフォルダーのパスです。</summary>
    private const string kAssetDirectory = "Assets/FUnity/VisualScripting/Macros";

    /// <summary>生成する ScriptGraphAsset の保存パスです。</summary>
    private const string kAssetPath = kAssetDirectory + "/Fooni_FloatSetup.asset";

    /// <summary>FooniController の完全修飾型名です。</summary>
    private const string kControllerTypeName = "FUnity.Runtime.Integrations.VisualScripting.FooniController";

    /// <summary>
    /// FUnity → VS → Create Macro → Fooni Float Setup を実行してマクロを生成します。
    /// 既存のアセットがある場合は上書きし、生成結果をログに出力します。
    /// </summary>
    [MenuItem("FUnity/VS/Create Macro/Fooni Float Setup")]
    public static void CreateMacro()
    {
        try
        {
            EnsureAssetDirectory();

            var controllerType = ResolveControllerType();
            if (controllerType == null)
            {
                Debug.LogError("[FUnity] FooniController 型を解決できません: " + kControllerTypeName);
                return;
            }

            var macro = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            var graph = new FlowGraph();
            macro.graph = graph;
            macro.title = "Fooni Float Setup";

            var onStart = new Start();
            var getComponent = new GetComponent
            {
                type = controllerType
            };
            var invokeEnable = CreateInvokeMember(controllerType, "EnableFloat", typeof(bool));
            var literalTrue = new Literal<bool>
            {
                value = true
            };
            var invokeAmplitude = CreateInvokeMember(controllerType, "SetFloatAmplitude", typeof(float));
            var literalAmplitude = new Literal<float>
            {
                value = 10f
            };
            var invokePeriod = CreateInvokeMember(controllerType, "SetFloatPeriod", typeof(float));
            var literalPeriod = new Literal<float>
            {
                value = 3f
            };

            graph.units.Add(onStart);
            graph.units.Add(getComponent);
            graph.units.Add(invokeEnable);
            graph.units.Add(literalTrue);
            graph.units.Add(invokeAmplitude);
            graph.units.Add(literalAmplitude);
            graph.units.Add(invokePeriod);
            graph.units.Add(literalPeriod);

            onStart.position = new Vector2(0f, 0f);
            getComponent.position = new Vector2(300f, 0f);
            invokeEnable.position = new Vector2(600f, 0f);
            literalTrue.position = new Vector2(600f, -150f);
            invokeAmplitude.position = new Vector2(900f, 0f);
            literalAmplitude.position = new Vector2(900f, -150f);
            invokePeriod.position = new Vector2(1200f, 0f);
            literalPeriod.position = new Vector2(1200f, -150f);

            graph.controlConnections.Add(new ControlConnection(onStart.trigger, invokeEnable.controlInput));
            graph.controlConnections.Add(new ControlConnection(invokeEnable.controlOutput, invokeAmplitude.controlInput));
            graph.controlConnections.Add(new ControlConnection(invokeAmplitude.controlOutput, invokePeriod.controlInput));

            graph.valueConnections.Add(new ValueConnection(getComponent.result, invokeEnable.target));
            graph.valueConnections.Add(new ValueConnection(getComponent.result, invokeAmplitude.target));
            graph.valueConnections.Add(new ValueConnection(getComponent.result, invokePeriod.target));
            graph.valueConnections.Add(new ValueConnection(literalTrue.output, invokeEnable.arguments[0]));
            graph.valueConnections.Add(new ValueConnection(literalAmplitude.output, invokeAmplitude.arguments[0]));
            graph.valueConnections.Add(new ValueConnection(literalPeriod.output, invokePeriod.arguments[0]));

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
    /// マクロを保存するフォルダーを作成し、存在しない場合は生成します。
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
    /// マクロ保存先に既存アセットがある場合は削除してから新規作成できるようにします。
    /// </summary>
    private static void OverwriteExistingAsset()
    {
        if (!File.Exists(kAssetPath))
        {
            return;
        }

        AssetDatabase.DeleteAsset(kAssetPath);
    }

    /// <summary>
    /// FooniController 型をリフレクションで取得します。Assembly-CSharp を優先します。
    /// </summary>
    /// <returns>解決できた型。失敗時は null。</returns>
    private static Type ResolveControllerType()
    {
        var resolved = Type.GetType(kControllerTypeName + ", Assembly-CSharp", false)
                       ?? Type.GetType(kControllerTypeName, false);
        return resolved;
    }

    /// <summary>
    /// 指定された型のインスタンスメソッドを呼び出す InvokeMember ユニットを生成します。
    /// </summary>
    /// <param name="controllerType">ターゲット型。</param>
    /// <param name="methodName">メソッド名。</param>
    /// <param name="parameterType">唯一の引数の型。</param>
    /// <returns>構成済みの InvokeMember ユニット。</returns>
    private static InvokeMember CreateInvokeMember(Type controllerType, string methodName, Type parameterType)
    {
        var invokeMember = new InvokeMember
        {
            member = new Member
            {
                targetType = controllerType,
                name = methodName,
                parameterTypes = new[] { parameterType },
                isStatic = false
            }
        };

        return invokeMember;
    }
}
#endif
