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
    NWSYNC_ROOT SERVER_ROOT COMPOSE_FILE RELEASE_ROOT STATE_ROOT CACHE_ROOT
    NWSYNC_WRITE NWSYNC_DESCRIPTION MODULE_NAME
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

DEPENDENCY_SERVICES="${DEPENDENCY_SERVICES:-}"
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-}"

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
Usage: swlor-deploy [--if-changed | --force | --rollback | --status]

  --if-changed  Deploy only when GIT_REMOTE/BRANCH differs from the active
                release. This is the default used by the optional polling timer.
  --force       Rebuild and redeploy even when the commit is already active.
  --rollback    Switch to the retained previous release and verify server health.
  --status      Display active/previous releases and Compose state without changes.
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
        --rollback) MODE=rollback ;;
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
    basename chown chmod cp curl date df docker dotnet find flock git grep install jq ln \
    mv readlink realpath rm rsync sed sha256sum sleep stat tail tee tr unzip wc
do
    require_command "$required_command"
done

for numeric_setting in \
    STOP_TIMEOUT_SECONDS HEALTH_TIMEOUT_SECONDS HEALTH_STABLE_SECONDS \
    MIN_FREE_GIB_BEFORE_BUILD MIN_FREE_GIB_BEFORE_CUTOVER
do
    [[ "${!numeric_setting}" =~ ^[0-9]+$ ]] ||
        die "$numeric_setting must be a non-negative integer."
done

for absolute_path in \
    "$SOURCE_ROOT" "$NWSYNC_ROOT" "$SERVER_ROOT" "$COMPOSE_FILE" \
    "$RELEASE_ROOT" "$STATE_ROOT" "$CACHE_ROOT" "$NWSYNC_WRITE"
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
if [[ -n "$COMPOSE_PROJECT_NAME" &&
      ! "$COMPOSE_PROJECT_NAME" =~ ^[a-z0-9][a-z0-9_-]*$ ]]
then
    die "COMPOSE_PROJECT_NAME contains unsupported characters: $COMPOSE_PROJECT_NAME"
fi

install -d -o root -g root -m 0750 \
    "$RELEASE_ROOT" "$STATE_ROOT" "$CACHE_ROOT"

same_filesystem()
{
    [[ "$(stat -c '%d' "$1")" == "$(stat -c '%d' "$2")" ]]
}

for hardlink_path in "$NWSYNC_ROOT" "$SERVER_ROOT"; do
    same_filesystem "$RELEASE_ROOT" "$hardlink_path" ||
        die "$RELEASE_ROOT and $hardlink_path must be on the same filesystem."
done

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
    df --output=avail -B1 "$RELEASE_ROOT" |
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
        die "$phase requires at least ${required_gib} GiB free on $RELEASE_ROOT; only $(( free_bytes / 1024 / 1024 / 1024 )) GiB is available."
    fi

    log "$phase free-space check: $(( free_bytes / 1024 / 1024 / 1024 )) GiB available."
}

resolved_link()
{
    local link_path="$1"
    if [[ -L "$link_path" ]]; then
        readlink -f "$link_path"
    fi
}

atomic_link()
{
    local target="$1"
    local link_path="$2"
    local temporary_link="${link_path}.new.$$"

    ln -s "$target" "$temporary_link"
    mv -Tf "$temporary_link" "$link_path"
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
        die "Refusing to remove the release root."

    rm -rf --one-file-system -- "$resolved_target"
}

copy_tree_as_links()
{
    local source_directory="$1"
    local destination_directory="$2"

    install -d -o root -g root -m 0750 "$destination_directory"
    cp -al -- "$source_directory/." "$destination_directory/"
}

copy_tree_as_links_if_present()
{
    local source_directory="$1"
    local destination_directory="$2"

    install -d -o root -g root -m 0750 "$destination_directory"
    if [[ -d "$source_directory" ]]; then
        cp -al -- "$source_directory/." "$destination_directory/"
    fi
}

