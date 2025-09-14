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

- [POCO structure and attributes](../../docs/oss_design_combined.md#3-poco-attribute-based-dsl-design-rules-fluent-api-elimination-policy)
- [Schema registration](../../docs/oss_design_combined.md#4-schema-building-and-initialization-procedures-onmodelcreating)
- [Produce operations](../../docs/oss_design_combined.md#5-produce-operations)
- [Consume operations](../../docs/oss_design_combined.md#6-consume-operations-retry-error-dlq-commit-misconceptions)
- [Logging and query visibility](../../docs/oss_design_combined.md#8-logging-and-query-visibility)
