// Updated: 2025-10-19
using System;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch 系 Unit 共通の補助処理を提供し、Presenter アダプタの自動解決や方向ベクトル計算を行うユーティリティクラスです。
    /// </summary>
    internal static class ScratchUnitUtil
    {
        /// <summary>直近で解決したアダプタを保持する WeakReference です。破棄後は null になります。</summary>
        private static WeakReference<ActorPresenterAdapter> m_CachedAdapter;

        /// <summary>解決失敗時の警告を重複表示しないためのフラグです。</summary>
        private static bool m_HasLoggedResolutionFailure;

        /// <summary>
        /// 現在のフローがホストしている Runner（Self）に紐づく Object 変数を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。null の場合は null を返します。</param>
        /// <returns>Runner 単位の Object 変数。取得できない場合は null。</returns>
        public static VariableDeclarations GetObjectVars(Flow flow)
        {
            if (flow == null || flow.stack == null)
            {
                return null;
            }

            var host = flow.stack.gameObject;
            if (host == null)
            {
                return null;
            }

            return Variables.Object(host);
        }

        /// <summary>
        /// Runner に格納された presenter 参照を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>presenter 参照。未登録の場合は null。</returns>
        public static object GetPresenter(Flow flow)
        {
            VariableDeclarations objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            return objectVariables.IsDefined("presenter") ? objectVariables.Get("presenter") : null;
        }

        /// <summary>
        /// Runner に格納された view 参照を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>view 参照。未登録の場合は null。</returns>
        public static object GetView(Flow flow)
        {
            VariableDeclarations objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            return objectVariables.IsDefined("view") ? objectVariables.Get("view") : null;
        }

        /// <summary>
        /// Runner に格納された ui 参照を取得します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>ui 参照。未登録の場合は null。</returns>
        public static object GetUI(Flow flow)
        {
            VariableDeclarations objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            return objectVariables.IsDefined("ui") ? objectVariables.Get("ui") : null;
        }

        /// <summary>
        /// Flow 情報から <see cref="ActorPresenterAdapter"/> を自動的に解決します。
        /// 優先度: ScriptGraphAsset Variables → Graph Variables → Object Variables → 自身の GameObject → 静的キャッシュ → シーン検索。
        /// </summary>
        /// <param name="flow">現在のフロー情報。null の場合は静的キャッシュとシーン検索のみ利用します。</param>
        /// <returns>解決に成功した <see cref="ActorPresenterAdapter"/>。見つからない場合は null。</returns>
        public static ActorPresenterAdapter ResolveAdapter(Flow flow)
        {
            var declarations = (flow?.stack?.graph as FlowGraph)?.variables;
            if (declarations != null)
            {
                if (declarations.IsDefined("adapter"))
                {
                    if (declarations.Get("adapter") is ActorPresenterAdapter fromAsset && fromAsset != null)
                    {
                        CacheAdapter(fromAsset);
                        return fromAsset;
                    }
                }
            }

            var fromGraphVariables = TryGetFromGraphVariables(flow);
            if (fromGraphVariables != null)
            {
                CacheAdapter(fromGraphVariables);
                return fromGraphVariables;
            }

            var fromObjectVariables = TryGetFromObjectVariables(flow);
            if (fromObjectVariables != null)
            {
                CacheAdapter(fromObjectVariables);
                return fromObjectVariables;
            }

            var fromSelf = TryGetFromSelf(flow);
            if (fromSelf != null)
            {
                CacheAdapter(fromSelf);
                return fromSelf;
            }

            if (TryGetFromStaticCache(out var cached))
            {
                CacheAdapter(cached);
                return cached;
            }

            var fromScene = FindAdapterInScene();
            if (fromScene != null)
            {
                CacheAdapter(fromScene);
                return fromScene;
            }

            LogResolutionFailureOnce();
            return null;
        }

        /// <summary>
        /// Flow 情報と任意のアダプタから <see cref="ActorPresenter"/> を解決する。
        /// </summary>
        /// <param name="flow">現在のフロー情報。null 許容。</param>
        /// <param name="adapter">優先的に使用する <see cref="ActorPresenterAdapter"/>。null 時は自動解決を試みる。</param>
        /// <returns>解決した <see cref="ActorPresenter"/>。取得できない場合は null。</returns>
        public static ActorPresenter ResolveActorPresenter(Flow flow, ActorPresenterAdapter adapter)
        {
            var resolvedAdapter = adapter ?? ResolveAdapter(flow);
            if (resolvedAdapter != null && resolvedAdapter.Presenter != null)
            {
                return resolvedAdapter.Presenter;
            }

            if (GetPresenter(flow) is ActorPresenter presenterFromVariables && presenterFromVariables != null)
            {
                return presenterFromVariables;
            }

            var bridge = VSPresenterBridge.Instance;
            if (bridge != null && bridge.Target != null)
            {
                return bridge.Target;
            }

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
        /// Flow 情報から Object Variables（Graph をホストする GameObject 単位）を参照し、一般的なキーでアダプタを探します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter TryGetFromGraphVariables(Flow flow)
        {
            var objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            if (objectVariables.IsDefined("adapter"))
            {
                if (objectVariables.Get("adapter") is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            if (objectVariables.IsDefined(nameof(ActorPresenterAdapter)))
            {
                if (objectVariables.Get(nameof(ActorPresenterAdapter)) is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            return null;
        }

        /// <summary>
        /// Object 変数（GameObject 単位）からアダプタを取得します。
        /// </summary>
        /// <param name="flow">現在のフロー。</param>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter TryGetFromObjectVariables(Flow flow)
        {
            var objectVariables = GetObjectVars(flow);
            if (objectVariables == null)
            {
                return null;
            }

            if (objectVariables.IsDefined("adapter"))
            {
                if (objectVariables.Get("adapter") is ActorPresenterAdapter adapter && adapter != null)
                {
                    return adapter;
                }
            }

            if (objectVariables.IsDefined(nameof(ActorPresenterAdapter)))
            {
                if (objectVariables.Get(nameof(ActorPresenterAdapter)) is ActorPresenterAdapter adapter && adapter != null)
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
        /// シーン全体から最初の <see cref="ActorPresenterAdapter"/> を検索します。
        /// </summary>
        /// <returns>見つかったアダプタ。存在しない場合は null。</returns>
        private static ActorPresenterAdapter FindAdapterInScene()
        {
            ActorPresenterAdapter found = null;
#if UNITY_6000_0_OR_NEWER
            found = UnityEngine.Object.FindFirstObjectByType<ActorPresenterAdapter>();
#else
            found = UnityEngine.Object.FindObjectOfType<ActorPresenterAdapter>();
#endif
            return found;
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

            Debug.LogWarning("[FUnity] ActorPresenterAdapter を自動解決できませんでした。ScriptGraphAsset の Variables もしくは対象 GameObject の Object/Graph Variables に adapter を設定してください。");
            m_HasLoggedResolutionFailure = true;
        }
    }
}
