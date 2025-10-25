# 既定データの構成

**FUnity/Create/Default Project Data** が生成・更新するアセットと、その配置パスを整理します。

## 目次
- [生成されるアセット一覧](#生成されるアセット一覧)
- [俳優データの後処理](#俳優データの後処理)
- [関連ドキュメント](#関連ドキュメント)

## 生成されるアセット一覧
| アセット | パス | 説明 |
|----------|------|------|
| `FUnityProjectData.asset` | `Assets/Resources/FUnityProjectData.asset` | プロジェクト全体のステージ・俳優参照をまとめる ScriptableObject。既定で `FUnityStageData` と `FUnityActorData_Fooni` を登録。 |
| `FUnityStageData.asset` | `Assets/Resources/FUnityStageData.asset` | 背景画像として `Runtime/Resources/Backgrounds/Background_01.png` を設定し、ステージのカラースキームを既定化。 |
| `FUnityPanelSettings.asset` | `Assets/FUnity/UI/FUnityPanelSettings.asset` | `PanelSettings` を生成し、ThemeStyleSheet に Unity 既定 Theme または FUnity 生成 Theme を割り当てる。 |
| `FUnityActorData_Fooni.asset` | `Assets/FUnity/Data/Actors/FUnityActorData_Fooni.asset` | Portrait/UXML/USS を既定テンプレートで割り当て、ScriptGraph に `Fooni_FloatSetup.asset` を設定。 |
| `Fooni_FloatSetup.asset` | `Assets/FUnity/VisualScripting/Macros/Fooni_FloatSetup.asset` | Visual Scripting の Macro。既存ファイルが無い場合に自動生成され、`FUnityActorData_Fooni` へ割り当てられる。 |

> `FUnityProjectData.asset` と `FUnityStageData.asset` は `Assets/Resources/` 直下に生成され、`Resources.Load` で即座に参照できるようになっています。差分管理のため、バージョン管理では `Assets/Resources/` 配下を追跡してください。

## 俳優データの後処理
- 既存の `Assets/Resources/FUnityActorData_Fooni.asset` が見つかった場合は自動で削除し、`Assets/FUnity/Data/Actors/FUnityActorData_Fooni.asset` のみに統一されます。
- Portrait/UXML/USS は `Assets/FUnity/Art/Characters/`、`Assets/FUnity/UI/UXML/`、`Assets/FUnity/UI/USS/` を優先的に参照します。欠落している場合はログで警告し、最小構成で生成します。
- Macro が見つからない場合は `Assets/FUnity/VisualScripting/Macros/` に `Fooni_FloatSetup.asset` を新規作成し、`ScriptMachine` へ即時割り当てされます。

## 関連ドキュメント
- [導入手順](setup.md)
- [UI テーマ適用戦略](ui-theme.md)
- [トラブルシュート集](troubleshooting.md)
