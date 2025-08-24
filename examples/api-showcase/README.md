# API Showcase Example

This program exercises multiple DSL features including `Where`, `GroupBy`,
`Select`, `OnError`, and `WithRetry`. Records with `Category` "A" are
aggregated and the count printed.

See the API reference in [api_reference.md](../../docs/api_reference.md) for
details on each method.

## Prerequisites
- .NET 8 SDK
- Docker

## Run Steps
1. Start the containers:
   ```bash
   docker-compose up -d
   ```
2. Execute the sample:
   ```bash
   dotnet run --project .
   ```

Enable debug logging to inspect the generated ksql queries.
