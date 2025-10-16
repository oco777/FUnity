using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;

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
        private VisualElement m_BoundElement;
        private bool m_FloatEnabled = true;
        private float m_Amplitude = 10f;   // px
        private float m_PeriodSec = 3f;    // seconds
        private float m_Phase;
        private Vector3 m_BaseTranslation;
        private float m_CurrentOffset;

        private IVisualElementScheduledItem m_Ticker;

        public VisualElement BoundElement => m_BoundElement;

        private void Awake()
        {
            // Ensure UIDocument and root
            if (!m_UIDocument) m_UIDocument = GetComponent<UIDocument>();
            var root = m_UIDocument ? m_UIDocument.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("[FUnity] FooniController: UIDocument / rootVisualElement is missing.");
                return;
            }

            if (m_BoundElement == null)
            {
                var existing = root.Q<FooniElement>();
                if (existing != null)
                {
                    BindActorElement(existing);
                    Debug.LogWarning("[FUnity] FooniController: auto-bound existing FooniElement (legacy path). Use FUnityManager-driven actors instead.");
                }
                else
                {
                    Debug.LogWarning("[FUnity] FooniController: no actor element bound. Awaiting BindActorElement from FUnityManager.");
                }
            }
        }

        private void OnEnable()
        {
            if (!m_FloatEnabled || m_BoundElement == null)
            {
                return;
            }

            if (m_Ticker == null)
            {
                StartTicker();
            }
            else
            {
                m_Ticker.Resume();
            }
        }

        private void OnDisable()
        {
            m_Ticker?.Pause();
        }

        // ===== Visual Scripting APIs (auto-exposed as units) =====

        /// <summary>Bind an existing actor VisualElement for control.</summary>
        public void BindActorElement(VisualElement ve)
        {
            if (ve == null)
            {
                Debug.LogWarning("[FUnity] FooniController: BindActorElement called with null element.");
                return;
            }

            if (m_BoundElement != ve)
            {
                m_Ticker?.Pause();
                m_Ticker = null;
            }

            m_BoundElement = ve;
            m_BaseTranslation = ResolveBaseTranslation(m_BoundElement);
            m_CurrentOffset = 0f;
            ApplyTranslation();

            if (m_FloatEnabled && isActiveAndEnabled)
            {
                StartTicker();
            }
        }

        /// <summary>Enable/disable floating animation.</summary>
        public void EnableFloat(bool enabled)
        {
            m_FloatEnabled = enabled;

            if (!m_FloatEnabled)
            {
                if (m_BoundElement != null)
                {
                    // Reset translation when disabled (visual reset)
                    m_CurrentOffset = 0f;
                    ApplyTranslation();
                }
                m_Ticker?.Pause();
                return;
            }

            if (m_BoundElement == null)
            {
                Debug.LogWarning("[FUnity] FooniController: EnableFloat called without a bound actor element.");
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

            if (m_FloatEnabled && m_BoundElement != null && m_Ticker != null)
            {
                StartTicker();
            }
        }

        /// <summary>Nudge vertically by delta pixels (instant add).</summary>
        public void NudgeY(float deltaPx)
        {
            if (m_BoundElement == null) return;

            m_BaseTranslation.y += deltaPx;
            ApplyTranslation();
        }

        /// <summary>Emit a "Fooni/Say" custom event with message for VS graphs.</summary>
        public void Say(string message)
        {
            CustomEvent.Trigger(gameObject, "Fooni/Say", message);
            Debug.Log($"Fooni says: {message}");
        }

        // ===== Internal: schedule-based floating =====
        private void StartTicker()
        {
            m_Ticker?.Pause();
            if (m_BoundElement == null) return;

            m_Phase = 0f;
            m_CurrentOffset = 0f;
            m_Ticker = m_BoundElement.schedule.Execute(() =>
            {
                if (!m_FloatEnabled || m_PeriodSec <= 0f) return;

                m_Phase += Time.deltaTime / m_PeriodSec;
                if (m_Phase > 1f) m_Phase -= 1f;

                float offset = Mathf.Sin(m_Phase * Mathf.PI * 2f) * m_Amplitude;
                m_CurrentOffset = offset;
                ApplyTranslation();
            }).Every(16); // ~60 FPS
        }

        private void ApplyTranslation()
        {
            if (m_BoundElement == null)
            {
                return;
            }

            m_BoundElement.style.translate = new Translate(m_BaseTranslation.x, m_BaseTranslation.y + m_CurrentOffset, m_BaseTranslation.z);
        }

        private static Vector3 ResolveBaseTranslation(VisualElement element)
        {
            if (element == null)
            {
                return Vector3.zero;
            }

            var translate = element.style.translate;
            if (translate.keyword == StyleKeyword.Undefined)
            {
                var value = translate.value;
                return new Vector3(value.x.value, value.y.value, value.z);
            }

            var resolved = element.resolvedStyle.translate;
            return new Vector3(resolved.x, resolved.y, resolved.z);
        }
    }
}
