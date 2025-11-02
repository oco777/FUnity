# 🧩 FUnity Visual Scripting 対応表
Scratch ブロック ↔ Visual Scripting ノード 対応一覧

> ノードは Visual Scripting の検索で `Scratch/` または `Fooni/` に分類されます。

## 基本操作（移動／向き）

> **Adapter ポートは廃止:** 2025-10-19 更新より、Scratch ユニットは ActorPresenterAdapter を内部で自動解決します。優先度は「ScriptGraphAsset の Variables["adapter"] → Graph Variables → Object Variables → Self（グラフの GameObject）→ 静的キャッシュ → シーン検索」の順です。Editor メニューで生成されたマクロは、ScriptGraphAsset の Variables["adapter"] に ActorPresenterAdapter を自動登録します。

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Change X By | x座標を ◯ ずつ変える | 中心 X 座標を相対移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Change Y By | y座標を ◯ ずつ変える | 中心 Y 座標を相対移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Go To X,Y | x:◯ y:◯ へ行く | 指定中心座標（px）に移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Move Steps | ◯歩動かす | 現在の向きに沿って移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MoveStepsUnit.cs |
| Scratch/Point Direction | ◯度に向ける | 向きを絶対角度に設定 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TurnAndPointUnits.cs |
| Scratch/Set X | x座標を ◯ にする | X 座標を代入 | 未実装: 対応する Unit が見つかりません |
| Scratch/Set Y | y座標を ◯ にする | Y 座標を代入 | 未実装: 対応する Unit が見つかりません |
| Scratch/Turn Degrees | ◯度回す | アクター画像を中心ピボットで相対回転 | ActorPresenter を Graph Variables("presenter") に自動登録し、自分の UI のみ回転。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TurnAndPointUnits.cs |

## 制御（ループ／待機）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Forever | ずっと | 無限ループ | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LoopUnits.cs |
| Scratch/Repeat N | ◯ 回繰り返す | 指定回数ループ | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LoopUnits.cs |
| Scratch/Wait Seconds | ◯ 秒待つ | 指定秒だけ待機 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/WaitSecondsUnit.cs |
| Scratch/Control/If Then | もし <条件> なら | 条件が true のとき Body を 1 回実行 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ConditionUnits.cs / Body 実行後は同フレームで exit ポートに戻る |

## 調べる（入力判定）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Sensing/Key Pressed? | 〇キーが押された？ | 指定キーが押されている間は true | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/InputPredicateUnits.cs / 押下中は true（イベントの OnKeyPressed は押下瞬間のみ） |

## 表示・演出（Fooni 関連）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Say | ◯ と言う | 吹き出し表示 | 未実装: 対応する Unit が見つかりません |
| Scratch/Set Size % | 大きさを ◯ % にする | 拡大率を絶対指定で適用 (中心ピボットで拡縮) | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SizeUnits.cs |
| Scratch/Change Size by % | 大きさを ◯ % ずつ変える | 拡大率を相対変更 (中心ピボットで拡縮) | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SizeUnits.cs |

## イベント

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Events/On Key Pressed | 〇キーが押されたとき | 指定キーの押下瞬間にトリガーを発火 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/InputEventUnits.cs / ScratchKey で監視キーを選択 / 押しっぱなしでは再発火しない |
| Scratch/Broadcast Message | メッセージを送る | 指定メッセージを全リスナーへ配信 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs |
| Scratch/Broadcast And Wait | メッセージを送って待つ | 受信ハンドラがすべて完了するまで待機 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs |
| Scratch/When I Receive | メッセージを受け取ったとき | 指定メッセージ受信時にフロー発火（payload 出力あり） | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs |

---
### 補足
- 対応表は Tools/generate_vs_scratch_mapping.py により自動生成されたログをもとにしています（自動生成日時: 2025-10-21 12:25:56）。
- Scratch モードがアクティブな場合、移動系ユニットはステージ中央原点の論理座標で動作します。UI Toolkit 座標への変換はランタイムが自動で行います。
- すべての位置系ユニットは画像中心座標（px）を受け渡しします。Presenter が内部でアンカー種別に応じて補正します。
- `FUnityActorData.Anchor` を TopLeft に設定した場合でも、Visual Scripting から扱う座標は画像中心です（境界計算のみ左上基準で処理されます）。
- Scratch モードでは `ActorPresenter` が `ScratchBounds.ClampCenter` を通じて中心座標を `[-240 - width_afterScale, 240 + width_afterScale]` / `[-180 - height_afterScale, 180 + height_afterScale]` にクランプします。ユニット側での追加クランプは不要です。

### 使い方メモ
- Runner（ScriptMachine）にグラフを割り当て、`Scratch/` / `Fooni/` からノードを配置
- Scratch ユニットは `ActorPresenterAdapter` をポート経由で受け取りません。ScriptGraphAsset Variables → Graph Variables → Object Variables → Self → 静的キャッシュ → シーン検索の順で自動解決します。ScriptGraphAsset の Variables["adapter"] が最優先で参照され、Editor メニューで生成したランナーはこの値を自動で設定します。
- エディターの `FUnity/VS/Create Fooni Macros & Runner` は、生成された ScriptGraphAsset の Variables["adapter"] と Runner の Object Variables に ActorPresenterAdapter を自動で書き込みます。
- キャラクター操作は `ActorPresenterAdapter → ActorPresenter → View` で更新されます
