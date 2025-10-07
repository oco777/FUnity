using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class FUnitySceneFixer
{
    private const string MenuPath = "FUnity/Repair Sample Scene";
    private const string PanelResourcePath = "FUnityPanelSettings";
    private const string PanelAssetDirectory = "Assets/FUnity/Resources";
    private const string PanelAssetPath = PanelAssetDirectory + "/FUnityPanelSettings.asset";
    private const string PackageUxmlPath = "Packages/com.papacoder.funity/UXML/block.uxml";
    private const string PackageUssRelativePath = "../USS/block.uss";

    [MenuItem(MenuPath)]
    public static void Repair()
    {
        var go = GameObject.Find("FUnity UI");
        if (!go)
        {
            go = new GameObject("FUnity UI");
            Undo.RegisterCreatedObjectUndo(go, "Create FUnity UI");
        }

        var uiDoc = go.GetComponent<UIDocument>();
        if (!uiDoc)
        {
            uiDoc = Undo.AddComponent<UIDocument>(go);
        }

        var panel = Resources.Load<PanelSettings>(PanelResourcePath);
        if (!panel)
        {
            EnsureDirectoryExists(PanelAssetDirectory);
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panel, PanelAssetPath);
            AssetDatabase.SaveAssets();
        }
        uiDoc.panelSettings = panel;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PackageUxmlPath);
        if (!visualTree)
        {
            Debug.LogWarning($"FUnitySceneFixer: Could not load VisualTreeAsset at {PackageUxmlPath}.");
        }
        else
        {
            uiDoc.visualTreeAsset = visualTree;
            EnsureUssReference(PackageUxmlPath, PackageUssRelativePath);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("✅ FUnitySceneFixer: UIDocument と PanelSettings を設定しました");
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void EnsureUssReference(string uxmlPath, string relativeUssPath)
    {
        if (!File.Exists(uxmlPath))
        {
            return;
        }

        if (!AssetDatabase.IsOpenForEdit(uxmlPath))
        {
            Debug.LogWarning($"FUnitySceneFixer: '{uxmlPath}' is read-only and could not be edited.");
            return;
        }

        try
        {
            var contents = File.ReadAllText(uxmlPath);
            if (contents.Contains("block.uss"))
            {
                return;
            }

            var styleLine = $"    <ui:Style src=\"{relativeUssPath}\" />{Environment.NewLine}";
            var closingTag = "</ui:UXML>";
            if (contents.Contains(closingTag))
            {
                contents = contents.Replace(closingTag, styleLine + closingTag);
            }
            else
            {
                closingTag = "</UXML>";
                if (contents.Contains(closingTag))
                {
                    contents = contents.Replace(closingTag, styleLine + closingTag);
                }
                else
                {
                    contents += Environment.NewLine + styleLine;
                }
            }

            File.WriteAllText(uxmlPath, contents);
            AssetDatabase.ImportAsset(uxmlPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"FUnitySceneFixer: Failed to update '{uxmlPath}': {ex.Message}");
        }
    }
}
