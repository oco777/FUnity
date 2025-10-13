# 俳優 UI テンプレート

## 目次
- [要点](#要点)
- [手順](#手順)
- [補足](#補足)
- [参考](#参考)

## 要点
- UXML は `name="root"` と `name="portrait"` を持つ A 案テンプレートを使用する。
- `CreateActorElement()` が `root` サイズと `portrait` 画像を自動調整する。
- USS は Theme から参照し、FUnity 配下で一元管理する。

## 手順
### 1. UXML テンプレートの構成
```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">
  <ui:VisualElement name="root" class="fooniportrait__root">
    <ui:VisualElement name="portrait" class="fooniportrait__image" />
  </ui:VisualElement>
</ui:UXML>
```
- `name="root"` はレイアウトの基準サイズとして扱う。
- `name="portrait"` は Texture2D をバインドするターゲットとなる。

### 2. USS の例
```css
.fooniportrait__root {
  width: 320px;
  height: 320px;
  align-items: center;
  justify-content: center;
}

.fooniportrait__image {
  width: 280px;
  height: 280px;
  background-size: contain;
}
```
- USS は `Assets/FUnity/USS/` に配置し、Theme へインポートする。
- 背景サイズを `contain` に設定し、画像の縦横比を維持する。

### 3. 画像の準備
- Portrait 用 PNG は `Assets/FUnity/Art/Characters/` に配置する。
- 推奨ファイル名は `Fooni.png` など ActorData 名と一致させる。
- 透明背景の 512px 以上の画像を使用すると UI 表示が安定する。

## 補足
- 追加フィールドを作成する場合も `name` 属性を用いて参照可能にする。
- USS を差し替える際は `UnityDefaultRuntimeTheme.uss` に `@import` を追加する。
- Legacy の `Assets/Legacy/` 配下を参照している場合は削除し、FUnity 配下に移動する。

## 参考
- [既定データの構成](data-defaults.md)
- [UI テーマ適用戦略](ui-theme.md)
- [トラブルシュート集](troubleshooting.md)
