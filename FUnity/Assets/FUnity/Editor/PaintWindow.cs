using UnityEngine;
using UnityEditor;

namespace FUnity.Editor {
    public class PaintWindow : EditorWindow {
        private Texture2D _canvasTexture;
        private Color _drawColor = Color.black;
        private Vector2Int _lastPixelPos = new Vector2Int(-1, -1);
        private int _canvasSize = 256;

        [MenuItem("FUnity/Paint Window")]
        public static void ShowWindow() {
            var window = GetWindow<PaintWindow>();
            window.titleContent = new GUIContent("FUnity Paint");
            window.Show();
        }

        private void OnEnable() {
            if (_canvasTexture == null) {
                _canvasTexture = new Texture2D(_canvasSize, _canvasSize, TextureFormat.RGBA32, false);
                ClearCanvas();
            }
        }

        private void OnGUI() {
            GUILayout.Label("🎨 FUnity Paint Tool", EditorStyles.boldLabel);

            // 色選択
            _drawColor = EditorGUILayout.ColorField("Draw Color", _drawColor);

            // キャンバス描画
            Rect drawArea = GUILayoutUtility.GetRect(_canvasSize, _canvasSize, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(drawArea, _canvasTexture);

            HandleMouseInput(drawArea);

            GUILayout.Space(10);
            if (GUILayout.Button("Clear Canvas")) {
                ClearCanvas();
            }
        }

        private void HandleMouseInput(Rect drawArea) {
            Event e = Event.current;
            if (e.type == EventType.MouseDrag && drawArea.Contains(e.mousePosition)) {
                Vector2 localPos = e.mousePosition - new Vector2(drawArea.x, drawArea.y);
                Vector2Int pixelPos = new Vector2Int((int)localPos.x, _canvasSize - (int)localPos.y);

                DrawPixel(pixelPos);
                _lastPixelPos = pixelPos;
                e.Use();
            }
            else if (e.type == EventType.MouseUp) {
                _lastPixelPos = new Vector2Int(-1, -1);
            }
        }

        private void DrawPixel(Vector2Int pos) {
            if (pos.x < 0 || pos.x >= _canvasSize || pos.y < 0 || pos.y >= _canvasSize) return;
            _canvasTexture.SetPixel(pos.x, pos.y, _drawColor);
            _canvasTexture.Apply();
        }

        private void ClearCanvas() {
            Color[] pixels = new Color[_canvasSize * _canvasSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            _canvasTexture.SetPixels(pixels);
            _canvasTexture.Apply();
        }
    }
}
