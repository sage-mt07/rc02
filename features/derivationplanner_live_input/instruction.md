# DerivationPlanner live input

Weekly windows should consume daily live topics.
Live inputs are fixed to base 1m streams without chaining (weekly uses 1d live). A 1m window consumes the base topic.
Planner expands a 1m hub even when 1m windows aren't requested.
See docs/diff_log/diff_derivationplanner_prevwindow_removal_20250908.md for details.
