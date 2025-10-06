# FUnity

FUnity は Scratch に着想を得た Unity プロジェクト向けビジュアルプログラミングツールキットです。このパッケージは Unity Package Manager (UPM) 形式で提供されており、教育者やクリエイターが Unity 6000.0.58f2 以降を対象とする任意のプロジェクトにワークスペースを簡単に追加できます。

## 特長
- 子ども向けに最適化されたノードベースのワークスペース
- UI Toolkit を利用したブロックパレットとステージプレビュー
- 拡張可能なブロック定義と実行ランタイム
- 最小構成を示すサンプルシーン

## 必要条件
- Unity 6000.0.58f2 以降
- UI Toolkit パッケージ (`com.unity.ui`)

## インストール手順
1. Unity プロジェクトを開きます。
2. **Window > Package Manager** を開きます。
3. `+` ボタンをクリックし、**Add package from disk...** を選択します。
4. `Packages/com.papacoder.funity` 内の `package.json` を選択します。

## はじめに
1. **Package Manager > FUnity > Samples > Basic Scratch-Style Workspace** からサンプルをインポートします。
2. `BasicScratchScene` サンプルシーンを開きます。
3. 再生ボタンを押してスターターブロック付きのワークスペースを表示します。独自のブロック挙動を構築するには `Runtime/` 内のスクリプトを参照してください。

## フォルダ概要
- `Runtime/Core`: ビジュアルプログラミングエンジンを支えるコアのランタイムデータモデルとサービス。
- `Runtime/UI`: ワークスペース、パレット、ステージを描画する UI Toolkit コンポーネント。
- `Runtime/Blocks`: 再利用可能なブロック定義と挙動。
- `Runtime/Resources`: ランタイムで読み込まれる共有 ScriptableObject とデフォルトアセット。
- `UXML` & `USS`: UI Toolkit のテンプレートとスタイル。
- `Art`: インターフェース用のアートワークとアイコン。
- `Samples~`: 実践的な使用例を示すインポート可能なサンプル。

## ライセンス
MIT ライセンスの下で配布されています。詳細は `LICENSE.md` を参照してください。
