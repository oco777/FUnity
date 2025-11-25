# 🧩 FUnity Visual Scripting 対応表

Scratch ブロックと FUnity 独自 Visual Scripting Unit の対応関係です。すべての Unit は日本語タイトル・カテゴリ・共通アイコン規約に従っており、新規追加や変更時には本表を必ず更新してください。

## 運用ルール
- `[UnitTitle]` は Scratch 日本語ブロック名に合わせるか、Scratch 流儀の短い日本語で命名する。
- `[UnitCategory]` は Scratch 系ユニットの場合、イベント系は `Events/FUnity/Scratch/◯◯`、その他は `FUnity/Scratch/◯◯` 形式（カテゴリ名は日本語）で統一する。拡張ユニットは `FUnity/Scratch/拡張` を使用する。
- `[TypeIcon(typeof(FUnityScratchUnitIcon))]` を全ユニットへ付与し、FUnity Scratch 系ユニットであることを明示する。
- 質問入力系ユニットは Blocks カテゴリ（`Events/FUnity/Blocks/調べる`）を使用する。
- Visual Scripting 上のサブタイトルはカテゴリ名のみを表示するため、`[UnitSubtitle]` には必ずカテゴリ名（例：`動き` `見た目` `制御`）だけを設定し、検索用のキーワードは含めない。
- コード変更と同じ PR でこの対応表を更新し、タイトルやカテゴリの差異が無いよう同期する。
- Scratch 系のコルーチン Unit は Visual Scripting 標準の `flow.StartCoroutine` を用い、開始後に `ScratchUnitUtil.RegisterScratchFlow` で Flow を登録して停止ブロックと連動させる。
- Scratch のイベント Unit（緑の旗/キー押下/メッセージ受信/クローン開始など）は EventBus 登録時に Flow を新規作成し、`flow.StartCoroutine(trigger)` で起動してから `ScratchUnitUtil.RegisterScratchFlow(flow)` で ActorId/ThreadId を Flow.variables に保存する。Unity の Coroutine へは依存しない。
- `ScratchUnitUtil.RegisterScratchFlow` は Flow.variables に `FUNITY_SCRATCH_ACTOR_ID` / `FUNITY_SCRATCH_THREAD_ID` を格納し、`FUnityScriptThreadManager` の `(actorId, threadId)` テーブルへ登録する。停止系ユニットは Flow から逆引きした ActorId/ThreadId を使って停止対象を精密に選択する。
- Scratch のイベントリスナー状態は GraphReference 単位で static Dictionary に保持し、EventBus.Register/Unregister ではジェネリック型引数を明示する。`GraphStack.SetElementData` や EventUnit.Data 拡張に依存しない。
- コルーチン専用ポート（例: Forever の `enter`）を叩く際は、Flow 上で `flow.StartCoroutine(trigger)` を使って実行し、コルーチンとしてのみ許可されているポートを正しく起動する。
- Presenter 取得は `ScratchUnitUtil.TryGetActorPresenter` を経由し、内部で `ResolveAdapter` と `ResolveActorPresenter` に委譲する共通ロジックを利用する。

## 動き
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| ○歩動かす | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.MoveStepsUnit | ○歩動かす | FUnity/Scratch/動き | 境界反射と残距離再移動に対応。Presenter は `ScratchUnitUtil.TryGetActorPresenter` 経由で取得。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/MoveStepsUnit.cs |
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
| x座標を〇にする | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.SetXToUnit | x座標を〇にする | FUnity/Blocks/動き | X 座標を絶対設定。定義: Runtime/Integrations/VisualScripting/Units/Blocks/MotionSetGetUnits.cs |
| y座標を〇にする | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.SetYToUnit | y座標を〇にする | FUnity/Blocks/動き | Y 座標を絶対設定。定義: Runtime/Integrations/VisualScripting/Units/Blocks/MotionSetGetUnits.cs |
| x座標 | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.GetXPositionUnit | x座標 | FUnity/Blocks/動き | 現在の X 座標を取得。定義: Runtime/Integrations/VisualScripting/Units/Blocks/MotionSetGetUnits.cs |
| y座標 | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.GetYPositionUnit | y座標 | FUnity/Blocks/動き | 現在の Y 座標を取得。定義: Runtime/Integrations/VisualScripting/Units/Blocks/MotionSetGetUnits.cs |
| 向き | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.GetDirectionUnit | 向き | FUnity/Blocks/動き | Scratch 互換の角度を返す。定義: Runtime/Integrations/VisualScripting/Units/Blocks/MotionSetGetUnits.cs |
| ○度回す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.TurnDegreesUnit | ○度回す | FUnity/Scratch/動き | 向きを相対回転。定義: Runtime/.../TurnAndPointUnits.cs |
| ○度に向ける | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.PointDirectionUnit | ○度に向ける | FUnity/Scratch/動き | 向きを絶対設定。定義: Runtime/.../TurnAndPointUnits.cs |
| マウスポインターへ向ける | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.PointTowardsMousePointerUnit | マウスポインターへ向ける | FUnity/Scratch/動き | カーソル方向へ即時旋回。定義: Runtime/.../TurnAndPointUnits.cs |

