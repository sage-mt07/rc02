# SQL Server vs Kafka Example

This sample mirrors a simple insert/select workflow familiar to SQL Server
developers using the Kafka DSL. `SqlOrder` is written to a topic and then
consumed back, illustrating the similarity between traditional SQL operations
and the LINQ-based streaming approach.

See [sqlserver-to-kafka-guide.md](../../docs/sqlserver-to-kafka-guide.md) for the
related migration guide.

## Prerequisites
- .NET 8 SDK
- Docker

## Run Steps
1. Launch Kafka and ksqlDB:
   ```bash
   docker-compose up -d
   ```
2. Execute the sample:
   ```bash
   dotnet run --project .
   ```

Enable debug logging to view the translated queries.
