# diff_deadcode_cleanup_20250911

## Summary
- remove unused internal tracking property from GroupByClauseBuilder
- delete commented-out cache helper methods

## Testing
- `dotnet build src/Kafka.Ksql.Linq.csproj`
- `dotnet test tests/Kafka.Ksql.Linq.Tests.csproj`
