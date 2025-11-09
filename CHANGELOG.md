# Changelog

## [Unreleased]

_No entries yet._

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
