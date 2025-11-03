// Updated: 2025-10-21
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「どこかの場所へ行く」ブロックを再現し、ランダムな論理座標へ瞬間移動させる Unit です。
    /// </summary>
    [UnitTitle("Scratch/Go To Random Position")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GoToRandomPositionUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// ランダムな論理座標を生成し、対象俳優を瞬間移動させます。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>exit ポートへ接続する列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Go To Random Position: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var random = new Vector2(
                Random.Range(-ScratchBounds.StageHalfW, ScratchBounds.StageHalfW),
                Random.Range(-ScratchBounds.StageHalfH, ScratchBounds.StageHalfH));
            var clamped = ScratchUnitUtil.ClampToStageBounds(random);
            var ui = adapter.ToUiPosition(clamped);
            adapter.SetPositionPixels(ui);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇秒でどこかの場所へ行く」ブロックを再現し、指定秒数でランダム座標へ移動する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Glide Seconds To Random Position")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GlideSecondsToRandomPositionUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>移動秒数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>seconds ポートを公開します。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と秒数入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Seconds = ValueInput<float>("seconds", 1f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Seconds, m_Enter);
        }

        /// <summary>
        /// ランダムな論理座標を生成し、指定時間で滑らかに移動させます。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>移動完了までの列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide Seconds To Random Position: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var random = new Vector2(
                Random.Range(-ScratchBounds.StageHalfW, ScratchBounds.StageHalfW),
                Random.Range(-ScratchBounds.StageHalfH, ScratchBounds.StageHalfH));
            var seconds = flow.GetValue<float>(m_Seconds);
            return ScratchUnitUtil.GlideActorTo(adapter, random, seconds, m_Exit);
        }
    }

    /// <summary>
    /// Scratch の「マウスのポインターへ行く」ブロックを再現し、カーソル位置へ瞬間移動する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Go To Mouse Pointer")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GoToMousePointerUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// マウスポインターの推定論理座標に瞬間移動します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>exit へ制御を戻す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Go To Mouse Pointer: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var logical = ScratchUnitUtil.GetMouseLogicalPosition(adapter);
            var ui = adapter.ToUiPosition(logical);
            adapter.SetPositionPixels(ui);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇秒でマウスのポインターへ行く」ブロックを再現し、指定時間でカーソルへ移動する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Glide Seconds To Mouse Pointer")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GlideSecondsToMousePointerUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>移動秒数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>seconds ポートを公開します。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と秒数入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Seconds = ValueInput<float>("seconds", 1f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Seconds, m_Enter);
        }

        /// <summary>
        /// マウスポインターの推定論理座標へ、指定時間で移動させます。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>移動処理を行う列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide Seconds To Mouse Pointer: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var logical = ScratchUnitUtil.GetMouseLogicalPosition(adapter);
            var seconds = flow.GetValue<float>(m_Seconds);
            return ScratchUnitUtil.GlideActorTo(adapter, logical, seconds, m_Exit);
        }
    }

    /// <summary>
    /// Scratch の「他のActorへ行く」ブロックを再現し、DisplayName で指定した俳優へ瞬間移動する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Go To Actor By DisplayName")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GoToActorByDisplayNameUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>対象 DisplayName を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DisplayName;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>displayName ポートを公開します。</summary>
        public ValueInput DisplayName => m_DisplayName;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と DisplayName 入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_DisplayName = ValueInput<string>("displayName", string.Empty);

            Succession(m_Enter, m_Exit);
            Requirement(m_DisplayName, m_Enter);
        }

        /// <summary>
        /// DisplayName で俳優を探索し、同じ論理座標へ瞬間移動します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>exit へ接続する列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Go To Actor By DisplayName: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var displayName = flow.GetValue<string>(m_DisplayName);
            if (!ScratchUnitUtil.TryFindActorByDisplayName(displayName, out var target) || target == null)
            {
                Debug.LogWarning($"[FUnity] Scratch/Go To Actor By DisplayName: '{displayName}' に一致する俳優が見つかりません。");
                yield return m_Exit;
                yield break;
            }

            var presenter = target.Presenter;
            if (presenter == null)
            {
                Debug.LogWarning($"[FUnity] Scratch/Go To Actor By DisplayName: '{displayName}' の Presenter が未初期化です。");
                yield return m_Exit;
                yield break;
            }

            var logical = presenter.GetPosition();
            var clamped = ScratchUnitUtil.ClampToStageBounds(logical);
            var ui = adapter.ToUiPosition(clamped);
            adapter.SetPositionPixels(ui);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「〇秒で他のActorへ行く」ブロックを再現し、指定俳優へ滑らかに移動する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Glide Seconds To Actor By DisplayName")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GlideSecondsToActorByDisplayNameUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>移動秒数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>対象 DisplayName を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DisplayName;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>seconds ポートを公開します。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>displayName ポートを公開します。</summary>
        public ValueInput DisplayName => m_DisplayName;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Seconds = ValueInput<float>("seconds", 1f);
            m_DisplayName = ValueInput<string>("displayName", string.Empty);

            Succession(m_Enter, m_Exit);
            Requirement(m_Seconds, m_Enter);
            Requirement(m_DisplayName, m_Enter);
        }

        /// <summary>
        /// DisplayName で俳優を探索し、指定秒数で同じ座標へ滑らかに移動します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>移動処理を行う列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide Seconds To Actor By DisplayName: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var displayName = flow.GetValue<string>(m_DisplayName);
            if (!ScratchUnitUtil.TryFindActorByDisplayName(displayName, out var target) || target == null)
            {
                Debug.LogWarning($"[FUnity] Scratch/Glide Seconds To Actor By DisplayName: '{displayName}' に一致する俳優が見つかりません。");
                yield return m_Exit;
                yield break;
            }

            var presenter = target.Presenter;
            if (presenter == null)
            {
                Debug.LogWarning($"[FUnity] Scratch/Glide Seconds To Actor By DisplayName: '{displayName}' の Presenter が未初期化です。");
                yield return m_Exit;
                yield break;
            }

            var logical = presenter.GetPosition();
            var seconds = flow.GetValue<float>(m_Seconds);
            return ScratchUnitUtil.GlideActorTo(adapter, logical, seconds, m_Exit);
        }
    }

    /// <summary>
    /// Scratch の「〇秒で x 座標を〇に、y 座標を〇に変える」ブロックを再現し、差分移動を時間指定で行う Unit です。
    /// </summary>
    [UnitTitle("Scratch/Glide Seconds By XY Delta")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GlideSecondsByXYDeltaUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>X 座標差分を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaX;

        /// <summary>Y 座標差分を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_DeltaY;

        /// <summary>移動秒数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>dx ポートを公開します。</summary>
        public ValueInput DeltaX => m_DeltaX;

        /// <summary>dy ポートを公開します。</summary>
        public ValueInput DeltaY => m_DeltaY;

        /// <summary>seconds ポートを公開します。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_DeltaX = ValueInput<float>("dx", 0f);
            m_DeltaY = ValueInput<float>("dy", 0f);
            m_Seconds = ValueInput<float>("seconds", 1f);

            Succession(m_Enter, m_Exit);
            Requirement(m_DeltaX, m_Enter);
            Requirement(m_DeltaY, m_Enter);
            Requirement(m_Seconds, m_Enter);
        }

        /// <summary>
        /// 現在位置に差分を加算した目標へ、指定時間で移動します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>移動処理を行う列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide Seconds By XY Delta: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var presenter = adapter.Presenter;
            if (presenter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide Seconds By XY Delta: ActorPresenter が未初期化です。");
                yield return m_Exit;
                yield break;
            }

            var start = presenter.GetPosition();
            var delta = new Vector2(flow.GetValue<float>(m_DeltaX), flow.GetValue<float>(m_DeltaY));
            var target = start + delta;
            var seconds = flow.GetValue<float>(m_Seconds);
            return ScratchUnitUtil.GlideActorTo(adapter, target, seconds, m_Exit);
        }
    }

    /// <summary>
    /// Scratch の「〇秒で x 座標を〇、y 座標を〇にする」ブロックを再現し、絶対座標へ時間指定で移動する Unit です。
    /// </summary>
    [UnitTitle("Scratch/Glide Seconds To X,Y")]
    [UnitCategory("FUnity/Scratch/Motion")]
    public sealed class GlideSecondsToXYUnit : Unit
    {
        /// <summary>フロー開始を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>X 座標を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_X;

        /// <summary>Y 座標を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Y;

        /// <summary>移動秒数を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Seconds;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>x ポートを公開します。</summary>
        public ValueInput X => m_X;

        /// <summary>y ポートを公開します。</summary>
        public ValueInput Y => m_Y;

        /// <summary>seconds ポートを公開します。</summary>
        public ValueInput Seconds => m_Seconds;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線と値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_X = ValueInput<float>("x", 0f);
            m_Y = ValueInput<float>("y", 0f);
            m_Seconds = ValueInput<float>("seconds", 1f);

            Succession(m_Enter, m_Exit);
            Requirement(m_X, m_Enter);
            Requirement(m_Y, m_Enter);
            Requirement(m_Seconds, m_Enter);
        }

        /// <summary>
        /// 指定した論理座標へ、指定秒数で移動します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>移動処理を行う列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Glide Seconds To X,Y: ActorPresenterAdapter が解決できません。");
                yield return m_Exit;
                yield break;
            }

            var logical = new Vector2(flow.GetValue<float>(m_X), flow.GetValue<float>(m_Y));
            var seconds = flow.GetValue<float>(m_Seconds);
            return ScratchUnitUtil.GlideActorTo(adapter, logical, seconds, m_Exit);
        }
    }
}
