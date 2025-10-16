# 環境構築ガイド

## 目次
- [要点](#要点)
- [手順](#手順)
- [補足](#補足)
- [参考](#参考)

## 要点
- Unity 6 (6000.x) と .NET Standard 2.1 を満たす環境を用意する。
- FUnity パッケージを UPM もしくはサブモジュールで導入する。
- サンプルシーンと Default Project Data を利用して挙動を確認する。
- Unity Visual Scripting 1.9.7 以降が必須（FUnity を導入すると依存関係として自動追加される）。

## 手順
### 1. 必須ソフトウェア
- Unity Hub から Unity 6 LTS をインストールする。
- UI Toolkit と UI Builder パッケージを Package Manager で有効化する。
- Visual Scripting パッケージは FUnity の依存関係として取得されるが、既存プロジェクトで無効化している場合は有効化しておく。

### 2. パッケージ導入
- `Packages/manifest.json` に Git URL を追加する。

```json
"com.papacoder.funity": "https://github.com/oco777/FUnity.git"
```

- 既存環境でソースを保持したい場合は次を実行する。

```bash
git submodule add https://github.com/oco777/FUnity.git Packages/com.papacoder.funity
```

### 3. サンプルシーンの確認
- Package Manager → Samples → **FUnitySample** をインポートする。
- `Assets/FUnity/Samples/FUnitySample.unity` を開いて再生する。
- シーンには **FUnityManager** を 1 体置くだけでよく、再生開始時に `FUnity UI` GameObject と UIDocument が自動生成される。
- ブロック UI・俳優ウィンドウ・背景表示を確認する。
- `FUnity UI` には `ScriptMachine` と `FooniUIBridge` が自動付与され、同梱の Visual Scripting グラフからフーニーを移動できる。

### 4. Default Project Data の生成
- メニュー **FUnity → Create → Default Project Data** を選ぶ。
- `Assets/FUnity/` 配下に Project/Stage/Actor/PanelSettings/Theme が生成される。
- 生成直後にシーンを再生して Theme が割り当てられたか確認する。
- FUnityManager が Resources からデータを読み、実行時に `FUnity UI` を生成して背景とフーニーを表示する。

## 補足
- StageData は `Art/Backgrounds/Background_01.png` を背景として設定する。
- ActorData は `FUnityActorData_Fooni` を生成し、Portrait/UXML/USS を候補優先で解決する。
- PanelSettings は Editor 実行時に Theme を割り当て、Resources に保存する。

## 参考
- [UI テーマ適用戦略](ui-theme.md)
- [既定データの構成](data-defaults.md)
- [トラブルシュート集](troubleshooting.md)
