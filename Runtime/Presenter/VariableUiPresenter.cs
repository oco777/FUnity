// Updated: 2025-03-18
using System.Collections.Generic;
using System.Globalization;
using FUnity.Runtime.Core;
using FUnity.Runtime.Variables;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 変数モニター UI を管理する Presenter のインターフェースです。値変更と可視状態の通知を受け、
    /// UI Toolkit 上のラベルへ反映します。
    /// </summary>
    public interface IVariableUiPresenter
    {
        /// <summary>サービス初期化時に現在の変数一覧を受け取ります。</summary>
        void Initialize(
            IFUnityVariableService service,
            IEnumerable<FUnityVariableService.VariableState> globalStates,
            IEnumerable<FUnityVariableService.VariableState> actorStates);

        /// <summary>特定の変数値が更新された際に呼び出されます。</summary>
        void OnValueChanged(FUnityVariableService.VariableState state);

        /// <summary>特定の変数の表示状態が変更された際に呼び出されます。</summary>
        void OnVisibilityChanged(FUnityVariableService.VariableState state);
    }

    /// <summary>
    /// Scratch の変数モニター風 UI を Stage のオーバーレイへ描画する Presenter 実装です。
    /// </summary>
    public sealed class VariableUiPresenter : IVariableUiPresenter
    {
        /// <summary>モニターコンテナの要素名。</summary>
        private const string ContainerElementName = "FUnityVariableMonitor";

        /// <summary>モニターコンテナへ付与する USS クラス名。</summary>
        private const string ContainerClassName = "funity-variable-monitor";

        /// <summary>各ラベルへ付与する USS クラス名。</summary>
        private const string EntryClassName = "funity-variable-monitor__entry";

        /// <summary>値の文字列表現で使用するカルチャ。</summary>
        private static readonly CultureInfo s_Culture = CultureInfo.InvariantCulture;

        /// <summary>ラベルのキャッシュ。変数状態をキーに保持します。</summary>
        private readonly Dictionary<FUnityVariableService.VariableState, Label> m_LabelMap
            = new Dictionary<FUnityVariableService.VariableState, Label>();

        /// <summary>ラベルを格納するルート要素。</summary>
        private VisualElement m_Container;

        /// <summary>配置先の親要素。Stage のオーバーレイを想定します。</summary>
        private VisualElement m_Parent;

        /// <summary>
        /// 変数モニターを配置する親要素を設定します。再設定時は既存コンテナを移動します。
        /// </summary>
        /// <param name="parent">配置先の VisualElement。null の場合はコンテナを一時的に未配置にします。</param>
        public void SetRoot(VisualElement parent)
        {
            m_Parent = parent;
            EnsureContainer();
            AttachContainer();
        }

        /// <inheritdoc />
        public void Initialize(
            IFUnityVariableService service,
            IEnumerable<FUnityVariableService.VariableState> globalStates,
            IEnumerable<FUnityVariableService.VariableState> actorStates)
        {
            EnsureContainer();

            m_LabelMap.Clear();
            m_Container?.Clear();

            if (globalStates != null)
            {
                foreach (var state in globalStates)
                {
                    CreateOrUpdateEntry(state);
                }
            }

            if (actorStates != null)
            {
                foreach (var state in actorStates)
                {
                    CreateOrUpdateEntry(state);
                }
            }
        }

        /// <inheritdoc />
        public void OnValueChanged(FUnityVariableService.VariableState state)
        {
            if (state == null)
            {
                return;
            }

            if (!m_LabelMap.TryGetValue(state, out var _))
            {
                CreateOrUpdateEntry(state);
                return;
            }

            UpdateEntry(state);
        }

        /// <inheritdoc />
        public void OnVisibilityChanged(FUnityVariableService.VariableState state)
        {
            if (state == null)
            {
                return;
            }

            if (!m_LabelMap.TryGetValue(state, out var label))
            {
                CreateOrUpdateEntry(state);
                return;
            }

            label.style.display = state.Visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>コンテナを生成し、基本スタイルを適用します。</summary>
        private void EnsureContainer()
        {
            if (m_Container != null)
            {
                return;
            }

            m_Container = new VisualElement
            {
                name = ContainerElementName,
                pickingMode = PickingMode.Ignore
            };

            m_Container.AddToClassList(ContainerClassName);
            m_Container.style.position = Position.Absolute;
            m_Container.style.left = 16f;
            m_Container.style.top = 16f;
            m_Container.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);
            m_Container.style.paddingLeft = 8f;
            m_Container.style.paddingRight = 8f;
            m_Container.style.paddingTop = 8f;
            m_Container.style.paddingBottom = 8f;
            m_Container.style.borderBottomLeftRadius = 6f;
            m_Container.style.borderBottomRightRadius = 6f;
            m_Container.style.borderTopLeftRadius = 6f;
            m_Container.style.borderTopRightRadius = 6f;
            m_Container.style.flexDirection = FlexDirection.Column;
            m_Container.style.gap = 4f;
            m_Container.style.minWidth = 160f;

            AttachContainer();
        }

        /// <summary>現在の親要素へコンテナを追加します。</summary>
        private void AttachContainer()
        {
            if (m_Container == null)
            {
                return;
            }

            if (m_Container.parent != null)
            {
                m_Container.RemoveFromHierarchy();
            }

            if (m_Parent != null)
            {
                m_Parent.Add(m_Container);
            }
        }

        /// <summary>エントリを生成または更新し、ラベル辞書へ登録します。</summary>
        private void CreateOrUpdateEntry(FUnityVariableService.VariableState state)
        {
            if (state == null)
            {
                return;
            }

            if (!m_LabelMap.TryGetValue(state, out var label))
            {
                label = new Label
                {
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList(EntryClassName);
                m_LabelMap[state] = label;
                m_Container?.Add(label);
            }

            UpdateEntry(state);
            label.style.display = state.Visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>ラベルの文字列を最新値へ更新します。</summary>
        private void UpdateEntry(FUnityVariableService.VariableState state)
        {
            if (!m_LabelMap.TryGetValue(state, out var label))
            {
                return;
            }

            var ownerLabel = ResolveOwnerLabel(state);
            var valueText = state.Value.ToString("0.###", s_Culture);
            label.text = string.IsNullOrEmpty(ownerLabel)
                ? $"{state.Name}: {valueText}"
                : $"{ownerLabel} / {state.Name}: {valueText}";
        }

        /// <summary>俳優スコープ変数の表示ラベルを決定します。</summary>
        private static string ResolveOwnerLabel(FUnityVariableService.VariableState state)
        {
            if (state == null || state.Scope != FUnityVariableScope.Actor)
            {
                return string.Empty;
            }

            var actor = state.OwnerActor;
            if (actor == null)
            {
                return string.Empty;
            }

            var displayName = actor.ActorData != null ? actor.ActorData.DisplayName : string.Empty;
            if (!string.IsNullOrEmpty(displayName))
            {
                return displayName;
            }

            return actor.ActorKey;
        }
    }
}
