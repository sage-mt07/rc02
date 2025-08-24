# Architecture Refresh & MappingManager Draft

## 日付
2025-07-21

## 作成者
広夢

## 背景
- 旧APIの肥大化と責務混在により保守性が低下。
- Fluent API への移行とコンポーネント再配置が進み、アーキテクチャの整理が必要となった。
- 鳴瀬作成の [key_value_flow_naruse.md](../../docs/structure/naruse/key_value_flow_naruse.md) で新構成の流れを定義。

## 主要な変更点
1. **責務分離の明確化**
   - `KsqlContextBuilder` と `QueryBuilder` を分離し、Context 生成と LINQ 解析をそれぞれ担当。
   - Messaging / Serialization レイヤーを独立させ、依存方向を一本化。
2. **MappingManager の新設**
   - POCO と KSQL・Key/Value の対応を管理する `MappingManager` を追加。
   - `Register<TEntity>()` と `ExtractKeyValue<TEntity>()` を備え、Key/Value 生成を一元化。
3. **POCO 属性廃止と Fluent API 化**
   - 旧 `TopicAttribute` などの属性を削除し、`WithTopic` や `HasKey` メソッドで設定する方式へ移行。
4. **ドキュメント整備と移行ガイド**
   - `architecture_restart.md` および `oss_migration_guide.md` を更新し、段階的移行手順を追記。
   - diff_log ディレクトリに設計差分を記録し、外部公開を見据えた履歴管理を開始。

## 経緯
- 2025 年 7 月上旬より `architecture_restart.md` を基に再設計を進行。
- 7 月 21 日時点で MappingManager の初期 API 案とクラス配置を確認。
- テストコードも Fluent API を基軸に整理され、従来の属性依存を排除。

## 新アーキテクチャ利用ストーリー
`EntitySet` から `Messaging` までの一連の流れをまとめた
[`entityset_to_messaging_story.md`](../../docs/architecture/entityset_to_messaging_story.md)
を作成。DI 設定例や DLQ 活用手順を盛り込み、実装時の指針とする。

## フォーマット案（外部公開用）
```
# Release Notes {Version}
- 概要（1〜2文）
- 主要な変更点（箇条書き）
- 移行に関する注意点
- 参考ドキュメントリンク
```
*内部向けには diff_log/ との関連付けを追記し、リファレンス化を促進する。*

