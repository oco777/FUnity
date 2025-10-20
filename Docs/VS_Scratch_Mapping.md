# 🧩 FUnity Visual Scripting 対応表
Scratch ブロック ↔ Visual Scripting ノード 対応一覧

> ノードは Visual Scripting の検索で `Scratch/` または `Fooni/` に分類されます。

## 基本操作（移動／向き）

> **Adapter ポートは任意:** 2025-10-19 更新より、Scratch ユニットは ActorPresenterAdapter（旧称 FooniController）を自動解決します。優先度は「明示ポート → ScriptGraphAsset の Variables["adapter"] → Graph Variables → Object Variables → Self（グラフの GameObject）→ 静的キャッシュ → シーン検索」の順です。未接続でも動作し、必要であれば従来どおりポート接続も利用できます。Editor メニューで生成されたマクロは、ScriptGraphAsset の Variables["adapter"] に ActorPresenterAdapter を自動登録します。

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Change X By | x座標を ◯ ずつ変える | X 座標を相対移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Change Y By | y座標を ◯ ずつ変える | Y 座標を相対移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Go To X,Y | x:◯ y:◯ へ行く | 指定座標（px）に移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Move Steps | ◯歩動かす | 現在の向きに沿って移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MoveStepsUnit.cs |
| Scratch/Point Direction | ◯度に向ける | 向きを絶対角度に設定 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TurnAndPointUnits.cs |
| Scratch/Set X | x座標を ◯ にする | X 座標を代入 | 未実装: 対応する Unit が見つかりません |
| Scratch/Set Y | y座標を ◯ にする | Y 座標を代入 | 未実装: 対応する Unit が見つかりません |
| Scratch/Turn Degrees | ◯度回す | 時計回り・反時計回りに回転 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TurnAndPointUnits.cs |

## 制御（ループ／待機）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Forever | ずっと | 無限ループ | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LoopUnits.cs |
| Scratch/Repeat N | ◯ 回繰り返す | 指定回数ループ | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LoopUnits.cs |
| Scratch/Wait Seconds | ◯ 秒待つ | 指定秒だけ待機 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/WaitSecondsUnit.cs |

## 表示・演出（Fooni 関連）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Fooni/Enable Float | ふわふわ移動をオンにする | 浮遊アニメ開始 | 定義: Runtime/Integrations/VisualScripting/Units/FooniFloatUnits.cs |
| Fooni/Set Float Amplitude | ふわふわの振幅を ◯ にする | 振幅の設定 | 定義: Runtime/Integrations/VisualScripting/Units/FooniFloatUnits.cs |
| Fooni/Set Float Period | ふわふわの周期を ◯ 秒にする | 周期の設定 | 定義: Runtime/Integrations/VisualScripting/Units/FooniFloatUnits.cs |
| Scratch/Say | ◯ と言う | 吹き出し表示 | 未実装: 対応する Unit が見つかりません |

---
### 補足
- 対応表は Tools/generate_vs_scratch_mapping.py により自動生成されたログをもとにしています（自動生成日時: 2025-10-19 11:47:10）。

### 使い方メモ
- Runner（ScriptMachine）にグラフを割り当て、`Scratch/` / `Fooni/` からノードを配置
- Scratch ユニットは `ActorPresenterAdapter` ポート未接続でも、明示ポート → ScriptGraphAsset Variables → Graph Variables → Object Variables → Self → 静的キャッシュ → シーン検索の順に自動解決します（従来どおりポート接続も可能）。ScriptGraphAsset の Variables["adapter"] が最優先で参照され、Editor メニューで生成したランナーはこの値を自動で設定します。
- エディターの `FUnity/VS/Create Fooni Macros & Runner` は、生成された ScriptGraphAsset の Variables["adapter"] と Runner の Object Variables に ActorPresenterAdapter（旧称 FooniController）を自動で書き込みます。
- キャラクター操作は `ActorPresenterAdapter → ActorPresenter → View` で更新されます
