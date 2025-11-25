using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Audio;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「終わるまで◯の音を鳴らす」ブロックを再現し、サウンド再生完了まで待機してから後続へ進めるユニットです。
    /// </summary>
    [UnitTitle("終わるまで○の音を鳴らす")]
    [UnitShortTitle("終わるまで○の音を鳴らす")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class PlaySoundUntilDoneUnit : Unit
    {
        /// <summary>フロー入力を受け取り、サウンド再生コルーチンを起動する ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>再生完了後に後続へ接続する ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>再生するサウンド ID を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_SoundId;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>soundId ポートを公開します。</summary>
        public ValueInput SoundId => m_SoundId;

        /// <summary>
        /// ControlInputCoroutine を用いて、サウンド再生完了まで待機するポート定義を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", RunCoroutine);
            m_Exit = ControlOutput("exit");
            m_SoundId = ValueInput<string>("soundId", string.Empty);

            Succession(m_Enter, m_Exit);
            Requirement(m_SoundId, m_Enter);
        }

        /// <summary>
        /// サウンドサービスを介して指定 ID の音を再生し、完了まで待機してから exit を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>サウンド再生を待機する列挙子。</returns>
        private IEnumerator RunCoroutine(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] PlaySoundUntilDoneUnit: サウンドサービスが未設定のため再生できません。");
                yield return m_Exit;
                yield break;
            }

            var soundId = flow.GetValue<string>(m_SoundId);
            if (string.IsNullOrEmpty(soundId))
            {
                Debug.LogWarning("[FUnity.Sound] PlaySoundUntilDoneUnit: soundId が空のため再生できません。");
                yield return m_Exit;
                yield break;
            }

            var routine = service.PlaySoundUntilDone(soundId);
            if (routine != null)
            {
                while (routine.MoveNext())
                {
                    yield return routine.Current;
                }
            }

            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「◯の音を鳴らす」ブロックを再現し、指定サウンドの再生を開始するユニットです。
    /// </summary>
    [UnitTitle("○の音を鳴らす")]
    [UnitShortTitle("○の音を鳴らす")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class PlaySoundUnit : Unit
    {
        /// <summary>フロー入力を受け取り、サウンド再生を開始する ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>再生開始後に後続へ進む ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>再生するサウンド ID を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_SoundId;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>soundId ポートを公開します。</summary>
        public ValueInput SoundId => m_SoundId;

        /// <summary>
        /// サウンド再生を即時実行するポート定義を行います。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");
            m_SoundId = ValueInput<string>("soundId", string.Empty);

            Succession(m_Enter, m_Exit);
            Requirement(m_SoundId, m_Enter);
        }

        /// <summary>
        /// サウンドサービスを介して指定 ID の音を再生し、後続フローへ進めます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] PlaySoundUnit: サウンドサービスが未設定のため再生できません。");
                return m_Exit;
            }

            var soundId = flow.GetValue<string>(m_SoundId);
            if (string.IsNullOrEmpty(soundId))
            {
                Debug.LogWarning("[FUnity.Sound] PlaySoundUnit: soundId が空のため再生できません。");
                return m_Exit;
            }

            service.PlaySound(soundId);
            return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「すべての音を止める」ブロックを再現し、再生中のサウンドを一括停止するユニットです。
    /// </summary>
    [UnitTitle("すべての音を止める")]
    [UnitShortTitle("すべての音を止める")]
    [UnitCategory("FUnity/Blocks/音")]
    [UnitSubtitle("音")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class StopAllSoundsUnit : Unit
    {
        /// <summary>サウンド停止を指示するフロー入力です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>停止処理後に後続へ進む ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートを公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートを公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// 停止処理を行うポート定義を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInput("enter", OnEnter);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// サウンドサービスを介して全サウンドを停止し、後続フローへ進めます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>後続へ接続する ControlOutput。</returns>
        private ControlOutput OnEnter(Flow flow)
        {
            var service = FUnitySoundServiceLocator.Service;
            if (service == null)
            {
                Debug.LogWarning("[FUnity.Sound] StopAllSoundsUnit: サウンドサービスが未設定のため停止できません。");
                return m_Exit;
            }

            service.StopAllSounds();
            return m_Exit;
        }
    }
}
