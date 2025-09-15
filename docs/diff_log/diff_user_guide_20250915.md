# diff_user_guide_20250915

## Summary
- Rewrote user guide to describe core features, extension points, and operations/monitoring.
- Added sections for DI/logging, serializer swap, middleware pipeline, config management, metrics, schema registry, and testing guidance.
- Expanded with additional code samples for headers, manual commit, windowed aggregation, table cache filtering, and DI/validation options.

## Testing
- `dotnet test tests/Kafka.Ksql.Linq.Tests.csproj` *(fails: 16 failed, 529 passed)*
- `dotnet test tests/Kafka.Ksql.Linq.Cache.Tests/Kafka.Ksql.Linq.Cache.Tests.csproj`
