using UnityEngine;

namespace FUnity.Runtime.Presenter
{
    /// <summary>
    /// 入力デバイスから移動ベクトルを読み取る責務を持つプレゼンター。
    /// </summary>
    public sealed class InputPresenter
    {
        public Vector2 ReadMove()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var y = -Input.GetAxisRaw("Vertical");
            return new Vector2(x, y);
        }
    }
}
