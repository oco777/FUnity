#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using FUnity.Runtime.Core;
using Unity.VisualScripting;

namespace FUnity.EditorTools
{
    // なぜ: FUnity用Actor UIテンプレを即時生成し著者作業を簡略化したい。何を: UXML/USSとActorData/ScriptGraphを統一フォルダへ生成・リンクする。
    // どうする: エディタウィンドウからテンプレ設定を受け取りファイル生成・リンク処理を一括実行する。
    public class GenerateActorUITemplateWindow : EditorWindow
    {
        // なぜ: Actorテンプレートを識別し再利用したい。何を: 生成するUXML/USS/ActorDataのベース名。どうする: 入力値でファイル名を決定。
        private string m_TemplateName = "MyActor";
        // なぜ: 生成物をプロジェクトに配備する必要がある。何を: UXML/USS/ActorData/ScriptGraphの出力フォルダ。どうする: 指定パスに書き出す。
        private string m_Folder = string.Empty;
        // なぜ: ランタイムで画像差し替え箇所を特定したい。何を: VisualElement.Q()で探すportrait用スロット名。
        // どうする: UXML内のname属性として埋め込み、差し込み先を決定する。
        private string m_PortraitSlot = "portrait";
        // なぜ: UIサイズをテンプレで統制したい。何を: 生成UXMLに反映する幅。どうする: USSにwidthをpx指定。
        private int m_Width = 128;
        // なぜ: UI高さも合わせたい。何を: 生成UXMLの高さ。どうする: USSのheightにpx指定。
        private int m_Height = 128;
        // なぜ: ブランドカラーをテンプレで共有したい。何を: 背景に使うPrimaryColor。どうする: USSにrgbaで埋め込む。
        private Color m_PrimaryColor = new Color(0f, 0f, 0f, 0f);
        // なぜ: 選択中情報からテンプレ名称を補完したい。何を: 直近サジェスト名を保持。どうする: Selectionからの提案があれば初期値に使う。
        private string m_LastSuggestedName = string.Empty;
        // なぜ: サジェストしたフォルダ名とユーザー入力を区別したい。何を: 直近サジェストを保持する変数。どうする: Template変更時の自動更新に利用。
        private string m_LastSuggestedFolder = string.Empty;
        // なぜ: 初回ウィンドウ表示で一度だけ初期化したい。何を: 初期化済みフラグ。どうする: OnEnableで設定を行うかどうか判定する。
        private bool m_IsInitialized;

        [MenuItem("FUnity/Create/FUnityActorData")]
        public static void Open()
        {
            // なぜ: 著者に最小テンプレ生成を提供したい。何を: FUnity向けUXML/USSを作る専用ウィンドウ。どうする: EditorWindowを開く。
            var window = GetWindow<GenerateActorUITemplateWindow>("Generate Actor UI");
            window.minSize = new Vector2(440, 360);
            window.Show();
        }

        private void OnEnable()
        {
            // なぜ: ウィンドウ生成時に初期フォルダ候補を整えたい。何を: 選択内容から名前とパスをサジェストする。どうする: 一度だけ初期化し、再表示時の入力を保持する。
            if (m_IsInitialized)
            {
                return;
            }

            m_IsInitialized = true;
            ApplySelectionHints();
            SuggestOutputFolder(true);
        }

        private void OnGUI()
        {
            // なぜ: テンプレ構成要素を一括設定したい。何を: 名前/出力先/portraitスロット/サイズ/色の入力群。
            // どうする: EditorGUILayoutでフォームを描画し、生成ボタンでUXML/USS/ActorData/ScriptGraph出力とリンクを実行する。
            EditorGUILayout.LabelField("FUnity Actor UI Template", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            var newTemplate = EditorGUILayout.TextField("Template Name", m_TemplateName);
            if (EditorGUI.EndChangeCheck())
            {
                m_TemplateName = newTemplate;
                SuggestOutputFolder(false);
            }

            EditorGUI.BeginChangeCheck();
            var folderInput = EditorGUILayout.TextField("Output Folder", m_Folder);
            if (EditorGUI.EndChangeCheck())
            {
                m_Folder = NormalizeFolderPath(folderInput);
            }

            m_PortraitSlot = EditorGUILayout.TextField("Portrait Slot Name", m_PortraitSlot);
            m_Width = EditorGUILayout.IntField("Width (px)", m_Width);
            m_Height = EditorGUILayout.IntField("Height (px)", m_Height);
            m_PrimaryColor = EditorGUILayout.ColorField("Primary Color", m_PrimaryColor);
            var actorName = GetActorName();
            var hasValidActorName = !string.IsNullOrEmpty(actorName);
            var activeProjectActorsFolder = GetActiveProjectActorsFolder(false);
            var baseFolderPreview = string.IsNullOrEmpty(m_Folder) ? activeProjectActorsFolder : m_Folder;
            if (string.IsNullOrEmpty(baseFolderPreview))
            {
                baseFolderPreview = "Assets";
            }
            var folderValid = IsFolderInAssets(baseFolderPreview);

            if (!hasValidActorName)
            {
                EditorGUILayout.HelpBox("Template Name を入力してください（ファイル名に使用します）。", MessageType.Warning);
            }

            if (!folderValid)
            {
                EditorGUILayout.HelpBox("Output Folder は Assets 配下に指定してください。", MessageType.Error);
            }
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!hasValidActorName || !folderValid))
            {
                if (GUILayout.Button("Create UXML/USS and Link"))
                {
                    CreateFilesAndLink();
                }
            }
        }

