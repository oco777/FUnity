using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class FUnitySceneFixer
{
    [MenuItem("FUnity/Repair Sample Scene")]
    public static void Repair()
    {
        // シーンの有効確認
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogError("❌ シーンが開かれていません。FUnitySample.unity を開いてから実行してください。");
            return;
        }

        // 1) FUnity UI GameObject を取得または生成
        var go = GameObject.Find("FUnity UI");
        if (!go)
        {
            go = new GameObject("FUnity UI");
            Undo.RegisterCreatedObjectUndo(go, "Create FUnity UI");
        }

        // 2) UIDocument を取得または追加
        var uiDoc = go.GetComponent<UIDocument>();
        if (!uiDoc)
        {
            uiDoc = Undo.AddComponent<UIDocument>(go);
        }

        // 3) PanelSettings を取得または生成（Assets 側に作成＝書き込み可）
        var panel = Resources.Load<PanelSettings>("FUnityPanelSettings");
        if (!panel)
        {
            var dir = "Assets/FUnity/Resources";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            panel = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panel, Path.Combine(dir, "FUnityPanelSettings.asset"));
            AssetDatabase.SaveAssets();
            Debug.Log("🆕 FUnityPanelSettings.asset を生成しました");
        }
        uiDoc.panelSettings = panel;

        // 4) block.uxml を読み込み
        const string uxmlAssetPath = "Packages/com.papacoder.funity/UXML/block.uxml";
        var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlAssetPath);
        if (vta == null)
        {
            Debug.LogError("❌ block.uxml が見つかりません。パッケージ構成を確認してください。");
            return;
        }
        uiDoc.visualTreeAsset = vta;

        // 5) block.uxml に USS スタイル参照が無ければ追加
        if (File.Exists(uxmlAssetPath))
        {
            var contents = File.ReadAllText(uxmlAssetPath);
            if (!contents.Contains("USS/block.uss"))
            {
                const string insertion = "  <Style src=\"../USS/block.uss\"/>\n</UXML>";
                contents = contents.Replace("</UXML>", insertion);
                File.WriteAllText(uxmlAssetPath, contents);
                AssetDatabase.ImportAsset(uxmlAssetPath);
                Debug.Log("🎨 block.uxml に block.uss の参照を追加しました");
            }
        }

        // 保存処理
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ FUnitySceneFixer: UIDocument と PanelSettings を設定しました");
    }
}
