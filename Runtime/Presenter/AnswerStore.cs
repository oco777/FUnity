// Updated: 2025-11-12

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 質問への最後の回答文字列を保持するための静的ストアです。
    /// </summary>
    public static class AnswerStore
    {
        /// <summary>直近で入力された回答文字列。未設定時は空文字列。</summary>
        public static string LastAnswer { get; set; } = string.Empty;
    }
}
