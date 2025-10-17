using UnityEngine;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// Unity Visual Scripting から ActorPresenter を呼び出すためのブリッジ。
    /// </summary>
    public sealed class VSPresenterBridge : MonoBehaviour
    {
        [SerializeField]
        private ActorPresenter m_Target;

        public ActorPresenter Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        public void VS_Move(Vector2 direction, float deltaTime)
        {
            m_Target?.Tick(deltaTime, direction);
        }
    }
}
