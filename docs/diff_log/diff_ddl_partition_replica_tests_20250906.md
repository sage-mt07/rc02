# DDL partition/replica tests

- Added unit tests to verify that partition count and replication factor specified via POCO attributes or appsettings.json appear in generated DDL.
- Covered both entities defined directly and entities defined through ToQuery.
