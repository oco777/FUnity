# SB3 Importer (Experimental)

`.sb3` ファイル（Scratch 3.0 プロジェクト）を FUnity に取り込むための実験的パイプラインです。ブロックモードを前提としており、Visual Scripting グラフとアセットを自動生成します。

## 処理フロー
1. `.sb3` (ZIP) を展開し、`project.json` と `assets/` を読み込みます。
2. `project.json` の `targets` を `FUnityProjectData` / `FUnityActorData` にマッピングします。
3. スプライトの `costumes` は `Texture2D` として取り込み、`Sprite.Create` でコスチュームを生成します。
4. サウンドの `md5ext` をキーに `AudioClip` を生成し、再生設定を `Sound` カテゴリのブロックに接続します。
5. ブロック列は Visual Scripting ノードに変換し、`Scripts/Generated/` 配下にグラフを生成します。

## 未対応要素
- ペン拡張 (`penDown`, `stamp` など) は未対応。警告ログに記録され、処理は継続します。
- ビデオモーション、音声入力などハードウェア依存ブロックはスキップされます。
- Cloud 変数は Unity ランタイムへの保存先が未確定のため除外しています。

## 設定項目
- `FUnityModeConfig.EnableScratchImport` が `true` の場合のみパイプラインが有効になります。
- インポート時に作成されるアセットは `Assets/FUnity/Generated/Scratch/` に保存されます。削除するとプロジェクトから参照が切れます。
- 変換ロジックは `FUnity.Editor.ScratchImport` 名前空間に配置される予定です。

## 既知の課題
- ブロックの並列実行（`when` イベント複数）を完全には再現できていません。逐次実行で代替しています。
- 一部のコスチュームフィルタ（色/ゴースト等）は Visual Scripting のカスタムユニットで近似します。
- JSON 内のローカル変数スコープが複数層ネストした場合、現在はトップレベルに昇格させています。

フィードバックを歓迎します。改善要望や互換性報告は Issue に記録してください。
