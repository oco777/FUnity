// Updated: 2025-03-14
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// ステージ全体を表現する UI Toolkit のカスタム要素。背景・俳優・オーバーレイの 3 層構造を提供し、
    /// ステージサイズに応じたクリッピングと Scratch 互換の座標系を維持する View レイヤーの土台。
    /// </summary>
    [UxmlElement]
    public partial class StageElement : VisualElement
    {
        /// <summary>ステージ要素の既定名称。UI Builder や検索で再利用する。</summary>
        internal const string StageRootName = "FUnityStage";

        /// <summary>背景レイヤーへ付与する USS クラス名。</summary>
        private const string BackgroundLayerClassName = "funity-stage__background";

        /// <summary>背景レイヤー要素の名称。StageBackgroundService と共有する。</summary>
        internal const string BackgroundLayerName = "FUnityBackgroundLayer";

        /// <summary>背景レイヤーの inline background-size を監視済みと示す USS クラス名。</summary>
        private const string BackgroundInlineGuardClassName = "bg--inline-guarded";

        /// <summary>背景用 USS を読み込む際の Resources パス。</summary>
        private const string BackgroundStyleSheetResourcePath = "UI/StageBackground";

        /// <summary>ステージ要素へ付与する USS クラス名。</summary>
        private const string StageRootClassName = "funity-stage";

        /// <summary>俳優要素を格納するコンテナの既定名称。</summary>
        internal const string ActorContainerName = "FUnityActorContainer";

        /// <summary>オーバーレイ表示（UI 等）を格納するコンテナの既定名称。</summary>
        internal const string OverlayContainerName = "FUnityOverlayContainer";

        /// <summary>Scratch 固定ステージ適用時に付与する USS クラス名。</summary>
        internal const string ScratchStageClassName = "scratch-stage";

        /// <summary>ステージ名ラベルの USS クラス名。</summary>
        private const string StageNameClassName = "funity-stage__name";

        /// <summary>ステージ名ラベルの要素名称。</summary>
        private const string StageNameElementName = "FUnityStageName";

        /// <summary>俳優コンテナへ付与する USS クラス名。</summary>
        private const string ActorContainerClassName = "funity-stage__actors";

        /// <summary>オーバーレイコンテナへ付与する USS クラス名。</summary>
        private const string OverlayContainerClassName = "funity-stage__overlay";

        /// <summary>ステージ内でクリッピングを行うビューポート要素の名称。</summary>
        internal const string StageViewportName = "FUnityStageViewport";

        /// <summary>ステージビューポートへ付与する USS クラス名。</summary>
        private const string StageViewportClassName = "funity-stage__viewport";

        /// <summary>背景 USS のキャッシュ。null のまま読み込み失敗を示す。</summary>
        private static StyleSheet s_cachedBackgroundStyleSheet;

        /// <summary>背景 USS 読み込みを試行したかどうか。</summary>
        private static bool s_backgroundStyleSheetLoaded;

        /// <summary>ステージ表示を行うビューポート。overflow:hidden によるクリッピングを担当。</summary>
        private readonly VisualElement m_stageViewport;

        /// <summary>背景を描画するためのレイヤー。</summary>
        private readonly VisualElement m_backgroundLayer;

        /// <summary>俳優を配置するためのコンテナ。</summary>
        private readonly VisualElement m_actorContainer;

        /// <summary>UI オーバーレイを配置するためのコンテナ。</summary>
        private readonly VisualElement m_overlayContainer;

        /// <summary>ステージ名を表示するためのラベル。</summary>
        private Label m_stageNameLabel;

        /// <summary>現在のステージ論理サイズ（px）。背景・俳優レイヤーの幅と高さに使用する。</summary>
        private Vector2Int m_stageSize = new Vector2Int(FUnityStageData.DefaultStageWidth, FUnityStageData.DefaultStageHeight);

        /// <summary>
        /// 背景・俳優・オーバーレイの 3 層を初期化し、親へ追加された直後から利用可能な状態にする。
        /// </summary>
        public StageElement()
        {
            name = StageRootName;
            pickingMode = PickingMode.Ignore;
            focusable = false;

            style.flexGrow = 1f;
            style.flexShrink = 0f;
            style.position = Position.Relative;
            style.left = StyleKeyword.Auto;
            style.top = StyleKeyword.Auto;

            AddToClassList(StageRootClassName);

            m_stageViewport = new VisualElement
            {
                name = StageViewportName,
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            m_stageViewport.AddToClassList(StageViewportClassName);
            m_stageViewport.style.position = Position.Relative;
            m_stageViewport.style.overflow = Overflow.Hidden;
            m_stageViewport.style.flexGrow = 0f;
            m_stageViewport.style.flexShrink = 0f;

            m_backgroundLayer = CreateLayer(BackgroundLayerName, BackgroundLayerClassName, PickingMode.Ignore);
            EnsureBackgroundStyleSheet(m_backgroundLayer);
            EnsureBackgroundInlineGuard(m_backgroundLayer);

            m_actorContainer = CreateLayer(ActorContainerName, ActorContainerClassName, PickingMode.Position);
            m_actorContainer.style.justifyContent = Justify.FlexStart;
            m_actorContainer.style.alignItems = Align.FlexStart;

            m_overlayContainer = CreateLayer(OverlayContainerName, OverlayContainerClassName, PickingMode.Position);

            m_stageViewport.Add(m_backgroundLayer);
            m_stageViewport.Add(m_actorContainer);
            m_stageViewport.Add(m_overlayContainer);

            Add(m_stageViewport);

            ApplyStageSize(m_stageSize);

            EnsureLayerOrder();

            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                ApplyStageSize(m_stageSize);
                EnsureLayerOrder();
            });
        }

        /// <summary>背景レイヤーを内部から公開し、Presenter 側での再利用を可能にする。</summary>
        internal VisualElement BackgroundLayer => m_backgroundLayer;

        /// <summary>俳優コンテナを公開する。Presenter から直接 UI ツリーを操作しないように注意する。</summary>
        public VisualElement ActorContainer => m_actorContainer;

        /// <summary>オーバーレイコンテナを公開する。HUD やダイアログなどの拡張に利用する。</summary>
        public VisualElement OverlayContainer => m_overlayContainer;

        /// <summary>ステージビューポートを公開する。背景サービスでのクリッピング対象に利用する。</summary>
        internal VisualElement StageViewport => m_stageViewport;

        /// <summary>現在のステージ論理サイズ（px）。UIScaleService がスケール計算に利用する。</summary>
        public Vector2Int StageSize => m_stageSize;

        /// <summary>
        /// 俳優要素をステージへ追加する。既に別の親に属している場合は移動させる。
        /// </summary>
        /// <param name="actorElement">追加する俳優の UI 要素。</param>
        public void AddActorElement(VisualElement actorElement)
        {
            if (actorElement == null || m_actorContainer == null)
            {
                return;
            }

            if (actorElement.parent != null && actorElement.parent != m_actorContainer)
            {
                actorElement.RemoveFromHierarchy();
            }

            if (actorElement.parent != m_actorContainer)
            {
                m_actorContainer.Add(actorElement);
            }
        }

        /// <summary>
        /// 論理座標（Scratch 中央原点）を UI Toolkit 座標へ変換し、指定要素の left/top に適用するヘルパーです。
        /// </summary>
        /// <param name="child">座標を適用したい子要素。</param>
        /// <param name="logical">論理座標（px）。右が +X、上が +Y。</param>
        /// <param name="activeMode">アクティブなモード設定。null の場合は unityroom と同じ左上原点を仮定します。</param>
        public void SetChildPositionByLogical(VisualElement child, Vector2 logical, FUnityModeConfig activeMode)
        {
            if (child == null)
            {
                return;
            }

            var ui = CoordinateConverter.LogicalToUI(logical, activeMode);
            child.style.position = Position.Absolute;
            child.style.left = ui.x;
            child.style.top = ui.y;
        }

        /// <summary>
        /// 俳優コンテナ内をクリアし、背景レイヤーやオーバーレイには影響を与えないようにする。
        /// </summary>
        public void ClearActors()
        {
            m_actorContainer?.Clear();
        }

        /// <summary>
        /// ステージ設定を反映し、ラベルやツールチップおよびレイヤーサイズを更新する。
        /// </summary>
        /// <param name="stage">適用するステージ設定。null の場合は名称とサイズを既定値へ戻す。</param>
        public void Configure(FUnityStageData stage)
        {
            if (stage == null)
            {
                ApplyStageSize(new Vector2Int(FUnityStageData.DefaultStageWidth, FUnityStageData.DefaultStageHeight));
                UpdateStageName(string.Empty);
                tooltip = string.Empty;
                return;
            }

            ApplyStageSize(stage.StageSize);
            UpdateStageName(stage.StageName);
            tooltip = string.IsNullOrEmpty(stage.StageName) ? string.Empty : stage.StageName;
        }

        /// <summary>
        /// ステージ名ラベルを更新し、空文字の場合は非表示にする。
        /// </summary>
        /// <param name="stageName">表示するステージ名。</param>
        private void UpdateStageName(string stageName)
        {
            if (string.IsNullOrEmpty(stageName))
            {
                if (m_stageNameLabel != null)
                {
                    m_stageNameLabel.text = string.Empty;
                    m_stageNameLabel.style.display = DisplayStyle.None;
                }

                return;
            }

            if (m_stageNameLabel == null)
            {
                m_stageNameLabel = new Label
                {
                    name = StageNameElementName,
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                m_stageNameLabel.AddToClassList(StageNameClassName);
                m_overlayContainer.Add(m_stageNameLabel);
            }

            m_stageNameLabel.text = stageName;
            m_stageNameLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 背景レイヤーへ StageBackground.uss を適用する。読み込みに失敗した場合は警告を出力する。
        /// </summary>
        /// <param name="target">スタイルシートを適用したい要素。</param>
        private static void EnsureBackgroundStyleSheet(VisualElement target)
        {
            if (target == null)
            {
                return;
            }

            if (s_cachedBackgroundStyleSheet == null && !s_backgroundStyleSheetLoaded)
            {
                s_cachedBackgroundStyleSheet = Resources.Load<StyleSheet>(BackgroundStyleSheetResourcePath);
                s_backgroundStyleSheetLoaded = true;

                if (s_cachedBackgroundStyleSheet == null)
                {
                    Debug.LogWarning($"[FUnity.Stage] Resources/{BackgroundStyleSheetResourcePath}.uss が見つからず、背景スケール USS を適用できません。");
                }
            }

            if (s_cachedBackgroundStyleSheet == null)
            {
                return;
            }

            var styleSheets = target.styleSheets;
            for (var i = 0; i < styleSheets.count; i++)
            {
                if (styleSheets[i] == s_cachedBackgroundStyleSheet)
                {
                    return;
                }
            }

            styleSheets.Add(s_cachedBackgroundStyleSheet);
        }

        /// <summary>
        /// ステージレイヤーを生成し、absolute で敷くための基本スタイルを適用する。
        /// </summary>
        /// <param name="elementName">生成する要素の名称。</param>
        /// <param name="className">適用する USS クラス。</param>
        /// <param name="picking">ピッキングモード。</param>
        /// <returns>生成した VisualElement。</returns>
        private static VisualElement CreateLayer(string elementName, string className, PickingMode picking)
        {
            var layer = new VisualElement
            {
                name = elementName,
                pickingMode = picking,
                focusable = false
            };

            layer.AddToClassList(className);
            layer.style.position = Position.Absolute;
            layer.style.left = 0f;
            layer.style.top = 0f;

            return layer;
        }

        /// <summary>
        /// 背景・俳優・オーバーレイの順序を追加順に合わせて補強し、z-index 非対応環境でも描画順を維持する。
        /// </summary>
        private void EnsureLayerOrder()
        {
            if (m_stageViewport == null)
            {
                return;
            }

            if (m_backgroundLayer != null)
            {
                m_backgroundLayer.SendToBack();
            }

            if (m_overlayContainer != null)
            {
                m_overlayContainer.BringToFront();
            }
        }

        /// <summary>
        /// 現在のステージサイズを正規化し、レイヤー各要素へ反映する。
        /// </summary>
        /// <param name="size">適用するステージサイズ（px）。</param>
        private void ApplyStageSize(Vector2Int size)
        {
            var normalized = NormalizeStageSize(size);
            m_stageSize = normalized;

            if (m_stageViewport != null)
            {
                m_stageViewport.style.width = normalized.x;
                m_stageViewport.style.height = normalized.y;
            }

            ApplyLayerSize(m_backgroundLayer, normalized);
            ApplyLayerSize(m_actorContainer, normalized);
            ApplyLayerSize(m_overlayContainer, normalized);
        }

        /// <summary>
        /// 与えられたレイヤーへステージサイズを適用する。null の場合は何もしない。
        /// </summary>
        /// <param name="layer">サイズを更新する対象。</param>
        /// <param name="size">適用するステージサイズ。</param>
        private static void ApplyLayerSize(VisualElement layer, Vector2Int size)
        {
            if (layer == null)
            {
                return;
            }

            layer.style.width = size.x;
            layer.style.height = size.y;
        }

        /// <summary>
        /// ステージサイズを 1px 以上へ丸め、安全な値を返す。
        /// </summary>
        /// <param name="size">正規化前のステージサイズ。</param>
        /// <returns>幅・高さとも 1 以上へ補正した結果。</returns>
        private static Vector2Int NormalizeStageSize(Vector2Int size)
        {
            var width = Mathf.Max(1, size.x);
            var height = Mathf.Max(1, size.y);
            return new Vector2Int(width, height);
        }

        /// <summary>
        /// 背景レイヤーに inline background-size 監視用のガードを設定する。
        /// </summary>
        /// <param name="backgroundLayer">ガードを設定する対象。</param>
        private static void EnsureBackgroundInlineGuard(VisualElement backgroundLayer)
        {
            if (backgroundLayer == null)
            {
                return;
            }

            backgroundLayer.style.backgroundSize = StyleKeyword.Null;

            if (backgroundLayer.ClassListContains(BackgroundInlineGuardClassName))
            {
                return;
            }

            backgroundLayer.AddToClassList(BackgroundInlineGuardClassName);
            backgroundLayer.schedule.Execute(() =>
            {
                backgroundLayer.style.backgroundSize = StyleKeyword.Null;
            }).StartingIn(0);
            backgroundLayer.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                backgroundLayer.style.backgroundSize = StyleKeyword.Null;
            });
            backgroundLayer.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                backgroundLayer.style.backgroundSize = StyleKeyword.Null;
            });
        }
    }
}
