# diff_physical_per_test_20250829

- Category: physical-tests infra + tests
- Date (UTC): 2025-08-29

## Summary

- Add per-test execution script for physical tests on WSL that performs docker compose up/down per test, saves logs, and optionally prunes resources.
- Fix Schema Registry integration tests by implementing ResetAsync/SetupAsync helpers to register minimal Avro schemas and clearing subjects before each test.

## Changes

- Added: `tools/run_physical_tests_per_test.sh`
  - Per-test `docker compose up/down -v --remove-orphans`
  - Logs saved under `Reportsx/physical/<UTC>/NNN_<test_name>/`
  - Optional `PRUNE_AFTER_EACH=true` to prune networks/volumes

- Updated: `physicalTests/Connectivity/SchemaRegistryResetTests.cs`
  - Implement `EnvSchemaRegistryResetTests.ResetAsync()` to delete existing subjects
  - Implement `EnvSchemaRegistryResetTests.SetupAsync()` to register minimal Avro schemas for `<table>-key/-value` and `source-value`
  - Call Reset/Setup at start of the three tests

## Rationale

- Monolithic run left residual state leading to flaky failures. Per-test isolation ensures clean environment.
- Schema Registry tests assumed pre-registered subjects; explicit setup makes tests deterministic.

## Migration/Usage

- Run: `wsl PRUNE_AFTER_EACH=true /mnt/c/dev/rc02/tools/run_physical_tests_per_test.sh` (optionally pass filter)
- Results appended to `Reportsx/index.md`; detailed logs per test in run directory.

