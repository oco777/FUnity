using FUnity.Runtime.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// Scratch モード時に UI ルート直下へスケール用の子要素を用意し、ルート worldBound をパネルサイズへ維持するサービスです。
    /// 論理座標は 480x360 に固定し、実ウィンドウが大きい場合のみ子要素を拡大します。
    /// </summary>
    public sealed class UIScaleService
    {
        /// <summary>Scratch モードの論理幅（px）。</summary>
        private const float LogicalWidth = 480f;

        /// <summary>Scratch モードの論理高さ（px）。</summary>
        private const float LogicalHeight = 360f;

        /// <summary>Scratch 表示用コンテンツを内包する要素名称。他クラスから検索できるよう internal に公開する。</summary>
        internal const string ScaledContentRootName = "FUnityScaledContentRoot";

        /// <summary>スケール対象となるルート要素。UIDocument の rootVisualElement を想定する。</summary>
        private VisualElement m_RootElement;

        /// <summary>Scratch 表示時に拡大対象となる子要素。ルート直下へ生成し、論理 480x360 の座標系を保持する。</summary>
        private VisualElement m_ContentRoot;

        /// <summary>GeometryChangedEvent の登録状態を示すフラグ。二重登録を防ぐ。</summary>
        private bool m_GeometryCallbackRegistered;

        /// <summary>現在アクティブなモード設定。Scratch 判定や論理サイズ計算に利用する。</summary>
        private FUnityModeConfig m_ActiveModeConfig;

        /// <summary>
        /// ルート要素を対象にコンテンツルートを保証し、Scratch モード時のみ内側へスケールを適用します。
        /// </summary>
        /// <param name="root">対象となる UIDocument ルート要素。</param>
        /// <param name="activeMode">現在のモード設定。null の場合は Scratch 扱いを無効化します。</param>
        public void Initialize(VisualElement root, FUnityModeConfig activeMode)
        {
            if (root == null)
            {
                Debug.LogWarning("[FUnity.UI] UIScaleService: ルート要素が null のため初期化を中止します。");
                return;
            }

            if (!ReferenceEquals(m_RootElement, root) && m_RootElement != null && m_GeometryCallbackRegistered)
            {
                m_RootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                m_GeometryCallbackRegistered = false;
            }

            m_RootElement = root;
            m_ActiveModeConfig = activeMode;

            EnsureContentRoot();
            ApplyScale();

            if (!m_GeometryCallbackRegistered)
            {
                m_RootElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                m_GeometryCallbackRegistered = true;
            }
        }

        /// <summary>Scratch 表示用に用意したコンテンツルートを外部へ公開する。null の場合は未初期化。</summary>
        public VisualElement ContentRoot => m_ContentRoot;

        /// <summary>
        /// GeometryChangedEvent を解除し、保持している参照を破棄します。Disable 時のクリーンアップ用です。
        /// </summary>
        public void Dispose()
        {
            if (m_RootElement != null && m_GeometryCallbackRegistered)
            {
                m_RootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                m_GeometryCallbackRegistered = false;
            }

            if (m_ContentRoot != null)
            {
                ResetToUnityroomLayout();
            }

            m_RootElement = null;
            m_ContentRoot = null;
            m_ActiveModeConfig = null;
        }

        /// <summary>
        /// Scratch 表示用コンテンツルートを保証する。既に存在する場合は再利用し、未存在なら生成する。
        /// </summary>
        private void EnsureContentRoot()
        {
            if (m_RootElement == null)
            {
                return;
            }

            var existing = m_RootElement.Q<VisualElement>(ScaledContentRootName);
            if (existing != null)
            {
                m_ContentRoot = existing;
                return;
            }

            m_ContentRoot = new VisualElement
            {
                name = ScaledContentRootName,
                pickingMode = PickingMode.Ignore,
                focusable = false
            };

            m_ContentRoot.style.position = Position.Relative;
            m_ContentRoot.style.left = StyleKeyword.Auto;
            m_ContentRoot.style.top = StyleKeyword.Auto;
            m_ContentRoot.style.width = new Length(100f, LengthUnit.Percent);
            m_ContentRoot.style.height = new Length(100f, LengthUnit.Percent);
            m_ContentRoot.style.flexGrow = 1f;
            m_ContentRoot.style.flexShrink = 0f;
            m_ContentRoot.transform.scale = Vector3.one;

            m_RootElement.Add(m_ContentRoot);
        }

        /// <summary>
        /// 現在のルートサイズとモード設定から子要素へのスケール値を計算し、表示位置を中央へ揃える。
        /// </summary>
        private void ApplyScale()
        {
            if (m_RootElement == null || m_ContentRoot == null)
            {
                return;
            }

            if (m_ActiveModeConfig == null || m_ActiveModeConfig.Mode != FUnityAuthoringMode.Scratch)
            {
                ResetToUnityroomLayout();
                return;
            }

            var resolvedWidth = m_RootElement.worldBound.width;
            var resolvedHeight = m_RootElement.worldBound.height;
            if (resolvedWidth <= 0f || resolvedHeight <= 0f)
            {
                return;
            }

            var scaleX = resolvedWidth / LogicalWidth;
            var scaleY = resolvedHeight / LogicalHeight;
            var scale = Mathf.Max(1f, Mathf.Min(scaleX, scaleY));

            var scaledWidth = LogicalWidth * scale;
            var scaledHeight = LogicalHeight * scale;
            var offsetX = (resolvedWidth - scaledWidth) * 0.5f;
            var offsetY = (resolvedHeight - scaledHeight) * 0.5f;

            m_ContentRoot.style.position = Position.Absolute;
            m_ContentRoot.style.left = offsetX;
            m_ContentRoot.style.top = offsetY;
            m_ContentRoot.style.width = LogicalWidth;
            m_ContentRoot.style.height = LogicalHeight;
            m_ContentRoot.transform.scale = new Vector3(scale, scale, 1f);

            m_RootElement.transform.scale = Vector3.one;
        }

        /// <summary>
        /// unityroom 等の等倍モード用に、コンテンツルートをルートと同じレイアウトへ戻す。
        /// </summary>
        private void ResetToUnityroomLayout()
        {
            m_ContentRoot.style.position = Position.Relative;
            m_ContentRoot.style.left = StyleKeyword.Auto;
            m_ContentRoot.style.top = StyleKeyword.Auto;
            m_ContentRoot.style.width = new Length(100f, LengthUnit.Percent);
            m_ContentRoot.style.height = new Length(100f, LengthUnit.Percent);
            m_ContentRoot.transform.scale = Vector3.one;

            if (m_RootElement != null)
            {
                m_RootElement.transform.scale = Vector3.one;
            }
        }

        /// <summary>
        /// ルートサイズ変化時に再スケールを適用するための GeometryChangedEvent コールバック。
        /// </summary>
        /// <param name="evt">UI Toolkit から通知されるジオメトリ変更イベント。</param>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            ApplyScale();
        }
    }
}
