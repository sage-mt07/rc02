## Tumbling final window emits
- Final tables now use `WINDOW TUMBLING` with `EMIT FINAL`
- Derivation planner links finals to base 1m tables instead of compose
- Tests updated for new final semantics
