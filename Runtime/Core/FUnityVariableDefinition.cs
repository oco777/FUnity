// Updated: 2025-03-18
using System;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// FUnity で扱う変数のスコープ種別を表す列挙体です。Scratch の変数定義と同様に、
    /// プロジェクト全体で共有するグローバル変数と俳優固有のローカル変数を区別します。
    /// </summary>
    public enum FUnityVariableScope
    {
        /// <summary>プロジェクト全体で共有される変数を示します。</summary>
        Global,

        /// <summary>特定の俳優専用で、各インスタンスごとに値を保持する変数を示します。</summary>
        Actor
    }

    /// <summary>
    /// Scratch 互換の変数定義を保持するデータクラスです。ProjectData または ActorData に格納され、
    /// ランタイム開始時に <see cref="IFUnityVariableService"/> が初期値と可視状態を構築します。
    /// </summary>
    [Serializable]
    public class FUnityVariableDefinition
    {
        /// <summary>変数名。空文字や null は無効として無視されます。</summary>
        public string Name;

        /// <summary>変数のスコープ。グローバルか俳優ローカルかを指定します。</summary>
        public FUnityVariableScope Scope;

        /// <summary>初期値。Scratch 互換の数値型のみをサポートします。</summary>
        public float InitialValue = 0f;

        /// <summary>起動直後に変数モニターへ表示するかどうか。</summary>
        public bool InitialVisible = false;
    }
}

