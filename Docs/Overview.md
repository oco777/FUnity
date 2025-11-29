# Overview

FUnity は「Scratch の楽しさを Unity の表現力で広げる」ことを目的に設計された教育向けツールキットです。Scratch 互換のブロックモードを備え、既存の Unity プロジェクトへ追加するだけで、Actor（キャラクター）とステージを用いた学習体験を提供します。

## コンセプト
- **Scratch ライクな操作感**：カテゴリやブロック名を合わせ、子どもが違和感なく移行できる UI を目指します。
- **MVP アーキテクチャ**：Model（ScriptableObject とランタイム状態）、View（UI Toolkit）、Presenter（状態更新）の三層で責務を分離し、テストしやすい構造を維持します。
- **Unity との親和性**：UI Toolkit / Visual Scripting / UPM と連携し、既存資産やエディタ拡張と共存できる形で提供します。

## Scratch との互換性
- ブロックカテゴリは「動き／見た目／音／調べる／制御／変数」を用意し、Scratch の代表的なブロックを Visual Scripting Unit として提供します。
- 停止ブロックやクローン、背景切り替えなど、Scratch の学習パターンを維持したまま Unity の描画と入力に接続します。
- ActorState の `CostumeIndex` を Presenter が適用し、`ActorPresenter.ApplyCostumeFromState()` → `ActorView.SetSprite(Sprite)` の順で UI へ反映することで、Scratch の「コスチューム」切り替えと同等の体験を実現します。

## アーキテクチャ概要
```
+-----------------------+        +---------------------+
|   ScriptThreadManager | <----> | VSPresenterBridge   |
+-----------+-----------+        +----------+----------+
            ^                               |
            | registers/stop                | calls presenter methods
            |                               v
+-----------+-----------+        +---------------------+
|     FUnityManager     | -----> |   ActorPresenter    |
+-----------+-----------+        +----------+----------+
            |                               |
            v                               v
+-----------------------+        +---------------------+
|       StageView       |        |     ActorView       |
+-----------------------+        +---------------------+
```
- **Manager**：`FUnityManager` が Stage と Actor を初期化し、Presenter と View を結線します。
- **ActorView**：UI Toolkit 上のスプライトや吹き出しを描画する View 層。Presenter からの命令のみで更新します。
- **Stage**：背景やステージサイズを管理し、背景リストからの切り替えも担当します。
- **ScriptThreadManager**：Scratch のイベント開始時にスレッドを登録し、「すべてを止める」などの停止ブロックを管理します。

## Block Mode
- Visual Scripting の Unit を Scratch 表記で提供し、`FUnity/Scratch/<カテゴリ>` に分類しています。
- 角度はブロックモードに合わせて「上=0° / 右=90° / 下=180° / 左=270°」を維持します。
- ScriptMachine に Macro を割り当てるだけでブロックモードを利用でき、C# とのハイブリッドも可能です。
- Maze サンプルには StopAll から GameClear UI 表示までの演出例を含め、学習フローを追体験できます。
