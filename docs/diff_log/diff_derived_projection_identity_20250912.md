# diff: derived projection changed to identity (SELECT *)

- Date: 2025-09-12
- Author: assistant

## Summary
- The derived tumbling pipeline projection for `Prev1m` and `Final1sStream` no longer hardcodes `Open/High/Low/KsqlTimeFrameClose`.
- It now uses identity projection (`SELECT *`) to avoid app-specific column name assumptions.

## Rationale
- Column shapes are application-specific; framework code should not fix names.
- Identity projection keeps derived streams/tables aligned with the app-defined schema.

## Code Changes
- File: `src/Query/Analysis/DerivedTumblingPipeline.cs`
  - `BuildInputProjection(Type)` updated to return `x => x`.

## Docs Updated
- `docs/api_reference.md`: clarified `[KsqlTimeFrameClose]` is optional and not required by derived stages.
- `docs/advanced_rules.md`: example uses `Close` column; note added that derived stages use `SELECT *`.

## Migration Notes
- If you relied on auto-copy of `Open/High/Low/KsqlTimeFrameClose`, ensure your base view defines the needed columns. Derived stages will carry them via `SELECT *`.
