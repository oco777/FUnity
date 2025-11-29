# Block List

カテゴリ別に代表的なブロックと補足をまとめます。Scratch 互換の文言を基準にしています。

| カテゴリ | ブロック名 | 機能 | 備考 |
| --- | --- | --- | --- |
| 動き | MoveSteps | Actor を n 歩動かす | 1 歩 = 1px。ステージ境界でクランプと反射を実施。 |
| 動き | TurnRight / TurnLeft | 指定角度だけ回転する | ブロックモードの角度系（上=0°/右=90°）を維持。 |
| 動き | GoTo | 位置を指定して移動 | マウス座標や他 Actor へのジャンプに対応。 |
| 見た目 | Say / SayTimed | 吹き出しを表示 | ActorPresenter 経由で吹き出し UI を更新。 |
| 見た目 | SwitchCostume | コスチュームを切り替える | `CostumeIndex` を ActorState に設定し Presenter が反映。 |
| 見た目 | SetBackgroundByIndex | 背景を番号で変更 | StageData の背景リストから切り替え。 |
| 見た目 | SetBackgroundByName | 背景を名前で変更 | 背景名に一致する項目へ切り替え。 |
| 見た目 | NextBackground | 次の背景にする | 背景リストを巡回しながら進める。 |
| 見た目 | GetBackgroundIndex | 背景の番号 | 現在の背景番号を取得。 |
| 見た目 | GetBackgroundName | 背景の名前 | 現在の背景名を取得。 |
| 音 | PlaySound | サウンドを再生 | 音量や再生位置はサービスを介して管理。 |
| 調べる | TouchingColor | 指定色に触れたか判定 | 色判定精度を改善。境界の色も検出。 |
| 制御 | StopAll | 全スクリプトを停止 | `flow.StopCoroutine(false)` を利用し ScriptThreadManager と連動。 |
| 制御 | WaitSeconds | 指定時間待つ | ScriptThreadManager 上で待機スレッドを管理。 |
| 変数 | SetVariable / ChangeVariable | 変数を設定・加算 | `FUnityVariableService` を介して管理。 |
| 変数 | ShowVariable / HideVariable | 変数表示を切り替え | UI 上の変数表示パネルを制御。 |

> 表にないユニットの詳細は [Docs/VS_Scratch_Mapping.md](VS_Scratch_Mapping.md) を参照してください。
