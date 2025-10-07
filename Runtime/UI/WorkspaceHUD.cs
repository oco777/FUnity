using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.UI
{
    /// <summary>
    /// UI Toolkit 要素をバインドしてシンプルなランタイム操作を提供します。
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class WorkspaceHUD : MonoBehaviour
    {
        private UIDocument m_document;
        private Button m_runButton;

        /// <summary>
        /// 必要な UIDocument コンポーネントを取得します。
        /// </summary>
        private void Awake()
        {
            m_document = GetComponent<UIDocument>();
        }

        /// <summary>
        /// ルート要素と実行ボタンの参照を設定します。
        /// </summary>
        private void OnEnable()
        {
            var root = m_document?.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("[FUnity] WorkspaceHUD missing UIDocument root.");
                return;
            }

            m_runButton = root.Q<Button>("run-button");
            if (m_runButton != null)
            {
                m_runButton.clicked += OnRunClicked;
            }
        }

        /// <summary>
        /// 実行ボタンのイベント登録を解除します。
        /// </summary>
        private void OnDisable()
        {
            if (m_runButton != null)
            {
                m_runButton.clicked -= OnRunClicked;
                m_runButton = null;
            }
        }

        /// <summary>
        /// ワークスペースの実行要求をログに出力します。
        /// </summary>
        private static void OnRunClicked()
        {
            Debug.Log("[FUnity] Run workspace requested.");
        }
    }
}
