# 差分履歴: TimeFrame schedule parse

## Capture join and boundary metadata
- `KsqlQueryable.TimeFrame` now parses the predicate to record schedule join keys and open/close boundaries with inclusivity flags.
- `KsqlQueryModel` stores these fields and `BuildQao` forwards them into `BasedOnSpec`.
- `QueryBuilderUtils.ApplyTimeFrame` uses the recorded inclusivity flags when rendering join SQL.
