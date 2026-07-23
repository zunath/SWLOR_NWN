#!/usr/bin/env bash
#
# Builds SWLOR.Game.Server, runs the in-engine integration test suite in a
# headless Docker container, and reports pass/fail based on the JSON report
# the server writes (see SWLOR.Game.Server/Service/EngineTest.cs).
#
# Runs against a server home directory (modules/, hak/, tlk/, swlor.env already
# populated - the normal dev/deploy-machine flow). The home resolves to
# --server-home, then SWLOR_ENGINE_TEST_SERVER_HOME, then the repo's
# debugserver/ directory, then SWLOR.Game.Server/Docker (the CI-staged layout).
# The script only builds and stages the compiled .NET assembly into the home,
# then runs the test container against it.
#
# Usage:
#   scripts/run-engine-tests.sh [--skip-build] [--filter <substring>]
#                                [--arena-resref <resref>] [--configuration <cfg>]
#                                [--server-home <dir>]
#
# Requires: dotnet SDK (unless --skip-build), docker compose, jq
#
# Functionally identical to run-engine-tests.ps1 - keep both in sync.

set -uo pipefail

SKIP_BUILD=0
FILTER=""
ARENA_RESREF=""
CONFIGURATION="Release"
SERVER_HOME="${SWLOR_ENGINE_TEST_SERVER_HOME:-}"

while [ $# -gt 0 ]; do
    case "$1" in
        --skip-build)
            SKIP_BUILD=1
            shift
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --arena-resref)
            ARENA_RESREF="$2"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --server-home)
            SERVER_HOME="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 2
            ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SERVER_PROJECT="$REPO_ROOT/SWLOR.Game.Server/SWLOR.Game.Server.csproj"

# The server home is the directory mounted as /nwn/home: it holds modules/, hak/,
# tlk/, dotnet/, swlor.env, and receives app_logs/. SWLOR.Game.Server/Docker only
# holds the tracked compose configuration - it is NOT a runtime home. Resolution
# order: --server-home, SWLOR_ENGINE_TEST_SERVER_HOME, the repo's debugserver/
# directory (the dev-machine convention), then SWLOR.Game.Server/Docker as the
# last resort (what the CI workflow stages).
if [ -z "$SERVER_HOME" ]; then
    if [ -f "$REPO_ROOT/debugserver/swlor.env" ]; then
        SERVER_HOME="$REPO_ROOT/debugserver"
    else
        SERVER_HOME="$REPO_ROOT/SWLOR.Game.Server/Docker"
    fi
fi
if [ ! -f "$SERVER_HOME/swlor.env" ]; then
    echo "Server home '$SERVER_HOME' has no swlor.env - it doesn't look like an NWN home directory." >&2
    exit 1
fi

COMPOSE_FILE="$REPO_ROOT/SWLOR.Game.Server/Docker/docker-compose.enginetests.yml"
# A dedicated project name keeps these containers isolated from the normal dev
# stack even though both may use the same server home directory.
COMPOSE_PROJECT="swlor-engine-tests"
DOTNET_OUTPUT_DIR="$SERVER_HOME/dotnet"
REPORT_PATH="$SERVER_HOME/app_logs/engine_tests/engine-test-results.json"

section() {
    echo ""
    echo "=== $1 ==="
}

if [ "$SKIP_BUILD" -eq 0 ]; then
    section "Building SWLOR.Game.Server ($CONFIGURATION)"

    # RunPostBuildEvent=Never skips the SWLOR.CLI -o post-build deploy step
    # (which is Windows-only anyway); we do our own targeted copy below.
    dotnet build "$SERVER_PROJECT" -c "$CONFIGURATION" -p:RunPostBuildEvent=Never
    if [ $? -ne 0 ]; then
        echo "dotnet build failed" >&2
        exit 1
    fi

    BUILD_OUTPUT_DIR="$REPO_ROOT/SWLOR.Game.Server/bin/$CONFIGURATION/net10.0"
    if [ ! -d "$BUILD_OUTPUT_DIR" ]; then
        echo "Expected build output directory not found: $BUILD_OUTPUT_DIR" >&2
        exit 1
    fi

    section "Deploying build output to $DOTNET_OUTPUT_DIR"
    # Every staging step is checked: silently running the test container against a stale
    # or partially copied assembly would produce a passing report for the wrong build.
    if ! rm -rf "$DOTNET_OUTPUT_DIR"; then
        echo "Failed to remove previous staging directory $DOTNET_OUTPUT_DIR" >&2
        exit 1
    fi
    if ! mkdir -p "$DOTNET_OUTPUT_DIR"; then
        echo "Failed to create staging directory $DOTNET_OUTPUT_DIR" >&2
        exit 1
    fi
    if ! cp -r "$BUILD_OUTPUT_DIR"/. "$DOTNET_OUTPUT_DIR"/; then
        echo "Failed to stage build output into $DOTNET_OUTPUT_DIR" >&2
        exit 1
    fi
    echo "Copied $BUILD_OUTPUT_DIR -> $DOTNET_OUTPUT_DIR"
