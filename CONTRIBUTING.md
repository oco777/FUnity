# CONTRIBUTING

## ドキュメント更新ポリシー: VS Scratch Mapping の維持

FUnity では、Visual Scripting の Scratch 互換ノード群（`Runtime/Integrations/VisualScripting/Units/**`）に
変更が入った場合、**`Docs/VS_Scratch_Mapping.md`** も必ず同じ Pull Request で更新してください。

### 対象となる変更
- Unit クラスの追加・削除・改名
- `UnitTitle("Scratch/...")` または `UnitTitle("Fooni/...")` のタイトル変更
- Scratch ブロック対応の日本語表記（Docs 内のテキスト）に影響する変更
- 関連 API (`ActorPresenterAdapter` / 旧 `FooniController`, `ActorPresenter`) の動作変更

### 更新手順
1. 変更を確認し、`Docs/VS_Scratch_Mapping.md` に反映
   - 新しいノードを追加する場合は表に追記
   - 名称変更・削除の場合は表の該当行を修正または削除
2. 概要・備考を見直し、説明が正しいことを確認
3. 必要に応じて `Docs/VS_Scratch_Mapping.txt` の元データも更新（自動生成元）

### チェックリスト（PR 作成時）
- [ ] VS ノードの追加／変更／削除を `Docs/VS_Scratch_Mapping.md` に反映した
- [ ] テーブルのフォーマットが崩れていない（Markdown プレビューで確認済み）
- [ ] 新しいユニットに対応する Scratch 日本語表記を追加した

### Commit 例
```
docs(vs): update VS_Scratch_Mapping.md for new/renamed Units
```

> このルールは AGENTS.md にも記載されています。Pull Request 時は両方を参照してください。
