// Updated: 2025-03-28
using System;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「色の効果を ◯ ずつ変える」に対応し、Presenter 経由で色相回転を相対適用するカスタム Unit です。
    /// 対象の <see cref="ActorPresenterAdapter"/> は <see cref="ScratchUnitUtil.ResolveAdapter(Flow)"/> により自動解決します。
    /// </summary>
    [UnitTitle("色の効果を○ずつ変える")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ChangeColorEffectByUnit : Unit
    {
        /// <summary>ログ出力時に利用するユニット名です。</summary>
        private const string UnitName = "色の効果を○ずつ変える";

        /// <summary>フロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>フロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>加算する色効果量を受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Delta;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>delta ポートへの参照を公開します。</summary>
        public ValueInput Delta => m_Delta;

        /// <summary>
        /// ポートを定義し、enter→exit の制御線と delta の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Delta = ValueInput<float>("delta", 10f);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// フローを受信した際に ActorPresenterAdapter を解決し、色効果に差分を適用します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var delta = flow.GetValue<float>(m_Delta);
            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning("[FUnity] Scratch/Change Color Effect by: ActorPresenterAdapter が未解決のため色の効果を変更できません。VSPresenterBridge などでアダプタを登録してください。");
                yield return m_Exit;
                yield break;
            }

            adapter.ChangeColorEffect(delta);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「色の効果を ◯ にする」に対応し、指定値を絶対適用するカスタム Unit です。
    /// </summary>
    [UnitTitle("色の効果を○にする")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetColorEffectToUnit : Unit
    {
        /// <summary>ログ出力に使用するユニット名です。</summary>
        private const string UnitName = "色の効果を○にする";

        /// <summary>フロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>フロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>設定する色効果値を受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_Value;

        /// <summary>対象俳優を識別する DisplayName または Self を受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_ActorKey;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>value ポートへの参照を公開します。</summary>
        public ValueInput Value => m_Value;

        /// <summary>actor ポートへの参照を公開します。</summary>
        public ValueInput Actor => m_ActorKey;

        /// <summary>
        /// ポートを定義し、enter→exit の制御線と value/actor の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_Value = ValueInput<float>("value", 0f);
            m_ActorKey = ValueInput<string>("actor", "Self");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// フロー受信時に対象俳優を解決し、色効果を絶対値で設定します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var value = flow.GetValue<float>(m_Value);
            var actorKey = flow.GetValue<string>(m_ActorKey);
            var adapter = GraphicEffectUnitUtil.ResolveAdapter(flow, actorKey, UnitName, out var usedSelf);
            if (adapter == null)
            {
                if (usedSelf)
                {
                    Debug.LogWarning("[FUnity] Scratch/Set Color Effect to: ActorPresenterAdapter が未解決のため色の効果を設定できません。VSPresenterBridge などでアダプタを登録してください。");
                }

                yield return m_Exit;
                yield break;
            }

            adapter.SetColorEffect(value);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「画像効果をなくす」に対応し、Tint を初期状態へ戻すカスタム Unit です。
    /// </summary>
    [UnitTitle("画像効果をなくす")]
    [UnitCategory("FUnity/Blocks/見た目")]
    [UnitSubtitle("見た目")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class ClearGraphicEffectsUnit : Unit
    {
        /// <summary>ログ出力に使用するユニット名です。</summary>
        private const string UnitName = "画像効果をなくす";

        /// <summary>フロー入力ポートです。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>フロー出力ポートです。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>対象俳優を識別する DisplayName または Self を受け取るポートです。</summary>
        [DoNotSerialize]
        private ValueInput m_ActorKey;

        /// <summary>enter ポートへの参照を公開します。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照を公開します。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>actor ポートへの参照を公開します。</summary>
        public ValueInput Actor => m_ActorKey;

        /// <summary>
        /// ポートを定義し、enter→exit の制御線と actor の値入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_ActorKey = ValueInput<string>("actor", "Self");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// フロー受信時に対象俳優を解決し、画像効果を既定値へ戻します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            var actorKey = flow.GetValue<string>(m_ActorKey);
            var adapter = GraphicEffectUnitUtil.ResolveAdapter(flow, actorKey, UnitName, out var usedSelf);
            if (adapter == null)
            {
                if (usedSelf)
                {
                    Debug.LogWarning("[FUnity] Scratch/Clear Graphic Effects: ActorPresenterAdapter が未解決のため画像効果をリセットできません。VSPresenterBridge などでアダプタを登録してください。");
                }

                yield return m_Exit;
                yield break;
            }

            adapter.ClearGraphicEffects();
            yield return m_Exit;
        }
    }

    /// <summary>
    /// グラフィック効果系ユニットの共通処理を提供するユーティリティです。
    /// </summary>
    internal static class GraphicEffectUnitUtil
    {
        /// <summary>
        /// actorKey から <see cref="ActorPresenterAdapter"/> を解決します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="actorKey">Self または DisplayName。</param>
        /// <param name="unitName">ログに表示するユニット名。</param>
        /// <param name="usedSelf">Self 解決を試みたかどうか。</param>
        /// <returns>解決したアダプタ。見つからない場合は null。</returns>
        public static ActorPresenterAdapter ResolveAdapter(Flow flow, string actorKey, string unitName, out bool usedSelf)
        {
            if (string.IsNullOrWhiteSpace(actorKey))
            {
                usedSelf = true;
                return ScratchUnitUtil.ResolveAdapter(flow);
            }

            var normalized = actorKey.Trim();
            if (string.Equals(normalized, "Self", StringComparison.OrdinalIgnoreCase))
            {
                usedSelf = true;
                return ScratchUnitUtil.ResolveAdapter(flow);
            }

            usedSelf = false;

            if (!ScratchUnitUtil.TryFindActorByDisplayName(normalized, out var adapter) || adapter == null)
            {
                Debug.LogWarning($"[FUnity] {unitName}: '{normalized}' に一致する俳優が見つかりません。DisplayName を正しく指定してください。");
                return null;
            }

            return adapter;
        }
    }
}