        /// <summary>
        /// <list type="bullet">
        /// <item><description>Actorごとの出力フォルダを用意する。</description></item>
        /// <item><description>UXML/USSを生成し保存する。</description></item>
        /// <item><description>新規 ActorData と ScriptGraph を生成する。</description></item>
        /// <item><description>新規 ActorData に UXML/USS/ScriptGraph をリンクする。</description></item>
        /// <item><description>アセット保存と選択を更新する。</description></item>
        /// </list>
        /// </summary>
        private void CreateFilesAndLink()
        {
            var actorName = GetActorName();
            if (string.IsNullOrEmpty(actorName))
            {
                EditorUtility.DisplayDialog("Error", "Template Name is required.", "OK");
                return;
            }

            if (!TryGetActiveFUnityProject(out var projectData, true))
            {
                return;
            }

            if (!TryGetActorsFolderPath(projectData, true, out var actorsFolderPath))
            {
                return;
            }

            var baseFolder = NormalizeFolderPath(string.IsNullOrEmpty(m_Folder) ? actorsFolderPath : m_Folder);

            if (!IsFolderInAssets(baseFolder))
            {
                EditorUtility.DisplayDialog("Error", "Output Folder must be inside the Assets directory.", "OK");
                return;
            }

            if (!IsFolderInsideActorsRoot(baseFolder, actorsFolderPath))
            {
                EditorUtility.DisplayDialog("Error", "Output Folder must be inside the current project's Actors folder.", "OK");
                return;
            }

            var actorFolder = ResolveActorFolder(baseFolder, actorName, actorsFolderPath);
            if (!IsFolderInAssets(actorFolder))
            {
                EditorUtility.DisplayDialog("Error", "Actor folder must be inside the Assets directory.", "OK");
                return;
            }

            Directory.CreateDirectory(actorFolder);
            var uxmlPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(actorFolder, $"{actorName}.uxml"));
            var ussPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(actorFolder, $"{actorName}.uss"));

            var uss = $@"/* {actorName}.uss - generated by FUnity */
.actor-root {{
    width: {m_Width}px;
    height: {m_Height}px;
    border-radius: 12px;
    background-color: rgba({(int)(m_PrimaryColor.r * 255)}, {(int)(m_PrimaryColor.g * 255)}, {(int)(m_PrimaryColor.b * 255)}, {m_PrimaryColor.a});
    justify-content: center;
    align-items: center;
}}
.actor-portrait {{
    width: 100%;
    height: 100%;
    background-size: contain;
    background-position: center;
    background-repeat: no-repeat;
}}";

            var uxml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ui:UXML xmlns:ui=""UnityEngine.UIElements"" xmlns:uie=""UnityEditor.UIElements"" editor-extension-mode=""False"">
    <!-- {actorName}.uxml - generated by FUnity -->
    <ui:VisualElement name=""root"" class=""actor-root"">
        <ui:VisualElement name=""{m_PortraitSlot}"" class=""actor-portrait"" />
    </ui:VisualElement>