> **角度の扱い:** Runtime/Integrations/VisualScripting/Units/ScratchUnits/ScratchUnitUtil.cs の `GetDirectionDegreesForCurrentMode` を経由し、ブロックモードでは上=0°/右=90°/左=-90°/下=±180°、通常モードでは従来通り右=0° の角度ルールを適用しています。最終的に Presenter や ActorState へ渡す際は Runtime/Core/ScratchAngleUtil.cs を使い、必ず内部角度（0°=右）へ変換してから `ActorPresenterAdapter.SetDirection` に渡します。

## 見た目
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| コスチュームを ( ) にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetCostumeNumberUnit | コスチュームを〇にする | FUnity/Scratch/見た目 | ActorState.CostumeIndex と FUnityActorData.Sprites を利用。定義: Runtime/.../CostumeUnits.cs |
| 次のコスチュームにする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.NextCostumeUnit | 次のコスチュームにする | FUnity/Scratch/見た目 | コスチュームを 1 → 2 → … → N → 1 と循環。定義: Runtime/.../CostumeUnits.cs |
| コスチュームの番号 | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.CostumeNumberUnit | コスチュームの番号 | FUnity/Scratch/見た目 | 1 始まりの Scratch コスチューム番号を返す。定義: Runtime/.../CostumeUnits.cs |
| 大きさを○%にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetSizePercentUnit | 大きさを○%にする | FUnity/Scratch/見た目 | 拡大率を絶対設定。定義: Runtime/.../SizeUnits.cs |
| 大きさを○%ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ChangeSizeByPercentUnit | 大きさを○%ずつ変える | FUnity/Scratch/見た目 | 拡大率を相対変更。定義: Runtime/.../SizeUnits.cs |
| ○と○秒言う | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SayForSecondsUnit | ○と○秒言う | FUnity/Scratch/見た目 | 指定秒数で吹き出し表示。Scratch スレッド登録で停止ブロックと連動。定義: Runtime/.../SpeechUnits.cs |
| ○と言う | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SayUnit | ○と言う | FUnity/Scratch/見た目 | 無期限の発言吹き出し。定義: Runtime/.../SpeechUnits.cs |
| ○と○秒考える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ThinkForSecondsUnit | ○と○秒考える | FUnity/Scratch/見た目 | 指定秒数で思考吹き出し。Scratch スレッド登録で停止ブロックと連動。定義: Runtime/.../SpeechUnits.cs |
| ○と考える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ThinkUnit | ○と考える | FUnity/Scratch/見た目 | 無期限の思考吹き出し。定義: Runtime/.../SpeechUnits.cs |
| 色の効果を○ずつ変える | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ChangeColorEffectByUnit | 色の効果を○ずつ変える | FUnity/Scratch/見た目 | 色効果を相対変更。定義: Runtime/.../EffectUnits.cs |
| 色の効果を○にする | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SetColorEffectToUnit | 色の効果を○にする | FUnity/Scratch/見た目 | 色効果を絶対設定。定義: Runtime/.../EffectUnits.cs |
| 画像効果をなくす | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ClearGraphicEffectsUnit | 画像効果をなくす | FUnity/Scratch/見た目 | Tint をリセット。定義: Runtime/.../EffectUnits.cs |
| 表示する | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ShowActorUnit | 表示する | FUnity/Scratch/見た目 | 俳優を表示状態へ。定義: Runtime/.../VisibilityUnits.cs |
| 隠す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.HideActorUnit | 隠す | FUnity/Scratch/見た目 | 俳優を非表示へ。定義: Runtime/.../VisibilityUnits.cs |

