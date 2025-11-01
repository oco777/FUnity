// Updated: 2025-02-14
using System.Collections.Generic;
using FUnity.Runtime.Authoring;
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

        [Header("Mode Configuration")]
        /// <summary>現在のゲームモードを示す列挙値。Scratch / unityroom いずれかを選択する。</summary>
        [SerializeField] private FUnityGameMode m_GameMode = FUnityGameMode.Scratch;

        /// <summary>Scratch モードで適用する ModeConfig。null の場合は Scratch 固有設定が無効化される。</summary>
        [SerializeField] private FUnityModeConfig m_ScratchModeConfig;

        /// <summary>unityroom モードで適用する ModeConfig。null の場合は unityroom 固有設定が無効化される。</summary>
        [SerializeField] private FUnityModeConfig m_UnityroomModeConfig;

        /// <summary>読み取り専用のステージ設定。</summary>
        public FUnityStageData Stage => m_stage;

        /// <summary>俳優データのリスト。Presenter 初期化時に順次消費される。</summary>
        public List<FUnityActorData> Actors => m_actors;

        /// <summary>現在選択されているゲームモード。Inspector で切り替え可能。</summary>
        public FUnityGameMode GameMode => m_GameMode;

        /// <summary>
        /// 現在のゲームモードに対応する <see cref="FUnityModeConfig"/> を返す。
        /// 未設定の場合は null を返し、呼び出し元側でフォールバック処理を行う。
        /// </summary>
        /// <returns>選択中のゲームモードに紐づいた ModeConfig。設定が無い場合は null。</returns>
        public FUnityModeConfig GetActiveModeConfig()
        {
            switch (m_GameMode)
            {
                case FUnityGameMode.Unityroom:
                    return m_UnityroomModeConfig;
                case FUnityGameMode.Scratch:
                default:
                    return m_ScratchModeConfig;
            }
        }
    }

    /// <summary>
    /// ゲーム全体の挙動を切り替えるモード種別を表す列挙体。
    /// </summary>
    public enum FUnityGameMode
    {
        /// <summary>Scratch 互換モード。ステージ固定サイズや Scratch 入力に最適化される。</summary>
        Scratch = 0,

        /// <summary>unityroom 公開向けモード。画面全体表示や Web 公開に合わせた設定となる。</summary>
        Unityroom = 1,
    }
}
