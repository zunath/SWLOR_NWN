#!/bin/bash
# Input and capture helpers for the tools/nwn_live_client client container. Every action
# targets the single client window on the container's private display :99, so nothing
# here touches the host's desktop, focus, mouse, or keyboard.
set -eu

die() { echo "$*" >&2; exit 1; }

client_window() {
    local pid
    pid=$(cat /tmp/client.pid 2>/dev/null) || die "The client is not running"
    mapfile -t windows < <(xdotool search --onlyvisible --pid "$pid" 2>/dev/null || true)
    [ "${#windows[@]}" -eq 1 ] || return 1
    echo "${windows[0]}"
}

window() { client_window || die "Expected one visible client window"; }

coordinate() {
    [[ "$1" =~ ^[0-9]+$ ]] && [ "$1" -lt "$2" ] || die "Coordinate $1 is outside 0..$(($2 - 1))"
}

point() { coordinate "$1" 1280; coordinate "$2" 720; }

press() {
    local w=$1 x=$2 y=$3 button=$4
    xdotool windowfocus --sync "$w"
    xdotool mousemove --window "$w" "$x" "$y"
    sleep 0.25
    xdotool mousedown "$button"
    sleep 0.25
    xdotool mouseup "$button"
}

action=${1:-}
[ -n "$action" ] || die "Usage: ui.sh <action> [arguments]"
shift

case "$action" in
    wait-window)
        timeout=${1:-120}
        for _ in $(seq 1 "$timeout"); do
            client_window > /dev/null && exit 0
            sleep 1
        done
        die "No client window after $timeout seconds"
        ;;
    move)
        [ $# -eq 2 ] || die "Usage: move X Y"
        point "$1" "$2"; w=$(window)
        xdotool windowfocus --sync "$w"
        xdotool mousemove --window "$w" "$1" "$2"
        sleep 1
        ;;
    click|rightclick)
        [ $# -eq 2 ] || die "Usage: $action X Y"
        point "$1" "$2"; w=$(window)
        button=1; [ "$action" = rightclick ] && button=3
        press "$w" "$1" "$2" "$button"
        sleep 1
        ;;
    doubleclick)
        [ $# -eq 2 ] || die "Usage: doubleclick X Y"
        point "$1" "$2"; w=$(window)
        xdotool windowfocus --sync "$w"
        xdotool mousemove --window "$w" "$1" "$2"
        sleep 0.25
        xdotool click --repeat 2 --delay 120 1
        sleep 1
        ;;
    drag)
        [ $# -eq 4 ] || die "Usage: drag X1 Y1 X2 Y2"
        point "$1" "$2"; point "$3" "$4"; w=$(window)
        xdotool windowfocus --sync "$w"
        xdotool mousemove --window "$w" "$1" "$2"
        sleep 0.25
        xdotool mousedown 1
        sleep 0.25
        xdotool mousemove --window "$w" "$3" "$4"
        sleep 0.25
        xdotool mouseup 1
        sleep 1
        ;;
    key)
        [ $# -eq 1 ] || die "Usage: key KEYSYM (for example Return, Escape, ctrl+a)"
        w=$(window)
        xdotool windowfocus --sync "$w"
        [ "$(xdotool getwindowfocus)" = "$w" ] || die "Client window did not take focus"
        xdotool key --clearmodifiers --delay 250 "$1"
        sleep 1
        ;;
    type)
        [ $# -eq 1 ] || die "Usage: type TEXT"
        w=$(window)
        xdotool windowfocus --sync "$w"
        [ "$(xdotool getwindowfocus)" = "$w" ] || die "Client window did not take focus"
        xdotool type --clearmodifiers --delay 120 -- "$1"
        sleep 1
        ;;
    capture)
        [ $# -eq 1 ] || die "Usage: capture LABEL"
        [[ "$1" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$ ]] || die "Labels use letters, digits, '.', '_' and '-'"
        out="/verify/$1.png"
        [ ! -e "$out" ] || die "$out already exists; use a new label"
        w=$(window)
        sleep 0.3
        import -window "$w" "$out"
        echo "$out"
        ;;
    *)
        die "Unknown action: $action"
        ;;
esac
printf '%s %s %s\n' "$(date -u +%FT%TZ)" "$action" "$*" >> /verify/ui-actions.log