## 音
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| ○の音を鳴らす | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.PlaySoundUnit | ○の音を鳴らす | FUnity/Blocks/音 | サウンドサービスを介して指定 ID の音を即時再生。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SoundUnits.cs |
| 終わるまで○の音を鳴らす | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.PlaySoundUntilDoneUnit | 終わるまで○の音を鳴らす | FUnity/Blocks/音 | サウンド再生完了までコルーチンで待機。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SoundUnits.cs |
| すべての音を止める | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StopAllSoundsUnit | すべての音を止める | FUnity/Blocks/音 | サウンドサービス経由で全サウンド停止。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/SoundUnits.cs |

## 制御
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| ○回繰り返す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.RepeatNUnit | ○回繰り返す | FUnity/Blocks/制御 | 指定回数ループ。定義: Runtime/.../LoopUnits.cs |
| ずっと | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ForeverUnit | ずっと | FUnity/Blocks/制御 | 永続ループ。Visual Scripting 標準の `ControlInputCoroutine` で Flow コルーチンを回し、`body` を毎フレーム実行してから 1 フレーム待機する。定義: Runtime/.../LoopUnits.cs |
| ○まで繰り返す | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.RepeatUntilUnit | ○まで繰り返す | FUnity/Blocks/制御 | 条件が真になるまで body を実行し、毎反復で 1 フレーム待機する。ループ継続判定はコルーチン内で行い、ポート定義は enter→body / enter→exit のみ。定義: Runtime/.../LoopUnits.cs |
| ○秒待つ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WaitSecondsUnit | ○秒待つ | FUnity/Blocks/制御 | 指定時間待機。Visual Scripting 標準のコルーチン経由で待機し、完了後に後続フローへ進む。定義: Runtime/.../WaitSecondsUnit.cs |
| ○まで待つ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WaitUntilUnit | ○まで待つ | FUnity/Blocks/制御 | 条件成立まで 1 フレームずつ待機し、成立後に exit へ進む。定義: Runtime/.../WaitSecondsUnit.cs |
| 自分のクローンを作る | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.CreateCloneOfSelfUnit | 自分のクローンを作る | FUnity/Blocks/制御 | 自身を複製。定義: Runtime/.../CloneUnits.cs |
| ○のクローンを作る | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.CreateCloneOfDisplayNameUnit | ○のクローンを作る | FUnity/Blocks/制御 | 指定俳優を複製。CloneAdapter 出力は廃止済み。定義: Runtime/.../CloneUnits.cs |
| クローンされたとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WhenIStartAsCloneUnit | クローンされたとき | Events/FUnity/Blocks/制御 | クローン生成時に発火する Scratch スクリプトの入口。`flow.StartCoroutine(trigger)` で起動後、`ScratchUnitUtil.RegisterScratchFlow` により Flow をスレッド登録して停止ブロックと連動させる。Flow の破棄は Visual Scripting のコルーチン側で行う。定義: Runtime/.../CloneUnits.cs |
| このクローンを削除する | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.DeleteThisCloneUnit | このクローンを削除する | FUnity/Blocks/制御 | クローンを破棄。定義: Runtime/.../CloneUnits.cs |
| すべてを止める | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StopAllUnit | すべてを止める | FUnity/Blocks/制御 | Scratch 用スレッドテーブル経由で全スレッド停止。定義: Runtime/.../StopControlUnits.cs |
| このスクリプトを止める | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StopThisScriptUnit | このスクリプトを止める | FUnity/Blocks/制御 | Flow.variables から ActorId/ThreadId を取得し、`FUnityScriptThreadManager.StopScratchThread(actorId, threadId)` で自身のみ停止。定義: Runtime/.../StopControlUnits.cs |
| スプライトの他のスクリプトを止める | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StopOtherScriptsInSpriteUnit | スプライトの他のスクリプトを止める | FUnity/Blocks/制御 | 同俳優の他 Scratch スレッド停止。定義: Runtime/.../StopControlUnits.cs |
| もし○なら | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.IfThenUnit | もし○なら | FUnity/Blocks/制御 | 条件成立時のみ本体を実行。定義: Runtime/.../ConditionUnits.cs |
| もし○なら でなければ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.IfElseUnit | もし ○ なら でなければ | FUnity/Blocks/制御 | 条件の真偽で ifTrue/ifFalse へ分岐させる。定義: Runtime/.../ConditionUnits.cs |

