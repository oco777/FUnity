# FUnity Runtime MVP Overview

FUnity のランタイム層は Model–View–Presenter (MVP) に基づいて構成されています。ここでは主要クラスの責務と初期化フローを整理します。

## 目次
- [Model 層](#model-層)
- [View 層](#view-層)
- [Presenter 層](#presenter-層)
- [Composition Root](#composition-root)
- [Visual Scripting からの呼び出し](#visual-scripting-からの呼び出し)

## Model 層
- **ScriptableObject 設定**: `FUnityProjectData`, `FUnityStageData`, `FUnityActorData` が静的設定を提供します。**FUnity/Create/FUnityProjectData** 実行時に背景画像や Macro が割り当てられます。
- **ランタイム状態**: `Runtime/Model/ActorState.cs` がアクターの座標や移動速度、UI 回転角 (`RotationDeg`)、拡大率 (`SizePercent`: 100=等倍, 1～300% にクランプ) など実行中に変化する値を保持します。Presenter が更新し、View からは直接変更しません。

## View 層
- **IActorView** (`Runtime/View/Interfaces/IActorView.cs`): Presenter が UI を更新するための抽象インターフェース。
- **ActorView** (`Runtime/View/ActorView.cs`): `FooniUIBridge` を介して UI Toolkit 要素の位置や画像差し替え、`SetRotationDegrees` によるポートレート回転、`SetScale` による `style.scale` 適用 (左上原点固定の #root スケール) を行います。表示処理のみに責務を限定します。
- **FooniUIBridge** (`Runtime/View/FooniUIBridge.cs`): UXML 上の `name="root"` / `name="actor-root"` を取得し、`UIDocument` に適用された Theme と連携します。

## Presenter 層
- **ActorPresenter** (`Runtime/Presenter/ActorPresenter.cs`): 入力や命令を `ActorState` に反映し、`IActorView` に描画を指示します。`RotateBy` / `SetRotation` で `RotationDeg` を管理し、`SetScale` / `ChangeSizeByPercent` で拡大率を 1～300% の範囲に保ちながら UITK の #root スケールを更新します。初期化時に ScriptableObject のデータを取り込みます。
- **VSPresenterBridge** (`Runtime/Presenter/VSPresenterBridge.cs`): Visual Scripting Graph から Presenter API を呼び出すためのブリッジ。`ScriptMachine` の `Variables.Object` に登録され、Macro から Presenter にアクセスできます。

## Composition Root
- **FUnityManager** (`Runtime/Core/FUnityManager.cs`) が初期化の起点です。
  1. `Resources.Load` で `FUnityProjectData` を読み込み、ステージと俳優データを収集します。
  2. `UIDocument` を持つ “FUnity UI” GameObject を生成し、`FUnityPanelSettings.asset` を割り当てます。
  3. 各俳優に対して `ActorState` / `ActorView` / `ActorPresenter` を組み立て、Runner 側の `ScriptMachine` と `FooniUIBridge` を接続します。
4. Visual Scripting Runner に配置した `ActorPresenterAdapter` と `ActorPresenter` を結び付け、`VSPresenterBridge` からの命令を Presenter へ委譲できるようにします。

## Visual Scripting からの呼び出し
- **FUnity/Create/FUnityProjectData** が `Assets/FUnity/VisualScripting/Macros/Fooni_FloatSetup.asset` を用意し、`FUnityActorData_Fooni` の ScriptGraph に設定します。
- Macro からは `Variables.Object("VSPresenterBridge")` を取得し、`Actor/MoveBy` などの Custom Event を介して Presenter を呼び出します。必要に応じて `ActorPresenterAdapter` を取得し、`MoveSteps` や `SetPositionPixels` といった API を直接呼び出すこともできます。
- Presenter 層を経由することで、Visual Scripting と C# 双方から同じロジックを再利用でき、MVP の責務分離を維持できます。`Scratch/Turn Degrees` は `VSPresenterBridge.TurnDegrees` を通じて Presenter の `RotateBy` に委譲され、UI Toolkit のポートレートが中心ピボットで回転します。`Scratch/Set Size %` / `Scratch/Change Size by %` は `VSPresenterBridge.SetActorScale` / `ChangeActorSizeByPercent` を介して `ActorPresenter.SetScale` / `ChangeSizeByPercent` を実行し、UITK の #root が中心基準のまま拡縮されます。
