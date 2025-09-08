# Diff: DerivationPlanner prev window removal

- Removed chaining of live inputs; all windows now consume base 1m live topics (weekly uses 1d live).
- ExpressionAnalysisResult metadata reflects the same 1m-based live inputs.
- Planner always expands a 1m heartbeat and prev1m entity even if 1m windows aren't specified.
