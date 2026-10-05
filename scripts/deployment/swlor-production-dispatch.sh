#!/usr/bin/env bash
set -Eeuo pipefail
umask 0027
die() { printf 'Production dispatch: %s\n' "$*" >&2; exit 1; }
(( EUID == 0 )) || die "Run as root."
config="${SWLOR_PRODUCTION_CONFIG:-/etc/swlor-production-deploy.conf}"
[[ -f "$config" && ! -L "$config" && "$(realpath -m "$config")" == "$config" ]] || die "Invalid configuration path."
[[ "$(stat -c '%u' "$config")" == 0 ]] || die "Configuration is not root-owned."
permissions="$(stat -c '%a' "$config")"
(( (8#$permissions & 0022) == 0 )) || die "Configuration is writable by group or others."
# shellcheck source=/dev/null
source "$config"
[[ "${DEPLOYMENTS_ENABLED:-false}" == true ]] || exit 0
[[ "$GITHUB_DEPLOY_REPOSITORY" == zunath/SWLOR_NWN &&
   "$GITHUB_DEPLOY_WORKFLOW" == request-production-deployment.yml &&
   "$GITHUB_DEPLOY_WORKFLOW_BRANCH" == master && "$GITHUB_DEPLOY_ACTOR" == zunath ]] || die "Unexpected request source."
[[ "$STATE_ROOT" == "$DEPLOYMENT_ROOT/"* && "$(realpath -m "$STATE_ROOT")" == "$STATE_ROOT" ]] || die "Unsafe state path."
[[ -d "$STATE_ROOT" && ! -L "$STATE_ROOT" ]] || die "Install production automation first."
[[ ! -e "$STATE_ROOT/recovery-required" ]] || die "A previous cutover requires recovery."
exec 8>"$STATE_ROOT/dispatch.lock"
flock --nonblock 8 || exit 0
baseline_file="$STATE_ROOT/github-dispatch-enabled-at"
[[ -s "$baseline_file" ]] || die "Missing dispatch baseline."
baseline="$(tr -d '\r\n' < "$baseline_file")"
[[ "$baseline" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$ ]] || die "Invalid baseline."
response="$(curl --silent --show-error --fail --retry 2 --connect-timeout 10 --max-time 30 \
    --header 'Accept: application/vnd.github+json' --header 'X-GitHub-Api-Version: 2022-11-28' \
    "https://api.github.com/repos/$GITHUB_DEPLOY_REPOSITORY/actions/workflows/$GITHUB_DEPLOY_WORKFLOW/runs?event=workflow_dispatch&status=completed&per_page=20")"
record="$(jq -r --arg baseline "$baseline" '
    [.workflow_runs[] | select(.event == "workflow_dispatch" and .status == "completed" and
      .conclusion == "success" and .head_branch == "master" and .actor.login == "zunath" and
      .triggering_actor.login == "zunath" and .created_at > $baseline)] | sort_by(.id) | last
    | if . == null then empty else [.id,.display_title] | @tsv end' <<< "$response")"
[[ -n "$record" ]] || exit 0
IFS=$'\t' read -r run_id title <<< "$record"
[[ "$run_id" =~ ^[0-9]+$ && "$title" =~ ^Deploy\ production\ ([0-9a-f]{40})\ ([0-9a-f]{40})$ ]] || die "Invalid production release request."
commit="${BASH_REMATCH[1]}"; manifest="${BASH_REMATCH[2]}"
claimed=0
if [[ -s "$STATE_ROOT/github-dispatch-run" ]]; then
    claimed="$(tr -d '\r\n' < "$STATE_ROOT/github-dispatch-run")"
    [[ "$claimed" =~ ^[0-9]+$ ]] || die "Invalid claimed request ID."
fi
(( run_id > claimed )) || exit 0
command="${SWLOR_PRODUCTION_COMMAND:-/usr/local/sbin/swlor-production-deploy}"
[[ -x "$command" && ! -L "$command" ]] || die "Production deployment command is unavailable."
# Failed deployments need a fresh owner request; the timer never retries them.
printf '%s\n' "$run_id" > "$STATE_ROOT/.dispatch-claim.$$"
chmod 0640 "$STATE_ROOT/.dispatch-claim.$$"
mv "$STATE_ROOT/.dispatch-claim.$$" "$STATE_ROOT/github-dispatch-run"
exec "$command" --deploy --commit "$commit" --hash "$manifest"
