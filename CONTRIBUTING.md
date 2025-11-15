# CONTRIBUTING

## ドキュメント更新ポリシー: VS Scratch Mapping の維持

FUnity では、Visual Scripting の Scratch 互換ノード群（`Runtime/Integrations/VisualScripting/Units/**`）に
変更が入った場合、**`Docs/VS_Scratch_Mapping.md`** も必ず同じ Pull Request で更新してください。

### 対象となる変更
- Unit クラスの追加・削除・改名
- `UnitTitle("Scratch/...")` または `UnitTitle("Fooni/...")` のタイトル変更
- Scratch ブロック対応の日本語表記（Docs 内のテキスト）に影響する変更
- 関連 API (`ActorPresenterAdapter` / `VSPresenterBridge`, `ActorPresenter`) の動作変更

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

## Visual Scripting Unit 命名・カテゴリ規約（恒久）

- Visual Scripting 用の独自 Unit を作成・変更するときは、以下の規約を厳守してください。
  - `[UnitTitle]` は Scratch 日本語ブロック表記に合わせる（対応ブロックが無い場合は Scratch 風の短い日本語名にする）。
  - `[UnitCategory]` は `FUnity/Scratch/◯◯` 形式（カテゴリ名は日本語）で記述し、Scratch 非対応の拡張は `FUnity/Scratch/拡張` を使う。
  - `[TypeIcon(typeof(FUnityScratchUnitIcon))]` を必ず付与し、FUnity Scratch 系ユニット共通のアイコンを利用する。
  - ノード検索性を高めるため、利用可能な場合は `[UnitSubtitle]`（または同等のキーワード属性）に `funity scratch`・カテゴリ名・日本語/英語の関連語を半角スペース区切りで登録する（例：`funity scratch イベント green flag`）。
  - 追加・変更に合わせて `Docs/VS_Scratch_Mapping.md` を更新し、表内の `UnitTitle` / `UnitCategory` をコードと一致させる。
  - 新しい VS ユニットを追加する際はカテゴリパス（`FUnity/Scratch/...`）と日本語タイトルを確定させ、同じ表記で `Docs/VS_Scratch_Mapping.md` に追記する。
  - マウス座標や押下状態を扱うユニットは `IMousePositionProvider`（`FUnityManager.MouseProvider`）経由で取得し、スクリーン座標からの直接計算を避ける。
  - 向きや角度を扱う Scratch 系ユニットでは `ScratchUnitUtil.GetDirectionDegreesForCurrentMode` と `DirFromDegrees` を必ず利用し、Scratch モード時は上=0°・右=90°・左=-90°・下=±180°、通常モードでは右=0° となるよう変換する。

## ランタイム配置に関する必須ルール

- ランタイム C# スクリプトは **すべて `Runtime/` 直下の Unity パッケージ側** に配置します。`Assets/FUnity/Runtime/` に C# を追加しないでください。
- 既存の PR でランタイムコードを追加する場合は、レビュアーがファイルパスを確認し、`Runtime/` に統一されていることをチェックしてください。
- `Assets/FUnity/Runtime/` に C# / asmdef / asmref が混入していた場合、Editor ガードと CI ジョブ（`verify-runtime-layout`）がエラーを出します。必ず修正してからマージしてください。

## C# スクリプトと `.meta` の管理ルール（恒久）

- `.cs` ファイルを新規に追加・生成する場合は、**必ず対応する `.meta` ファイルも Unity エディタで生成したものを同一コミットで追加**してください（AI や手作業で GUID を捏造しない）。
- `.cs` ファイルを削除・リネームする場合は、**対応する `.meta` ファイルも同時に削除・リネーム**し、どちらか片方のみが残らないようにします。
- `.cs` と `.meta` のペアが欠けている不整合を見つけた場合は、その場で修正するか、少なくとも PR 説明で不足しているファイルを明示してください。`.meta` が欠けている場合は Unity エディタで正規生成したものを追加します。
- `.meta` ファイルの YAML や GUID を手作業で編集・生成しないでください。破損している場合は Unity エディタで再生成してください。
- このルールは FUnity リポジトリ全体（`Runtime/`, `Editor/`, `Packages/`, `Assets/FUnity/` など）で恒久的に適用します。

## 変数サービス運用ルール

- Scratch 互換の変数機能は `IFUnityVariableService` / `FUnityVariableService` を経由して操作してください。Presenter や VS からの直接アクセスは禁止です。
- Visual Scripting の変数系ユニットはサービスへ直接アクセスせず、共通アクセサ（例: `ScratchVariableUnitUtility`）を経由して呼び出します。
- 変数系ユニットを含む新しい VS Unit を追加した場合は、`Docs/VS_Scratch_Mapping.md` を更新し、対応表へ必ず追記してください。
- `.cs` ファイルの追加・削除時には対応する `.meta` を忘れずに管理してください（上記ルールの再掲）。

## Move/Bounce 実装ポリシー（必読）

- Scratch モーションの Bounce 系処理は、反射後に**中心座標をステージ内へ押し戻す（クランプ＋ε）**ロジックを必須とします。
- 端判定は常に俳優の見た目サイズ（halfSize）を基準に行い、矩形サイズが確定していない場合は `ScratchUnitUtil.TryGetActorWorldRect` を再試行してください。
- `MoveStepsUnit` などの移動処理は、**境界まで進む → 反射 → 残り距離で再移動**の繰り返しで大きな移動量にも耐えるよう実装します。
- 歩数の換算は「1 歩 = 1px」を既定とし、倍率は `MoveStepsUnit` の `StepToPixels` 定数だけで管理します（他所へコピーしない）。

## 画像アセットの取り扱い

- 画像ファイルは **必ず `Docs/images/`（先頭大文字）** に配置してください。`docs/images/` やその他のディレクトリに置くとビルドや CI で警告が表示されます。
- README のヒーロー画像は `Docs/images/readme-hero.png` への参照に統一し、実ファイルのサイズが 0 バイトでないことを確認してください。
- PNG を更新した場合は、関連するドキュメント（`README.md` や `Docs/*.md`）のリンクが `Docs/images/` を参照しているかを再確認してください。

## Actor Sprite の管理指針

- 俳優アセット（`FUnityActorData`）の見た目は `Sprites` リストのみを正規データとし、旧 `Portrait` / `PortraitSprite` フィールドは廃止済みです。
- Sprite の差し替えは Presenter API（`ActorPresenter.SetSpriteIndex`）およびアダプタ API（`ActorPresenterAdapter.SetSpriteIndex`）経由で行い、直接 `style.backgroundImage` に Texture を設定しないでください。
- Unity の Sprite Editor でスライスした Sprite をインポートし、Inspector 上では `Sprites` に順序どおり登録してから PR を提出してください。
- 新規アセットは Texture Type = Sprite, Sprite Mode = Multiple を基本とし、差分は 1 アセット内で管理してください。
- 複数差分を利用する場合は `Sprites` リストへ登録し、コード側では `SetSpriteIndex()` を使用して切り替えます。
