// Updated: 2025-10-19
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units
{
    /// <summary>
    /// <see cref="FooniController.EnableFloat"/> を呼び出して浮遊アニメーションの有効・無効を切り替えるカスタム Unit です。
    /// 制御ポートは enter/exit、値ポートは target/on を使用します。
    /// </summary>
    [UnitTitle("Fooni/Enable Float")]
    [UnitCategory("FUnity/Fooni")]
    public sealed class Fooni_EnableFloatUnit : Unit
    {
        /// <summary>グラフの前段から制御信号を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続ユニットへ制御信号を流す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>処理対象の <see cref="FooniController"/> を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>浮遊を有効化するかどうかのフラグを受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_On;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>on ポートへの参照を公開します。</summary>
        public ValueInput On => m_On;

        /// <summary>
        /// ポートの定義を行います。enter/exit 制御線と target/on 値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");

            m_Target = ValueInput<FooniController>("target", (FooniController)null);
            m_On = ValueInput<bool>("on", true);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 制御信号を受け取った際に FooniController を解決し、<see cref="FooniController.EnableFloat"/> を呼び出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var controller = FooniFloatUnitUtility.ResolveTargetOrFind(flow, m_Target);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Fooni_EnableFloatUnit: target FooniController not found.");
                return m_Exit;
            }

            var shouldEnable = flow.GetValue<bool>(m_On);
            controller.EnableFloat(shouldEnable);
            return m_Exit;
        }

    }

    /// <summary>
    /// <see cref="FooniController.SetFloatAmplitude"/> を呼び出して浮遊振幅(px)を設定するカスタム Unit です。
    /// 制御ポートは enter/exit、値ポートは target/amplitudePx を使用します。
    /// </summary>
    [UnitTitle("Fooni/Set Float Amplitude")]
    [UnitCategory("FUnity/Fooni")]
    public sealed class Fooni_SetFloatAmplitudeUnit : Unit
    {
        /// <summary>グラフの前段から制御信号を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続ユニットへ制御信号を流す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>処理対象の <see cref="FooniController"/> を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>浮遊振幅(px)を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_AmplitudePx;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>amplitudePx ポートへの参照を公開します。</summary>
        public ValueInput AmplitudePx => m_AmplitudePx;

        /// <summary>
        /// ポートの定義を行います。enter/exit 制御線と target/amplitudePx 値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");

            m_Target = ValueInput<FooniController>("target", (FooniController)null);
            m_AmplitudePx = ValueInput<float>("amplitudePx", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 制御信号を受け取った際に FooniController を解決し、振幅設定メソッドを呼び出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var controller = FooniFloatUnitUtility.ResolveTargetOrFind(flow, m_Target);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Fooni_SetFloatAmplitudeUnit: target FooniController not found.");
                return m_Exit;
            }

            var amplitude = flow.GetValue<float>(m_AmplitudePx);
            controller.SetFloatAmplitude(amplitude);
            return m_Exit;
        }

    }

    /// <summary>
    /// <see cref="FooniController.SetFloatPeriod"/> を呼び出して浮遊周期(sec)を設定するカスタム Unit です。
    /// 制御ポートは enter/exit、値ポートは target/periodSec を使用します。
    /// </summary>
    [UnitTitle("Fooni/Set Float Period")]
    [UnitCategory("FUnity/Fooni")]
    public sealed class Fooni_SetFloatPeriodUnit : Unit
    {
        /// <summary>グラフの前段から制御信号を受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続ユニットへ制御信号を流す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>処理対象の <see cref="FooniController"/> を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_Target;

        /// <summary>浮遊周期(sec)を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_PeriodSec;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>target ポートへの参照を公開します。</summary>
        public ValueInput Target => m_Target;

        /// <summary>periodSec ポートへの参照を公開します。</summary>
        public ValueInput PeriodSec => m_PeriodSec;

        /// <summary>
        /// ポートの定義を行います。enter/exit 制御線と target/periodSec 値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");

            m_Target = ValueInput<FooniController>("target", (FooniController)null);
            m_PeriodSec = ValueInput<float>("periodSec", 3f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// enter 制御信号を受け取った際に FooniController を解決し、周期設定メソッドを呼び出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ制御を渡す exit ポート。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var controller = FooniFloatUnitUtility.ResolveTargetOrFind(flow, m_Target);
            if (controller == null)
            {
                Debug.LogWarning("[FUnity] Fooni_SetFloatPeriodUnit: target FooniController not found.");
                return m_Exit;
            }

            var period = flow.GetValue<float>(m_PeriodSec);
            controller.SetFloatPeriod(period);
            return m_Exit;
        }

    }

    /// <summary>
    /// Fooni 関連のカスタム Unit で共通利用する補助メソッドを提供します。
    /// </summary>
    internal static class FooniFloatUnitUtility
    {
        /// <summary>
        /// target ポートから <see cref="FooniController"/> を取得し、未指定の場合はシーン内から最初のインスタンスを探索します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="targetPort">取得対象の ValueInput ポート。</param>
        /// <returns>解決された <see cref="FooniController"/>。未検出時は null。</returns>
        public static FooniController ResolveTargetOrFind(Flow flow, ValueInput targetPort)
        {
            if (flow == null)
            {
                return Object.FindFirstObjectByType<FooniController>();
            }

            if (targetPort == null)
            {
                return Object.FindFirstObjectByType<FooniController>();
            }

            var resolved = flow.GetValue<FooniController>(targetPort);
            if (resolved != null)
            {
                return resolved;
            }

            return Object.FindFirstObjectByType<FooniController>();
        }
    }
}
