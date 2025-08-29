#!/usr/bin/env bash
set -euo pipefail

SUMMARY_PATH=${1:?"usage: rerun_failed_from_summary.sh <summary.csv>"}
REPO=${REPO:-/mnt/c/dev/rc02}

if [ ! -f "$SUMMARY_PATH" ]; then
  echo "summary not found: $SUMMARY_PATH" >&2
  exit 1
fi

export COMPOSE_FILE="${COMPOSE_FILE:-$REPO/physicalTests/docker-compose.yaml}"

RESULTS="$REPO/Reportsx/physical/rerun_$(date -u +%Y%m%d-%H%M%S).txt"
: > "$RESULTS"

mapfile -t FAILED < <(sed -n "/,Failed,/p" "$SUMMARY_PATH" | sed -n "s/^[^,]*,\(.*\),Failed,.*/\1/p")
if [ "${#FAILED[@]}" -eq 0 ]; then
  echo "NO_FAILED" | tee -a "$RESULTS"
  echo "$RESULTS"
  exit 0
fi

for name in "${FAILED[@]}"; do
  base_noparams="$(echo "$name" | sed 's/(.*)//')"
  class_method="$(echo "$base_noparams" | awk -F. '{ if (NF>=2) {print $(NF-1)"."$NF} else {print $0} }')"
  echo "[ReRun] $name -> $class_method" | tee -a "$RESULTS"
  INFER_DOWNS=false PREPULL=false PRUNE_AFTER_EACH=true "$REPO/tools/run_physical_tests_per_test.sh" "FullyQualifiedName~$class_method" >/dev/null 2>&1 || true
  last_dir=$(ls -1 "$REPO/Reportsx/physical" | grep -E '^[0-9]{8}-[0-9]{6}$' | sort | tail -n1 || true)
  if [ -n "$last_dir" ]; then
    run_dir="$REPO/Reportsx/physical/$last_dir"
    test_dir=$(ls -1 "$run_dir" | head -n1 || true)
    if [ -n "$test_dir" ] && [ -f "$run_dir/$test_dir/dotnet_test.log" ]; then
      if grep -q "Failed!" "$run_dir/$test_dir/dotnet_test.log"; then
        err=$(grep -n "Error Message:" "$run_dir/$test_dir/dotnet_test.log" | head -n1)
        echo "  -> Failed: ${err:-see log}" | tee -a "$RESULTS"
      else
        echo "  -> Passed" | tee -a "$RESULTS"
      fi
    else
      echo "  -> Unknown (no log)" | tee -a "$RESULTS"
    fi
  else
    echo "  -> Unknown (no run dir)" | tee -a "$RESULTS"
  fi
done

echo "$RESULTS"

