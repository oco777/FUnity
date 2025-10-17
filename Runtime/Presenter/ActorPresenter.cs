// Updated: 2025-02-14
using UnityEngine;
using FUnity.Runtime.Core;
using FUnity.Runtime.Model;
using FUnity.Runtime.View;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 俳優の状態（Model）と UI 表示（View）を調停する Presenter。
    /// 入力から得た移動ベクトルを <see cref="ActorState"/> に反映し、View に一方向で通知する。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnityActorData"/>, <see cref="ActorState"/>, <see cref="IActorView"/>
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> が俳優生成時に <see cref="Initialize"/> を呼び出し、
    ///     以降は <see cref="Tick"/> をフレーム毎に実行する。Presenter 自体はステートレスであり、Model/View 間の同期のみを担当。
    /// </remarks>
    public sealed class ActorPresenter
    {
        /// <summary>ランタイム状態を保持する Model。</summary>
        private ActorState m_State;

        /// <summary>UI Toolkit 側の描画を担当する View。</summary>
        private IActorView m_View;

        /// <summary>ポートレート表示用に生成したランタイムスプライト。</summary>
        private Sprite m_RuntimePortrait;

        /// <summary>
        /// モデルとビューを初期化し、初期位置・速度・ポートレートを反映する。
        /// </summary>
        /// <param name="data">静的設定。null の場合は既定値を使用。</param>
        /// <param name="state">既存の状態。null の場合は内部で新規生成する。</param>
        /// <param name="view">表示先の View。</param>
        /// <example>
        /// <code>
        /// presenter.Initialize(actorData, new ActorState(), actorView);
        /// </code>
        /// </example>
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

        /// <summary>
        /// 入力方向と経過時間をもとに Model を更新し、View へ最新座標を反映する。
        /// </summary>
        /// <param name="deltaTime">経過時間（秒）。0 以下の場合は位置更新をスキップ。</param>
        /// <param name="inputDir">入力方向。必要に応じて正規化される。</param>
        /// <example>
        /// <code>
        /// presenter.Tick(Time.deltaTime, move);
        /// </code>
        /// </example>
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

        /// <summary>
        /// <see cref="FUnityActorData.MoveSpeed"/> をクランプして返す。
        /// </summary>
        /// <param name="data">俳優設定。</param>
        /// <returns>0 以上の移動速度。</returns>
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
