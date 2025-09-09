# Diff: DerivationPlanner final input cleanup

- 1m final now derives input via `prevFinalId ?? baseId`, removing intermediate aggregation tables.
- `InputHint` remains immutable, preventing accidental overrides.
