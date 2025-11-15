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

## ランタイム配置規約（厳守）
- ランタイム C# は **必ず** ルート直下の `Runtime/` に置く。`Assets/FUnity/Runtime/` に配置しない。
- `Assets/FUnity/Runtime/` 配下に C# や asmdef/asmref を見つけたら移動し、空になったディレクトリも削除する。
- Visual Scripting の Scratch 系ユニット／ユーティリティは `Runtime/Integrations/VisualScripting/...` に統一する。
- `ScratchUnitUtil` にはレイアウト・座標系のヘルパーを集約し、`TryGetActorWorldRect` を含む共通処理を追加すること。
- `Runtime/Integrations/VisualScripting` のコードを変更した場合は **必ず `Docs/VS_Scratch_Mapping.md` も更新** する。

## Move/Bounce 実装ポリシー（恒久ルール）
- 「もし端に着いたら跳ね返る」を含む Bounce 系の実装は、**反射後に中心座標をステージ内へ押し戻す（クランプ＋ε）処理を必須**とする。
- 端判定は俳優の見た目サイズを基準とし、**半幅・半高（halfSize）を必ず考慮**すること。
- `MoveStepsUnit` を含む移動ユニットは、大きな移動量で境界を飛び越えないように**境界まで進む → 反射 → 残り距離を再移動**のループを実装する。
- Scratch の「1 歩」は **常に 1px** とし、倍率は `MoveStepsUnit` の `StepToPixels` 定数で一元管理する。調整が必要な場合は同定数のみ変更する。

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
- **座標クランプなどの状態整合性は Presenter/Model の更新経路で完結させる**（View 側での `ClampToPanel` は非推奨）
  - View 側の `FooniUIBridge.ClampToPanel` は互換目的のみに残っており、Presenter へのフォワードで即時警告を出す
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
- ツールや初期化コードでシーンルート（例: "FUnity UI"）に `ScriptMachine` をサイレント追加しない。Runner や対象 GameObject に明示配置する。
- プリプロセッサ `#if UNITY_VISUAL_SCRIPTING` は原則 **不要**（必須依存のため）
- 名前空間の衝突に注意（例：`FUnity.Runtime.Input` と `UnityEngine.Input`）
  → **`UnityEngine.Input` を明示**または `using UInput = UnityEngine.Input;` エイリアスを使用

## 俳優 Sprite 運用ルール（2025-05-20 更新）
- `FUnityActorData` の見た目は `Sprites` リストのみで管理し、`Portrait` / `PortraitSprite` フィールドは存在しません。
- Sprites[0] をメインビジュアルとして扱い、Presenter / View は常に `Sprites` または `MainSprite` を参照してください。
- Presenter や View からスプライトを設定する際は `SetSprite(Sprite)` / `ApplyActorData(data, index)` を経由し、Texture2D を直接渡さない。
- SpriteList を利用する API（`ActorPresenter.SetSpriteIndex` / `ActorPresenterAdapter.SetSpriteIndex`）を経由して差分表示を切り替える。直接 `style.backgroundImage` に Texture を設定しない。
- 新規キャラクターアセットは Sprite Editor でスライスした Sprite を `Sprites` に登録し、Texture2D のみの登録は禁止する。
- 俳優の画像表示は UI Toolkit の `Image` 要素を必須とし、背景画像 (`style.backgroundImage`) を直接操作しない。

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
- [ ] `.cs` ファイルを追加した場合は必ず対応する `.meta` を Unity エディタで生成したものとペアで管理し、片方のみになっていない（AI が GUID を捏造しない）
- [ ] `.cs` ファイルを削除・リネームした場合は、対応する `.meta` も同時に削除・リネームして不整合を残さない

## Docs: VS Scratch Mapping の更新ポリシー

**対象:** Visual Scripting の Scratch 互換ユニットおよび関連 API の変更
（例）`Runtime/Integrations/VisualScripting/Units/**`, `ActorPresenterAdapter`, `VSPresenterBridge` のメソッド名/引数/UnitTitle 変更 など

**原則:** これらに変更が入った場合は、**`Docs/VS_Scratch_Mapping.md` を同一 PR 内で更新**してください。

### 更新手順
1. 変更点を確認
   - 追加/改名/削除された Unit の `UnitTitle` / カテゴリ `Scratch/` `Fooni/` を洗い出す
   - 日本語表示（Scratch のブロック名に相当）との対応がズレていないか確認
2. ドキュメントを更新
   - `Docs/VS_Scratch_Mapping.md` の表に **追加/改名/削除**の差分を反映
   - 必要に応じて概要・備考を更新
3. 自動生成ルートがある場合
   - 用意している Editor/スクリプト（UnitTitle 走査）で再生成し、手修正を最小化

### PR チェックリスト（抜粋）
- [ ] VS Unit/タイトル/カテゴリの変更が `Docs/VS_Scratch_Mapping.md` に反映されている
- [ ] 追加/改名/削除の差分が表に反映され、リンク切れ・表崩れがない
- [ ] 変更点に合わせた説明（例：「Forever は1フレーム待機」など）を更新

