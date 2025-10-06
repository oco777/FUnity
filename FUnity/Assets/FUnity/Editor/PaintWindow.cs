using UnityEngine;
using UnityEditor;

namespace FUnity.Editor {
    public class PaintWindow : EditorWindow {
        private Texture2D m_canvasTexture;
        private Color m_drawColor = Color.black;
        private Vector2Int m_lastPixelPos = new Vector2Int(-1, -1);
        private int m_canvasSize = 256;

        [MenuItem("FUnity/Paint Window")]
        public static void ShowWindow() {
            var window = GetWindow<PaintWindow>();
            window.titleContent = new GUIContent("FUnity Paint");
            window.Show();
        }

        private void OnEnable() {
            if (m_canvasTexture == null) {
                m_canvasTexture = new Texture2D(m_canvasSize, m_canvasSize, TextureFormat.RGBA32, false);
                ClearCanvas();
            }
        }

        private void OnGUI() {
            GUILayout.Label("🎨 FUnity Paint Tool", EditorStyles.boldLabel);

            // 色選択
            m_drawColor = EditorGUILayout.ColorField("Draw Color", m_drawColor);

            // キャンバス描画
            Rect drawArea = GUILayoutUtility.GetRect(m_canvasSize, m_canvasSize, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(drawArea, m_canvasTexture);

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
                Vector2Int pixelPos = new Vector2Int((int)localPos.x, m_canvasSize - (int)localPos.y);

                DrawPixel(pixelPos);
                m_lastPixelPos = pixelPos;
                e.Use();
            }
            else if (e.type == EventType.MouseUp) {
                m_lastPixelPos = new Vector2Int(-1, -1);
            }
        }

        private void DrawPixel(Vector2Int pos) {
            if (pos.x < 0 || pos.x >= m_canvasSize || pos.y < 0 || pos.y >= m_canvasSize) return;
            m_canvasTexture.SetPixel(pos.x, pos.y, m_drawColor);
            m_canvasTexture.Apply();
        }

        private void ClearCanvas() {
            Color[] pixels = new Color[m_canvasSize * m_canvasSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            m_canvasTexture.SetPixels(pixels);
            m_canvasTexture.Apply();
        }
    }
}
