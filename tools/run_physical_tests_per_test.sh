#!/usr/bin/env bash
set -euo pipefail

REPO="/mnt/c/dev/rc02"
PROJECT="$REPO/physicalTests/Kafka.Ksql.Linq.Tests.Integration.csproj"
# Compose file selection policy
# - Default: physicalTests/docker-compose.yaml (validated combination)
# - Override only when COMPOSE_FILE is explicitly provided via environment
if [ -n "${COMPOSE_FILE:-}" ]; then
  COMPOSE_FILE="$COMPOSE_FILE"
else
  COMPOSE_FILE="$REPO/physicalTests/docker-compose.yaml"
fi
PRUNE_AFTER_EACH="${PRUNE_AFTER_EACH:-false}"
FILTER_EXPR="${1:-}"
DOWN_STYLE="${DOWN_STYLE:-mid}"     # 'mid' (stop during test) or 'pre' (stop before test)
DOWN_AFTER="${DOWN_AFTER:-5}"       # seconds to wait before stopping service when DOWN_STYLE=mid
INFER_DOWNS="${INFER_DOWNS:-false}" # if true, infer service-down from test name
PREPULL="${PREPULL:-true}"          # if true, run one-time docker compose pull at start
UP_PULL_POLICY="${UP_PULL_POLICY:-never}" # compose up pull policy: never|missing|always

TS="$(date -u +%Y%m%d-%H%M%S)"
ROOT_RUN_DIR="$REPO/Reportsx/physical/$TS"
mkdir -p "$ROOT_RUN_DIR"

# Pre-pull images once to avoid pulling per test
if [ "$PREPULL" = "true" ]; then
  (docker compose -f "$COMPOSE_FILE" pull) >"$ROOT_RUN_DIR/prepull.log" 2>&1 || true
fi

wait_for_service() {
  # $1: service name, $2: state token (e.g., '(healthy)' or 'Exit')
  local svc="$1"; local token="$2"; local deadline=$((SECONDS+150))
  while (( SECONDS < deadline )); do
    if docker compose -f "$COMPOSE_FILE" ps | grep -E "${svc}.*${token}" >/dev/null 2>&1; then
      return 0
    fi
    sleep 2
  done
  return 1
}

decide_scenario() {
  # Infer infra scenario from test name
  # Echo one of: all_up | kafka_down | ksqldb_down | schema_registry_down
  local name="$1"
  if echo "$name" | grep -qiE "KafkaDown|KafkaIsDown"; then echo kafka_down; return; fi
  if echo "$name" | grep -qiE "KsqlDbDown|KsqlDbIsDown"; then echo ksqldb_down; return; fi
  if echo "$name" | grep -qiE "SchemaRegistryDown|SchemaRegistryIsDown"; then echo schema_registry_down; return; fi
  echo all_up
}

start_infra() {
  # $1: scenario
  local scenario="$1"
  (docker compose -f "$COMPOSE_FILE" up --pull "$UP_PULL_POLICY" -d) >"$RUN_DIR/compose_up.out" 2>&1
  (docker compose -f "$COMPOSE_FILE" ps -a) >"$RUN_DIR/compose_ps.out" 2>&1
  (docker compose -f "$COMPOSE_FILE" logs --no-color) >"$RUN_DIR/compose_up.log" 2>&1 || true

  case "$scenario" in
    all_up)
      wait_for_service kafka "(healthy)" || true
      ;;
    kafka_down)
      if [ "$DOWN_STYLE" = "pre" ]; then
        docker compose -f "$COMPOSE_FILE" stop kafka >/dev/null 2>&1 || true
        wait_for_service zookeeper "Up" || true
      else
        wait_for_service kafka "(healthy)" || true
        ( sleep "$DOWN_AFTER"; docker compose -f "$COMPOSE_FILE" stop kafka >/dev/null 2>&1 || true ) & echo $! >"$RUN_DIR/injector.pid"
      fi
      ;;
    ksqldb_down)
      if [ "$DOWN_STYLE" = "pre" ]; then
        wait_for_service kafka "(healthy)" || true
        docker compose -f "$COMPOSE_FILE" stop ksqldb-server >/dev/null 2>&1 || true
      else
        wait_for_service kafka "(healthy)" || true
        ( sleep "$DOWN_AFTER"; docker compose -f "$COMPOSE_FILE" stop ksqldb-server >/dev/null 2>&1 || true ) & echo $! >"$RUN_DIR/injector.pid"
      fi
      ;;
    schema_registry_down)
      if [ "$DOWN_STYLE" = "pre" ]; then
        wait_for_service kafka "(healthy)" || true
        docker compose -f "$COMPOSE_FILE" stop schema-registry >/dev/null 2>&1 || true
      else
        wait_for_service kafka "(healthy)" || true
        ( sleep "$DOWN_AFTER"; docker compose -f "$COMPOSE_FILE" stop schema-registry >/dev/null 2>&1 || true ) & echo $! >"$RUN_DIR/injector.pid"
      fi
      ;;
  esac
  # refresh ps after potential stops
  (docker compose -f "$COMPOSE_FILE" ps -a) >"$RUN_DIR/compose_ps_after.out" 2>&1
}

