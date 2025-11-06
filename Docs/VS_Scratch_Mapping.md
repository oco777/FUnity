# 🧩 FUnity Visual Scripting 対応表
Scratch ブロック ↔ Visual Scripting ノード 対応一覧

> ノードは Visual Scripting の検索で `Scratch/` または `Fooni/` に分類されます。

## 基本操作（移動／向き）

> **Adapter ポートは廃止:** 2025-10-19 更新より、Scratch ユニットは ActorPresenterAdapter を内部で自動解決します。優先度は「ScriptGraphAsset の Variables["adapter"] → Graph Variables → Object Variables → Self（グラフの GameObject）→ 静的キャッシュ → シーン検索」の順です。Editor メニューで生成されたマクロは、ScriptGraphAsset の Variables["adapter"] に ActorPresenterAdapter を自動登録します。

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Change X By | x座標を ◯ ずつ変える | 中心 X 座標を相対移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Change Y By | y座標を ◯ ずつ変える | 中心 Y 座標を相対移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Go To Random Position | どこかの場所へ行く | ステージ範囲内のランダム座標へ瞬間移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Glide Seconds To Random Position | ◯ 秒でどこかの場所へ行く | 指定秒数でランダム座標へ滑らかに移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Go To X,Y | x:◯ y:◯ へ行く | 指定中心座標（px）に移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/PositionUnits.cs |
| Scratch/Glide Seconds To X,Y | ◯ 秒で x 座標を ◯ に、y 座標を ◯ にする | 指定座標へ指定秒数で移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Glide Seconds By XY Delta | ◯ 秒で x 座標を ◯ に、y 座標を ◯ に変える | 現在位置に差分 (x, y) を加算する目標へ滑らかに移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Go To Mouse Pointer | マウスのポインターへ行く | マウスポインターの論理座標へ瞬間移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Glide Seconds To Mouse Pointer | ◯ 秒でマウスのポインターへ行く | 指定秒数でマウスポインターへ移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Go To Actor By DisplayName | 他の Actor へ行く | DisplayName で指定した俳優の座標へ瞬間移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Glide Seconds To Actor By DisplayName | ◯ 秒で他の Actor へ行く | DisplayName で指定した俳優の座標へ滑らかに移動 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GoAndGlideUnits.cs |
| Scratch/Move Steps | ◯歩動かす | 現在の向きに沿って移動（1歩=1px、境界で分割移動＆反射継続） | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MoveStepsUnit.cs |
| Scratch/Point Direction | ◯度に向ける | 向きを絶対角度に設定 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TurnAndPointUnits.cs |
| Scratch/Set X | x座標を ◯ にする | X 座標を代入 | 未実装: 対応する Unit が見つかりません |
| Scratch/Set Y | y座標を ◯ にする | Y 座標を代入 | 未実装: 対応する Unit が見つかりません |
| Scratch/Turn Degrees | ◯度回す | アクター画像を中心ピボットで相対回転 | ActorPresenter を Graph Variables("presenter") に自動登録し、自分の UI のみ回転。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TurnAndPointUnits.cs |
| Scratch/Bounce If On Edge | もし端に着いたら、跳ね返る | 端接触時に方向を反射し、halfSize を考慮して内側へ押し戻す | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/BounceAndRotationStyleUnits.cs |
| Scratch/Set Rotation Style: Left-Right | 回転方向を左右のみにする | 見た目を左右反転のみで表現する回転スタイルへ切り替え | 定義: Assets/FUnity/Runtime/Integrations/VisualScripting/Units/ScratchUnits/BounceAndRotationStyleUnits.cs |
| Scratch/Set Rotation Style: Don't Rotate | 回転方向を回転しないにする | 見た目を常に直立させる回転スタイルへ切り替え | 定義: Assets/FUnity/Runtime/Integrations/VisualScripting/Units/ScratchUnits/BounceAndRotationStyleUnits.cs |
| Scratch/Set Rotation Style: All Around | 回転方向を自由に回転にする | 任意角度で回転できる既定スタイルへ戻す | 定義: Assets/FUnity/Runtime/Integrations/VisualScripting/Units/ScratchUnits/BounceAndRotationStyleUnits.cs |

## 制御（ループ／待機）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Forever | ずっと | 無限ループ | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LoopUnits.cs |
| Scratch/Repeat N | ◯ 回繰り返す | 指定回数ループ | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LoopUnits.cs |
| Scratch/Wait Seconds | ◯ 秒待つ | 指定秒だけ待機 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/WaitSecondsUnit.cs / 同期チェーンから呼ぶ場合は FUnity/Flow/To Coroutine を挟む |
| Scratch/Control/Create Clone of Self | クローンを作る（自分） | 現在の俳優 Presenter を複製 | Actor 入力不要。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/CloneUnits.cs |
| Scratch/Control/Create Clone Of (DisplayName) | クローンを作る（DisplayName 指定） | DisplayName で指定した俳優 Presenter を複製 | Value 出力に CloneAdapter（ActorPresenterAdapter）を返す。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/CloneUnits.cs |
| Scratch/Control/When I Start as a Clone | クローンされたとき | クローン生成直後にトリガーを発火 | target=Self（Runner）のカスタムイベント。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/CloneUnits.cs |
| Scratch/Control/Delete This Clone | このクローンを削除する | クローンのみ破棄（本体は警告） | Actor 入力不要。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/CloneUnits.cs |
| Scratch/Control/If Then | もし <条件> なら | 条件が true のとき Body を 1 回実行 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ConditionUnits.cs / Body 実行後は同フレームで exit ポートに戻る |

