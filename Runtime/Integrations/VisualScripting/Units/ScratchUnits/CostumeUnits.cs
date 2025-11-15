// Updated: 2025-05-22
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using FUnity.Runtime.Integrations.VisualScripting;
using FUnity.Runtime.Presenter;

namespace FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits
{
    /// <summary>
    /// Scratch の「コスチュームを ◯ にする」ブロックに対応し、Presenter を通じてコスチューム番号を絶対設定する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("コスチュームを○にする")]
    [UnitCategory("FUnity/Scratch/見た目")]
    [UnitSubtitle("funity scratch 見た目 costume set コスチューム")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class SetCostumeNumberUnit : Unit
    {
        /// <summary>ログ出力に利用するユニット名です。</summary>
        private const string UnitName = "コスチュームを○にする";

        /// <summary>制御フローを受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>1 始まりのコスチューム番号を受け取る ValueInput です。</summary>
        [DoNotSerialize]
        private ValueInput m_CostumeNumber;

        /// <summary>enter ポートへの参照です。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照です。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>costumeNumber ポートへの参照です。</summary>
        public ValueInput CostumeNumber => m_CostumeNumber;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線とコスチューム番号入力を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");
            m_CostumeNumber = ValueInput<int>("costumeNumber", 1);

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// フロー入力時に Presenter を解決し、指定されたコスチューム番号を適用します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            if (!CostumeUnitUtil.TryResolvePresenter(flow, UnitName, out var presenter))
            {
                yield return m_Exit;
                yield break;
            }

            var spriteCount = presenter.SpriteCount;
            if (spriteCount <= 0)
            {
                presenter.SetSpriteIndex(0);
                yield return m_Exit;
                yield break;
            }

            var costumeNumber = flow.GetValue<int>(m_CostumeNumber);
            var clampedNumber = Mathf.Clamp(costumeNumber, 1, spriteCount);
            var index0 = clampedNumber - 1;

            presenter.SetSpriteIndex(index0);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「次のコスチュームにする」ブロックに対応し、コスチュームを循環的に切り替える Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("次のコスチュームにする")]
    [UnitCategory("FUnity/Scratch/見た目")]
    [UnitSubtitle("funity scratch 見た目 costume next 次へ")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class NextCostumeUnit : Unit
    {
        /// <summary>ログ出力に利用するユニット名です。</summary>
        private const string UnitName = "次のコスチュームにする";

        /// <summary>制御フローを受け取る ControlInput です。</summary>
        [DoNotSerialize]
        private ControlInput m_Enter;

        /// <summary>後続へ制御を渡す ControlOutput です。</summary>
        [DoNotSerialize]
        private ControlOutput m_Exit;

        /// <summary>enter ポートへの参照です。</summary>
        public ControlInput Enter => m_Enter;

        /// <summary>exit ポートへの参照です。</summary>
        public ControlOutput Exit => m_Exit;

        /// <summary>
        /// ポート定義を行い、enter→exit の制御線のみを登録します。
        /// </summary>
        protected override void Definition()
        {
            m_Enter = ControlInputCoroutine("enter", Run);
            m_Exit = ControlOutput("exit");

            Succession(m_Enter, m_Exit);
        }

        /// <summary>
        /// フロー入力時に Presenter を解決し、次のコスチュームへ循環的に切り替えます。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>exit ポートへ制御を渡す列挙子。</returns>
        private IEnumerator Run(Flow flow)
        {
            if (!CostumeUnitUtil.TryResolvePresenter(flow, UnitName, out var presenter))
            {
                yield return m_Exit;
                yield break;
            }

            var spriteCount = presenter.SpriteCount;
            if (spriteCount <= 0)
            {
                presenter.SetSpriteIndex(0);
                yield return m_Exit;
                yield break;
            }

            var current = presenter.SpriteIndex;
            var safeCurrent = Mathf.Clamp(current, 0, spriteCount - 1);
            var next = (safeCurrent + 1) % spriteCount;

            presenter.SetSpriteIndex(next);
            yield return m_Exit;
        }
    }

    /// <summary>
    /// Scratch の「コスチュームの番号」ブロックに対応し、現在のコスチューム番号（1 始まり）を取得する Visual Scripting Unit です。
    /// </summary>
    [UnitTitle("コスチュームの番号")]
    [UnitCategory("FUnity/Scratch/見た目")]
    [UnitSubtitle("funity scratch 見た目 costume number 番号")]
    [TypeIcon(typeof(FUnityScratchUnitIcon))]
    public sealed class GetCostumeNumberUnit : Unit
    {
        /// <summary>ログ出力に利用するユニット名です。</summary>
        private const string UnitName = "コスチュームの番号";

        /// <summary>現在のコスチューム番号を出力する ValueOutput です。</summary>
        [DoNotSerialize]
        private ValueOutput m_CostumeNumber;

        /// <summary>costumeNumber ポートへの参照です。</summary>
        public ValueOutput CostumeNumber => m_CostumeNumber;

        /// <summary>
        /// ポート定義を行い、現在のコスチューム番号を返す ValueOutput を登録します。
        /// </summary>
        protected override void Definition()
        {
            m_CostumeNumber = ValueOutput<int>("costumeNumber", GetCostumeNumber);
        }

        /// <summary>
        /// Presenter を解決して現在のコスチューム番号（1 始まり）を返します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <returns>1 始まりで表現したコスチューム番号。利用可能な Sprite が無い場合は 0。</returns>
        private int GetCostumeNumber(Flow flow)
        {
            if (!CostumeUnitUtil.TryResolvePresenter(flow, UnitName, out var presenter))
            {
                return 0;
            }

            var spriteCount = presenter.SpriteCount;
            if (spriteCount <= 0)
            {
                return 0;
            }

            var index0 = Mathf.Clamp(presenter.SpriteIndex, 0, spriteCount - 1);
            return index0 + 1;
        }
    }

    /// <summary>
    /// コスチューム系ユニットで共通利用する Presenter 解決ロジックをまとめたユーティリティです。
    /// </summary>
    internal static class CostumeUnitUtil
    {
        /// <summary>
        /// ActorPresenterAdapter および ActorPresenter を解決し、失敗時には警告ログを出力します。
        /// </summary>
        /// <param name="flow">現在のフロー情報。</param>
        /// <param name="unitName">ログ出力に使用するユニット名。</param>
        /// <param name="presenter">解決できた Presenter。失敗時は null。</param>
        /// <returns>解決に成功した場合は <c>true</c>。</returns>
        public static bool TryResolvePresenter(Flow flow, string unitName, out ActorPresenter presenter)
        {
            presenter = null;

            var adapter = ScratchUnitUtil.ResolveAdapter(flow);
            if (adapter == null)
            {
                Debug.LogWarning($"[FUnity] Scratch/Looks/{unitName}: ActorPresenterAdapter を自動解決できません。ScriptMachine の Variables 設定を確認してください。");
                return false;
            }

            presenter = adapter.Presenter;
            if (presenter == null)
            {
                Debug.LogWarning($"[FUnity] Scratch/Looks/{unitName}: ActorPresenter が未接続のためコスチュームを処理できません。Adapter と Presenter の紐付けを確認してください。");
                return false;
            }

            return true;
        }
    }
}
