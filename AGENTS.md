# AGENTS.md

## 目的
このドキュメントは、FUnity のコードを自動生成・修正するエージェント（Codex 等）が**常に意識すべき指針**をまとめたものです。  
特に **MVP パターンの徹底**, **日本語での分かりやすいコメント**, **コーディング規約（`m_` プレフィックス）** を最重要項目として扱います。

## 基本原則（必読）
- **MVP（Model–View–Presenter）を厳守**：
  - **Model**＝静的設定（ScriptableObject）＋ランタイム状態
  - **View**＝UI 表示・見た目の更新のみ（UI Toolkit / UXML/USS, Mono は“薄く”）
  - **Presenter**＝入力→状態更新→View 反映の仲介。ロジックは Presenter に集約
- **日本語コメントを丁寧に**：クラス／メンバ変数／プロパティ／メソッドには、役割・前提・副作用・注意点を**日本語で**書く
- **コーディング規約**：
  - **メンバ変数は `m_` で始める**（例：`m_speed`, `m_actorView`）
  - **プロパティは PascalCase**（例：`DefaultSpeed`）
  - **メソッドは PascalCase**（例：`Initialize`, `Tick`）
  - **ローカル変数は camelCase**（例：`currentSpeed`）
  - 不要な `using` を入れない／名前空間の衝突に注意（`UnityEngine.Input` は明示可）

## MVP の適用方針
- **Model**
  - ScriptableObject 群（`FUnityProjectData`, `FUnityActorData`, `FUnityStageData`）＝**静的設定**
  - ランタイム状態クラス（例：`ActorState`）＝**変化する値**（座標・速度など）
- **View**
  - UI 更新（座標/見た目の反映）のみ担当  
  - `FooniUIBridge` のような UI 操作ヘルパーは View 側の実装詳細として扱う
- **Presenter**
  - 入力やイベントを受けて **Model を更新し、View に反映**
  - 可能な限り **MonoBehaviour にしない**（テスト容易化）。必要時のみアダプタを用意
- **Visual Scripting**  
  - VS から Presenter を呼ぶブリッジを用意してもよい（例：`VSPresenterBridge`）  
  - 直接 View をいじるグラフは可だが、**推奨は Presenter 経由**

## コメント規約（日本語）
- **クラス**：目的／責務／関連クラス
- **メンバ変数**：用途／単位／初期値の意味
- **プロパティ**：公開理由／不変条件
- **メソッド**：何をするか／引数の意味／戻り値／副作用／例外／注意点
- 長くなる説明は `<summary>` XML ドキュメントコメントで記述し、**短文でも日本語で端的に**。

### 例（最小）
```csharp
/// <summary>
/// 俳優（キャラクター）のランタイム状態を保持します。
/// UI 上の座標はピクセル単位で扱います。
/// </summary>
public sealed class ActorState
{
    /// <summary>現在座標（px）。左上原点で、右+ / 下+。</summary>
    public Vector2 Position;

    /// <summary>移動速度（px/sec）。0以上を想定。</summary>
    public float Speed;
}
```

## 命名規約（抜粋）
- フィールド：`private`/`protected` は **`m_`**（例：`m_state`, `m_defaultSpeed`）
- プロパティ：`PascalCase`（例：`DefaultSpeed`）
- メソッド：`PascalCase`（例：`Initialize`, `SetPosition`, `Tick`）
- インターフェース：`I` 接頭（例：`IActorView`）
- 名前空間：`FUnity.Runtime.<Layer>`（`Model` / `View` / `Presenter` / `Core` など）

## Unity / Visual Scripting に関する注意
- FUnity は **Visual Scripting を必須**とし、API を**直参照**（`using Unity.VisualScripting;`）
- `ScriptMachine` の付与・グラフ割当などは **Presenter から直接ではなく**、ブリッジや初期化コードで行う
- プリプロセッサ `#if UNITY_VISUAL_SCRIPTING` は原則 **不要**（必須依存のため）
- 名前空間の衝突に注意（例：`FUnity.Runtime.Input` と `UnityEngine.Input`）  
  → **`UnityEngine.Input` を明示**または `using UInput = UnityEngine.Input;` エイリアスを使用

## 例外・エラー処理の方針
- **早期 return** と **わかりやすい `Debug.LogWarning/Error`** を徹底
- 例外は基本的に**投げない**（Unity ランタイムの都合）。代わりにガード節で防御
- Null 許容：外部参照は `null` ガード＋メッセージを必ず

## PR チェックリスト（Codex 向け）
- [ ] MVP になっている（ロジックが View に混ざっていない）
- [ ] すべての **クラス／メンバ変数／プロパティ／メソッドに日本語コメント**がある
- [ ] **メンバ変数は `m_`** で始まる
- [ ] 余計な `using` が無く、**名前空間衝突がない**（`UnityEngine.Input` 明示など）
- [ ] 例外ではなく警告ログ＋早期 return で安全にハンドル
- [ ] 既存の挙動（背景＋フーニー表示、VS 連携）が壊れていない
- [ ] サンプルシーンで移動が確認できる

---
