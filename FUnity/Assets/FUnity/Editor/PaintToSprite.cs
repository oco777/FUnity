using UnityEngine;
using UnityEditor;
using System.IO;
using FUnity.Stage;

namespace FUnity.Editor {
    public class PaintToSprite : EditorWindow {
        private Texture2D m_canvasTexture;
        private int m_canvasSize = 256;

        [MenuItem("FUnity/Paint & Save as Sprite")]
        public static void ShowWindow() {
            var window = GetWindow<PaintToSprite>();
            window.titleContent = new GUIContent("Paint to Sprite");
            window.Show();
        }

        private void OnEnable() {
            if (m_canvasTexture == null) {
                m_canvasTexture = new Texture2D(m_canvasSize, m_canvasSize, TextureFormat.RGBA32, false);
                ClearCanvas();
            }
        }

        private void OnGUI() {
            GUILayout.Label("🎨 Draw your Sprite", EditorStyles.boldLabel);

            // 描画領域
            Rect drawArea = GUILayoutUtility.GetRect(m_canvasSize, m_canvasSize, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(drawArea, m_canvasTexture);

            HandleMouse(drawArea);

            GUILayout.Space(10);
            if (GUILayout.Button("Clear Canvas")) ClearCanvas();
            if (GUILayout.Button("Save as Sprite")) SaveAsSprite();
        }

        private void HandleMouse(Rect area) {
            Event e = Event.current;
            if (e.type == EventType.MouseDrag && area.Contains(e.mousePosition)) {
                Vector2 local = e.mousePosition - new Vector2(area.x, area.y);
                Vector2Int pixel = new Vector2Int((int)local.x, m_canvasSize - (int)local.y);
                m_canvasTexture.SetPixel(pixel.x, pixel.y, Color.black);
                m_canvasTexture.Apply();
                e.Use();
            }
        }

        private void ClearCanvas() {
            Color[] pixels = new Color[m_canvasSize * m_canvasSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            m_canvasTexture.SetPixels(pixels);
            m_canvasTexture.Apply();
        }

        private void SaveAsSprite() {
            string path = EditorUtility.SaveFilePanel("Save Sprite", "Assets", "MyDrawing", "png");
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = m_canvasTexture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.Refresh();

            // Assetsフォルダ内ならSprite化
            if (path.StartsWith(Application.dataPath)) {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer != null) {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                }

                // インポート完了まで待機
                AssetDatabase.Refresh();

                // ここで再ロード
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);

                if (sprite == null) {
                    Debug.LogWarning("⚠️ Spriteがまだロードされていません。再インポート後に再試行してください。");
                    return;
                }

                // Stage用の定義アセットを自動生成
                CreateStageSpriteDefinition(sprite, relativePath);

                // シーン上に配置
                GameObject obj = new GameObject("FUnitySprite");
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);

                Debug.Log("✅ Sprite created and placed in the scene!");
            }
        }

        private static void CreateStageSpriteDefinition(Sprite sprite, string spriteAssetPath) {
            string directory = Path.GetDirectoryName(spriteAssetPath);
            if (string.IsNullOrEmpty(directory)) return;

            string definitionPath = Path.Combine(directory, sprite.name + "_StageSprite.asset");
            var definition = ScriptableObject.CreateInstance<StageSpriteDefinition>();
            definition.Initialize(sprite, sprite.name);
            AssetDatabase.CreateAsset(definition, definitionPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"🧩 StageSpriteDefinition created at {definitionPath}. Visual Scriptingから StageVisualScripting.Spawn を呼び出すことで配置できます。");
        }
    }
}
