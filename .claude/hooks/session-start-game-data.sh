#!/bin/bash
# Copies the NWN:EE dedicated-server data out of the pinned server Docker image to /opt/nwn in a
# Claude Code cloud session (Linux). Registered as an async hook. Toolset tests and the script
# compiler find it through NWN_INSTALL_PATH and NWN_ROOT, which session-start.sh exports.
#
# It holds the base game's 2DAs, scripts, walkmeshes, tilesets, palettes, and dialog.tlk. Models,
# textures, and sounds are zero-byte stubs or absent, so tests that read base-game art still need
# a client install. Progress goes to /tmp/swlor-game-data.log, and /tmp/swlor-game-data.done
# appears on success. Does nothing on local machines. Safe to run more than once.
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

if [ -f "$DEST/data/nwn_base.key" ]; then
  log "Game data is already in $DEST"
  touch "$DONE"
  exit 0
fi

image=$(head -n 1 "$REPO/scripts/deployment/server-image.txt" | tr -d '\r')
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
  log "Could not pull $image; tests that need game data will skip or fail"
  exit 1
fi

container=$(docker create "$image")
trap 'docker rm -f "$container" >/dev/null 2>&1 || true' EXIT
staging=$(mktemp -d "$DEST.XXXXXX")
docker cp -q "$container:/nwn/data/." "$staging/"
rm -rf "$DEST" # Only an incomplete copy from an earlier run can be here.
mv "$staging" "$DEST"
touch "$DONE"
log "Game data from $image is in $DEST"
