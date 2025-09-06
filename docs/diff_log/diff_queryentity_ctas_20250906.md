# Query entity CTAS for tables
- Generate `CREATE TABLE AS SELECT` when ToQuery defines a table entity.
- Streams still use `INSERT INTO ... SELECT` after initial creation.
