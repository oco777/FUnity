using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    /// <summary>
    /// Bridge MonoBehaviour so Unity Visual Scripting can control FooniElement (UI Toolkit).
    /// Exposes simple public APIs that become VS units automatically.
    /// </summary>
    public class FooniController : MonoBehaviour
    {
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiDocument")]
        private UIDocument m_UIDocument;

        public void SetUIDocument(UIDocument doc)
        {
            m_UIDocument = doc;
        }

        // Cached references and animation state
        private FooniElement m_Fooni;
        private bool m_FloatEnabled = true;
        private float m_Amplitude = 10f;   // px
        private float m_PeriodSec = 3f;    // seconds
        private float m_Phase;

        private IVisualElementScheduledItem m_Ticker;

        private void Awake()
        {
            // Ensure UIDocument and root
            if (!m_UIDocument) m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument ? m_UIDocument.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("FooniController: UIDocument / rootVisualElement is missing.");
                return;
            }

            // Try get existing FooniElement; if not found, create one
            m_Fooni = root.Q<FooniElement>();
            if (m_Fooni == null)
            {
                m_Fooni = new FooniElement();
                root.Add(m_Fooni);
                Debug.Log("FooniController: FooniElement not found. Created a new one.");
            }

            StartTicker();
        }

        private void OnEnable()
        {
            if (m_Fooni != null) StartTicker();
        }

        private void OnDisable()
        {
            m_Ticker?.Pause();
        }

        // ===== Visual Scripting APIs (auto-exposed as units) =====

        /// <summary>Enable/disable floating animation.</summary>
        public void EnableFloat(bool enabled)
        {
            m_FloatEnabled = enabled;

            if (!m_FloatEnabled)
            {
                if (m_Fooni != null)
                {
                    // Reset translation when disabled (visual reset)
                    m_Fooni.style.translate = new Translate(0, 0, 0);
                }
                m_Ticker?.Pause();
                return;
            }

            if (m_Ticker == null)
            {
                StartTicker();
            }
            m_Ticker?.Resume();
        }

        /// <summary>Set floating amplitude in pixels.</summary>
        public void SetFloatAmplitude(float amplitudePx)
        {
            m_Amplitude = Mathf.Max(0f, amplitudePx);
        }

        /// <summary>Set floating period in seconds.</summary>
        public void SetFloatPeriod(float periodSeconds)
        {
            m_PeriodSec = Mathf.Max(0.1f, periodSeconds);
        }

        /// <summary>Nudge vertically by delta pixels (instant add).</summary>
        public void NudgeY(float deltaPx)
        {
            if (m_Fooni == null) return;
            float current = m_Fooni.resolvedStyle.translate.y;
            m_Fooni.style.translate = new Translate(0, current + deltaPx, 0);
        }

        /// <summary>Emit a "Fooni/Say" custom event with message for VS graphs.</summary>
        public void Say(string message)
        {
#if UNITY_VISUAL_SCRIPTING
            Unity.VisualScripting.CustomEvent.Trigger(gameObject, "Fooni/Say", message);
#endif
            Debug.Log($"Fooni says: {message}");
        }

        // ===== Internal: schedule-based floating =====
        private void StartTicker()
        {
            m_Ticker?.Pause();
            if (m_Fooni == null) return;

            m_Phase = 0f;
            m_Ticker = m_Fooni.schedule.Execute(() =>
            {
                if (!m_FloatEnabled || m_PeriodSec <= 0f) return;

                m_Phase += Time.deltaTime / m_PeriodSec;
                if (m_Phase > 1f) m_Phase -= 1f;

                float offset = Mathf.Sin(m_Phase * Mathf.PI * 2f) * m_Amplitude;
                m_Fooni.style.translate = new Translate(0, offset, 0);
            }).Every(16); // ~60 FPS
        }
    }
}
