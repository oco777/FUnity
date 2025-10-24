#if UNITY_EDITOR
// Updated: 2025-03-10
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Core;
using FUnity.Runtime.Core;

namespace FUnity.EditorTools
{
    /// <summary>
    /// FUnity の初期設定に必要なアセットを検証し、欠落時は自動生成を補助するエディタウィンドウ。
    /// </summary>
    public sealed class FUnitySetupValidatorWindow : EditorWindow
    {
        /// <summary>検出した問題の一覧。</summary>
        private readonly List<string> m_Issues = new List<string>();

        /// <summary>検証結果を表示するラベル。</summary>
        private Label m_ResultLabel;

        /// <summary>問題一覧を表示するスクロールビュー。</summary>
        private ScrollView m_IssueList;

        /// <summary>Resources 下に生成する既定フォルダー。</summary>
        private const string GeneratedResourcesFolder = "Assets/FUnityGenerated/Resources";

        /// <summary>メニューからウィンドウを開く。</summary>
        [MenuItem("Tools/FUnity/Validate Setup", priority = 0)]
        private static void OpenWindow()
        {
            var window = GetWindow<FUnitySetupValidatorWindow>();
            window.titleContent = new GUIContent("FUnity Setup Validator");
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        /// <summary>
        /// ウィンドウ有効化時に UI を構築し、即座に検証を実行する。
        /// </summary>
        private void OnEnable()
        {
            BuildLayout();
            Validate();
        }

        /// <summary>
        /// UI Toolkit を用いてレイアウトを生成する。
        /// </summary>
        private void BuildLayout()
        {
            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 12f;
            root.style.paddingRight = 12f;
            root.style.paddingTop = 12f;
            root.style.paddingBottom = 12f;
            root.style.flexGrow = 1f;

            root.Clear();

            var header = new Label("FUnity 初期設定チェック");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 16f;
            header.style.marginBottom = 6f;
            root.Add(header);

            m_ResultLabel = new Label("検証は未実行です。");
            m_ResultLabel.style.marginBottom = 6f;
            root.Add(m_ResultLabel);

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginBottom = 8f;

            var refreshButton = new Button(Validate)
            {
                text = "再チェック"
            };
            refreshButton.style.marginRight = 6f;
            buttonRow.Add(refreshButton);

            var createButton = new Button(CreateDefaultAssets)
            {
                text = "Create Default Assets"
            };
            buttonRow.Add(createButton);

            root.Add(buttonRow);

            m_IssueList = new ScrollView();
            m_IssueList.style.flexGrow = 1f;
            root.Add(m_IssueList);
        }

        /// <summary>
        /// 必須アセットの存在を確認し、結果を UI に反映する。
        /// </summary>
        private void Validate()
        {
            m_Issues.Clear();

            CheckResourceExists<VisualTreeAsset>("UI/ActorElement", "UI/ActorElement.uxml が見つかりません。サンプルをインポートするか、Resources/UI/ActorElement.uxml を配置してください。");
            CheckResourceExists<StyleSheet>("UI/ActorElement", "UI/ActorElement.uss が見つかりません。フォールバックが利用されますが、スタイルが欠落します。");
            CheckResourceExists<VisualTreeAsset>("UI/FooniElement", "UI/FooniElement.uxml が見つかりません。サンプルフォールバックを利用できません。");
            CheckResourceExists<StyleSheet>("UI/FooniElement", "UI/FooniElement.uss が見つかりません。フォールバック時のスタイルが欠落します。");

            var project = Resources.Load<FUnityProjectData>("FUnityProjectData");
            if (project == null)
            {
                m_Issues.Add("Resources/FUnityProjectData.asset が見つかりません。'Create Default Assets' で自動生成できます。");
            }
            else
            {
                ValidateProjectData(project);
            }

            UpdateIssueList();
        }

        /// <summary>
        /// 指定の Resources パスにアセットが存在するかを確認する。
        /// </summary>
        /// <typeparam name="T">探索するアセット型。</typeparam>
        /// <param name="resourcePath">Resources.Load 用のパス。</param>
        /// <param name="issueMessage">見つからない場合に追加するメッセージ。</param>
        private void CheckResourceExists<T>(string resourcePath, string issueMessage) where T : Object
        {
            if (Resources.Load<T>(resourcePath) != null)
            {
                return;
            }

            m_Issues.Add(issueMessage);
        }

        /// <summary>
        /// プロジェクト設定に紐付く俳優やリソースの妥当性を検証する。
        /// </summary>
        /// <param name="project">検証対象の設定。</param>
        private void ValidateProjectData(FUnityProjectData project)
        {
            if (project.Actors == null || project.Actors.Count == 0)
            {
                m_Issues.Add("FUnityProjectData.Actors に俳優が登録されていません。");
                return;
            }

            for (var i = 0; i < project.Actors.Count; i++)
            {
                var actor = project.Actors[i];
                if (actor == null)
                {
                    m_Issues.Add($"Actors[{i}] が null です。ScriptableObject を割り当ててください。");
                    continue;
                }

                if (actor.Portrait == null)
                {
                    m_Issues.Add($"俳優 '{actor.DisplayName}' の Portrait が未設定です。Resources/Characters にテクスチャを配置してください。");
                }
            }
        }

        /// <summary>
        /// UI 上の問題一覧を更新する。
        /// </summary>
        private void UpdateIssueList()
        {
            m_IssueList.Clear();

            if (m_Issues.Count == 0)
            {
                m_ResultLabel.text = "すべての必須アセットが確認できました。";
                m_IssueList.Add(new HelpBox("セットアップに問題は見つかりませんでした。", HelpBoxMessageType.Info));
                return;
            }

            m_ResultLabel.text = $"検出した問題: {m_Issues.Count} 件";
            foreach (var issue in m_Issues)
            {
                m_IssueList.Add(new HelpBox(issue, HelpBoxMessageType.Error));
            }
        }

        /// <summary>
        /// 最低限の Resources アセットを自動生成し、検証を再実行する。
        /// </summary>
        private void CreateDefaultAssets()
        {
            EnsureGeneratedFolders();

            var projectPath = Path.Combine(GeneratedResourcesFolder, "FUnityProjectData.asset").Replace(Path.DirectorySeparatorChar, '/');
            var actorPath = Path.Combine(GeneratedResourcesFolder, "DefaultActor.asset").Replace(Path.DirectorySeparatorChar, '/');

            var project = AssetDatabase.LoadAssetAtPath<FUnityProjectData>(projectPath);
            if (project == null)
            {
                project = ScriptableObject.CreateInstance<FUnityProjectData>();
                AssetDatabase.CreateAsset(project, projectPath);
            }

            var actor = AssetDatabase.LoadAssetAtPath<FUnityActorData>(actorPath);
            if (actor == null)
            {
                actor = ScriptableObject.CreateInstance<FUnityActorData>();
                AssetDatabase.CreateAsset(actor, actorPath);
            }

            if (!project.Actors.Contains(actor))
            {
                project.Actors.Add(actor);
                EditorUtility.SetDirty(project);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            FUnityLog.LogInfo("デフォルトの FUnity アセットを生成しました。");
            Validate();
        }

        /// <summary>
        /// 生成用フォルダーが存在しない場合に作成する。
        /// </summary>
        private void EnsureGeneratedFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FUnityGenerated"))
            {
                AssetDatabase.CreateFolder("Assets", "FUnityGenerated");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets/FUnityGenerated", "Resources");
            }
        }
    }
}
#endif
