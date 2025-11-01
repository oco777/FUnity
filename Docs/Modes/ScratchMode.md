# Scratch モードの座標クランプ仕様

Scratch モードではステージ中央を原点 (0,0) とする中心座標系を使用します。右方向が +X、上方向が +Y です。俳優のアンカーは画像中心となり、Presenter が UI 座標との相互変換を担当します。

## 中心座標のクランプ範囲

俳優の中心座標は、拡大率を適用した見た目サイズを考慮して下記の範囲へクランプされます。

- **X 座標**: `-240 - width_afterScale` ～ `+240 + width_afterScale`
- **Y 座標**: `-180 - height_afterScale` ～ `+180 + height_afterScale`

ここで `width_afterScale` / `height_afterScale` は、`ActorView.GetScaledSizePx()` が返すスケール適用後の実ピクセルサイズです。回転は中心回転であるため、クランプ判定には含めません。

## スケール変更時の挙動

- 拡大率を 50% / 100% / 200% と変更した場合、上記の `width_afterScale` / `height_afterScale` がリアルタイムに変化し、許容範囲も同じ比率で広がります。
- 俳優のサイズを変更する Unit や Presenter API からも同一の計算が走り、ステージ外へ突き抜けることを防ぎます。

## Visual Scripting の挙動

Visual Scripting の Scratch 互換ユニット（位置セット・座標加算・〇歩動かす）は、最終的に `ActorPresenter` を経由して座標を更新します。Presenter 側で `ScratchBounds.ClampCenter` が呼び出されるため、グラフ側で追加のクランプ処理を行う必要はありません。

## unityroom モードとの違い

unityroom モードでは左上原点 (TopLeft) の座標系を利用するため、本クランプ仕様は適用されません。既存のステージ境界計算（`m_PositionBoundsLogical` による矩形クランプ）が従来通り働きます。
