# Key reference policy update
- DSL now writes key columns using plain column names.
- `KsqlCreateStatementBuilder` adds `RenderOptions.KeyPathStyle` to render keys as inline, `key.` or `KEY->`.
- Default `KeyPathStyle.None` outputs inline key columns, while `Arrow` generates `KEY->col` for table compatibility.
