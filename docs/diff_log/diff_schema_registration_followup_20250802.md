# 差分履歴: schema_registration_followup

🗕 2025-08-02 (JST)
🧐 作業者: assistant

## ModelBuilder key attribute
- Scan `[KsqlKey]` attributes to populate `EntityModel.KeyProperties`

## SpecificRecordGenerator concurrency
- Cache dynamic Avro types via `Lazy<Type>` to avoid duplicate definitions when registering models in parallel

## Join builder behavior
- Removed requirement for `WHERE` clause on JOIN queries in `KsqlCreateStatementBuilder`
- Updated DSL tests to assert join SQL generation without extra filters

## Client creation and mapping tests
- Adjusted client creation test to expect default `http://localhost:8088` when Schema Registry URL is missing
- Updated mapping namespace expectations to match sanitized type names
