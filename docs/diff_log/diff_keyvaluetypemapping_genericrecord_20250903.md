## Summary
- support GenericRecord for key mapping and formatting in KeyValueTypeMapping

## Testing
- `dotnet test tests/Kafka.Ksql.Linq.Tests.csproj --filter Mapping`
- `dotnet test tests/Kafka.Ksql.Linq.Tests.csproj` (fails: 17 tests)
