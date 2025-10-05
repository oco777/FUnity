# 🤝 Contributing to FUnity

FUnity（ファニティ）への関心ありがとうございます！  
このプロジェクトは「Fun × Unity」をテーマに、  
**子ども（小中学生）でも楽しめる Unity 教育環境** を目指しています。

誰でも自由にアイデア・コード・デザインで参加できます。  
このページでは、FUnity に貢献するための手順を紹介します。

---

## 🚀 はじめに

FUnity は以下の目的で開発されています：

- Unity の **Visual Scripting** と **UI Toolkit** を使い、Scratchのように作品を作れる環境を作る  
- 子どもが **絵を描いて、動かして、遊べる** 体験を提供する  
- 教育現場や個人学習で自由に使えるようにする  

---

## 🧰 開発環境

| 項目 | 内容 |
|------|------|
| Unity バージョン | 2022.3 LTS 以降 |
| プラットフォーム | WebGL（unityroom公開対応） |
| スクリプト言語 | C# |
| 主な技術 | UI Toolkit / Visual Scripting / Editor 拡張 |

---

## 🪜 参加の手順

1. **リポジトリをフォーク**
   - GitHubの [FUnity リポジトリ](https://github.com/oco777/FUnity) をフォークします。  
     → 「Fork」ボタンをクリック。

2. **ローカル環境にクローン**
   ```bash
   git clone https://github.com/<yourname>/FUnity.git
   cd FUnity
   ```

3. **新しいブランチを作成**
   ```bash
   git checkout -b feature/your-feature-name
   ```

4. **コードを修正・追加**
   - `Assets/FUnity/` 以下にコードを追加。  
   - Editor拡張は `Assets/FUnity/Editor/` に配置。

5. **テスト・確認**
   - Unityエディタで動作確認。  
   - WebGLビルドでエラーが出ないか確認。

6. **コミットとプルリクエスト**
   ```bash
   git add .
   git commit -m "Add: 〇〇機能を追加"
   git push origin feature/your-feature-name
   ```
   - GitHubで Pull Request を作成。  
   - タイトルと簡単な説明を記載してください。

---

## 🧩 コーディングガイドライン

- 名前空間：`FUnity` または `FUnity.Editor`
- フォルダ構成：
  ```
  Assets/FUnity/
    Scripts/
    UI/
    VisualScripting/
    Editor/
  ```
- C#の命名規則：
  - クラス：PascalCase（例：`PaintWindow`）  
  - 変数：camelCase（例：`drawColor`）  
  - 定数：UPPER_CASE（例：`CANVAS_SIZE`）

---

## 🎨 貢献できる分野の例

| 分野 | 内容 |
|------|------|
| 💻 プログラミング | Editorツール、Visual Scriptingノード、UI機能の追加 |
| 🎨 デザイン | ロゴ、UIレイアウト、子ども向けデザイン提案 |
| 🧠 教材企画 | チュートリアル、教材コンテンツ、ワークショップ案 |
| 🌍 翻訳 | READMEやUIの英語化・多言語化 |
| 🧩 不具合報告 | Issue でのバグ報告や改善提案 |

---

## 🐛 バグ報告・提案方法

- GitHubの [Issues](https://github.com/oco777/FUnity/issues) に投稿してください。  
- タイトルに `[Bug]` や `[Feature]` をつけると分かりやすいです。

**例：**
```
[Bug] PaintWindowでブラシが反応しない
[Feature] 背景レイヤーを追加したい
```

---

## 💬 コミュニティガイドライン

- 互いを尊重し、丁寧な言葉でやり取りしましょう。  
- 初心者や子どもの提案も歓迎します。  
- 教育目的のため、商用利用・広告投稿は控えてください。

---

## 📜 ライセンス

このプロジェクトは [MIT License](./LICENSE) のもとで公開されています。  
自由に利用・改変・配布できますが、クレジット表記を推奨します。

---

## 🙌 開発チームと貢献者

- 開発者：**パパコーダー (PapaCoder)**  
- 公式サイト：[https://papacoder.net](https://papacoder.net)  
- 貢献者リストは [Contributors](https://github.com/oco777/FUnity/graphs/contributors) をご覧ください。
