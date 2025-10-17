# FUnity Runtime MVP Overview

FUnity ではランタイム層に Model–View–Presenter (MVP) を導入し、UI Toolkit を用いた表示と入力処理を分離しました。本書では主要クラスと起動フローを整理します。

## クラス構成

### Model
- **FUnityProjectData / FUnityActorData**: ScriptableObject として背景・アクター構成を定義します。
- **ActorState** (`Runtime/Model/ActorState.cs`): 実行中に変化する座標・速度などの可変情報を保持します。`MoveSpeed` は `FUnityActorData` から初期化されます。

### View
- **IActorView** (`Runtime/View/Interfaces/IActorView.cs`): Presenter が操作する描画用インターフェース。
- **ActorView** (`Runtime/View/ActorView.cs`): `FooniUIBridge` をラップし、UI Toolkit 上の `left/top` 更新とポートレート差し替えのみを担当します。
- **FooniUIBridge**: 既存の View 実装詳細。`BindElement` / `SetPosition` を介して UI 要素を操作します。

### Presenter
- **ActorPresenter** (`Runtime/Presenter/ActorPresenter.cs`): 入力から `ActorState` を更新し、`IActorView` に描画を依頼します。初期化時にアクターの初期座標・ポートレートを反映します。
- **InputPresenter** (`Runtime/Presenter/InputPresenter.cs`): 現行の Input Manager から WASD/矢印入力を読み取ります。後から新 Input System へ差し替えやすい形です。
- **VSPresenterBridge** (`Runtime/Presenter/VSPresenterBridge.cs`): Visual Scripting Graph から `VS_Move(dir, dt)` を呼ぶと、`ActorPresenter.Tick` を実行します。

## Composition Root
- **FUnityManager** (`Runtime/Core/FUnityManager.cs`)
  1. `FUnityProjectData` を読み込み、`FUnity UI` GameObject と UIDocument を用意します。
  2. アクターの UXML/USS を生成し、`ActorView` と `ActorPresenter`、`ActorState` を束ねます。
  3. `Update()` で `InputPresenter.ReadMove()` を呼び出し、各 `ActorPresenter.Tick(dt, dir)` を実行します。
  4. Visual Scripting Runner には `VSPresenterBridge` を `Variables.Object` として渡し、VS からも Presenter 経由で座標更新できるようにしています。

## Visual Scripting からの利用
- `FUnity UI` には `VSPresenterBridge` コンポーネントがアタッチされ、`Target` に最初の `ActorPresenter` が割り当てられます。
- VS グラフでは `Variables.Object("VSPresenterBridge")` を取得し、`VS_Move` カスタムユニットで Presenter を呼び出せます。
- 既存の `FooniUIBridge` API も残っているため、レガシーなグラフとの互換性を維持しつつ、MVP 経由の呼び出しを推奨します。

## 拡張のヒント
- 複数アクターを扱う場合は、`FUnityManager` が生成する `ActorPresenter` / `ActorView` の配列を拡張して、個別の入力や AI Presenter を割り当てられます。
- `ActorState` にアニメーション状態やカスタムフラグを追加することで、View を変更せずに表現のバリエーションを増やせます。
- 新 Input System へ移行する際は `InputPresenter` を差し替えるだけで Presenter 層は再利用できます。
