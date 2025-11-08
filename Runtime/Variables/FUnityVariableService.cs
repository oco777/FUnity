// Updated: 2025-03-18
using System.Collections.Generic;
using FUnity.Runtime.Core;
using FUnity.Runtime.Presenter;
using UnityEngine;

namespace FUnity.Runtime.Variables
{
    /// <summary>
    /// Visual Scripting や Presenter 層から共有される変数操作サービスのインターフェースです。
    /// Scratch の変数ブロック相当の操作をランタイムで一元管理します。
    /// </summary>
    public interface IFUnityVariableService
    {
        /// <summary>プロジェクト設定と俳優リストからサービスを初期化します。</summary>
        /// <param name="projectData">グローバル変数定義を含むプロジェクト設定。</param>
        /// <param name="actors">初期化済みの俳優 Presenter 群。</param>
        void Initialize(FUnityProjectData projectData, IEnumerable<IActorPresenter> actors);

        /// <summary>指定した変数へ絶対値を設定します。</summary>
        void SetValue(string variableName, float value, IActorPresenter actorContext = null);

        /// <summary>指定した変数へ加算を行います。</summary>
        void AddValue(string variableName, float delta, IActorPresenter actorContext = null);

        /// <summary>指定した変数の現在値を返します。見つからない場合は 0。</summary>
        float GetValue(string variableName, IActorPresenter actorContext = null);

        /// <summary>変数モニター上の可視状態を切り替えます。</summary>
        void SetVisible(string variableName, bool visible, IActorPresenter actorContext = null);

        /// <summary>新たに生成された俳優のローカル変数を登録します。</summary>
        void RegisterActor(IActorPresenter actor, IActorPresenter sourceActor = null);

        /// <summary>破棄された俳優のローカル変数をサービスから除去します。</summary>
        void UnregisterActor(IActorPresenter actor);

        /// <summary>UI 表示を担当する Presenter を設定します。</summary>
        void SetUiPresenter(IVariableUiPresenter presenter);
    }

    /// <summary>
    /// Scratch 互換の変数を一元管理するサービス実装です。グローバルおよび俳優ローカル変数を
    /// 辞書で保持し、UI Presenter へ値と可視状態の更新を通知します。
    /// </summary>
    public class FUnityVariableService : IFUnityVariableService
    {
        /// <summary>グローバル変数を名前で引く辞書。</summary>
        private readonly Dictionary<string, VariableState> m_GlobalVariables = new Dictionary<string, VariableState>();

        /// <summary>俳優ローカル変数を (ActorKey, Name) で管理する辞書。</summary>
        private readonly Dictionary<(string actorId, string name), VariableState> m_ActorVariables
            = new Dictionary<(string, string), VariableState>();

        /// <summary>クローン破棄時の辞書削除で再利用するバッファ。</summary>
        private readonly List<(string actorId, string name)> m_RemovalBuffer
            = new List<(string actorId, string name)>(8);

        /// <summary>UI Toolkit ベースの変数モニター Presenter。</summary>
        private IVariableUiPresenter m_UiPresenter;

        /// <summary>
        /// プロジェクト設定と俳優一覧から変数状態を構築します。既存の値は初期値で上書きされます。
        /// </summary>
        public void Initialize(FUnityProjectData projectData, IEnumerable<IActorPresenter> actors)
        {
            m_GlobalVariables.Clear();
            m_ActorVariables.Clear();

            if (projectData?.GlobalVariables != null)
            {
                foreach (var definition in projectData.GlobalVariables)
                {
                    if (definition == null || string.IsNullOrEmpty(definition.Name))
                    {
                        continue;
                    }

                    if (definition.Scope != FUnityVariableScope.Global)
                    {
                        continue;
                    }

                    var state = new VariableState
                    {
                        Name = definition.Name,
                        Scope = FUnityVariableScope.Global,
                        OwnerActor = null,
                        Value = definition.InitialValue,
                        Visible = definition.InitialVisible
                    };

                    m_GlobalVariables[definition.Name] = state;
                }
            }

            if (actors != null)
            {
                foreach (var actor in actors)
                {
                    CreateActorStates(actor, null);
                }
            }

            m_UiPresenter?.Initialize(this, m_GlobalVariables.Values, m_ActorVariables.Values);
        }

        /// <summary>UI Presenter を設定し、既存状態を同期します。</summary>
        /// <param name="presenter">変数モニター Presenter。</param>
        public void SetUiPresenter(IVariableUiPresenter presenter)
        {
            m_UiPresenter = presenter;
            m_UiPresenter?.Initialize(this, m_GlobalVariables.Values, m_ActorVariables.Values);
        }

        /// <inheritdoc />
        public void SetValue(string variableName, float value, IActorPresenter actorContext = null)
        {
            if (TryFindVariable(variableName, actorContext, out var state))
            {
                state.Value = value;
                m_UiPresenter?.OnValueChanged(state);
            }
            else
            {
                Debug.LogWarning($"[FUnity] VariableService: '{variableName}' を解決できなかったため SetValue をスキップします。");
            }
        }

