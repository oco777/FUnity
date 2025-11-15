# Docs/AGENTS.md

## Visual Scripting ドキュメント更新方針
- Visual Scripting から Presenter を操作するアダプタは `ActorPresenterAdapter` と `VSPresenterBridge` を標準名称とします。
- 互換目的の旧名称は紹介せず、最新 API を基準に説明してください。
- サンプル生成・ドキュメント記述では、`FUnity UI` などのシーンルートにコンポーネントをサイレント追加しないよう注意してください。
- コスチュームの挙動を説明する際は `ActorState.CostumeIndex` → `ActorPresenter.ApplyCostumeFromState()` → `ActorView.SetSprite(Sprite)` の順で状態が伝播することを明記してください。