## 調べる（入力判定）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Sensing/Key Pressed? | 〇キーが押された？ | 指定キーが押されている間は true | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/InputPredicateUnits.cs / 押下中は true（イベントの OnKeyPressed は押下瞬間のみ） |
| Scratch/Sensing/Touching Mouse Pointer? | マウスポインターに触れた？ | 俳優の矩形にマウス座標が含まれるか判定 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TouchPredicates.cs |
| Scratch/Sensing/Touching Edge? | 端に触れた？ | ステージ境界へ接触しているか判定 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TouchPredicates.cs |
| Scratch/Sensing/Touching Actor By DisplayName? | ◯◯に触れた？（DisplayName） | 指定 DisplayName の俳優と矩形が重なっているか判定 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/TouchPredicates.cs |

## 表示・演出（Fooni 関連）

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Say For Seconds | ◯ と◯秒言う | 指定秒数だけ発言吹き出しを表示し自動で非表示 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SpeechUnits.cs / コルーチンで待機し、待機完了後に HideSpeech → exit |
| Scratch/Say | ◯ と言う | 発言吹き出しを無期限表示（新しい発言で上書き） | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SpeechUnits.cs / 表示後ただちに exit へ進むノンブロッキング |
| Scratch/Think For Seconds | ◯ と◯秒考える | 指定秒数だけ思考吹き出しを表示し自動で非表示 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SpeechUnits.cs / コルーチンで待機し、待機完了後に HideSpeech → exit |
| Scratch/Think | ◯ と考える | 思考吹き出しを無期限表示（新しい吹き出しで上書き） | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SpeechUnits.cs / 表示後ただちに exit へ進むノンブロッキング |
| Scratch/Set Size % | 大きさを ◯ % にする | 拡大率を絶対指定で適用 (中心ピボットで拡縮) | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SizeUnits.cs |
| Scratch/Change Size by % | 大きさを ◯ % ずつ変える | 拡大率を相対変更 (中心ピボットで拡縮) | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SizeUnits.cs |
| Show (Scratch/Looks) | 表示する | style.display を Flex に設定して俳優を表示 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/VisibilityUnits.cs / ActorPresenterAdapter は Unit 内で自動解決 |
| Hide (Scratch/Looks) | 隠す | style.display を None に設定して俳優を非表示 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/VisibilityUnits.cs / ActorPresenterAdapter は Unit 内で自動解決 |

## イベント

| VS ノード名 | Scratch 日本語 | 概要 | 備考 |
|---|---|---|---|
| Scratch/Events/When Green Flag Clicked | 緑の旗が押されたとき | 本体俳優に対して緑の旗イベントを発火 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/GreenFlagUnits.cs / FUnityManager.TriggerGreenFlag() で Runner 単位に配信（クローンは除外） |
| Scratch/Events/On Key Pressed | 〇キーが押されたとき | 指定キーの押下瞬間にトリガーを発火 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/InputEventUnits.cs / ScratchKey で監視キーを選択 / 押しっぱなしでは再発火しない |
| Scratch/Broadcast Message | メッセージを送る | 指定メッセージ名を全リスナーへ配信 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs / メッセージ名のみを送信（payload/sender ポート廃止） |
| Scratch/Broadcast And Wait | メッセージを送って待つ | EventBus.Trigger で同期的に配信し、処理完了後に続行 | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs / メッセージ名のみを送信（payload/sender ポート廃止） |
| Scratch/When I Receive | メッセージを受け取ったとき | 指定メッセージ受信時にフロー発火（message 出力のみ） | 定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MessagingUnits.cs / filter 空欄でワイルドカード受信 |

---
### 補足
- コルーチン専用ユニットを同期チェーンから呼び出す場合は `FUnity/Flow/To Coroutine` を事前に挟んでコルーチンパイプラインへ切り替えてください。
- 対応表は Tools/generate_vs_scratch_mapping.py により自動生成されたログをもとにしています（自動生成日時: 2025-10-21 12:25:56）。
- Scratch モードがアクティブな場合、移動系ユニットはステージ中央原点の論理座標で動作します。UI Toolkit 座標への変換はランタイムが自動で行います。
- すべての位置系ユニットは画像中心座標（px）を受け渡しします。Presenter が内部でアンカー種別に応じて補正します。
- `FUnityActorData.Anchor` を TopLeft に設定した場合でも、Visual Scripting から扱う座標は画像中心です（境界計算のみ左上基準で処理されます）。
- Scratch モードでは `ActorPresenter` が `ScratchBounds.ClampCenter` を通じて中心座標を `[-240 - width_afterScale, 240 + width_afterScale]` / `[-180 - height_afterScale, 180 + height_afterScale]` にクランプします。ユニット側での追加クランプは不要です。
- メッセージ関連ユニットはメッセージ名のみを送受信し、payload/sender ポートや出力は廃止されています。

### 使い方メモ
- Runner（ScriptMachine）にグラフを割り当て、`Scratch/` / `Fooni/` からノードを配置
- Scratch ユニットは `ActorPresenterAdapter` をポート経由で受け取りません。ScriptGraphAsset Variables → Graph Variables → Object Variables → Self → 静的キャッシュ → シーン検索の順で自動解決します。ScriptGraphAsset の Variables["adapter"] が最優先で参照され、Editor メニューで生成したランナーはこの値を自動で設定します。
- エディターの `FUnity/VS/Create Fooni Macros & Runner` は、生成された ScriptGraphAsset の Variables["adapter"] と Runner の Object Variables に ActorPresenterAdapter を自動で書き込みます。
- キャラクター操作は `ActorPresenterAdapter → ActorPresenter → View` で更新されます
