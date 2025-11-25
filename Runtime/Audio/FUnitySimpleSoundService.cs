using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FUnity.Runtime.Audio
{
    /// <summary>
    /// <see cref="IFUnitySoundService"/> を簡易実装した MonoBehaviour。単一の AudioSource を使い、
    /// プロジェクトで定義したサウンド ID をキーに再生・停止を行う。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class FUnitySimpleSoundService : MonoBehaviour, IFUnitySoundService
    {
        /// <summary>シーンを跨いでサービスを共有する場合に true。既定で有効。</summary>
        [SerializeField]
        private bool m_DontDestroyOnLoad = true;

        /// <summary>再生に使用する AudioSource。未設定の場合は自動で付与する。</summary>
        [SerializeField]
        private AudioSource m_AudioSource;

        /// <summary>サウンド ID と AudioClip の対応表。Initialize で再構築される。</summary>
        private readonly Dictionary<string, AudioClip> m_SoundMap = new Dictionary<string, AudioClip>();

        /// <summary>
        /// コンポーネント有効化時に AudioSource を保証し、ロケーターへ自身を登録する。
        /// </summary>
        private void Awake()
        {
            if (m_DontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureAudioSource();

            if (FUnitySoundServiceLocator.Service == null)
            {
                FUnitySoundServiceLocator.Service = this;
            }
        }

        /// <summary>
        /// プロジェクトで定義したサウンド一覧を受け取り、辞書を再構築する。
        /// </summary>
        /// <param name="entries">初期化に使用するサウンドエントリの列挙。null の場合は警告のみ出す。</param>
        public void Initialize(IEnumerable<FUnitySoundData.SoundEntry> entries)
        {
            m_SoundMap.Clear();

            if (entries == null)
            {
                Debug.LogWarning("[FUnity.Sound] Initialize に null entries が渡されました。");
                return;
            }

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || entry.clip == null)
                {
                    continue;
                }

                if (m_SoundMap.ContainsKey(entry.id))
                {
                    Debug.LogWarning($"[FUnity.Sound] サウンドID '{entry.id}' が重複しています。最初の定義を使用します。");
                    continue;
                }

                m_SoundMap.Add(entry.id, entry.clip);
            }
        }

        /// <summary>
        /// 指定されたサウンド ID を即時再生する。見つからない場合は警告を出し終了する。
        /// </summary>
        /// <param name="soundId">再生するサウンドの ID。</param>
        public void PlaySound(string soundId)
        {
            if (!TryGetClip(soundId, out var clip))
            {
                return;
            }

            EnsureAudioSource();
            m_AudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// サウンドを再生し、完了まで待機するコルーチンを返す。ID が無効な場合は即時終了する。
        /// </summary>
        /// <param name="soundId">再生するサウンドの ID。</param>
        /// <returns>再生完了まで待機するイテレーター。</returns>
        public IEnumerator PlaySoundUntilDone(string soundId)
        {
            if (!TryGetClip(soundId, out var clip))
            {
                yield break;
            }

            EnsureAudioSource();
            m_AudioSource.Stop();
            m_AudioSource.clip = clip;
            m_AudioSource.Play();

            while (m_AudioSource.isPlaying)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 再生中の全てのサウンドを停止する。AudioSource 未確保時は何も行わない。
        /// </summary>
        public void StopAllSounds()
        {
            if (m_AudioSource == null)
            {
                return;
            }

            m_AudioSource.Stop();
            m_AudioSource.clip = null;
        }

        /// <summary>
        /// 指定 ID に対応する AudioClip を取得する。欠如時は警告を出す。
        /// </summary>
        /// <param name="soundId">検索するサウンド ID。</param>
        /// <param name="clip">見つかった AudioClip。失敗時は null。</param>
        /// <returns>有効なクリップが見つかった場合は true。</returns>
        private bool TryGetClip(string soundId, out AudioClip clip)
        {
            clip = null;

            if (string.IsNullOrEmpty(soundId))
            {
                Debug.LogWarning("[FUnity.Sound] サウンド ID が空です。");
                return false;
            }

            if (!m_SoundMap.TryGetValue(soundId, out var foundClip) || foundClip == null)
            {
                Debug.LogWarning($"[FUnity.Sound] サウンド ID '{soundId}' に対応する AudioClip が見つかりません。");
                return false;
            }

            clip = foundClip;
            return true;
        }

        /// <summary>
        /// AudioSource を確保する。欠如している場合は新規に付与し、playOnAwake を無効化する。
        /// </summary>
        private void EnsureAudioSource()
        {
            if (m_AudioSource != null)
            {
                return;
            }

            m_AudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            m_AudioSource.playOnAwake = false;
        }

        /// <summary>
        /// 破棄時にロケーターをクリアし、次回生成時に参照が更新されるようにする。
        /// </summary>
        private void OnDestroy()
        {
            if (ReferenceEquals(FUnitySoundServiceLocator.Service, this))
            {
                FUnitySoundServiceLocator.Service = null;
            }
        }
    }
}
