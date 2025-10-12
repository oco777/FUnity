# Coding Style Guide

## Coding Style (.editorconfig)
- このリポジトリでは `.editorconfig` により C# の命名・スタイルを統一しています。
- private/protected フィールドは `m_` + PascalCase（例：m_UIDocument, m_Ticker）。
- public メンバ（クラス/メソッド/プロパティ/イベント/フィールド）は PascalCase。
- 定数と static readonly は PascalCase（Unity 慣習に合わせ ALL_CAPS は非推奨）。
- 既存のシリアライズ名を変更する場合は `[FormerlySerializedAs("旧名")]` を必ず付与し、
  Editor 拡張の `SerializedObject.FindProperty("旧名")` なども新名へ追随してください。
