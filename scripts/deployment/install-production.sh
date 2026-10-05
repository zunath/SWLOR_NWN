#!/usr/bin/env bash
set -Eeuo pipefail
umask 0027
die() { printf 'Production installer: %s\n' "$*" >&2; exit 1; }
(( EUID == 0 )) || die "Run as root."
script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")"; pwd)"
source_config="${1:-$script_directory/swlor-production.conf.example}"
destination=/etc/swlor-production-deploy.conf
trusted() {
    [[ -f "$1" && ! -L "$1" && "$(realpath -m "$1")" == "$1" && "$(stat -c '%u' "$1")" == 0 ]] || die "Not a root-owned regular file: $1"
    local permissions
    permissions="$(stat -c '%a' "$1")"
    (( (8#$permissions & 0022) == 0 )) || die "File is writable by group or others: $1"
}
trusted "$source_config"
validate_config="$source_config"
[[ ! -e "$destination" ]] || validate_config="$destination"
trusted "$validate_config"
# shellcheck source=/dev/null
source "$validate_config"
[[ "${DEPLOYMENTS_ENABLED:-}" == false ]] || die "Install only with production deployments disabled."
# Refuse to replace running automation; the installer never starts or enables it.
! systemctl is-active --quiet swlor-production-deploy.service || die "Production deployment is running."
! systemctl is-active --quiet swlor-production-deploy.timer || die "Production deployment timer is running."
! systemctl is-enabled --quiet swlor-production-deploy.timer || die "Production deployment timer is enabled."
[[ "$DEPLOYMENT_ROOT" =~ ^/[A-Za-z0-9/._-]+$ && "$DEPLOYMENT_ROOT" != / &&
   "$(realpath -m "$DEPLOYMENT_ROOT")" == "$DEPLOYMENT_ROOT" &&
   "$DEPLOYMENT_ROOT" != "$SERVER_ROOT" && "$DEPLOYMENT_ROOT" != "$SERVER_ROOT/"* &&
   "$SERVER_ROOT" != "$DEPLOYMENT_ROOT/"* ]] || die "Unsafe preparation root."
for path in "$STATE_ROOT" "$CACHE_ROOT" "$RELEASE_ROOT" "$BACKUP_ROOT"; do
    [[ "$path" == "$DEPLOYMENT_ROOT/"* && "$(realpath -m "$path")" == "$path" ]] || die "Unsafe preparation path."
done
[[ "$BUILD_SCRIPT" == /usr/local/lib/swlor-production/build.sh ]] || die "Unexpected build script location."
install -d -o root -g root -m 0750 "$DEPLOYMENT_ROOT" "$STATE_ROOT" "$CACHE_ROOT" "$RELEASE_ROOT" "$BACKUP_ROOT" /usr/local/lib/swlor-production
install -o root -g root -m 0750 "$script_directory/swlor-production-build.sh" "$BUILD_SCRIPT"
install -o root -g root -m 0750 "$script_directory/swlor-production-deploy.sh" /usr/local/sbin/swlor-production-deploy
install -o root -g root -m 0750 "$script_directory/swlor-production-dispatch.sh" /usr/local/sbin/swlor-production-dispatch
[[ -e "$destination" ]] || install -o root -g root -m 0640 "$source_config" "$destination"
if [[ ! -e "$STATE_ROOT/github-dispatch-enabled-at" ]]; then
    date -u +%Y-%m-%dT%H:%M:%SZ > "$STATE_ROOT/github-dispatch-enabled-at"
    chmod 0640 "$STATE_ROOT/github-dispatch-enabled-at"
fi
install -o root -g root -m 0644 "$script_directory/swlor-production-deploy.service" /etc/systemd/system/swlor-production-deploy.service
install -o root -g root -m 0644 "$script_directory/swlor-production-deploy.timer" /etc/systemd/system/swlor-production-deploy.timer
systemctl daemon-reload
printf 'Production preparation tools installed. Deployment gate and timer remain disabled.\n'
