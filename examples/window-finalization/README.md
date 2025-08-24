# Window Finalization Example

This sample demonstrates tumbling window aggregation with finalized results.
`Program.cs` produces a `Sale` record and then queries a 1-minute window using
`Window(...).UseFinalized()`.

This example corresponds to the window processing description in
[docs_advanced_rules.md](../../docs/docs_advanced_rules.md).

## Prerequisites
- .NET 8 SDK
- Docker

## Run Steps
1. Start Kafka and ksqlDB:
   ```bash
   docker-compose up -d
   ```
2. Run the program:
   ```bash
   dotnet run --project .
   ```

Debug logging reveals the generated queries for troubleshooting.
