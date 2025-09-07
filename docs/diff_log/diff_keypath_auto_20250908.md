# DSL Key Path Auto Selection
- KsqlCreateStatementBuilder now chooses key path style based on source type attributes.
- Streams render key columns as regular names; tables render `KEY->` syntax.
- RenderOptions is an internal test/back-compat knob; Dot style is override-only and never chosen automatically.
- Join/WHERE/HAVING clauses also auto-apply key styles with guards to avoid double or quoted replacements.
- Key projections use column aliases directly; `AS_VALUE` is not emitted for key columns.
