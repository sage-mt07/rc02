# Diff: ctx.TimeBucket API

- Add `ctx.TimeBucket.Get<T>(Period)` convenience to retrieve bars via ksqlDB Pull.
- Implement internal `ITimeBucketContext` binding in `KsqlContext` (ksqlDB-backed).
- Update docs to mention ctx-based retrieval alongside `TimeBucket.Get(ctx, ...)` patterns.

Notes
- Select must include a WindowStart-backed bucket column for Prev/Fill alignment.
- Periods ≥1m return live topics; 1s uses final.
