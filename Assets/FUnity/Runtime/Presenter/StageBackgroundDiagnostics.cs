using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;
using FUnity.Runtime.UI;
using UnityEngine;
using UnityEngine.UIElements;
using UInput = UnityEngine.Input;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 背景レイヤの実際のスケール適用状態を文字列で表現する診断用ユーティリティです。
    /// </summary>
    internal static class StageBackgroundDiagnosticsUtil
    {
        /// <summary>
        /// 背景レイヤに適用されたスケールを判定し、contain/cover の USS クラスを優先して説明文を返します。
        /// 指定クラスが無い場合は設定値のみを補足し、不明扱いとします。
        /// </summary>
        /// <param name="background">診断対象となる背景レイヤ要素。</param>
        /// <param name="configuredValue">ステージ設定から取得した期待スケール。null の場合は未設定扱い。</param>
        /// <returns>USS クラスまたは style から推測したスケール説明。要素が無い場合は設定値を補足した "n/a" を返却。</returns>
        public static string GetBackgroundScaleLabel(VisualElement background, string configuredValue)
        {
            if (background == null)
            {
                return string.IsNullOrEmpty(configuredValue) ? "n/a" : $"n/a (configured={configuredValue})";
            }

            if (background.ClassListContains("bg--cover"))
            {
                return "cover (by class)";
            }

            if (background.ClassListContains("bg--contain"))
            {
                return "contain (by class)";
            }

            return string.IsNullOrEmpty(configuredValue) ? "unknown (no class)" : $"unknown (configured={configuredValue})";
        }
    }

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
        /// <param name="stageData">診断対象ステージの設定値。null の場合は未設定として扱う。</param>
        public static void RunBackgroundDiagnostics(VisualElement panelRoot, string backgroundName = "Background_01", bool tryTemporaryFix = true, FUnityStageData stageData = null)
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

            var activeConfig = Resources.Load<FUnityModeConfig>("FUnityActiveMode");
            var origin = CoordinateConverter.GetActiveOrigin(activeConfig);

            if (origin == CoordinateOrigin.Center)
            {
                var stageForCoords = panelRoot.Q<VisualElement>(StageElement.ActorContainerName)
                    ?? panelRoot.Q<VisualElement>(StageElement.StageRootName)
                    ?? panelRoot;

                if (stageForCoords != null)
                {
                    var uiZero = CoordinateConverter.LogicalToUI(Vector2.zero, stageForCoords, origin);
                    Log($"[Coord] logical(0,0) -> ui({uiZero.x:F1},{uiZero.y:F1})");

                    var uiHundred = CoordinateConverter.LogicalToUI(new Vector2(100f, 100f), stageForCoords, origin);
                    Log($"[Coord] logical(100,100) -> ui({uiHundred.x:F1},{uiHundred.y:F1})");

                    var panel = stageForCoords.panel ?? panelRoot.panel;
                    if (panel != null)
                    {
                        var mouseScreen = UInput.mousePosition;
                        var uiMouse = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(mouseScreen.x, mouseScreen.y));
                        var logicalMouse = CoordinateConverter.UIToLogical(uiMouse, stageForCoords, origin);
                        Log($"[Coord] mouse ui({uiMouse.x:F1},{uiMouse.y:F1}) -> logical({logicalMouse.x:F1},{logicalMouse.y:F1})");
                    }
                }
                else
                {
                    Log("[Coord] Coordinate diagnostics skipped (stage root not found).");
                }
            }

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

            var classList = string.Join(" ", layer.GetClasses());
            Log($"Layer classes: {classList}");

            var configuredScale = stageData != null ? stageData.BackgroundScale : "(null)";
            var appliedScale = StageBackgroundDiagnosticsUtil.GetBackgroundScaleLabel(layer, configuredScale);
            Log($"Layer background scale: configured='{configuredScale}', applied='{appliedScale}'");

            if (lw <= 0 || lh <= 0)
            {
                Warn("背景レイヤのレイアウトが 0x0。四辺 0、position:absolute が適用されているか確認してください。");
            }

            var resolvedBackground = layer.resolvedStyle.backgroundImage;
            bool hasImage = resolvedBackground.texture != null || resolvedBackground.sprite != null;
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

            var hasContain = layer.ClassListContains("bg--contain");
            var hasCover = layer.ClassListContains("bg--cover");
            Log($"Layer scale classes: contain={hasContain}, cover={hasCover}");

            if (tryTemporaryFix && !hasContain && !hasCover)
            {
                layer.AddToClassList("bg--contain");
                Log("臨時: 背景スケールクラス bg--contain を追加しました。");
            }

            if (tryTemporaryFix)
            {
                StageBackgroundService.ForceClearInlineBackgroundSize(layer);
                Log("臨時: background-size を inline から除去し USS 指定に戻しました。");
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
