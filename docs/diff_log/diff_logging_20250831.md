# diff: logging_config_ddl_20250831

- Scope: Add generalized Information-level logging for Kafka config (Producer/Consumer/SchemaRegistry) and ksqlDB DDL statements to aid integration test diagnostics.
- Motivation: Investigate JoinIntegrationTests (TwoTableJoin_Query_ShouldBeValid) by capturing effective client configs and emitted DDL.

## Changes
- Added `ConfigLoggingExtensions` to flatten config objects and mask sensitive keys.
- Wired logging into:
  - Producer: `BuildProducerConfig`, Schema Registry client creation
  - Consumer: `BuildConsumerConfig`, Schema Registry client creation
  - ksqlDB: Log generated DDL in `KsqlContext` for query-defined and simple entities

## Security
- Auto-masks keys containing: password, secret, token, apikey.

## Impact
- No behavioral changes; log-only.
- Helps confirm effective settings and the exact DDL submitted during tests.

