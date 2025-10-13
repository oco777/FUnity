# FAQ

## 目次
- [要点](#要点)
- [手順](#手順)
- [補足](#補足)
- [参考](#参考)

## 要点
- Theme と PanelSettings の配置、俳優データの編集方法をまとめる。
- FUnity 配下にアセットを集約し、Legacy を廃止する方針を共有する。
- サンプルシーンと Default Project Data を活用して挙動を確認する。

## 手順
### Q1. Theme ファイルはどこに置くべきか？
- まず `Assets/UI Toolkit/UnityThemes/Unity Default Runtime Theme.uss` を用意する。
- ない場合は `Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss` を生成し、PanelSettings に割り当てる。

### Q2. 俳優の画像を差し替えるには？
- `Assets/FUnity/Art/Characters/` に PNG を配置する。
- `FUnityActorData_Fooni` の Portrait 参照を書き換える。
- 必要に応じて UXML/USS もテンプレートを複製し調整する。

### Q3. Default Project Data を再生成しても大丈夫か？
- 既存アセットは上書きされず、重複した場合は手動で整理する。
- バージョン管理で不要なアセットが残らないよう確認する。

### Q4. Package Manager で更新が検出されない場合は？
- `Packages/manifest.json` の Git URL にハッシュを指定して更新する。
- 例：`"com.papacoder.funity": "https://github.com/oco777/FUnity.git#main"`
- キャッシュを削除する場合は `Library/PackageCache` を消去して再読み込みする。

### Q5. Build 時に Theme が消える。
- Editor で生成した `Resources/FUnityPanelSettings.asset` をリポジトリに含める。
- カスタム Theme を `Assets/FUnity/UI/USS/` に配置してからビルドする。

## 補足
- 俳優データの追加は今後 Docs に手順を追記予定（TODO）。
- 追加の FAQ があれば Issue で提案する。

## 参考
- [環境構築ガイド](setup.md)
- [UI テーマ適用戦略](ui-theme.md)
- [既定データの構成](data-defaults.md)
