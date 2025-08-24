# Error Handling Example

This sample demonstrates how **Kafka.Ksql.Linq** handles processing errors.
`Program.cs` defines an `Order` entity and shows usage of `.OnError(ErrorAction.DLQ)`
with `.WithRetry(3)`.

## Prerequisites

- .NET 8 SDK
- Docker (for Kafka and ksqlDB)

## Setup

1. Start the local Kafka stack:
   ```bash
   docker-compose up -d
   ```
2. Run the example:
   ```bash
   dotnet run --project .
   ```

## Design Document References

- [POCO構造と属性の説明](../../docs/oss_design_combined.md#3-poco属性ベースdsl設計ルール（fluent-apiの排除方針）)
- [スキーマ登録の説明](../../docs/oss_design_combined.md#4-スキーマ構築と初期化手順onmodelcreating)
- [送信操作](../../docs/oss_design_combined.md#5-プロデュース操作)
- [エラーハンドリング](../../docs/oss_design_combined.md#6-コンシューム操作、（リトライ、エラー、dlq、commitの誤解）)
- [ログ設定](../../docs/oss_design_combined.md#8ロギングとクエリ可視化)
