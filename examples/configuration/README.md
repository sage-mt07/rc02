# Configuration（設定サンプル）

目的: appsettings.json と Builder 設定の最小構成を把握する。

## これでできること
- Kafka/Schema Registry/ksqlDB の接続設定
- Topic/Consumer/Producer の基本プロパティ
- DSL/Builder でのオプション適用

## 前提
- .NET 8 SDK
- ローカルの Kafka/Schema Registry/ksqlDB（`docker-compose -f tools/docker-compose.kafka.yml up -d`）

## 実行
```
cd examples/configuration
# 必要に応じて appsettings.json を編集
# dotnet run など、各プロジェクト手順に従ってください
```

## 関連サンプル
- `examples/configuration-mapping`：マッピング設定の拡張
- `examples/schema-attributes`：`[KsqlKey]` / `[KsqlDecimal]` / `[KsqlTimestamp]`

## 参考
- 関数/型対応表：`docs/ksql-function-type-mapping.md`
- SQLServer→ksqlDB ガイド：`docs/sqlserver-to-kafka-guide.md`
