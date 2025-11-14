// Updated: 2025-03-20
using System;
using UnityEngine;
using UnityEngine.UIElements;
using FUnity.Runtime.Core;

namespace FUnity.Runtime.Input
{
    /// <summary>
    /// ステージ中心原点（Scratch 座標系）でのマウス座標を提供するプロバイダのインターフェースです。
    /// </summary>
    public interface IMousePositionProvider
    {
        /// <summary>現在のマウス座標（x, y）をステージ中心原点で返します。</summary>
        Vector2 StagePosition { get; }

        /// <summary>現在のマウス x 座標（ステージ中心原点基準）を返します。</summary>
        float X { get; }

        /// <summary>現在のマウス y 座標（ステージ中心原点基準）を返します。</summary>
        float Y { get; }

        /// <summary>マウスポインターがステージ矩形内に存在する場合は true を返します。</summary>
        bool IsInsideStage { get; }

        /// <summary>現在左ボタンが押下されている場合は true を返します。</summary>
        bool IsPressed { get; }
    }

    /// <summary>
    /// UI Toolkit の PointerMoveEvent からマウス座標を受け取り、Scratch 座標系へ変換・保持するサービスです。
    /// </summary>
    public sealed class MousePositionService : IMousePositionProvider, IDisposable
    {
        /// <summary>座標変換対象のルート要素。ステージ直下のビューポートを想定する。</summary>
        private readonly VisualElement m_Root;

        /// <summary>ステージの論理サイズ（幅・高さ）を取得するデリゲート。</summary>
        private readonly Func<Vector2> m_GetStageSize;

        /// <summary>最後に観測した Scratch 座標系でのマウス座標。</summary>
        private Vector2 m_LastPosition;

        /// <summary>現在マウスポインターがステージ矩形内にあるかどうか。</summary>
        private bool m_IsInsideStage;

        /// <summary>ステージ領域へ座標をクランプするかどうか。</summary>
        private bool m_ClampToStage;

        /// <summary>左ボタンが押下されているかどうか。</summary>
        private bool m_IsPressed;

        /// <summary>Dispose 済みかどうかを示すフラグ。</summary>
        private bool m_Disposed;

        /// <summary>
        /// UI Toolkit 要素とステージサイズ取得デリゲートを受け取り、マウス座標監視を開始します。
        /// </summary>
        /// <param name="root">PointerMoveEvent を受け取るルート要素。</param>
        /// <param name="getStageSize">現在のステージ幅・高さを取得するデリゲート。</param>
        /// <param name="clampToStage">true の場合、座標をステージ境界へクランプします。</param>
        public MousePositionService(VisualElement root, Func<Vector2> getStageSize, bool clampToStage = true)
        {
            m_Root = root;
            m_GetStageSize = getStageSize;
            m_ClampToStage = clampToStage;
            m_LastPosition = Vector2.zero;
            m_IsInsideStage = false;
            m_IsPressed = false;

            if (m_Root == null)
            {
                Debug.LogWarning("[FUnity.Input] MousePositionService: ルート要素が null のためポインター監視を開始できません。");
                return;
            }

            m_Root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
            m_Root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
            m_Root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.NoTrickleDown);
            m_Root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.NoTrickleDown);
        }

        /// <summary>現在のクランプ設定を参照または更新します。</summary>
        public bool ClampToStage
        {
            get => m_ClampToStage;
            set => m_ClampToStage = value;
        }

        /// <summary>最後に変換したマウス座標を返します。</summary>
        public Vector2 StagePosition => m_LastPosition;

        /// <summary>現在の x 座標を返します。</summary>
        public float X => m_LastPosition.x;

        /// <summary>現在の y 座標を返します。</summary>
        public float Y => m_LastPosition.y;

        /// <summary>マウスポインターがステージ内に存在する場合は true を返します。</summary>
        public bool IsInsideStage => m_IsInsideStage;

        /// <summary>現在左ボタンが押下されているかどうかを返します。</summary>
        public bool IsPressed => m_IsPressed;

        /// <summary>
        /// PointerMoveEvent を受け取り、Scratch 座標系（中心原点・上向き +Y）へ変換します。
        /// </summary>
        /// <param name="evt">UI Toolkit から通知されたポインター移動イベント。</param>
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt == null || m_Root == null)
            {
                return;
            }

            var local = m_Root.WorldToLocal(evt.position);
            var stageSize = ResolveStageSize();
            var width = Mathf.Max(1f, stageSize.x);
            var height = Mathf.Max(1f, stageSize.y);

            var centeredX = local.x - (width * 0.5f);
            var centeredY = (height * 0.5f) - local.y;

            m_IsInsideStage = local.x >= 0f && local.x <= width && local.y >= 0f && local.y <= height;

            if (m_ClampToStage)
            {
                centeredX = Mathf.Clamp(centeredX, -width * 0.5f, width * 0.5f);
                centeredY = Mathf.Clamp(centeredY, -height * 0.5f, height * 0.5f);
            }

            m_LastPosition = new Vector2(centeredX, centeredY);
        }

        /// <summary>
        /// PointerLeaveEvent を受け取り、ステージ外へ移動したことを記録します。
        /// </summary>
        /// <param name="evt">UI Toolkit から通知されたポインター離脱イベント。</param>
        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            m_IsInsideStage = false;
        }

        /// <summary>
        /// PointerDownEvent を受け取り、左ボタンの押下状態を保持します。
        /// </summary>
        /// <param name="evt">UI Toolkit から通知されたポインター押下イベント。</param>
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (evt.button == (int)MouseButton.LeftMouse)
            {
                m_IsPressed = true;
            }
        }

        /// <summary>
        /// PointerUpEvent を受け取り、左ボタンの押下状態を解除します。
        /// </summary>
        /// <param name="evt">UI Toolkit から通知されたポインター解放イベント。</param>
        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (evt.button == (int)MouseButton.LeftMouse)
            {
                m_IsPressed = false;
            }
        }

        /// <summary>
        /// ステージサイズを取得し、無効な値であれば Scratch 既定サイズへフォールバックします。
        /// </summary>
        /// <returns>有効なステージサイズ。</returns>
        private Vector2 ResolveStageSize()
        {
            if (m_GetStageSize != null)
            {
                var size = m_GetStageSize();
                if (size.x > 0f && size.y > 0f)
                {
                    return size;
                }
            }

            return new Vector2(FUnityStageData.DefaultStageWidth, FUnityStageData.DefaultStageHeight);
        }

        /// <summary>
        /// 登録した UI Toolkit コールバックを解除し、参照を開放します。
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;

            if (m_Root != null)
            {
                m_Root.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.NoTrickleDown);
                m_Root.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.NoTrickleDown);
                m_Root.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.NoTrickleDown);
                m_Root.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.NoTrickleDown);
            }
        }
    }
}
