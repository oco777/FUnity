# 導入手順（Setup）

FUnity を既存プロジェクトへ追加し、サンプルの背景とフーニー表示までを一気に体験するための手順です。

## 目次
- [前提環境](#前提環境)
- [パッケージ導入](#パッケージ導入)
- [サンプルシーンの確認](#サンプルシーンの確認)
- [Default Project Data の生成](#default-project-data-の生成)
- [補足情報](#補足情報)

## 前提環境
- Unity 6 (6000.x) 以降と .NET Standard 2.1 互換ランタイム。
- UI Toolkit / UI Builder パッケージ。
- Unity Visual Scripting 1.9.7 以降（FUnity の依存として自動導入されるため、`#if UNITY_VISUAL_SCRIPTING` などの条件分岐は不要）。

## パッケージ導入
1. Package Manager を開き、左上の **+** ボタンから **Add package from git URL...** を選択します。
2. 通常は `https://github.com/oco777/FUnity.git` を入力します。特定バージョンを固定したい場合は `https://github.com/oco777/FUnity.git#v0.1.0` のようにタグを指定します。
3. 依存パッケージとして `com.unity.visualscripting` が自動追加されることを確認します。
4. 既存プロジェクトでソースを保持したい場合は Git サブモジュールとして `Packages/com.papacoder.funity` に追加しても構いません。

## サンプルシーンの確認
1. Package Manager で **FUnity** パッケージを選び、**Samples → BasicScene → Import** を実行します。
2. `Samples~/BasicScene/FUnitySample.unity` を開き、シーンに `FUnityManager` が存在することを確認します（追加の GameObject は不要です）。
3. 再生すると `FUnityManager` が “FUnity UI” GameObject と `UIDocument`、`FooniUIBridge` を自動構成し、背景とフーニーが表示されます。`ScriptMachine` はグラフを実行する Runner やアクター用 GameObject に配置してください。
4. サンプルでは `FUnity Actor Adapter` GameObject に `ActorPresenterAdapter` (旧 FooniController) を明示的に配置し、`FUnityManager` の **Default Actor Presenter Adapter** フィールドへ割り当てています。自身のシーンでも任意の GameObject に同コンポーネントを追加し、参照を設定してください。

## Default Project Data の生成
1. メニュー **FUnity > Create > Default Project Data** を実行します。
2. 以下が自動生成されます。
   - `Resources/FUnityProjectData.asset` / `Resources/FUnityStageData.asset`
   - `Assets/FUnity/UI/FUnityPanelSettings.asset`（既存 Theme を優先して割り当て）
   - `Assets/FUnity/Data/Actors/FUnityActorData_Fooni.asset`
   - `Assets/FUnity/VisualScripting/Macros/Fooni_FloatSetup.asset`（見つからない場合は新規作成）
3. シーンを再生して背景とフーニー表示を確認します。`FUnityActorData_Fooni` の ScriptGraph には自動的に `Fooni_FloatSetup.asset` が設定されます。

## 補足情報
- Default Project Data 実行時に `Assets/Resources/FUnityActorData_Fooni.asset` が存在すると、重複防止のために削除されます。
- Theme の優先度は `Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.uss` → `Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss` の順です。
- 入力 API を Visual Scripting で呼び出す際は `UnityEngine.Input.GetAxisRaw` のように完全修飾名を使うと、`FUnity.Runtime.Input` と衝突しません。
- メニュー **FUnity/VS/Create Fooni Macros & Runner** を実行すると、生成された Runner に `ScriptMachine` を割り当て、関連する ScriptGraphAsset の Variables["adapter"] と Runner の Object Variables に `ActorPresenterAdapter`（旧称 FooniController）を自動で書き込みます。
- オリジナルの Runner を作成する場合は `ActorPresenterAdapter` や `ScriptMachine` を手動で追加し、同様に ScriptGraphAsset / Object Variables に参照を設定してください。
