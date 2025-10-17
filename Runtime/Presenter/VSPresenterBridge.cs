// Updated: 2025-02-14
using UnityEngine;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// Unity Visual Scripting から <see cref="ActorPresenter"/> を呼び出すための Presenter ブリッジ。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="ActorPresenter"/>, <see cref="Unity.VisualScripting.ScriptMachine"/>（呼び出し元）
    /// 想定ライフサイクル: <see cref="FUnity.Core.FUnityManager"/> が UI GameObject に付与し、Visual Scripting グラフから
    ///     メソッドを叩く。Presenter 層内でのオーケストレーションのみを担い、ビジネスロジックは保持しない。
    /// </remarks>
    public sealed class VSPresenterBridge : MonoBehaviour
    {
        /// <summary>
        /// ブリッジ経由で操作する <see cref="ActorPresenter"/>。
        /// </summary>
        [SerializeField]
        private ActorPresenter m_Target;

        /// <summary>
        /// Visual Scripting から操作可能な Presenter インスタンス。
        /// </summary>
        public ActorPresenter Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        /// <summary>
        /// Visual Scripting から移動入力とデルタ時間を受け取り、Presenter の <see cref="ActorPresenter.Tick"/> を代理実行する。
        /// </summary>
        /// <param name="direction">正規化済みを想定した入力方向。</param>
        /// <param name="deltaTime">経過時間（秒）。</param>
        /// <example>
        /// <code>
        /// VS_Move(new Vector2(1, 0), Time.deltaTime);
        /// </code>
        /// </example>
        public void VS_Move(Vector2 direction, float deltaTime)
        {
            m_Target?.Tick(deltaTime, direction);
        }
    }
}
