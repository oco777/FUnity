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
        private Color m_PrimaryColor = new Color(0.15f, 0.6f, 1f, 1f);
        // なぜ: 生成直後にアクター定義へ適用したい。何を: 選択中FUnityActorDataとのリンク要否。どうする: trueならSelection経由で適用。
        private bool m_LinkToSelectedActor = true;
        // なぜ: ActorData未選択時にも統一フォルダで生成したい。何を: ActorData自動生成の有効/無効。どうする: trueなら不足時に新規作成。
        private bool m_CreateActorDataIfNone = true;
        // なぜ: 俳優ごとの Visual Scripting を同梱したい。何を: ScriptGraphAssetの自動生成可否。どうする: trueなら不足時に新規作成。
        private bool m_CreateScriptGraphIfNone = true;
        // なぜ: サジェストしたフォルダ名とユーザー入力を区別したい。何を: 直近サジェストを保持する変数。どうする: Template変更時の自動更新に利用。
        private string m_LastSuggestedFolder = string.Empty;
        // なぜ: 初回ウィンドウ表示で一度だけ初期化したい。何を: 初期化済みフラグ。どうする: OnEnableで設定を行うかどうか判定する。
        private bool m_IsInitialized;

        [MenuItem("FUnity/Authoring/Generate Actor UI Template...")]
        public static void Open()
        {
            // なぜ: 著者に最小テンプレ生成を提供したい。何を: FUnity向けUXML/USSを作る専用ウィンドウ。どうする: EditorWindowを開く。
            var window = GetWindow<GenerateActorUITemplateWindow>("Generate Actor UI");
            window.minSize = new Vector2(440, 360);
            window.Show();
        }

        private void OnEnable()
        {
            // なぜ: ウィンドウ生成時に初期フォルダ候補を整えたい。何を: サジェストフォルダとトグル初期値。どうする: 一度だけ初期化し、再表示時の入力を保持する。
            if (m_IsInitialized)
            {
                return;
            }

            m_IsInitialized = true;
            m_CreateActorDataIfNone = true;
            m_CreateScriptGraphIfNone = true;
            SuggestOutputFolder(true);
        }

        private void OnGUI()
        {
            // なぜ: テンプレ構成要素を一括設定したい。何を: 名前/出力先/portraitスロット/サイズ/色/リンク有無の入力群。
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

            EditorGUI.BeginChangeCheck();
            var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(m_Folder);
            folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder Asset", folderAsset, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                var assetPath = AssetDatabase.GetAssetPath(folderAsset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    m_Folder = NormalizeFolderPath(assetPath);
                }
            }

            m_PortraitSlot = EditorGUILayout.TextField("Portrait Slot Name", m_PortraitSlot);
            m_Width = EditorGUILayout.IntField("Width (px)", m_Width);
            m_Height = EditorGUILayout.IntField("Height (px)", m_Height);
            m_PrimaryColor = EditorGUILayout.ColorField("Primary Color", m_PrimaryColor);
            m_LinkToSelectedActor = EditorGUILayout.Toggle("Link to selected FUnityActorData", m_LinkToSelectedActor);
            m_CreateActorDataIfNone = EditorGUILayout.Toggle("Create ActorData if none", m_CreateActorDataIfNone);
            m_CreateScriptGraphIfNone = EditorGUILayout.Toggle("Create ScriptGraph if none", m_CreateScriptGraphIfNone);

            var actorName = GetActorName();
            var hasValidActorName = !string.IsNullOrEmpty(actorName);
            var folderValid = IsFolderInAssets(m_Folder);

            if (!hasValidActorName)
            {
                EditorGUILayout.HelpBox("Template Name を入力してください（ファイル名に使用します）。", MessageType.Warning);
            }

            if (!folderValid)
            {
                EditorGUILayout.HelpBox("Output Folder は Assets 配下に指定してください。", MessageType.Error);
            }

            if (!m_LinkToSelectedActor && !m_CreateActorDataIfNone)
            {
                EditorGUILayout.HelpBox("ActorData を選択するか、Create ActorData if none を有効にしてください。", MessageType.Warning);
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
        /// <item><description>出力フォルダを用意する。</description></item>
        /// <item><description>UXML/USSを生成し保存する。</description></item>
        /// <item><description>必要であれば ActorData と ScriptGraph を生成・取得する。</description></item>
        /// <item><description>対象 ActorData に UXML/USS/ScriptGraph をリンクする。</description></item>
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

            if (!IsFolderInAssets(m_Folder))
            {
                EditorUtility.DisplayDialog("Error", "Output Folder must be inside the Assets directory.", "OK");
                return;
            }

            var outputFolder = NormalizeFolderPath(m_Folder);
            if (string.IsNullOrEmpty(outputFolder))
            {
                EditorUtility.DisplayDialog("Error", "Output Folder is invalid.", "OK");
                return;
            }

            Directory.CreateDirectory(outputFolder);
            var uxmlPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(outputFolder, $"{actorName}.uxml"));
            var ussPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(outputFolder, $"{actorName}.uss"));

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

            FUnityActorData targetActor = null;
            if (m_LinkToSelectedActor && Selection.activeObject is FUnityActorData selectedActor)
            {
                targetActor = selectedActor;
            }
            else if (m_CreateActorDataIfNone)
            {
                targetActor = ScriptableObject.CreateInstance<FUnityActorData>();
                var actorAssetPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(outputFolder, $"{actorName}_Actor.asset"));
                AssetDatabase.CreateAsset(targetActor, actorAssetPath);
                AssetDatabase.ImportAsset(actorAssetPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "ActorData を選択するか、新規作成を有効にしてください。", "OK");
                return;
            }

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            var ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            ScriptGraphAsset scriptGraphAsset = targetActor != null ? targetActor.ScriptGraph : null;

            if (targetActor == null)
            {
                Debug.LogError("[FUnity] Failed to resolve or create FUnityActorData target.");
                return;
            }

            if (m_CreateScriptGraphIfNone && scriptGraphAsset == null)
            {
                var scriptGraphPath = AssetDatabase.GenerateUniqueAssetPath(CombinePath(outputFolder, $"{actorName}_ScriptGraph.asset"));
                scriptGraphAsset = CreateScriptGraphAsset(scriptGraphPath);
            }
            else if (!m_CreateScriptGraphIfNone && scriptGraphAsset == null)
            {
                Debug.LogWarning($"[FUnity] ScriptGraph is not assigned for {targetActor.name}. You can assign it manually later.");
            }

            var serializedActor = new SerializedObject(targetActor);
            var changed = false;

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
            if (scriptGraphAsset != null)
            {
                messageBuilder.AppendLine(AssetDatabase.GetAssetPath(scriptGraphAsset));
            }
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"Linked to: {targetActor.name}");

            EditorUtility.DisplayDialog("Success", messageBuilder.ToString(), "OK");
            Debug.Log($"[FUnity] Generated assets for {actorName} in {outputFolder}, linked to {targetActor.name}.");
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

            var suggested = $"Assets/FUnity/Actors/{actorName}";
            if (force || string.IsNullOrEmpty(m_Folder) || m_Folder == m_LastSuggestedFolder)
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
    }
}
#endif
