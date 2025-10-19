# トラブルシュート集

Default Project Data 実行後やサンプルシーン再生時に発生しやすい問題と対処法をまとめました。

## 目次
- [Input 系の名前空間衝突](#input-系の名前空間衝突)
- [テーマ/PanelSettings が null になる](#テーマpanelsettings-が-null-になる)
- [俳優 UI が表示されない](#俳優-ui-が表示されない)
- [関連ドキュメント](#関連ドキュメント)

## Input 系の名前空間衝突
- エラー例: `CS0234` や `The name 'Input' does not exist in the current context`。
- 原因: `FUnity.Runtime.Input` 名前空間に `Input` クラスが存在し、`UnityEngine.Input` と衝突する。
- 対処:
  - C# から呼び出す場合は `UnityEngine.Input.GetAxisRaw("Horizontal")` のように完全修飾します。
  - Visual Scripting で `Input.GetAxis` ユニットが見つからない場合は、カスタムユニットで `UnityEngine.Input` を参照するか、`using UInput = UnityEngine.Input;` エイリアスを追加したスクリプトからブリッジします。

## テーマ/PanelSettings が null になる
- 症状: コンソールに `PanelSettings theme is null` が表示され、UI が素のスタイルになる。
- 確認ポイント:
  1. `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss` が存在するか。
  2. `Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss` が生成されているか。
  3. `Assets/FUnity/UI/FUnityPanelSettings.asset` の `themeStyleSheets` に上記いずれかが登録されているか。
- 対処: メニュー **FUnity > Create > Default Project Data** を再実行すると、優先度 1 → 2 の順に Theme が自動割り当てされます。

## 俳優 UI が表示されない
- 症状: 背景は表示されるが、フーニーが見えない / `FooniUIBridge` が要素を見つけられない。
- 確認ポイント:
  - `Assets/FUnity/UI/UXML/FooniElement.uxml` のルート要素に `name="root"`、俳優の表示領域に `name="actor-root"` が設定されているか。
  - `FUnityActorData_Fooni.asset` の Portrait/UXML/USS 参照が実在するか。
  - `Assets/FUnity/VisualScripting/Macros/Fooni_FloatSetup.asset` が `FUnityActorData_Fooni` の ScriptGraph に割り当てられているか。
  - シーンに `ActorPresenterAdapter` (旧 FooniController) が存在し、`FUnityManager` の **Default Actor Presenter Adapter** フィールドまたは Visual Scripting グラフの変数から参照されているか。
  - Visual Scripting グラフを実行する Runner（または対象 GameObject）に `ScriptMachine` が付与され、グラフの参照先として設定されているか。
- 対処: Default Project Data を再生成すると参照が再設定されます。UXML の name 属性が欠けている場合は手動で修正してください。

## 関連ドキュメント
- [導入手順](setup.md)
- [UI テーマ適用戦略](ui-theme.md)
- [既定データの構成](data-defaults.md)
