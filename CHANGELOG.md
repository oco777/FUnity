# Changelog

## [Unreleased]

_No unreleased changes yet._

## [v0.5.0] - 2025-11-25

### Added
- 背景を切り替えるブロックを追加し、StageData の背景リストからインデックス指定で変更できるようにしました。
- 迷路サンプルのゲームクリア演出を拡充し、StopAll → UI 表示 → Finish までの流れを整理しました。

### Changed
- 色に触れた判定のサンプリングを見直し、境界の色も検出しやすくなるよう改善しました。
- Speech bubble の反転処理を調整し、左右向きでの表示崩れを防止しました。

### Fixed
- StopAll が coroutines を正しく停止するよう `flow.StopCoroutine(false)` を用いる実装へ修正しました。
- Actor のリセット時にエフェクトが残らないよう、初期化タイミングの処理を修正しました。

## [v0.4.0] - 2025-11-23

### Added
- `FUnityProjectCreatorWindow` とプロジェクトランチャーを追加し、新規プロジェクト生成時に既定データを再利用しつつ FUnityManager を新規プロジェクトへ切り替えられるようにしました。
- ステージ背景を複数登録できるリスト対応を追加し、ModeConfig から背景スプライトのセットを柔軟に管理できるようにしました。
- パッケージに既定の UI テーマと PanelSettings リソースを同梱し、初期セットアップ時にテーマ不足でつまずかないようにしました。

### Changed
- ブロックモードの文言へ統一し、Visual Scripting のユニットカテゴリやドキュメントの「Scratch」表記を「Blocks/ブロックモード」に整理しました。
- ブロックモードのターゲット FPS を 30 に設定し、プロジェクト生成後の挙動を安定させました。
- Actor テンプレート生成時にアクティブなプロジェクト配下へ出力し、テンプレート名から DisplayName を初期化するなど作成フローを改善しました。
- PanelSettings の自動生成を停止し、既存資産を優先利用するよう初期化処理を見直しました。

### Fixed
- Sprite ベースの俳優で色の効果（Hue 回転）が正しく適用されず見た目が崩れる問題を修正しました。
- 吹き出し（Say/Think）のフォントサイズや折り返し、Drawable 要素選択の不具合を修正し、意図した外観で表示されるようにしました。

### Docs
- ブロックモードの呼称整理やセットアップ手順の変更に合わせて README や Docs の記述を更新しました。

## [v0.3.0] - 2025-11-20

### Added
- `MousePositionService` を追加し、UI Toolkit の PointerMoveEvent を Scratch 座標系へ変換するマウス座標サービスに左ボタン押下追跡を含めました。
- Visual Scripting にマウスポインター関連ユニット（x / y 座標、距離、押下判定、マウスポインターへ向ける）を `FUnity/Scratch/調べる` および `FUnity/Scratch/動き` カテゴリへ追加しました。
- `ActorPresenter` と `ActorPresenterAdapter` に SpriteList の切り替え API（`SetSpriteIndex` / `SpriteIndex` / `SpriteCount`）を追加し、アクターの見た目を複数スプライトから選択できるようにしました。
- `FUnityActorDataMigrationWindow` を追加し、旧 Portrait / PortraitSprite フィールドを Sprites リストへ一括移行できるようにしました。
- ブロックモードの停止ユニット（すべてを止める／このスクリプトを止める／スプライトの他のスクリプトを止める）と専用スレッド管理 API を追加し、Scratch の停止挙動を再現しました。

### Changed
- `ScratchUnitUtil` に `GetDirectionDegreesForCurrentMode` を追加し、ブロックモードでは上=0°/右=90°/左=-90°/下=±180°となる角度計算へ統一しました。`DirFromDegrees` もモード差を吸収するよう更新しています。
- `FUnityActorData` の見た目設定を `Sprites` リストのみで運用するよう更新し、Texture2D フォールバックを廃止しました。
- `ActorView` と `ActorPresenter` を Sprite リスト前提に再設計し、旧 Portrait 系フィールドに依存しない実装へ移行しました。

### Removed
- `FUnityActorData.EnsureSpritesMigrated()` を含む Portrait 移行用 API と関連フォールバックを削除しました。

