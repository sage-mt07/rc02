# Diff: DerivationPlanner live input null

- 1m live windows omit `InputHint` so `bar_1m_live` reads the base source.
- Fallback-generated 1m live window also sets `InputHint` to null.

