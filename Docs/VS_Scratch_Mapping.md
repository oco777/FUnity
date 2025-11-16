# 🧩 FUnity Visual Scripting 対応表

Scratch ブロックと FUnity 独自 Visual Scripting Unit の対応関係です。すべての Unit は日本語タイトル・カテゴリ・共通アイコン規約に従っており、新規追加や変更時には本表を必ず更新してください。

## 運用ルール
- `[UnitTitle]` は Scratch 日本語ブロック名に合わせるか、Scratch 流儀の短い日本語で命名する。
- `[UnitCategory]` は Scratch 系ユニットの場合、イベント系は `Events/FUnity/Scratch/◯◯`、その他は `FUnity/Scratch/◯◯` 形式（カテゴリ名は日本語）で統一する。拡張ユニットは `FUnity/Scratch/拡張` を使用する。
- `[TypeIcon(typeof(FUnityScratchUnitIcon))]` を全ユニットへ付与し、FUnity Scratch 系ユニットであることを明示する。
- ノード検索性向上のため、利用可能な場合は `[UnitSubtitle]`（または同等の検索キーワード属性）に `funity scratch` とカテゴリ名・日本語/英語の関連語を半角スペース区切りで登録する（例：`funity scratch 見た目 say speech`）。
- コード変更と同じ PR でこの対応表を更新し、タイトルやカテゴリの差異が無いよう同期する。

## 動き
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| ○歩動かす | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.MoveStepsUnit | ○歩動かす | FUnity/Scratch/動き | 境界反射と残距離再移動に対応。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MoveStepsUnit.cs |
| もし端に着いたら、跳ね返る | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.BounceIfOnEdgeUnit | もし端に着いたら、跳ね返る | FUnity/Scratch/動き | 反射後に中心座標をステージ内へ押し戻す。定義: Runtime/.../BounceAndRotationStyleUnits.cs |
| 回転方法を左右のみにする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetRotationStyleLeftRightUnit | 回転方法を左右のみにする | FUnity/Scratch/動き | 左右反転のみ許可。定義: Runtime/.../BounceAndRotationStyleUnits.cs |
| 回転方法を回転しないにする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetRotationStyleDontRotateUnit | 回転方法を回転しないにする | FUnity/Scratch/動き | 見た目を常に直立へ固定。定義: Runtime/.../BounceAndRotationStyleUnits.cs |
| 回転方法を自由に回転するにする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetRotationStyleAllAroundUnit | 回転方法を自由に回転するにする | FUnity/Scratch/動き | 既定の自由回転へ戻す。定義: Runtime/.../BounceAndRotationStyleUnits.cs |
| ランダムな場所へ行く | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GoToRandomPositionUnit | ランダムな場所へ行く | FUnity/Scratch/動き | 論理座標でランダム移動。定義: Runtime/.../GoAndGlideUnits.cs |
| ○秒でランダムな場所へ行く | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GlideSecondsToRandomPositionUnit | ○秒でランダムな場所へ行く | FUnity/Scratch/動き | コルーチンで滑らかに移動。定義: Runtime/.../GoAndGlideUnits.cs |
| マウスポインターへ行く | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GoToMousePointerUnit | マウスポインターへ行く | FUnity/Scratch/動き | 推定マウス座標へ瞬間移動。定義: Runtime/.../GoAndGlideUnits.cs |
| ○秒でマウスポインターへ行く | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GlideSecondsToMousePointerUnit | ○秒でマウスポインターへ行く | FUnity/Scratch/動き | 指定秒数でマウスへ移動。定義: Runtime/.../GoAndGlideUnits.cs |
| ○へ行く | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GoToActorByDisplayNameUnit | ○へ行く | FUnity/Scratch/動き | DisplayName 指定で別俳優へ瞬間移動。定義: Runtime/.../GoAndGlideUnits.cs |
| ○秒で○へ行く | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GlideSecondsToActorByDisplayNameUnit | ○秒で○へ行く | FUnity/Scratch/動き | DisplayName 指定で滑らかに移動。定義: Runtime/.../GoAndGlideUnits.cs |
| ○秒で x を○、y を○ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GlideSecondsByXYDeltaUnit | ○秒で x を○、y を○ずつ変える | FUnity/Scratch/動き | 差分移動を時間指定。定義: Runtime/.../GoAndGlideUnits.cs |
| ○秒で x 座標を○に、y 座標を○にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GlideSecondsToXYUnit | ○秒で x 座標を○に、y 座標を○にする | FUnity/Scratch/動き | 絶対座標へ時間指定で移動。定義: Runtime/.../GoAndGlideUnits.cs |
| x:○ y:○ へ行く | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GoToXYUnit | x:○ y:○ へ行く | FUnity/Scratch/動き | 絶対座標へ瞬間移動。定義: Runtime/.../PositionUnits.cs |
| x座標を○ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ChangeXByUnit | x座標を○ずつ変える | FUnity/Scratch/動き | X 座標を相対移動。定義: Runtime/.../PositionUnits.cs |
| y座標を○ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ChangeYByUnit | y座標を○ずつ変える | FUnity/Scratch/動き | Y 座標を相対移動。定義: Runtime/.../PositionUnits.cs |
| ○度回す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.TurnDegreesUnit | ○度回す | FUnity/Scratch/動き | 向きを相対回転。定義: Runtime/.../TurnAndPointUnits.cs |
| ○度に向ける | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.PointDirectionUnit | ○度に向ける | FUnity/Scratch/動き | 向きを絶対設定。定義: Runtime/.../TurnAndPointUnits.cs |
| マウスポインターへ向ける | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.PointTowardsMousePointerUnit | マウスポインターへ向ける | FUnity/Scratch/動き | カーソル方向へ即時旋回。定義: Runtime/.../TurnAndPointUnits.cs |

