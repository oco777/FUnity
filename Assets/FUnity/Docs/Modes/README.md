# FUnity Modes

FUnity では制作者の目的に合わせて 2 種類の制作モードを提供しています。

- **ブロックモード (Block Mode)** — Scratch 風の 480x360 ピクセルステージでブロックプログラミングを行うモードです。SB3 インポートにも対応予定です。
- **unityroom モード** — 16:9 (推奨 960x540) のステージを用いて unityroom への WebGL 公開を最適化するモードです。Unity 2D 拡張機能を積極的に活用できます。

切り替えは `FUnityProjectData` アセットの Inspector で行ってください。
`FUnityProjectData` に登録された ModeConfig がゲーム起動時に自動適用されます。
