# Kafka.Ksql.Linq.Configuration namespace 責務概要

## 概要
`Kafka.Ksql.Linq.Configuration` は DSL と `appsettings.json` のバインディングを担当し、Kafka や DLQ、キャッシュなどの設定値を型安全に扱います。

## 主なコンポーネント
- **KsqlDslOptions**: DSL 全体のオプション
- **KafkaSubscriptionOptions** や **BarLimitOptions**: メッセージング関連設定
- **DlqOptions** / **TableCacheOptions**: 各機能の専用設定セクション
- **CommonSection**, **ProducerSection**, **ConsumerSection**: Kafka 接続設定

## 責務
- JSON 設定との相互マッピング
- 既定値の提供と簡易検証

