// Updated: 2025-11-12
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 質問入力フォームを生成し、Enter または OK ボタンで回答を受け取る UI Toolkit サービスです。
    /// </summary>
    public static class AskPromptService
    {
        /// <summary>オーバーレイ要素の名前。</summary>
        private const string OverlayName = "FUnityQuestionOverlay";

        /// <summary>パネル要素の名前。</summary>
        private const string PanelName = "FUnityQuestionPanel";

        /// <summary>現在表示中のオーバーレイ要素。</summary>
        private static VisualElement s_Overlay;

        /// <summary>質問入力フィールド。</summary>
        private static TextField s_InputField;

        /// <summary>送信時に呼び出されるコールバック。</summary>
        private static Action<string> s_OnSubmitted;

        /// <summary>
        /// オーバーレイを生成して表示し、回答完了時に <paramref name="onSubmitted"/> を呼び出します。
        /// </summary>
        /// <param name="parent">配置先となる UI Toolkit ルート要素。</param>
        /// <param name="question">表示する質問文。</param>
        /// <param name="onSubmitted">回答完了時に呼び出すコールバック。</param>
        public static void Show(VisualElement parent, string question, Action<string> onSubmitted)
        {
            if (parent == null)
            {
                Debug.LogWarning("[FUnity] AskPromptService: 親要素が null のため質問フォームを表示できません。");
                return;
            }

            Cleanup();

            s_OnSubmitted = onSubmitted;
            s_Overlay = CreateOverlay(question ?? string.Empty);

            parent.Add(s_Overlay);
            FocusInputField();
        }

        /// <summary>
        /// 現在のオーバーレイを破棄し、状態を初期化します。
        /// </summary>
        public static void Cleanup()
        {
            if (s_Overlay != null)
            {
                s_Overlay.RemoveFromHierarchy();
            }

            s_Overlay = null;
            s_InputField = null;
            s_OnSubmitted = null;
        }

        /// <summary>
        /// オーバーレイ全体を生成します。
        /// </summary>
        /// <param name="question">表示する質問文。</param>
        /// <returns>生成したオーバーレイ要素。</returns>
        private static VisualElement CreateOverlay(string question)
        {
            var overlay = new VisualElement
            {
                name = OverlayName,
                pickingMode = PickingMode.Position
            };

            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.right = 0f;
            overlay.style.top = 0f;
            overlay.style.bottom = 0f;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);

            var panel = CreatePanel(question);
            overlay.Add(panel);

            return overlay;
        }

        /// <summary>
        /// パネルを生成し、ラベル・入力欄・ボタンを配置します。
        /// </summary>
        /// <param name="question">表示する質問文。</param>
        /// <returns>生成したパネル要素。</returns>
        private static VisualElement CreatePanel(string question)
        {
            var panel = new VisualElement
            {
                name = PanelName
            };

            panel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.92f);
            panel.style.paddingLeft = 16f;
            panel.style.paddingRight = 16f;
            panel.style.paddingTop = 12f;
            panel.style.paddingBottom = 12f;
            panel.style.borderBottomLeftRadius = 6f;
            panel.style.borderBottomRightRadius = 6f;
            panel.style.borderTopLeftRadius = 6f;
            panel.style.borderTopRightRadius = 6f;
            panel.style.minWidth = 260f;
            panel.style.maxWidth = 420f;
            panel.style.display = DisplayStyle.Flex;
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.rowGap = 8f;

            var label = new Label(question)
            {
                pickingMode = PickingMode.Ignore
            };
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            label.style.whiteSpace = WhiteSpace.Normal;

            s_InputField = new TextField
            {
                value = string.Empty,
                isDelayed = false,
                pickingMode = PickingMode.Position
            };
            s_InputField.style.width = Length.Percent(100f);
            s_InputField.style.flexGrow = 1f;
            s_InputField.RegisterCallback<KeyDownEvent>(OnKeyDownSubmit);

            var submitButton = new Button(Submit)
            {
                text = "OK",
                pickingMode = PickingMode.Position
            };
            submitButton.style.alignSelf = Align.FlexEnd;

            panel.Add(label);
            panel.Add(s_InputField);
            panel.Add(submitButton);

            return panel;
        }

        /// <summary>
        /// Enter キーで送信できるよう入力フィールドのキーイベントを処理します。
        /// </summary>
        /// <param name="evt">押下されたキーイベント。</param>
        private static void OnKeyDownSubmit(KeyDownEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Submit();
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// 入力フィールドへフォーカスを当て、既存入力を選択します。
        /// </summary>
        private static void FocusInputField()
        {
            if (s_InputField == null)
            {
                return;
            }

            s_InputField.schedule.Execute(() =>
            {
                s_InputField.Focus();
                s_InputField.SelectAll();
            }).StartingIn(0);
        }

        /// <summary>
        /// 現在の入力値を確定し、コールバックを呼び出します。
        /// </summary>
        private static void Submit()
        {
            var callback = s_OnSubmitted;
            var answer = s_InputField != null ? s_InputField.value : string.Empty;

            Cleanup();
            callback?.Invoke(answer);
        }
    }
}
