// Updated: 2025-03-03
using UnityEngine;
using UnityEngine.UIElements;
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
        /// <summary>Scratch 互換の「1 歩」をピクセル換算する係数（px/歩）。</summary>
        private const float StepToPixels = 10f;

        /// <summary>俳優の初期向き（度）。Scratch 互換で 90°=上。</summary>
        private const float DefaultDirectionDeg = 90f;

        /// <summary>ランタイム状態を保持する Model。</summary>
        private ActorState m_State;

        /// <summary>UI Toolkit 側の描画を担当する View。</summary>
        private IActorView m_View;

        /// <summary>ポートレート表示用に生成したランタイムスプライト。</summary>
        private Sprite m_RuntimePortrait;

        /// <summary>初期サイズ（幅・高さ）。0 の場合は View 側の既定値を利用する。</summary>
        private Vector2 m_BaseSize = Vector2.zero;

        /// <summary>現在のスケール。等倍は 1。</summary>
        private float m_CurrentScale = 1f;

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
            var resolvedState = state ?? new ActorState();
            var shouldInitializeDirection = state == null;

            m_State = resolvedState;
            m_View = view;

            m_State.Position = data != null ? data.InitialPosition : Vector2.zero;
            m_State.Speed = Mathf.Max(0f, data != null ? GetConfiguredSpeed(data) : 300f);
            if (shouldInitializeDirection)
            {
                m_State.DirectionDeg = DefaultDirectionDeg;
            }

            m_BaseSize = data != null ? data.Size : Vector2.zero;
            m_CurrentScale = 1f;

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

                if (m_BaseSize.x > 0f || m_BaseSize.y > 0f)
                {
                    m_View.SetSize(m_BaseSize);
                }

                m_View.SetScale(m_CurrentScale);
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
        /// 絶対座標を直接指定して俳優を移動させる。
        /// Visual Scripting の Custom Event "Actor/SetPosition" から利用することを想定。
        /// </summary>
        /// <param name="position">適用する座標。</param>
        public void SetPosition(Vector2 position)
        {
            SetPositionPixels(position);
        }

        /// <summary>
        /// 現在の座標を取得する。Presenter 未初期化時は原点を返す。
        /// </summary>
        /// <returns>現在の座標（px）。</returns>
        public Vector2 GetPosition()
        {
            if (m_State == null)
            {
                return Vector2.zero;
            }

            return m_State.Position;
        }

        /// <summary>
        /// 現在位置からピクセル単位の差分を加算し、即座に View へ反映する。
        /// Visual Scripting の「〇歩動かす」ブロックからの呼び出しを想定し、Presenter 経由で Model を更新する。
        /// </summary>
        /// <param name="deltaPx">加算する移動量（px）。右+ / 下+ の座標系を想定。</param>
        public void MoveByPixels(Vector2 deltaPx)
        {
            if (m_State == null || m_View == null)
            {
                return;
            }

            m_State.Position += deltaPx;
            m_View.SetPosition(m_State.Position);
        }

        /// <summary>
        /// 現在位置から相対移動させる。
        /// 既存の API 互換性を維持しつつ <see cref="MoveByPixels"/> に処理を委譲する。
        /// </summary>
        /// <param name="delta">移動量。</param>
        public void MoveBy(Vector2 delta)
        {
            AddPositionPixels(delta);
        }

        /// <summary>
        /// 吹き出しを表示する。View 実装側で時間管理を行う。
        /// </summary>
        /// <param name="message">表示するメッセージ。</param>
        /// <param name="seconds">表示時間。</param>
        public void Say(string message, float seconds)
        {
            if (m_View == null)
            {
                return;
            }

            m_View.ShowSpeech(message, Mathf.Max(0.1f, seconds));
        }

        /// <summary>
        /// スケールを設定し、View の Transform に反映する。
        /// </summary>
        /// <param name="scale">等倍=1 としたスケール値。</param>
        public void SetScale(float scale)
        {
            if (m_View == null)
            {
                return;
            }

            m_CurrentScale = Mathf.Max(0.01f, scale);
            m_View.SetScale(m_CurrentScale);
        }

        /// <summary>
        /// 幅と高さを直接指定して View のサイズを変更する。
        /// </summary>
        /// <param name="size">幅・高さ（px）。負値の場合は無視される。</param>
        public void SetSize(Vector2 size)
        {
            if (m_View == null)
            {
                return;
            }

            var clamped = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
            m_BaseSize = clamped;
            m_View.SetSize(clamped);
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

        /// <summary>
        /// VisualTreeAsset を指定して俳優 UI を構築し、必要な PanelSettings を適用する。Runner 側に UIDocument を直接付与せず、Presenter が責務を担う。
        /// </summary>
        /// <param name="owner">UIDocument を保持する GameObject。null の場合は処理を中止する。</param>
        /// <param name="template">俳優のルート要素を記述した UXML。null の場合は警告を出して既存レイアウトを維持する。</param>
        /// <param name="panelSettings">使用する PanelSettings。null の場合はシーン内から既定を探索する。</param>
        public void EnsureActorUI(GameObject owner, VisualTreeAsset template, PanelSettings panelSettings = null)
        {
            if (owner == null)
            {
                Debug.LogWarning("[FUnity] ActorPresenter: owner GameObject が null のため UI を生成できません。");
                return;
            }

            var document = owner.GetComponent<UIDocument>() ?? owner.AddComponent<UIDocument>();

            if (panelSettings != null)
            {
                document.panelSettings = panelSettings;
            }
            else if (document.panelSettings == null)
            {
                var fallback = Object.FindFirstObjectByType<PanelSettings>();
                if (fallback != null)
                {
                    document.panelSettings = fallback;
                }
                else
                {
                    Debug.LogWarning("[FUnity] ActorPresenter: PanelSettings が見つからないため UIDocument に適用できません。");
                }
            }

            if (template != null)
            {
                document.visualTreeAsset = template;
            }
            else
            {
                Debug.LogWarning("[FUnity] ActorPresenter: VisualTreeAsset が null のため UIDocument に適用しません。");
            }
        }

        /// <summary>
        /// Scratch の「〇歩動かす」に相当する距離を現在向きに沿って移動させる。
        /// </summary>
        /// <param name="steps">移動する歩数。負値の場合は逆方向へ移動する。</param>
        public void MoveSteps(float steps)
        {
            if (m_State == null || m_View == null)
            {
                return;
            }

            if (Mathf.Approximately(steps, 0f))
            {
                return;
            }

            var radians = m_State.DirectionDeg * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians));
            var delta = direction * (steps * StepToPixels);

            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            MoveByPixels(delta);
        }

        /// <summary>
        /// 絶対座標をピクセル単位で設定し、View に反映する。
        /// </summary>
        /// <param name="positionPx">適用する座標（px）。右=+X、下=+Y。</param>
        public void SetPositionPixels(Vector2 positionPx)
        {
            if (m_State == null || m_View == null)
            {
                return;
            }

            m_State.Position = positionPx;
            m_View.SetPosition(m_State.Position);
        }

        /// <summary>
        /// 相対的な座標差分をピクセル単位で加算する。
        /// </summary>
        /// <param name="deltaPx">加算する差分（px）。</param>
        public void AddPositionPixels(Vector2 deltaPx)
        {
            MoveByPixels(deltaPx);
        }

        /// <summary>
        /// 現在の向きを取得する。Presenter 未初期化時は既定値の 90 度を返す。
        /// </summary>
        /// <returns>現在の向き（度）。</returns>
        public float GetDirection()
        {
            if (m_State == null)
            {
                return DefaultDirectionDeg;
            }

            return m_State.DirectionDeg;
        }

        /// <summary>
        /// 現在の向きを設定する。View 側は方向を保持しないため、主に移動ベクトル計算に利用する。
        /// </summary>
        /// <param name="degrees">設定する角度（度）。</param>
        public void SetDirection(float degrees)
        {
            if (m_State == null)
            {
                return;
            }

            m_State.DirectionDeg = degrees;
        }
    }
}