else
    section "Skipping build (assuming $DOTNET_OUTPUT_DIR is already current)"
fi

# Stale report from a previous run must not be mistaken for this run's result.
# If deletion silently fails (e.g. the directory is owned by another UID from a
# prior Docker run) and the server then crashes before writing a fresh report,
# we would otherwise parse the old report and report a bogus pass.
rm -f "$REPORT_PATH"
if [ -e "$REPORT_PATH" ]; then
    echo "Could not remove stale report at $REPORT_PATH - aborting so this run cannot be judged by a previous run's results." >&2
    exit 1
fi

section "Running engine tests via docker compose (server home: $SERVER_HOME)"
export SWLOR_ENGINE_TEST_FILTER="$FILTER"
export SWLOR_ENGINE_TEST_ARENA_RESREF="$ARENA_RESREF"

# Run from the server home so the compose file's ${PWD-.} mounts resolve to it.
pushd "$SERVER_HOME" > /dev/null

# Remove anything a previously interrupted run left behind before starting fresh.
docker compose -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE" down --volumes --remove-orphans

docker compose -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE" up --abort-on-container-exit --exit-code-from swlor-server
COMPOSE_EXIT_CODE=$?

section "Tearing down containers"
docker compose -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE" down --volumes

popd > /dev/null

echo "docker compose exit code: $COMPOSE_EXIT_CODE"

section "Engine test results"
if [ ! -f "$REPORT_PATH" ]; then
    echo "No report found at $REPORT_PATH - the server likely crashed or was killed before it could write one." >&2
    exit 1
fi

if ! command -v jq > /dev/null 2>&1; then
    echo "jq is required to parse the engine test report but was not found on PATH." >&2
    exit 1
fi

TOTAL=$(jq -r '.Total' "$REPORT_PATH")
PASSED=$(jq -r '.Passed' "$REPORT_PATH")
FAILED=$(jq -r '.Failed' "$REPORT_PATH")
SKIPPED=$(jq -r '.Skipped' "$REPORT_PATH")

printf "%-16s %-40s %-8s %8s  %s\n" "CATEGORY" "NAME" "OUTCOME" "MS" "MESSAGE"
jq -r '
  .Results
  | sort_by(.Category, .Name)
  | .[]
  | [
      .Category,
      .Name,
      (if .Outcome == 0 then "Passed" elif .Outcome == 1 then "Failed" else "Skipped" end),
      (.DurationMilliseconds | tostring),
      (.Message // "")
    ]
  | @tsv
' "$REPORT_PATH" | while IFS=$'\t' read -r category name outcome ms message; do
    printf "%-16s %-40s %-8s %8s  %s\n" "$category" "$name" "$outcome" "$ms" "$message"
done

echo "SUMMARY total=$TOTAL passed=$PASSED failed=$FAILED skipped=$SKIPPED"

# The container exit status participates in the verdict: a server that crashed AFTER
# writing a passing report (e.g. during the delayed shutdown) must not be reported green.
if [ "$COMPOSE_EXIT_CODE" -eq 0 ] && [ "$TOTAL" -gt 0 ] && [ "$FAILED" -eq 0 ]; then
    echo "Engine tests passed."
    exit 0
else
    if [ "$COMPOSE_EXIT_CODE" -ne 0 ]; then
        echo "Engine tests failed: server container exited with code $COMPOSE_EXIT_CODE." >&2
    else
        echo "Engine tests failed (or none ran)." >&2
    fi
    exit 1
fi
