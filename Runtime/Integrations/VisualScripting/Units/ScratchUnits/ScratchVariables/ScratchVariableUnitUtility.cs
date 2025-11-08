// Updated: 2025-03-18
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Core;
using FUnity.Runtime.Presenter;
using FUnity.Runtime.Variables;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// 変数系ユニット共通のヘルパーです。サービスや俳優 Presenter の解決を行います。
    /// </summary>
    internal static class ScratchVariableUnitUtility
    {
        /// <summary>変数サービス未設定時の警告重複を防止するフラグ。</summary>
        private static bool s_LoggedMissingService;

        /// <summary>
        /// 変数サービスを取得し、未初期化の場合は警告を出します。
        /// </summary>
        /// <param name="blockTitle">ログに含めるブロック名。</param>
        /// <returns>利用可能な <see cref="IFUnityVariableService"/>。未初期化時は null。</returns>
        public static IFUnityVariableService ResolveService(string blockTitle)
        {
            var service = FUnityServices.Variables;
            if (service == null)
            {
                if (!s_LoggedMissingService)
                {
                    Debug.LogWarning($"[FUnity] Scratch/Variables/{blockTitle}: IFUnityVariableService が初期化されていません。FUnityManager.Awake を確認してください。");
                    s_LoggedMissingService = true;
                }

                return null;
            }

            s_LoggedMissingService = false;
            return service;
        }

        /// <summary>
        /// Flow から俳優 Presenter を解決します。失敗しても null を返し、グローバル変数操作を可能にします。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>解決した <see cref="ActorPresenter"/>。見つからない場合は null。</returns>
        public static ActorPresenter ResolveActorPresenter(Flow flow)
        {
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            return adapter != null ? adapter.Presenter : null;
        }
    }
}
