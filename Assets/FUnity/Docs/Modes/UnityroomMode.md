# unityroom モード

unityroom モードは WebGL での公開を前提に、16:9 のステージ解像度と Unity 2D 機能を活用できるプリセットです。ブロックモードとの互換性よりも演出やパフォーマンスチューニングを優先します。

## ステージ仕様
- **推奨ピクセルサイズ**: 960 x 540（必要に応じて 1280 x 720 などへスケール可能）
- **Pixels Per Unit**: 100（UI と物理の整合を取りつつ拡縮対応）
- **カメラ推奨**: Orthographic / Size = 270 で 16:9 のレターボックスを許容
- **UI Toolkit**: `Fit (Contain)` を基本とし、フルスクリーン時は背景色でレターボックスを埋めます

### 表示スケール（変更なし）
- ブロックモードのような 480 x 360 固定スケール処理は行わず、PanelSettings や CSS に設定した既定レイアウトをそのまま使用します。
- ウィンドウリサイズ時の拡縮は Unity 標準の Canvas / PanelSettings 設定に委ねてください。

## 座標系（現状維持）
- 原点は **左上(0,0)**。右が +X、下が +Y のままです。
- ブロックモードの中央原点変換は適用されません。UI Toolkit 座標と論理座標は一致します。
- `FUnityActorData.Anchor` は **Center (既定)** / **TopLeft** を選択可能です。Center を選ぶと座標が画像中心に対応し、TopLeft は従来通り左上を基準とします。

## 機能差分
- Scratch 標準ブロックの一部は非対応です（録音・ビデオ入力など WebGL で扱いづらい要素）。
- 代わりに Unity 固有ブロック（Physics2D、パーティクル、ポストエフェクト、カメラシェイク等）を利用できます。
- 入力はキーボード / マウス / タッチに加えて、フォーカス喪失時のポーズ処理やポインタロックをサポートすることを推奨します。

## WebGL ビルド推奨設定
1. `File > Build Settings` で WebGL を選択し、`Compression Format` を **Brotli** に設定します。
2. `Publishing Settings` で `Data Caching` を有効化し、`Decompression Fallback` をオフにして読み込み時間を短縮します。
3. `Player Settings > Resolution and Presentation` で `Default Canvas Width/Height` を 960x540 に設定します。
4. テンプレート `index.html` に unityroom の埋め込みスクリプトを追加し、`<meta name="viewport">` を `width=device-width` に設定します。
5. `Memory Size` はプロジェクト規模に応じて 256MB 以上を確保し、起動ログで `TOTAL_MEMORY` を確認します。

## 配信時の注意
- unityroom の規約に従い、読み込み時間 10 秒以内、メモリ 256MB 以内を目指します。
- 入力フォーカスの喪失時にゲームが停止するよう `Application.focusChanged` を利用することを推奨します。
- `StreamingAssets` を使用する場合は WebGL ビルドでの配置制限に注意してください。

ブロックモードとの併用を想定し、共通ドキュメントやブロックマッピングを適宜参照してください。
