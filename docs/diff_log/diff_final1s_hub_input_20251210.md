# Diff: final1s hub input

- DerivedTumblingPipeline ignores `input` for `*_1s_final_s` so stream derives from base topic.
- DerivationPlanner assigns `<base>_1s_final_s` as `InputHint` for all derived entities.
- KsqlCreateWindowedStatementBuilder tests expect `*_1s_final_s` as the source topic.
