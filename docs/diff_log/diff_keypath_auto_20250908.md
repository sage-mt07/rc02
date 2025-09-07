# DSL Key Path Auto Selection
- KsqlCreateStatementBuilder now chooses key path style based on source type attributes.
- Streams render key columns as regular names; tables render `KEY->` syntax.
- RenderOptions remains for tests to force Dot or Arrow styles.
