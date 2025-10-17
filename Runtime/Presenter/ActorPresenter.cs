using UnityEngine;
using FUnity.Runtime.Core;
using FUnity.Runtime.Model;
using FUnity.Runtime.View;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 入力で得た情報を ActorState へ反映し、View を更新するプレゼンター。
    /// </summary>
    public sealed class ActorPresenter
    {
        private ActorState m_State;
        private IActorView m_View;
        private Sprite m_RuntimePortrait;

        public void Initialize(FUnityActorData data, ActorState state, IActorView view)
        {
            m_State = state ?? new ActorState();
            m_View = view;

            m_State.Position = data != null ? data.InitialPosition : Vector2.zero;
            m_State.Speed = Mathf.Max(0f, data != null ? GetConfiguredSpeed(data) : 300f);

            if (m_View != null)
            {
                if (m_RuntimePortrait == null && data?.Portrait != null)
                {
                    m_RuntimePortrait = Sprite.Create(
                        data.Portrait,
                        new Rect(0f, 0f, data.Portrait.width, data.Portrait.height),
                        new Vector2(0.5f, 0.5f));
                }

                if (m_RuntimePortrait != null)
                {
                    m_View.SetPortrait(m_RuntimePortrait);
                }

                m_View.SetPosition(m_State.Position);
            }
        }

        public void Tick(float deltaTime, Vector2 inputDir)
        {
            if (m_State == null || m_View == null)
            {
                return;
            }

            if (deltaTime <= 0f)
            {
                m_View.SetPosition(m_State.Position);
                return;
            }

            var direction = inputDir;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            if (direction.sqrMagnitude > 0f && m_State.Speed > 0f)
            {
                m_State.Position += direction * (m_State.Speed * deltaTime);
            }

            m_View.SetPosition(m_State.Position);
        }

        private static float GetConfiguredSpeed(FUnityActorData data)
        {
            if (data == null)
            {
                return 300f;
            }

            return Mathf.Max(0f, data.MoveSpeed);
        }
    }
}
