# OSS Test Guidelines

This project only tests queries that conform to ksqlDB specifications.

## Pull Query Restrictions

- A `SELECT` statement without `EMIT CHANGES` is treated as a pull query.
- Using `GROUP BY` in a pull query causes an error at build or runtime and suggests switching to a push query.
- When aggregation is required, use a push query (add `EMIT CHANGES`) or reference a pre-aggregated table.

## Additional Rules

- Kafka message production relies on automatic POCO schema mapping via `Chr.Avro.Confluent`.
- Tests that include `GROUP BY` target only push queries and are not generated for pull queries.
- `MIN` and `MAX` aggregations are used only in STREAM queries, not in tables.
- Ensure the `WINDOW` clause appears immediately after `GROUP BY`.
- Tests fail if the `THEN` and `ELSE` types in a `CASE` expression differ.

The test generation process automatically skips queries that violate the above rules.

### Physical Test Categories

- **Connectivity**: verify connectivity to Kafka brokers and the Schema Registry.
- **KsqlSyntax**: check that generated KSQL statements are accepted by ksqlDB.
- **OssSamples**: integration tests using sample code to validate API behavior.

## Physical and Integration Test Procedure

### Prerequisites
1. Ensure the `.NET 6 SDK` is installed.
2. After cloning the repository, run `dotnet restore`.
3. Start Kafka, ksqlDB, and the Schema Registry.
   ```bash
   docker-compose -f tools/docker-compose.kafka.yml up -d
   ```

### Run the Sample
Basic send/receive can be tried with `examples/hello-world`.
```bash
cd examples/hello-world
dotnet run
```

### Run Integration Tests
Execute the following while the environment is running:
```bash
dotnet test physicalTests/Kafka.Ksql.Linq.Tests.Integration.csproj --filter Category=Integration
```
- Required Avro schemas are automatically registered even if Kafka or the Schema Registry restarts.
- Failure or skip reasons can be checked in the `logs/` folder or via `docker logs`.

`tools/quickstart_integration.sh` performs setup and test execution in one step.
