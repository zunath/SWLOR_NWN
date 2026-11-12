#!/usr/bin/env bash
#
# The local hot reload check (MaintainabilityRefactorHotReload.md, P4-13 and P4-14). Boots the engine-test
# stack (SWLOR.Game.Server/Docker/docker-compose.enginetests.yml, its own Compose project and a throwaway
# tmpfs Redis) on the host entry with SWLOR_HOT_RELOAD=true, waits until the server watches the reload signal
# and has logged its boot digests, then replaces the running game build --reloads times with SWLOR.CLI
# --reload (publish a candidate into dotnet/generations/<id>/, signal it, wait for the receipt) and prints
# each receipt, the pause the server logged and the reload bootstrap's slowest steps. The game is staged
# without the engine-test assembly, so no engine test runs: the server boots, logs "ENGINE TEST HARNESS
# MISSING" and idles until the check tears it down.
#
# The reload digests (P4-14, SWLOR_RELOAD_DIGEST lines, logged only with hot reload on) are then compared:
#   - cache: after every reload, the digest of each class that owns a rerun boot handler equals the boot's;
#   - adopted and world: after every reload, the adopted state (spawns, properties, docks, flights, weather,
#     stores) and the world the boot-only handlers wrote (store prices and flags, dock ids, spawned objects per
#     area, property objects, weather, sky and fog per area, objects per area) equal what the build it
#     replaced logged at quiescence, so no boot-only side effect happened again;
#   - every digest was taken (none is error:<exception> or unresolved).
#
# Usage:
#   scripts/run-hot-reload-check.sh [--server-home <dir>] [--reloads <n>] [--skip-build]
#                                   [--boot-timeout-minutes <n>] [--reload-timeout-seconds <n>]
#
# The server home resolves like run-engine-tests.sh's: --server-home, SWLOR_ENGINE_TEST_SERVER_HOME, the
# repo's enginetests-home/, the repo's debugserver/. Its dotnet/ is restaged (the host in dotnet/, the game
# in dotnet/game/) and its reload/ is recreated; everything else is left as it was.
#
# Exit status 0 only when every reload's receipt says Applied, every digest matches and the server is still
# running after them.
#
# Requires: dotnet SDK, docker compose, timeout, awk.

set -uo pipefail

SKIP_BUILD=0
SERVER_HOME="${SWLOR_ENGINE_TEST_SERVER_HOME:-}"
RELOADS=3
DIGEST_TIMEOUT_SECONDS=300
BOOT_TIMEOUT_MINUTES=20
RELOAD_TIMEOUT_SECONDS=900

while [ $# -gt 0 ]; do
    case "$1" in
        --skip-build)
            SKIP_BUILD=1
            shift
            ;;
        --server-home)
            SERVER_HOME="$2"
            shift 2
            ;;
        --reloads)
            RELOADS="$2"
            shift 2
            ;;
        --boot-timeout-minutes)
            BOOT_TIMEOUT_MINUTES="$2"
            shift 2
            ;;
        --reload-timeout-seconds)
            RELOAD_TIMEOUT_SECONDS="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 2
            ;;
    esac
done

case "$RELOADS" in
    ''|*[!0-9]*|0) echo "--reloads must be a positive number, not '$RELOADS'." >&2; exit 2 ;;
esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [ -z "$SERVER_HOME" ]; then
    if [ -f "$REPO_ROOT/enginetests-home/swlor.env" ]; then
        SERVER_HOME="$REPO_ROOT/enginetests-home"
    elif [ -f "$REPO_ROOT/debugserver/swlor.env" ]; then
        SERVER_HOME="$REPO_ROOT/debugserver"
        echo "WARNING: using debugserver/ as the server home - it is SHARED with the standard dev server. Stop that server first." >&2
    else
        echo "No server home: pass --server-home <dir> (modules/, hak/, tlk/, swlor.env) or create <repo>/enginetests-home." >&2
        exit 1
    fi
fi
if [ ! -f "$SERVER_HOME/swlor.env" ]; then
    echo "Server home '$SERVER_HOME' has no swlor.env - it doesn't look like an NWN home directory." >&2
    exit 1
