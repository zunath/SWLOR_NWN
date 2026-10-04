#!/bin/sh
set -eu

# Run Compose commands through this wrapper so the database-only secret is projected
# from secrets/secrets.json into the environment-backed Compose secret.
SWLOR_COMPOSE_DIR="$(CDPATH= cd "$(dirname "$0")" && pwd)"

swlor_compose_init() {
    cd "$SWLOR_COMPOSE_DIR"
    secrets_file="$SWLOR_COMPOSE_DIR/secrets/secrets.json"
    if [ ! -r "$secrets_file" ]; then
        printf '%s\n' 'Expected readable secrets/secrets.json beside compose.yml.' >&2
        return 1
    fi

    if ! SWLOR_BOT_DATABASE_SECRETS="$(jq -cser '
        if length != 1 then error("invalid secrets JSON")
        elif (.[0] | type) != "object" then error("invalid secrets JSON")
        elif (.[0].databasePassword | type) != "string" then error("invalid databasePassword")
        elif (.[0].databasePassword | length) == 0 then error("invalid databasePassword")
        elif (.[0].databasePassword | test("^\\s*$")) then error("invalid databasePassword")
        elif (.[0].databasePassword | all(explode[]; . != 0 and . != 10 and . != 13) | not) then error("invalid databasePassword")
        else { databasePassword: .[0].databasePassword }
        end
    ' "$secrets_file" 2>/dev/null)"; then
        printf '%s\n' 'The secrets JSON must contain exactly one object with a non-whitespace databasePassword without line breaks or NUL.' >&2
        return 1
    fi
    export SWLOR_BOT_DATABASE_SECRETS
}

swlor_compose() {
    docker compose "$@"
}

# rotate-database-password.sh sources this file to project the JSON once and reuse the
# same values for the password update and both service recreations.
if [ "${0##*/}" = "compose.sh" ]; then
    swlor_compose_init
    exec docker compose "$@"
fi
