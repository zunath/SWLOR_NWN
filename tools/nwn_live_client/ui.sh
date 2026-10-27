#!/bin/bash
# Input and capture helpers for the tools/nwn_live_client client container. Every action
# targets the single client window on the container's private display :99, so nothing
# here touches the host's desktop, focus, mouse, or keyboard.
set -eu

die() { echo "$*" >&2; exit 1; }

client_window() {
    local pid
    # Not fatal here: wait-window polls while run-client.sh is still starting the client.
    pid=$(cat /tmp/client.pid 2>/dev/null) || return 1
    mapfile -t windows < <(xdotool search --onlyvisible --pid "$pid" 2>/dev/null || true)
    [ "${#windows[@]}" -eq 1 ] || return 1
    echo "${windows[0]}"
}

window() { client_window || die "Expected one visible client window; is the client running?"; }

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
    drag-slow)
        # Like drag, but with eight intermediate pointer moves: NUI windows and some inventory
        # drops ignore a drag that jumps straight to its end point.
        [ $# -eq 4 ] || die "Usage: drag-slow X1 Y1 X2 Y2"
        point "$1" "$2"; point "$3" "$4"; w=$(window)
        xdotool windowfocus --sync "$w"
        xdotool mousemove --window "$w" "$1" "$2"
        sleep 0.5
        xdotool mousedown 1
        # A short wiggle first: the client starts a drag only after the pointer has left the source.
        xdotool mousemove --window "$w" $(( $1 + 4 )) $(( $2 + 4 ))
        sleep 0.3
        for step in 1 2 3 4 5 6 7 8; do
            xdotool mousemove --window "$w" $(( $1 + ($3 - $1) * step / 8 )) $(( $2 + ($4 - $2) * step / 8 ))
            sleep 0.25
        done
        # Rest over the target before letting go, so it registers as the drop point.
        sleep 0.7
        xdotool mouseup 1
        sleep 1
        ;;
    hostile-scan)
        # Hovers each point in turn and prints the first one whose name label (just above the
        # pointer) is hostile red; prints nothing when none is. One call instead of a move and a
        # capture per point, which is what makes a wide search practical.
        [ $# -ge 2 ] && [ $(($# % 2)) -eq 0 ] || die "Usage: hostile-scan X Y [X Y ...]"
        w=$(window)
        xdotool windowfocus --sync "$w"
        while [ $# -ge 2 ]; do
            x=$1; y=$2; shift 2
            point "$x" "$y"
            xdotool mousemove --window "$w" "$x" "$y"
            sleep 0.6
            left=$(( x > 70 ? x - 70 : 0 )); top=$(( y > 45 ? y - 45 : 0 ))
            red=$(import -window "$w" -crop "140x50+$left+$top" -depth 8 txt:- | tr -d ' ' \
                | sed -n 's/^[0-9]*,[0-9]*:(\([0-9]*\),\([0-9]*\),\([0-9]*\).*/\1 \2 \3/p' \
                | awk '$1 > 190 && $2 < 120 && $3 < 120 { n++ } END { print n + 0 }')
            if [ "$red" -ge 40 ]; then
                echo "$x $y"
                break
            fi
        done
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
    pixel)
        # Prints "R G B" of one pixel; used to tell the loading screen from the game. Not logged.
        [ $# -eq 2 ] || die "Usage: pixel X Y"
        point "$1" "$2"; w=$(window)
        import -window "$w" -crop "1x1+$1+$2" -depth 8 txt:- | tr -d ' ' \
            | sed -n 's/^[0-9]*,[0-9]*:(\([0-9]*\),\([0-9]*\),\([0-9]*\).*/\1 \2 \3/p' | head -n 1
        exit 0
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