publish_tree()
{
    local release_directory="$1"
    local source_name="$2"
    local destination_directory="$3"
    local source_directory="$release_directory/$source_name"

    [[ -d "$source_directory" ]] ||
        die "Release directory is missing: $source_directory"
    if [[ ! -d "$destination_directory" ]]; then
        install -d -o root -g root -m 0755 "$destination_directory"
    fi

    rsync \
        --archive \
        --delete \
        --link-dest="$source_directory" \
        "$source_directory/" \
        "$destination_directory/"
}

publish_latest()
{
    local release_directory="$1"
    local temporary_latest="$NWSYNC_ROOT/.latest.$$"

    [[ -s "$release_directory/latest" ]] ||
        die "Release is missing its NWSync latest file: $release_directory"

    install -o root -g root -m 0644 \
        "$release_directory/latest" \
        "$temporary_latest"
    mv -f "$temporary_latest" "$NWSYNC_ROOT/latest"
}

release_commit()
{
    local release_directory="$1"
    if [[ -s "$release_directory/commit" ]]; then
        tr -d '\r\n' < "$release_directory/commit"
    else
        printf 'unknown'
    fi
}

validate_release()
{
    local release_directory="$1"
    local expected_hak_count
    local actual_hak_count
    local manifest_id

    [[ -s "$release_directory/dotnet/SWLOR.Game.Server.dll" ]] ||
        die "Release does not contain SWLOR.Game.Server.dll."
    [[ -s "$release_directory/dotnet/SWLOR.Game.Server.runtimeconfig.json" ]] ||
        die "Release does not contain the server runtime configuration."
    [[ -s "$release_directory/modules/$MODULE_NAME" ]] ||
        die "Release does not contain the packed module."
    [[ -s "$release_directory/tlk/sw_tlk.tlk" ]] ||
        die "Release does not contain sw_tlk.tlk."
    [[ -s "$release_directory/latest" ]] ||
        die "NWSync did not create the latest manifest pointer."

    expected_hak_count="$(
        jq '[.HakList[] | select(. != null and (.Name // "") != "")] | length' \
            "$SOURCE_ROOT/Build/hakbuilder.json"
    )"
    while IFS= read -r hak_name; do
        [[ -s "$release_directory/hak/$hak_name.hak" ]] ||
            die "Release is missing expected HAK: $hak_name.hak"
    done < <(
        jq -r \
            '.HakList[] | select(. != null and (.Name // "") != "") | .Name' \
            "$SOURCE_ROOT/Build/hakbuilder.json"
    )
    actual_hak_count="$(
        find "$release_directory/hak" -maxdepth 1 -type f -name '*.hak' |
            wc -l |
            tr -d '[:space:]'
    )"

    (( actual_hak_count >= expected_hak_count )) ||
        die "Expected at least $expected_hak_count HAK files but found $actual_hak_count."

    manifest_id="$(tr -d '\r\n' < "$release_directory/latest")"
    [[ "$manifest_id" =~ ^[0-9a-fA-F]{40}$ ]] ||
        die "Unexpected NWSync manifest identifier: $manifest_id"
    [[ -e "$release_directory/manifests/$manifest_id" ]] ||
        die "NWSync manifest $manifest_id was not created."
    [[ -n "$(find "$release_directory/data" -type f -print -quit)" ]] ||
        die "NWSync data directory is empty."

    log "Validated release: $expected_hak_count required HAKs present ($actual_hak_count total), manifest $manifest_id."
}

validate_rollback_release()
{
    local release_directory="$1"

    [[ -d "$release_directory/dotnet" ]] ||
        die "Rollback release is missing its dotnet directory."
    [[ -d "$release_directory/hak" ]] ||
        die "Rollback release is missing its hak directory."
    [[ -d "$release_directory/modules" ]] ||
        die "Rollback release is missing its modules directory."
    [[ -d "$release_directory/tlk" ]] ||
        die "Rollback release is missing its tlk directory."
    [[ -s "$release_directory/latest" ]] ||
        die "Rollback release is missing its NWSync latest pointer."
}

