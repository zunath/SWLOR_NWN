#!/usr/bin/env bash
#
# Runs the integration test tier (SWLOR.Game.Server.IntegrationTests): the server's real container and database service
# against a real Redis, with a fake engine. It starts a throwaway Redis container of the production image, runs the project
# against it and always removes the container:
#
#   dotnet build SWLOR.Game.Server.sln -c Release -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly
#   scripts/ci/run-integration-tests.sh [--out <directory outside the repository>] [--configuration Release]
#                                       [--filter <dotnet test filter>] [--dump <path to a dump.rdb>]
#
# It does not build (build the solution once first). It needs Docker and, for the first run, the redislabs/redismod:latest
# image. The container is named swlor-it-redis-<pid>-<random>, publishes its port on 127.0.0.1 only (Docker picks a free
# one), keeps its data in a tmpfs and is the only container the script ever stops or removes. The exit code is the test run's.
#
# --dump (or the SWLOR_INTEGRATION_DUMP variable) names a production-copy dump.rdb for the optional production-copy test: the
# test copies the file into a container of its own and never writes to the original. Without it that test is skipped.

set -uo pipefail

# Git Bash on Windows would rewrite the container paths below (/data, /usr/lib/...) into host paths.
export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

OUT=""
CONFIGURATION="Release"
FILTER=""

while [ $# -gt 0 ]; do
    case "$1" in
        --out) OUT="$2"; shift 2 ;;
        --configuration) CONFIGURATION="$2"; shift 2 ;;
        --filter) FILTER="$2"; shift 2 ;;
        --dump) export SWLOR_INTEGRATION_DUMP="$2"; shift 2 ;;
        -h|--help) sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "Unknown option: $1" >&2; exit 2 ;;
    esac
done

cd "$(dirname "${BASH_SOURCE[0]}")/../.."

if ! command -v docker > /dev/null 2>&1 || ! docker info > /dev/null 2>&1; then
    echo "Docker is not running. The integration tier needs it for its throwaway Redis." >&2
    exit 2
fi

IMAGE="redislabs/redismod:latest"
NAME="swlor-it-redis-$$-$RANDOM"
RUN_ID="$NAME"
STARTED=0

cleanup() {
    local code=$?
    # Containers the production-copy test started carry this run's label; the Redis container is named.
    local labelled
    labelled="$(docker ps -aq --filter "label=swlor-integration-run=$RUN_ID" 2> /dev/null || true)"
    # shellcheck disable=SC2086
    [ -n "$labelled" ] && docker rm -f $labelled > /dev/null 2>&1
    [ "$STARTED" = 1 ] && docker rm -f "$NAME" > /dev/null 2>&1
    return $code
}
trap cleanup EXIT
trap 'exit 130' INT TERM

# The image's default module list, plus a RediSearch GC threshold no test run reaches: this RediSearch build's fork GC
# leaves a numeric range with a NULL cardinality array once every entry in it is deleted, and the next value added there
# segfaults Redis (NumericRange_Add). The tests create and delete records constantly. Same setting as
# SWLOR.Game.Server/Docker/docker-compose.enginetests.yml.
if ! docker run -d --name "$NAME" --label "swlor-integration-run=$RUN_ID" --tmpfs /data -p 127.0.0.1::6379 "$IMAGE" \
        --loadmodule /usr/lib/redis/modules/redisai.so \
        --loadmodule /usr/lib/redis/modules/redisearch.so FORK_GC_CLEAN_THRESHOLD 100000000 \
        --loadmodule /usr/lib/redis/modules/redisgraph.so \
        --loadmodule /usr/lib/redis/modules/redistimeseries.so \
        --loadmodule /usr/lib/redis/modules/rejson.so \
        --loadmodule /usr/lib/redis/modules/redisbloom.so \
        --loadmodule /usr/lib/redis/modules/redisgears.so Plugin /var/opt/redislabs/modules/rg/plugin/gears_python.so \
        > /dev/null; then
    echo "Could not start the Redis container." >&2
    exit 2
fi
STARTED=1

# Waits for PONG: while Redis loads its data it answers LOADING instead.
ready=0
for _ in $(seq 1 120); do
    if [ "$(docker exec "$NAME" redis-cli ping 2> /dev/null | tr -d '\r')" = "PONG" ]; then
        ready=1
        break
    fi
    sleep 1
done
if [ "$ready" != 1 ]; then
    echo "Redis did not become ready in 120 seconds:" >&2
    docker logs --tail 20 "$NAME" >&2
    exit 2
fi

port="$(docker port "$NAME" 6379/tcp | head -n 1 | sed 's/.*://' | tr -d '\r')"
if [ -z "$port" ]; then
    echo "Could not read the published Redis port." >&2
    exit 2
fi
docker exec "$NAME" redis-cli SET swlor:integration:throwaway 1 > /dev/null

export NWNX_REDIS_HOST="127.0.0.1:$port"
export SWLOR_INTEGRATION_RUN_ID="$RUN_ID"
echo "Redis $NAME on $NWNX_REDIS_HOST"

# A test that hangs (an engine loop the fake never ends) must not hold the run: the blame collector kills the test host.
args=(test SWLOR.Game.Server.IntegrationTests -c "$CONFIGURATION" --no-build --blame-hang-timeout 15m)
if [ -n "$OUT" ]; then
    mkdir -p "$OUT"
    args+=(--logger "trx;LogFileName=SWLOR.Game.Server.IntegrationTests.trx" --results-directory "$OUT")
fi
[ -n "$FILTER" ] && args+=(--filter "$FILTER")

dotnet "${args[@]}"
exit $?
