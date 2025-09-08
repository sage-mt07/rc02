# BuildQao time key and based-on propagation

- `BuildQao` derives `TimeKey` from the entity's stored timestamp column with `[KsqlTimestamp]` fallback to model.
- `Projection` now lists non-key columns for aggregation outputs.
- `BasedOnSpec` populated with schedule boundaries (open/close/day key) from `KsqlQueryModel`.
