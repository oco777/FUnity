#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Core;
using ObjectField = UnityEditor.UIElements.ObjectField;

namespace FUnity.EditorTools
{
    /// <summary>
    /// シーン内の <see cref="FUnityManager"/> が参照する <see cref="FUnityProjectData"/> を切り替える EditorWindow。
    /// プロジェクトランチャーとして ProjectData 一覧を提示し、選択したプロジェクトに差し替える。
    /// </summary>
    public sealed class FUnityProjectLauncherWindow : EditorWindow
    {
        /// <summary>ProjectData を探索するルートフォルダー。Assets 全体を対象にする。</summary>
        private const string AssetsFolderPath = "Assets";

        /// <summary>UI Toolkit のルート要素。</summary>
        private VisualElement m_Root;

        /// <summary>ProjectData を並べる ListView。</summary>
        private ListView m_ListView;

        /// <summary>検索結果や実行結果を表示するステータスラベル。</summary>
        private Label m_StatusLabel;

        /// <summary>検索して保持する ProjectData の一覧。</summary>
        private List<FUnityProjectData> m_Projects;

        /// <summary>
        /// メニューからウィンドウを開き、タイトルを設定します。
        /// </summary>
        [MenuItem("FUnity/Authoring/Open Project Launcher.")]
        public static void Open()
        {
            var window = GetWindow<FUnityProjectLauncherWindow>();
            window.titleContent = new GUIContent("FUnity Project Launcher");
            window.Show();
        }

        /// <summary>
        /// UI 要素を構築し、ProjectData 一覧を読み込みます。
        /// </summary>
        public void CreateGUI()
        {
            m_Root = rootVisualElement;
            m_Root.style.paddingLeft = 8;
            m_Root.style.paddingRight = 8;
            m_Root.style.paddingTop = 8;
            m_Root.style.paddingBottom = 8;

            var title = new Label("FUnity Projects");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            title.style.marginBottom = 4;
            m_Root.Add(title);

            var help = new Label("プロジェクトを選択すると、現在のシーンの FUnityManager が参照する ProjectData が切り替わります。");
            help.style.whiteSpace = WhiteSpace.Normal;
            help.style.marginBottom = 8;
            m_Root.Add(help);

            m_StatusLabel = new Label();
            m_StatusLabel.style.marginBottom = 4;
            m_Root.Add(m_StatusLabel);

            ReloadProjects();
            BuildListView();
        }

        /// <summary>
        /// Assets フォルダー全体から <see cref="FUnityProjectData"/> を検索し、一覧に格納します。
        /// </summary>
        private void ReloadProjects()
        {
            m_Projects = new List<FUnityProjectData>();

            var guids = AssetDatabase.FindAssets("t:FUnityProjectData", new[] { AssetsFolderPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var projectData = AssetDatabase.LoadAssetAtPath<FUnityProjectData>(path);
                if (projectData != null)
                {
                    m_Projects.Add(projectData);
                }
            }

            if (m_StatusLabel != null)
            {
                if (m_Projects.Count == 0)
                {
                    m_StatusLabel.text = "Assets フォルダに FUnityProjectData が見つかりませんでした。";
                }
                else
                {
                    m_StatusLabel.text = $"{m_Projects.Count} 個の FUnityProjectData が見つかりました。";
                }
            }
        }

        /// <summary>
        /// ProjectData 一覧を表示する ListView を生成し、各行に切り替えボタンを配置します。
        /// </summary>
        private void BuildListView()
        {
            if (m_ListView != null)
            {
                m_Root.Remove(m_ListView);
            }

            m_ListView = new ListView
            {
                itemsSource = m_Projects,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };

            m_ListView.makeItem = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Column;

                var header = new Label();
                header.style.unityFontStyleAndWeight = FontStyle.Bold;

                var pathLabel = new Label();
                pathLabel.style.fontSize = 10;
                pathLabel.style.color = new StyleColor(Color.gray);

                var objectField = new ObjectField
                {
                    objectType = typeof(FUnityProjectData)
                };
                objectField.style.marginTop = 2;

                var button = new Button
                {
                    text = "このプロジェクトを使用"
                };
                button.style.marginTop = 2;

                row.Add(header);
                row.Add(pathLabel);
                row.Add(objectField);
                row.Add(button);

                return row;
            };

            m_ListView.bindItem = (element, index) =>
            {
                if (index < 0 || index >= m_Projects.Count)
                {
                    return;
                }

                var projectData = m_Projects[index];
                var header = (Label)element.ElementAt(0);
                var pathLabel = (Label)element.ElementAt(1);
                var objectField = (ObjectField)element.ElementAt(2);
                var button = (Button)element.ElementAt(3);

                header.text = projectData != null ? GetDisplayName(projectData) : "(null)";

                var path = AssetDatabase.GetAssetPath(projectData);
                pathLabel.text = string.IsNullOrEmpty(path) ? "(path not found)" : path;

                objectField.value = projectData;
                objectField.SetEnabled(false);

                button.clickable = null;
                button.clickable = new Clickable(() => SwitchProject(projectData));
            };

            m_Root.Add(m_ListView);
        }

        /// <summary>
        /// ProjectData の表示名を返します。ProjectName が空の場合はアセット名を使用します。
        /// </summary>
        /// <param name="project">表示対象の ProjectData。</param>
        /// <returns>UI に表示する名称。</returns>
        private static string GetDisplayName(FUnityProjectData project)
        {
            return !string.IsNullOrEmpty(project.ProjectName) ? project.ProjectName : project.name;
        }

        /// <summary>
        /// ListView のボタン押下時に、現在のシーンに存在する FUnityManager の ProjectData を切り替えます。
        /// </summary>
        /// <param name="selectedProject">選択された ProjectData。</param>
        private void SwitchProject(FUnityProjectData selectedProject)
        {
            if (selectedProject == null)
            {
                EditorUtility.DisplayDialog(
                    "FUnity Project Launcher",
                    "選択された FUnityProjectData が null です。",
                    "OK");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "FUnity Project Launcher",
                    "再生中はプロジェクトを切り替えできません。再生を停止してから再度お試しください。",
                    "OK");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                EditorUtility.DisplayDialog(
                    "FUnity Project Launcher",
                    "有効なシーンが開かれていません。",
                    "OK");
                return;
            }

            var managers = UnityEngine.Object.FindObjectsOfType<FUnityManager>();
            if (managers == null || managers.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "FUnity Project Launcher",
                    "現在のシーンに FUnityManager が見つかりません。",
                    "OK");
                return;
            }

            if (managers.Length > 1)
            {
                EditorUtility.DisplayDialog(
                    "FUnity Project Launcher",
                    "現在のシーンに複数の FUnityManager が存在します。1 つに絞ってからやり直してください。",
                    "OK");
                return;
            }

            var manager = managers[0];
            Undo.RecordObject(manager, "Change FUnity Project");
            manager.Editor_SetProjectData(selectedProject);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(activeScene);

            m_StatusLabel.text = $"現在のプロジェクトを '{selectedProject.name}' に切り替えました。";
        }
    }
}
#endif
