# Install Guide

FUnity をプロジェクトへ組み込む手順と、セットアップを自動化するツールの使い方をまとめます。

## 1. パッケージ導入
1. Unity の Package Manager で **Add package from git URL...** を選択します。
2. `https://github.com/oco777/FUnity.git#v0.5.0` を入力してインポートします。
3. 依存パッケージ（Visual Scripting など）が自動で追加されます。

## 2. プロジェクト生成ウィザード
`FUnityProjectCreatorWindow` を使うと、新規プロジェクトに必要なアセットをまとめて生成できます。

1. メニュー **FUnity/Create/Project Creator...** を開きます。
2. 保存先を選び、**Create** を実行すると以下が自動生成されます。
   - `FUnityProjectData`（プロジェクト設定）
   - `FUnityStageData`（ステージ背景と論理サイズ）
   - `PanelSettings` や UI テーマ（不足していれば同梱テンプレートから生成）
   - `FUnityActorData_Fooni` とサンプル Macro
3. シーンに `FUnityManager` を配置し、生成された `FUnityProjectData` を参照に設定します。

## 3. ActorData / ProjectData の構成
- **FUnityProjectData**
  - モード設定（Block Mode / unityroom モード）
  - 既定の StageData / PanelSettings / UI テーマ参照
  - サンプル背景リストとターゲット FPS
- **FUnityActorData**
  - Sprites リスト（コスチューム）と初期インデックス
  - Visual Scripting Macro 参照と吹き出し設定
  - 移動速度や当たり判定設定
- **FUnityStageData**
  - 背景スプライトのリスト
  - ステージ論理サイズ（例: 480x360）

## 4. シーンへの適用
1. `FUnityManager` をシーンへ 1 体追加します。
2. `FUnityProjectData` を `FUnityManager` の参照に割り当てます。
3. 再生すると `FUnityManager` が StageView / ActorView を生成し、`ActorPresenterAdapter` と `VSPresenterBridge` を通じて Macro を実行します。

## 5. トラブルシュートのヒント
- UI が表示されない場合は、`FUnityProjectCreatorWindow` を再実行して PanelSettings とテーマが生成されているか確認してください。
- Macro を差し替えた後は、`FUnityActorData` の ScriptGraph 参照が切れていないかを確認します。
- Block Mode の角度がずれる場合は、`ScratchUnitUtil.GetDirectionDegreesForCurrentMode` を利用するユニットが最新か確認してください。
