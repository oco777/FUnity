# 🤖 AGENTS.md — FUnity開発エージェント設定

## 🎯 プロジェクト概要
**FUnity** は、UnityとUI Toolkitを用いて子どもがプログラミングを学べる「Scratch風」環境を目指した教育用プロジェクトです。  
UIブロック、ビジュアルスクリプティング、キャラクター演出などを通じて、「Fun × Unity」を体現します。

---

## 🧩 リポジトリ構成

| フォルダ | 説明 |
|-----------|------|
| `Runtime/` | ランタイムコード（UI, Core, GameManagerなど） |
| `USS/` | UI Toolkit スタイル定義 |
| `UXML/` | UI Toolkit レイアウト定義 |
| `Samples~/BasicScene/` | サンプルシーン（FUnityManager 単体での最小表示を確認） |
| `Art/` | キャラクターやロゴなどのアート素材 |
| `Docs/` | ドキュメント、キャラクター設定、設計資料など |

---

## 🧠 Codex Agent 指針

### 🎨 `UIAgent`
UI Toolkit・UXML・USS 関連の改善、UIレイアウト修正を担当。  
`Runtime/UI/`, `UXML/`, `USS/` を主に扱う。  

### 🧱 `BlockAgent`
ビジュアルスクリプティング（ブロック操作）機能を開発。  
`Runtime/Blocks/` を担当。  
Scratchのようなブロック定義や動作シミュレーションを実装。

### ⚙️ `CoreAgent`
基盤コード（`GameManager`, `PanelSettingsInitializer`, etc.）の安定化と最適化を担当。  
依存パス（manifest.json）調整、`Resources` 管理も行う。

### 🖼️ `ArtAgent`
`Art/Characters` のキャラクターやUIデザイン、ロゴ制作を担当。  
画像の追加・整形、UIへの適用指示を行う。

---

## 🔧 開発ルール
- 生成されたコードはC#（Unity 2022+）に準拠。
- フォルダ構成を壊さないこと。
- `namespace FUnity` を維持。
- UXMLは `UXML/`、USSは `USS/`、リソースは `Runtime/Resources/` に配置。
- 新規機能は `Runtime/` 以下にクリーンな構成で追加。

---

## 📜 作者メモ
- **メインキャラクター:** フーニー（風の精霊）
- **サブキャラクター:** ドジョウ忍者、ユニくん
- **テーマ:** 「学びを、もっと楽しく、もっと自由に」

---

## 💬 Codexへのお願い
> 生成時は、既存の構造・コンセプトを尊重してください。  
> 新機能を追加する場合は、FUnityの教育的・ビジュアルな目的に沿うよう提案してください。  
> Unityバージョンは `2022.3+`（または `6000.0.58f2`）を前提とします。
