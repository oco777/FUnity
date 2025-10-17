using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Input;

namespace FUnity.Runtime.View
{
    /// <summary>
    /// UI Toolkit 上の俳優表示（描画専用の薄い View）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ActorView : MonoBehaviour, IActorView
    {
        [SerializeField]
        private FooniUIBridge m_Bridge;

        private VisualElement m_BoundElement;

        private void Reset()
        {
            if (m_Bridge == null)
            {
                m_Bridge = GetComponent<FooniUIBridge>();
            }
        }

        /// <summary>
        /// FUnityManager からバインド対象を通知するためのセットアップ。
        /// </summary>
        public void Configure(FooniUIBridge bridge, VisualElement element)
        {
            if (bridge != null)
            {
                m_Bridge = bridge;
            }
            else if (m_Bridge == null)
            {
                m_Bridge = GetComponent<FooniUIBridge>();
                if (m_Bridge == null)
                {
                    m_Bridge = gameObject.AddComponent<FooniUIBridge>();
                }
            }

            m_BoundElement = element;
            if (m_Bridge != null && element != null)
            {
                m_Bridge.BindElement(element);
            }
        }

        public void SetPosition(Vector2 pos)
        {
            if (m_Bridge == null)
            {
                return;
            }

            if (!m_Bridge.HasBoundElement && !m_Bridge.TryBind())
            {
                return;
            }

            m_Bridge.SetPosition(pos);
        }

        public void SetPortrait(Sprite sprite)
        {
            if (sprite == null || m_BoundElement == null)
            {
                return;
            }

            var portrait = m_BoundElement.Q<VisualElement>("portrait") ?? m_BoundElement.Q<VisualElement>(className: "portrait");
            if (portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(sprite);
            }
        }
    }
}