wait_for_server_health()
{
    local started_at="$1"
    local deadline=$(( $(date +%s) + HEALTH_TIMEOUT_SECONDS ))
    local container_id
    local restart_count
    local state
    local current_id

    container_id="$(compose ps -q "$SERVER_SERVICE")"
    [[ -n "$container_id" ]] || return 1
    restart_count="$(docker inspect --format '{{.RestartCount}}' "$container_id")"

    log "Waiting up to ${HEALTH_TIMEOUT_SECONDS}s for '$HEALTH_LOG_MARKER'."
    while (( $(date +%s) < deadline )); do
        state="$(docker inspect --format '{{.State.Status}}' "$container_id" 2>/dev/null || true)"
        [[ "$state" == running ]] || return 1

        if compose logs --no-color --since "$started_at" "$SERVER_SERVICE" 2>&1 |
            grep -Fq "$HEALTH_LOG_MARKER"
        then
            break
        fi
        sleep 5
    done

    if ! compose logs --no-color --since "$started_at" "$SERVER_SERVICE" 2>&1 |
        grep -Fq "$HEALTH_LOG_MARKER"
    then
        return 1
    fi

    log "Startup marker found; checking ${HEALTH_STABLE_SECONDS}s of stability."
    local stable_deadline=$(( $(date +%s) + HEALTH_STABLE_SECONDS ))
    while (( $(date +%s) < stable_deadline )); do
        current_id="$(compose ps -q "$SERVER_SERVICE")"
        [[ "$current_id" == "$container_id" ]] || return 1
        [[ "$(docker inspect --format '{{.State.Status}}' "$container_id")" == running ]] ||
            return 1
        [[ "$(docker inspect --format '{{.RestartCount}}' "$container_id")" == "$restart_count" ]] ||
            return 1
        sleep 5
    done

    return 0
}

restore_release()
{
    local release_directory="$1"

    log "Restoring release $(basename "$release_directory")."
    compose stop --timeout "$STOP_TIMEOUT_SECONDS" "$SERVER_SERVICE" || true
    publish_tree "$release_directory" dotnet "$SERVER_ROOT/dotnet"
    publish_tree "$release_directory" hak "$NWSYNC_ROOT/hak"
    publish_tree "$release_directory" modules "$NWSYNC_ROOT/modules"
    publish_tree "$release_directory" tlk "$NWSYNC_ROOT/tlk"
    publish_latest "$release_directory"
}

create_baseline_if_needed()
{
    local current_link="$STATE_ROOT/current"
    local baseline_id
    local baseline_directory

    if [[ -n "$(resolved_link "$current_link")" ]]; then
        return
    fi

    baseline_id="baseline-$(date -u '+%Y%m%dT%H%M%SZ')"
    baseline_directory="$RELEASE_ROOT/$baseline_id"
    log "Capturing the current live files as rollback baseline $baseline_id."

    install -d -o root -g root -m 0750 "$baseline_directory"
    copy_tree_as_links "$SERVER_ROOT/dotnet" "$baseline_directory/dotnet"
    copy_tree_as_links "$NWSYNC_ROOT/hak" "$baseline_directory/hak"
    copy_tree_as_links "$NWSYNC_ROOT/modules" "$baseline_directory/modules"
    copy_tree_as_links "$NWSYNC_ROOT/tlk" "$baseline_directory/tlk"
    install -o root -g root -m 0644 "$NWSYNC_ROOT/latest" "$baseline_directory/latest"
    printf 'legacy-unknown\n' > "$baseline_directory/commit"
    printf '%s\n' "$(timestamp)" > "$baseline_directory/created-at"
    atomic_link "$baseline_directory" "$current_link"
}

show_status()
{
    local current_directory
    local previous_directory

    current_directory="$(resolved_link "$STATE_ROOT/current")"
    previous_directory="$(resolved_link "$STATE_ROOT/previous")"

    printf 'Source:   %s\n' "$SOURCE_ROOT"
    printf 'Branch:   %s/%s\n' "$GIT_REMOTE" "$BRANCH"
    printf 'Current:  %s\n' "${current_directory:-not recorded}"
    if [[ -n "$current_directory" ]]; then
        printf 'Commit:   %s\n' "$(release_commit "$current_directory")"
    fi
    printf 'Previous: %s\n' "${previous_directory:-not recorded}"
    if [[ -n "$previous_directory" ]]; then
        printf 'Commit:   %s\n' "$(release_commit "$previous_directory")"
    fi
    compose ps
    df -h "$RELEASE_ROOT"
}

