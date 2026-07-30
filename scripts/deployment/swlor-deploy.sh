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

DEPENDENCY_SERVICES="${DEPENDENCY_SERVICES:-}"
COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-}"
HAKS_SUBMODULE_PATH="${HAKS_SUBMODULE_PATH:-SWLOR_Haks}"
NWSYNC_BUILD_SCRIPT="${NWSYNC_BUILD_SCRIPT:-$NWSYNC_ROOT/build.sh}"
NWSYNC_DOTNET_ROOT="${NWSYNC_DOTNET_ROOT:-$NWSYNC_ROOT/dotnet}"
SERVER_DOTNET_ROOT="${SERVER_DOTNET_ROOT:-$SERVER_ROOT/dotnet}"
SERVER_CONTENT_MODE="${SERVER_CONTENT_MODE:-bind}"

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
    awk basename chown chmod cp curl date df docker dotnet find findmnt flock \
    git grep install jq ln mktemp mv realpath rm rsync sed sha256sum sleep \
    stat tail tee tr unzip wc
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
    "$STATE_ROOT" "$CACHE_ROOT" "$NWSYNC_BUILD_SCRIPT" \
    "$NWSYNC_DOTNET_ROOT" "$SERVER_DOTNET_ROOT"
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
case "$SERVER_CONTENT_MODE" in
    bind|copy) ;;
    *) die "SERVER_CONTENT_MODE must be either 'bind' or 'copy'." ;;
esac

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

show_status()
{
    local active_commit
    local previous_commit
    local active_manifest

    active_commit="$(state_value active-commit)"
    previous_commit="$(state_value previous-commit)"
    if [[ -s "$NWSYNC_ROOT/latest" ]]; then
        active_manifest="$(tr -d '\r\n' < "$NWSYNC_ROOT/latest")"
    else
        active_manifest=not-present
    fi

    printf 'Source:           %s\n' "$SOURCE_ROOT"
    printf 'Branch:           %s/%s\n' "$GIT_REMOTE" "$BRANCH"
    printf 'Active commit:    %s\n' "${active_commit:-not recorded}"
    printf 'Previous commit:  %s\n' "${previous_commit:-not recorded}"
    printf 'NWSync root:      %s\n' "$NWSYNC_ROOT"
    printf 'NWSync manifest:  %s\n' "$active_manifest"
    printf 'Server content:   %s\n' "$SERVER_CONTENT_MODE"
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
[[ -x "$NWSYNC_BUILD_SCRIPT" ]] ||
    die "NWSync build script is not executable: $NWSYNC_BUILD_SCRIPT"
[[ "$(stat -c '%u:%g' "$SOURCE_ROOT")" == 0:0 ]] ||
    die "Deployment source must be owned by root:root."

for content_directory in hak modules tlk; do
    [[ -d "$NWSYNC_ROOT/$content_directory" ]] ||
        die "NWSync content directory is missing: $NWSYNC_ROOT/$content_directory"
done
install -d -o root -g root -m 0755 "$NWSYNC_DOTNET_ROOT"

if [[ "$SERVER_CONTENT_MODE" == bind ]]; then
    for content_directory in hak modules tlk; do
        server_content_path="$SERVER_ROOT/$content_directory"
        [[ "$(findmnt -T "$server_content_path" -n -o TARGET)" == "$server_content_path" ]] ||
            die "$server_content_path is not a dedicated bind mount. Set SERVER_CONTENT_MODE=copy if this host does not bind NWSync content into the server tree."
    done
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

work_directory="$(mktemp -d "$CACHE_ROOT/work.XXXXXXXX")"
module_temporary=
cutover_started=0
deployment_succeeded=0

cleanup_on_exit()
{
    local exit_status=$?
    trap - EXIT INT TERM
    set +e

    if [[ -n "${work_directory:-}" && -e "$work_directory" ]]; then
        safe_remove_under "$CACHE_ROOT" "$work_directory"
    fi
    if [[ -n "${module_temporary:-}" && -e "$module_temporary" ]]; then
        safe_remove_under "$NWSYNC_ROOT/modules" "$module_temporary"
    fi
    if [[ -d "$SOURCE_ROOT/Module/packing" ]]; then
        safe_remove_under "$SOURCE_ROOT/Module" "$SOURCE_ROOT/Module/packing"
    fi

    if (( exit_status != 0 && cutover_started == 1 )); then
        log "WARNING: deployment failed after server restart began. No local HAK/TLK rollback copy is retained."
        container_id="$(compose ps -q "$SERVER_SERVICE" 2>/dev/null || true)"
        container_state=
        if [[ -n "$container_id" ]]; then
            container_state="$(
                docker inspect --format '{{.State.Status}}' "$container_id" 2>/dev/null ||
                    true
            )"
        fi
        if [[ "$container_state" != running ]]; then
            log "Attempting to leave $SERVER_SERVICE running for diagnosis."
            compose up -d --no-deps "$SERVER_SERVICE" ||
                log "CRITICAL: $SERVER_SERVICE could not be started."
        fi
    fi

    exit "$exit_status"
}

