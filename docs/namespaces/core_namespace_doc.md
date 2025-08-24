# Kafka.Ksql.Linq.Core Namespace 責務ドキュメント

## 概要
`Kafka.Ksql.Linq.Core` は DSL の土台となるエンティティモデル、設定、エラーハンドリングを提供するコア層です。

## Namespace構成と責務

### `Kafka.Ksql.Linq.Core.Abstractions`
- `IKsqlContext`, `IEntitySet<T>` などの基盤インターフェース
- `ErrorHandlingPolicy`, `ErrorContext` など失敗処理契約

### `Kafka.Ksql.Linq.Core.Attributes`
- `[KsqlKey]`, `[KsqlTopic]` などモデル定義用属性

### `Kafka.Ksql.Linq.Core.Configuration`
- `KafkaContextOptions`, `SchemaRegistrySection` など設定クラス

### `Kafka.Ksql.Linq.Core.Dlq`
- `DlqClient` と `DlqRecord` による Dead Letter Queue 支援

### `Kafka.Ksql.Linq.Core.Extensions`
- `CoreExtensions`, `LoggerFactoryExtensions` などの補助メソッド

### `Kafka.Ksql.Linq.Core.Modeling`
- `ModelBuilder` と `EntityModelBuilder<T>` による Fluent API モデル構築

### `Kafka.Ksql.Linq.Core.Models`
- `EntityModel`, `PropertyMeta`, `KafkaMessageContext` などのヘルパー

## 設計原則
- Fluent API による型安全なモデル設定
- 上位層への依存を禁止し純粋なドメインロジックに集中
- `ErrorAction` / `ErrorHandlingPolicy` による統一された失敗処理

