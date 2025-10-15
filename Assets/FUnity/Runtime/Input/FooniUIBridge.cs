using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity
{
    [RequireComponent(typeof(UIDocument))]
    public class FooniUIBridge : MonoBehaviour
    {
        public string rootName = "root";
        public string rootClass = "actor-root";
        public float defaultSpeed = 300f;
        public bool clampToPanel = true;

        private UIDocument _ui;
        private VisualElement _actorRoot;

        private void Awake()
        {
            _ui = GetComponent<UIDocument>();
        }

        private void Start()
        {
            TryBind();
        }

        public bool TryBind()
        {
            if (_ui == null)
            {
                _ui = GetComponent<UIDocument>();
            }

            if (_ui == null || _ui.rootVisualElement == null)
            {
                return false;
            }

            var root = _ui.rootVisualElement;
            _actorRoot = null;

            if (!string.IsNullOrEmpty(rootName))
            {
                _actorRoot = root.Q<VisualElement>(rootName);
            }

            if (_actorRoot == null && !string.IsNullOrEmpty(rootClass))
            {
                _actorRoot = root.Query<VisualElement>(className: rootClass).First();
            }

            if (_actorRoot != null)
            {
                _actorRoot.focusable = true;
                if (float.IsNaN(_actorRoot.resolvedStyle.left))
                {
                    _actorRoot.style.left = 100;
                }

                if (float.IsNaN(_actorRoot.resolvedStyle.top))
                {
                    _actorRoot.style.top = 100;
                }
            }

            return _actorRoot != null;
        }

        public Vector2 GetPosition()
        {
            if (_actorRoot == null)
            {
                return Vector2.zero;
            }

            return new Vector2(_actorRoot.resolvedStyle.left, _actorRoot.resolvedStyle.top);
        }

        public void SetPosition(Vector2 pos)
        {
            if (_actorRoot == null)
            {
                return;
            }

            var size = new Vector2(_actorRoot.resolvedStyle.width, _actorRoot.resolvedStyle.height);
            var clamped = ClampToPanel(pos, size);
            _actorRoot.style.left = clamped.x;
            _actorRoot.style.top = clamped.y;
        }

        public void Nudge(Vector2 delta)
        {
            if (_actorRoot == null)
            {
                return;
            }

            SetPosition(GetPosition() + delta);
        }

        public void NudgeBySpeed(Vector2 dir, float speed, float deltaTime)
        {
            Nudge(dir * speed * deltaTime);
        }

        public void NudgeByDefaultSpeed(Vector2 dir, float deltaTime)
        {
            NudgeBySpeed(dir, defaultSpeed, deltaTime);
        }

        private Vector2 ClampToPanel(Vector2 pos, Vector2 size)
        {
            if (!clampToPanel || _ui == null || _ui.rootVisualElement == null)
            {
                return pos;
            }

            var panel = _ui.rootVisualElement.resolvedStyle;
            var maxX = Mathf.Max(0, panel.width - size.x);
            var maxY = Mathf.Max(0, panel.height - size.y);
            pos.x = Mathf.Clamp(pos.x, 0, maxX);
            pos.y = Mathf.Clamp(pos.y, 0, maxY);
            return pos;
        }
    }
}
