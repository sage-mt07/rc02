# diff: Prev1m static table DDL (2025-09-08)
- Role.Prev1m generates simple `CREATE TABLE` instead of windowed DDL.
- Prev1m entity schema reduced to a single value column named `KsqlTimeFrameClose`.
- EntityModelAdapter maps Prev1m projection to the configured close column.
- Tests verify static DDL and single-column projection.
- Close column is derived from the `[KsqlTimeFrameClose]` attribute on the entity.
