// Updated: 2025-03-18

using FUnity.Runtime.Input;
using FUnity.Runtime.Variables;

namespace FUnity.Runtime.Core
{
    /// <summary>
    /// ランタイムサービスへのグローバルアクセサを提供する静的クラスです。Visual Scripting など
    /// MonoBehaviour 外部のコードが依存注入を受け取れない場合に利用します。
    /// </summary>
    public static class FUnityServices
    {
        /// <summary>変数操作を担当するサービスのインスタンス。</summary>
        private static IFUnityVariableService s_VariableService;

        /// <summary>マウス座標を提供するサービスのインスタンス。</summary>
        private static IMousePositionProvider s_MousePositionProvider;

        /// <summary>
        /// 現在利用可能な変数サービスを返します。未初期化の場合は null を返し、呼び出し側で
        /// ガードする必要があります。
        /// </summary>
        public static IFUnityVariableService Variables
        {
            get => s_VariableService;
            internal set => s_VariableService = value;
        }

        /// <summary>
        /// 現在利用可能なマウス座標プロバイダを返します。未初期化の場合は null を返します。
        /// </summary>
        public static IMousePositionProvider MousePosition
        {
            get => s_MousePositionProvider;
            internal set => s_MousePositionProvider = value;
        }

        /// <summary>
        /// 静的キャッシュを初期状態へ戻します。主にテストや Domain Reload 時の再初期化で使用します。
        /// </summary>
        internal static void ResetAll()
        {
            s_VariableService = null;
            s_MousePositionProvider = null;
        }
    }
}

