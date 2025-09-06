# diff_queryentity_create_insert_20250907

- Execute CREATE TABLE for query entities and follow with `INSERT INTO ... EMIT CHANGES` when defined via `ToQuery`.
- Update `EnsureQueryEntityDdlAsync` to run CREATE first and conditional INSERT.
- Adjust unit test to expect separate CREATE and INSERT statements.
