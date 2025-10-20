// Updated: 2025-10-19
using System;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch 系 Unit 共通の補助処理を提供し、Presenter アダプタの自動解決や方向ベクトル計算を行うユーティリティクラスです。
    /// </summary>
    internal static class ScratchUnitUtil
    {
        /// <summary>直近で解決したアダプタを保持する WeakReference です。破棄後は null になります。</summary>
        private static WeakReference<ActorPresenterAdapter> m_CachedAdapter;

        /// <summary>Graph 単位でキャッシュする際に使用する隠しキー名です。</summary>
        private const string kGraphCacheKey = "__FUnity_ActorPresenterAdapter__";

        /// <summary>解決失敗時の警告を重複表示しないためのフラグです。</summary>
        private static bool m_HasLoggedResolutionFailure;

        /// <summary>
        /// Flow 情報と明示引数から <see cref="ActorPresenterAdapter"/> を自動的に解決します。
        /// 優先度: 明示引数 → Graph キャッシュ → 自身の GameObject → Graph Variables → 静的キャッシュ → シーン検索。
        /// </summary>
        /// <param name="flow">現在のフロー情報。null の場合は明示引数と静的キャッシュのみ利用します。</param>
        /// <param name="explicitAdapter">後方互換用に接続されたアダプタ。null の場合は自動探索します。</param>
        /// <returns>解決に成功した <see cref="ActorPresenterAdapter"/>。見つからない場合は null。</returns>
        public static ActorPresenterAdapter ResolveAdapter(Flow flow, ActorPresenterAdapter explicitAdapter = null)
        {
            if (explicitAdapter != null)
            {
                CacheAdapter(explicitAdapter);
                CacheGraph(flow, explicitAdapter);
                return explicitAdapter;
            }

            if (TryGetFromGraphCache(flow, out var fromGraph))
            {
                CacheAdapter(fromGraph);
                return fromGraph;
            }

            var fromSelf = TryGetFromSelf(flow);
            if (fromSelf != null)
            {
                CacheAdapter(fromSelf);
                CacheGraph(flow, fromSelf);
                return fromSelf;
            }

            var fromVariables = TryGetFromGraphVariables(flow);
            if (fromVariables != null)
            {
                CacheAdapter(fromVariables);
                CacheGraph(flow, fromVariables);
                return fromVariables;
            }

            if (TryGetFromStaticCache(out var cached))
            {
                CacheGraph(flow, cached);
                return cached;
            }

            var fromScene = FindAdapterInScene();
            if (fromScene != null)
            {
                CacheAdapter(fromScene);
                CacheGraph(flow, fromScene);
                return fromScene;
            }

            LogResolutionFailureOnce();
            return null;
        }

        /// <summary>
        /// Scratch 方位（0°=右、90°=上、180°=左、270°=下）を UI 座標系（右=+X、下=+Y）へ変換した単位ベクトルを返します。
        /// </summary>
        /// <param name="degrees">変換する角度（度）。</param>
        /// <returns>UI 座標系における進行方向ベクトル。</returns>
        public static Vector2 DirFromDegrees(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
        }

        /// <summary>
        /// 直近の解決結果を WeakReference に保存します。
        /// </summary>
        /// <param name="adapter">キャッシュ対象のアダプタ。</param>
        private static void CacheAdapter(ActorPresenterAdapter adapter)
        {
            if (adapter == null)
            {
                return;
            }

            if (m_CachedAdapter == null)
            {
                m_CachedAdapter = new WeakReference<ActorPresenterAdapter>(adapter);
            }
            else
            {
                m_CachedAdapter.SetTarget(adapter);
            }

            m_HasLoggedResolutionFailure = false;
        }

        /// <summary>
        /// 静的キャッシュからアダプタを取得します。
        /// </summary>
        /// <param name="adapter">取得したアダプタ。</param>
        /// <returns>取得に成功した場合 true。</returns>
        private static bool TryGetFromStaticCache(out ActorPresenterAdapter adapter)
        {
            adapter = null;
            if (m_CachedAdapter == null)
            {
                return false;
            }

            if (!m_CachedAdapter.TryGetTarget(out var target) || target == null)
            {
                return false;
            }

            adapter = target;
            return true;
        }

        /// <summary>
        /// Flow 情報から Graph キャッシュを取得します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <param name="adapter">取得したアダプタ。</param>
        /// <returns>キャッシュが存在し有効な場合 true。</returns>
        private static bool TryGetFromGraphCache(Flow flow, out ActorPresenterAdapter adapter)
        {
            adapter = null;
            var appVariables = GetAppVariables(flow);
            if (appVariables == null)
            {
                return false;
            }

            if (!appVariables.IsDefined(kGraphCacheKey))
            {
                return false;
            }

            if (appVariables.Get(kGraphCacheKey) is ActorPresenterAdapter cached && cached != null)
            {
                adapter = cached;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Flow 情報から Graph Variables を参照し、一般的なキーでアダプタを探します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter TryGetFromGraphVariables(Flow flow)
        {
            if (flow?.stack == null)
            {
                return null;
            }

            var graphVariables = Variables.Graph(flow.stack);
            if (graphVariables == null)
            {
                return null;
            }

            if (graphVariables.IsDefined("adapter"))
            {
                if (graphVariables.Get("adapter") is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            if (graphVariables.IsDefined(nameof(ActorPresenterAdapter)))
            {
                if (graphVariables.Get(nameof(ActorPresenterAdapter)) is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            return null;
        }

        /// <summary>
        /// Flow スタックが紐付く GameObject からアダプタを取得します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>取得したアダプタ。見つからない場合は null。</returns>
        private static ActorPresenterAdapter TryGetFromSelf(Flow flow)
        {
            var owner = flow?.stack?.gameObject;
            if (owner == null)
            {
                return null;
            }

            return owner.GetComponent<ActorPresenterAdapter>();
        }

        /// <summary>
        /// Graph 単位のキャッシュへアダプタを保存します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <param name="adapter">保存するアダプタ。</param>
        private static void CacheGraph(Flow flow, ActorPresenterAdapter adapter)
        {
            var appVariables = GetAppVariables(flow);
            if (appVariables == null || adapter == null)
            {
                return;
            }

            appVariables.Set(kGraphCacheKey, adapter);
        }

        /// <summary>
        /// GraphReference.app に相当する変数領域を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>アプリケーション変数領域。存在しない場合は null。</returns>
        private static Variables GetAppVariables(Flow flow)
        {
            if (flow?.stack == null)
            {
                return null;
            }

            try
            {
                var reference = flow.stack.ToReference();
                return reference?.app;
            }
            catch (InvalidOperationException)
            {
                // Visual Scripting 初期化前などで参照が確立していない場合は例外が発生するため、静かにフォールバックします。
                return null;
            }
        }

        /// <summary>
        /// シーン全体から最初の <see cref="ActorPresenterAdapter"/> を検索します。
        /// </summary>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter FindAdapterInScene()
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindFirstObjectByType<ActorPresenterAdapter>();
#else
            return Object.FindObjectOfType<ActorPresenterAdapter>();
#endif
        }

        /// <summary>
        /// 解決失敗時に一度だけ警告を表示します。
        /// </summary>
        private static void LogResolutionFailureOnce()
        {
            if (m_HasLoggedResolutionFailure)
            {
                return;
            }

            Debug.LogWarning("[FUnity] ActorPresenterAdapter を自動解決できませんでした。対象の GameObject へコンポーネントを追加するか、Graph Variables に adapter を設定してください。");
            m_HasLoggedResolutionFailure = true;
        }
    }
}
