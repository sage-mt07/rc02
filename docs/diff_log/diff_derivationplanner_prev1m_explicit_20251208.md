# diff: derivation planner Prev1m explicit (2025-12-08)
- Remove automatic generation of Prev1m and hb_1m when 1m window is absent.
- Emit Prev1m/hb_1m only when 1m timeframe is explicitly requested.
- Update DerivationPlannerTests to expect Prev1m only with explicit 1m window.