### 停止ブロック

| Scratch ブロック名 | FUnity Unit 名 | 備考 |
| --- | --- | --- |
| すべてを止める | Events/FUnity/Scratch/制御/すべてを止める | `FUnityScriptThreadManager.StopAllScratchThreads()` を呼び出し、登録済みの Scratch スレッドを全停止する。 |
| このスクリプトを止める | Events/FUnity/Scratch/制御/このスクリプトを止める | `ScratchUnitUtil` から ActorId/ThreadId を取得し、`FUnityScriptThreadManager.StopScratchThread(actorId, threadId)` で現在の Scratch スレッドのみ停止する。 |
| スプライトの他のスクリプトを止める | Events/FUnity/Scratch/制御/スプライトの他のスクリプトを止める | `ScratchUnitUtil` で ActorId/ThreadId を取得し、`FUnityScriptThreadManager.StopOtherScratchThreadsOfActor(...)` を使って同一 Actor の他スレッドを停止する。 |

> **補足:** Scratch 系の停止ユニットは、`FUnityScriptThreadManager` に用意した `(actorId, threadId)` ベースのスレッドテーブルを利用して Flow を管理します。`ScratchUnitUtil` に保存した ActorId/ThreadId を参照し、`StopAllScratchThreads`・`StopScratchThread`・`StopOtherScratchThreadsOfActor` を呼び出して対象を制御します。`FUnityScriptThreadManager.FindOrCreate()` はシーン上に存在しない場合でもアクセス時に自動生成され、`DontDestroyOnLoad` で保持されるため、停止ブロックからの呼び出しが常に有効になります。

## イベント
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| 緑の旗が押されたとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WhenGreenFlagClickedUnit | 緑の旗が押されたとき | Events/FUnity/Scratch/イベント | Runner 対象の緑の旗イベント。EventBus 登録時に Flow を生成し、`flow.StartCoroutine(trigger)` で開始してから `ScratchUnitUtil.RegisterScratchFlow` でスレッド登録する。Flow の解放は Visual Scripting のコルーチン側に委任。定義: Runtime/.../GreenFlagUnits.cs |
| ○キーが押されたとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.OnKeyPressedUnit | ○キーが押されたとき | Events/FUnity/Scratch/イベント | FUnityManager.Update → `ScratchInputEventDispatcher.Tick()` 経由で毎フレーム呼び出される静的ディスパッチに登録し、押下エッジ検出時だけ Flow を生成して `flow.StartCoroutine(trigger)` を開始する。直後に `ScratchUnitUtil.RegisterScratchFlow` で登録し、停止ブロックと同期させる。EventBus には依存しない。定義: Runtime/.../InputEventUnits.cs |
| メッセージを送る | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.BroadcastMessageUnit | メッセージを送る | FUnity/Scratch/イベント | 即時配信。定義: Runtime/.../MessagingUnits.cs |
| メッセージを送って待つ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.BroadcastAndWaitUnit | メッセージを送って待つ | FUnity/Scratch/イベント | 同期配信後に継続。定義: Runtime/.../MessagingUnits.cs |
| メッセージを受け取ったとき | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.WhenIReceiveMessageUnit | メッセージを受け取ったとき | Events/FUnity/Scratch/イベント | フィルタ一致時に Flow を生成し、`flow.StartCoroutine(trigger)` で実行した直後に `ScratchUnitUtil.RegisterScratchFlow` で登録する。Flow の寿命は Visual Scripting のコルーチン管理に任せる。停止ブロックとの連動を担保。定義: Runtime/.../MessagingUnits.cs |

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
| ○と聞いて待つ | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.AskAndWaitUnit | ○と聞いて待つ | FUnity/Blocks/調べる | 中央に質問フォームを表示し、回答完了後に AnswerStore.LastAnswer へ保存してから後続フローを再開する。定義: Runtime/Integrations/VisualScripting/Units/Blocks/QuestionUnits.cs |
| 答え | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.AnswerUnit | 答え | FUnity/Blocks/調べる | AnswerStore.LastAnswer に保持された直近の回答文字列を返す。定義: Runtime/Integrations/VisualScripting/Units/Blocks/QuestionUnits.cs |