rollback_manually()
{
    local current_directory
    local previous_directory
    local rollback_started_at

    current_directory="$(resolved_link "$STATE_ROOT/current")"
    previous_directory="$(resolved_link "$STATE_ROOT/previous")"
    [[ -d "$current_directory" ]] || die "The active release is not recorded."
    [[ -d "$previous_directory" ]] || die "There is no retained previous release."
    validate_rollback_release "$current_directory"
    validate_rollback_release "$previous_directory"

    restore_release "$previous_directory"
    rollback_started_at="$(timestamp)"
    compose up -d --no-deps "$SERVER_SERVICE"
    if ! wait_for_server_health "$rollback_started_at"; then
        log "Previous release failed its health check; restoring the release that was active."
        restore_release "$current_directory"
        rollback_started_at="$(timestamp)"
        compose up -d --no-deps "$SERVER_SERVICE"
        if wait_for_server_health "$rollback_started_at"; then
            log "The originally active release was restored successfully."
        else
            log "CRITICAL: the originally active release also failed its health check."
        fi
        die "Manual rollback failed and the release pointers were not changed."
    fi

    atomic_link "$current_directory" "$STATE_ROOT/previous"
    atomic_link "$previous_directory" "$STATE_ROOT/current"
    log "Rollback completed successfully."
    show_status
}

if [[ "$MODE" == status ]]; then
    show_status
    exit 0
fi

if [[ "$MODE" == rollback ]]; then
    rollback_manually
    exit 0
fi

[[ -d "$SOURCE_ROOT/.git" || -f "$SOURCE_ROOT/.git" ]] ||
    die "Deployment source is not a Git repository: $SOURCE_ROOT"
[[ -d "$NWSYNC_ROOT" ]] || die "NWSync root does not exist: $NWSYNC_ROOT"
[[ -d "$SERVER_ROOT" ]] || die "Server root does not exist: $SERVER_ROOT"
[[ -f "$COMPOSE_FILE" ]] || die "Compose file does not exist: $COMPOSE_FILE"
[[ -x "$NWSYNC_WRITE" ]] || die "nwsync_write is not executable: $NWSYNC_WRITE"
[[ "$(stat -c '%u:%g' "$SOURCE_ROOT")" == 0:0 ]] ||
    die "Deployment source must be owned by root:root."

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
fi

log "Synchronizing recursive submodules."
git -C "$SOURCE_ROOT" submodule sync --recursive
git -C "$SOURCE_ROOT" submodule update --init --recursive --depth 1