trap cleanup_on_exit EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

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

# HakBuilder uses the existing .hak/.md5 pairs in NWSYNC_ROOT and rebuilds only
# changed HAKs. It also installs the current TLK directly into NWSYNC_ROOT/tlk.
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

log "Building changed HAKs and the TLK directly in $NWSYNC_ROOT."
(
    cd "$work_directory"
    dotnet "$cli_directory/SWLOR.CLI.dll" --hak
)

if [[ -d "$SOURCE_ROOT/Module/packing" ]]; then
    safe_remove_under "$SOURCE_ROOT/Module" "$SOURCE_ROOT/Module/packing"
fi
log "Packing $MODULE_NAME."
(
    cd "$SOURCE_ROOT/Module"
    dotnet "$cli_directory/SWLOR.CLI.dll" --pack "./$MODULE_NAME"
)
module_temporary="$NWSYNC_ROOT/modules/.${MODULE_NAME}.new.$$"
install -o root -g root -m 0644 \
    "$SOURCE_ROOT/Module/$MODULE_NAME" \
    "$module_temporary"
mv -f "$module_temporary" "$NWSYNC_ROOT/modules/$MODULE_NAME"

log "Installing production .NET assemblies into $NWSYNC_DOTNET_ROOT."
rsync \
    --archive \
    --delete \
    "$SOURCE_ROOT/SWLOR.Game.Server/bin/Release/net10.0/" \
    "$NWSYNC_DOTNET_ROOT/"

previous_manifest=
if [[ -s "$NWSYNC_ROOT/latest" ]]; then
    previous_manifest="$(tr -d '\r\n' < "$NWSYNC_ROOT/latest")"
fi

log "Running the existing NWSync build script from $NWSYNC_ROOT."
(
    cd "$NWSYNC_ROOT"
    "$NWSYNC_BUILD_SCRIPT"
)

[[ -s "$NWSYNC_DOTNET_ROOT/SWLOR.Game.Server.dll" ]] ||
    die "NWSync dotnet directory does not contain SWLOR.Game.Server.dll."
[[ -s "$NWSYNC_DOTNET_ROOT/SWLOR.Game.Server.runtimeconfig.json" ]] ||
    die "NWSync dotnet directory does not contain the server runtime configuration."
[[ -s "$NWSYNC_ROOT/modules/$MODULE_NAME" ]] ||
    die "NWSync modules directory does not contain the packed module."
[[ -s "$NWSYNC_ROOT/tlk/sw_tlk.tlk" ]] ||
    die "NWSync tlk directory does not contain sw_tlk.tlk."

