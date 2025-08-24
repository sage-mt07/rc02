# Configuration Example

This sample demonstrates how to switch logging output between development and production environments using **Kafka.Ksql.Linq**.
The default `appsettings.json` is tuned for debugging with `LogLevel:Debug`.
`Program.cs` is copied from the Hello World example. `appsettings.Development.json` enables verbose logging for development, while
`appsettings.Production.json` suppresses most output for a quieter runtime. Replace the file referenced in `Program.cs` as needed before running.
`docker-compose.yml` provides the required Kafka and ksqlDB services.

## Prerequisites

- .NET 8 SDK
- Docker (for Kafka and ksqlDB)

## Setup

1. Start the local Kafka stack:
   ```bash
   docker-compose up -d
   ```
2. Run your application with the desired configuration, for example:
   ```bash
   dotnet run --no-build --environment Development
   ```

## Design Document Reference

- [ロギングとクエリ可視化](../../docs/oss_design_combined.md#8ロギングとクエリ可視化)

This sample corresponds to [docs_configuration_reference.md](../../docs/docs_configuration_reference.md).
