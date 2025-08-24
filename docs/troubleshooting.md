# Troubleshooting Guide

This document lists common checks when the quick start or integration tests fail.

## Verify Services
- `docker ps` should show `zookeeper`, `kafka`, and `ksqldb-server` containers running.
- If any container exited, restart all services:
  ```bash
  docker-compose -f tools/docker-compose.kafka.yml down
  docker-compose -f tools/docker-compose.kafka.yml up -d
  ```

## Schema Registry
- Confirm registered subjects:
  ```bash
  curl http://localhost:8081/subjects
  ```
- On `NAME_MISMATCH` errors, delete the subject and rerun tests:
  ```bash
  curl -X DELETE http://localhost:8081/subjects/<subject>
  ```

## Logs
- Check `logs/` in the repository for test output.
- Container logs can be viewed with `docker logs <container>`.

## Production Note
- The integration tests call `ResetAsync()` which drops topics and schemas every run. Avoid this approach in production environments to prevent unintended data loss.