expected_hak_count="$(
    jq '[.HakList[] | select(. != null and (.Name // "") != "")] | length' \
        "$SOURCE_ROOT/Build/hakbuilder.json"
)"
while IFS= read -r hak_name; do
    [[ -s "$NWSYNC_ROOT/hak/$hak_name.hak" ]] ||
        die "NWSync is missing expected HAK: $hak_name.hak"
done < <(
    jq -r \
        '.HakList[] | select(. != null and (.Name // "") != "") | .Name' \
        "$SOURCE_ROOT/Build/hakbuilder.json"
)
actual_hak_count="$(
    find "$NWSYNC_ROOT/hak" -maxdepth 1 -type f -name '*.hak' |
        wc -l |
        tr -d '[:space:]'
)"
(( actual_hak_count >= expected_hak_count )) ||
    die "Expected at least $expected_hak_count HAK files but found $actual_hak_count."

[[ -s "$NWSYNC_ROOT/latest" ]] ||
    die "NWSync build did not create the latest manifest pointer."
manifest_id="$(tr -d '\r\n' < "$NWSYNC_ROOT/latest")"
[[ "$manifest_id" =~ ^[0-9a-fA-F]{40}$ ]] ||
    die "Unexpected NWSync manifest identifier: $manifest_id"
[[ -e "$NWSYNC_ROOT/manifests/$manifest_id" ]] ||
    die "NWSync manifest $manifest_id was not created."
[[ -n "$(find "$NWSYNC_ROOT/data" -type f -print -quit)" ]] ||
    die "NWSync data directory is empty."
log "Validated $expected_hak_count required HAKs ($actual_hak_count total) and manifest $manifest_id."

if [[ -n "$(git -C "$SOURCE_ROOT" status --porcelain --untracked-files=all)" ]]; then
    die "Deployment source has unexpected changes after building."
fi
check_free_space "$MIN_FREE_GIB_BEFORE_CUTOVER" "Pre-restart"

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
    log "Ensuring supporting Compose services are running before server downtime."
    compose up -d "${dependency_services[@]}"
fi

cutover_started=1
log "Stopping $SERVER_SERVICE with a ${STOP_TIMEOUT_SECONDS}s graceful timeout."
compose stop --timeout "$STOP_TIMEOUT_SECONDS" "$SERVER_SERVICE"

log "Publishing .NET assemblies to $SERVER_DOTNET_ROOT."
install -d -o root -g root -m 0755 "$SERVER_DOTNET_ROOT"
rsync \
    --archive \
    --delete \
    "$NWSYNC_DOTNET_ROOT/" \
    "$SERVER_DOTNET_ROOT/"

if [[ "$SERVER_CONTENT_MODE" == copy ]]; then
    for content_directory in hak modules tlk; do
        log "Publishing $content_directory to $SERVER_ROOT/$content_directory."
        install -d -o root -g root -m 0755 "$SERVER_ROOT/$content_directory"
        rsync \
            --archive \
            --delete \
            "$NWSYNC_ROOT/$content_directory/" \
            "$SERVER_ROOT/$content_directory/"
    done
fi

server_started_at="$(timestamp)"
compose up -d --no-deps "$SERVER_SERVICE"

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

if ! wait_for_server_health "$server_started_at"; then
    compose logs --no-color --tail 200 "$SERVER_SERVICE" || true
    die "The deployed server failed its health check."
fi

if [[ -n "$active_commit" ]]; then
    record_state previous-commit "$active_commit"
fi
if [[ -n "$previous_manifest" ]]; then
    record_state previous-manifest "$previous_manifest"
fi
record_state active-commit "$target_commit"
record_state active-manifest "$manifest_id"
record_state deployed-at "$(timestamp)"

cutover_started=0
deployment_succeeded=1

if [[ "$PRUNE_DANGLING_DOCKER_IMAGES" == true ]]; then
    docker image prune --force >/dev/null ||
        log "WARNING: Docker dangling-image cleanup failed."
fi

log "Deployment completed successfully."
show_status
