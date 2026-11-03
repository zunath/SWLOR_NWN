#!/bin/bash
set -euo pipefail

# Bind mounts hide ownership established in the image. Prepare only runtime data;
# do not recursively change the content, Redis, or monitoring directories.
cd /nwn/home
stamp=.server-home-prepared
for path in app_logs database development logs nwsync override portraits saves servervault \
            cryptographic_secret nwn.ini nwnplayer.ini settings.tml "$stamp"; do
    if test -L "$path"; then
        echo "Refusing symbolic link in writable server home: $path" >&2
        exit 1
    fi
done

chown 1000:1000 /nwn/home
chmod u+rwx /nwn/home

# Walking every file over a Docker Desktop for Windows bind mount (v9fs) takes over a
# minute. There, after one full pass, only the top-level directories are checked; the
# server creates everything else as 1000:1000. Delete the stamp to force a full pass.
depth=()
if test "$(stat -f -c %T .)" = v9fs && test -f "$stamp"; then
    depth=(-maxdepth 0)
fi

for path in app_logs database development logs nwsync override portraits saves servervault; do
    mkdir -p "$path"
    # Never follow links inside an existing data directory into another mount.
    find "$path" "${depth[@]}" -xdev -type d \( ! -uid 1000 -o ! -gid 1000 -o ! -perm -0700 \) -exec chown 1000:1000 {} + -exec chmod u+rwx {} +
    find "$path" "${depth[@]}" -xdev -type f \( ! -uid 1000 -o ! -gid 1000 -o ! -perm -0600 \) -exec chown 1000:1000 {} + -exec chmod u+rw {} +
done
for path in cryptographic_secret nwn.ini nwnplayer.ini settings.tml; do
    if test -f "$path"; then
        chown 1000:1000 "$path"
        chmod u+rw "$path"
    fi
done
touch "$stamp"
