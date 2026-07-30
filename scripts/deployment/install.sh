#!/usr/bin/env bash

set -Eeuo pipefail
umask 0027

if (( EUID != 0 )); then
    printf 'Run this installer as root.\n' >&2
    exit 1
fi

script_directory="$(
    cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
    pwd
)"

config_source="${1:-$script_directory/swlor-deploy.conf.example}"
config_path=/etc/swlor-deploy.conf
command_path=/usr/local/sbin/swlor-deploy
systemd_unit_directory=/etc/systemd/system

[[ -f "$config_source" && ! -L "$config_source" ]] || {
    printf 'Configuration source is not a regular file: %s\n' "$config_source" >&2
    exit 1
}

[[ "$(stat -c '%u' "$config_source")" == 0 ]] || {
    printf 'Configuration source must be owned by root: %s\n' "$config_source" >&2
    exit 1
}

config_source_permissions="$(stat -c '%a' "$config_source")"
if (( (8#$config_source_permissions & 0022) != 0 )); then
    printf 'Configuration source must not be writable by group or others: %s\n' \
        "$config_source" >&2
    exit 1
fi

install -d -o root -g root -m 0755 /usr/local/sbin

install -o root -g root -m 0750 \
    "$script_directory/swlor-deploy.sh" \
    "$command_path"

if [[ ! -e "$config_path" ]]; then
    install -o root -g root -m 0640 \
        "$config_source" \
        "$config_path"
    printf 'Created %s\n' "$config_path"
else
    printf 'Preserved existing %s\n' "$config_path"
fi

[[ -f "$config_path" && ! -L "$config_path" ]] || {
    printf 'Installed configuration is not a regular file: %s\n' "$config_path" >&2
    exit 1
}
[[ "$(stat -c '%u' "$config_path")" == 0 ]] || {
    printf 'Installed configuration must be owned by root: %s\n' "$config_path" >&2
    exit 1
}
config_permissions="$(stat -c '%a' "$config_path")"
if (( (8#$config_permissions & 0022) != 0 )); then
    printf 'Installed configuration must not be writable by group or others: %s\n' \
        "$config_path" >&2
    exit 1
fi

# shellcheck source=/dev/null
source "$config_path"

required_settings=(
    DEPLOYMENT_NAME SOURCE_ROOT RELEASE_ROOT STATE_ROOT CACHE_ROOT LOG_FILE
)
for required_setting in "${required_settings[@]}"; do
    [[ -n "${!required_setting:-}" ]] || {
        printf 'Required configuration value is missing: %s\n' \
            "$required_setting" >&2
        exit 1
    }
done

[[ -d "$SOURCE_ROOT/.git" || -f "$SOURCE_ROOT/.git" ]] || {
    printf 'Deployment source is not a Git repository: %s\n' "$SOURCE_ROOT" >&2
    exit 1
}

[[ "$(stat -c '%u:%g' "$SOURCE_ROOT")" == 0:0 ]] || {
    printf 'Deployment source must be owned by root:root: %s\n' "$SOURCE_ROOT" >&2
    exit 1
}

chmod 0750 "$SOURCE_ROOT"
install -d -o root -g root -m 0750 \
    "$RELEASE_ROOT" "$STATE_ROOT" "$CACHE_ROOT"

install -d -o root -g root -m 0755 "$systemd_unit_directory"
install -o root -g root -m 0644 \
    "$script_directory/swlor-deploy.service" \
    "$systemd_unit_directory/swlor-deploy.service"
install -o root -g root -m 0644 \
    "$script_directory/swlor-deploy.timer" \
    "$systemd_unit_directory/swlor-deploy.timer"
log_directory="$(dirname -- "$LOG_FILE")"
install -d -o root -g root -m 0755 "$log_directory"
if [[ ! -e "$LOG_FILE" ]]; then
    install -o root -g root -m 0640 /dev/null "$LOG_FILE"
else
    chown root:root "$LOG_FILE"
    chmod 0640 "$LOG_FILE"
fi

systemctl daemon-reload

printf '\nDeployment automation installed.\n'
printf 'Deployment:    %s\n' "$DEPLOYMENT_NAME"
printf 'Configuration: %s\n' "$config_path"
printf 'Manual deploy: %s\n' "$command_path"
printf 'Status:        %s --status\n' "$command_path"
printf 'Rollback:      %s --rollback\n' "$command_path"
printf '\nPolling remains disabled. Enable it later with:\n'
printf '  systemctl enable --now swlor-deploy.timer\n'
