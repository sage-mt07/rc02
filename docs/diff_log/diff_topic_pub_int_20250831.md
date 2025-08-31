# Topic pub/int responsibility diff

## Summary
- Add optional `includeKey` and `partitionBy` parameters to `KsqlCreateStatementBuilder` so public topics emit key/value SerDe and partitioning while internal topics keep value-only configuration.
- `KsqlContext` now detects `.pub` topic names and supplies key info and partition column when generating CSAS/CTAS statements.
- Added tests verifying key omission for internal streams and key/value output with `PARTITION BY` for public streams.

## Testing
- `dotnet test tests/Kafka.Ksql.Linq.Tests.csproj --filter FullyQualifiedName~KsqlCreateStatementBuilderDslTests`
