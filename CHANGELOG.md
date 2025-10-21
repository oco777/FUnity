# Changelog

## [Unreleased]
### Added
- feat(vs): Units read ActorPresenterAdapter from ScriptGraphAsset variables; editor assigns via ConfigureScriptMachine.
- feat(core): move clamping to state mutation; deprecate FooniUIBridge.ClampToPanel().
- PanelSettingsInitializer.EnsurePanelSettings() による Theme 自動割当（Editor 実行時）。
- feat(vs): scratch-ready bridge & macros（Custom Event 標準化、StageBackgroundService、Scratch Starter メニュー、マクロ雛形）。
- feat(vs): Scratch Units auto-resolve & cache ActorPresenterAdapter（ポート未接続でも動作）。

### Changed
- refactor(vs): remove ActorPresenterAdapter input ports from Scratch Units and rely on internal auto-resolution.
- refactor(vs): fix Variables.Object overload and qualify UnityEngine.Object usage in Scratch units.
- refactor(vs): stop auto-attaching ScriptMachine to 'FUnity UI'; require explicit placement
- refactor(core): rename FooniController to ActorPresenterAdapter with obsolete shim for gradual migration.
- Theme の正規パスを `Assets/FUnity/UI/USS/` に統一し、UI Builder 既定テーマを優先する運用に更新。
- Visual Scripting を必須依存に変更し、ランタイムから `UNITY_VISUAL_SCRIPTING` ガードと反射ベースの初期化を撤廃。
- ActorPresenter でステージ境界を保持し、ActorState 更新時に座標をクランプするよう統合。UI 側の `FooniUIBridge.ClampToPanel` は非推奨化。
- refactor(editor): stop auto-attaching ActorPresenterAdapter to 'FUnity UI' and expose explicit assignment in FUnityManager.

### Fixed
- fix(vs): replace Flow.TryGetValue/HasValue with Flow.GetValue and initialize local adapter references.
- fix(input): add System namespace or qualify [System.Obsolete].
- 壊れた USS や重複アセットを再生成・整理する手順を整備。
- fix(editor): avoid assigning FlowGraph.variables directly; initialize macro variables via SerializedObject managed reference.
- fix(vs): guard ScriptGraphAsset variable resolution against null FlowGraph.variables entries.

### Removed
- chore(editor): Visual Scripting 用のマクロ自動生成メニュー（`CreateFooniFloatMacro`, `CreateFooniMacros`, `CreateScratchStarterMacros`, `VisualScriptingScratchTools`）を撤去。
- VS: Fooni float units removed (`Fooni_EnableFloatUnit`, `Fooni_SetFloatAmplitudeUnit`, `Fooni_SetFloatPeriodUnit`). Feature is deprecated and no longer supported.

### Docs
- docs(vs): note Scratch Unit adapter auto-resolution and removal of Adapter ports in VS mapping.
- docs: update Runtime XML comments to clarify MVP responsibilities and UI Toolkit constraints.
- docs: update README and Docs to reflect current initialization flow, theme resolution, and VS macro auto-creation.
- README と Docs/ 配下を最新の実装と運用に合わせて全面更新。
- コーディング規約とトラブルシュートを明文化。
- Visual Scripting が前提となった手順とクイックスタートを追記。
- docs: update setup/troubleshooting guidance to remove hard dependency on 'FUnity UI' hosting ActorPresenterAdapter.
- docs: update setup/troubleshooting to remove hard dependency on 'FUnity UI' hosting ScriptMachine.

## [0.1.0] - 2024-05-04
### Added
- Recreated the FUnity Unity Package Manager structure with Runtime, Editor, UI, Art, and Samples directories.
- Added placeholder assets and scripts to ensure each folder is tracked in source control.
- Updated package metadata, documentation, and licensing information.