> **角度の扱い:** Runtime/Integrations/VisualScripting/Units/ScratchUnits/ScratchUnitUtil.cs の `GetDirectionDegreesForCurrentMode` を経由し、Scratch モードでは上=0°/右=90°/左=-90°/下=±180°、通常モードでは従来通り右=0° の角度ルールを適用しています。最終的に Presenter や ActorState へ渡す際は Runtime/Core/ScratchAngleUtil.cs を使い、必ず内部角度（0°=右）へ変換してから `ActorPresenterAdapter.SetDirection` に渡します。

## 見た目
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| コスチュームを ( ) にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetCostumeNumberUnit | コスチュームを〇にする | FUnity/Scratch/見た目 | ActorState.CostumeIndex と FUnityActorData.Sprites を利用。定義: Runtime/.../CostumeUnits.cs |
| 次のコスチュームにする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.NextCostumeUnit | 次のコスチュームにする | FUnity/Scratch/見た目 | コスチュームを 1 → 2 → … → N → 1 と循環。定義: Runtime/.../CostumeUnits.cs |
| コスチュームの番号 | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.CostumeNumberUnit | コスチュームの番号 | FUnity/Scratch/見た目 | 1 始まりの Scratch コスチューム番号を返す。定義: Runtime/.../CostumeUnits.cs |
| 大きさを○%にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetSizePercentUnit | 大きさを○%にする | FUnity/Scratch/見た目 | 拡大率を絶対設定。定義: Runtime/.../SizeUnits.cs |
| 大きさを○%ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ChangeSizeByPercentUnit | 大きさを○%ずつ変える | FUnity/Scratch/見た目 | 拡大率を相対変更。定義: Runtime/.../SizeUnits.cs |
| ○と○秒言う | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SayForSecondsUnit | ○と○秒言う | FUnity/Scratch/見た目 | 指定秒数で吹き出し表示。定義: Runtime/.../SpeechUnits.cs |
| ○と言う | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SayUnit | ○と言う | FUnity/Scratch/見た目 | 無期限の発言吹き出し。定義: Runtime/.../SpeechUnits.cs |
| ○と○秒考える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ThinkForSecondsUnit | ○と○秒考える | FUnity/Scratch/見た目 | 指定秒数で思考吹き出し。定義: Runtime/.../SpeechUnits.cs |
| ○と考える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ThinkUnit | ○と考える | FUnity/Scratch/見た目 | 無期限の思考吹き出し。定義: Runtime/.../SpeechUnits.cs |
| 色の効果を○ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ChangeColorEffectByUnit | 色の効果を○ずつ変える | FUnity/Scratch/見た目 | 色効果を相対変更。定義: Runtime/.../EffectUnits.cs |
| 色の効果を○にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetColorEffectToUnit | 色の効果を○にする | FUnity/Scratch/見た目 | 色効果を絶対設定。定義: Runtime/.../EffectUnits.cs |
| 画像効果をなくす | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ClearGraphicEffectsUnit | 画像効果をなくす | FUnity/Scratch/見た目 | Tint をリセット。定義: Runtime/.../EffectUnits.cs |
| 表示する | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ShowActorUnit | 表示する | FUnity/Scratch/見た目 | 俳優を表示状態へ。定義: Runtime/.../VisibilityUnits.cs |
| 隠す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.HideActorUnit | 隠す | FUnity/Scratch/見た目 | 俳優を非表示へ。定義: Runtime/.../VisibilityUnits.cs |

