using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 背景レイヤの表示異常を切り分ける診断機能を提供します。
    /// </summary>
    public static class StageBackgroundDiagnostics
    {
        /// <summary>背景レイヤ ID の第一候補。</summary>
        public const string BackgroundLayerIdA = "FUnityBackgroundLayer";

        /// <summary>背景レイヤ ID の第二候補。</summary>
        public const string BackgroundLayerIdB = "BackgroundLayer";

        /// <summary>
        /// 背景レイヤとルートの状態を点検し、表示されない原因候補をログへ出力します。
        /// </summary>
        /// <param name="panelRoot">診断対象となる UI ドキュメントのルート要素。</param>
        /// <param name="backgroundName">Resources/Backgrounds 内で検索するテクスチャ名。</param>
        /// <param name="tryTemporaryFix">診断中に暫定修正（可視化）を適用するかどうか。</param>
        public static void RunBackgroundDiagnostics(VisualElement panelRoot, string backgroundName = "Background_01", bool tryTemporaryFix = true)
        {
            void Log(string msg) => Debug.Log($"[FUnity.BGDiag] {msg}");
            void Warn(string msg) => Debug.LogWarning($"[FUnity.BGDiag] {msg}");
            void Err(string msg) => Debug.LogError($"[FUnity.BGDiag] {msg}");

            if (panelRoot == null)
            {
                Err("panelRoot == null。UIDocument.rootVisualElement を渡してください。");
                return;
            }

            Log($"RunBackgroundDiagnostics start (background='{backgroundName}')");

            var rw = panelRoot.resolvedStyle.width;
            var rh = panelRoot.resolvedStyle.height;
            Log($"Root size: {rw} x {rh}, display={panelRoot.resolvedStyle.display}, opacity={panelRoot.resolvedStyle.opacity}");
            if (rw <= 0 || rh <= 0)
            {
                Warn("Root のレイアウトが 0x0。親のサイズ/レイアウトを確認してください。");
            }

            var layer = panelRoot.Q<VisualElement>(BackgroundLayerIdA) ?? panelRoot.Q<VisualElement>(BackgroundLayerIdB);
            Log($"Background layer found: {(layer != null ? layer.name : "null")}");

            if (layer == null)
            {
                Warn("背景レイヤが見つかりません。子要素一覧を出力します（最大 32 件）。");
                int logged = 0;
                foreach (var child in panelRoot.Children())
                {
                    if (logged >= 32)
                    {
                        Log("... (truncated)");
                        break;
                    }

                    logged++;
                    Log($"Child[{logged}] name='{child.name}', classes='{string.Join(" ", child.GetClasses())}', size={child.resolvedStyle.width}x{child.resolvedStyle.height}");
                }

                return;
            }

            var lw = layer.resolvedStyle.width;
            var lh = layer.resolvedStyle.height;
            var disp = layer.resolvedStyle.display;
            var opac = layer.resolvedStyle.opacity;
            Log($"Layer size: {lw} x {lh}, display={disp}, opacity={opac}, pickingMode={layer.pickingMode}");

            var size = layer.resolvedStyle.backgroundSize;
            Log($"Layer backgroundSize: ({size.x.value}{size.x.unit}, {size.y.value}{size.y.unit})");

            if (lw <= 0 || lh <= 0)
            {
                Warn("背景レイヤのレイアウトが 0x0。四辺 0、position:absolute が適用されているか確認してください。");
            }

            bool hasImage = layer.style.backgroundImage.keyword != StyleKeyword.None && layer.resolvedStyle.backgroundImage.texture != null;
            Log($"Layer backgroundImage: {(hasImage ? "SET" : "NONE")}");
            if (!hasImage)
            {
                var testTex = Resources.Load<Texture2D>($"Backgrounds/{backgroundName}");
                Log($"Resources.Load('Backgrounds/{backgroundName}'): {(testTex ? "HIT" : "MISS")}");
                if (testTex == null)
                {
                    Warn("Resources/Backgrounds/ 配下に PNG が無いか、パスが誤っています。拡張子無し・相対で指定してください。");
                }
                else if (tryTemporaryFix)
                {
                    layer.style.backgroundImage = new StyleBackground(testTex);
                    Log("臨時: 読み込んだテクスチャを backgroundImage に適用しました。");
                }
            }

            Log($"unityBackgroundScaleMode={layer.resolvedStyle.unityBackgroundScaleMode}");
            if (tryTemporaryFix && layer.resolvedStyle.unityBackgroundScaleMode != ScaleMode.ScaleToFit)
            {
                layer.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                Log("臨時: unityBackgroundScaleMode を ScaleToFit に変更しました（見切れ対策）。");
            }

            if (tryTemporaryFix)
            {
                const string probeName = "BGDiagProbe";
                var existingProbe = layer.Q<VisualElement>(probeName);
                if (existingProbe == null)
                {
                    var probe = new VisualElement { name = probeName };
                    probe.pickingMode = PickingMode.Ignore;
                    probe.style.position = Position.Absolute;
                    probe.style.left = 0;
                    probe.style.top = 0;
                    probe.style.right = 0;
                    probe.style.bottom = 0;
                    probe.style.borderLeftWidth = 2;
                    probe.style.borderTopWidth = 2;
                    probe.style.borderRightWidth = 2;
                    probe.style.borderBottomWidth = 2;
                    probe.style.borderLeftColor = Color.magenta;
                    probe.style.borderTopColor = Color.magenta;
                    probe.style.borderRightColor = Color.magenta;
                    probe.style.borderBottomColor = Color.magenta;
                    layer.Add(probe);
                    Log("臨時: 背景レイヤ全面にマゼンタ枠（BGDiagProbe）を追加しました。見えるか確認してください。");
                }
                else
                {
                    Log("臨時: 既存の BGDiagProbe が見つかったため再利用します。");
                }
            }

            int idx = panelRoot.IndexOf(layer);
            Log($"Layer index under root: {idx}（0 が最背面）");
            if (idx != 0 && tryTemporaryFix)
            {
                panelRoot.Remove(layer);
                panelRoot.Insert(0, layer);
                Log("臨時: 背景レイヤをルートの先頭（最背面）へ移動しました。");
            }

            Log("RunBackgroundDiagnostics end");
        }
    }
}
