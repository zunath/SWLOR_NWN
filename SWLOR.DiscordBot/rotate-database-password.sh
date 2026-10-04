#!/bin/sh
set -eu

# Run after editing only databasePassword in secrets/secrets.json. Stop the worker before
# changing the persisted role, then recreate both secret mounts (atomic edits replace inodes).
CDPATH= cd "$(dirname "$0")"
if [ ! -r ./secrets/secrets.json ]; then
    printf '%s\n' 'Expected readable secrets/secrets.json beside compose.yml.' >&2
    exit 1
fi
docker compose stop bot
docker compose exec -T database /usr/local/bin/swlor-postgres-entrypoint.sh rotate-password-stdin < ./secrets/secrets.json
docker compose up -d --no-deps --force-recreate --wait --wait-timeout 60 database
docker compose up -d --no-deps --force-recreate --wait --wait-timeout 90 bot
printf '%s\n' 'Database password rotation completed; the bot passed its readiness check.'
