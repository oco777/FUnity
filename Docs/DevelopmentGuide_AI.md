# ⚙️ FUnity 開発効率化ガイド（ChatGPT × Codex連携）

---

## 🧭 概要

FUnityは、ChatGPT（GPT-5）とCodexを活用したAI協働開発プロジェクトです。  
このガイドでは、効率的に開発を進めるためのプロンプト設計と、
リポジトリ管理・実装フローの最適化方法を解説します。

---

## 🧩 AI連携の役割分担

| 役割 | 担当AI | 主なタスク |
|------|---------|-------------|
| 🎨 設計・構造化 | **ChatGPT（GPT-5）** | 仕様設計、プロンプト作成、命名、構成整理 |
| 🧱 コード生成・実装 | **Codex** | Unity C#/UXML/USSコード生成、修正 |
| 🔍 検証・整理 | **ChatGPT** | コードレビュー、ドキュメント化、改善提案 |

ChatGPT = **エンジニアリングマネージャー**  
Codex = **ペアプログラマー** という関係を意識します。

---

## 🧠 開発フロー（5ステップ）

### ① ChatGPTにリポジトリ構造を理解させる

- **uithub** で `FUnity` / `FUnityProject` を読み込ませる。
- `README.md`, `Docs/`, `package.json` の内容を確認させる。
- ChatGPTが最新構成を理解してからプロンプト生成に入る。

💡 コツ：  
「このリポジトリのフォルダ構成を要約しておいて」と伝える。

---

### ② Codex用の「構造化プロンプト」をChatGPTで生成

Codexは短文指示では誤解しやすいため、
ChatGPTが**構造化した明示的プロンプト**を組み立てます。

```text
# Codex Instruction
## タスク概要
FUnityに〜機能を追加する。

## ファイル出力場所
Runtime/UI/FUnityManager.cs

## 実装要件
- UIDocumentからrootVisualElementを取得
- FUnityProjectData.assetを読み込み
- StageElementとActorElementをUI上に生成
- エラー時はDebug.LogWarningで通知

## 確認条件
Unity 6000.0.58f2 / UI Toolkit対応
ビルドエラーなし
```

💡 ポイント：  
- 「どのファイルに」「何を」「どんな条件で」を必ず明示。

---

### ③ Codex出力をChatGPTでレビュー

Codex生成後、ChatGPTにコードを貼り付けてレビューさせる。

| チェック項目 | 目的 |
|---------------|------|
| 🔧 構文整合性 | APIバージョンの差異検出 |
| 📁 フォルダ適合性 | 実際の構成に沿っているか |
| 💡 保守性 | 命名・拡張性・コメント |

💡 コツ：  
「このコードをFUnityアーキテクチャに統合できるかレビューして」と依頼。

---

### ④ Git管理とコミット

ChatGPTにコミットメッセージを生成させると効率的です。

例：
```
git add Runtime/UI/FUnityManager.cs
git commit -m "Add dynamic UI generation via StageElement and ActorElement (#12)"
```

💡 コツ：  
「今回の変更を英語でGitHubフレンドリーに要約して」と依頼。

---

### ⑤ ドキュメント更新の自動化

- ChatGPTに `CHANGELOG.md` や `Docs/` の更新文を自動生成させる。
- Codexへの次回依頼時に履歴を反映できる。

💡 コツ：  
「今回の改修をCHANGELOG形式で追加して」と依頼。

---

## 🧰 効率を上げるツール連携Tips

| ツール | 目的 | コツ |
|--------|------|------|
| 🪣 **uithub** | ChatGPTがリポジトリ構造を理解 | 「フォルダ構成を確認」と一言で全読込 |
| 🧠 **Codex** | コード生成 | ChatGPTで構造化プロンプトを作成 |
| 🧩 **Copilot Chat** | Unity Editor補助 | 小規模修正や補完に活用 |
| 📝 **ChatGPT Canvas/Docs** | 設計・メモ管理 | Markdownドキュメントの下書きに便利 |

---

## 🔁 推奨開発サイクル

```
1️⃣ ChatGPT：仕様整理＋Codexプロンプト作成
2️⃣ Codex：実装生成
3️⃣ ChatGPT：コードレビュー・修正案
4️⃣ Unity Editor：動作確認
5️⃣ ChatGPT：CHANGELOG更新
🔁 次の機能へ
```

---

## 🧾 Scratch本対応ガイド

- **Custom Event 命名規則**：`"{Domain}/{Action}"` を基本とし、Visual Scripting から `VSPresenterBridge` の以下メソッドを呼び出す。

| Scratchブロック | Custom Event | C# ルート |
|------------------|--------------|-----------|
| 「x座標を～にする」 | `Actor/SetPosition` | `VSPresenterBridge.OnActorSetPosition(x, y)` → `ActorPresenter.SetPosition` |
| 「x座標を～ずつ変える」 | `Actor/MoveBy` | `VSPresenterBridge.OnActorMoveBy(dx, dy)` → `ActorPresenter.MoveBy` |
| 「～と言う」 | `Actor/Say` | `VSPresenterBridge.OnActorSay` → `ActorView.ShowSpeech` |
| 「大きさを～％にする」 | `Actor/SetSize` | `VSPresenterBridge.OnActorSetScale` → `ActorPresenter.SetScale` |
| 「背景を～にする」 | `Stage/SetBackground` | `StageBackgroundService.SetBackground` |

- **AOT 対策**：`Assets/Link.xml` に `VSPresenterBridge` / `StageBackgroundService` / `TimerServiceBehaviour` を列挙し、IL2CPP でもリフレクション呼び出しが剥がれないようにする。
- **Stage 背景**：`StageBackgroundService` が UI Toolkit ルートに `backgroundImage` / `backgroundColor` を適用する。Presenter 層から `ApplyStage` を呼ぶだけで Scratch の「背景を変える」に相当。
- **タイマー**：`TimerServiceBehaviour.Invoke(delay, Action)` を介して `InvokeCustomEventAfter` が動作する。Scratch の「n 秒後」を Visual Scripting の Custom Event で再現できる。
- **マクロ雛形**：エディタメニュー **FUnity > VS > Create Scratch Macros** で `SayOnKey` / `MoveWithArrow` など 5 種のテンプレートを生成。Graph コメントに `VSPresenterBridge` との結線例を掲載している。
- **Starter データ**：**FUnity > Create > Scratch Starter (Actor+Stage+VS)** が `FUnityActorData_Starter` と Runner を自動生成。生成された Runner は `ScriptMachine` と Object 変数 `VSPresenterBridge` を保持し、即座にノード編集が可能。

## 💡 上級テクニック

| シーン | コツ |
|--------|------|
| 🔄 リファクタ | ChatGPTに依存関係を可視化させ、Codexで再構成 |
| 📁 構造変更 | 「UPM互換構造で再配置するプロンプト」をChatGPTで作成 |
| 🎨 UI生成 | UXML/USS生成はCodex、調整はChatGPT |
| 📚 Docs | ChatGPTで「Docs自動生成プロンプト」を構築 |

---

## 🚀 まとめ：FUnity流 AI開発三原則

1. **ChatGPTはマネージャー、Codexは実装者。**
2. **「どこに・何を・どう動かすか」を明示する。**
3. **リポジトリを常にChatGPTが理解できる状態に保つ。**

---

© 2025 パパコーダー  
FUnity Project - Scratch inspired visual programming for Unity
