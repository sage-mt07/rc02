# Diff: DerivationPlanner 1s hub

- DerivationPlanner injects a 1s timeframe and creates `*_1s_final` and `*_1s_final_s` hub entities.
- All live/final/prev/fill windows derive from `<base>_1s_final_s` instead of chaining.
- Added roles `Final1s` and `Final1sStream` with corresponding DDL generation.
