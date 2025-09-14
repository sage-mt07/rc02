# Manual Commit Example

This sample demonstrates manual acknowledgement using **Kafka.Ksql.Linq**.
Manual commit is selected at runtime by passing `autoCommit: false` to `ForEachAsync`.
During consumption each record is passed to the delegate as the POCO instance.
Call `context.Orders.Commit(entity)` after successful processing to record the offset.

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

- [Manual commit operation](../../docs/manual_commit.md)
- [POCO attribute design](../../docs/oss_design_combined.md#3-poco-attribute-based-dsl-design-rules-fluent-api-elimination-policy)
- [Schema initialization](../../docs/oss_design_combined.md#4-schema-building-and-initialization-procedures-onmodelcreating)

See the manual commit API in [api_reference.md](../../docs/api_reference.md).
