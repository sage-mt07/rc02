# diff_query_final_live_tests_20250910

## Summary
- tests under Query/Builders updated to expect live tables instead of final for windows >1s
- ensure 1s final stream acts as source for live tables

## Testing
- `dotnet test tests/Kafka.Ksql.Linq.Tests.csproj`
