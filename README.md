# 🎨 FUnity — Fun × Unity  
_A Scratch-inspired visual programming environment for Unity._

---

## 🌐 Language  
🇯🇵 [日本語版を読む](#日本語版) | 🇺🇸 [English Version](#english-version)

---

<a id="日本語版"></a>

# 🇯🇵 日本語版 — FUnity（フーニティ）

**Fun × Unity — 子どもたちのための、楽しく学べるビジュアルプログラミング環境**

![Fooni](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/Fooni.png)

---

## 🌟 プロジェクト概要

**FUnity（フーニティ）** は、Unity と UI Toolkit を使って  
子どもたちが「Scratchのようにブロックを組み合わせて遊びながら学べる」  
教育用プログラミング環境を目指したオープンソースプロジェクトです。

> 「学びを、もっと楽しく、もっと自由に。」

---

## 🧩 主な特徴

- 🎮 **Unity上で動作**：UI Toolkitを活用したモダンなUI。
- 🧱 **ブロック式スクリプティング**：Scratch風の学びやすい構造。
- 🧠 **キャラクターによるナビゲーション**：やさしく世界を案内。
- 🧰 **サンプルシーン付き**：インポートするだけですぐ体験可能。

---

## 🧙 キャラクター紹介

| キャラクター | 役割 | 画像 |
|---------------|------|------|
| 🌬️ **フーニー（Fooni）** | チュートリアル案内役。風の精霊で、ふわふわと浮かびながら学びの世界を案内。 | ![Fooni](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/Fooni.png) |
| 🥷 **ドジョウ忍者（Dojo Ninja）** | アクション担当。挑戦する楽しさを子どもに伝える。 | ![DojoNinja](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/DojoNinja.png) |
| 🦄 **ユニくん（Uni-kun）** | アイデア担当。AIと創造の化身。 | ![UniKun](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/Uni-kun.png) |

---

## 🧰 動作環境

- **Unity 6.0 (6000.0) 以降**
- **UI Toolkit** パッケージ（自動解決されます）

---

## 📦 インストール方法（UPM経由）

Unity の `Packages/manifest.json` に以下を追加します：

```json
"com.papacoder.funity": "https://github.com/oco777/FUnity.git"
```

これで FUnity が Unity Package Manager 経由で導入されます。  
**Package Manager** ウィンドウから **Samples → Basic Scene** をインポートすれば、  
すぐに動作サンプルを体験できます。

---

## 🔧 ローカル開発

1. 本リポジトリをクローンします。
2. Unity プロジェクトの `Packages/manifest.json` に次を追加：
   ```json
   "com.papacoder.funity": "file:../FUnity"
   ```
3. Unity を起動すると、変更内容が即座に反映されます。

---

## 🧩 サンプル内容

`Samples~/BasicScene` に含まれるサンプルでは以下を確認できます：

- `WorkspaceHUD`：UI Toolkitで構成されたHUD表示。
- `BlockElement`：Scratch風ブロックの表示要素。
- `SampleController`：コンソール出力サンプル。

---

## 🗂️ リポジトリ構成

| フォルダ | 内容 |
|-----------|------|
| `Runtime/` | 実行用コード（Core / UI / Blocks） |
| `UXML/` | UI Toolkit レイアウト定義 |
| `USS/` | スタイルシート（テーマ・配色） |
| `Art/` | キャラクター画像やロゴ素材 |
| `Docs/` | 設計資料・キャラクター設定 |
| `Samples~/` | サンプルシーン・チュートリアル |
| `AGENTS.md` | Codex / AIエージェント設定ファイル |

---

## 🧠 Codex対応：エージェント設計

| エージェント名 | 担当範囲 |
|----------------|-----------|
| 🎨 `UIAgent` | UXML・USS・UI改善 |
| 🧱 `BlockAgent` | ビジュアルスクリプティング処理 |
| ⚙️ `CoreAgent` | GameManagerなど基盤コード |
| 🖼️ `ArtAgent` | アート・ロゴ・デザイン整備 |

CodexなどAIペアプログラマーと連携することで、  
自動生成や提案を安全かつ体系的に行える構造です。

---

## 📜 ライセンス

本プロジェクトは [MIT License](LICENSE.md) のもとで公開されています。

---

## 👤 作者情報

- 作者：**パパコーダー**  
- Webサイト：[https://papacoder.net](https://papacoder.net)  
- テーマ：**「学びを、もっと楽しく、もっと自由に。」**

---

> 🚀 **FUnity** は、プログラミングの最初の一歩を「創造の楽しさ」に変えるツールです。  
> 子どもたちが「風のように」自由に学び、作る喜びを感じられる世界を目指しています。

---

<a id="english-version"></a>

# 🇺🇸 English Version — FUnity

**Fun × Unity — A visual programming toolkit that brings Scratch-like learning to Unity.**

![FUnity Banner](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/Fooni.png)

---

## 🌟 Overview

**FUnity** is an educational open-source project built with Unity and UI Toolkit.  
It enables children to learn programming through visual, block-based interactions similar to Scratch.

> “Learn with freedom, and have fun while creating.”

---

## 🧩 Features

- 🎮 Works natively inside **Unity**
- 🧱 **Block-based scripting** using UI Toolkit
- 🧠 Character-based navigation and tutorial guidance
- 🧰 Includes **sample scenes** for immediate exploration

---

## 🧙 Characters

| Character | Role | Image |
|------------|------|-------|
| 🌬️ **Fooni** | Wind spirit and main guide of FUnity. | ![Fooni](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/Fooni.png) |
| 🥷 **Dojo Ninja** | Teaches the joy of challenge and persistence. | ![DojoNinja](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/DojoNinja.png) |
| 🦄 **Uni-kun** | A curious unicorn symbolizing AI and creativity. | ![UniKun](https://raw.githubusercontent.com/oco777/FUnity/a44577a60c0e3e6f32e8277b2ce32d0608b4e12e/Art/Characters/Uni-kun.png) |

---

## 🧰 Requirements

- Unity **6.0 (6000.0)** or newer  
- UI Toolkit package (`com.unity.ui`)

---

## 📦 Installation via UPM

Add the following line to your `Packages/manifest.json`:

```json
"com.papacoder.funity": "https://github.com/oco777/FUnity.git"
```

Then, open **Package Manager → Samples → Basic Scene**  
to import a ready-to-play example.

---

## 🔧 Local Development

1. Clone this repository beside your Unity project.
2. Reference it locally in your `manifest.json`:
   ```json
   "com.papacoder.funity": "file:../FUnity"
   ```
3. Launch Unity — edits in FUnity will sync instantly.

---

## 🧩 Samples

- `WorkspaceHUD`: UI Toolkit HUD example  
- `BlockElement`: Scratch-style visual block  
- `SampleController`: Console message sample

---

## 🗂️ Repository Layout

| Folder | Description |
|---------|-------------|
| `Runtime/` | Core runtime (Core / UI / Blocks) |
| `UXML/` | UI layout definitions |
| `USS/` | Style sheets for UI Toolkit |
| `Art/` | Logos, characters, and design assets |
| `Docs/` | Design docs and character profiles |
| `Samples~/` | Example scenes and scripts |
| `AGENTS.md` | Codex AI agent configuration |

---

## 🧠 Codex Agents

| Agent | Description |
|--------|-------------|
| 🎨 `UIAgent` | UI layout and styling (UXML/USS) |
| 🧱 `BlockAgent` | Visual scripting functionality |
| ⚙️ `CoreAgent` | Core systems and initialization |
| 🖼️ `ArtAgent` | Artwork, logo, and visual identity |

---

## 📜 License

Released under the [MIT License](LICENSE.md).

---

## 👤 Author

- **Papacoder**  
- Website: [https://papacoder.net](https://papacoder.net)  
- Theme: “Making learning more fun and more free.”

---

> 🚀 **FUnity** turns a child’s first step into programming into a joyful act of creation.  
> We aim to create a world where learning flows as freely as the wind.
