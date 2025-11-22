# Scratch 参照箇所レポート

このレポートは、FUnity リポジトリ内の "Scratch" という文字列を含む箇所を集約し、種別ごとに整理したものです。リネームや文言調整の検討時の洗い出しに利用してください。

## 1. サマリー

- 総ヒット数: 1112 件 (`rg --no-heading --line-number Scratch` の結果)
- ファイル数: 82 個
- ファイル名・フォルダ名に "Scratch" を含むもの: 26 件

### 種別ごとの件数 (概数)

- UnitCategory / メニューカテゴリ: 62 行 (主に Visual Scripting Unit 属性)
- クラス名 / ファイル名 / 名前空間: 26 ファイルに "Scratch" を含むパス。複数クラス・名前空間で命名に使用
- UI 表示文字列（ラベル、ツールチップ等）: 148 行 (ToolTip や Debug ログなどの文字列リテラル)
- コメントのみ: 244 行 (実装コメント・XML コメント)
- ドキュメント（README, Docs など）: 6 主要ファイル (README, CHANGELOG, VS_Scratch_Mapping など多数の表記)
- その他: package.json の説明文など少数

## 2. 詳細一覧

### 2-1. UnitCategory / メニューカテゴリ

Visual Scripting の Unit 属性として `Scratch` カテゴリ文字列が多数存在します（計 62 行、すべて Runtime/Integrations/VisualScripting/Units/ 配下）。主なファイル例:

- `Runtime/Integrations/VisualScripting/Units/ScratchUnits/MoveStepsUnit.cs`
  - 行 15-18: `[UnitTitle("○歩動かす")]`, `[UnitCategory("FUnity/Scratch/動き")]`, `[UnitSubtitle(...)]`, `[TypeIcon(...)]`
  - 種別: UnitCategory/タイトル（動きカテゴリ）
- `Runtime/Integrations/VisualScripting/Units/ScratchUnits/LoopUnits.cs`
  - 行 13, 80: `[UnitCategory("FUnity/Scratch/制御")]` など複数の制御カテゴリ指定
- `Runtime/Integrations/VisualScripting/Units/ScratchUnits/GreenFlagUnits.cs`
  - 行 15: `[UnitCategory("Events/FUnity/Scratch/イベント")]` (イベントカテゴリ)
- `Runtime/Integrations/VisualScripting/Units/ScratchUnits/Probe/DistanceToMousePointerUnit.cs`
  - 行 13: `[UnitCategory("FUnity/Scratch/調べる")]` (調べるカテゴリ)

### 2-2. クラス名 / ファイル名 / 名前空間

`Scratch` を名称に含むファイル・名前空間・クラスが多数あります（26 パス）。主なもの:

- `Runtime/Core/ScratchBounds.cs`, `Runtime/Core/ScratchAngleUtil.cs`
  - Scratch 座標系・角度変換のユーティリティ。クラス名とファイル名に Scratch を含む
- `Runtime/Integrations/VisualScripting/Units/ScratchUnits/ScratchUnitUtil.cs`
  - 名前空間 `FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits` 配下。ユーティリティ名に Scratch を含む
- `Runtime/Integrations/VisualScripting/FUnityScratchUnitIcon.cs`
  - Scratch 系ユニット共通アイコン定義クラス
- `Docs/VS_Scratch_Mapping.md` / `Docs/VS_Scratch_Mapping.txt`
  - ファイル名自体が Scratch を含み、VS 対応表として利用

### 2-3. UI 表示文字列（ラベル、説明文など）

ユーザー向けに表示されるツールチップやログに Scratch を含む箇所が 148 行あります。代表例:

- `Runtime/Authoring/FUnityModeConfig.cs`
  - 行 32-74: `Tooltip("制作モードの種類。Scratch 互換か unityroom 公開かを選びます。")` など、モード設定 Inspector 用の説明文に複数箇所
- `Runtime/Core/FUnityManager.cs`
  - 緑の旗イベント配信や Scratch 固有モード判定などのログ/コメントに Scratch を含む
- `Runtime/Presenter/ActorPresenter.cs`
  - Scratch 座標系やヒットテスト関連の Debug.LogWarning メッセージに Scratch を含む
- `package.json`
  - 行 6: "description": "A Scratch-inspired educational toolkit for Unity UI Toolkit."

### 2-4. コメントのみ

実装コメントや XML ドキュメントコメントに Scratch が言及される箇所が 244 行あります。

- `Runtime/Model/ActorState.cs`
  - 行 7-82 付近: Scratch 互換の回転スタイル・コスチューム番号などの説明コメント
- `Runtime/Integrations/VisualScripting/Units/ScratchUnits/*`
  - 各ユニットのクラス概要やメソッド説明に Scratch ブロックの挙動をコメントで明記
- `Runtime/Core/FUnityManager.cs`
  - Scratch モードのステージサイズ適用やイベント配信に関する説明コメント

### 2-5. ドキュメント（README, Docs）

ドキュメント系ファイルでの Scratch 言及。

- `README.md`
  - 行 3: 「Scratch 風学習環境」など概要説明に複数箇所
  - 行 14-30: Scratch モード切替や機能説明で複数記載
- `CHANGELOG.md`
  - 行 10-36, 55-59 など: Scratch 座標・停止ユニット追加、VS 対応表更新などの履歴
- `Docs/VS_Scratch_Mapping.md` (約 85 ヒット)
  - VS Unit と Scratch ブロック対応表。カテゴリ命名規約 (`FUnity/Scratch/...`) や各ブロック解説を詳細に記載
- `Docs/VS_Scratch_Mapping.txt`
  - テキスト版の対応表。未実装ブロックも含む
- `Docs/FUnityDesign_UI_Toolkit.md`
  - Scratch との対比セクションやサブタイトルに記載
- `Docs/DevelopmentGuide_AI.md`
  - Scratch 本対応ガイドの表や解説

### 2-6. その他

上記に当てはまらないが Scratch を含む箇所。

- `Tools/generate_vs_scratch_mapping.py`
  - VS 対応表生成スクリプト内のラベル/カテゴリ定義や注記に Scratch が含まれる
- `Assets/FUnity/Docs/Data/FUnityActorData.md`
  - Scratch コスチューム番号や座標系の説明を含む
- `Assets/FUnity/Editor/CreateProjectData.cs`
  - Scratch モード用の ModeConfig 自動割当や警告ログ

## 3. 取り扱いメモ

- **リネーム候補（後続タスクで検討）**: UnitCategory 文字列 (`FUnity/Scratch/...`)、クラス・名前空間・フォルダ名に含まれる `Scratch`。特に Visual Scripting ユニット群やユーティリティで多数。
- **そのままが望ましい/由来説明**: README や Docs の概要説明・履歴、Scratch 由来を説明するコメント。
- **UI 表示系**: Tooltip や Debug.Log などユーザー目線で表示される文字列が複数あるため、表示文言変更時は UX への影響を確認すること。

## 4. 作業ログ

- 検索コマンド: `rg --no-heading --line-number Scratch` (総ヒット数 1112, ファイル 82)
- 追加抽出: `rg --files -g '*Scratch*'` (パスに Scratch を含む 26 件) / `rg --no-heading -n '"[^\"]*Scratch[^\"]*"'` (文字列リテラル 148 行) / `rg --no-heading -n "//.*Scratch"` (コメント 244 行) / `rg --no-heading --line-number '\\[UnitCategory\\("[^\"]*Scratch'` (UnitCategory 62 行)

