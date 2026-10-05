#!/usr/bin/env bash
set -Eeuo pipefail
umask 0027

die() { printf 'Production deployment: %s\n' "$*" >&2; exit 1; }
log() { printf '%s %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*"; }
usage() {
    cat <<'HELP'
Usage: swlor-production-deploy --check
       swlor-production-deploy --prepare --commit FULL_SHA --hash NWSYNC_HASH
       swlor-production-deploy --verify  --commit FULL_SHA --hash NWSYNC_HASH
       swlor-production-deploy --deploy  --commit FULL_SHA --hash NWSYNC_HASH
--check reads host prerequisites and configuration only.
--prepare builds and validates an isolated release; it never changes live files.
--verify checks an already prepared release without deploying it.
--deploy requires DEPLOYMENTS_ENABLED=true and a verified backup hook.
Failed cutovers remain stopped for coordinated data recovery. No automatic
restart of old code against potentially migrated Redis or character files.
HELP
}
mode='' commit='' manifest=''
while (( $# )); do
    case "$1" in
        --help) usage; exit 0 ;;
        --check|--prepare|--verify|--deploy)
            [[ -z "$mode" ]] || die "Specify exactly one mode."
            mode="${1#--}"; shift ;;
        --commit|--hash)
            (( $# >= 2 )) || die "Missing value for $1."
            if [[ "$1" == --commit ]]; then commit="$2"; else manifest="$2"; fi
            shift 2 ;;
        *) die "Unknown argument: $1" ;;
    esac
done
[[ -n "$mode" ]] || { usage; exit 64; }
if [[ "$mode" != check ]]; then
    [[ "$commit" =~ ^[0-9a-f]{40}$ ]] || die "Commit must be a full lowercase SHA."
    [[ "$manifest" =~ ^[0-9a-f]{40}$ ]] || die "NWSync hash must be 40 lowercase hexadecimal characters."
fi
(( EUID == 0 )) || die "Run as root."
trusted_file() {
    [[ -f "$1" && ! -L "$1" ]] || die "Not a regular file: $1"
    [[ "$(stat -c '%u' "$1")" == 0 ]] || die "File is not root-owned: $1"
    local permissions
    permissions="$(stat -c '%a' "$1")"
    (( (8#$permissions & 0022) == 0 )) || die "File is writable by group or others: $1"
    [[ "$(realpath -m "$1")" == "$1" ]] || die "Symlink in file path: $1"
}
config="${SWLOR_PRODUCTION_CONFIG:-/etc/swlor-production-deploy.conf}"
trusted_file "$config"
# shellcheck source=/dev/null
source "$config"
for setting in SERVER_ROOT COMPOSE_FILE COMPOSE_PROJECT_NAME SERVER_SERVICE REDIS_SERVICE \
    SERVER_ENV_FILE SETTINGS_FILE NWSYNC_URL MODULE_NAME SERVER_RUNTIME_UID SERVER_RUNTIME_GID \
    DEPLOYMENT_ROOT SOURCE_ROOT STATE_ROOT CACHE_ROOT RELEASE_ROOT BACKUP_ROOT REPOSITORY_URL \
    RELEASE_BRANCH SDK_IMAGE BUILD_CPUS BUILD_MEMORY BUILD_SCRIPT LOCK_FILE MIN_FREE_GIB \
    NEVERWINTER_NIM_RELEASE_URL NEVERWINTER_NIM_SHA256 STOP_TIMEOUT_SECONDS \
    HEALTH_TIMEOUT_SECONDS HEALTH_STABLE_SECONDS HEALTH_LOG_MARKER HEALTH_FATAL_LOG_PATTERN; do
    [[ -n "${!setting:-}" ]] || die "Missing configuration: $setting"
done
[[ "${DEPLOYMENTS_ENABLED:-false}" == true || "${DEPLOYMENTS_ENABLED:-false}" == false ]] ||
    die "DEPLOYMENTS_ENABLED must be true or false."
if [[ "$mode" == deploy ]]; then
    [[ "$DEPLOYMENTS_ENABLED" == true ]] || die "Production deployments are disabled."
    [[ -n "${BACKUP_COMMAND:-}" ]] || die "A coordinated, verified backup hook is required."
    trusted_file "$BACKUP_COMMAND"
    [[ -x "$BACKUP_COMMAND" ]] || die "Backup hook is not executable."
    [[ ! -e "$STATE_ROOT/recovery-required" ]] || die "Recovery is required from a previous failed cutover."
fi
for command in awk chmod chown cmp cp curl date df docker du find findmnt flock git grep \
    install jq ln mktemp mv realpath sed sha256sum sleep sort stat tail tar tr unzip wc xargs; do
    command -v "$command" >/dev/null || die "Missing host command: $command"
done
for setting in SERVER_RUNTIME_UID SERVER_RUNTIME_GID MIN_FREE_GIB STOP_TIMEOUT_SECONDS \
    HEALTH_TIMEOUT_SECONDS HEALTH_STABLE_SECONDS BUILD_CPUS; do
    [[ "${!setting}" =~ ^[0-9]+$ ]] || die "$setting must be an integer."
done
(( SERVER_RUNTIME_UID > 0 && SERVER_RUNTIME_GID > 0 && BUILD_CPUS > 0 && HEALTH_TIMEOUT_SECONDS > 0 )) ||
    die "Runtime identity, build CPUs, and health timeout must be positive."
[[ "$BUILD_MEMORY" =~ ^[1-9][0-9]*[mg]$ ]] || die "Invalid BUILD_MEMORY."
[[ "$SDK_IMAGE" =~ ^mcr.microsoft.com/dotnet/sdk:[A-Za-z0-9.-]+@sha256:[0-9a-f]{64}$ ]] ||
    die "SDK_IMAGE must be an official Microsoft SDK pinned by digest."
[[ "$NEVERWINTER_NIM_SHA256" =~ ^[0-9a-f]{64}$ ]] || die "Invalid packaging tool checksum."
[[ "$REPOSITORY_URL" == https://github.com/zunath/SWLOR_NWN ]] || die "Unexpected source repository."
[[ "$RELEASE_BRANCH" == master ]] || die "Production releases must be on master."
[[ "$NWSYNC_URL" == https://nwsync2.starwarsnwn.com ]] || die "Unexpected public NWSync URL."
[[ "$MODULE_NAME" == 'Star Wars LOR v2.mod' ]] || die "Unexpected module name."
for name in COMPOSE_PROJECT_NAME SERVER_SERVICE REDIS_SERVICE; do
    [[ "${!name}" =~ ^[a-z0-9][a-z0-9_-]*$ ]] || die "Invalid $name."
done
for path in "$SERVER_ROOT" "$DEPLOYMENT_ROOT" "$SOURCE_ROOT" "$STATE_ROOT" "$CACHE_ROOT" \
    "$RELEASE_ROOT" "$BACKUP_ROOT" "$COMPOSE_FILE" "$SERVER_ENV_FILE" "$SETTINGS_FILE" \
    "$BUILD_SCRIPT" "$LOCK_FILE"; do
    [[ "$path" =~ ^/[A-Za-z0-9/._-]+$ && "$path" != / ]] || die "Unsafe configured path: $path"
    [[ "$(realpath -m "$path")" == "$path" ]] || die "Symlink or noncanonical path: $path"
done
[[ "$DEPLOYMENT_ROOT" != "$SERVER_ROOT" && "$DEPLOYMENT_ROOT" != "$SERVER_ROOT/"* &&
   "$SERVER_ROOT" != "$DEPLOYMENT_ROOT/"* ]] || die "Preparation and live roots must be separate."
for path in "$SOURCE_ROOT" "$STATE_ROOT" "$CACHE_ROOT" "$RELEASE_ROOT" "$BACKUP_ROOT"; do
    [[ "$path" == "$DEPLOYMENT_ROOT/"* ]] || die "Deployment path escapes preparation root: $path"
done
deployment_paths=("$SOURCE_ROOT" "$STATE_ROOT" "$CACHE_ROOT" "$RELEASE_ROOT" "$BACKUP_ROOT")
for (( i=0; i<${#deployment_paths[@]}; i++ )); do
    for (( j=i+1; j<${#deployment_paths[@]}; j++ )); do
        [[ "${deployment_paths[i]}" != "${deployment_paths[j]}" &&
           "${deployment_paths[i]}" != "${deployment_paths[j]}/"* &&
           "${deployment_paths[j]}" != "${deployment_paths[i]}/"* ]] || die "Overlapping deployment paths."
    done
done
[[ "$COMPOSE_FILE" == "$SERVER_ROOT/"* && "$SERVER_ENV_FILE" == "$SERVER_ROOT/"* &&
   "$SETTINGS_FILE" == "$SERVER_ROOT/"* && "$LOCK_FILE" == "$STATE_ROOT/"* ]] || die "Unsafe configuration location."
[[ -d "$SERVER_ROOT" && ! -L "$SERVER_ROOT" ]] || die "Live server directory is unavailable."
[[ -f "$SERVER_ENV_FILE" && -f "$SETTINGS_FILE" && -f "$COMPOSE_FILE" ]] || die "Live host configuration is missing."
trusted_file "$BUILD_SCRIPT"
compose() {
    docker compose --project-directory "$SERVER_ROOT" --project-name "$COMPOSE_PROJECT_NAME" \
        --file "$COMPOSE_FILE" "$@"
}
configured_url="$(sed -n 's/^NWN_NWSYNCURL=//p' "$SERVER_ENV_FILE" | tr -d '\r' | sed 's/[[:space:]]*$//')"
[[ "$configured_url" == "$NWSYNC_URL" ]] || die "Verify the host's NWN_NWSYNCURL before preparation."
for directory in hak tlk modules dotnet; do
    [[ -d "$SERVER_ROOT/$directory" && ! -L "$SERVER_ROOT/$directory" ]] || die "Invalid live artifact path: $directory"
    [[ "$(realpath "$SERVER_ROOT/$directory")" == "$SERVER_ROOT/$directory" ]] || die "Symlink in live artifact path."
    [[ "$(findmnt -T "$SERVER_ROOT/$directory" -n -o TARGET)" != "$SERVER_ROOT/$directory" ]] || die "Artifact directory is a mount point."
done
compose config --quiet
if [[ "$mode" == check ]]; then
    docker info --format '{{.OSType}}' | awk '$0 != "linux" {exit 1}'
    log "Read-only prerequisites passed. Compose project=$COMPOSE_PROJECT_NAME; deployments=$DEPLOYMENTS_ENABLED."
    exit 0
fi
install -d -o root -g root -m 0750 "$DEPLOYMENT_ROOT" "$STATE_ROOT" "$CACHE_ROOT" "$RELEASE_ROOT" "$BACKUP_ROOT"
exec 9>"$LOCK_FILE"
flock --nonblock 9 || die "Another production deployment is running."
if [[ "$mode" == deploy ]]; then
    [[ ! -e "$STATE_ROOT/recovery-required" ]] || die "Recovery is required from a previous failed cutover."
fi
[[ "$(stat -c '%d' "$SERVER_ROOT")" == "$(stat -c '%d' "$RELEASE_ROOT")" ]] || die "Staging and server must share a filesystem."
available="$(df --output=avail -B1 "$DEPLOYMENT_ROOT" | tail -n 1 | tr -d '[:space:]')"
(( available >= MIN_FREE_GIB * 1024 * 1024 * 1024 )) || die "Insufficient staging space."
release="$RELEASE_ROOT/$commit-$manifest"
release_checksums() {
    (cd "$release"; find artifacts -type f -print0 | sort -z | xargs -0 sha256sum; sha256sum swlor.env settings.tml ready.json)
}
verify_release() {
    [[ -f "$release/ready.json" && ! -L "$release" ]] || die "Release has not finished preparation."
    trusted_file "$release/ready.json"
    trusted_file "$release/SHA256SUMS"
    jq -e --arg commit "$commit" --arg manifest "$manifest" \
        '.commit == $commit and .nwsync_hash == $manifest' "$release/ready.json" >/dev/null || die "Release inputs do not match."
    [[ -z "$(find "$release/artifacts" -type l -print -quit)" ]] || die "Symlink in staged artifacts."
    (cd "$release"; sha256sum --check --strict --quiet SHA256SUMS)
    cmp --silent "$release/SHA256SUMS" <(release_checksums) || die "Staged file inventory or checksums changed."
    for group in hak tlk modules dotnet; do
        [[ "$(stat -c '%d' "$SERVER_ROOT/$group")" == "$(stat -c '%d' "$release/artifacts/$group")" ]] || die "Artifact filesystem changed."
    done
}
if [[ "$mode" == verify ]]; then verify_release; log "Prepared release checksums passed."; exit 0; fi

if [[ ! -f "$release/ready.json" ]]; then
    [[ ! -e "$release" ]] || die "Incomplete release exists at $release; preserve it for investigation."
    install -d -o root -g root -m 0750 "$release" "$release/artifacts" "$release/build" "$CACHE_ROOT/nuget"
    if [[ ! -d "$SOURCE_ROOT" ]]; then
        git clone --branch "$RELEASE_BRANCH" "$REPOSITORY_URL" "$SOURCE_ROOT"
    fi
    [[ -d "$SOURCE_ROOT/.git" && ! -L "$SOURCE_ROOT" ]] || die "Dedicated source is not a regular checkout."
    [[ -z "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]] || die "Dedicated source has local changes."
    [[ "$(git -C "$SOURCE_ROOT" remote get-url origin)" == "$REPOSITORY_URL" ]] || die "Source remote differs."
    git -C "$SOURCE_ROOT" fetch origin "+refs/heads/$RELEASE_BRANCH:refs/remotes/origin/$RELEASE_BRANCH"
    git -C "$SOURCE_ROOT" cat-file -e "$commit^{commit}" || die "Requested commit is unavailable."
    git -C "$SOURCE_ROOT" merge-base --is-ancestor "$commit" "origin/$RELEASE_BRANCH" || die "Requested commit is not on master."
    git -C "$SOURCE_ROOT" checkout --detach "$commit"
    git -C "$SOURCE_ROOT" submodule sync --recursive
    git -C "$SOURCE_ROOT" submodule update --init --recursive --depth 1
    pinned_haks="$(git -C "$SOURCE_ROOT" rev-parse "$commit:SWLOR_Haks")"
    [[ "$(git -C "$SOURCE_ROOT/SWLOR_Haks" rev-parse HEAD)" == "$pinned_haks" ]] || die "HAKs are not pinned to the requested commit."
    # Use the exact gitlink, even if master has advanced since release testing.
    git -C "$SOURCE_ROOT/SWLOR_Haks" ls-files -z '*.set' |
        git -C "$SOURCE_ROOT/SWLOR_Haks" checkout-index --force --stdin -z
    [[ -z "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]] || die "Source is dirty after checkout."
    tools="$CACHE_ROOT/tools-$NEVERWINTER_NIM_SHA256"
    if [[ ! -d "$tools" ]]; then
        tools_work="$(mktemp -d "$CACHE_ROOT/tools.XXXXXXXX")"
        curl --fail --show-error --location --connect-timeout 10 --max-time 180 \
            "$NEVERWINTER_NIM_RELEASE_URL" --output "$tools_work/tools.zip"
        printf '%s  %s\n' "$NEVERWINTER_NIM_SHA256" "$tools_work/tools.zip" | sha256sum --check --strict
        unzip -q "$tools_work/tools.zip" -d "$tools_work/unpacked"
        install -d -o root -g root -m 0750 "$tools"
        for tool in nwn_erf nwn_gff nwn_tlk; do
            install -m 0755 "$tools_work/unpacked/$tool" "$tools/$tool"
        done
    fi
    jq --arg source /src --arg output /artifacts/ \
        '.OutputPath = $output | .TlkPath = ($source + "/" + (.TlkPath | sub("^\\.\\./"; "")))
         | .HakList |= map(if . == null then . else .Path = ($source + "/" + (.Path | sub("^\\.\\./"; ""))) end)' \
        "$SOURCE_ROOT/Build/hakbuilder.json" > "$release/build/hakbuilder.json"
    docker pull "$SDK_IMAGE"
    log "Building $commit in an isolated SDK container with CPU and memory limits."
    docker run --rm --init --cap-drop ALL --security-opt no-new-privileges \
        --cpus "$BUILD_CPUS" --memory "$BUILD_MEMORY" --pids-limit 512 \
        --mount "type=bind,src=$SOURCE_ROOT,dst=/src,readonly" \
        --mount "type=bind,src=$release/build,dst=/work" \
        --mount "type=bind,src=$release/artifacts,dst=/artifacts" \
        --mount "type=bind,src=$tools,dst=/tools,readonly" \
        --mount "type=bind,src=$CACHE_ROOT/nuget,dst=/nuget" \
        --mount "type=bind,src=$BUILD_SCRIPT,dst=/build.sh,readonly" \
        "$SDK_IMAGE" bash /build.sh "$MODULE_NAME"
    jq -r '.HakList[] | select(. != null) | .Name' "$SOURCE_ROOT/Build/hakbuilder.json" | sort > "$release/expected-haks"
    find "$release/artifacts/hak" -maxdepth 1 -type f -name '*.hak' -printf '%f\n' | sed 's/\.hak$//' | sort > "$release/actual-haks"
    cmp "$release/expected-haks" "$release/actual-haks" || die "Staged HAK set does not match the build configuration."
    [[ "$(sort -u "$release/expected-haks" | wc -l)" == "$(wc -l < "$release/expected-haks")" ]] || die "Duplicate HAK names."
    for artifact in "modules/$MODULE_NAME" tlk/sw_tlk.tlk dotnet/SWLOR.Game.Server.dll dotnet/SWLOR.Game.Server.runtimeconfig.json; do
        [[ -s "$release/artifacts/$artifact" ]] || die "Missing staged artifact: $artifact"
    done
    [[ -z "$(find "$release/artifacts" -type f -empty -print -quit)" ]] || die "Empty staged artifact."
    jq -e '.runtimeOptions.tfm == "net10.0"' "$release/artifacts/dotnet/SWLOR.Game.Server.runtimeconfig.json" >/dev/null
    image="$(tr -d '\r\n' < "$SOURCE_ROOT/scripts/deployment/server-image.txt")"
    [[ "$image" =~ ^zunath/nwn-dotnet:[0-9][A-Za-z0-9.-]+$ ]] || die "Invalid release server image."
    docker pull "$image"
    [[ "$(docker image inspect --format '{{.Config.User}}' "$image")" == "$SERVER_RUNTIME_UID:$SERVER_RUNTIME_GID" ]] || die "Server image runtime identity differs."
    image_id="$(docker image inspect --format '{{.Id}}' "$image")"
    cp -a "$SERVER_ENV_FILE" "$release/swlor.env"
    cp -a "$SETTINGS_FILE" "$release/settings.tml"
    write_env() {
        local key="$1" value="$2" temporary="$release/env.tmp"
        awk -v key="$key" -v value="$value" 'index($0,key "=") != 1 {print} END {print key "=" value}' "$release/swlor.env" > "$temporary"
        mv "$temporary" "$release/swlor.env"
    }
    write_env NWN_NWSYNCHASH "$manifest"
    write_env SWLOR_ENVIRONMENT production
    write_env SWLOR_ENGINE_TESTS_ENABLED false
    write_env NWN_ELC 0
    write_env NWN_ILR 0
    write_env NWNX_DOTNET_SKIP n
    write_env NWNX_DOTNET_NEW_BOOTSTRAP 1
    write_env NWNX_DOTNET_ASSEMBLY /nwn/home/dotnet/SWLOR.Game.Server
    write_env NWNX_DOTNET_ENTRYPOINT SWLOR.Game.Server.Core.ServerManager
    write_env NWNX_DOTNET_METHOD Bootstrap
    write_env NWNX_UTIL_PRE_MODULE_START_SCRIPT mod_preload
    write_env NWNX_RENAME_SKIP n
    write_env NWNX_RENAME_ON_MODULE_CHAR_LIST true
    write_env NWNX_RENAME_ON_PLAYER_LIST true
    write_env NWNX_RENAME_OVERWRITE_DISPLAY_NAME false
    write_env NWNX_TWEAKS_HIDE_PLAYERS_ON_CHAR_LIST 3
    write_env NWNX_TWEAKS_MATERIAL_NAME_NULL_IS_ALL true
    write_setting() {
        local section="$1" key="$2" value="$3"
        awk -v section="$section" -v key="$key" -v value="$value" '
            function finish() {if (inside && !written) {print "\t" key " = " value; written=1}}
            /^[[:space:]]*\[/ {finish(); inside=($0 ~ "^[[:space:]]*\\[" section "\\][[:space:]]*$"); if(inside) found++}
            inside && $0 ~ "^[[:space:]]*" key "[[:space:]]*=" {if (written) exit 1; print "\t" key " = " value; written=1; next}
            {print}
            END {finish(); if(!found) print "[" section "]\n\t" key " = " value; if(found>1) exit 1}
        ' "$release/settings.tml" > "$release/settings.tmp"
        mv "$release/settings.tmp" "$release/settings.tml"
    }
    write_setting ruleset enforce-legal-characters false
    write_setting server player-party-control true
    jq -n --arg commit "$commit" --arg manifest "$manifest" --arg image "$image" --arg image_id "$image_id" \
        --arg sdk "$SDK_IMAGE" --arg haks "$pinned_haks" \
        --arg env_sha "$(sha256sum "$SERVER_ENV_FILE" | awk '{print $1}')" \
        --arg settings_sha "$(sha256sum "$SETTINGS_FILE" | awk '{print $1}')" \
        --arg compose_sha "$(sha256sum "$COMPOSE_FILE" | awk '{print $1}')" \
        '{commit:$commit,nwsync_hash:$manifest,image:$image,image_id:$image_id,sdk:$sdk,haks:$haks,
          original_env_sha:$env_sha,original_settings_sha:$settings_sha,original_compose_sha:$compose_sha}' > "$release/ready.json"
    release_checksums > "$release/SHA256SUMS"
    chmod 0640 "$release/swlor.env" "$release/settings.tml" "$release/ready.json" "$release/SHA256SUMS"
fi
verify_release
if [[ "$mode" == prepare ]]; then log "Release is prepared at $release. Live server files and containers were untouched."; exit 0; fi

# Every check above runs before any service stop or live file change.
for pair in "$SERVER_ENV_FILE|original_env_sha" "$SETTINGS_FILE|original_settings_sha" "$COMPOSE_FILE|original_compose_sha"; do
    IFS='|' read -r file key <<< "$pair"
    [[ "$(sha256sum "$file" | awk '{print $1}')" == "$(jq -r ".$key" "$release/ready.json")" ]] || die "Host configuration changed since preparation: $file"
done
image_id="$(jq -r '.image_id' "$release/ready.json")"
[[ "$(docker image inspect --format '{{.Id}}' "$image_id")" == "$image_id" ]] || die "Prepared runtime image is unavailable."
# Check runtime paths before downtime, then recheck before permission changes.
for directory in app_logs database development logs nwsync override portraits saves servervault; do
    path="$SERVER_ROOT/$directory"
    [[ "$(realpath -m "$path")" == "$path" && ! -L "$path" ]] || die "Unsafe runtime path: $path"
    [[ ! -e "$path" || -d "$path" ]] || die "Runtime path is not a directory: $path"
done
for file in cryptographic_secret nwn.ini nwnplayer.ini; do
    path="$SERVER_ROOT/$file"
    [[ ! -e "$path" && ! -L "$path" ]] && continue
    [[ -f "$path" && ! -L "$path" && "$(realpath -m "$path")" == "$path" ]] || die "Unsafe runtime configuration: $path"
done
# Backup hook preflight must be read-only and succeed with all writers running.
"$BACKUP_COMMAND" --check
backup="$BACKUP_ROOT/$(date -u +%Y%m%dT%H%M%SZ)-$commit"
[[ ! -e "$backup" ]] || die "Backup directory already exists."
install -d -o root -g root -m 0750 "$backup"
cp -a "$SERVER_ENV_FILE" "$backup/swlor.env"
cp -a "$SETTINGS_FILE" "$backup/settings.tml"
cp -a "$COMPOSE_FILE" "$backup/docker-compose.yml"
cp "$release/ready.json" "$backup/release.json"
old_container="$(compose ps -q "$SERVER_SERVICE")"
[[ -n "$old_container" ]] || die "Expected a running game server before cutover."
docker inspect --format '{{.Image}}' "$old_container" > "$backup/previous-image-id"
cutover_started=0
cleanup() {
    local code=$?
    trap - EXIT INT TERM
    if (( code != 0 && cutover_started == 1 )); then
        set +e
        cleanup_confirmed=1
        compose stop --timeout "$STOP_TIMEOUT_SECONDS" "$SERVER_SERVICE" || cleanup_confirmed=0
        # A restart policy or host reboot must not restart code on uncertain data.
        failed_container="$(compose ps --all -q "$SERVER_SERVICE")"
        if [[ -n "$failed_container" ]]; then
            docker update --restart=no "$failed_container" >/dev/null || cleanup_confirmed=0
            [[ "$(docker inspect --format '{{.State.Running}}' "$failed_container")" == false ]] || cleanup_confirmed=0
        else
            cleanup_confirmed=0
        fi
        printf '%s\n' "$backup" > "$STATE_ROOT/recovery-required"
        if (( cleanup_confirmed == 1 )); then
            log "Cutover failed. Game is confirmed stopped with automatic restart disabled."
        else
            log "CRITICAL: Could not confirm the game stopped with restart disabled. Inspect its container immediately."
        fi
        log "Deployment is blocked for recovery. Preserve $backup and restore matching persistent data before restarting any code."
    fi
    exit "$code"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
cutover_started=1
compose stop --timeout "$STOP_TIMEOUT_SECONDS" "$SERVER_SERVICE"
[[ "$(docker inspect --format '{{.State.Running}}' "$old_container")" == false ]] || die "Game server did not stop."
# The hook must account for all other writers and verify a restoreable snapshot.
# No bundled backup implementation is assumed safe for an uninspected host.
"$BACKUP_COMMAND" "$backup"
[[ -s "$backup/backup-verified" ]] || die "Backup hook did not confirm a verified coordinated snapshot."
for group in hak tlk modules dotnet; do
    mv "$SERVER_ROOT/$group" "$backup/$group"
    mv "$release/artifacts/$group" "$SERVER_ROOT/$group"
    chown -R root:"$SERVER_RUNTIME_GID" "$SERVER_ROOT/$group"
    chmod -R u=rwX,g=rX,o= "$SERVER_ROOT/$group"
done
install -o root -g root -m 0640 "$release/swlor.env" "$SERVER_ENV_FILE"
install -o root -g "$SERVER_RUNTIME_GID" -m 0640 "$release/settings.tml" "$SETTINGS_FILE"
# Reject paths that could make permission preparation affect unrelated data.
for directory in app_logs database development logs nwsync override portraits saves servervault; do
    path="$SERVER_ROOT/$directory"
    [[ "$(realpath -m "$path")" == "$path" && ! -L "$path" ]] || die "Unsafe runtime path: $path"
    [[ ! -e "$path" || -d "$path" ]] || die "Runtime path is not a directory: $path"
    install -d -o "$SERVER_RUNTIME_UID" -g "$SERVER_RUNTIME_GID" -m 0750 "$path"
    find "$path" -xdev \( -type f -o -type d \) -exec chown "$SERVER_RUNTIME_UID:$SERVER_RUNTIME_GID" -- {} +
    find "$path" -xdev -type d -exec chmod u+rwx -- {} +
    find "$path" -xdev -type f -exec chmod u+rw -- {} +
done
chown root:"$SERVER_RUNTIME_GID" "$SERVER_ROOT"
chmod 0750 "$SERVER_ROOT"
for file in cryptographic_secret nwn.ini nwnplayer.ini; do
    path="$SERVER_ROOT/$file"
    [[ ! -e "$path" && ! -L "$path" ]] && continue
    [[ -f "$path" && ! -L "$path" ]] || die "Unsafe runtime configuration: $path"
    chown root:"$SERVER_RUNTIME_GID" "$path"
    chmod 0640 "$path"
done
override="$STATE_ROOT/runtime.override.yml"
{
    printf 'services:\n  %s:\n    image: "%s"\n    user: "%s:%s"\n' "$SERVER_SERVICE" "$image_id" "$SERVER_RUNTIME_UID" "$SERVER_RUNTIME_GID"
    printf '    volumes:\n'
    for group in hak tlk modules dotnet; do
        printf '      - "%s/%s:/nwn/home/%s:ro"\n' "$SERVER_ROOT" "$group" "$group"
    done
} > "$override"
chmod 0640 "$override"
started_at="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
# Target only the game service. Preserve Redis and monitoring containers/images.
compose --file "$override" up -d --no-deps --no-build --force-recreate "$SERVER_SERVICE"
container="$(compose ps -q "$SERVER_SERVICE")"
[[ -n "$container" ]] || die "No replacement server container."
deadline=$(( SECONDS + HEALTH_TIMEOUT_SECONDS ))
loaded=0
while (( SECONDS < deadline )); do
    [[ "$(docker inspect --format '{{.State.Running}} {{.RestartCount}}' "$container")" == 'true 0' ]] || die "Server exited or restarted."
    logs="$(docker logs --since "$started_at" "$container" 2>&1)"
    if grep -Eiq "$HEALTH_FATAL_LOG_PATTERN" <<< "$logs"; then die "Fatal startup error in server logs."; fi
    if grep -Fq "$HEALTH_LOG_MARKER" <<< "$logs"; then loaded=1; break; fi
    sleep 5
done
(( loaded == 1 )) || die "Module load timed out."
deadline=$(( SECONDS + HEALTH_STABLE_SECONDS ))
while (( SECONDS < deadline )); do
    [[ "$(docker inspect --format '{{.State.Running}} {{.RestartCount}}' "$container")" == 'true 0' ]] || die "Server failed its stability check."
    logs="$(docker logs --since "$started_at" "$container" 2>&1)"
    if grep -Eiq "$HEALTH_FATAL_LOG_PATTERN" <<< "$logs"; then die "Fatal error during stability check."; fi
    sleep 5
done
printf '%s %s\n' "$commit" "$manifest" > "$STATE_ROOT/active-release"
cutover_started=0
log "Production release passed startup checks. Recovery artifacts remain at $backup."
