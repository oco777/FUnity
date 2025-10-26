using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// UIDocument の rootVisualElement が高さ 0 のままになる問題を防ぐための初期化コンポーネント。
    /// UI Toolkit パネル全体にルート要素をフィットさせ、ジオメトリ確定後にも再確認する。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RootLayoutBootstrapper : MonoBehaviour
    {
        /// <summary>同一ゲームオブジェクト上の UIDocument。未設定時は Awake で自動取得する。</summary>
        [SerializeField] private UIDocument m_Document;

        /// <summary>
        /// 初期化処理。UIDocument を特定し、rootVisualElement をパネルいっぱいに広げる。
        /// </summary>
        private void Awake()
        {
            if (m_Document == null)
            {
                m_Document = GetComponent<UIDocument>();
            }

            if (m_Document == null)
            {
                Debug.LogWarning("[FUnity.Root] UIDocument が見つからないため RootLayoutBootstrapper を初期化できません。");
                return;
            }

            var root = m_Document.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[FUnity.Root] rootVisualElement が null のためレイアウトを調整できません。");
                return;
            }

            ApplyFullscreenLayout(root);
            root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        }

        /// <summary>
        /// 破棄時に GeometryChangedEvent の監視を解除し、不要なコールバックを残さない。
        /// </summary>
        private void OnDestroy()
        {
            if (m_Document == null)
            {
                return;
            }

            var root = m_Document.rootVisualElement;
            if (root != null)
            {
                root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            }
        }

        /// <summary>
        /// GeometryChangedEvent 発生時に root サイズを確認し、高さが決定した段階で最終調整を行う。
        /// </summary>
        /// <param name="evt">UI Toolkit から渡されるレイアウト変更イベント。</param>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            var root = evt.target as VisualElement;
            if (root == null)
            {
                return;
            }

            var resolvedWidth = root.resolvedStyle.width;
            var resolvedHeight = root.resolvedStyle.height;
            if (resolvedHeight <= 0f)
            {
                return;
            }

            ApplyFullscreenLayout(root);
            root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            Debug.Log($"[FUnity.Root] Root size settled: {resolvedWidth} x {resolvedHeight}");
        }

        /// <summary>
        /// rootVisualElement をパネル全体に広げるスタイルを適用する。
        /// </summary>
        /// <param name="root">レイアウトを調整したい UI Toolkit ルート要素。</param>
        private static void ApplyFullscreenLayout(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.style.flexGrow = 1f;
            root.style.flexShrink = 0f;
            root.style.width = Length.Percent(100f);
            root.style.height = Length.Percent(100f);
        }
    }
}
