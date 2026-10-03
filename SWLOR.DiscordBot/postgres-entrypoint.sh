#!/bin/sh
set -eu

secrets_file="${SWLOR_BOT_SECRETS_FILE:-/run/secrets/discord_bot_secrets}"
if ! database_password="$(jq -ser '
    if length != 1 then empty
    elif (.[0] | type) != "object" then empty
    else
        .[0].databasePassword
        | strings
        | select(length > 0)
        | select(test("^\\s*$") | not)
        | select(all(explode[]; . != 0 and . != 10 and . != 13))
    end
' "$secrets_file" 2>/dev/null)"; then
    printf '%s\n' 'The secrets JSON must contain exactly one document and a non-whitespace databasePassword without line breaks or NUL.' >&2
    exit 1
fi

export POSTGRES_PASSWORD="$database_password"
unset database_password
exec /usr/local/bin/docker-entrypoint.sh "$@"
