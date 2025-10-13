# トラブルシュート集

## 目次
- [要点](#要点)
- [手順](#手順)
- [補足](#補足)
- [参考](#参考)

## 要点
- USS や Theme の破損は Default Project Data の再生成で復旧する。
- 重複アセットは FUnity 配下に統一し、Legacy 経路を掃除する。
- NullReferenceException は PanelSettings と ActorData の参照不足が原因となる。

## 手順
### 1. USS が壊れている
- コンソールに `Unsupported selector format: '---'` が出たら `UnityDefaultRuntimeTheme.uss` を削除する。
- メニュー **FUnity → Create → Default Project Data** を再実行し、新しい USS を生成する。

### 2. Theme が見つからない
- `Unity Default Runtime Theme.uss` と `UnityDefaultRuntimeTheme.uss` の表記ゆれを確認する。
- PanelSettings の `styleSheets` から不足している参照を再設定する。
- `Assets/UI Toolkit/UnityThemes/` に Theme がある場合はそちらを優先する。

### 3. PanelSettings が生成されない
- `Resources/FUnityPanelSettings.asset` を削除したか確認する。
- エディタを再生すると `PanelSettingsInitializer.EnsurePanelSettings()` が自動復旧する。
- スクリプトエラーで停止している場合は Console を確認し、再生をやり直す。

### 4. 俳優 UI が表示されない
- ActorData の Portrait/UXML/USS 参照を点検する。
- UXML に `name="root"/"portrait"` がない場合はテンプレートを修正する。
- 画像パスが移動している場合は `Assets/FUnity/Art/Characters/` に戻す。

## 補足
- 再生成後は Git の差分で余分なアセットが残っていないか確認する。
- PanelSettings を複数置くと UI Document の参照が競合するため 1 つに統一する。
- Legacy フォルダを掃除した後は meta ファイルを更新して差分を確定させる。

## 参考
- [UI テーマ適用戦略](ui-theme.md)
- [既定データの構成](data-defaults.md)
- [環境構築ガイド](setup.md)
