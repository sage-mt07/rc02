# diff: remove KsqlTimeFrameClose attribute from public docs

- Date: 2025-09-12
- Author: assistant

## Summary
- `[KsqlTimeFrameClose]` is removed from `docs/api_reference.md`.

## Rationale
- Functionally unnecessary: Close boundary should be defined via TimeFrame predicate and app-level projection.
- Derived stages (Prev/1s_final_s) now use identity projection (`SELECT *`), not fixed column names.
- Avoids implying a required attribute for end-users.

## Impact
- No runtime impact; code still tolerates the attribute but does not require it.
- Users should define their Close column explicitly in projections if needed (e.g., `Close = g.LatestByOffset(...)`).

## Related changes
- `docs/advanced_rules.md` updated to use `Close` column and clarified identity projection.
- `docs/diff_log/diff_derived_projection_identity_20250912.md` documents the identity projection change.