        /// <inheritdoc />
        public void AddValue(string variableName, float delta, IActorPresenter actorContext = null)
        {
            if (TryFindVariable(variableName, actorContext, out var state))
            {
                state.Value += delta;
                m_UiPresenter?.OnValueChanged(state);
            }
            else
            {
                Debug.LogWarning($"[FUnity] VariableService: '{variableName}' を解決できなかったため AddValue をスキップします。");
            }
        }

        /// <inheritdoc />
        public float GetValue(string variableName, IActorPresenter actorContext = null)
        {
            return TryFindVariable(variableName, actorContext, out var state) ? state.Value : 0f;
        }

        /// <inheritdoc />
        public void SetVisible(string variableName, bool visible, IActorPresenter actorContext = null)
        {
            if (TryFindVariable(variableName, actorContext, out var state))
            {
                state.Visible = visible;
                m_UiPresenter?.OnVisibilityChanged(state);
            }
            else
            {
                Debug.LogWarning($"[FUnity] VariableService: '{variableName}' を解決できなかったため SetVisible をスキップします。");
            }
        }

        /// <inheritdoc />
        public void RegisterActor(IActorPresenter actor, IActorPresenter sourceActor = null)
        {
            if (!CreateActorStates(actor, sourceActor))
            {
                return;
            }

            m_UiPresenter?.Initialize(this, m_GlobalVariables.Values, m_ActorVariables.Values);
        }

        /// <inheritdoc />
        public void UnregisterActor(IActorPresenter actor)
        {
            if (actor == null)
            {
                return;
            }

            var actorId = actor.ActorKey;
            if (string.IsNullOrEmpty(actorId))
            {
                return;
            }

            m_RemovalBuffer.Clear();

            foreach (var pair in m_ActorVariables)
            {
                if (pair.Key.actorId == actorId)
                {
                    m_RemovalBuffer.Add(pair.Key);
                }
            }

            if (m_RemovalBuffer.Count == 0)
            {
                return;
            }

            for (var i = 0; i < m_RemovalBuffer.Count; i++)
            {
                m_ActorVariables.Remove(m_RemovalBuffer[i]);
            }

            m_UiPresenter?.Initialize(this, m_GlobalVariables.Values, m_ActorVariables.Values);
        }

        /// <summary>
        /// 変数を検索し、成功時に状態を返します。俳優ローカルを優先し、見つからない場合はグローバルを参照します。
        /// </summary>
        private bool TryFindVariable(string name, IActorPresenter actorContext, out VariableState state)
        {
            state = null;

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (actorContext != null)
            {
                var actorId = actorContext.ActorKey;
                if (!string.IsNullOrEmpty(actorId))
                {
                    var key = (actorId, name);
                    if (m_ActorVariables.TryGetValue(key, out state))
                    {
                        return true;
                    }
                }
            }

            if (m_GlobalVariables.TryGetValue(name, out state))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 俳優設定からローカル変数状態を生成します。sourceActor が指定された場合は値と可視状態を複製します。
        /// </summary>
        private bool CreateActorStates(IActorPresenter actor, IActorPresenter sourceActor)
        {
            if (actor == null)
            {
                return false;
            }

            var actorId = actor.ActorKey;
            if (string.IsNullOrEmpty(actorId))
            {
                Debug.LogWarning("[FUnity] VariableService: ActorKey が空の Presenter の変数を登録できません。");
                return false;
            }

            var actorData = actor.ActorData;
            if (actorData?.ActorVariables == null)
            {
                return false;
            }

            var added = false;

            foreach (var definition in actorData.ActorVariables)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Name))
                {
                    continue;
                }

                if (definition.Scope != FUnityVariableScope.Actor)
                {
                    continue;
                }

                var key = (actorId, definition.Name);
                var state = new VariableState
                {
                    Name = definition.Name,
                    Scope = FUnityVariableScope.Actor,
                    OwnerActor = actor,
                    Value = definition.InitialValue,
                    Visible = definition.InitialVisible
                };

                if (sourceActor != null
                    && TryFindVariable(definition.Name, sourceActor, out var sourceState)
                    && sourceState.Scope == FUnityVariableScope.Actor)
                {
                    state.Value = sourceState.Value;
                    state.Visible = sourceState.Visible;
                }

                m_ActorVariables[key] = state;
                added = true;
            }

            return added;
        }

        /// <summary>
        /// サービスが管理する変数状態を表す内部クラスです。UI Presenter への通知にも利用されます。
        /// </summary>
        public class VariableState
        {
            /// <summary>変数名。</summary>
            public string Name;

            /// <summary>変数スコープ。</summary>
            public FUnityVariableScope Scope;

            /// <summary>所有俳優。グローバルの場合は null。</summary>
            public IActorPresenter OwnerActor;

            /// <summary>現在値。</summary>
            public float Value;

            /// <summary>変数モニターで可視かどうか。</summary>
            public bool Visible;
        }
    }
}

