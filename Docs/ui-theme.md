# UI テーマ適用戦略

Default Project Data による PanelSettings/Theme の解決ロジックをまとめます。

## 目次
- [テーマ探索の優先度](#テーマ探索の優先度)
- [FUnityPanelSettings.asset の生成](#funitypanelsettingsasset-の生成)
- [運用上のヒント](#運用上のヒント)
- [関連ドキュメント](#関連ドキュメント)

## テーマ探索の優先度
`Assets/FUnity/Editor/CreateProjectData.cs` は次の順序で ThemeStyleSheet を決定します。
1. `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss`（Unity 提供の Legacy Theme）。存在すればこれを `FUnityPanelSettings.asset` に割り当てます。
2. 上記が見つからない場合、`Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss` を新規生成し、`FUnityPanelSettings.asset` に割り当てます。

生成後は PanelSettings の `themeStyleSheets` に 1 件だけ登録されます。既存プロジェクトでカスタム Theme を使用したい場合は、`FUnityPanelSettings.asset` の `themeStyleSheets` に追加で差し替えてください。

## FUnityPanelSettings.asset の生成
- `Assets/FUnity/UI/FUnityPanelSettings.asset` が存在しない場合に自動生成されます。
- Editor スクリプトは `SerializedObject` を用いて `PanelSettings` の `themeStyleSheets` 配列へ Theme アセットを追加し、変更を `ApplyModifiedProperties()` で保存します。
- Runtime では `FUnityManager` がこの PanelSettings を `UIDocument` に設定し、再生時に “FUnity UI” が Theme 付きで表示されます。

## 運用上のヒント
- Theme が null と表示される場合は `Assets/UI Toolkit/UnityThemes/` を確認し、存在しない場合は Default Project Data を再実行してフォールバック Theme を生成します。
- Unity の Legacy Theme を利用する場合でも、`Assets/FUnity/UI/FUnityPanelSettings.asset` はリポジトリに保持してください。ビルド環境で Theme を再生成する必要がなくなります。
- PanelSettings を手動で削除した場合、次回 Default Project Data 実行時に再生成されます。差分が大きい場合は Git で不要なアセットが残っていないか確認します。

## 関連ドキュメント
- [導入手順](setup.md)
- [既定データの構成](data-defaults.md)
- [トラブルシュート集](troubleshooting.md)