if [[ -n "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]]; then
    die "Deployment source is not clean after updating."
fi

target_commit="$(git -C "$SOURCE_ROOT" rev-parse HEAD)"
current_release="$(resolved_link "$STATE_ROOT/current")"
if [[ "$MODE" == if-changed && -n "$current_release" &&
      "$(release_commit "$current_release")" == "$target_commit" ]]
then
    log "Commit $target_commit is already active; nothing to deploy."
    exit 0
fi

release_id="release-${target_commit:0:12}-$(date -u '+%Y%m%dT%H%M%SZ')"
partial_release="$RELEASE_ROOT/.${release_id}.partial.$$"
new_release="$RELEASE_ROOT/$release_id"
work_directory="$RELEASE_ROOT/.work-${release_id}.$$"
rollback_directory=
cutover_started=0
deployment_succeeded=0

cleanup_on_exit()
{
    local exit_status=$?
    trap - EXIT INT TERM
    set +e

    if (( exit_status != 0 && cutover_started == 1 )) &&
       [[ -n "$rollback_directory" && -d "$rollback_directory" ]]
    then
        log "Deployment failed after cutover began; starting automatic rollback."
        restore_release "$rollback_directory"
        local rollback_started_at
        rollback_started_at="$(timestamp)"
        compose up -d --no-deps "$SERVER_SERVICE"
        if wait_for_server_health "$rollback_started_at"; then
            log "Automatic rollback passed its health check."
        else
            log "CRITICAL: automatic rollback did not pass its health check."
        fi
    fi

    if [[ -n "${work_directory:-}" && -e "$work_directory" ]]; then
        safe_remove_under "$RELEASE_ROOT" "$work_directory"
    fi
    if [[ -n "${partial_release:-}" && -e "$partial_release" ]]; then
        safe_remove_under "$RELEASE_ROOT" "$partial_release"
    fi
    if (( deployment_succeeded == 0 )) &&
       [[ -n "${new_release:-}" && -d "$new_release" ]]
    then
        safe_remove_under "$RELEASE_ROOT" "$new_release"
    fi

    exit "$exit_status"
}

trap cleanup_on_exit EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

install -d -o root -g root -m 0750 \
    "$partial_release" "$work_directory" \
    "$partial_release/dotnet" "$partial_release/hak" \
    "$partial_release/modules" "$partial_release/tlk"

# Seed the release with hard links to current content. HakBuilder's checksum
# files then let unchanged HAKs be reused without rebuilding or consuming a
# second copy on disk.
copy_tree_as_links_if_present "$NWSYNC_ROOT/hak" "$partial_release/hak"
copy_tree_as_links_if_present "$NWSYNC_ROOT/tlk" "$partial_release/tlk"
copy_tree_as_links_if_present "$NWSYNC_ROOT/data" "$partial_release/data"
copy_tree_as_links_if_present "$NWSYNC_ROOT/manifests" "$partial_release/manifests"
if [[ -f "$NWSYNC_ROOT/latest" ]]; then
    install -o root -g root -m 0644 "$NWSYNC_ROOT/latest" "$partial_release/latest"
fi

install -d -o root -g root -m 0750 \
    "$CACHE_ROOT/nuget" "$CACHE_ROOT/dotnet-home" "$CACHE_ROOT/tmp"
export NUGET_PACKAGES="$CACHE_ROOT/nuget"
export DOTNET_CLI_HOME="$CACHE_ROOT/dotnet-home"
export TMPDIR="$CACHE_ROOT/tmp"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

log "Building SWLOR.CLI and the production game server for commit $target_commit."
dotnet build "$SOURCE_ROOT/SWLOR.CLI/SWLOR.CLI.csproj" \
    --configuration Release \
    --property:OS=Unix \
    --property:RunPostBuildEvent=Never

tools_directory="$CACHE_ROOT/neverwinter/$NEVERWINTER_NIM_VERSION"
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

cli_directory="$SOURCE_ROOT/SWLOR.CLI/bin/Release/net10.0"
for tool_name in nwn_erf nwn_gff nwn_tlk; do
    ln -sfn "$tools_directory/$tool_name" "$cli_directory/$tool_name.exe"
done

jq \
    --arg source "$SOURCE_ROOT" \
    --arg output "$partial_release/" \
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

log "Building changed HAKs and staging the TLK."
(
    cd "$work_directory"
    dotnet "$cli_directory/SWLOR.CLI.dll" --hak
)

log "Packing $MODULE_NAME."
if same_filesystem "$SOURCE_ROOT" "$work_directory"; then
    cp -al -- "$SOURCE_ROOT/Module" "$work_directory/Module"
else
    cp -a -- "$SOURCE_ROOT/Module" "$work_directory/Module"
fi
safe_remove_under "$work_directory/Module" "$work_directory/Module/packing"
rm -f -- "$work_directory/Module/$MODULE_NAME"
(
    cd "$work_directory/Module"
    dotnet "$cli_directory/SWLOR.CLI.dll" --pack "./$MODULE_NAME"
)
install -o root -g root -m 0644 \
    "$work_directory/Module/$MODULE_NAME" \
    "$partial_release/modules/$MODULE_NAME"

log "Staging production .NET assemblies."
rsync \
    --archive \
    --delete \
    "$SOURCE_ROOT/SWLOR.Game.Server/bin/Release/net10.0/" \
    "$partial_release/dotnet/"

printf '%s\n' "$target_commit" > "$partial_release/commit"
printf '%s\n' "$BRANCH" > "$partial_release/branch"
printf '%s\n' "$(timestamp)" > "$partial_release/created-at"

log "Generating the staged NWSync manifest while the live server remains online."
"$NWSYNC_WRITE" \
    --description="$NWSYNC_DESCRIPTION ($target_commit)" \
    "$partial_release" \
    "$partial_release/modules/$MODULE_NAME"

validate_release "$partial_release"
check_free_space "$MIN_FREE_GIB_BEFORE_CUTOVER" "Pre-cutover"

mv "$partial_release" "$new_release"
partial_release=

# NWSync payloads and the new manifest are immutable/content-addressed. Publish
# them while the old latest pointer remains active; latest is switched only
# after the game server has stopped.
log "Pre-publishing immutable NWSync data without changing the active manifest."
rsync \
    --archive \
    --ignore-existing \
    --link-dest="$new_release/data" \
    "$new_release/data/" \
    "$NWSYNC_ROOT/data/"
rsync \
    --archive \
    --ignore-existing \
    --link-dest="$new_release/manifests" \
    "$new_release/manifests/" \
    "$NWSYNC_ROOT/manifests/"

create_baseline_if_needed
rollback_directory="$(resolved_link "$STATE_ROOT/current")"
[[ -d "$rollback_directory" ]] || die "Unable to identify the rollback release."

if ! docker image inspect "$SERVER_IMAGE" >/dev/null 2>&1; then
    log "Building the missing server container image before downtime."
    compose build "$SERVER_SERVICE"
fi

read -r -a dependency_services <<< "$DEPENDENCY_SERVICES"
if (( ${#dependency_services[@]} > 0 )); then
    for dependency_service in "${dependency_services[@]}"; do
        [[ "$dependency_service" =~ ^[A-Za-z0-9._-]+$ ]] ||
            die "DEPENDENCY_SERVICES contains an invalid service name."
    done
    log "Ensuring supporting Compose services are running before cutover."
    compose up -d "${dependency_services[@]}"
fi

cutover_started=1
log "Stopping $SERVER_SERVICE with a ${STOP_TIMEOUT_SECONDS}s graceful timeout."
compose stop --timeout "$STOP_TIMEOUT_SECONDS" "$SERVER_SERVICE"

log "Publishing the fully built release."
publish_tree "$new_release" dotnet "$SERVER_ROOT/dotnet"
publish_tree "$new_release" hak "$NWSYNC_ROOT/hak"
publish_tree "$new_release" modules "$NWSYNC_ROOT/modules"
publish_tree "$new_release" tlk "$NWSYNC_ROOT/tlk"
publish_latest "$new_release"

server_started_at="$(timestamp)"
compose up -d --no-deps "$SERVER_SERVICE"
if ! wait_for_server_health "$server_started_at"; then
    compose logs --no-color --tail 200 "$SERVER_SERVICE" || true
    die "The new release failed its server health check."
fi

atomic_link "$rollback_directory" "$STATE_ROOT/previous"
atomic_link "$new_release" "$STATE_ROOT/current"
cutover_started=0
deployment_succeeded=1

# NWSync payloads have been linked into the live repository. Releases only
# need their server assets and latest pointer for rollback.
safe_remove_under "$new_release" "$new_release/data"
safe_remove_under "$new_release" "$new_release/manifests"

current_directory="$(resolved_link "$STATE_ROOT/current")"
previous_directory="$(resolved_link "$STATE_ROOT/previous")"
for candidate in "$RELEASE_ROOT"/release-* "$RELEASE_ROOT"/baseline-*; do
    [[ -d "$candidate" ]] || continue
    candidate="$(realpath -m "$candidate")"
    if [[ "$candidate" != "$current_directory" &&
          "$candidate" != "$previous_directory" ]]
    then
        log "Removing expired release $(basename "$candidate")."
        safe_remove_under "$RELEASE_ROOT" "$candidate"
    fi
done

if [[ "$PRUNE_DANGLING_DOCKER_IMAGES" == true ]]; then
    docker image prune --force >/dev/null ||
        log "WARNING: Docker dangling-image cleanup failed."
fi

log "Deployment completed successfully."
show_status
