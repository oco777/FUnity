# Blocks Mapping

Scratch 標準ブロックと FUnity Visual Scripting ユニットの対応表です。`Status` 列は実装状況を示します。

| Scratch Category | Scratch Block | FUnity Graph/Node | Status | Note |
| --- | --- | --- | --- | --- |
| Motion | move _ steps | `Scratch/Motion/MoveSteps` | ✅ | PixelsPerUnit と連動し 1 歩 = 10px を既定化 |
| Motion | turn _ degrees | `Scratch/Motion/Turn` | ✅ | 左回りは負数で指定 |
| Motion | go to x: _ y: _ | `Scratch/Motion/GoToXY` | ✅ | 座標は中心原点基準 |
| Looks | switch costume to _ | `Scratch/Looks/SwitchCostume` | ✅ | コスチュームは `Sprite` にマップ |
| Looks | set size to _ % | `Scratch/Looks/SetSize` | ✅ | 1%〜300% へ自動クランプ |
| Sound | play sound _ until done | `Scratch/Sound/PlaySoundAwait` | ✅ | Unity `AudioSource` を逐次再生 |
| Sound | change volume by _ | `Scratch/Sound/ChangeVolume` | ✅ | `AudioMixer` のボリュームに反映 |
| Events | when green flag clicked | `Scratch/Events/OnGreenFlag` | ✅ | `FUnity.Core.FUnityManager` の再生イベントを受信 |
| Events | when I receive _ | `Scratch/Events/OnBroadcast` | ✅ | Visual Scripting イベントバスで同期 |
| Control | forever | `Scratch/Control/Forever` | ✅ | 1 フレーム待機で CPU 占有を防止 |
| Control | repeat _ | `Scratch/Control/Repeat` | ✅ | ループ回数は整数へ丸め込み |
| Control | create clone of _ | `Scratch/Control/CreateClone` | ⏳ | ランタイム複製を最適化中 |
| Sensing | touching _ ? | `Scratch/Sensing/IsTouching` | ✅ | Collider2D または Rect オーバーラップで判定 |
| Sensing | mouse x | `Scratch/Sensing/MouseX` | ✅ | UI Toolkit 座標を Scratch 座標に変換 |
| Operators | pick random _ to _ | `Scratch/Operators/PickRandom` | ✅ | `Random.Range` を用い端点含めて計算 |
| Operators | join _ _ | `Scratch/Operators/JoinText` | ✅ | `StringBuilder` 経由で連結 |
| Variables | set _ to _ | `Scratch/Data/SetVariable` | ✅ | 可変長辞書に保存 |
| Variables | change _ by _ | `Scratch/Data/ChangeVariable` | ✅ | 型推論で数値を加算 |
| Lists | add _ to _ | `Scratch/Data/ListAdd` | ✅ | リスト末尾へ追加 |
| Lists | delete _ of _ | `Scratch/Data/ListDelete` | ✅ | 1 始まりのインデックスで削除 |

`⏳` は開発中の項目です。詳細な API 仕様やサンプルは随時更新します。