## 演算
| Scratch ブロック (日本語) | FUnity Unit クラス | UnitTitle | UnitCategory | 備考 |
| --- | --- | --- | --- | --- |
| ○から○までの乱数 | FUnity.Runtime.Integrations.VisualScripting.Units.Blocks.RandomFromToUnit | ○から○までの乱数 | FUnity/Blocks/演算 | 両端を含む整数乱数と小数乱数の両方に対応。min > max 時は内部で入れ替え、Mathf.Approximately で端点同一を検出する。定義: Runtime/Integrations/VisualScripting/Units/Blocks/RandomFromToUnit.cs |
| ○ + ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.AddNumbersUnit | ○ + ○ | FUnity/Blocks/演算 | 2 つの値を加算する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ArithmeticOperatorUnits.cs |
| ○ - ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.SubtractNumbersUnit | ○ - ○ | FUnity/Blocks/演算 | 2 つの値を減算する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ArithmeticOperatorUnits.cs |
| ○ * ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.MultiplyNumbersUnit | ○ * ○ | FUnity/Blocks/演算 | 2 つの値を乗算する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ArithmeticOperatorUnits.cs |
| ○ / ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.DivideNumbersUnit | ○ / ○ | FUnity/Blocks/演算 | 2 つの値を除算する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ArithmeticOperatorUnits.cs |
| ○ を ○ で割った余り | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.ModuloNumbersUnit | ○ を ○ で割った余り | FUnity/Blocks/演算 | 剰余演算を返す。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ArithmeticOperatorUnits.cs |
| ○ を四捨五入 | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.RoundNumberUnit | ○ を四捨五入 | FUnity/Blocks/演算 | Mathf.Round で丸める。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/ArithmeticOperatorUnits.cs |
| A と B | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.JoinStringsUnit | A と B | FUnity/Blocks/演算 | 2 つの文字列を連結する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/StringOperatorUnits.cs |
| S の N 番目の文字 | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.LetterOfStringUnit | S の N 番目の文字 | FUnity/Blocks/演算 | 1 始まりで丸めた位置の 1 文字を返す。範囲外は空文字列。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/StringOperatorUnits.cs |
| S の長さ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.LengthOfStringUnit | S の長さ | FUnity/Blocks/演算 | 文字列長を返す。空文字列なら 0。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/StringOperatorUnits.cs |
| ○ > ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.GreaterThanNumbersUnit | ○ > ○ | FUnity/Blocks/演算 | 2 つの数値を比較し、左辺が大きいかを判定する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LogicOperatorUnits.cs |
| ○ < ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.LessThanNumbersUnit | ○ < ○ | FUnity/Blocks/演算 | 2 つの数値を比較し、左辺が小さいかを判定する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LogicOperatorUnits.cs |
| ○ = ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.EqualNumbersUnit | ○ = ○ | FUnity/Blocks/演算 | 2 つの数値が等しいかを判定する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LogicOperatorUnits.cs |
| ○ かつ ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.AndBooleanUnit | ○ かつ ○ | FUnity/Blocks/演算 | 2 つの bool 値の論理積を計算する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LogicOperatorUnits.cs |
| ○ または ○ | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.OrBooleanUnit | ○ または ○ | FUnity/Blocks/演算 | 2 つの bool 値の論理和を計算する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LogicOperatorUnits.cs |
| ○ ではない | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.NotBooleanUnit | ○ ではない | FUnity/Blocks/演算 | 1 つの bool 値を反転する。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/LogicOperatorUnits.cs |
| S に SUB が含まれる | FUnity.Runtime.Integrations.VisualScripting.Units.ScratchUnits.StringContainsUnit | S に SUB が含まれる | FUnity/Blocks/演算 | 部分文字列の包含を判定する。空部分文字列は常に true。定義: Runtime/Integrations/VisualScripting/Units/ScratchUnits/StringOperatorUnits.cs |

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
