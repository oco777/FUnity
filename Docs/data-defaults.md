# 既定データの構成

## 目次
- [要点](#要点)
- [手順](#手順)
- [補足](#補足)
- [参考](#参考)

## 要点
- `CreateProjectData.CreateDefault()` が Project/Stage/Actor/PanelSettings/Theme を生成する。
- StageData は `Background_01.png` を背景に使用し、欠損時は TODO を残す。
- ActorData は Portrait/UXML/USS を候補パス優先で割り当て、足りない項目は探索する。

## 手順
### 1. 生成コマンド
- メニュー **FUnity → Create → Default Project Data** を実行する。
- 既存アセットがある場合は上書きせず、新規ファイルとして配置する。

### 2. 生成されるアセット
| アセット | パス | 内容 |
|----------|------|------|
| FUnityProjectData | `Assets/FUnity/Data/Project/FUnityProjectData.asset` | システム全体の参照を保持する。 |
| FUnityStageData | `Assets/FUnity/Data/Stages/FUnityStageData.asset` | 背景に `Art/Backgrounds/Background_01.png` を設定する。 |
| FUnityActorData_Fooni | `Assets/FUnity/Data/Actors/FUnityActorData_Fooni.asset` | Portrait/UXML/USS を自動割当する。 |
| FUnityPanelSettings | `Assets/FUnity/UI/FUnityPanelSettings.asset` | PanelSettings と Theme 参照を持つ。 |
| UnityDefaultRuntimeTheme | `Assets/FUnity/UI/USS/UnityDefaultRuntimeTheme.uss` | フォールバック Theme。 |

### 3. 俳優データの割当ルール
- Portrait は `Assets/FUnity/Art/Characters/Fooni.png` を最優先で参照する。
- UXML は `Assets/FUnity/UXML/FUnityActorPortrait.uxml` を想定し、`name="root"/"portrait"` を持つテンプレを選択する。
- USS は `Assets/FUnity/USS/FUnityActorPortrait.uss` を候補とし、見つからない場合は TODO コメントを残す。

## 補足
- Stage 背景が欠けている場合は `TODO: 背景差し替え` を `FUnityStageData` に記録する。（TODO）
- 俳優データの探索は候補リスト→リソース検索の順で実行する。
- PanelSettings は `Resources/FUnityPanelSettings.asset` にコピーされ、再生時に Theme を付与する。

## 参考
- [環境構築ガイド](setup.md)
- [UI テーマ適用戦略](ui-theme.md)
- [俳優 UI テンプレート](actor-template.md)
