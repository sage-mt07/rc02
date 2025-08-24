# Dummy Flag Schema Recognition Test Viewpoints

## Goal
Verify that sending dummy messages with `is_dummy=true` in Kafka headers immediately after executing `CREATE TABLE`/`CREATE STREAM` eliminates schema unresolved errors and that these messages are ignored by consumer logic.

## Test Points

1. **Dummy Message Generation**
   - Create AVRO records for each table/stream with all columns populated.
   - Attach Kafka header `is_dummy=true` when producing the message.
2. **Schema Confirmation**
   - After sending the dummy message, wait 2–5 seconds.
   - Run DML queries such as `SELECT` or `GROUP BY` to ensure the schema is recognized and no `column cannot be resolved` errors occur.
3. **Consumer Behavior**
   - Confirm the application logic identifies the `is_dummy=true` header and skips or ignores these records.
4. **Cleanup**
   - Ensure dummy records do not remain in normal data flows (filtered out by header check).

5. **Schema Name Consistency**
   - Confirm that the entity and field names used in the tests exactly match the names registered in Schema Registry. Case mismatches will cause `SchemaRegistryException` errors.

6. **Pull Query Limitations**
   - Pull queries cannot use `GROUP BY`. When grouping is required, ensure `EMIT CHANGES` is appended so that the query runs as a push query.

## Notes
- Use the docker environment in `physicalTests/docker_compose.yaml` to spin up Kafka and ksqlDB when running the integration tests.
- Refer to `features/dummy_flag_test/instruction.md` for the original instructions.
