using System.Collections;

namespace FUnity.Runtime.Audio
{
    /// <summary>
    /// サウンド再生を抽象化するサービスインターフェース。
    /// Visual Scripting の音声ユニットが依存する共通 API を提供する。
    /// </summary>
    public interface IFUnitySoundService
    {
        /// <summary>
        /// 指定 ID のサウンドを即時再生する。ID が無効な場合は何も行わない。
        /// </summary>
        /// <param name="soundId">再生したいサウンドの識別子。</param>
        void PlaySound(string soundId);

        /// <summary>
        /// サウンドを再生し、終了まで待機するコルーチンを返す。
        /// 呼び出し元は StartCoroutine で実行することを想定する。
        /// </summary>
        /// <param name="soundId">再生したいサウンドの識別子。</param>
        /// <returns>完了まで待機するイテレーター。失敗時は空の列挙を返す。</returns>
        IEnumerator PlaySoundUntilDone(string soundId);

        /// <summary>
        /// 再生中のすべてのサウンドを停止する。未再生時は何もしない。
        /// </summary>
        void StopAllSounds();

        /// <summary>
        /// マスター音量（パーセント表記）を取得または設定する。
        /// 0～100 の範囲で解釈し、実際の AudioSource へ反映する際は 0.0～1.0 へ変換する。
        /// </summary>
        float VolumePercent { get; set; }

        /// <summary>
        /// 現在設定されている音量を再生中の AudioSource 群へ適用する。
        /// </summary>
        void ApplyVolumeToAllActiveSources();
    }
}
