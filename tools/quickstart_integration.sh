#!/bin/bash
# Simplified setup and integration test flow
# Note: Drop/reset logic is for local tests only. Avoid in production.
set -e

# Ensure dependencies
if ! command -v dotnet >/dev/null; then
  echo ".NET SDK is required" >&2
  exit 1
fi

# Restore packages
 dotnet restore

# Start Kafka/ksqlDB environment
 docker-compose -f tools/docker-compose.kafka.yml up -d

# Run sample to verify basic connectivity
 pushd examples/hello-world >/dev/null
 dotnet run
 popd >/dev/null

# Execute integration tests
 dotnet test physicalTests/Kafka.Ksql.Linq.Tests.Integration.csproj --filter Category=Integration
