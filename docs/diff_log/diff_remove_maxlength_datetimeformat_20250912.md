# diff: remove MaxLength and KsqlDatetimeFormat from public API reference

- Date: 2025-09-12
- Author: assistant

## Summary
- Removed `[MaxLength]` and `[KsqlDatetimeFormat]` from `docs/api_reference.md`.
- Deleted unused attribute classes from code: `src/Core/Attributes/MaxLengthAttribute.cs`, `src/Core/Attributes/KsqlDatetimeFormatAttribute.cs`.

## Rationale
- Currently unused by schema/DDL builders and mapping; no effect at runtime.
- Avoids implying guarantees that framework does not enforce.

## Impact
- Code now excludes these attributes. No remaining references; build unaffected.
- If future need arises, reintroduce with concrete behavior and tests.

## Follow-ups
- Optionally deprecate or remove the attribute classes if confirmed unnecessary across repos.
