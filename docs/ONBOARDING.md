# FUnity オンボーディングガイド

## プロジェクト概要
FUnity は Unity 6 (エディターバージョン `6000.0.58f2`) を用いた URP ベースの 2D/3D ハイブリッド学習プロジェクトです。`FUnity/Assets/FUnity` フォルダ配下に共通スクリプトとカスタムエディターツールをまとめており、チームで繰り返し使える作業効率化機能を育てていきます。本ガイドでは、リポジトリの把握から日々の開発フローまで、参加初日のセットアップに必要な情報を整理しています。

## 環境要件
- **Unity Hub**: 最新版 (3.x 以上) を使用し、プロジェクトで指定されたエディターバージョンをインストールしてください。
- **Unity エディター**: `6000.0.58f2` (Unity 6)。`ProjectSettings/ProjectVersion.txt` に記載されています。
- **開発 IDE**: Visual Studio、Rider など C# の補完が効く IDE を用意すると便利です。`manifest.json` では `com.unity.ide.visualstudio` を有効化しています。
- **追加モジュール (任意)**: Windows / macOS 向け Build Support、必要に応じて WebGL など。ビルド対象に合わせてインストールしてください。

## 初期セットアップ手順
1. **リポジトリを取得**
   ```bash
   git clone <このリポジトリの URL>
   cd FUnity
   git checkout work # メインブランチ
   ```
2. **Unity Hub にプロジェクトを追加**
   - `FUnity/FUnity` フォルダを指定して開きます。初回起動時はパッケージのインポートが終わるまで待機してください。
3. **シーンとレンダリング設定の確認**
   - `Assets/Scenes/SampleScene.unity` を開き、URP の設定が適用されているか (マテリアルがピンクになっていないか) を確認します。
   - ピンク表示の場合は `Edit > Project Settings > Graphics` で `PC_RPAsset.asset` を `Scriptable Render Pipeline Settings` に割り当ててください。
4. **Input System の確認**
   - `Assets/InputSystem_Actions.inputactions` が最新か確認し、更新した場合は Inspector から `Apply Changes to Asset` を忘れないようにします。
   - 必要に応じて `Generate C# Class` を有効化し、変更後にコミットしてください。
5. **カスタムエディターツールの試用**
   - Unity メニューの **FUnity > Paint Window** を開くと、シンプルなドローイングツールでテクスチャを編集できます。
   - **FUnity > Paint & Save as Sprite** では描いた内容を PNG として保存し、`Assets/` 配下に保存した場合は自動で Sprite 化とシーンへの配置 (GameObject `FUnitySprite`) まで行われます。
6. **プロジェクト設定の同期**
   - `.meta` ファイルを含めて差分を確認し、意図しない設定変更がないかレビューします。

## リポジトリ構成の見取り図
| パス | 役割 | 補足 |
| --- | --- | --- |
| `README.md` | プロジェクトのトップ概要と主要ドキュメントへの導線 | 変更時はオンボーディングガイドも更新 |
| `docs/` | ドキュメント置き場 | 本ガイドを含むナレッジを蓄積 |
| `FUnity/` | Unity プロジェクトのルート | Unity Hub で開くフォルダ |
| `FUnity/Assets/FUnity/` | 共通スクリプトとカスタムツール | `Editor/` 配下にペイント用ウィンドウ (`PaintWindow.cs`, `PaintToSprite.cs`) を格納 |
| `FUnity/Assets/InputSystem_Actions.inputactions` | 新 Input System のアクションマップ | 変更時は `.inputactions` と生成された C# の両方をコミット |
| `FUnity/Assets/Scenes/SampleScene.unity` | 検証用の初期シーン | カスタム Sprite 生成時のスポーン先 |
| `FUnity/Assets/Settings/` | URP 用の Render Pipeline Asset と Volume | シーン追加時は適切なアセットを参照 |
| `FUnity/Packages/manifest.json` | 依存パッケージ一覧 | URP (`com.unity.render-pipelines.universal`) や Input System などを管理 |
| `FUnity/ProjectSettings/ProjectVersion.txt` | 使用すべき Unity バージョン | チームで統一 |

