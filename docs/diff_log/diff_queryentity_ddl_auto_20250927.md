# Ensure query entities are materialized
- `ApplyModelBuilderSettings` now retains query-defined models, enabling `RegisterSchemasAndMaterializeAsync` to issue DDL automatically.
- Added regression test to confirm query-based entities generate `CREATE STREAM` and `INSERT INTO` statements.
- Added regression test covering `CREATE TABLE AS SELECT` materialization.
