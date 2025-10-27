// Updated: 2025-03-14
using UnityEngine.UIElements;
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

        /// <summary>ステージ名ラベルの USS クラス名。</summary>
        private const string StageNameClassName = "funity-stage__name";

        /// <summary>ステージ名ラベルの要素名称。</summary>
        private const string StageNameElementName = "FUnityStageName";

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
    }
}
