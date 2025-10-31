# FUnityActorData メモ

`FUnityActorData` はアクターの静的設定を ScriptableObject で保持します。本ファイルではアンカー項目と座標変換の注意点をまとめます。

## Anchor（表示基準）
- **Center（既定）**: アクターの座標 (X, Y) は画像の中心を指します。Scratch と同様に、`GoToXY(0,0)` で中心がステージ中央に一致します。
- **TopLeft**: アクターの座標 (X, Y) は画像左上を指します。UI Toolkit の `style.left/top` と同じ基準です。
- **変換順序**: `論理座標 → UI座標（モードの原点変換） → アンカー補正 → style.left/top`。逆変換ではアンカー補正を先に戻してから論理座標へ写像します。
- **実装注意**: アンカー補正ではステージ幅・高さではなく、俳優要素の実サイズ（`contentRect` や `resolvedStyle`）を利用します。原点変換による中心補正と重ねて適用しないでください。
- **デフォルト**: Scratch モード / unityroom モードともに Center が既定です。旧データは Anchor 未設定でも自動的に Center が適用されます。

## 既存プロパティとの関係
- `Size` を指定すると、アンカー補正は指定サイズに基づいて計算されます。拡大率（`SizePercent`）変更後も最新の解決済みサイズを使用します。
- `InitialPosition` はアンカー適用後の UI 座標（left/top）として解釈されます。Center 指定時は画像中心が座標に一致します。
- `ScriptGraphAsset` 等その他のプロパティは従来通りで、アンカー設定による副作用はありません。
