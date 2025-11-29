# FUnity — Scratch スタイルで学べる Unity 教育ツール

![FUnity overview](Docs/images/readme-hero.png)

## 概要
FUnity は、Scratch 風のブロックでゲームづくりを学べる Unity 用パッケージです。Actor（スプライト）、背景、サウンドを視覚的に組み合わせ、子どもや初心者でも Unity 6 環境で直感的に作品を作成できます。

- 最新バージョン: **v0.6.0**
- Visual Scripting 互換の **ブロックモード (Block Mode)** を同梱
- Actor / Costume / Speech Balloon / Background を Scratch ライクに操作
- UPM Git URL からそのまま導入可能

## 主な機能
- **Visual Scripting 互換 Scratch ブロック**：ブロックモードのカテゴリと文言を揃え、学習用のサンプル Macro 付き。
- **Actor / Costume / Speech Balloon**：Actor のスプライト切り替え、吹き出し表示、コスチュームの状態遷移を Presenter 経由で適用。
- **Background 制御**：背景番号／背景名による切り替え、次の背景への移動、現在の番号・名前の取得といったブロックを追加し、背景カラー取得の精度も強化。
- **Sound / Effects**：サウンド再生や効果切り替えを Scratch 互換のブロックで管理。
- **Clone System**：クローン生成・停止を ScriptThreadManager が管理し、Scratch の停止ブロックとも連携。
- **Script Thread Manager**：`FUnityScriptThreadManager` が Scratch スレッドを一元管理し、StopAll のコルーチン停止を安全な `flow.StopCoroutine(false)` へ統一して「すべてを止める」「スプライトの他のスクリプトを止める」を再現。

## 動作環境
- Unity 6 (6000.x) 以降
- .NET Standard 2.1 互換ランタイム
- UI Toolkit / UI Builder
- Unity Visual Scripting 1.9.7 以降（依存として自動追加）

## インストール
`Packages/manifest.json` に Git URL を追加します。

```json
"com.papacoder.funity": "https://github.com/oco777/FUnity.git#v0.6.0"
```

UPM の **Add package from git URL...** に貼り付けても導入できます。

## 使い方の流れ
1. Unity を起動し、メニュー **FUnity/Create/FUnityProjectData** を実行してプロジェクト既定データを生成します。
2. 必要に応じて **FUnity/Create/FUnityActorData** で俳優テンプレートを追加し、スプライトや吹き出しを設定します。
3. シーンに `FUnityManager` を 1 体配置すると、起動時に Stage / Actor / UI Document が自動構築されます。
4. Visual Scripting の Macro で Scratch 互換ブロックを配置し、`VSPresenterBridge` を経由して Actor を操作します。

## Samples~/ から始める
- **BasicScene**：`Samples~/BasicScene/FUnitySample.unity` を開き、サンプル Macro で移動と吹き出しを体験できます。
- **Maze**：`Samples~/Maze` の説明に従い、StopAll → GameClearUI → Finish の流れを強化したゲームクリア演出を体験できます（演出例 UI を同梱）。

## ブロックモードの特徴
- Scratch に近い見た目とカテゴリ構成（動き／見た目／音／調べる／制御／変数）。
- ActorState の `CostumeIndex` を Presenter が受け取り、`ActorPresenter.ApplyCostumeFromState()` → `ActorView.SetSprite(Sprite)` の順に UI へ反映。
- ScriptThreadManager がイベント開始時にスレッドを登録し、停止ブロックが正しく効きます。

## ドキュメント
- [Docs/Overview.md](Docs/Overview.md)：FUnity のコンセプトとアーキテクチャ概要
- [Docs/InstallGuide.md](Docs/InstallGuide.md)：セットアップとプロジェクト生成手順
- [Docs/BlockList.md](Docs/BlockList.md)：ブロック一覧と備考
- [Docs/VS_Scratch_Mapping.md](Docs/VS_Scratch_Mapping.md)：Visual Scripting と Scratch の対応表

## ライセンスと貢献
- ライセンス: [MIT License](LICENSE.md)
- Issue / Pull Request を歓迎します。貢献時は [Docs/conventions.md](Docs/conventions.md) を参照してください。