## 実装済み機能の概要
- **Universal Render Pipeline**: `PC_RPAsset.asset` と `Mobile_RPAsset.asset` を用意しています。新規シーンでは Render Pipeline Asset の割り当てを確認してください。
- **Input System**: `InputSystem_Actions.inputactions` に入力アクションを定義しています。プレイヤーコントロール追加時はここにアクションを追加し、`Generate C# Class` を再生成します。
- **カスタムペイントツール**: `PaintWindow` と `PaintToSprite` の 2 種類のエディターメニューを提供します。`PaintToSprite` は描画した結果を PNG/Sprite に変換し、`SpriteRenderer` を持つ GameObject (`FUnitySprite`) をシーンに配置します。
- **アセンブリ定義**: `FUnity.asmdef` と `FUnity.Editor.asmdef` によって、ランタイムコードとエディタコードが分離されています。新しいスクリプトを追加する際は該当するアセンブリに含まれているか確認してください。

## 重要事項・開発ルール
- **Unity バージョンの統一**: `6000.0.58f2` 以外で開いた場合は差分が発生しやすいため、必ずバージョンを合わせてください。
- **Git 運用**: メインブランチは `work` です。機能追加はトピックブランチを切り、PR でレビューを受けてからマージします。PR には確認方法 (操作手順・スクリーンショット等) を添付してください。
- **アセット管理**: PNG・Audio などのバイナリは必要に応じて Git LFS を導入します。`Assets/` 配下の `.meta` を忘れずコミットし、GUID の不整合を防ぎます。
- **Input System 更新時の注意**: `.inputactions` ファイルを編集したら `Apply Changes`、生成された C# クラスの差分も併せてコミットします。既存のプレハブがアクションを参照している場合は影響範囲を必ずテストしてください。
- **カスタムツールの拡張**: `FUnity/Assets/FUnity/Editor` で提供するツールを拡張する際は、操作ガイドを `docs/` に追記して周知します。
- **ドキュメント更新**: 仕様決定や運用ルールの変更は必ず `docs/` に追記し、`README.md` からリンクを張り替えます。オンボーディング情報は常に最新状態を保つことを目指します。

## 新規参加者の学習ステップ
1. **リポジトリとドキュメントを俯瞰**: `README.md` と本ガイドを読み、現状の機能と進行中タスクを把握します。
2. **サンプルシーンで動作確認**: `SampleScene` に追加されている GameObject やレンダリング設定を確認し、描画結果が期待通りかチェックします。
3. **Input System を理解**: アクションマップの構成や想定するプレイヤー操作を確認し、必要であれば `Assets/FUnity` 配下にスクリプトを追加して挙動を試します。
4. **カスタムペイントツールのワークフローを習得**: `Paint & Save as Sprite` で Sprite を生成し、`SampleScene` に自動配置される GameObject の扱いを試してみてください。
5. **次に学ぶ領域を決める**: 不足しているアセット (Sprite、SFX、UI)、ゲームプレイロジック、エディタ拡張など、担当予定の領域を確認し、関連する Unity 機能をキャッチアップします。

## FAQ (随時更新)
- **Q. `Paint & Save as Sprite` で保存したのにシーンにオブジェクトが出ない**
  - A. 保存先が `Assets/` 配下でない場合は Sprite 化と配置が行われません。`Assets/Sprites` などプロジェクト内に保存して再度実行してください。
- **Q. Unity 起動時にマテリアルがピンクになる**
  - A. URP の Render Pipeline Asset が未設定です。`Edit > Project Settings > Graphics` の `Scriptable Render Pipeline Settings` に `PC_RPAsset.asset` もしくは適切な RP Asset を設定してください。
- **Q. Input System の変更が反映されない**
  - A. `.inputactions` 編集後に Inspector の `Apply Changes to Asset` を押し、生成された C# クラスを再生成してください。生成されたクラスを参照するスクリプトはリコンパイル後に Unity を再生すると反映されます。

---
プロジェクトは継続的にアップデートされます。疑問点があれば Discord / Slack などのチームチャネルで共有し、ノウハウを `docs/` に蓄積していきましょう。
