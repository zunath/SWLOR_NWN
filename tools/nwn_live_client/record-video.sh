#!/bin/bash
# Bounded capture of the single NWN window on the container's private display.
set -eu
die() { echo "$*" >&2; exit 1; }
[ $# -eq 3 ] || die "Usage: record-video.sh LABEL SECONDS FPS"
[[ "$1" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$ ]] || die "Invalid video label"
[[ "$2" =~ ^[0-9]+$ ]] && [ "$2" -ge 1 ] && [ "$2" -le 120 ] || die "Duration must be 1..120 seconds"
[[ "$3" =~ ^(15|30|60)$ ]] || die "Frame rate must be 15, 30 or 60"
out="/verify/$1.mp4"
partial="/verify/.$1.recording.mp4"
[ ! -e "$out" ] && [ ! -e "$partial" ] && [ ! -e "/verify/$1.video.json" ] || die "Video label already exists"
pid=$(cat /tmp/client.pid)
mapfile -t windows < <(xdotool search --onlyvisible --pid "$pid" 2>/dev/null || true)
[ "${#windows[@]}" -eq 1 ] || die "Expected one visible NWN client window"
w=${windows[0]}
xdotool windowfocus --sync "$w"
# Leave failed acquisition/probe output for the host to quarantine with its logs.
# A dedicated input queue keeps encoder work from delaying X11 acquisition.
# No audio input, host desktop capture, interpolation, or synthetic motion.
timeout --signal=INT --kill-after=5 "$(($2 + 30))" ffmpeg -hide_banner -loglevel info -nostdin -n \
    -filter_threads 1 -thread_queue_size 120 -f x11grab -draw_mouse 0 -framerate "$3" \
    -window_id "$w" -video_size "${SWLOR_LIVE_WINDOW_WIDTH:-1280}x${SWLOR_LIVE_WINDOW_HEIGHT:-720}" -i "$DISPLAY" -t "$2" -an \
    -c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p -r "$3" \
    -threads 2 -movflags +faststart "$partial" \
    -map 0:v:0 -t "$2" -an -c:v rawvideo -pix_fmt bgr0 -threads 1 \
    -fps_mode passthrough -enc_time_base 1:1000000 -f framemd5 "/verify/$1.frames.md5" \
    2>"/verify/$1.ffmpeg.log"
ffprobe -v error -count_frames -select_streams v:0 \
    -show_entries stream=codec_name,width,height,r_frame_rate,avg_frame_rate,nb_read_frames,duration \
    -of json "$partial" > "/verify/$1.video.json"
mv -- "$partial" "$out"
echo "$out"