**Commit 例:**
```
docs(vs): update VS_Scratch_Mapping.md for new/renamed Units
```

> 備考: 上記ルールは `Docs/README.md` や `CONTRIBUTING.md` にも転載して構いません。

## Visual Scripting Unit 共通ルール（恒久）
- FUnity の Visual Scripting 用 Unit を追加・変更するときは必ず以下を満たしてください。
  - `[UnitTitle]` は Scratch 日本語ブロック表記に合わせる（Scratch 非対応の拡張は短い日本語名を付ける）。
  - `[UnitCategory]` は `FUnity/Scratch/◯◯` 形式（カテゴリ名は日本語）で記述する。拡張系は `FUnity/Scratch/拡張` を利用する。
  - `[TypeIcon(typeof(FUnityScratchUnitIcon))]` を付与し、Scratch 系ユニット共通アイコンを使用する。
  - ノード検索性向上のため、利用可能な場合は `[UnitSubtitle]`（または同等の検索キーワード属性）に `funity scratch` とカテゴリ名・関連日本語・英語キーワードを半角スペース区切りで設定する（例：`funity scratch 動き move steps`）。
  - コード差分がある場合は `Docs/VS_Scratch_Mapping.md` を同期させ、タイトルとカテゴリの差異を残さない。
  - 向きの角度を計算する際は **必ず `ScratchUnitUtil.GetDirectionDegreesForCurrentMode` を経由し、`DirFromDegrees` と対で利用**する。Scratch モードでは上=0°/右=90°、通常モードでは右=0° というルールを崩さないこと。

## Scratch マウス座標ユニット規約（2025-03-20 追加）
- UI Toolkit で取得したローカル座標は必ず Scratch 座標系へ変換する。式: `x = local.x - (width * 0.5f)`, `y = (height * 0.5f) - local.y`。
- ステージ論理サイズは UIScaleService などから取得し、幅・高さいずれかが 0 以下の場合は 480x360 へフォールバックする。
- `MousePositionService` などのマウス座標サービスはデフォルトでステージ矩形内へクランプし、`m_ClampToStage` で切り替え可能にする。
- マウス座標ユニットは Value Unit（Flow 不要）とし、カテゴリは必ず `FUnity/Scratch/調べる` を使用する。EventUnit として実装しないこと。
- 整数版ユニットを追加する場合は `Round/Floor/Ceil` を選択できる `IntRounding` enum を用い、`Mathf.RoundToInt` / `Mathf.FloorToInt` / `Mathf.CeilToInt` をそれぞれ呼び出す。
- `MousePositionService` は左ボタン押下状態を `IsPressed` で公開し、PointerDown/PointerUp を確実に購読する。
- 「○秒でマウスポインターへ行く」を含むグライド系ユニットは開始時にマウス座標をサンプリングし、移動中にマウスが動いても追従しない。

## 変数機能に関する恒久ルール
- Scratch 互換の変数は必ず `IFUnityVariableService` / `FUnityVariableService` を経由して操作すること。Presenter や VS からの直接参照は禁止です。
- Visual Scripting の変数系ユニットはサービスへ直接アクセスせず、共通アクセサ（`ScratchVariableUnitUtility` など）を介して呼び出すこと。
- `.cs` ファイルを追加・削除した場合は対応する `.meta` も必ず同時に管理し、`.meta` の生成は必ず Unity エディタに任せる（再掲）。
- 新しいユニットを追加・改名した場合は `Docs/VS_Scratch_Mapping.md` を必ず更新し、対応表を最新に保つこと。

---
## Authoring モードに関する方針 / 将来計画

- FUnity には以下の Authoring モードを用意する。
  - Scratch モード
    - 解像度 480x360（Scratch 互換）
  - Unityroom モード
    - Unityroom 用の推奨解像度に合わせる
  - YouTube モード（将来追加予定）
    - 目的:
      - Visual Scripting でアニメーション作品を作り、
        YouTube にアップロードできる動画ファイルとして書き出すためのモード。
    - 解像度:
      - 基本は 1920x1080（フルHD, 16:9）
      - 負荷を考慮して 1280x720 も選択肢として検討
    - FPS:
      - 30fps または 60fps（FUnityProjectData で設定可能にする）
    - 演出テンプレート:
      - 定型の演出構成をあらかじめ Visual Scripting のグラフとして用意しておく。
        - 例: 「タイトル → 本編 → エンディング」の 3 構成
        - 子どもはテキストやキャラクターを差し替えるだけで、
          YouTube 用の動画構成が手軽に作れるようにする。
    - 録画方法:
      - 実装方針としては Unity Recorder を利用する。
      - Editor 拡張メニュー（例: `FUnity/Authoring/Record YouTube Video…`）から
        再生中の Game ビューもしくは Camera 出力を動画ファイルとして保存する。
      - 必要に応じて `Time.captureFramerate` の利用などを検討し、
        コマ落ちしない一定 FPS の動画を書き出せるようにする。
    - ステータス:
      - 現時点では「構想段階」。すぐには実装しないが、
        将来の拡張として常に意識しておく。

