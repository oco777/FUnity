# FUnity Modes

FUnity では制作者の目的に合わせて 2 種類の制作モードを提供しています。

- **Scratch モード** — Scratch と同じ 480x360 ピクセルのステージと標準ブロック互換を目指すモードです。SB3 インポートにも対応予定です。
- **unityroom モード** — 16:9 (推奨 960x540) のステージを用いて unityroom への WebGL 公開を最適化するモードです。Unity 2D 拡張機能を積極的に活用できます。

切り替えは Unity エディタ上部メニューの `FUnity / Authoring / Switch Mode…` から行ってください。
選択結果は `Assets/FUnity/Resources/FUnityActiveMode.asset` に保存され、ランタイム初期化時に読み込まれます。
