// Updated: 2025-02-14
using System;
using UnityEngine;

namespace FUnity.Runtime.Integrations.VisualScripting
{
    /// <summary>
    /// 後方互換性のために残される旧称コンポーネントです。ActorPresenterAdapter へ処理を委譲します。
    /// </summary>
    [Obsolete("FooniController は廃止予定です。ActorPresenterAdapter を使用してください。")]
    public class FooniController : ActorPresenterAdapter
    {
    }
}
