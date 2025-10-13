# コーディング規約

## 目次
- [要点](#要点)
- [手順](#手順)
- [補足](#補足)
- [参考](#参考)

## 要点
- 命名は PascalCase（型）と camelCase（変数）、SCREAMING_SNAKE_CASE（定数）を使用する。
- UNITY_EDITOR ガードで Editor 固有コードとビルド用コードを分離する。
- SerializedObject の差異はメソッド化し、環境差を吸収する。

## 手順
### 1. 命名とファイル配置
- `namespace FUnity` を保ち、機能単位でサブ名前空間を切る。
- UXML は `Assets/FUnity/UXML/`、USS は `Assets/FUnity/UI/USS/` に配置する。
- ScriptableObject は `Assets/FUnity/Data/` にまとめ、`FUnity` プレフィックスを付ける。

### 2. UNITY_EDITOR ガード
```csharp
#if UNITY_EDITOR
  // Editor 専用の初期化やメニューコマンドを記述する。
#endif
```
- ガード内部では Editor API への参照のみを使用する。
- ランタイム側は `Resources` や `Addressable` に依存せず、生成済みアセットを参照する。

### 3. SerializedObject 差異の吸収
- Unity のバージョン差異がある場合は `ApplyModifiedPropertiesWithoutUndo` などの条件分岐を用意する。
- プロパティ名は `SerializedPropertyHelper` などに定数化し、スペルミスを防ぐ。
- UXML/USS の参照パスは `FUnityPaths` などの静的クラスで集中管理する。

## 補足
- アセット生成ロジックは Editor フォルダに配置し、実行時コードと分離する。
- UI Toolkit のリソースは `Assets/FUnity/UI/` に集約し、Legacy パスを廃止する。
- PR では `.meta` 差分も確認し、不要なファイルは追加しない。

## 参考
- [環境構築ガイド](setup.md)
- [UI テーマ適用戦略](ui-theme.md)
- [トラブルシュート集](troubleshooting.md)
