using UnityEngine;

namespace FUnity.Runtime.View
{
    /// <summary>
    /// プレゼンターから受け取った状態を描画へ反映するためのインターフェース。
    /// </summary>
    public interface IActorView
    {
        void SetPosition(Vector2 pos);

        void SetPortrait(Sprite sprite);
    }
}
