#if UNITY_EDITOR
using System.IO;
using FUnity.Runtime.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.EditorTools
{
    /// <summary>
    /// プロジェクト名を入力し、FUnity 用のプロジェクトフォルダと ScriptableObject アセットを生成する EditorWindow。
    /// </summary>
    public sealed class FUnityProjectCreatorWindow : EditorWindow
    {
        private const string ProjectsRootFolder = "Assets/FUnity/Projects";

        /// <summary>ユーザー入力を受け付けるプロジェクト名フィールド。</summary>
        private TextField m_ProjectNameField;

        /// <summary>入力エラーを表示するラベル。赤字で警告を出す。</summary>
        private Label m_ErrorLabel;

        /// <summary>Unity メニューからウィンドウを開くエントリーポイント。</summary>
        [MenuItem("FUnity/Create/New Project...")]
        public static void Open()
        {
            var window = GetWindow<FUnityProjectCreatorWindow>();
            window.titleContent = new GUIContent("Create FUnity Project");
            window.minSize = new Vector2(360f, 140f);
            window.Show();
        }

        /// <summary>UI Toolkit を用いて入力欄とボタンを構築する。</summary>
        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            var title = new Label("FUnity プロジェクトの作成");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            title.style.marginBottom = 4;
            root.Add(title);

            var help = new Label("プロジェクト名を入力すると、フォルダと必要なアセットが自動生成されます。");
            help.style.whiteSpace = WhiteSpace.Normal;
            help.style.marginBottom = 8;
            root.Add(help);

            m_ProjectNameField = new TextField("プロジェクト名");
            m_ProjectNameField.value = "MyFUnityProject";
            m_ProjectNameField.style.marginBottom = 4;
            root.Add(m_ProjectNameField);

            m_ErrorLabel = new Label();
            m_ErrorLabel.style.color = new StyleColor(Color.red);
            m_ErrorLabel.style.marginBottom = 4;
            root.Add(m_ErrorLabel);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.FlexEnd;
            buttonRow.style.marginTop = 8;

            var cancelButton = new Button(() => Close())
            {
                text = "キャンセル"
            };

            var createButton = new Button(OnCreateProjectClicked)
            {
                text = "作成"
            };
            createButton.style.marginLeft = 4;

            buttonRow.Add(cancelButton);
            buttonRow.Add(createButton);
            root.Add(buttonRow);
        }

        /// <summary>入力されたプロジェクト名を検証し、問題なければアセット生成を実行する。</summary>
        private void OnCreateProjectClicked()
        {
            m_ErrorLabel.text = string.Empty;

            var projectName = m_ProjectNameField.value?.Trim();
            if (string.IsNullOrEmpty(projectName))
            {
                m_ErrorLabel.text = "プロジェクト名を入力してください。";
                return;
            }

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                if (projectName.Contains(invalidChar.ToString()))
                {
                    m_ErrorLabel.text = "プロジェクト名に使用できない文字が含まれています。";
                    return;
                }
            }

            CreateProjectAssets(projectName);
        }

        /// <summary>
        /// プロジェクト名に基づいてフォルダを作成し、Stage/Project の ScriptableObject を生成する。
        /// </summary>
        /// <param name="projectName">生成に使用するプロジェクト名。フォルダ名とアセット名に利用する。</param>
        private void CreateProjectAssets(string projectName)
        {
            if (!AssetDatabase.IsValidFolder("Assets/FUnity"))
            {
                AssetDatabase.CreateFolder("Assets", "FUnity");
            }

            if (!AssetDatabase.IsValidFolder(ProjectsRootFolder))
            {
                AssetDatabase.CreateFolder("Assets/FUnity", "Projects");
            }

            var projectFolderPath = $"{ProjectsRootFolder}/{projectName}";
            if (!AssetDatabase.IsValidFolder(projectFolderPath))
            {
                AssetDatabase.CreateFolder(ProjectsRootFolder, projectName);
            }

            var actorsFolderPath = $"{projectFolderPath}/Actors";
            if (!AssetDatabase.IsValidFolder(actorsFolderPath))
            {
                AssetDatabase.CreateFolder(projectFolderPath, "Actors");
            }

            var stageData = ScriptableObject.CreateInstance<FUnityStageData>();
            var stageDataPath = $"{projectFolderPath}/{projectName}_StageData.asset";
            AssetDatabase.CreateAsset(stageData, stageDataPath);

            var projectData = ScriptableObject.CreateInstance<FUnityProjectData>();
            projectData.InitializeForNewProject(projectName, stageData);
            var projectDataPath = $"{projectFolderPath}/{projectName}_ProjectData.asset";
            AssetDatabase.CreateAsset(projectData, projectDataPath);

            EditorUtility.SetDirty(stageData);
            EditorUtility.SetDirty(projectData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = projectData;

            EditorUtility.DisplayDialog(
                "FUnity Project",
                $"プロジェクト '{projectName}' を作成しました。\n\n" +
                $"{projectFolderPath} 配下にフォルダとアセットを生成しています。",
                "OK");

            Close();
        }
    }
}
#endif
