#!/bin/bash
# Client entrypoint for tools/nwn_live_client. Runs the Linux NWN:EE client on a private
# Xvfb display inside this container and connects it to the stack's server. The display
# exists only in the container: xdotool input and captures never reach the host desktop.
set -eu
client_pid=
display_pid=
cleanup() {
    if [ -n "$client_pid" ] && kill -0 "$client_pid" 2>/dev/null; then
        kill "$client_pid" 2>/dev/null || true
        for _ in $(seq 1 20); do
            kill -0 "$client_pid" 2>/dev/null || break
            sleep 0.5
        done
        kill -9 "$client_pid" 2>/dev/null || true
    fi
    if [ -n "$display_pid" ] && kill -0 "$display_pid" 2>/dev/null; then
        kill "$display_pid" 2>/dev/null || true
    fi
    # The key is copied in by the host tool for this run only.
    rm -f /profile/cdkey.ini /tmp/client.pid
}
trap cleanup EXIT
trap 'exit 143' INT TERM

Xvfb :99 -screen 0 1280x720x24 -nolisten tcp > /verify/xvfb.log 2>&1 &
display_pid=$!
for _ in $(seq 1 50); do
    xdotool getdisplaygeometry > /dev/null 2>&1 && break
    sleep 0.2
done

server_ip=$(getent hosts "$SWLOR_LIVE_SERVER_HOST" | awk '{print $1; exit}')
if [ -z "$server_ip" ]; then
    echo "Cannot resolve $SWLOR_LIVE_SERVER_HOST" >&2
    exit 1
fi

args=(-userdirectory /profile)
if [ "$SWLOR_LIVE_CLIENT_MODE" = dm ]; then
    args+=(-dmc +connect "$server_ip:$SWLOR_LIVE_SERVER_PORT" +password "$SWLOR_LIVE_DM_PASSWORD")
else
    args+=(+connect "$server_ip:$SWLOR_LIVE_SERVER_PORT")
fi
./nwmain-linux "${args[@]}" > /verify/client-stdout.log 2>&1 &
client_pid=$!
echo "$client_pid" > /tmp/client.pid

deadline=$((SECONDS + SWLOR_LIVE_MAX_SECONDS))
while kill -0 "$client_pid" 2>/dev/null; do
    if [ "$SECONDS" -ge "$deadline" ]; then
        echo "Client lifetime limit of $SWLOR_LIVE_MAX_SECONDS seconds reached" >&2
        exit 124
    fi
    sleep 1
done
status=0
wait "$client_pid" || status=$?
exit "$status"