### Docs
- `VS_Scratch_Mapping.md` にマウス座標ユニットを追記し、`AGENTS.md` と `CONTRIBUTING.md` に座標変換・カテゴリ規約の運用ルールを追加しました。
- マウスポインター関連ユニットの追加に合わせて `VS_Scratch_Mapping.md` / `AGENTS.md` / `CONTRIBUTING.md` を更新し、マウス押下追跡やグライド挙動の指針を明文化しました。
- ブロックモードで上=0°となる角度ルールと共通変換関数の利用を `AGENTS.md` / `CONTRIBUTING.md` / `Docs/VS_Scratch_Mapping.md` に追記しました。
- Sprite 運用ルールを `AGENTS.md` / `CONTRIBUTING.md` で Sprites 一本化の方針に更新しました。

## [v0.2.0] - 2025-11-09

### Added
- **FUnity/Create/FUnityProjectData** メニューを追加し、初期アセット生成と ModeConfig の自動割当（Scratch / Unityroom 用）をワンコマンド化しました。
- **FUnity/Create/FUnityActorData** メニューから Actor 用 UXML・USS・FUnityActorData・ScriptGraph を一括生成するテンプレートフローを整備しました。
- Scratch 互換の Visual Scripting ユニットを大幅拡充（緑の旗イベント、クローン管理、メッセージ送受信、セリフ／考える吹き出し、角度・サイズ・表示制御）し、Presenter 連携で UI を即時更新します。
- ScriptGraphAsset 上の ActorPresenterAdapter 解決や StageBackgroundService など、Scratch ワークフローに必要なマクロ／ブリッジ類を同梱しました。
- PanelSettingsInitializer.EnsurePanelSettings() により、エディタ実行時でもテーマと PanelSettings を自動生成・割当する仕組みを導入しました。

### Changed
- Actor テンプレート生成時の出力先を `Assets/FUnity/Actors/<ActorName>/` に統一し、関連ファイルがまとまるよう整理しました。
- Primary Color の既定値を `RGBA(0,0,0,0)` に変更し、テンプレート生成直後のデザインをニュートラルな状態から調整できるようにしました。
- Visual Scripting 依存を明示して `UNITY_VISUAL_SCRIPTING` ガードや自動 ScriptMachine 付与を撤廃し、FUnityManager から明示的に管理する構成へ移行しました。
- ActorPresenter がステージ境界クランプを担うよう再設計し、UI 側の `FooniUIBridge.ClampToPanel()` を非推奨化しました。
- メニュー体系を見直し、Authoring/Diagnostics に分散していた機能を Create 配下へ集約することで学習コストを削減しました。

### Fixed
- Visual Scripting のメッセージングと緑の旗イベントを共通 EventHook で処理し、ペイロード未設定時でも例外が発生しないよう防止しました。
- ScriptGraphAsset 変数の null 参照や FlowGraph 直接操作による初期化不備を解消し、マクロ生成時の安定性を向上しました。
- セットアップ手順で PanelSettings テーマ／Fooni プレースホルダーの不足を自動検出・補完し、初回起動時の警告を削減しました。
- 破損した USS や重複アセットを再生成するユーティリティとトラブルシュート手順を整備し、環境差異による崩れを解消しました。

### Removed
- 旧来のランタイム管理 MonoBehaviour と Visual Scripting ブリッジを削除し、`FUnityManager` / `VSPresenterBridge` / `ActorPresenterAdapter` に統一しました。
- 使用されていなかった Editor メニュー（`FUnity/Tools/Fix Runtime Layout` など）と `GenerateActorUITemplateWindow` / `StageBackgroundDiagnosticsMenu` などの補助クラスを整理しました。
- Scratch Float 機能を正式に廃止し、`Fooni_EnableFloatUnit` など関連ユニットと自動生成メニューを撤去しました。

### Docs
- README と Docs 配下を刷新し、Visual Scripting 前提の初期化手順・メニュー構成・テーマ解決フローを最新実装に合わせました。
- VS Scratch 対応表に新規ユニット（緑の旗、クローン、メッセージ、サイズ／表示／回転）と Presenter 連携の挙動を追記しました。
- トラブルシューティングやコーディング規約を整理し、Actor テンプレート生成や ModeConfig 自動設定の注意点を明文化しました。

### Internal
- UPM パッケージ利用を想定し、ModeConfig 検出ロジックやアセット探索のパス解決を再実装して複数配置（`Assets/` / `Packages/com.papacoder.funity`）をサポートしました。
- ドキュメントと実装のメニュー表記を統一し、セットアップウィザードや各種警告ログの文言を最新版に揃えました。

## [0.1.0] - 2024-05-04
### Added
- Recreated the FUnity Unity Package Manager structure with Runtime, Editor, UI, Art, and Samples directories.
- Added placeholder assets and scripts to ensure each folder is tracked in source control.
- Updated package metadata, documentation, and licensing information.
