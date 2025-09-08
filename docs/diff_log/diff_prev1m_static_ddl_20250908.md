# diff: Prev1m static table DDL (2025-09-08)
- Role.Prev1m generates simple `CREATE TABLE` instead of windowed DDL.
- Prev1m entity schema reduced to `Close` column only.
- EntityModelAdapter maps Prev1m projection to `Close`.
- Tests verify static DDL and `Close`-only projection.
