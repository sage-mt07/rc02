# Tumbling TimeKey and Grace persistence

- Tumbling DSL records the timestamp property name in `KsqlQueryModel.TimeKey`.
- Optional grace periods set `KsqlQueryModel.GraceSeconds`.
- `BuildQao` forwards `TimeKey` and `GraceSeconds` to `TumblingQao`.

