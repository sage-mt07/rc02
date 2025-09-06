# Insert statement emit changes (2025-09-06)
- always append `EMIT CHANGES` in `KsqlInsertStatementBuilder`
- add regression test ensuring emit is always included
