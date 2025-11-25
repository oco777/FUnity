using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Audio;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「ピッチの効果を○ずつ変える」に相当し、ピッチ効果を相対変更するユニットです。
    /// </summary>
    [UnitTitle("ピッチの効果を増減")]
    [UnitShortTitle("ピッチ+")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ChangePitchEffectByUnit : Unit
    {
        /// <summary>サウンド効果変更を開始するフロー入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>処理完了後に次へ進むフロー出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>加算するピッチ効果量（半音単位）を受け取る入力です。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>value ポートを公開します。</summary>
        public ValueInput Value => m_Value;

        /// <summary>
        /// フロー・値ポートの定義を行い、相互依存関係を設定します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Value = ValueInput<float>("value", 10f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Value, m_Enter);
        }

        /// <summary>
        /// サウンドサービスを取得し、指定量だけピッチ効果を増減します。サービス未設定時は警告のみ出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] ChangePitchEffectByUnit: サウンドサービスが未設定のためピッチ効果を変更できません。");
                return m_Exit;
            }

            var delta = flow.GetValue<float>(m_Value);
            service.PitchEffect = service.PitchEffect + delta;

            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「ピッチの効果を○にする」に相当し、ピッチ効果を絶対設定するユニットです。
    /// </summary>
    [UnitTitle("ピッチの効果を指定")]
    [UnitShortTitle("ピッチ=")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetPitchEffectToUnit : Unit
    {
        /// <summary>サウンド効果設定を開始するフロー入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>処理完了後に次へ進むフロー出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>設定するピッチ効果量（半音単位）を受け取る入力です。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>value ポートを公開します。</summary>
        public ValueInput Value => m_Value;

        /// <summary>
        /// フロー・値ポートの定義を行い、相互依存関係を設定します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Value = ValueInput<float>("value", 0f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Value, m_Enter);
        }

        /// <summary>
        /// サウンドサービスを取得し、ピッチ効果を指定値へ設定します。サービス未設定時は警告のみ出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] SetPitchEffectToUnit: サウンドサービスが未設定のためピッチ効果を設定できません。");
                return m_Exit;
            }

            var value = flow.GetValue<float>(m_Value);
            service.PitchEffect = value;

            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「左右にパンの効果を○ずつ変える」に相当し、パン効果を相対変更するユニットです。
    /// </summary>
    [UnitTitle("左右にパンの効果を増減")]
    [UnitShortTitle("パン+")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ChangePanEffectByUnit : Unit
    {
        /// <summary>サウンド効果変更を開始するフロー入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>処理完了後に次へ進むフロー出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>加算するパン効果量（%）を受け取る入力です。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>value ポートを公開します。</summary>
        public ValueInput Value => m_Value;

        /// <summary>
        /// フロー・値ポートの定義を行い、相互依存関係を設定します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Value = ValueInput<float>("value", 10f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Value, m_Enter);
        }

        /// <summary>
        /// サウンドサービスを取得し、指定量だけパン効果を増減します。サービス未設定時は警告のみ出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] ChangePanEffectByUnit: サウンドサービスが未設定のためパン効果を変更できません。");
                return m_Exit;
            }

            var delta = flow.GetValue<float>(m_Value);
            service.PanEffect = service.PanEffect + delta;

            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「左右にパンの効果を○にする」に相当し、パン効果を絶対設定するユニットです。
    /// </summary>
    [UnitTitle("左右にパンの効果を指定")]
    [UnitShortTitle("パン=")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetPanEffectToUnit : Unit
    {
        /// <summary>サウンド効果設定を開始するフロー入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>処理完了後に次へ進むフロー出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>設定するパン効果量（%）を受け取る入力です。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>value ポートを公開します。</summary>
        public ValueInput Value => m_Value;

        /// <summary>
        /// フロー・値ポートの定義を行い、相互依存関係を設定します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_Value = ValueInput<float>("value", 0f);

            Succession(m_Enter, m_Exit);
            Requirement(m_Value, m_Enter);
        }

        /// <summary>
        /// サウンドサービスを取得し、パン効果を指定値へ設定します。サービス未設定時は警告のみ出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] SetPanEffectToUnit: サウンドサービスが未設定のためパン効果を設定できません。");
                return m_Exit;
            }

            var value = flow.GetValue<float>(m_Value);
            service.PanEffect = value;

            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「音の効果をなくす」に相当し、ピッチ・パン効果を初期値へ戻すユニットです。
    /// </summary>
    [UnitTitle("音の効果をなくす")]
    [UnitShortTitle("効果リセット")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ClearSoundEffectsUnit : Unit
    {
        /// <summary>効果リセットを開始するフロー入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>処理完了後に次へ進むフロー出力です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// フロー定義を行い、リセット処理後に後続へ進むよう設定します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// サウンドサービスを取得し、ピッチ・パン効果をリセットします。サービス未設定時は警告のみ出します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] ClearSoundEffectsUnit: サウンドサービスが未設定のため効果をリセットできません。");
                return m_Exit;
            }

            service.ResetSoundEffects();
            return m_Exit;
        }
    }
}
