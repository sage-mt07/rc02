#!/usr/bin/env bash
set -euo pipefail

REPO=${REPO:-/mnt/c/dev/rc02}
export COMPOSE_FILE="${COMPOSE_FILE:-$REPO/physicalTests/docker-compose.yaml}"

# Patterns to rerun (class name fragments)
patterns=(
  "FullyQualifiedName~InvalidQueryTests"
  "FullyQualifiedName~KsqlSyntaxTests"
  "FullyQualifiedName~AdvancedDataTypeTests"
  "FullyQualifiedName~CompositeKeyPocoTests"
  "FullyQualifiedName~DefaultAndBoundaryValueTests"
  "FullyQualifiedName~DlqIntegrationTests"
  "FullyQualifiedName~DummyFlagMessageTests"
  "FullyQualifiedName~DummyFlagSchemaRecognitionTests"
  "FullyQualifiedName~JoinIntegrationTests"
  "FullyQualifiedName~ManualCommitIntegrationTests"
  "FullyQualifiedName~NoKeyPocoTests"
)

for pat in "${patterns[@]}"; do
  echo "[Group] $pat"
  INFER_DOWNS=false PREPULL=false PRUNE_AFTER_EACH=true "$REPO/tools/run_physical_tests_per_test.sh" "$pat" >/dev/null 2>&1 || true
  last=$(ls -1 "$REPO/Reportsx/physical" | grep -E '^[0-9]{8}-[0-9]{6}$' | sort | tail -n1 || true)
  if [ -n "$last" ] && [ -f "$REPO/Reportsx/physical/$last/summary.csv" ]; then
    sumfile="$REPO/Reportsx/physical/$last/summary.csv"
    # Count results
    passed=$(awk -F, 'NR>1 && $3=="Passed" {c++} END{print c+0}' "$sumfile")
    failed=$(awk -F, 'NR>1 && $3=="Failed" {c++} END{print c+0}' "$sumfile")
    total=$(awk -F, 'NR>1 {c++} END{print c+0}' "$sumfile")
    echo "  -> Passed=$passed Failed=$failed Total=$total dir=Reportsx/physical/$last"
  else
    echo "  -> No summary"
  fi
done

