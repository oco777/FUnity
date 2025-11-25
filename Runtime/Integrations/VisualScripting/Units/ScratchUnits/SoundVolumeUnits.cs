using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Audio;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「音量を○ずつ変える」を再現し、グローバル音量を相対変更するユニットです。
    /// </summary>
    [UnitTitle("音量を増減")]
    [UnitShortTitle("音量+")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ChangeVolumeByUnit : Unit
    {
        /// <summary>音量変更を開始するフロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>処理完了後に後続へ進むフロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>音量変化量（%）を受け取る入力ポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Delta;

        /// <summary>外部へ enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>外部へ exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>外部へ delta ポートを公開します。</summary>
        public ValueInput Delta => m_Delta;

        /// <summary>
        /// フローと値ポートの定義を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Delta = ValueInput<float>("delta", 10f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Delta, m_Enter);
        }

        /// <summary>
        /// 音量を取得し、指定差分を加算してクランプした上で適用します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] ChangeVolumeByUnit: サウンドサービスが未設定のため音量変更できません。");
                return m_Exit;
            }

            var delta = flow.GetValue<float>(m_Delta);
            var next = ClampVolumePercent(service.VolumePercent + delta);

            service.VolumePercent = next;
            service.ApplyVolumeToAllActiveSources();

            return m_Exit;
        }

        /// <summary>
        /// 音量を 0～100% の範囲へ収めます。
        /// </summary>
        /// <param name="value">入力された音量（%）。</param>
        /// <returns>クランプ済みの音量（%）。</returns>
        private float ClampVolumePercent(float value)
        {
            return Mathf.Clamp(value, 0f, 100f);
        }
    }

    /// <summary>
    /// Scratch の「音量を○%にする」を再現し、グローバル音量を絶対設定するユニットです。
    /// </summary>
    [UnitTitle("音量を指定")]
    [UnitShortTitle("音量=")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetVolumeToUnit : Unit
    {
        /// <summary>音量設定を開始するフロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>設定完了後に後続へ進むフロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>目標音量（%）を受け取る入力ポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>外部へ enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>外部へ exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>外部へ value ポートを公開します。</summary>
        public ValueInput Value => m_Value;

        /// <summary>
        /// フローと値ポートの定義を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Value = ValueInput<float>("value", 100f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Value, m_Enter);
        }

        /// <summary>
        /// 入力値をクランプしてマスター音量へ反映します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] SetVolumeToUnit: サウンドサービスが未設定のため音量設定できません。");
                return m_Exit;
            }

            var value = flow.GetValue<float>(m_Value);
            var clamped = ClampVolumePercent(value);

            service.VolumePercent = clamped;
            service.ApplyVolumeToAllActiveSources();

            return m_Exit;
        }

        /// <summary>
        /// 音量を 0～100% に収めます。
        /// </summary>
        /// <param name="value">入力された音量（%）。</param>
        /// <returns>クランプ済みの音量（%）。</returns>
        private float ClampVolumePercent(float value)
        {
            return Mathf.Clamp(value, 0f, 100f);
        }
    }

    /// <summary>
    /// Scratch の「音量」ブロックを再現し、現在のマスター音量（%）を返すユニットです。
    /// </summary>
    [UnitTitle("音量")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(float))]
    public sealed class GetVolumeUnit : Unit
    {
        /// <summary>現在の音量（%）を出力するポートです。</summary>
        [DoNotSerialize]
        private ValueOutput m_Volume;

        /// <summary>外部へ volume ポートを公開します。</summary>
        public ValueOutput Volume => m_Volume;

        /// <summary>
        /// 値ポートの定義を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Volume = ValueOutput<float>("volume", GetVolume);
        }

        /// <summary>
        /// サウンドサービスが保持するマスター音量（%）を返します。サービス未設定時は 100% を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>現在の音量（%）。</returns>
        private float GetVolume(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] GetVolumeUnit: サウンドサービスが未設定のため既定値を返します。");
                return 100f;
            }

            return service.VolumePercent;
        }
    }
}
