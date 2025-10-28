using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// UIDocument の <see cref="VisualElement.rootVisualElement"/> をパネル全体へ広げ、背景レイヤーのサイズを自動調整する初期化コンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RootLayoutBootstrapper : MonoBehaviour
    {
        /// <summary>対象となる UIDocument。未設定時は同一 GameObject から自動取得する。</summary>
        [SerializeField]
        private UIDocument m_Document;

        /// <summary>レイアウト調整対象のルート要素。UIDocument の変更に追従して再取得する。</summary>
        private VisualElement m_RootElement;

        /// <summary>GeometryChangedEvent を登録済みかどうかのフラグ。</summary>
        private bool m_GeometryCallbackRegistered;

        /// <summary>UIDocument が未検出の際に重複警告を避けるためのフラグ。</summary>
        private bool m_LoggedMissingDocument;

        /// <summary>
        /// スクリプト初期化時にレイアウト適用を試み、可能であれば UI 全体をパネルへフィットさせる。
        /// </summary>
        private void Awake()
        {
            TryApplyLayout();
        }

        /// <summary>
        /// 有効化時にルートレイアウトを再計算し、GeometryChangedEvent の監視を開始する。
        /// </summary>
        private void OnEnable()
        {
            TryApplyLayout();
        }

        /// <summary>
        /// 無効化時に GeometryChangedEvent の登録を解除し、不要なコールバック実行を防ぐ。
        /// </summary>
        private void OnDisable()
        {
            UnregisterGeometryCallback();
        }

#if UNITY_EDITOR
        /// <summary>
        /// インスペクター上で値が変化した際にもルートレイアウトを適用し、エディタ表示との乖離を防ぐ。
        /// </summary>
        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                TryApplyLayout();
            }
        }
#endif

        /// <summary>
        /// UIDocument とルート要素を取得し、取得に成功した場合はレイアウト調整を実行する。
        /// </summary>
        private void TryApplyLayout()
        {
            if (!TryCacheDocument())
            {
                return;
            }

            ApplyRootLayout(m_RootElement);
            EnsureBackgroundLayerLayout(m_RootElement);
            RegisterGeometryCallback();
        }

        /// <summary>
        /// UIDocument と rootVisualElement をキャッシュし、変化があれば既存のイベント登録を更新する。
        /// </summary>
        /// <returns>キャッシュに成功し、ルート要素が取得できた場合は true。</returns>
        private bool TryCacheDocument()
        {
            if (m_Document == null)
            {
                m_Document = GetComponent<UIDocument>();
            }

            if (m_Document == null)
            {
                if (!m_LoggedMissingDocument)
                {
                    Debug.LogWarning("[FUnity.Root] UIDocument が見つからないため RootLayoutBootstrapper を初期化できません。", this);
                    m_LoggedMissingDocument = true;
                }

                m_RootElement = null;
                UnregisterGeometryCallback();
                return false;
            }

            m_LoggedMissingDocument = false;

            var currentRoot = m_Document.rootVisualElement;
            if (currentRoot == null)
            {
                m_RootElement = null;
                UnregisterGeometryCallback();
                return false;
            }

            if (!ReferenceEquals(m_RootElement, currentRoot))
            {
                UnregisterGeometryCallback();
                m_RootElement = currentRoot;
            }

            return true;
        }

        /// <summary>
        /// GeometryChangedEvent をルートへ登録し、サイズ変更時にもレイアウトが維持されるようにする。
        /// </summary>
        private void RegisterGeometryCallback()
        {
            if (m_RootElement == null || m_GeometryCallbackRegistered)
            {
                return;
            }

            m_RootElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            m_GeometryCallbackRegistered = true;
        }

        /// <summary>
        /// GeometryChangedEvent の登録を解除し、不要な参照を解放する。
        /// </summary>
        private void UnregisterGeometryCallback()
        {
            if (!m_GeometryCallbackRegistered || m_RootElement == null)
            {
                m_GeometryCallbackRegistered = false;
                return;
            }

            m_RootElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            m_GeometryCallbackRegistered = false;
        }

        /// <summary>
        /// ルート要素のジオメトリが変化した際に再度レイアウトを適用し、0 サイズ問題を防止する。
        /// </summary>
        /// <param name="evt">UI Toolkit から通知されるジオメトリ変更イベント。</param>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            TryApplyLayout();
        }

        /// <summary>
        /// rootVisualElement に 100% サイズのスタイルを付与し、パネル全体にフィットさせる。
        /// </summary>
        /// <param name="root">調整対象のルート要素。</param>
        private static void ApplyRootLayout(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            root.style.flexGrow = 1f;
            root.style.flexShrink = 0f;
            root.style.width = new Length(100f, LengthUnit.Percent);
            root.style.height = new Length(100f, LengthUnit.Percent);
            root.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// 背景レイヤーを探索し、フルスクリーン配置と背景画像の適切な適用を保証する。
        /// </summary>
        /// <param name="root">背景レイヤーを内包するルート要素。</param>
        private static void EnsureBackgroundLayerLayout(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            var background = root.Q<VisualElement>("FUnityBackgroundLayer");
            if (background == null)
            {
                background = root.Q<VisualElement>(className: "funity__background-layer");
            }

            if (background == null)
            {
                return;
            }

            background.style.position = Position.Absolute;
            background.style.left = 0f;
            background.style.right = 0f;
            background.style.top = 0f;
            background.style.bottom = 0f;
            background.style.flexGrow = 0f;
            background.style.flexShrink = 0f;
            background.pickingMode = PickingMode.Ignore;

            if (!background.ClassListContains("bg--contain") && !background.ClassListContains("bg--cover"))
            {
                background.AddToClassList("bg--contain");
            }

            background.style.backgroundSize = StyleKeyword.Null;
            background.style.unityBackgroundScaleMode = StyleKeyword.Null;
            var resolvedTexture = background.resolvedStyle.backgroundImage.texture;
            if (resolvedTexture != null)
            {
                background.style.backgroundImage = new StyleBackground(resolvedTexture);
            }
        }
    }
}
