# Kafka.Ksql.Linq 構造共通ベース

この構造は、AIと人間の協働を支えるために定義されたものです。各AIエージェントがこの構造をもとに、自身の視点で構造を記述・維持します。

## 目次
1. [全体概要](#全体概要)
2. [構造一覧表](#構造一覧表)
3. [参考タグ定義](#参考タグ定義)

## 全体概要
本ドキュメントは、OSS 全体の構造を一覧化し、各 namespace の責務とレイヤーを参照できるようにすることを目的としています。AI チームが共通の前提として利用することで、議論や設計を容易にします。

### 構造分割思想
- **責務分離**: 各 namespace は特定の役割のみに集中し、依存方向は上位から下位へ限定します。
- **参照方向**: Application → Core → Messaging → Cache → External の流れを基本とします。
- **PM統括**: 天城がレイヤー間の優先度と整合性を統括し、各エージェントの作業を調整します。

## 構造一覧表

| Namespace | 所属レイヤー | 主なクラス | 責務 | 関連エージェント |
|-----------|--------------|------------|------|-----------------|
| `Kafka.Ksql.Linq.Query` | Query | `QueryBuilder`, `KsqlFunctionRegistry` | LINQ式からKSQLへの変換 | 鳴瀬・鏡花 |
| `Kafka.Ksql.Linq.Core` | Core | `KsqlEntity`, `EntityModelBuilder` | エンティティ管理・Fluent API 定義 | 鳴瀬・鏡花 |
| `Kafka.Ksql.Linq.Messaging` | Messaging | `KafkaProducer`, `KafkaConsumer` | 型安全なProducer/Consumer抽象 | 鳴瀬・詩音 |
| `Kafka.Ksql.Linq.Cache` | Cache | `StreamizCache`, `CacheManager` | ストリーム状態の永続化 | 鳴瀬・詩音 |
| `Kafka.Ksql.Linq.Application` | Application | `KsqlContextBuilder`, `KsqlContextOptions` | コンテキスト構築・統合設定 | 鳴瀬・天城 |
| `Kafka.Ksql.Linq.Configuration` | Configuration | `ProducerSection`, `ConsumerSection` | Kafka設定オブジェクト管理 | 鳴瀬 |
| `Kafka.Ksql.Linq.Context` | Context | `KsqlContext`, `KsqlModelBuilder` | DSL解析とモデル構築 | 鳴瀬 |

※ 一部クラス名や責務は今後変更の可能性があります。`// TBD` は確定待ち項目です。

## 参考タグ定義
タグはドキュメント内で以下のように記述します。

- `@layer`: レイヤー名を示す
- `@ns`: namespace 名
- `@agent`: 関連 AI (複数可)
- `@usage`: 参照用途 (ビルド時、送信時、評価時など)

