using UnityEngine;
using UnityEditor;
using System.IO;

namespace FUnity.Editor {
    public class PaintToSprite : EditorWindow {
        private Texture2D _canvasTexture;
        private int _canvasSize = 256;

        [MenuItem("FUnity/Paint & Save as Sprite")]
        public static void ShowWindow() {
            var window = GetWindow<PaintToSprite>();
            window.titleContent = new GUIContent("Paint to Sprite");
            window.Show();
        }

        private void OnEnable() {
            if (_canvasTexture == null) {
                _canvasTexture = new Texture2D(_canvasSize, _canvasSize, TextureFormat.RGBA32, false);
                ClearCanvas();
            }
        }

        private void OnGUI() {
            GUILayout.Label("🎨 Draw your Sprite", EditorStyles.boldLabel);

            // 描画領域
            Rect drawArea = GUILayoutUtility.GetRect(_canvasSize, _canvasSize, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(drawArea, _canvasTexture);

            HandleMouse(drawArea);

            GUILayout.Space(10);
            if (GUILayout.Button("Clear Canvas")) ClearCanvas();
            if (GUILayout.Button("Save as Sprite")) SaveAsSprite();
        }

        private void HandleMouse(Rect area) {
            Event e = Event.current;
            if (e.type == EventType.MouseDrag && area.Contains(e.mousePosition)) {
                Vector2 local = e.mousePosition - new Vector2(area.x, area.y);
                Vector2Int pixel = new Vector2Int((int)local.x, _canvasSize - (int)local.y);
                _canvasTexture.SetPixel(pixel.x, pixel.y, Color.black);
                _canvasTexture.Apply();
                e.Use();
            }
        }

        private void ClearCanvas() {
            Color[] pixels = new Color[_canvasSize * _canvasSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            _canvasTexture.SetPixels(pixels);
            _canvasTexture.Apply();
        }

        private void SaveAsSprite() {
            string path = EditorUtility.SaveFilePanel("Save Sprite", "Assets", "MyDrawing", "png");
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = _canvasTexture.EncodeToPNG();
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

                // シーン上に配置
                GameObject obj = new GameObject("FUnitySprite");
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);

                Debug.Log("✅ Sprite created and placed in the scene!");
            }
        }
    }
}
