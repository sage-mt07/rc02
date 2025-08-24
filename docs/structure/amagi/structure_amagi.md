# 天城視点 全体構造統括

この資料は [shared/structure_base.md](../shared/structure_base.md) の内容をもとに、PMである天城が全体の依存関係と優先度を整理したものです。

## 依存関係マップ

```
Application
  └─ Core
       ├─ Messaging
       │    └─ Serialization
       ├─ Cache
       └─ Window
```

## 責任階層

1. **Application** : OSS外部との接点を持つ最上位レイヤー。
2. **Core** : ドメインモデルと設定を管理。
3. **Messaging/Serialization/Cache/Window** : 技術要素ごとの下位モジュール群。
4. **Context** : これらを束ねる実行環境。

## 優先度

- 機能拡張時は `Core` と `Messaging` を最優先で整備
- パフォーマンス課題が出た場合は `Cache` と `Window` を重点的に調査
- 設計変更は常に `docs/diff_log` へ記録し、各担当へフィードバック
