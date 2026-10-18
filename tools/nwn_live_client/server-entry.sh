#!/bin/bash
# Server entrypoint for tools/nwn_live_client. Feeds the interactive nwserver console
# from /nwn/run/console.in, where the host tool appends commands such as "kick <name>",
# and stops the server with SIGINT once SWLOR_LIVE_MAX_SECONDS have passed so a
# forgotten stack cannot run forever. docker stop's SIGINT is forwarded by timeout.
set -eu
console=/nwn/run/console.in
: > "$console"
exec timeout --signal=INT --kill-after=60 "$SWLOR_LIVE_MAX_SECONDS" \
    /bin/bash /nwn/run-server.sh < <(tail -n 0 -F "$console" 2>/dev/null)
