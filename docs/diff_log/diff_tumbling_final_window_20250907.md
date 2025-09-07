## Tumbling final window emits
- Final tables now use `WINDOW TUMBLING` with `EMIT FINAL`
- Derivation planner links finals to base 1m tables instead of compose
- Tests updated for new final semantics
- Final は常に TUMBLING + EMIT FINAL、AS_VALUE は使わずキーは自動 Arrow、WINDOWSTART は Final/Lateでも擬似列として使用
- Final queries read directly from source tables without `COMPOSE()` wrappers
- Added regression ensuring mixed stream-table finals render Arrow only for table keys and guard against COMPOSE