## 制御
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| ○回繰り返す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.RepeatNUnit | ○回繰り返す | FUnity/Scratch/制御 | 指定回数ループ。定義: Runtime/.../LoopUnits.cs |
| ずっと | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ForeverUnit | ずっと | FUnity/Scratch/制御 | 永続ループ。定義: Runtime/.../LoopUnits.cs |
| ○秒待つ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WaitSecondsUnit | ○秒待つ | FUnity/Scratch/制御 | 指定時間待機。定義: Runtime/.../WaitSecondsUnit.cs |
| 自分のクローンを作る | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.CreateCloneOfSelfUnit | 自分のクローンを作る | FUnity/Scratch/制御 | 自身を複製。定義: Runtime/.../CloneUnits.cs |
| ○のクローンを作る | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.CreateCloneOfDisplayNameUnit | ○のクローンを作る | FUnity/Scratch/制御 | 指定俳優を複製。定義: Runtime/.../CloneUnits.cs |
| クローンされたとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WhenIStartAsCloneUnit | クローンされたとき | Events/FUnity/Scratch/制御 | クローン生成時イベント。定義: Runtime/.../CloneUnits.cs |
| このクローンを削除する | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.DeleteThisCloneUnit | このクローンを削除する | FUnity/Scratch/制御 | クローンを破棄。定義: Runtime/.../CloneUnits.cs |
| すべてを止める | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StopAllUnit | Scratch/すべてを止める | FUnity/Scratch/制御 | ScriptThreadManager で全スレッド停止。定義: Runtime/.../StopControlUnits.cs |
| このスクリプトを止める | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StopThisScriptUnit | Scratch/このスクリプトを止める | FUnity/Scratch/制御 | 現在スレッドのみ停止。定義: Runtime/.../StopControlUnits.cs |
| スプライトの他のスクリプトを止める | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StopOtherScriptsInSpriteUnit | Scratch/スプライトの他のスクリプトを止める | FUnity/Scratch/制御 | 同俳優の他スレッド停止。定義: Runtime/.../StopControlUnits.cs |
| もし○なら | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.IfThenUnit | もし○なら | FUnity/Scratch/制御 | 条件成立時のみ本体を実行。定義: Runtime/.../ConditionUnits.cs |

> **補足:** Scratch 系の停止ユニットは、`FUnityScriptThreadManager` に用意した Scratch 専用スレッドテーブルを利用してコルーチンを管理します。Step1 ではスレッド登録 API（`RegisterScratchThread` など）を追加しただけで、Unit からの呼び出しは今後のステップで接続します。現在は Guid ベースの `TryGetThreadContext` を明示的に使用し、停止対象のスレッド ID を確実に取得する実装に統一しています。

## イベント
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| 緑の旗が押されたとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WhenGreenFlagClickedUnit | 緑の旗が押されたとき | Events/FUnity/Scratch/イベント | Runner 対象の緑の旗イベント。開始時に Scratch 用スレッドを登録。定義: Runtime/.../GreenFlagUnits.cs |
| ○キーが押されたとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.OnKeyPressedUnit | ○キーが押されたとき | Events/FUnity/Scratch/イベント | 押下エッジで発火。定義: Runtime/.../InputEventUnits.cs |
| メッセージを送る | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.BroadcastMessageUnit | メッセージを送る | FUnity/Scratch/イベント | 即時配信。定義: Runtime/.../MessagingUnits.cs |
| メッセージを送って待つ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.BroadcastAndWaitUnit | メッセージを送って待つ | FUnity/Scratch/イベント | 同期配信後に継続。定義: Runtime/.../MessagingUnits.cs |
| メッセージを受け取ったとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WhenIReceiveMessageUnit | メッセージを受け取ったとき | Events/FUnity/Scratch/イベント | フィルタ一致時に発火。定義: Runtime/.../MessagingUnits.cs |

