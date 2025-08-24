# Hello World Example

This sample demonstrates the minimal workflow of **Kafka.Ksql.Linq**.
`Program.cs` contains all logic: it defines a simple POCO entity,
registers it in a context, sends one message with `AddAsync`, waits until the
stream is ready using `WaitForEntityReadyAsync`, and then consumes it with
`ForEachAsync`.

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
- [受信操作](../../docs/oss_design_combined.md#6-コンシューム操作、（リトライ、エラー、dlq、commitの誤解）)
- [ログ設定](../../docs/oss_design_combined.md#8ロギングとクエリ可視化)
