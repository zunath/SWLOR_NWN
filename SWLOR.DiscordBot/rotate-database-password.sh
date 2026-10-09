#!/bin/sh
set -eu

# Run after editing only databasePassword in secrets/secrets.json. Stop the worker before
# changing the persisted role, then recreate both services with the new projected secret.
CDPATH= cd "$(dirname "$0")"
. ./compose.sh
swlor_compose_init

database_secrets="$SWLOR_BOT_DATABASE_SECRETS"
swlor_compose stop bot
printf '%s\n' "$database_secrets" |
    swlor_compose exec -T database /usr/local/bin/swlor-postgres-entrypoint.sh rotate-password-stdin
swlor_compose up -d --no-deps --force-recreate --wait --wait-timeout 60 database
swlor_compose up -d --no-deps --force-recreate --wait --wait-timeout 90 bot
printf '%s\n' 'Database password rotation completed; the bot passed its readiness check.'
