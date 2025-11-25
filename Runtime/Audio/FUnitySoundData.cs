using System.Collections.Generic;
using UnityEngine;

namespace FUnity.Runtime.Audio
{
    /// <summary>
    /// プロジェクトで利用するサウンド ID と AudioClip の対応をまとめた ScriptableObject。
    /// プロジェクト単位で 1 つだけ生成し、起動時のサウンドサービス初期化に用いる。
    /// </summary>
    [CreateAssetMenu(menuName = "FUnity/Sound/FUnitySoundData", fileName = "FUnitySoundData")]
    public sealed class FUnitySoundData : ScriptableObject
    {
        /// <summary>
        /// サウンド ID と AudioClip のペアを表すシリアライズクラス。
        /// </summary>
        [System.Serializable]
        public sealed class SoundEntry
        {
            /// <summary>サウンドを識別する ID。Visual Scripting からこの文字列で参照する。</summary>
            public string id;

            /// <summary>再生する AudioClip。null の場合は無視される。</summary>
            public AudioClip clip;
        }

        /// <summary>定義済みのサウンド一覧。ID はユニークであることを推奨する。</summary>
        public List<SoundEntry> sounds = new List<SoundEntry>();
    }
}