# Collect tests
LIST_TMP="$(mktemp)"
if [ -n "$FILTER_EXPR" ]; then
  dotnet test "$PROJECT" --list-tests --filter "$FILTER_EXPR" --verbosity quiet >"$LIST_TMP"
else
  dotnet test "$PROJECT" --list-tests --verbosity quiet >"$LIST_TMP"
fi

# Heuristic: fully-qualified names typically start with Kafka. for this repo
mapfile -t TESTS < <(sed 's/^\s\+//' "$LIST_TMP" | grep -E '^Kafka\.' | sed 's/\r$//' | sed 's/[[:space:]]*$//')
rm -f "$LIST_TMP"

if [ "${#TESTS[@]}" -eq 0 ]; then
  echo "No tests found. You can pass a filter: FullyQualifiedName~<pattern>" >&2
  exit 1
fi

SUMMARY="$ROOT_RUN_DIR/summary.csv"
echo "index,test_name,result,duration_sec" >"$SUMMARY"

idx=0
for t in "${TESTS[@]}"; do
  idx=$((idx+1))
  safe="$(echo "$t" | tr ' /:\\|*?"<>' '_' | cut -c1-120)"
  RUN_DIR="$ROOT_RUN_DIR/$(printf "%03d" "$idx")_${safe}"
  mkdir -p "$RUN_DIR"

  echo "[START] $t"
  SECONDS=0

  # Down any leftovers, then start fresh
  (docker compose -f "$COMPOSE_FILE" down -v --remove-orphans || true) >"$RUN_DIR/compose_down_pre.log" 2>&1 || true
  scenario="all_up"
  if [ "$INFER_DOWNS" = "true" ]; then
    scenario="$(decide_scenario "$t")"
  fi
  echo "[infra] scenario=$scenario (INFER_DOWNS=$INFER_DOWNS, DOWN_STYLE=$DOWN_STYLE, DOWN_AFTER=$DOWN_AFTER)" | tee -a "$RUN_DIR/infra.txt"
  start_infra "$scenario"

  set +e
  dotnet test "$PROJECT" --filter "FullyQualifiedName=$t" -v minimal >"$RUN_DIR/dotnet_test.log" 2>&1
  CODE=$?
  set -e

  (docker compose -f "$COMPOSE_FILE" logs --no-color) >"$RUN_DIR/compose_logs_final.log" 2>&1 || true
  (docker compose -f "$COMPOSE_FILE" down -v --remove-orphans || true) >"$RUN_DIR/compose_down.log" 2>&1 || true

  if [ "$PRUNE_AFTER_EACH" = "true" ]; then
    (docker network prune -f || true) >>"$RUN_DIR/prune.log" 2>&1
    (docker volume prune -f || true) >>"$RUN_DIR/prune.log" 2>&1
  fi

  RESULT="Passed"
  if [ $CODE -ne 0 ]; then RESULT="Failed"; fi
  DUR=$SECONDS
  echo "$(printf "%03d" "$idx"),\"$t\",$RESULT,$DUR" >>"$SUMMARY"
  echo "[DONE ] $t -> $RESULT (${DUR}s)"
done

# Write short report
DT="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
{
  echo "# Physical Test Report"
  echo "- Datetime UTC: $DT"
  echo "- Mode: per-test compose up/down"
  echo
  echo "## Summary"
  echo "Saved to Reportsx/physical/$(basename "$ROOT_RUN_DIR")/summary.csv"
} >"$ROOT_RUN_DIR/report.md"

# Update index
printf "%s [physical] per-test run finished. See Reportsx/physical/%s/\n" "$(date -u +%Y-%m-%d\ %H:%M:%S)" "$(basename "$ROOT_RUN_DIR")" >> "$REPO/Reportsx/index.md"

echo "ROOT_RUN_DIR=$ROOT_RUN_DIR"
echo "DONE"
