#!/bin/bash
set -euo pipefail

# Bind mounts hide ownership established in the image. Prepare only runtime data;
# do not recursively change the content, Redis, or monitoring directories.
cd /nwn/home
for path in app_logs database development logs nwsync override portraits saves servervault \
            cryptographic_secret nwn.ini nwnplayer.ini settings.tml; do
    if test -L "$path"; then
        echo "Refusing symbolic link in writable server home: $path" >&2
        exit 1
    fi
done

chown 1000:1000 /nwn/home
chmod u+rwx /nwn/home
for path in app_logs database development logs nwsync override portraits saves servervault; do
    mkdir -p "$path"
    # Never follow links inside an existing data directory into another mount.
    find "$path" -xdev -type d \( ! -uid 1000 -o ! -gid 1000 -o ! -perm -0700 \) -exec chown 1000:1000 {} + -exec chmod u+rwx {} +
    find "$path" -xdev -type f \( ! -uid 1000 -o ! -gid 1000 -o ! -perm -0600 \) -exec chown 1000:1000 {} + -exec chmod u+rw {} +
done
for path in cryptographic_secret nwn.ini nwnplayer.ini settings.tml; do
    if test -f "$path"; then
        chown 1000:1000 "$path"
        chmod u+rw "$path"
    fi
done
