# Maze Sample

迷路を進んでゴールするとゲームクリア演出が再生されるサンプルです。Macro で StopAll → GameClearUI → Finish の流れを確認できます。

## 動作フロー
1. ゴール判定に到達すると **StopAll** ブロックで全スクリプトを停止します。
2. `GameClearUI` を表示し、演出用の UI をフェードインします。
3. 完了演出後に **Finish** 処理を呼び、スコア表示やリトライ操作へ遷移します。

## 学べるポイント
- StopAll が `flow.StopCoroutine(false)` を用いて確実にスレッドを停止する実装。
- 背景変更ブロックを使ったステージ演出の切り替え。
- Actor のコスチュームと吹き出しを Presenter 経由で更新する一連の流れ。