## 調べる
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| ○キーが押された？ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.KeyIsPressedUnit | ○キーが押された？ | FUnity/Scratch/調べる | 押下中は true。定義: Runtime/.../InputPredicateUnits.cs |
| マウスのx座標 | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe.MouseXUnit | マウスのx座標 | FUnity/Scratch/調べる | ステージ中心原点でのマウス x 座標。定義: Runtime/.../Probe/MouseXUnit.cs |
| マウスのy座標 | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe.MouseYUnit | マウスのy座標 | FUnity/Scratch/調べる | ステージ中心原点でのマウス y 座標。定義: Runtime/.../Probe/MouseYUnit.cs |
| マウスポインターに触れた？ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.TouchingMousePointerPredicateUnit | マウスポインターに触れた？ | FUnity/Scratch/調べる | 俳優矩形とマウス座標を判定。定義: Runtime/.../TouchPredicates.cs |
| マウスポインターまでの距離 | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe.DistanceToMousePointerUnit | マウスポインターまでの距離 | FUnity/Scratch/調べる | 自身中心とカーソル距離。定義: Runtime/.../Probe/DistanceToMousePointerUnit.cs |
| マウスが押された | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.Probe.MouseDownPredicateUnit | マウスが押された | FUnity/Scratch/調べる | 左ボタン押下状態を返す。定義: Runtime/.../Probe/MouseDownPredicateUnit.cs |
| 端に触れた？ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.TouchingEdgePredicateUnit | 端に触れた？ | FUnity/Scratch/調べる | ステージ境界との接触判定。定義: Runtime/.../TouchPredicates.cs |
| ○○に触れた？ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.TouchingActorByDisplayNamePredicateUnit | ○○に触れた？ | FUnity/Scratch/調べる | DisplayName 指定で矩形重なりを判定。定義: Runtime/.../TouchingActorByDisplayNamePredicateUnit.cs |

## 変数
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| （変数）を○にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetVariableUnit | （変数）を○にする | FUnity/Scratch/変数 | 変数サービス経由で絶対値を設定。定義: Runtime/.../ScratchVariables/SetVariableUnit.cs |
| （変数）を○ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ChangeVariableUnit | （変数）を○ずつ変える | FUnity/Scratch/変数 | 変数サービス経由で加算。定義: Runtime/.../ScratchVariables/ChangeVariableUnit.cs |
| 変数（変数）を表示する | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ShowVariableUnit | 変数（変数）を表示する | FUnity/Scratch/変数 | 変数モニターを表示。定義: Runtime/.../ScratchVariables/ShowVariableUnit.cs |
| 変数（変数）を隠す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.HideVariableUnit | 変数（変数）を隠す | FUnity/Scratch/変数 | 変数モニターを非表示。定義: Runtime/.../ScratchVariables/HideVariableUnit.cs |

## 拡張
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| コルーチンに切り替える | FUnity.Runtime.Integrations.VisualScripting.Units.Common.ToCoroutineUnit | コルーチンに切り替える | FUnity/Scratch/拡張 | 同期フローをコルーチンパイプラインへ橋渡し。定義: Runtime/Integrations/VisualScripting/Units/Common/FlowBridgeUnits.cs |

> **メモ:** `Runtime/...` は `Runtime/Integrations/VisualScripting/Units/` 以下の具体的なパスを示しています。`ScratchUnitUtil` や `ScratchHitTestUtil` の補助メソッドを利用するユニットは、移動・当たり判定の共通処理を共有しています。

## 今後のメンテナンス
- 新しいユニットを追加した場合は、本表のカテゴリに追記し、タイトル・カテゴリが規約通りであることを確認してください。
- 既存ユニットのタイトルやカテゴリを変更した場合は、該当行の `UnitTitle` と `UnitCategory` を同じ値へ更新してください。
- TypeIcon を変更した場合は、本ドキュメントにその理由を記載し、プロジェクト全体で統一できるか検討してください。
- Sprite 差し替え系ユニットを追加する場合は、`ActorPresenterAdapter.SetSpriteIndex` と `ActorPresenter.SetSpriteIndex` を呼び出し、`SpriteIndex` / `SpriteCount` のプロパティを活用して枚数管理を行ってください。
- 歩行など SpriteList を利用するアニメーションは `NextSpriteUnit` / `SetSpriteIndexUnit` で操作し、Presenter API を直接呼び出す際も同名メソッドを経由します。
- コスチューム関連の処理では `ActorState.CostumeIndex` と `ActorPresenter.ApplyCostumeFromState()` を基点にし、必ず `ActorView.SetSprite(Sprite)` を介して見た目を更新してください。
