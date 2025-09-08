# diff: Prev1m role unification (2025-09-08)
- Removed special-case Prev1m table DDL and schema projection.
- Derivation planner no longer emits Prev1m entities; only Hb remains for 1m windows.
- Updated tests accordingly.
