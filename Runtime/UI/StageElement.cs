// Updated: 2025-03-14
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Authoring;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// ステージ全体を表現する UI Toolkit のカスタム要素。背景・俳優コンテナ・オーバーレイの 3 層構造を提供する View レイヤーの土台。
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

        /// <summary>背景 USS のキャッシュ。null のまま読み込み失敗を示す。</summary>
        private static StyleSheet s_cachedBackgroundStyleSheet;

        /// <summary>背景 USS 読み込みを試行したかどうか。</summary>
        private static bool s_backgroundStyleSheetLoaded;

        /// <summary>俳優要素を格納するコンテナの既定名称。</summary>
        internal const string ActorContainerName = "FUnityActorContainer";

        /// <summary>オーバーレイ表示（UI 等）を格納するコンテナの既定名称。</summary>
        internal const string OverlayContainerName = "FUnityOverlayContainer";

        /// <summary>ステージ要素へ付与する USS クラス名。</summary>
        private const string StageRootClassName = "funity-stage";

        /// <summary>俳優コンテナへ付与する USS クラス名。</summary>
        private const string ActorContainerClassName = "funity-stage__actors";

        /// <summary>オーバーレイコンテナへ付与する USS クラス名。</summary>
        private const string OverlayContainerClassName = "funity-stage__overlay";

        /// <summary>Scratch 固定ステージ適用時に付与する USS クラス名。</summary>
        internal const string ScratchStageClassName = "scratch-stage";

        /// <summary>ステージ名ラベルの USS クラス名。</summary>
        private const string StageNameClassName = "funity-stage__name";

        /// <summary>ステージ名ラベルの要素名称。</summary>
        private const string StageNameElementName = "FUnityStageName";

        /// <summary>背景を描画するためのレイヤー。</summary>
        private readonly VisualElement m_BackgroundLayer;

        /// <summary>俳優を配置するためのコンテナ。</summary>
        private readonly VisualElement m_ActorContainer;

        /// <summary>UI オーバーレイを配置するためのコンテナ。</summary>
        private readonly VisualElement m_OverlayContainer;

        /// <summary>ステージ名を表示するためのラベル。</summary>
        private Label m_StageNameLabel;

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

            m_BackgroundLayer = new VisualElement
            {
                name = BackgroundLayerName,
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            m_BackgroundLayer.AddToClassList(BackgroundLayerClassName);
            m_BackgroundLayer.style.position = Position.Absolute;
            m_BackgroundLayer.style.left = 0f;
            m_BackgroundLayer.style.top = 0f;
            m_BackgroundLayer.style.right = 0f;
            m_BackgroundLayer.style.bottom = 0f;
            m_BackgroundLayer.style.flexGrow = 1f;
            m_BackgroundLayer.style.flexShrink = 0f;
            EnsureBackgroundStyleSheet(m_BackgroundLayer);
            m_BackgroundLayer.style.backgroundSize = StyleKeyword.Null;
            if (!m_BackgroundLayer.ClassListContains(BackgroundInlineGuardClassName))
            {
                m_BackgroundLayer.AddToClassList(BackgroundInlineGuardClassName);
                m_BackgroundLayer.schedule.Execute(() =>
                {
                    m_BackgroundLayer.style.backgroundSize = StyleKeyword.Null;
                }).StartingIn(0);
                m_BackgroundLayer.RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    m_BackgroundLayer.style.backgroundSize = StyleKeyword.Null;
                });
                m_BackgroundLayer.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    m_BackgroundLayer.style.backgroundSize = StyleKeyword.Null;
                });
            }
            Add(m_BackgroundLayer);

            m_ActorContainer = new VisualElement
            {
                name = ActorContainerName,
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            m_ActorContainer.AddToClassList(ActorContainerClassName);
            m_ActorContainer.style.position = Position.Relative;
            m_ActorContainer.style.flexGrow = 1f;
            m_ActorContainer.style.flexShrink = 0f;
            m_ActorContainer.style.justifyContent = Justify.FlexStart;
            m_ActorContainer.style.alignItems = Align.FlexStart;
            Add(m_ActorContainer);

            m_OverlayContainer = new VisualElement
            {
                name = OverlayContainerName,
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            m_OverlayContainer.AddToClassList(OverlayContainerClassName);
            m_OverlayContainer.style.position = Position.Relative;
            m_OverlayContainer.style.flexGrow = 0f;
            m_OverlayContainer.style.flexShrink = 0f;
            Add(m_OverlayContainer);
        }

        /// <summary>背景レイヤーを内部から公開し、Presenter 側での再利用を可能にする。</summary>
        internal VisualElement BackgroundLayer => m_BackgroundLayer;

        /// <summary>俳優コンテナを公開する。Presenter から直接 UI ツリーを操作しないように注意する。</summary>
        public VisualElement ActorContainer => m_ActorContainer;

        /// <summary>オーバーレイコンテナを公開する。HUD やダイアログなどの拡張に利用する。</summary>
        public VisualElement OverlayContainer => m_OverlayContainer;

        /// <summary>
        /// 俳優要素をステージへ追加する。既に別の親に属している場合は移動させる。
        /// </summary>
        /// <param name="actorElement">追加する俳優の UI 要素。</param>
        public void AddActorElement(VisualElement actorElement)
        {
            if (actorElement == null || m_ActorContainer == null)
            {
                return;
            }

            if (actorElement.parent != null && actorElement.parent != m_ActorContainer)
            {
                actorElement.RemoveFromHierarchy();
            }

            if (actorElement.parent != m_ActorContainer)
            {
                m_ActorContainer.Add(actorElement);
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
            m_ActorContainer?.Clear();
        }

        /// <summary>
        /// ステージ設定を反映し、ラベルやツールチップを更新する。
        /// </summary>
        /// <param name="stage">適用するステージ設定。null の場合は名称を非表示にする。</param>
        public void Configure(FUnityStageData stage)
        {
            if (stage == null)
            {
                UpdateStageName(string.Empty);
                tooltip = string.Empty;
                return;
            }

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
                if (m_StageNameLabel != null)
                {
                    m_StageNameLabel.text = string.Empty;
                    m_StageNameLabel.style.display = DisplayStyle.None;
                }

                return;
            }

            if (m_StageNameLabel == null)
            {
                m_StageNameLabel = new Label
                {
                    name = StageNameElementName,
                    pickingMode = PickingMode.Ignore,
                    focusable = false
                };
                m_StageNameLabel.AddToClassList(StageNameClassName);
                m_OverlayContainer.Add(m_StageNameLabel);
            }

            m_StageNameLabel.text = stageName;
            m_StageNameLabel.style.display = DisplayStyle.Flex;
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
    }
}
