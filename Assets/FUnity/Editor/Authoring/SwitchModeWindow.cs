using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;

namespace FUnity.Editor.Authoring
{
    /// <summary>
    /// FUnity の制作モードを切り替えるための EditorWindow です。
    /// 既存のモード設定アセットを一覧表示し、選択した内容をアクティブ設定へ複製して保存します。
    /// </summary>
    public sealed class SwitchModeWindow : EditorWindow
    {
        private const string ActiveAssetPath = "Assets/FUnity/Resources/FUnityActiveMode.asset";

        /// <summary>一覧表示するモード設定アセットのキャッシュ。</summary>
        private FUnityModeConfig[] m_AvailableConfigs = System.Array.Empty<FUnityModeConfig>();

        /// <summary>Inspector 表示用に加工したアセット名一覧。</summary>
        private string[] m_AvailableNames = System.Array.Empty<string>();

        /// <summary>現在選択中のアセットインデックス。</summary>
        private int m_SelectedIndex = -1;

        /// <summary>リソースフォルダに保持するアクティブ設定アセット。</summary>
        private FUnityModeConfig m_ActiveConfig;

        /// <summary>
        /// メニューからウィンドウを開きます。
        /// </summary>
        [MenuItem("FUnity/Authoring/Switch Mode…", priority = 100)]
        public static void Open()
        {
            var window = GetWindow<SwitchModeWindow>(utility: true, title: "FUnity Mode Switcher");
            window.minSize = new Vector2(420f, 360f);
            window.Initialize();
            window.Show();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnFocus()
        {
            Initialize();
        }

        /// <summary>
        /// アセットを読み込み、選択状態を初期化します。
        /// </summary>
        private void Initialize()
        {
            m_AvailableConfigs = LoadAvailableConfigs();
            m_AvailableNames = m_AvailableConfigs.Select(config => config != null ? config.name : "(Missing)").ToArray();
            m_ActiveConfig = CreateOrLoadActiveConfigAsset();

            m_SelectedIndex = DetermineInitialSelection();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("制作モードの切り替え", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scratch モードは 480x360px・標準ブロック互換、unityroom モードは 16:9 と Unity 2D 拡張を想定しています。", MessageType.Info);

            using (new EditorGUI.DisabledScope(m_AvailableConfigs == null || m_AvailableConfigs.Length == 0))
            {
                if (m_AvailableNames != null && m_AvailableNames.Length > 0)
                {
                    var newIndex = EditorGUILayout.Popup(
                        "アクティブモード",
                        Mathf.Clamp(m_SelectedIndex, 0, m_AvailableNames.Length - 1),
                        m_AvailableNames);
                    if (newIndex != m_SelectedIndex)
                    {
                        m_SelectedIndex = newIndex;
                        Repaint();
                    }
                }
                else
                {
                    EditorGUILayout.Popup("アクティブモード", 0, new[] { "(利用可能な設定がありません)" });
                }
            }

            EditorGUILayout.Space();
            DrawSelectedConfigDetails();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(GetSelectedConfig() == null))
            {
                if (GUILayout.Button("アクティブ設定に適用", GUILayout.Height(32f)))
                {
                    ApplySelectedMode();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("適用後はカメラ解像度・Canvas Scaler・WebGL PlayerSettings をモードに合わせて更新してください。\nこのウィンドウは推奨値を案内し、既存プロジェクトの破壊的変更は行いません。", MessageType.None);
        }

        /// <summary>
        /// 利用可能なモード設定アセットを検索して読み込みます。
        /// </summary>
        /// <returns>検索結果の配列。</returns>
        private static FUnityModeConfig[] LoadAvailableConfigs()
        {
            var guidList = AssetDatabase.FindAssets("t:FUnityModeConfig");
            var configs = new List<FUnityModeConfig>();

            foreach (var guid in guidList)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(path, ActiveAssetPath))
                {
                    continue;
                }

                var config = AssetDatabase.LoadAssetAtPath<FUnityModeConfig>(path);
                if (config != null)
                {
                    configs.Add(config);
                }
            }

            return configs.OrderBy(config => config != null ? config.Mode.ToString() : string.Empty).ToArray();
        }

        /// <summary>
        /// アクティブ設定アセットを読み込み、存在しない場合は親ディレクトリを生成して新規作成します。
        /// </summary>
        /// <returns>読み込んだ、または生成したアクティブ設定アセット。</returns>
        private static FUnityModeConfig CreateOrLoadActiveConfigAsset()
        {
            var directoryPath = Path.GetDirectoryName(ActiveAssetPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var config = AssetDatabase.LoadAssetAtPath<FUnityModeConfig>(ActiveAssetPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<FUnityModeConfig>();
            AssetDatabase.CreateAsset(config, ActiveAssetPath);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return config;
        }

        /// <summary>
        /// 初期表示時に利用する選択インデックスを求めます。
        /// </summary>
        /// <returns>選択するインデックス。候補がない場合は 0。</returns>
        private int DetermineInitialSelection()
        {
            if (m_AvailableConfigs == null || m_AvailableConfigs.Length == 0)
            {
                return 0;
            }

            if (m_ActiveConfig != null)
            {
                for (var i = 0; i < m_AvailableConfigs.Length; i++)
                {
                    var candidate = m_AvailableConfigs[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.Mode == m_ActiveConfig.Mode)
                    {
                        return i;
                    }
                }
            }

            return Mathf.Clamp(m_SelectedIndex, 0, m_AvailableConfigs.Length - 1);
        }

        /// <summary>
        /// 現在選択されているモード設定を返します。
        /// </summary>
        /// <returns>選択中の設定。存在しない場合は null。</returns>
        private FUnityModeConfig GetSelectedConfig()
        {
            if (m_AvailableConfigs == null || m_AvailableConfigs.Length == 0)
            {
                return null;
            }

            var index = Mathf.Clamp(m_SelectedIndex, 0, m_AvailableConfigs.Length - 1);
            return m_AvailableConfigs[index];
        }

        /// <summary>
        /// 選択中の設定内容を一覧表示します。
        /// </summary>
        private void DrawSelectedConfigDetails()
        {
            var selected = GetSelectedConfig();
            if (selected == null)
            {
                EditorGUILayout.HelpBox("利用可能な FUnityModeConfig アセットが見つかりません。Assets 内で作成してください。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("モード概要", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("モード種別", selected.Mode.ToString());
            EditorGUILayout.LabelField("ステージ解像度", $"{selected.StagePixels.x} x {selected.StagePixels.y} px");
            EditorGUILayout.LabelField("Pixels Per Unit", selected.PixelsPerUnit.ToString("F2"));
            EditorGUILayout.LabelField("Physics2D 利用可", selected.AllowUnityPhysics2D ? "はい" : "いいえ");
            EditorGUILayout.LabelField("カスタムシェーダー", selected.AllowCustomShaders ? "許可" : "制限");
            EditorGUILayout.LabelField("SB3 インポート", selected.EnableScratchImport ? "有効" : "無効");

            var extensions = selected.EnabledExtensions != null && selected.EnabledExtensions.Count > 0
                ? string.Join(", ", selected.EnabledExtensions)
                : "（なし）";
            EditorGUILayout.LabelField("有効拡張", extensions);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("推奨アクション", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(BuildGuidanceText(selected), MessageType.Info);
        }

        /// <summary>
        /// 選択したモード設定をアクティブアセットに適用します。
        /// </summary>
        private void ApplySelectedMode()
        {
            var template = GetSelectedConfig();
            if (template == null)
            {
                EditorUtility.DisplayDialog("FUnity Modes", "適用するモード設定が選択されていません。", "OK");
                return;
            }

            if (m_ActiveConfig == null)
            {
                m_ActiveConfig = CreateOrLoadActiveConfigAsset();
            }

            Undo.RecordObject(m_ActiveConfig, "Switch FUnity Mode");
            m_ActiveConfig.ApplyFrom(template);
            EditorUtility.SetDirty(m_ActiveConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var guidance = BuildGuidanceText(template);
            var message = new StringBuilder();
            message.AppendLine($"現在の制作モードを {template.Mode} に切り替えました。");
            message.AppendLine();
            message.AppendLine(guidance);

            EditorUtility.DisplayDialog("FUnity Modes", message.ToString(), "了解");
            Debug.Log($"[FUnity Modes] {template.Mode} をアクティブ化しました。推奨調整:\n{guidance}");
        }

        /// <summary>
        /// 指定したモードに応じた推奨設定テキストを生成します。
        /// </summary>
        /// <param name="config">対象となるモード設定。</param>
        /// <returns>ユーザー向けガイダンス文字列。</returns>
        private static string BuildGuidanceText(FUnityModeConfig config)
        {
            var builder = new StringBuilder();
            if (config == null)
            {
                return string.Empty;
            }

            if (config.Mode == FUnityAuthoringMode.Scratch)
            {
                builder.AppendLine("・ステージカメラを 480x360px / 中心原点に合わせてください。");
                builder.AppendLine("・UI Toolkit の PanelSettings は Fit (Contain) を選択し、1px=1unit のスケールを維持します。");
                builder.AppendLine("・Scratch ブロック互換表 (Docs/Modes/BlocksMapping.md) を参照して Visual Scripting グラフを構成してください。");
                builder.Append("・SB3 インポートを有効にしている場合は、Importer 実験機能を有効化してください。");
            }
            else
            {
                builder.AppendLine("・カメラと Canvas の解像度を 960x540 (16:9) へ設定してください。");
                builder.AppendLine("・WebGL PlayerSettings の Compression を Brotli、Data Caching を有効に設定します。");
                builder.AppendLine("・Physics2D やカスタムシェーダーなどの Unity 拡張ブロックを有効化し、演出を強化できます。");
                builder.Append("・unityroom の公開ガイドラインに従い、入力フォーカスとフルスクリーン制御を確認してください。");
            }

            return builder.ToString();
        }
    }
}
