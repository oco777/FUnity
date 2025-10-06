# 🎨 FUnity  
[![Unity](https://img.shields.io/badge/Unity-2022%2B-black?logo=unity)]()
[![License](https://img.shields.io/badge/license-MIT-blue.svg)]()
[![Platform](https://img.shields.io/badge/platform-WebGL-orange)]()
[![MadeWithUnity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?logo=unity)]()

> **Fun × Unity – 描いて、動かして、遊べるプログラミング環境**

FUnity（ファニティ）は、子ども（小中学生）向けに開発中の
**Scratch風の教育的Unityプロジェクト** です。
Unityの **UI Toolkit** や **Visual Scripting** を活用することを目指し、
「絵を描いて」「動かして」「遊べる」作品を簡単に作れる環境づくりに取り組んでいます。

---

## ✨ コンセプト

- **Fun × Unity = 楽しく × Unityで**  
- コードを書かずに、ブロック（Visual Scripting）でキャラを動かす  
- 自分で描いた絵をそのままキャラクターにできる  
- unityroom に公開して、みんなと作品を共有できる

---

## 🧠 主な機能

| 機能 | 内容 |
|------|------|
| 🎨 **エディタ拡張ペイントツール** | Unityエディタ上で絵を描いてSprite化 |
| 🧾 **Stage Sprite定義の自動生成** | 描いたSpriteからUI Toolkit用スプライト定義を同時生成 |
| 🌐 **unityroom公開対応** | WebGLビルドで作品を共有 |
| 🧩 **Visual Scripting連携（予定）** | ブロックプログラミング風の操作感を提供予定 |
| 🧰 **UI Toolkitベースの舞台** | Scratchライクなステージとスプライト一覧を自動構築 |

### 🧑‍💻 Visual Scripting + UI Toolkit ステージの使い方

1. **シーンを再生**すると、自動的に `FUnity Stage` GameObject が生成されます。UI Toolkitで構成されたステージ／スプライトパネル／Visual Scriptingガイドが表示されます。
2. エディタ拡張の「Paint & Save as Sprite」で描いた画像を保存すると、Spriteと同じ場所に `*_StageSprite.asset` が生成されます。
3. Visual Scripting Graph から `StageVisualScripting.Spawn` を呼び出し、生成した `StageSpriteDefinition` を渡すと、ステージ上にスプライトが配置されます。
4. 生成された `StageSpriteActor` コンポーネントの `MoveBy` / `MoveTo` / `SetSprite` などのメソッドをVisual Scriptingノードから呼び出すことで、Scratchのようにスプライトを動かせます。

> 📌 Stageは `StageBootstrapper` により自動生成されるため、既存のシーンを編集する必要はありません。プレイモードに入るだけでScratch風ワークスペースが立ち上がります。

---

## 📁 プロジェクト構成

```
FUnity/
 └─ Assets/
     └─ FUnity/
         ├─ FUnity.asmdef
         └─ Editor/ ← Editor拡張用フォルダ
             ├─ FUnity.Editor.asmdef
             ├─ PaintToSprite.cs
             └─ PaintWindow.cs
```

- `FUnity.asmdef`：ランタイム用のメインモジュール（将来の拡張を想定）
- `FUnity.Editor.asmdef`：Editor拡張（ペイントツールなど）
- `PaintToSprite.cs`：ペイントデータをSpriteに変換
- `PaintWindow.cs`：ペイントウィンドウの基本機能

---

## 🚀 ビルド手順（unityroom公開）

1. **Build Settings**  
   - Platform：WebGL  
   - Compression Format：Disabled  
   - Scenes in Build：メインシーンを追加  

2. **Player Settings**  
   - Resolution：1280×720  
   - Template：Default  
   - Publishing Settings → Compression Format：Disabled  

3. **ビルド実行**  
   - 出力先：`Build/WebGL/`

4. **unityroomにアップロード**  
   - `Build/WebGL` 内のファイルをすべてZIP圧縮してアップロード  
   - タイトル例：「FUnityで描いて動かす！」

---

## 💡 今後の予定

- [ ] ペイントツールの強化（ブラシ、色、保存機能）  
- [ ] Visual Scriptingノードの教育向け拡張  
- [ ] UI Toolkitによるステージ構築エディタ  
- [ ] unityroom公開テンプレート整備  
- [ ] 教材化・ワークショップ用コンテンツ作成  

---

## 🧑‍💻 作者
**パパコーダー (PapaCoder)**  
開発・構想・デザイン  
🌐 [https://papacoder.net/](https://papacoder.net/)

---

## 📜 ライセンス
MIT License  
教育・非商用利用は自由です。  
詳細は [LICENSE](./LICENSE) を参照してください。

---

## 🔗 関連リンク

- 🎮 unityroom（作品公開予定・準備中）  
  https://unityroom.com/games/funity  

- 💻 GitHubリポジトリ  
  https://github.com/oco777/FUnity