fi

COMPOSE_FILE="$REPO_ROOT/SWLOR.Game.Server/Docker/docker-compose.enginetests.yml"
COMPOSE_PROJECT="swlor-hot-reload-check"
GAME_OUTPUT_DIR="$REPO_ROOT/SWLOR.Game.Server/bin/Release/net10.0"
HOST_OUTPUT_DIR="$REPO_ROOT/SWLOR.Game.Host/bin/Release/net10.0"
CLI_ASSEMBLY="$REPO_ROOT/SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll"
DOTNET_DIR="$SERVER_HOME/dotnet"
RELOAD_DIR="$SERVER_HOME/reload"

section() {
    echo ""
    echo "=== $1 ==="
}

# The CLI is a Windows program under Git Bash: hand it native paths.
native_path() {
    if command -v cygpath > /dev/null 2>&1; then
        cygpath -w "$1"
    else
        printf '%s\n' "$1"
    fi
}

if [ "$SKIP_BUILD" -eq 0 ]; then
    section "Building the game, the host and SWLOR.CLI (Release)"
    for project in SWLOR.Game.Server/SWLOR.Game.Server.csproj SWLOR.Game.Host/SWLOR.Game.Host.csproj SWLOR.CLI/SWLOR.CLI.csproj; do
        if ! dotnet build "$REPO_ROOT/$project" -c Release -p:RunPostBuildEvent=Never -nologo -v q -clp:ErrorsOnly; then
            echo "dotnet build of $project failed" >&2
            exit 1
        fi
    done
fi
for required in "$GAME_OUTPUT_DIR/SWLOR.Game.Server.dll" "$HOST_OUTPUT_DIR/SWLOR.Game.Host.dll" "$HOST_OUTPUT_DIR/SWLOR.Game.Host.runtimeconfig.json" "$CLI_ASSEMBLY"; do
    if [ ! -f "$required" ]; then
        echo "Missing build output: $required. Run without --skip-build." >&2
        exit 1
    fi
done

section "Staging the host entry into $DOTNET_DIR (no engine-test assembly)"
if ! rm -rf "$DOTNET_DIR" "$RELOAD_DIR"; then
    echo "Could not clear $DOTNET_DIR and $RELOAD_DIR" >&2
    exit 1
fi
if ! mkdir -p "$DOTNET_DIR/game" "$RELOAD_DIR/receipts" ||
   ! cp -r "$HOST_OUTPUT_DIR"/. "$DOTNET_DIR"/ ||
   ! cp -r "$GAME_OUTPUT_DIR"/. "$DOTNET_DIR/game"/; then
    echo "Could not stage the host and the game into $DOTNET_DIR" >&2
    exit 1
fi

export SWLOR_ENGINE_TEST_HOME="$SERVER_HOME"
export SWLOR_ENGINE_TEST_HOT_RELOAD=true
export SWLOR_ENGINE_TEST_SHUTDOWN=false
export SWLOR_ENGINE_TEST_DOTNET_ENTRY=host
export SWLOR_ENGINE_TEST_DOTNET_ASSEMBLY="/nwn/home/dotnet/SWLOR.Game.Host"
export SWLOR_ENGINE_TEST_DOTNET_ENTRYPOINT="SWLOR.Game.Host.Bootstrap.NativeBootstrap"
export SWLOR_ENGINE_TEST_DOTNET_METHOD="Initialize"
export SWLOR_ENGINE_TEST_GAME_COLLECTIBLE=true