</ui:UXML>";

            File.WriteAllText(ussPath, uss);
            File.WriteAllText(uxmlPath, uxml);
            AssetDatabase.ImportAsset(ussPath);
            AssetDatabase.ImportAsset(uxmlPath);

            var targetActor = ScriptableObject.CreateInstance<FUnityActorData>();
            var actorAssetPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(actorFolder, $"{actorName}_Actor.asset"));
            AssetDatabase.CreateAsset(targetActor, actorAssetPath);
            AssetDatabase.ImportAsset(actorAssetPath);

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            var scriptGraphPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(actorFolder, $"{actorName}_ScriptGraph.asset"));
            var scriptGraphAsset = CreateScriptGraphAsset(scriptGraphPath);
            var scriptGraphAssetPath = scriptGraphAsset != null ? AssetDatabase.GetAssetPath(scriptGraphAsset) : string.Empty;
            if (scriptGraphAsset == null)
            {
                Debug.LogWarning($"[FUnity] Failed to create ScriptGraphAsset at {scriptGraphPath}. ActorData will be created without a ScriptGraph.");
            }

            var serializedActor = new SerializedObject(targetActor);
            var changed = false;

            // なぜ: 新規生成時に表示名もテンプレート由来のActor名へ揃えたい。何を: m_displayNameへactorNameを設定。どうする: SerializedObject経由で値を上書きする。
            var displayNameProperty = serializedActor.FindProperty("m_displayName");
            if (displayNameProperty != null && displayNameProperty.stringValue != actorName)
            {
                displayNameProperty.stringValue = actorName;
                changed = true;
            }

            var uxmlProperty = serializedActor.FindProperty("m_ElementUxml");
            if (uxmlProperty != null && uxmlProperty.objectReferenceValue != uxmlAsset)
            {
                uxmlProperty.objectReferenceValue = uxmlAsset;
                changed = true;
            }

            var ussProperty = serializedActor.FindProperty("m_ElementStyle");
            if (ussProperty != null && ussProperty.objectReferenceValue != ussAsset)
            {
                ussProperty.objectReferenceValue = ussAsset;
                changed = true;
            }

            var scriptGraphProperty = serializedActor.FindProperty("m_scriptGraph");
            if (scriptGraphProperty != null && scriptGraphProperty.objectReferenceValue != scriptGraphAsset)
            {
                scriptGraphProperty.objectReferenceValue = scriptGraphAsset;
                changed = true;
            }

            if (changed)
            {
                serializedActor.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetActor);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = targetActor;
            ProjectWindowUtil.ShowCreatedAsset(targetActor);

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine("Created:");
            messageBuilder.AppendLine(uxmlPath);
            messageBuilder.AppendLine(ussPath);
            messageBuilder.AppendLine(actorAssetPath);
            if (scriptGraphAsset != null)
            {
                messageBuilder.AppendLine(scriptGraphAssetPath);
            }
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Linked to new ActorData: {targetActor.name}");

            EditorUtility.DisplayDialog("Success", messageBuilder.ToString(), "OK");
            var logScriptGraph = string.IsNullOrEmpty(scriptGraphAssetPath) ? "(not created)" : scriptGraphAssetPath;
            Debug.Log($"[FUnity] Generated assets for {actorName} in {actorFolder}. ActorData: {actorAssetPath}. ScriptGraph: {logScriptGraph}.");
        }

        /// <summary>
        /// 現在選択されているアセットからテンプレ名や出力フォルダの初期値を補完する。
        /// </summary>
        private void ApplySelectionHints()
        {
            var activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                return;
            }

            if (activeObject is FUnityActorData actorData)
            {
                if (string.IsNullOrEmpty(m_TemplateName) || m_TemplateName == "MyActor" || m_TemplateName == m_LastSuggestedName)
                {
                    m_TemplateName = actorData.name + "_New";
                    m_LastSuggestedName = m_TemplateName;
                }

                var actorPath = AssetDatabase.GetAssetPath(actorData);
                var actorFolder = Path.GetDirectoryName(actorPath);
                if (!string.IsNullOrEmpty(actorFolder) && IsFolderInAssets(actorFolder))
                {
                    m_Folder = NormalizeFolderPath(actorFolder);
                }

                return;
            }

            if (activeObject is Sprite || activeObject is Texture2D)
            {
                if (string.IsNullOrEmpty(m_TemplateName) || m_TemplateName == "MyActor" || m_TemplateName == m_LastSuggestedName)
                {
                    m_TemplateName = activeObject.name + "Actor";
                    m_LastSuggestedName = m_TemplateName;
                }

                var texturePath = AssetDatabase.GetAssetPath(activeObject);
                var textureFolder = Path.GetDirectoryName(texturePath);
                if (!string.IsNullOrEmpty(textureFolder) && IsFolderInAssets(textureFolder))
                {
                    m_Folder = NormalizeFolderPath(textureFolder);
                }

                return;
            }

            if (activeObject is DefaultAsset defaultAsset)
            {
                var folderPath = AssetDatabase.GetAssetPath(defaultAsset);
                if (AssetDatabase.IsValidFolder(folderPath) && IsFolderInAssets(folderPath))
                {
                    m_Folder = NormalizeFolderPath(folderPath);
                }
            }
        }

        /// <summary>
        /// Template Name からファイル名として安全に扱える俳優名を取得する。空白はアンダースコアにし、無効文字は除外する。
        /// </summary>
        private string GetActorName()
        {
            if (string.IsNullOrWhiteSpace(m_TemplateName))
            {
                return string.Empty;
            }

            var trimmed = m_TemplateName.Trim();
            var normalized = Regex.Replace(trimmed, "\\s+", "_");
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                if (invalidChars.Contains(ch) || ch == '/')
                {
                    builder.Append('_');
                    continue;
                }

                builder.Append(ch);
            }

            return builder.ToString().Trim('_');
        }

        /// <summary>
        /// 現在の俳優名から `Assets/FUnity/Actors/<Name>` 形式のサジェストフォルダを計算し、必要に応じて反映する。
        /// </summary>
        private void SuggestOutputFolder(bool force)
        {
            var actorName = GetActorName();
            if (string.IsNullOrEmpty(actorName))
            {
                return;
            }

            var activeProjectActorsFolder = GetActiveProjectActorsFolder(false);
            if (string.IsNullOrEmpty(activeProjectActorsFolder))
            {
                return;
            }

            var suggested = $"{activeProjectActorsFolder}/{actorName}";
            if (force)
            {
                if (string.IsNullOrEmpty(m_Folder))
                {
                    m_Folder = suggested;
                }
            }
            else if (string.IsNullOrEmpty(m_Folder) || m_Folder == m_LastSuggestedFolder)
            {
                m_Folder = suggested;
            }

            m_LastSuggestedFolder = suggested;
        }

        /// <summary>
        /// バックスラッシュ除去と末尾スラッシュ整理を行い、AssetDatabase で扱いやすい形へ整える。
        /// </summary>
        private string NormalizeFolderPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            var normalized = rawPath.Trim();
            normalized = normalized.Replace('\\', '/');
            while (normalized.EndsWith("/", System.StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, normalized.Length - 1);
            }

            return normalized;
        }

        /// <summary>
        /// 指定フォルダパスが Assets 配下かどうかを判定する。
        /// </summary>
        private bool IsFolderInAssets(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            var normalized = NormalizeFolderPath(folderPath);
            return normalized == "Assets" || normalized.StartsWith("Assets/", System.StringComparison.Ordinal);
        }

        /// <summary>
        /// パス結合を行い、AssetDatabase で扱えるスラッシュ区切りに正規化する。
        /// </summary>
        private string CombinePath(string folder, string fileName)
        {
            var combined = Path.Combine(folder, fileName);
            return combined.Replace('\\', '/');
        }

        /// <summary>
        /// 基底フォルダから俳優名サブフォルダを計算し、二重付与を避けた正規化パスを返す。
        /// </summary>
        private string ResolveActorFolder(string baseFolder, string actorName, string actorsRootFolder)
        {
            var normalizedBase = NormalizeFolderPath(baseFolder);
            if (string.IsNullOrEmpty(normalizedBase))
            {
                normalizedBase = NormalizeFolderPath(actorsRootFolder);
            }

            if (normalizedBase.EndsWith($"/{actorName}", System.StringComparison.Ordinal))
            {
                return normalizedBase;
            }

            return NormalizeFolderPath(CombinePath(normalizedBase, actorName));
        }

        /// <summary>
        /// 指定パスに ScriptGraphAsset を生成し、既存の場合は読み込んで返す。
        /// </summary>
        private ScriptGraphAsset CreateScriptGraphAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            var existing = AssetDatabase.LoadAssetAtPath<ScriptGraphAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var graph = ScriptableObject.CreateInstance<ScriptGraphAsset>();
            graph.graph = new FlowGraph();

            AssetDatabase.CreateAsset(graph, assetPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            Debug.Log($"[FUnity] Created ScriptGraphAsset: {assetPath}");
            return graph;
        }

        /// <summary>
        /// 現在のシーンに存在する FUnityManager から ProjectData を取得し、適合しない場合はダイアログ表示を行う。
        /// </summary>
        /// <param name="projectData">取得した <see cref="FUnityProjectData"/>。</param>
        /// <param name="showDialog">問題発生時にダイアログを表示するかどうか。</param>
        private static bool TryGetActiveFUnityProject(out FUnityProjectData projectData, bool showDialog)
        {
            projectData = null;

            var managers = Object.FindObjectsOfType<FUnityManager>();
            if (managers == null || managers.Length == 0)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "FUnity Actor 作成",
                        "現在のシーンに FUnityManager が見つかりません。\nFUnityManager を配置してから Actor を作成してください。",
                        "OK");
                }

                return false;
            }

            if (managers.Length > 1)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "FUnity Actor 作成",
                        "現在のシーンに複数の FUnityManager が存在します。\n1 つに絞ってから実行してください。",
                        "OK");
                }

                return false;
            }

            var manager = managers[0];
            if (manager.ProjectData == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "FUnity Actor 作成",
                        "FUnityManager の Project (FUnityProjectData) が設定されていません。\n有効なプロジェクトを設定してから実行してください。",
                        "OK");
                }

                return false;
            }

            projectData = manager.ProjectData;
            return true;
        }

        /// <summary>
        /// 指定された ProjectData から Actors フォルダパスを算出し、存在しなければ作成する。
        /// </summary>
        /// <param name="projectData">フォルダを導出する <see cref="FUnityProjectData"/>。</param>
        /// <param name="showDialog">失敗時にダイアログを表示するかどうか。</param>
        /// <param name="actorsFolderPath">算出された Actors フォルダパス。</param>
        private static bool TryGetActorsFolderPath(FUnityProjectData projectData, bool showDialog, out string actorsFolderPath)
        {
            actorsFolderPath = string.Empty;
            if (projectData == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "FUnity Actor 作成",
                        "FUnityProjectData が null です。",
                        "OK");
                }

                return false;
            }

            var projectDataPath = AssetDatabase.GetAssetPath(projectData);
            if (string.IsNullOrEmpty(projectDataPath))
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "FUnity Actor 作成",
                        "FUnityProjectData のパスを取得できませんでした。",
                        "OK");
                }

                return false;
            }

            var projectFolder = Path.GetDirectoryName(projectDataPath);
            if (string.IsNullOrEmpty(projectFolder))
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "FUnity Actor 作成",
                        "FUnityProjectData のフォルダを特定できませんでした。",
                        "OK");
                }

                return false;
            }

            projectFolder = projectFolder.Replace("\\", "/");
            actorsFolderPath = projectFolder + "/Actors";

            if (!AssetDatabase.IsValidFolder(actorsFolderPath))
            {
                AssetDatabase.CreateFolder(projectFolder, "Actors");
                actorsFolderPath = projectFolder + "/Actors";
            }

            return true;
        }

        /// <summary>
        /// 現在アクティブなプロジェクトの Actors フォルダパスを取得し、取得失敗時は空文字を返す。
        /// </summary>
        /// <param name="showDialog">問題発生時にダイアログを表示するかどうか。</param>
        private static string GetActiveProjectActorsFolder(bool showDialog)
        {
            if (!TryGetActiveFUnityProject(out var projectData, showDialog))
            {
                return string.Empty;
            }

            return TryGetActorsFolderPath(projectData, showDialog, out var actorsFolderPath) ? actorsFolderPath : string.Empty;
        }

        /// <summary>
        /// 指定フォルダが現在のプロジェクト Actors 直下に存在するか判定する。
        /// </summary>
        /// <param name="folderPath">判定対象のフォルダ。</param>
        /// <param name="actorsRootFolder">許可される Actors ルートフォルダ。</param>
        private bool IsFolderInsideActorsRoot(string folderPath, string actorsRootFolder)
        {
            var normalizedFolder = NormalizeFolderPath(folderPath);
            var normalizedActorsRoot = NormalizeFolderPath(actorsRootFolder);
            if (string.IsNullOrEmpty(normalizedFolder) || string.IsNullOrEmpty(normalizedActorsRoot))
            {
                return false;
            }

            return normalizedFolder == normalizedActorsRoot || normalizedFolder.StartsWith(normalizedActorsRoot + "/", System.StringComparison.Ordinal);
        }
    }
}
#endif
