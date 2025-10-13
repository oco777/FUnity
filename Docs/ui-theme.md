# UI テーマ適用戦略

## 目次
- [要点](#要点)
- [手順](#手順)
- [補足](#補足)
- [参考](#参考)

## 要点
- UI Builder 標準の Theme を最優先で使用し、存在しない場合は FUnity が安全ミニマムを生成する。
- PanelSettingsInitializer.EnsurePanelSettings() が Editor 実行時に Theme 付き PanelSettings を提供する。
- UNITY_EDITOR ガードを用いてビルド時は既存アセットを参照する運用に切り替える。

## 手順
### 1. Theme の探索
- `Assets/UI Toolkit/UnityThemes/Unity Default Runtime Theme.uss` を最初に探す。
- 見つからない場合は `Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss` を生成して割り当てる。
- Theme が破損している場合はメニューから Default Project Data を再生成して復元する。

### 2. PanelSettings の生成
- `PanelSettingsInitializer.EnsurePanelSettings()` が BeforeSceneLoad で呼ばれる。
- `Resources/FUnityPanelSettings.asset` を生成し、Theme の `styleSheet` に参照を追加する。
- Editor 実行中は `UNITY_EDITOR` ガード内で Theme が存在するか検証し、足りない場合は生成する。

### 3. ビルド時の取り扱い
- ビルドプロセスでは `Resources/FUnityPanelSettings.asset` がそのまま含まれる。
- ビルドマシンで Theme を再生成できないため、Editor で生成した Theme をリポジトリに保持する。
- カスタム Theme を利用する場合は `Assets/FUnity/UI/USS/` に配置し、PanelSettings の参照を書き換える。

## 補足
- UI Toolkit 既定 Theme と FUnity 生成 Theme のファイル名差異に注意する。
- `Unity Default Runtime Theme.uss` と `UnityDefaultRuntimeTheme.uss` を混在させない。
- PanelSettings を手動で削除した場合は再生時に自動復旧するが、バージョン管理では追跡しておく。
- UNITY_EDITOR ガード内でのみ SerializedObject 差分の吸収を行い、ビルド時は純粋なアセット参照とする。

## 参考
- [環境構築ガイド](setup.md)
- [既定データの構成](data-defaults.md)
- [トラブルシュート集](troubleshooting.md)
