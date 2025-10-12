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
        [SerializeField] private UIDocument uiDocument;

        public void SetUIDocument(UIDocument doc)
        {
            uiDocument = doc;
        }

        // Cached references and animation state
        private FooniElement _fooni;
        private bool _floatEnabled = true;
        private float _amplitude = 10f;   // px
        private float _periodSec = 3f;    // seconds
        private float _phase;

        private IVisualElementScheduledItem _ticker;

        private void Awake()
        {
            // Ensure UIDocument and root
            if (!uiDocument) uiDocument = GetComponent<UIDocument>();
            var root = uiDocument ? uiDocument.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError("FooniController: UIDocument / rootVisualElement is missing.");
                return;
            }

            // Try get existing FooniElement; if not found, create one
            _fooni = root.Q<FooniElement>();
            if (_fooni == null)
            {
                _fooni = new FooniElement();
                root.Add(_fooni);
                Debug.Log("FooniController: FooniElement not found. Created a new one.");
            }

            StartTicker();
        }

        private void OnEnable()
        {
            if (_fooni != null) StartTicker();
        }

        private void OnDisable()
        {
            _ticker?.Pause();
        }

        // ===== Visual Scripting APIs (auto-exposed as units) =====

        /// <summary>Enable/disable floating animation.</summary>
        public void EnableFloat(bool enabled)
        {
            _floatEnabled = enabled;

            if (!_floatEnabled)
            {
                if (_fooni != null)
                {
                    // Reset translation when disabled (visual reset)
                    _fooni.style.translate = new Translate(0, 0, 0);
                }
                _ticker?.Pause();
                return;
            }

            if (_ticker == null)
            {
                StartTicker();
            }
            _ticker?.Resume();
        }

        /// <summary>Set floating amplitude in pixels.</summary>
        public void SetFloatAmplitude(float amplitudePx)
        {
            _amplitude = Mathf.Max(0f, amplitudePx);
        }

        /// <summary>Set floating period in seconds.</summary>
        public void SetFloatPeriod(float periodSeconds)
        {
            _periodSec = Mathf.Max(0.1f, periodSeconds);
        }

        /// <summary>Nudge vertically by delta pixels (instant add).</summary>
        public void NudgeY(float deltaPx)
        {
            if (_fooni == null) return;
            float current = _fooni.resolvedStyle.translate.y;
            _fooni.style.translate = new Translate(0, current + deltaPx, 0);
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
            _ticker?.Pause();
            if (_fooni == null) return;

            _phase = 0f;
            _ticker = _fooni.schedule.Execute(() =>
            {
                if (!_floatEnabled || _periodSec <= 0f) return;

                _phase += Time.deltaTime / _periodSec;
                if (_phase > 1f) _phase -= 1f;

                float offset = Mathf.Sin(_phase * Mathf.PI * 2f) * _amplitude;
                _fooni.style.translate = new Translate(0, offset, 0);
            }).Every(16); // ~60 FPS
        }
    }
}
