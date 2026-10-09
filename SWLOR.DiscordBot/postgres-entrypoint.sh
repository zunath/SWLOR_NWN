#!/bin/sh
set -eu

secrets_file="${SWLOR_BOT_SECRETS_FILE:-/run/secrets/discord_bot_secrets}"
mode="${1:-postgres}"
if [ "$mode" = "rotate-password-stdin" ]; then secrets_file="-"; fi
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

case "$mode" in
    rotate-password-stdin)
        # Local administrative access is required. psql encrypts the password client-side;
        # it never appears in SQL logs, argv, shell history, or a second secret file.
        if ! printf '%s\n%s\n' "$database_password" "$database_password" |
            psql -X --no-password --host=/var/run/postgresql \
                --username="${POSTGRES_USER:-postgres}" --dbname="${POSTGRES_DB:-postgres}" \
                --set=ON_ERROR_STOP=1 --command='\password' >/dev/null 2>&1; then
            printf '%s\n' 'Database password rotation failed; keep the bot stopped and check local administrative access.' >&2
            exit 1
        fi
        unset database_password
        printf '%s\n' 'Database role password rotated.'
        exit 0
        ;;
    check-password)
        # pg_isready accepts an incorrect password; health must verify the mounted secret.
        if ! PGPASSWORD="$database_password" PGCONNECT_TIMEOUT=3 \
            psql -X --no-password --host="$(hostname)" \
                --username="${POSTGRES_USER:-postgres}" --dbname="${POSTGRES_DB:-postgres}" \
                --set=ON_ERROR_STOP=1 --command='SELECT 1' >/dev/null 2>&1; then
            printf '%s\n' 'Database authentication failed; use rotate-database-password.sh for retained volumes.' >&2
            exit 1
        fi
        unset database_password
        exit 0
        ;;
esac

export POSTGRES_PASSWORD="$database_password"
unset database_password
exec /usr/local/bin/docker-entrypoint.sh "$@"
