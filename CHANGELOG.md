# Changelog

## [Unreleased]
### Added
- PanelSettingsInitializer.EnsurePanelSettings() による Theme 自動割当（Editor 実行時）。
- feat(vs): scratch-ready bridge & macros（Custom Event 標準化、StageBackgroundService、Scratch Starter メニュー、マクロ雛形）。

### Changed
- Theme の正規パスを `Assets/FUnity/UI/USS/` に統一し、UI Builder 既定テーマを優先する運用に更新。
- Visual Scripting を必須依存に変更し、ランタイムから `UNITY_VISUAL_SCRIPTING` ガードと反射ベースの初期化を撤廃。
- ActorPresenter でステージ境界を保持し、ActorState 更新時に座標をクランプするよう統合。UI 側の `FooniUIBridge.ClampToPanel` は非推奨化。

### Fixed
- 壊れた USS や重複アセットを再生成・整理する手順を整備。

### Docs
- docs: update Runtime XML comments to clarify MVP responsibilities and UI Toolkit constraints.
- docs: update README and Docs to reflect current initialization flow, theme resolution, and VS macro auto-creation.
- README と Docs/ 配下を最新の実装と運用に合わせて全面更新。
- コーディング規約とトラブルシュートを明文化。
- Visual Scripting が前提となった手順とクイックスタートを追記。

## [0.1.0] - 2024-05-04
### Added
- Recreated the FUnity Unity Package Manager structure with Runtime, Editor, UI, Art, and Samples directories.
- Added placeholder assets and scripts to ensure each folder is tracked in source control.
- Updated package metadata, documentation, and licensing information.
