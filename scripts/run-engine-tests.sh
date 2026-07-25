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
#                                [--server-home <dir>] [--timeout-minutes <n>]
#
# --timeout-minutes is a hard wall-clock limit on the containerized run (default
# 90; a full sweep takes roughly 45). It is the backstop against a server that
# never finishes on its own - e.g. one that boots healthy but schedules no tests,
# which stays responsive and therefore never trips the NWNX thread watchdog.
#
# Requires: dotnet SDK (unless --skip-build), docker compose, jq, timeout
#
# Functionally identical to run-engine-tests.ps1 - keep both in sync.

set -uo pipefail

SKIP_BUILD=0
FILTER=""
ARENA_RESREF=""
CONFIGURATION="Release"
SERVER_HOME="${SWLOR_ENGINE_TEST_SERVER_HOME:-}"
TIMEOUT_MINUTES=90

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
        --timeout-minutes)
            TIMEOUT_MINUTES="$2"
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
# Build/stage the ENGINE TEST project, not the game project: it carries the game
# assembly along as a project reference, so its output directory contains both. The
# game project's own output deliberately excludes the test assembly, which is what
# keeps test code out of a production deploy.
SERVER_PROJECT="$REPO_ROOT/SWLOR.Game.Server.EngineTests/SWLOR.Game.Server.EngineTests.csproj"

# The server home is the directory mounted as /nwn/home: it holds modules/, hak/,
# tlk/, dotnet/, swlor.env, and receives app_logs/. SWLOR.Game.Server/Docker only
# holds the tracked compose configuration - it is NOT a runtime home. Resolution
# order: --server-home, SWLOR_ENGINE_TEST_SERVER_HOME, the repo's enginetests-home/
# (dedicated, fully isolated from the dev server), the repo's debugserver/ (SHARED
# with the standard dev server - fallback only), then SWLOR.Game.Server/Docker as
# the last resort (what the CI workflow stages).
if [ -z "$SERVER_HOME" ]; then
    if [ -f "$REPO_ROOT/enginetests-home/swlor.env" ]; then
        SERVER_HOME="$REPO_ROOT/enginetests-home"
    elif [ -f "$REPO_ROOT/debugserver/swlor.env" ]; then
        SERVER_HOME="$REPO_ROOT/debugserver"
        echo "WARNING: using debugserver/ as the server home - this is SHARED with the standard dev server (logs, database, module file). Create <repo>/enginetests-home (modules, hak, tlk, swlor.env) for full isolation." >&2
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

    # BOTH outputs are staged, game project first. A referenced project's DLL is copied into
    # the referencing project's output, but its runtimeconfig.json/deps.json are NOT - and
    # NWNX_DotNET boots the host by the GAME assembly's name, so it needs
    # SWLOR.Game.Server.runtimeconfig.json specifically. The engine-test output is layered on
    # top to add the test assembly.
    GAME_OUTPUT_DIR="$REPO_ROOT/SWLOR.Game.Server/bin/$CONFIGURATION/net10.0"
    ENGINE_TEST_OUTPUT_DIR="$REPO_ROOT/SWLOR.Game.Server.EngineTests/bin/$CONFIGURATION/net10.0"
    for dir in "$GAME_OUTPUT_DIR" "$ENGINE_TEST_OUTPUT_DIR"; do
        if [ ! -d "$dir" ]; then
            echo "Expected build output directory not found: $dir" >&2
            exit 1
        fi
    done

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
    for dir in "$GAME_OUTPUT_DIR" "$ENGINE_TEST_OUTPUT_DIR"; do
        if ! cp -r "$dir"/. "$DOTNET_OUTPUT_DIR"/; then
            echo "Failed to stage build output into $DOTNET_OUTPUT_DIR" >&2
            exit 1
        fi
        echo "Copied $dir -> $DOTNET_OUTPUT_DIR"
    done

else
    section "Skipping build (assuming $DOTNET_OUTPUT_DIR is already current)"
fi

# Checked for BOTH paths, not just after a build: with --skip-build against a
# stale game-only staging directory (exactly the state left behind by older
# revisions of this script, before the harness was its own assembly) the server
# would boot, schedule nothing, and burn the entire wall clock before failing.
# Fail here in a second instead.
if [ ! -f "$DOTNET_OUTPUT_DIR/SWLOR.Game.Server.EngineTests.dll" ]; then
    echo "$DOTNET_OUTPUT_DIR is missing SWLOR.Game.Server.EngineTests.dll - no engine tests would run. Re-run without --skip-build to stage it." >&2
    exit 1
fi
if [ ! -f "$DOTNET_OUTPUT_DIR/SWLOR.Game.Server.runtimeconfig.json" ]; then
    echo "$DOTNET_OUTPUT_DIR is missing SWLOR.Game.Server.runtimeconfig.json - the NWNX .NET host cannot boot. Re-run without --skip-build to stage it." >&2
    exit 1
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
# The compose file's mounts interpolate SWLOR_ENGINE_TEST_HOME rather than PWD -
# an explicit variable works identically across bash and Windows PowerShell (which
# never exports a PWD environment variable for Compose to consume).
export SWLOR_ENGINE_TEST_HOME="$SERVER_HOME"

# Share the dev server's hak set via a dedicated Docker mount when the test home has
# none of its own - NTFS junctions do not survive Docker bind mounts, so this is the
# supported way to avoid duplicating ~13GB of haks.
if [ -z "${SWLOR_ENGINE_TEST_HAK_DIR:-}" ]; then
    if ! ls "$SERVER_HOME"/hak/*.hak > /dev/null 2>&1; then
        # Probe both next to the repo root and next to the server home - when running
        # from a worktree, debugserver/ only exists beside the real server homes.
        for dev_hak_dir in "$REPO_ROOT/debugserver/hak" "$(dirname "$SERVER_HOME")/debugserver/hak"; do
            if ls "$dev_hak_dir"/*.hak > /dev/null 2>&1; then
                export SWLOR_ENGINE_TEST_HAK_DIR="$dev_hak_dir"
                echo "Sharing hak set from $SWLOR_ENGINE_TEST_HAK_DIR (test home has no haks of its own)."
                break
            fi
        done
        if [ -z "${SWLOR_ENGINE_TEST_HAK_DIR:-}" ]; then
            echo "Server home '$SERVER_HOME' has no haks and no debugserver hak set was found - the module will fail to load." >&2
            exit 1
        fi
    fi
fi

# Run from the server home so any '.'-fallback interpolation still resolves to it.
pushd "$SERVER_HOME" > /dev/null

# Remove anything a previously interrupted run left behind before starting fresh.
docker compose -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE" down --volumes --remove-orphans

# HARD WALL CLOCK. `up --abort-on-container-exit` blocks until a container exits, and a
# server that never schedules its tests (missing harness, bad filter) idles happily forever -
# it is responsive, so the NWNX thread watchdog never fires either. Without this, such a run
# hangs until someone notices.
TIMEOUT_SECONDS=$((TIMEOUT_MINUTES * 60))
timeout --foreground "${TIMEOUT_SECONDS}s" \
    docker compose -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE" up --abort-on-container-exit --exit-code-from swlor-server
COMPOSE_EXIT_CODE=$?

if [ "$COMPOSE_EXIT_CODE" -eq 124 ]; then
    echo "TIMED OUT after ${TIMEOUT_MINUTES} minute(s) - the run was killed. The server never finished (look for 'ENGINE TEST HARNESS MISSING' or a stalled test above)." >&2
fi

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
