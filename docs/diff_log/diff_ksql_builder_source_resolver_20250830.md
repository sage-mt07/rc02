# diff: KsqlCreateStatementBuilder source name resolver (2025-08-30)

## Summary
- Added an overload to `KsqlCreateStatementBuilder.Build` allowing table/stream name substitution for FROM/JOIN via a user-provided resolver.

## API
- New overload:
  - `string Build(string name, KsqlQueryModel model, int? keySchemaId, int? valueSchemaId, Func<Type, string> sourceNameResolver)`
- Backward compatible: existing overload kept unchanged.

## Rationale
- Enables tests and integrations (e.g., DummyFlagSchemaRecognitionTests.DummyMessages_EnableQueries) to remap entity types to concrete ksqlDB object names during CREATE AS generation.

## Files
- Updated: `src/Query/Builders/KsqlCreateStatementBuilder.cs`
- Docs: `docs/api_reference.md`

