#!/bin/bash
# Copies the NWN:EE dedicated-server data out of the pinned server Docker image to /opt/nwn in a
# Claude Code cloud session (Linux). Registered as an async hook. Toolset tests and the script
# compiler find it through NWN_INSTALL_PATH and NWN_ROOT, which session-start.sh exports.
#
# It holds the base game's 2DAs, scripts, walkmeshes, tilesets, palettes, and dialog.tlk. Models,
# textures, and sounds are zero-byte stubs or absent, so tests that read base-game art still need
# a client install. Progress goes to /tmp/swlor-game-data.log, and /tmp/swlor-game-data.done
# appears on success. Does nothing on local machines. Safe to run more than once. Async hooks
# ignore their "timeout" setting, so .claude/settings.json runs this under coreutils timeout.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

REPO="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
DEST=/opt/nwn
LOG=/tmp/swlor-game-data.log
DONE=/tmp/swlor-game-data.done

exec 9>/tmp/swlor-game-data.lock
if ! flock -n 9; then
  exit 0
fi
rm -f "$DONE"
exec >>"$LOG" 2>&1
log() { echo "[$(date -u +%H:%M:%S)] $*"; }

image=$(head -n 1 "$REPO/scripts/deployment/server-image.txt" | tr -d '\r')
# The image that filled DEST, so a newly pinned image replaces stale data.
MARKER="$DEST/.swlor-server-image"
if [ -f "$DEST/data/nwn_base.key" ] && [ "$(cat "$MARKER" 2>/dev/null)" = "$image" ]; then
  log "Game data from $image is already in $DEST"
  touch "$DONE"
  exit 0
fi
# Staging copies that an interrupted earlier run left behind.
STAGING_PREFIX=/opt/.swlor-game-data-staging
rm -rf "$STAGING_PREFIX".*
if ! docker info >/dev/null 2>&1; then
  log "Starting the Docker daemon (log: /tmp/dockerd.log)"
  setsid nohup dockerd >/tmp/dockerd.log 2>&1 </dev/null &
  for _ in $(seq 1 30); do docker info >/dev/null 2>&1 && break; sleep 1; done
fi

# Docker Hub rate-limits anonymous pulls; retry with backoff.
pulled=0
for delay in 0 30 60 120; do
  sleep "$delay"
  if docker pull -q "$image"; then
    pulled=1
    break
  fi
  log "Pull of $image failed; retrying"
done
if [ "$pulled" != 1 ]; then
  if ! docker image inspect "$image" >/dev/null 2>&1; then
    log "Could not pull $image; tests that need game data will skip or fail"
    exit 1
  fi
  log "Could not pull $image; using the local copy"
fi

container="" staging=""
cleanup() {
  [ -z "$container" ] || docker rm -f "$container" >/dev/null 2>&1 || true
  [ -z "$staging" ] || rm -rf "$staging"
}
trap cleanup EXIT
trap 'exit 143' TERM INT HUP # so a timeout still runs cleanup
container=$(docker create "$image")
staging=$(mktemp -d "$STAGING_PREFIX.XXXXXX")
docker cp -q "$container:/nwn/data/." "$staging/"
echo "$image" > "$staging/.swlor-server-image"
rm -rf "$DEST" # An incomplete copy or data from another image.
mv "$staging" "$DEST"
staging=""
touch "$DONE"
log "Game data from $image is in $DEST"