# Share the dev server's hak set when the home has none, as run-engine-tests.sh does.
if [ -z "${SWLOR_ENGINE_TEST_HAK_DIR:-}" ] && ! ls "$SERVER_HOME"/hak/*.hak > /dev/null 2>&1; then
    for dev_hak_dir in "$REPO_ROOT/debugserver/hak" "$(dirname "$SERVER_HOME")/debugserver/hak"; do
        if ls "$dev_hak_dir"/*.hak > /dev/null 2>&1; then
            export SWLOR_ENGINE_TEST_HAK_DIR="$dev_hak_dir"
            echo "Sharing hak set from $SWLOR_ENGINE_TEST_HAK_DIR."
            break
        fi
    done
    if [ -z "${SWLOR_ENGINE_TEST_HAK_DIR:-}" ]; then
        echo "Server home '$SERVER_HOME' has no haks and no debugserver hak set was found - the module will fail to load." >&2
        exit 1
    fi
fi

compose() {
    docker compose -p "$COMPOSE_PROJECT" -f "$COMPOSE_FILE" "$@"
}

# The SWLOR_RELOAD_DIGEST lines the server logged, one per line as "<generation> <phase> <kind> <name> <count> <hash>".
# The summary lines (SWLOR_RELOAD_DIGESTS) do not match.
digest_lines() {
    compose logs --no-color swlor-server 2>/dev/null |
        sed -n 's/.*SWLOR_RELOAD_DIGEST \(generation=.*\)$/\1/p' |
        tr -d '\r' |
        awk '{
            for (i = 1; i <= NF; i++) {
                split($i, pair, "=")
                value[pair[1]] = substr($i, length(pair[1]) + 2)
            }
            print value["generation"], value["phase"], value["kind"], value["name"], value["count"], value["hash"]
        }'
}

# How many digest sets generation $1 has logged in phase $2 (one SWLOR_RELOAD_DIGESTS summary line per set).
digest_sets() {
    compose logs --no-color swlor-server 2>/dev/null | grep -c "SWLOR_RELOAD_DIGESTS generation=$1 phase=$2 "
}

# Waits until generation $1 has logged $3 digest sets in phase $2: a boot logs one (cache), a reload two (adopted and
# world inside the pause, cache at the next frame).
wait_for_digests() {
    local deadline=$(( $(date +%s) + DIGEST_TIMEOUT_SECONDS ))
    until [ "$(digest_sets "$1" "$2")" -ge "$3" ]; do
        if [ "$(date +%s)" -ge "$deadline" ]; then
            echo "Generation $1 did not log $3 $2 digest set(s) within $DIGEST_TIMEOUT_SECONDS second(s)." >&2
            return 1
        fi
        sleep 2
    done
}

# Prints the differences between two digest sets ("<kind> <name> <count> <hash>" lines) and fails when there are any.
compare_digest_sets() {
    local label="$1" expected="$2" actual="$3"
    if [ -z "$expected" ]; then
        echo "$label: nothing to compare against." >&2
        return 1
    fi
    local differences
    differences="$(diff <(printf '%s\n' "$expected" | sort) <(printf '%s\n' "$actual" | sort))"
    if [ -n "$differences" ]; then
        echo "$label: the digests differ (< expected, > after the reload):" >&2
        printf '%s\n' "$differences" >&2
        return 1
    fi
    echo "$label: $(printf '%s\n' "$expected" | wc -l | tr -d ' ') digest(s) match."
}

pushd "$SERVER_HOME" > /dev/null

HOST_UID="$(id -u)"
HOST_GID="$(id -g)"
PERMISSION_SNAPSHOT_NAME=".swlor-hot-reload-permissions.$$.snapshot"
PERMISSION_SNAPSHOT_CONTAINER="/nwn/home/$PERMISSION_SNAPSHOT_NAME"
PERMISSIONS_PREPARED=0
STACK_ACTIVE=1

# The same ownership handling as run-engine-tests.sh, with reload/ added: the non-root server (UID 1000)
# writes its receipts there and the developer's SWLOR.CLI writes the signal, so the folder is open to both.
restore_permissions()
{
    local restore_status=0
    if [ "$PERMISSIONS_PREPARED" -eq 1 ]; then
        compose run --rm --no-deps -T --user 0:0 --entrypoint /bin/bash \
            -e "SWLOR_PERMISSION_SNAPSHOT=$PERMISSION_SNAPSHOT_CONTAINER" \
            -e "SWLOR_HOST_UID=$HOST_UID" -e "SWLOR_HOST_GID=$HOST_GID" \
            swlor-server -s <<'SWLOR_RESTORE_PERMISSIONS'
set -e
snapshot="$SWLOR_PERMISSION_SNAPSHOT"
test -f "$snapshot" || exit 0
for path in app_logs database development logs nwsync override portraits saves servervault reload; do
  if test -e "/nwn/home/$path"; then
    chown -R "$SWLOR_HOST_UID:$SWLOR_HOST_GID" "/nwn/home/$path"
  fi
done
while IFS= read -r -d "" relative_path &&
      IFS= read -r -d "" owner_uid &&
      IFS= read -r -d "" owner_gid &&
      IFS= read -r -d "" file_mode &&
      IFS= read -r -d "" file_type
do
  target="/nwn/home/$relative_path"
  if test -e "$target" || test -L "$target"; then
    if test "$file_type" = l; then
      chown -h "$owner_uid:$owner_gid" "$target"
    else
      chown "$owner_uid:$owner_gid" "$target"
      chmod "$file_mode" "$target"
    fi
  fi
done < "$snapshot"
rm -f "$snapshot"
SWLOR_RESTORE_PERMISSIONS
        restore_status=$?
    fi
    PERMISSIONS_PREPARED=0
    return "$restore_status"
}

cleanup()
{
    local cleanup_status=$?
    trap - EXIT INT TERM
    set +e
    if [ "$STACK_ACTIVE" -eq 1 ]; then
        section "Tearing down the stack"
        compose stop
        restore_permissions || cleanup_status=1
        compose down --volumes --remove-orphans
        popd > /dev/null || true
        STACK_ACTIVE=0
    fi
    exit "$cleanup_status"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

compose down --volumes --remove-orphans

section "Preparing the server home for the non-root runtime"
PERMISSIONS_PREPARED=1
compose run --rm --no-deps -T --user 0:0 --entrypoint /bin/bash \
    -e "SWLOR_PERMISSION_SNAPSHOT=$PERMISSION_SNAPSHOT_CONTAINER" \
    swlor-server -s <<'SWLOR_PREPARE_PERMISSIONS'
set -e
snapshot="$SWLOR_PERMISSION_SNAPSHOT"
cd /nwn/home
for path in cryptographic_secret nwn.ini nwnplayer.ini settings.tml; do
  if test -L "$path"; then
    echo "Server configuration must not be a symbolic link: /nwn/home/$path" >&2
    exit 1
  fi
done
: > "$snapshot"
chmod 0600 "$snapshot"
for path in app_logs database development logs nwsync override portraits saves servervault \
            cryptographic_secret nwn.ini nwnplayer.ini settings.tml; do
  if test -e "$path" || test -L "$path"; then
    find "$path" -xdev -printf "%p\0%U\0%G\0%m\0%y\0" >> "$snapshot"
  fi
done
for path in app_logs database development logs nwsync override portraits saves servervault reload; do
  mkdir -p "/nwn/home/$path"
  chown -R 1000:1000 "/nwn/home/$path"
  chmod -R u+rwX "/nwn/home/$path"
done
mkdir -p /nwn/home/reload/receipts
chown 1000:1000 /nwn/home/reload/receipts
chmod 0777 /nwn/home/reload /nwn/home/reload/receipts
for path in cryptographic_secret nwn.ini nwnplayer.ini settings.tml; do
  if test -f "/nwn/home/$path"; then
    chown 0:1000 "/nwn/home/$path"
    chmod 0640 "/nwn/home/$path"
  fi
done
SWLOR_PREPARE_PERMISSIONS
if [ $? -ne 0 ]; then
    echo "Could not prepare the server home for UID/GID 1000." >&2
    exit 1
fi

section "Booting the host entry with SWLOR_HOT_RELOAD=true (project $COMPOSE_PROJECT)"
if ! compose up -d; then
    echo "docker compose up failed" >&2
    exit 1
fi

# The host logs "SWLOR_HOST reload-watching" once the module has loaded and it watches reload/signal.json.
deadline=$(( $(date +%s) + BOOT_TIMEOUT_MINUTES * 60 ))
until compose logs --no-color swlor-server 2>/dev/null | grep -q "SWLOR_HOST reload-watching"; do
    if compose logs --no-color swlor-server 2>/dev/null | grep -q "SWLOR_HOST reload-refused"; then
        echo "The host refused hot reload:" >&2
        compose logs --no-color swlor-server | grep "SWLOR_HOST reload-refused" >&2
        exit 1
    fi
    if [ -z "$(compose ps --status running -q swlor-server)" ]; then
        echo "The server container is not running. Its last lines:" >&2
        compose logs --no-color --tail 80 swlor-server >&2
        exit 1
    fi
    if [ "$(date +%s)" -ge "$deadline" ]; then
        echo "The server did not start watching the reload signal within $BOOT_TIMEOUT_MINUTES minute(s)." >&2
        compose logs --no-color --tail 80 swlor-server >&2
        exit 1
    fi
    sleep 5
done
compose logs --no-color swlor-server | grep "SWLOR_HOST \(generation\|reload-on\|reload-watching\)"

failed=0
# The boot logs its cache digests at the frame after the module has loaded.
if ! wait_for_digests 1 boot 1; then
    exit 1
fi

applied=0
for reload in $(seq 1 "$RELOADS"); do
    section "Reload $reload of $RELOADS: SWLOR.CLI --reload"
    if ! dotnet "$CLI_ASSEMBLY" --reload \
            --server-home "$(native_path "$SERVER_HOME")" \
            --server-output "$(native_path "$GAME_OUTPUT_DIR")" \
            --reload-timeout "$RELOAD_TIMEOUT_SECONDS"; then
        echo "Reload $reload did not apply." >&2
        failed=1
        break
    fi
    applied=$reload
    # Generation <reload + 1> logs its adopted and world digests inside the pause and its cache digests a frame later.
    if ! wait_for_digests "$((reload + 1))" reload 2; then
        failed=1
        break
    fi
done

section "What the server logged about the reloads"
compose logs --no-color swlor-server |
    grep "SWLOR_HOST reload-\|SWLOR_HOST restart-required\|activated by a reload\|quiesced for a reload\|could not carry\|SWLOR_RELOAD_DIGESTS \|Hot reload: "

section "Comparing the reload digests"
digests="$(digest_lines)"
not_taken="$(printf '%s\n' "$digests" | awk '$6 ~ /^error:/ || $6 == "unresolved"')"
if [ -n "$not_taken" ]; then
    echo "Digests that could not be taken (generation phase kind name count hash):" >&2
    printf '%s\n' "$not_taken" >&2
    failed=1
fi
if compose logs --no-color swlor-server 2>/dev/null | grep -q "SWLOR_RELOAD_DIGESTS .* failed: "; then
    echo "A digest set failed:" >&2
    compose logs --no-color swlor-server | grep "SWLOR_RELOAD_DIGESTS .* failed: " >&2
    failed=1
fi
boot_cache="$(printf '%s\n' "$digests" | awk '$1 == 1 && $2 == "boot" && $3 == "cache" { print $3, $4, $5, $6 }')"
for generation in $(seq 2 $((applied + 1))); do
    reload_cache="$(printf '%s\n' "$digests" | awk -v g="$generation" '$1 == g && $2 == "reload" && $3 == "cache" { print $3, $4, $5, $6 }')"
    compare_digest_sets "Generation $generation cache against the boot" "$boot_cache" "$reload_cache" || failed=1
    quiesced="$(printf '%s\n' "$digests" | awk -v g="$((generation - 1))" '$1 == g && $2 == "quiesce" { print $3, $4, $5, $6 }')"
    adopted="$(printf '%s\n' "$digests" | awk -v g="$generation" '$1 == g && $2 == "reload" && $3 != "cache" { print $3, $4, $5, $6 }')"
    compare_digest_sets "Generation $generation adopted state and world against generation $((generation - 1)) at quiescence" \
        "$quiesced" "$adopted" || failed=1
done

if [ -z "$(compose ps --status running -q swlor-server)" ]; then
    echo "The server is no longer running after the reloads." >&2
    compose logs --no-color --tail 80 swlor-server >&2
    failed=1
fi

if [ "$failed" -eq 0 ]; then
    echo "Hot reload check passed: $RELOADS reload(s) applied, every digest matched and the server is still running."
    exit 0
fi
echo "Hot reload check failed." >&2
exit 1
