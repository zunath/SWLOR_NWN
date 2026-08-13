#!/usr/bin/env bash

set -Eeuo pipefail
umask 0022

CONFIG_FILE="${SWLOR_DEPLOY_CONFIG:-/etc/swlor-deploy.conf}"
LOG_FILE="${SWLOR_DEPLOY_LOG:-/var/log/swlor-deploy.log}"
LOCK_FILE="${SWLOR_DEPLOY_LOCK:-/run/lock/swlor-deploy.lock}"

if [[ -f "$CONFIG_FILE" && ! -L "$CONFIG_FILE" && -r "$CONFIG_FILE" ]]; then
    if [[ "$(stat -c '%u' "$CONFIG_FILE")" != 0 ]]; then
        printf 'Configuration file must be owned by root: %s\n' "$CONFIG_FILE" >&2
        exit 78
    fi
    config_permissions="$(stat -c '%a' "$CONFIG_FILE")"
    if (( (8#$config_permissions & 0022) != 0 )); then
        printf 'Configuration file must not be writable by group or others: %s\n' \
            "$CONFIG_FILE" >&2
        exit 78
    fi

    # The configuration is root-owned and intentionally uses shell assignment
    # syntax so values such as MODULE_NAME can contain spaces.
    # shellcheck source=/dev/null
    source "$CONFIG_FILE"
else
    printf 'Configuration file is missing or unreadable: %s\n' "$CONFIG_FILE" >&2
    exit 78
fi

required_settings=(
    DEPLOYMENT_NAME SOURCE_ROOT REPOSITORY_URL GIT_REMOTE BRANCH
    NWSYNC_ROOT SERVER_ROOT COMPOSE_FILE STATE_ROOT CACHE_ROOT MODULE_NAME
    SERVER_SERVICE SERVER_IMAGE
    STOP_TIMEOUT_SECONDS HEALTH_TIMEOUT_SECONDS HEALTH_STABLE_SECONDS
    HEALTH_LOG_MARKER MIN_FREE_GIB_BEFORE_BUILD MIN_FREE_GIB_BEFORE_CUTOVER
    NEVERWINTER_NIM_VERSION NEVERWINTER_NIM_RELEASE_URL NEVERWINTER_NIM_SHA256
    PRUNE_DANGLING_DOCKER_IMAGES
)
for required_setting in "${required_settings[@]}"; do
    [[ -n "${!required_setting:-}" ]] || {
        printf 'Required configuration value is missing: %s\n' \
            "$required_setting" >&2
        exit 78
    }
done

COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-}"
HAKS_SUBMODULE_PATH="${HAKS_SUBMODULE_PATH:-SWLOR_Haks}"
NWSYNC_BUILD_SCRIPT="${NWSYNC_BUILD_SCRIPT:-$NWSYNC_ROOT/build.sh}"
NWSYNC_HAK_ROOT="${NWSYNC_HAK_ROOT:-$NWSYNC_ROOT/hak}"
NWSYNC_TLK_ROOT="${NWSYNC_TLK_ROOT:-$NWSYNC_ROOT/tlk}"
NWSYNC_MODULE_ROOT="${NWSYNC_MODULE_ROOT:-$NWSYNC_ROOT/modules}"
SERVER_HAK_ROOT="${SERVER_HAK_ROOT:-$SERVER_ROOT/hak}"
SERVER_TLK_ROOT="${SERVER_TLK_ROOT:-$SERVER_ROOT/tlk}"
SERVER_MODULE_ROOT="${SERVER_MODULE_ROOT:-$SERVER_ROOT/modules}"
SERVER_DOTNET_ROOT="${SERVER_DOTNET_ROOT:-$SERVER_ROOT/dotnet}"
SERVER_ENV_FILE="${SERVER_ENV_FILE:-$SERVER_ROOT/swlor.env}"
NWSYNC_HASH_VARIABLE="${NWSYNC_HASH_VARIABLE:-NWN_NWSYNCHASH}"
REQUIRED_TWEAK_VARIABLE="NWNX_TWEAKS_MATERIAL_NAME_NULL_IS_ALL"
REQUIRED_TWEAK_VALUE="true"
HEALTH_FATAL_LOG_PATTERN="${HEALTH_FATAL_LOG_PATTERN:-buffer overflow|Fatal error|has crashed|Segmentation fault}"
HEALTH_LOG_TAIL_LINES="${HEALTH_LOG_TAIL_LINES:-2000}"

# A host config can set these directly. Environment overrides win when supplied
# for an individual invocation.
LOG_FILE="${SWLOR_DEPLOY_LOG:-${LOG_FILE:-/var/log/$DEPLOYMENT_NAME-deploy.log}}"
LOCK_FILE="${SWLOR_DEPLOY_LOCK:-${LOCK_FILE:-/run/lock/$DEPLOYMENT_NAME-deploy.lock}}"

timestamp()
{
    date -u '+%Y-%m-%dT%H:%M:%SZ'
}

log()
{
    printf '%s [%s] %s\n' "$(timestamp)" "$DEPLOYMENT_NAME" "$*"
}

die()
{
    log "ERROR: $*" >&2
    exit 1
}

usage()
{
    cat <<'EOF'
Usage: swlor-deploy [--if-changed | --force | --status]

  --if-changed  Deploy only when GIT_REMOTE/BRANCH differs from the last
                successfully started commit. This is the default.
  --force       Rebuild and redeploy even when the commit is already active.
  --status      Display recorded commits, the active NWSync manifest, Compose
                state, and free space without changing anything.
EOF
}

MODE=if-changed
if (( $# > 1 )); then
    usage >&2
    exit 64
fi

if (( $# == 1 )); then
    case "$1" in
        --if-changed) MODE=if-changed ;;
        --force) MODE=force ;;
        --status) MODE=status ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            usage >&2
            exit 64
            ;;
    esac
fi

if (( EUID != 0 )); then
    die "Run this command as root."
fi

if [[ ! -e "$LOG_FILE" ]]; then
    install -o root -g root -m 0640 /dev/null "$LOG_FILE"
else
    chown root:root "$LOG_FILE"
    chmod 0640 "$LOG_FILE"
fi
exec > >(tee -a "$LOG_FILE") 2>&1

exec 9>"$LOCK_FILE"
if ! flock --nonblock 9; then
    die "Another SWLOR deployment is already running."
fi

require_command()
{
    local command_name="$1"
    command -v "$command_name" >/dev/null 2>&1 ||
        die "Required command is not installed: $command_name"
}

for required_command in \
    awk basename chown chmod cmp cp curl date df docker dotnet find findmnt flock \
    du git grep install jq ln mktemp mv realpath rm rsync sed sha256sum sleep \
    sort stat tail tee tr unzip wc
do
    require_command "$required_command"
done

for numeric_setting in \
    STOP_TIMEOUT_SECONDS HEALTH_TIMEOUT_SECONDS HEALTH_STABLE_SECONDS \
    HEALTH_LOG_TAIL_LINES MIN_FREE_GIB_BEFORE_BUILD \
    MIN_FREE_GIB_BEFORE_CUTOVER
do
    [[ "${!numeric_setting}" =~ ^[0-9]+$ ]] ||
        die "$numeric_setting must be a non-negative integer."
done
(( HEALTH_TIMEOUT_SECONDS > 0 )) ||
    die "HEALTH_TIMEOUT_SECONDS must be greater than zero."
(( HEALTH_LOG_TAIL_LINES > 0 )) ||
    die "HEALTH_LOG_TAIL_LINES must be greater than zero."
[[ -n "$HEALTH_FATAL_LOG_PATTERN" ]] ||
    die "HEALTH_FATAL_LOG_PATTERN must not be empty."
health_pattern_status=0
printf '' |
    grep -E "$HEALTH_FATAL_LOG_PATTERN" >/dev/null 2>&1 ||
    health_pattern_status=$?
(( health_pattern_status <= 1 )) ||
    die "HEALTH_FATAL_LOG_PATTERN is not a valid extended regular expression."

for absolute_path in \
    "$SOURCE_ROOT" "$NWSYNC_ROOT" "$SERVER_ROOT" "$COMPOSE_FILE" \
    "$STATE_ROOT" "$CACHE_ROOT" "$NWSYNC_BUILD_SCRIPT" \
    "$NWSYNC_HAK_ROOT" "$NWSYNC_TLK_ROOT" "$NWSYNC_MODULE_ROOT" \
    "$SERVER_HAK_ROOT" "$SERVER_TLK_ROOT" "$SERVER_MODULE_ROOT" \
    "$SERVER_DOTNET_ROOT" "$SERVER_ENV_FILE"
do
    [[ "$absolute_path" == /* ]] ||
        die "Configured paths must be absolute: $absolute_path"
done

[[ "$BRANCH" =~ ^[A-Za-z0-9._/-]+$ ]] ||
    die "BRANCH contains unsupported characters: $BRANCH"
[[ "$DEPLOYMENT_NAME" =~ ^[A-Za-z0-9._-]+$ ]] ||
    die "DEPLOYMENT_NAME contains unsupported characters: $DEPLOYMENT_NAME"
[[ "$GIT_REMOTE" =~ ^[A-Za-z0-9._-]+$ ]] ||
    die "GIT_REMOTE contains unsupported characters: $GIT_REMOTE"
[[ "$SERVER_SERVICE" =~ ^[A-Za-z0-9._-]+$ ]] ||
    die "SERVER_SERVICE contains unsupported characters: $SERVER_SERVICE"
[[ "$HAKS_SUBMODULE_PATH" =~ ^[A-Za-z0-9._/-]+$ ]] ||
    die "HAKS_SUBMODULE_PATH contains unsupported characters."
if [[ -n "$COMPOSE_PROJECT_NAME" &&
      ! "$COMPOSE_PROJECT_NAME" =~ ^[a-z0-9][a-z0-9_-]*$ ]]
then
    die "COMPOSE_PROJECT_NAME contains unsupported characters: $COMPOSE_PROJECT_NAME"
fi
[[ "$NWSYNC_HASH_VARIABLE" =~ ^[A-Z][A-Z0-9_]*$ ]] ||
    die "NWSYNC_HASH_VARIABLE contains unsupported characters."

install -d -o root -g root -m 0750 "$STATE_ROOT" "$CACHE_ROOT"

compose()
{
    local compose_arguments=(
        docker compose
        --project-directory "$SERVER_ROOT"
        --file "$COMPOSE_FILE"
    )
    if [[ -n "$COMPOSE_PROJECT_NAME" ]]; then
        compose_arguments+=(--project-name "$COMPOSE_PROJECT_NAME")
    fi
    "${compose_arguments[@]}" "$@"
}

available_bytes()
{
    df --output=avail -B1 "$NWSYNC_ROOT" |
        tail -n 1 |
        tr -d '[:space:]'
}

check_free_space()
{
    local required_gib="$1"
    local phase="$2"
    local required_bytes=$(( required_gib * 1024 * 1024 * 1024 ))
    local free_bytes
    free_bytes="$(available_bytes)"

    if (( free_bytes < required_bytes )); then
        die "$phase requires at least ${required_gib} GiB free on $NWSYNC_ROOT; only $(( free_bytes / 1024 / 1024 / 1024 )) GiB is available."
    fi

    log "$phase free-space check: $(( free_bytes / 1024 / 1024 / 1024 )) GiB available."
}

state_value()
{
    local name="$1"
    if [[ -s "$STATE_ROOT/$name" ]]; then
        tr -d '\r\n' < "$STATE_ROOT/$name"
    fi
}

record_state()
{
    local name="$1"
    local value="$2"
    local temporary_file="$STATE_ROOT/.${name}.$$"

    printf '%s\n' "$value" > "$temporary_file"
    chown root:root "$temporary_file"
    chmod 0640 "$temporary_file"
    mv -f "$temporary_file" "$STATE_ROOT/$name"
}

git_paths_changed()
{
    local base_commit="$1"
    local target_commit="$2"
    local diff_status=0
    shift 2

    git -C "$SOURCE_ROOT" diff --quiet \
        "$base_commit" "$target_commit" -- "$@" ||
        diff_status=$?

    case "$diff_status" in
        0) return 1 ;;
        1) return 0 ;;
        *) die "Unable to compare deployment inputs between $base_commit and $target_commit." ;;
    esac
}

safe_remove_under()
{
    local parent="$1"
    local target="$2"
    local resolved_parent
    local resolved_target

    resolved_parent="$(realpath -m "$parent")"
    resolved_target="$(realpath -m "$target")"

    [[ "$resolved_target" == "$resolved_parent/"* ]] ||
        die "Refusing to remove a path outside $resolved_parent: $resolved_target"
    [[ "$resolved_target" != "$resolved_parent" ]] ||
        die "Refusing to remove the parent directory."

    rm -rf --one-file-system -- "$resolved_target"
}

same_object()
{
    local first_path="$1"
    local second_path="$2"

    [[ -e "$first_path" && -e "$second_path" ]] &&
        [[ "$(stat -Lc '%d:%i' "$first_path")" == \
           "$(stat -Lc '%d:%i' "$second_path")" ]]
}

same_filesystem()
{
    [[ "$(stat -Lc '%d' "$1")" == "$(stat -Lc '%d' "$2")" ]]
}

separation_status()
{
    if same_object "$1" "$2"; then
        printf 'STILL linked'
    else
        printf 'separate'
    fi
}

wait_for_server_health()
{
    local started_at="$1"
    local deadline=$(( $(date +%s) + HEALTH_TIMEOUT_SECONDS ))
    local container_id
    local current_id
    local recent_logs=
    local restart_count
    local state

    container_id="$(compose ps -q "$SERVER_SERVICE" 2>/dev/null)"
    [[ -n "$container_id" ]] || return 1
    restart_count="$(docker inspect --format '{{.RestartCount}}' "$container_id")"
    [[ "$restart_count" == 0 ]] || {
        log "Health check rejected a server container that already restarted $restart_count time(s)."
        return 1
    }

    log "Waiting up to ${HEALTH_TIMEOUT_SECONDS}s for '$HEALTH_LOG_MARKER'."
    while (( $(date +%s) < deadline )); do
        state="$(docker inspect --format '{{.State.Status}}' "$container_id" 2>/dev/null || true)"
        [[ "$state" == running ]] || return 1
        [[ "$(docker inspect --format '{{.RestartCount}}' "$container_id")" == 0 ]] ||
            return 1

        recent_logs="$(
            compose logs \
                --no-color \
                --since "$started_at" \
                --tail "$HEALTH_LOG_TAIL_LINES" \
                "$SERVER_SERVICE" 2>&1 || true
        )"
        if printf '%s\n' "$recent_logs" |
            grep -E "$HEALTH_FATAL_LOG_PATTERN" >/dev/null
        then
            log "A fatal server marker appeared during startup."
            return 1
        fi
        if printf '%s\n' "$recent_logs" |
            grep -F "$HEALTH_LOG_MARKER" >/dev/null
        then
            break
        fi
        sleep 5
    done

    if ! printf '%s\n' "$recent_logs" |
        grep -F "$HEALTH_LOG_MARKER" >/dev/null
    then
        return 1
    fi

    log "Startup marker found; checking ${HEALTH_STABLE_SECONDS}s of stability."
    local stable_deadline=$(( $(date +%s) + HEALTH_STABLE_SECONDS ))
    local stable_started_at
    local stable_elapsed
    local next_progress=30
    stable_started_at="$(date +%s)"
    while (( $(date +%s) < stable_deadline )); do
        current_id="$(compose ps -q "$SERVER_SERVICE" 2>/dev/null)"
        [[ "$current_id" == "$container_id" ]] || return 1
        [[ "$(docker inspect --format '{{.State.Status}}' "$container_id")" == running ]] ||
            return 1
        [[ "$(docker inspect --format '{{.RestartCount}}' "$container_id")" == 0 ]] ||
            return 1
        recent_logs="$(
            compose logs \
                --no-color \
                --since "$started_at" \
                --tail "$HEALTH_LOG_TAIL_LINES" \
                "$SERVER_SERVICE" 2>&1 || true
        )"
        if printf '%s\n' "$recent_logs" |
            grep -E "$HEALTH_FATAL_LOG_PATTERN" >/dev/null
        then
            log "A fatal server marker appeared during the stability window."
            return 1
        fi
        stable_elapsed=$(( $(date +%s) - stable_started_at ))
        if (( stable_elapsed >= next_progress )); then
            log "Server remains healthy after ${stable_elapsed}s of the ${HEALTH_STABLE_SECONDS}s stability window."
            next_progress=$(( next_progress + 30 ))
        fi
        sleep 5
    done

    current_id="$(compose ps -q "$SERVER_SERVICE" 2>/dev/null)"
    [[ "$current_id" == "$container_id" ]] || return 1
    [[ "$(docker inspect --format '{{.State.Status}}' "$container_id")" == running ]] ||
        return 1
    [[ "$(docker inspect --format '{{.RestartCount}}' "$container_id")" == 0 ]] ||
        return 1
    recent_logs="$(
        compose logs \
            --no-color \
            --since "$started_at" \
            --tail "$HEALTH_LOG_TAIL_LINES" \
            "$SERVER_SERVICE" 2>&1 || true
    )"
    if printf '%s\n' "$recent_logs" |
        grep -E "$HEALTH_FATAL_LOG_PATTERN" >/dev/null
    then
        log "A fatal server marker appeared at the end of the stability window."
        return 1
    fi

    return 0
}

write_server_env_setting()
{
    local setting_key="$1"
    local setting_value="$2"
    server_env_temporary="${SERVER_ENV_FILE}.new.$$"

    awk \
        -v key="$setting_key" \
        -v value="$setting_value" \
        '
          BEGIN { found = 0 }
          index($0, key "=") == 1 {
              if (!found) {
                  print key "=" value
                  found = 1
              }
              next
          }
          { print }
          END {
              if (!found)
                  print key "=" value
          }
        ' \
        "$SERVER_ENV_FILE" > "$server_env_temporary"
    chown --reference="$SERVER_ENV_FILE" "$server_env_temporary"
    chmod --reference="$SERVER_ENV_FILE" "$server_env_temporary"
    mv -f "$server_env_temporary" "$SERVER_ENV_FILE"
    server_env_temporary=

    grep -Fqx "${setting_key}=${setting_value}" "$SERVER_ENV_FILE" ||
        die "Failed to update $setting_key in $SERVER_ENV_FILE."
}

write_manifest_hash()
{
    write_server_env_setting "$NWSYNC_HASH_VARIABLE" "$1"
}

restore_directory()
{
    local rollback_path="$1"
    local server_path="$2"
    local failed_path="$3"
    local failed_parent="$4"

    [[ -d "$rollback_path" ]] || return 0

    if [[ -d "$server_path" ]]; then
        if [[ -e "$failed_path" ]]; then
            safe_remove_under "$failed_parent" "$failed_path"
        fi
        mv "$server_path" "$failed_path"
    fi
    mv "$rollback_path" "$server_path"
}

verify_directory_payloads()
{
    local source_path="$1"
    local destination_path="$2"
    local differences

    differences="$(
        rsync \
            --recursive \
            --links \
            --checksum \
            --delete \
            --dry-run \
            --itemize-changes \
            "$source_path/" \
            "$destination_path/"
    )"
    if [[ -n "$differences" ]]; then
        printf '%s\n' "$differences"
        return 1
    fi
}

verify_hak_payloads()
{
    local source_path="$1"
    local destination_path="$2"
    local differences

    differences="$(
        rsync \
            --recursive \
            --links \
            --checksum \
            --delete \
            --exclude='*.md5' \
            --dry-run \
            --itemize-changes \
            "$source_path/" \
            "$destination_path/"
    )"
    if [[ -n "$differences" ]]; then
        printf '%s\n' "$differences"
        return 1
    fi
}

show_status()
{
    local active_commit
    local previous_commit
    local active_manifest
    local configured_manifest
    local github_dispatch_run

    active_commit="$(state_value active-commit)"
    previous_commit="$(state_value previous-commit)"
    github_dispatch_run="$(state_value github-dispatch-run)"
    if [[ -s "$NWSYNC_ROOT/latest" ]]; then
        active_manifest="$(tr -d '\r\n' < "$NWSYNC_ROOT/latest")"
    else
        active_manifest=not-present
    fi
    configured_manifest="$(
        sed -n "s/^${NWSYNC_HASH_VARIABLE}=//p" "$SERVER_ENV_FILE" 2>/dev/null |
            tail -n 1
    )"

    printf 'Source:           %s\n' "$SOURCE_ROOT"
    printf 'Branch:           %s/%s\n' "$GIT_REMOTE" "$BRANCH"
    printf 'Active commit:    %s\n' "${active_commit:-not recorded}"
    printf 'Previous commit:  %s\n' "${previous_commit:-not recorded}"
    printf 'GitHub request:   %s\n' "${github_dispatch_run:-not recorded}"
    printf 'NWSync root:      %s\n' "$NWSYNC_ROOT"
    printf 'NWSync manifest:  %s\n' "$active_manifest"
    printf 'Server manifest:  %s\n' "${configured_manifest:-not configured}"
    printf 'Server env:       %s\n' "$SERVER_ENV_FILE"
    printf 'Server dotnet:     %s\n' "$SERVER_DOTNET_ROOT"
    printf 'HAK paths:         %s\n' \
        "$(separation_status "$NWSYNC_HAK_ROOT" "$SERVER_HAK_ROOT")"
    printf 'TLK paths:         %s\n' \
        "$(separation_status "$NWSYNC_TLK_ROOT" "$SERVER_TLK_ROOT")"
    printf 'Module paths:      %s\n' \
        "$(separation_status "$NWSYNC_MODULE_ROOT" "$SERVER_MODULE_ROOT")"
    compose ps
    df -h "$NWSYNC_ROOT"
}

if [[ "$MODE" == status ]]; then
    show_status
    exit 0
fi

[[ -d "$SOURCE_ROOT/.git" || -f "$SOURCE_ROOT/.git" ]] ||
    die "Deployment source is not a Git repository: $SOURCE_ROOT"
[[ -d "$NWSYNC_ROOT" ]] || die "NWSync root does not exist: $NWSYNC_ROOT"
[[ -d "$SERVER_ROOT" ]] || die "Server root does not exist: $SERVER_ROOT"
[[ -f "$COMPOSE_FILE" ]] || die "Compose file does not exist: $COMPOSE_FILE"
[[ -f "$SERVER_ENV_FILE" && ! -L "$SERVER_ENV_FILE" ]] ||
    die "Server environment file does not exist or is a symlink: $SERVER_ENV_FILE"
[[ -x "$NWSYNC_BUILD_SCRIPT" ]] ||
    die "NWSync build script is not executable: $NWSYNC_BUILD_SCRIPT"
[[ "$(stat -c '%u:%g' "$SOURCE_ROOT")" == 0:0 ]] ||
    die "Deployment source must be owned by root:root."

for content_directory in \
    "$NWSYNC_HAK_ROOT" "$NWSYNC_TLK_ROOT" "$NWSYNC_MODULE_ROOT"
do
    [[ -d "$content_directory" && ! -L "$content_directory" ]] ||
        die "NWSync content directory is missing or is a symlink: $content_directory"
done

for content_directory in \
    "$SERVER_HAK_ROOT" "$SERVER_TLK_ROOT" "$SERVER_MODULE_ROOT" \
    "$SERVER_DOTNET_ROOT"
do
    [[ -d "$content_directory" && ! -L "$content_directory" ]] ||
        die "Server content directory is missing or is a symlink: $content_directory"
done

for path_pair in \
    "$NWSYNC_HAK_ROOT|$SERVER_HAK_ROOT|HAK" \
    "$NWSYNC_TLK_ROOT|$SERVER_TLK_ROOT|TLK" \
    "$NWSYNC_MODULE_ROOT|$SERVER_MODULE_ROOT|module"
do
    IFS='|' read -r nwsync_path server_path content_name <<< "$path_pair"
    ! same_object "$nwsync_path" "$server_path" ||
        die "$content_name paths are still linked. Remove the bind mount before deploying."
    [[ "$(findmnt -T "$server_path" -n -o TARGET)" != "$server_path" ]] ||
        die "$server_path is still a dedicated mount point. Unmount it before deploying."
done
log "Verified HAK, TLK, and module build paths are separate from the server."

same_filesystem "$NWSYNC_ROOT" "$SERVER_ROOT" ||
    die "NWSync and server roots must be on the same filesystem for atomic moves."
same_filesystem "$CACHE_ROOT" "$SERVER_ROOT" ||
    die "CACHE_ROOT and SERVER_ROOT must be on the same filesystem for rollback."
for artifact_path in \
    "$NWSYNC_HAK_ROOT" "$NWSYNC_TLK_ROOT" "$NWSYNC_MODULE_ROOT" \
    "$SERVER_HAK_ROOT" "$SERVER_TLK_ROOT" "$SERVER_MODULE_ROOT" \
    "$SERVER_DOTNET_ROOT"
do
    same_filesystem "$artifact_path" "$CACHE_ROOT" ||
        die "$artifact_path must share a filesystem with CACHE_ROOT for atomic cutover and rollback."
done

hash_line_count="$(
    grep -Ec "^${NWSYNC_HASH_VARIABLE}=" "$SERVER_ENV_FILE" || true
)"
[[ "$hash_line_count" == 1 ]] ||
    die "$SERVER_ENV_FILE must contain exactly one ${NWSYNC_HASH_VARIABLE}= line."
configured_server_manifest="$(
    sed -n "s/^${NWSYNC_HASH_VARIABLE}=//p" "$SERVER_ENV_FILE" |
        tr -d '\r\n'
)"
if [[ -n "$configured_server_manifest" &&
      ! "$configured_server_manifest" =~ ^[0-9a-fA-F]{40}$ ]]
then
    die "$NWSYNC_HASH_VARIABLE must be empty or a 40-character manifest hash."
fi

required_tweak_line_count="$(
    grep -Ec "^${REQUIRED_TWEAK_VARIABLE}=" "$SERVER_ENV_FILE" || true
)"
configured_required_tweak="$(
    sed -n "s/^${REQUIRED_TWEAK_VARIABLE}=//p" "$SERVER_ENV_FILE" |
        tail -n 1 |
        tr -d '\r\n'
)"
server_env_migration_required=0
if [[ "$required_tweak_line_count" != 1 ||
      "$configured_required_tweak" != "$REQUIRED_TWEAK_VALUE" ]]
then
    server_env_migration_required=1
fi

compose config --quiet
check_free_space "$MIN_FREE_GIB_BEFORE_BUILD" "Pre-build"

if [[ -n "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]]; then
    die "Deployment source has local changes. Refusing to overwrite or deploy them."
fi

current_branch="$(git -C "$SOURCE_ROOT" symbolic-ref --quiet --short HEAD || true)"
[[ "$current_branch" == "$BRANCH" ]] ||
    die "Deployment source is on '$current_branch'; expected '$BRANCH'."

configured_url="$(
    git -C "$SOURCE_ROOT" remote get-url "$GIT_REMOTE" |
        sed -E 's#/$##; s#\.git$##'
)"
expected_url="$(printf '%s' "$REPOSITORY_URL" | sed -E 's#/$##; s#\.git$##')"
[[ "$configured_url" == "$expected_url" ]] ||
    die "$GIT_REMOTE is '$configured_url'; expected '$expected_url'."

log "Fetching $GIT_REMOTE/$BRANCH."
git -C "$SOURCE_ROOT" fetch --prune "$GIT_REMOTE" \
    "+refs/heads/$BRANCH:refs/remotes/$GIT_REMOTE/$BRANCH"

local_commit="$(git -C "$SOURCE_ROOT" rev-parse HEAD)"
remote_commit="$(
    git -C "$SOURCE_ROOT" rev-parse "refs/remotes/$GIT_REMOTE/$BRANCH"
)"
if [[ "$local_commit" != "$remote_commit" ]]; then
    git -C "$SOURCE_ROOT" merge-base --is-ancestor "$local_commit" "$remote_commit" ||
        die "$GIT_REMOTE/$BRANCH diverged from the deployment source; only fast-forward updates are allowed."
    git -C "$SOURCE_ROOT" merge --ff-only \
        "refs/remotes/$GIT_REMOTE/$BRANCH"

    # Continue with the just-fetched implementation rather than the old script body that was
    # already open when this process began. Deployment migrations added by this commit must run
    # during its first rollout, before the server is restarted.
    updated_deploy_script="$SOURCE_ROOT/scripts/deployment/swlor-deploy.sh"
    [[ -f "$updated_deploy_script" && ! -L "$updated_deploy_script" ]] ||
        die "Updated deployment script is missing or is a symbolic link: $updated_deploy_script"
    log "Re-executing deployment with the updated script from $remote_commit."
    flock --unlock 9
    exec "$BASH" "$updated_deploy_script" "$@"
fi

log "Synchronizing recursive submodules."
git -C "$SOURCE_ROOT" submodule sync --recursive
git -C "$SOURCE_ROOT" submodule update --init --recursive --depth 1

if [[ -n "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]]; then
    die "Deployment source is not clean after updating."
fi

target_commit="$(git -C "$SOURCE_ROOT" rev-parse HEAD)"
active_commit="$(state_value active-commit)"
if [[ "$MODE" == if-changed && "$active_commit" == "$target_commit" ]]; then
    log "Commit $target_commit is already active; nothing to deploy."
    exit 0
fi

configured_haks_branch="$(
    git -C "$SOURCE_ROOT" config -f .gitmodules \
        --get "submodule.$HAKS_SUBMODULE_PATH.branch" || true
)"
[[ "$configured_haks_branch" == "$BRANCH" ]] ||
    die "$HAKS_SUBMODULE_PATH tracks '$configured_haks_branch' in .gitmodules; expected '$BRANCH'."
haks_url="$(
    git -C "$SOURCE_ROOT" config -f .gitmodules \
        --get "submodule.$HAKS_SUBMODULE_PATH.url"
)"
pinned_haks_commit="$(
    git -C "$SOURCE_ROOT" rev-parse "HEAD:$HAKS_SUBMODULE_PATH"
)"
checked_out_haks_commit="$(
    git -C "$SOURCE_ROOT/$HAKS_SUBMODULE_PATH" rev-parse HEAD
)"
remote_haks_commit="$(
    git ls-remote "$haks_url" "refs/heads/$BRANCH" |
        awk 'NR == 1 { print $1 }'
)"
[[ -n "$remote_haks_commit" ]] ||
    die "Unable to resolve $HAKS_SUBMODULE_PATH branch $BRANCH."
[[ "$pinned_haks_commit" == "$checked_out_haks_commit" &&
   "$checked_out_haks_commit" == "$remote_haks_commit" ]] ||
    die "$HAKS_SUBMODULE_PATH is not pinned to the tip of $BRANCH (parent=$pinned_haks_commit checkout=$checked_out_haks_commit remote=$remote_haks_commit)."
log "Verified $HAKS_SUBMODULE_PATH $BRANCH at $checked_out_haks_commit."

haks_inputs_changed=1
module_inputs_changed=1
dotnet_inputs_changed=1
force_full_hak_rebuild=0

if [[ "$MODE" == if-changed &&
      -n "$active_commit" ]] &&
   git -C "$SOURCE_ROOT" cat-file -e "${active_commit}^{commit}" 2>/dev/null
then
    if ! git_paths_changed \
        "$active_commit" \
        "$target_commit" \
        "$HAKS_SUBMODULE_PATH" \
        Build/hakbuilder.json \
        SWLOR.CLI/HakBuilder.cs \
        SWLOR.CLI/ChecksumUtil.cs \
        SWLOR.CLI/Model/HakBuilderConfig.cs \
        SWLOR.CLI/Program.cs \
        SWLOR.CLI/SWLOR.CLI.csproj \
        global.json \
        Directory.Build.props \
        Directory.Build.targets \
        Directory.Packages.props \
        NuGet.config
    then
        haks_inputs_changed=0
    fi

    if ! git_paths_changed \
        "$active_commit" \
        "$target_commit" \
        Module \
        SWLOR.CLI/ModulePacker.cs \
        SWLOR.CLI/Program.cs \
        SWLOR.CLI/SWLOR.CLI.csproj \
        global.json \
        Directory.Build.props \
        Directory.Build.targets \
        Directory.Packages.props \
        NuGet.config
    then
        module_inputs_changed=0
    fi

    if ! git_paths_changed \
        "$active_commit" \
        "$target_commit" \
        SWLOR.Game.Server \
        ':(exclude)SWLOR.Game.Server/Readmes/**' \
        SWLOR.NWN.API \
        global.json \
        Directory.Build.props \
        Directory.Build.targets \
        Directory.Packages.props \
        NuGet.config
    then
        dotnet_inputs_changed=0
    fi
else
    log "No usable active-commit baseline exists; all content inputs will be rebuilt."
fi

neverwinter_tool_key="${NEVERWINTER_NIM_VERSION}:${NEVERWINTER_NIM_SHA256}"
recorded_neverwinter_tool_key="$(state_value neverwinter-tool-key)"
if [[ -n "$recorded_neverwinter_tool_key" &&
      "$recorded_neverwinter_tool_key" != "$neverwinter_tool_key" ]]
then
    log "The pinned neverwinter.nim tool changed; forcing HAK/TLK and module rebuilds."
    haks_inputs_changed=1
    module_inputs_changed=1
    force_full_hak_rebuild=1
fi

nwsync_build_script_sha="$(
    sha256sum "$NWSYNC_BUILD_SCRIPT" |
        awk '{ print $1 }'
)"
recorded_nwsync_build_script_sha="$(state_value nwsync-build-script-sha)"
nwsync_tool_changed=0
if [[ -n "$recorded_nwsync_build_script_sha" &&
      "$recorded_nwsync_build_script_sha" != "$nwsync_build_script_sha" ]]
then
    log "The NWSync build script changed; a new manifest will be generated."
    nwsync_tool_changed=1
fi

if [[ "$MODE" == force ]]; then
    haks_inputs_changed=1
    module_inputs_changed=1
    dotnet_inputs_changed=1
    force_full_hak_rebuild=1
    nwsync_tool_changed=1
fi

nwsync_inputs_changed=0
if (( haks_inputs_changed == 1 ||
      module_inputs_changed == 1 ||
      nwsync_tool_changed == 1 ))
then
    nwsync_inputs_changed=1
fi

log "Build plan: HAK/TLK=$(
    (( haks_inputs_changed == 1 )) && printf rebuild || printf reuse
), module=$(
    (( module_inputs_changed == 1 )) && printf repack || printf reuse
), NWSync=$(
    (( nwsync_inputs_changed == 1 )) && printf generate || printf reuse
), .NET=$(
    (( dotnet_inputs_changed == 1 )) && printf build || printf reuse
)."

haks_root="$SOURCE_ROOT/$HAKS_SUBMODULE_PATH"
tracked_set_count="$(
    git -C "$haks_root" ls-files '*.set' |
        wc -l |
        tr -d '[:space:]'
)"
(( tracked_set_count > 0 )) ||
    die "$HAKS_SUBMODULE_PATH does not contain any tracked .set files."

if (( haks_inputs_changed == 1 )); then
    log "Materializing $tracked_set_count tracked .set resources with repository-defined line endings."
    git -C "$haks_root" ls-files -z '*.set' |
        git -C "$haks_root" checkout-index --force --stdin -z

    validated_set_count=0
    while IFS= read -r -d '' set_relative_path; do
        set_path="$haks_root/$set_relative_path"
        allow_unterminated_final_line=0
        if [[ "$(
            tail -c 1 "$set_path" |
                wc -l |
                tr -d '[:space:]'
        )" == 0 ]]
        then
            allow_unterminated_final_line=1
        fi

        if ! awk \
            -v allow_unterminated_final_line="$allow_unterminated_final_line" \
            'BEGIN {
                 lines = 0
                 invalid_lines = 0
                 invalid_line = 0
             }
             {
                 lines += 1
                 if (substr($0, length($0), 1) != "\r") {
                     invalid_lines += 1
                     invalid_line = lines
                 }
              }
              END {
                 if (lines == 0) {
                     exit 1
                 }
                 if (invalid_lines == 0) {
                     exit 0
                 }
                 if (allow_unterminated_final_line == 1 &&
                     invalid_lines == 1 &&
                     invalid_line == lines) {
                     exit 0
                 }
                 exit 1
              }' \
            "$set_path"
        then
            die "$HAKS_SUBMODULE_PATH/$set_relative_path contains a non-CRLF line ending."
        fi
        (( validated_set_count += 1 ))
    done < <(git -C "$haks_root" ls-files -z '*.set')
    (( validated_set_count == tracked_set_count )) ||
        die "Validated $validated_set_count of $tracked_set_count tracked .set files."
    log "Verified CRLF line endings for all $validated_set_count tracked .set resources."
else
    log "HAK/TLK inputs are unchanged; skipping source materialization and HAK packing."
fi

if [[ -n "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]]; then
    die "Deployment source became dirty while materializing .set resources."
fi

work_directory="$(mktemp -d "$CACHE_ROOT/work.XXXXXXXX")"
staged_server_directory="$work_directory/staged"
staged_hak="$staged_server_directory/hak"
staged_tlk="$staged_server_directory/tlk"
staged_modules="$staged_server_directory/modules"
staged_dotnet="$staged_server_directory/dotnet"
rollback_directory="$work_directory/rollback"
failed_directory="$work_directory/failed"
module_temporary=
server_env_temporary=
server_env_restore_temporary=
cutover_started=0
preserve_work_directory=0

rollback_cutover()
{
    local rollback_started_at

    log "Restoring the pre-deployment server files and manifest hash."
    compose down --timeout "$STOP_TIMEOUT_SECONDS" || return 1

    restore_directory \
        "$rollback_directory/hak" \
        "$SERVER_HAK_ROOT" \
        "$failed_directory/hak" \
        "$failed_directory" ||
        return 1
    restore_directory \
        "$rollback_directory/tlk" \
        "$SERVER_TLK_ROOT" \
        "$failed_directory/tlk" \
        "$failed_directory" ||
        return 1
    restore_directory \
        "$rollback_directory/modules" \
        "$SERVER_MODULE_ROOT" \
        "$failed_directory/modules" \
        "$failed_directory" ||
        return 1
    restore_directory \
        "$rollback_directory/dotnet" \
        "$SERVER_DOTNET_ROOT" \
        "$failed_directory/dotnet" \
        "$failed_directory" ||
        return 1

    if [[ -f "$rollback_directory/swlor.env" ]]; then
        server_env_restore_temporary="${SERVER_ENV_FILE}.restore.$$"
        cp -a \
            "$rollback_directory/swlor.env" \
            "$server_env_restore_temporary" ||
            return 1
        mv -f "$server_env_restore_temporary" "$SERVER_ENV_FILE" ||
            return 1
        server_env_restore_temporary=
    fi

    rollback_started_at="$(timestamp)"
    compose up -d || return 1
    wait_for_server_health "$rollback_started_at"
}

cleanup_on_exit()
{
    local exit_status=$?
    trap - EXIT INT TERM
    set +e

    if (( exit_status != 0 && cutover_started == 1 )); then
        log "Deployment failed during cutover; starting automatic rollback."
        if rollback_cutover; then
            log "Automatic rollback passed its health check."
        else
            preserve_work_directory=1
            log "CRITICAL: rollback failed. Preserving recovery files at $work_directory."
        fi
    fi

    if [[ -n "${module_temporary:-}" && -e "$module_temporary" ]]; then
        safe_remove_under "$NWSYNC_MODULE_ROOT" "$module_temporary"
    fi
    if [[ -n "${server_env_temporary:-}" && -e "$server_env_temporary" ]]; then
        safe_remove_under "$SERVER_ROOT" "$server_env_temporary"
    fi
    if [[ -n "${server_env_restore_temporary:-}" &&
          -e "$server_env_restore_temporary" ]]
    then
        safe_remove_under "$SERVER_ROOT" "$server_env_restore_temporary"
    fi
    if [[ -d "$SOURCE_ROOT/Module/packing" ]]; then
        safe_remove_under "$SOURCE_ROOT/Module" "$SOURCE_ROOT/Module/packing"
    fi
    if [[ -f "$SOURCE_ROOT/Module/$MODULE_NAME" ]]; then
        safe_remove_under "$SOURCE_ROOT/Module" \
            "$SOURCE_ROOT/Module/$MODULE_NAME"
    fi
    if (( preserve_work_directory == 0 )) &&
       [[ -n "${work_directory:-}" && -e "$work_directory" ]]
    then
        safe_remove_under "$CACHE_ROOT" "$work_directory"
    fi

    exit "$exit_status"
}

trap cleanup_on_exit EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

install -d -o root -g root -m 0750 \
    "$CACHE_ROOT/nuget" "$CACHE_ROOT/dotnet-home" "$CACHE_ROOT/tmp" \
    "$staged_hak" "$staged_tlk" "$staged_modules" "$staged_dotnet" \
    "$rollback_directory" "$failed_directory"
export NUGET_PACKAGES="$CACHE_ROOT/nuget"
export DOTNET_CLI_HOME="$CACHE_ROOT/dotnet-home"
export TMPDIR="$CACHE_ROOT/tmp"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

log "Using the persistent NWSync artifact workspace without replacing it from the running server."

cli_directory="$SOURCE_ROOT/SWLOR.CLI/bin/Release/net10.0"
cli_build_required=0
if (( haks_inputs_changed == 1 ||
      module_inputs_changed == 1 ||
      dotnet_inputs_changed == 1 ))
then
    cli_build_required=1
fi

if (( cli_build_required == 1 )); then
    log "Building SWLOR.CLI and the production game server for commit $target_commit."
    dotnet build "$SOURCE_ROOT/SWLOR.CLI/SWLOR.CLI.csproj" \
        --configuration Release \
        --property:OS=Unix \
        --property:RunPostBuildEvent=Never
else
    log "No build inputs changed; skipping the .NET build."
fi

tools_directory="$CACHE_ROOT/neverwinter/$NEVERWINTER_NIM_VERSION"
if (( haks_inputs_changed == 1 || module_inputs_changed == 1 )); then
    if [[ ! -x "$tools_directory/nwn_erf" ||
          ! -x "$tools_directory/nwn_gff" ||
          ! -x "$tools_directory/nwn_tlk" ]]
    then
        tools_work="$work_directory/neverwinter"
        install -d -o root -g root -m 0750 "$tools_work" "$tools_directory"
        log "Downloading pinned neverwinter.nim $NEVERWINTER_NIM_VERSION tools."
        curl --silent --show-error --location --fail \
            --output "$tools_work/neverwinter.zip" \
            "$NEVERWINTER_NIM_RELEASE_URL"
        printf '%s  %s\n' \
            "$NEVERWINTER_NIM_SHA256" \
            "$tools_work/neverwinter.zip" |
            sha256sum --check --strict
        unzip -q "$tools_work/neverwinter.zip" -d "$tools_work/unpacked"
        for tool_name in nwn_erf nwn_gff nwn_tlk; do
            install -o root -g root -m 0755 \
                "$tools_work/unpacked/$tool_name" \
                "$tools_directory/$tool_name"
        done
    fi

    for tool_name in nwn_erf nwn_gff nwn_tlk; do
        ln -sfn "$tools_directory/$tool_name" "$cli_directory/$tool_name.exe"
    done
fi

if (( dotnet_inputs_changed == 1 )); then
    log "Staging production .NET assemblies in temporary workspace."
    rsync \
        --archive \
        --delete \
        "$SOURCE_ROOT/SWLOR.Game.Server/bin/Release/net10.0/" \
        "$staged_dotnet/"
else
    log "Production .NET inputs are unchanged; reusing the live .NET directory."
fi

# HakBuilder uses the persistent .hak/.md5 pairs in NWSYNC_ROOT and rebuilds
# only changed HAKs. The live server intentionally has no .md5 sidecars, so it
# must never be used to seed this workspace. HakBuilder also installs the
# current TLK directly into NWSYNC_ROOT/tlk.
jq \
    --arg source "$SOURCE_ROOT" \
    --arg output "$NWSYNC_ROOT/" \
    '
      .OutputPath = $output
      | .TlkPath = ($source + "/" + (.TlkPath | sub("^\\.\\./"; "")))
      | .HakList |= map(
          if . == null then .
          else .Path = ($source + "/" + (.Path | sub("^\\.\\./"; "")))
          end
        )
    ' \
    "$SOURCE_ROOT/Build/hakbuilder.json" \
    > "$work_directory/hakbuilder.json"

expected_haks_file="$work_directory/expected-haks.txt"
jq -r \
    '.HakList[] | select(. != null and (.Name // "") != "") | .Name' \
    "$SOURCE_ROOT/Build/hakbuilder.json" |
    sort -u > "$expected_haks_file"
configured_hak_count="$(
    jq '[.HakList[] | select(. != null and (.Name // "") != "")] | length' \
        "$SOURCE_ROOT/Build/hakbuilder.json"
)"
expected_hak_count="$(
    wc -l < "$expected_haks_file" |
        tr -d '[:space:]'
)"
(( expected_hak_count == configured_hak_count )) ||
    die "Build/hakbuilder.json contains duplicate HAK names."

if (( haks_inputs_changed == 1 )); then
    if (( force_full_hak_rebuild == 1 )); then
        log "Invalidating HAK checksum sidecars for an explicit/tool-driven full rebuild."
        while IFS= read -r hak_name; do
            if [[ -e "$NWSYNC_HAK_ROOT/$hak_name.md5" ]]; then
                safe_remove_under \
                    "$NWSYNC_HAK_ROOT" \
                    "$NWSYNC_HAK_ROOT/$hak_name.md5"
            fi
        done < "$expected_haks_file"
    fi

    log "Building changed HAKs and the TLK directly in $NWSYNC_ROOT."
    (
        cd "$work_directory"
        dotnet "$cli_directory/SWLOR.CLI.dll" --hak
    )

    while IFS= read -r -d '' hak_path; do
        hak_file_name="$(basename "$hak_path")"
        hak_name="${hak_file_name%.hak}"
        if ! grep -Fqx -- "$hak_name" "$expected_haks_file"; then
            log "Removing obsolete NWSync HAK $hak_file_name."
            safe_remove_under "$NWSYNC_HAK_ROOT" "$hak_path"
            if [[ -e "$NWSYNC_HAK_ROOT/$hak_name.md5" ]]; then
                safe_remove_under \
                    "$NWSYNC_HAK_ROOT" \
                    "$NWSYNC_HAK_ROOT/$hak_name.md5"
            fi
        fi
    done < <(find "$NWSYNC_HAK_ROOT" -maxdepth 1 -type f -name '*.hak' -print0)

    set_validation_root="$work_directory/set-validation"
    install -d -o root -g root -m 0750 "$set_validation_root"
    packaged_set_count=0
    log "Extracting and byte-validating every packaged .set resource."
    while IFS= read -r -d '' set_relative_path; do
        hak_name="${set_relative_path%%/*}"
        set_file_name="$(basename "$set_relative_path")"
        extracted_set_root="$set_validation_root/$hak_name"
        install -d -o root -g root -m 0750 "$extracted_set_root"

        (
            cd "$extracted_set_root"
            "$tools_directory/nwn_erf" \
                --quiet \
                -f "$NWSYNC_HAK_ROOT/$hak_name.hak" \
                -x "$set_file_name"
        )
        [[ -s "$extracted_set_root/$set_file_name" ]] ||
            die "$hak_name.hak does not contain $set_file_name."
        cmp --silent \
            "$haks_root/$set_relative_path" \
            "$extracted_set_root/$set_file_name" ||
            die "$hak_name.hak contains an altered $set_file_name resource."
        (( packaged_set_count += 1 ))
    done < <(git -C "$haks_root" ls-files -z '*.set')
    (( packaged_set_count == tracked_set_count )) ||
        die "Validated $packaged_set_count of $tracked_set_count packaged .set files."
    log "Verified all $packaged_set_count packaged .set resources byte-for-byte."
fi

if (( module_inputs_changed == 1 )); then
    if [[ -d "$SOURCE_ROOT/Module/packing" ]]; then
        safe_remove_under "$SOURCE_ROOT/Module" "$SOURCE_ROOT/Module/packing"
    fi
    log "Packing $MODULE_NAME."
    (
        cd "$SOURCE_ROOT/Module"
        dotnet "$cli_directory/SWLOR.CLI.dll" \
            --pack "./$MODULE_NAME" \
            --no-prompt
    )
    module_temporary="$NWSYNC_MODULE_ROOT/.${MODULE_NAME}.new.$$"
    install -o root -g root -m 0644 \
        "$SOURCE_ROOT/Module/$MODULE_NAME" \
        "$module_temporary"
    mv -f "$module_temporary" "$NWSYNC_MODULE_ROOT/$MODULE_NAME"
else
    log "Module inputs are unchanged; reusing $NWSYNC_MODULE_ROOT/$MODULE_NAME."
fi

previous_manifest="$configured_server_manifest"

if (( nwsync_inputs_changed == 1 )); then
    log "Running the existing NWSync build script from $NWSYNC_ROOT."
    (
        cd "$NWSYNC_ROOT"
        "$NWSYNC_BUILD_SCRIPT"
    )
else
    log "Client content is unchanged; reusing the active NWSync manifest."
fi

if (( dotnet_inputs_changed == 1 )); then
    [[ -s "$staged_dotnet/SWLOR.Game.Server.dll" ]] ||
        die "Temporary .NET output does not contain SWLOR.Game.Server.dll."
    [[ -s "$staged_dotnet/SWLOR.Game.Server.runtimeconfig.json" ]] ||
        die "Temporary .NET output does not contain the server runtime configuration."
else
    [[ -s "$SERVER_DOTNET_ROOT/SWLOR.Game.Server.dll" ]] ||
        die "Live .NET directory does not contain SWLOR.Game.Server.dll."
    [[ -s "$SERVER_DOTNET_ROOT/SWLOR.Game.Server.runtimeconfig.json" ]] ||
        die "Live .NET directory does not contain the server runtime configuration."
fi
[[ -s "$NWSYNC_MODULE_ROOT/$MODULE_NAME" ]] ||
    die "NWSync modules directory does not contain the packed module."
[[ -s "$NWSYNC_TLK_ROOT/sw_tlk.tlk" ]] ||
    die "NWSync tlk directory does not contain sw_tlk.tlk."

while IFS= read -r hak_name; do
    [[ -s "$NWSYNC_HAK_ROOT/$hak_name.hak" ]] ||
        die "NWSync is missing expected HAK: $hak_name.hak"
    [[ -s "$NWSYNC_HAK_ROOT/$hak_name.md5" ]] ||
        die "NWSync is missing expected HAK checksum cache: $hak_name.md5"
done < "$expected_haks_file"
actual_hak_count="$(
    find "$NWSYNC_HAK_ROOT" -maxdepth 1 -type f -name '*.hak' |
        wc -l |
        tr -d '[:space:]'
)"
(( actual_hak_count == expected_hak_count )) ||
    die "Expected exactly $expected_hak_count HAK files but found $actual_hak_count."

[[ -s "$NWSYNC_ROOT/latest" ]] ||
    die "NWSync build did not create the latest manifest pointer."
manifest_id="$(tr -d '\r\n' < "$NWSYNC_ROOT/latest")"
[[ "$manifest_id" =~ ^[0-9a-fA-F]{40}$ ]] ||
    die "Unexpected NWSync manifest identifier: $manifest_id"
[[ -e "$NWSYNC_ROOT/manifests/$manifest_id" ]] ||
    die "NWSync manifest $manifest_id was not created."
[[ -n "$(find "$NWSYNC_ROOT/data" -type f -print -quit)" ]] ||
    die "NWSync data directory is empty."
if (( nwsync_inputs_changed == 0 )) &&
   [[ "$manifest_id" != "$configured_server_manifest" ]]
then
    die "NWSync latest ($manifest_id) and the server manifest ($configured_server_manifest) differ even though client content was unchanged. Run an explicit --force deployment after investigating."
fi
log "Validated $expected_hak_count required HAKs ($actual_hak_count total) and manifest $manifest_id."

if [[ -n "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]]; then
    die "Deployment source has unexpected changes after building."
fi

dotnet_payload_changed=0
if (( dotnet_inputs_changed == 1 )); then
    if verify_directory_payloads \
        "$SOURCE_ROOT/SWLOR.Game.Server/bin/Release/net10.0" \
        "$SERVER_DOTNET_ROOT" >/dev/null
    then
        log "Production .NET output is unchanged; the live .NET directory will be reused."
    else
        dotnet_payload_changed=1
        log "Production .NET output changed and will be included in the cutover."
    fi
else
    dotnet_payload_changed=0
fi

manifest_changed=0
if [[ "$manifest_id" != "$configured_server_manifest" ]]; then
    manifest_changed=1
fi

runtime_payload_changed=0
if (( haks_inputs_changed == 1 ||
      module_inputs_changed == 1 ||
      dotnet_payload_changed == 1 ||
      manifest_changed == 1 ||
      server_env_migration_required == 1 ))
then
    runtime_payload_changed=1
fi

if (( runtime_payload_changed == 0 )); then
    record_state active-commit "$target_commit"
    record_state active-manifest "$manifest_id"
    record_state neverwinter-tool-key "$neverwinter_tool_key"
    record_state nwsync-build-script-sha "$nwsync_build_script_sha"
    record_state deployed-at "$(timestamp)"
    log "No deployable artifacts changed; the running Compose stack was left untouched."
    log "Deployment completed successfully."
    show_status
    exit 0
fi

if ! docker image inspect "$SERVER_IMAGE" >/dev/null 2>&1; then
    log "Building the missing server container image before downtime."
    compose build "$SERVER_SERVICE"
fi
while IFS= read -r compose_image; do
    [[ -n "$compose_image" ]] || continue
    if ! docker image inspect "$compose_image" >/dev/null 2>&1; then
        log "Pulling missing Compose image $compose_image before downtime."
        docker pull "$compose_image"
    fi
done < <(compose config --images | sort -u)

stage_sources=()
if (( haks_inputs_changed == 1 )); then
    stage_sources+=("$NWSYNC_HAK_ROOT" "$NWSYNC_TLK_ROOT")
fi
if (( module_inputs_changed == 1 )); then
    stage_sources+=("$NWSYNC_MODULE_ROOT")
fi
if (( dotnet_payload_changed == 1 )); then
    stage_sources+=("$staged_dotnet")
fi

stage_bytes="$(
    if (( ${#stage_sources[@]} == 0 )); then
        printf '0\n'
    else
        du --summarize --block-size=1 "${stage_sources[@]}" |
            awk '{ total += $1 } END { print total + 0 }'
    fi
)"
stage_reserve_bytes=$(( MIN_FREE_GIB_BEFORE_CUTOVER * 1024 * 1024 * 1024 ))
free_bytes="$(available_bytes)"
if (( free_bytes < stage_bytes + stage_reserve_bytes )); then
    die "Pre-staging requires $(( stage_bytes / 1024 / 1024 / 1024 + MIN_FREE_GIB_BEFORE_CUTOVER )) GiB free, including the configured reserve; only $(( free_bytes / 1024 / 1024 / 1024 )) GiB is available."
fi

log "Pre-staging only changed server artifacts while the live server remains online."
if (( haks_inputs_changed == 1 )); then
    rsync \
        --archive \
        --delete \
        --exclude='*.md5' \
        "$NWSYNC_HAK_ROOT/" \
        "$staged_hak/"
    rsync --archive --delete "$NWSYNC_TLK_ROOT/" "$staged_tlk/"
fi
if (( module_inputs_changed == 1 )); then
    rsync --archive --delete "$NWSYNC_MODULE_ROOT/" "$staged_modules/"
fi

log "Verifying every pre-staged artifact by checksum. This can remain silent for several minutes."
if (( haks_inputs_changed == 1 )); then
    verify_hak_payloads "$NWSYNC_HAK_ROOT" "$staged_hak" ||
        die "Pre-staged HAK verification failed."
    verify_directory_payloads "$NWSYNC_TLK_ROOT" "$staged_tlk" ||
        die "Pre-staged TLK verification failed."
fi
if (( module_inputs_changed == 1 )); then
    verify_directory_payloads "$NWSYNC_MODULE_ROOT" "$staged_modules" ||
        die "Pre-staged module verification failed."
fi
if (( dotnet_payload_changed == 1 )); then
    verify_directory_payloads \
        "$SOURCE_ROOT/SWLOR.Game.Server/bin/Release/net10.0" \
        "$staged_dotnet" ||
        die "Pre-staged .NET verification failed."
fi
check_free_space "$MIN_FREE_GIB_BEFORE_CUTOVER" "Pre-restart"

cp -a "$SERVER_ENV_FILE" "$rollback_directory/swlor.env"
cutover_started=1
log "Stopping and removing the Compose stack with a ${STOP_TIMEOUT_SECONDS}s timeout."
compose down --timeout "$STOP_TIMEOUT_SECONDS"

if (( manifest_changed == 1 )); then
    log "Setting $NWSYNC_HASH_VARIABLE to $manifest_id."
    write_manifest_hash "$manifest_id"
else
    log "$NWSYNC_HASH_VARIABLE is unchanged."
fi

if (( server_env_migration_required == 1 )); then
    log "Setting $REQUIRED_TWEAK_VARIABLE to $REQUIRED_TWEAK_VALUE."
    write_server_env_setting \
        "$REQUIRED_TWEAK_VARIABLE" \
        "$REQUIRED_TWEAK_VALUE"
else
    log "$REQUIRED_TWEAK_VARIABLE is already configured."
fi

log "Switching the server to the independently staged artifact set."
if (( haks_inputs_changed == 1 )); then
    mv "$SERVER_HAK_ROOT" "$rollback_directory/hak"
    mv "$staged_hak" "$SERVER_HAK_ROOT"

    mv "$SERVER_TLK_ROOT" "$rollback_directory/tlk"
    mv "$staged_tlk" "$SERVER_TLK_ROOT"
fi

if (( module_inputs_changed == 1 )); then
    mv "$SERVER_MODULE_ROOT" "$rollback_directory/modules"
    mv "$staged_modules" "$SERVER_MODULE_ROOT"
fi

if (( dotnet_payload_changed == 1 )); then
    mv "$SERVER_DOTNET_ROOT" "$rollback_directory/dotnet"
    mv "$staged_dotnet" "$SERVER_DOTNET_ROOT"
fi

server_started_at="$(timestamp)"
compose up -d

if ! wait_for_server_health "$server_started_at"; then
    compose logs --no-color --tail 200 "$SERVER_SERVICE" || true
    die "The deployed server failed its health check."
fi
log "Health check passed; the superseded live artifact set can now be removed."

if [[ -n "$active_commit" ]]; then
    record_state previous-commit "$active_commit"
fi
if [[ -n "$previous_manifest" ]]; then
    record_state previous-manifest "$previous_manifest"
fi
record_state active-commit "$target_commit"
record_state active-manifest "$manifest_id"
record_state neverwinter-tool-key "$neverwinter_tool_key"
record_state nwsync-build-script-sha "$nwsync_build_script_sha"
record_state deployed-at "$(timestamp)"

cutover_started=0

if [[ "$PRUNE_DANGLING_DOCKER_IMAGES" == true ]]; then
    docker image prune --force >/dev/null ||
        log "WARNING: Docker dangling-image cleanup failed."
fi

log "Deployment completed successfully."
show_status
