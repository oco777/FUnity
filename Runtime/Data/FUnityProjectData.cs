// Updated: 2025-02-14
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// プロジェクト全体のステージ・俳優・Visual Scripting Runner 設定をまとめた Model レイヤーの ScriptableObject。
    /// </summary>
    /// <remarks>
    /// 依存関係: <see cref="FUnity.Core.FUnityManager"/>（ランタイム初期化時に読み取る）
    /// 想定ライフサイクル: プロジェクト構築時にアセットを作成し、ランタイムでは参照のみ。
    /// Presenter から View へ渡されるデータの静的ソースとして機能する。
    /// </remarks>
    [CreateAssetMenu(menuName = "FUnity/Project Data", fileName = "FUnityProjectData")]
    public sealed class FUnityProjectData : ScriptableObject
    {
        [System.Serializable]
        public class RunnerEntry
        {
            /// <summary>生成する Runner GameObject 名称。</summary>
            public string name = "FUnity VS Runner";

            /// <summary>Unity Visual Scripting 用マクロ。null の場合は空グラフとして生成される。</summary>
            public ScriptGraphAsset macro;

            [System.Serializable]
            public class ObjectVar
            {
                /// <summary>Visual Scripting Object 変数のキー名。</summary>
                public string key;

                /// <summary>変数へ設定する参照。Presenter や UI などを注入する。</summary>
                public Object value;
            }

            /// <summary>Runner 生成時に設定する Object 変数の一覧。</summary>
            public List<ObjectVar> objectVariables = new List<ObjectVar>();
        }

        [Header("Runtime setup")]
        /// <summary>
        /// `true` の場合、<see cref="FUnity.Core.FUnityManager"/> が UI ドキュメントと必須コンポーネントを自動生成する。
        /// </summary>
        public bool ensureFUnityUI = true;

        [Header("Visual Scripting Runners")]
        /// <summary>
        /// ランタイム開始時に生成する Visual Scripting Runner の定義リスト。
        /// </summary>
        public List<RunnerEntry> runners = new List<RunnerEntry>();

        [Header("Stage & Actors")]
        /// <summary>利用するステージデータ。null の場合は背景設定がスキップされる。</summary>
        [SerializeField] private FUnityStageData m_stage;

        /// <summary>生成する俳優の静的設定コレクション。</summary>
        [SerializeField] private List<FUnityActorData> m_actors = new List<FUnityActorData>();

        /// <summary>読み取り専用のステージ設定。</summary>
        public FUnityStageData Stage => m_stage;

        /// <summary>俳優データのリスト。Presenter 初期化時に順次消費される。</summary>
        public List<FUnityActorData> Actors => m_actors;
    }
}
