#!/usr/bin/env bash

set -Eeuo pipefail
umask 0027

CONFIG_FILE="${SWLOR_DEPLOY_CONFIG:-/etc/swlor-deploy.conf}"
DEPLOY_COMMAND="${SWLOR_DEPLOY_COMMAND:-/usr/local/sbin/swlor-deploy}"

die() {
    printf 'SWLOR GitHub dispatch poller: %s\n' "$*" >&2
    exit 1
}

(( EUID == 0 )) || die "This command must run as root."
for required_command in chown chmod curl date install jq mv stat tr; do
    command -v "$required_command" >/dev/null 2>&1 ||
        die "Required command is not installed: $required_command"
done

[[ -f "$CONFIG_FILE" && ! -L "$CONFIG_FILE" ]] ||
    die "Configuration is not a regular file: $CONFIG_FILE"
[[ "$(stat -c '%u' "$CONFIG_FILE")" == 0 ]] ||
    die "Configuration must be owned by root: $CONFIG_FILE"
config_permissions="$(stat -c '%a' "$CONFIG_FILE")"
(( (8#$config_permissions & 0022) == 0 )) ||
    die "Configuration must not be writable by group or others: $CONFIG_FILE"

# shellcheck source=/dev/null
source "$CONFIG_FILE"

required_settings=(
    DEPLOYMENT_NAME
    STATE_ROOT
    GITHUB_DEPLOY_REPOSITORY
    GITHUB_DEPLOY_WORKFLOW
    GITHUB_DEPLOY_WORKFLOW_BRANCH
    GITHUB_DEPLOY_ACTOR
)
for required_setting in "${required_settings[@]}"; do
    [[ -n "${!required_setting:-}" ]] ||
        die "Required configuration value is missing: $required_setting"
done

[[ "$GITHUB_DEPLOY_REPOSITORY" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] ||
    die "Invalid GITHUB_DEPLOY_REPOSITORY."
[[ "$GITHUB_DEPLOY_WORKFLOW" =~ ^[A-Za-z0-9_.-]+\.ya?ml$ ]] ||
    die "GITHUB_DEPLOY_WORKFLOW must be a workflow filename."
[[ "$GITHUB_DEPLOY_WORKFLOW_BRANCH" =~ ^[A-Za-z0-9._/-]+$ ]] ||
    die "Invalid GITHUB_DEPLOY_WORKFLOW_BRANCH."
[[ "$GITHUB_DEPLOY_ACTOR" =~ ^[A-Za-z0-9-]+$ ]] ||
    die "Invalid GITHUB_DEPLOY_ACTOR."
[[ -x "$DEPLOY_COMMAND" ]] ||
    die "Deployment command is unavailable: $DEPLOY_COMMAND"

install -d -o root -g root -m 0750 "$STATE_ROOT"
baseline_file="$STATE_ROOT/github-dispatch-enabled-at"
claim_file="$STATE_ROOT/github-dispatch-run"
[[ -s "$baseline_file" ]] ||
    die "GitHub dispatch baseline is missing. Re-run the deployment installer."

baseline="$(tr -d '\r\n' < "$baseline_file")"
[[ "$baseline" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$ ]] ||
    die "GitHub dispatch baseline is invalid: $baseline"

api_url="https://api.github.com/repos/${GITHUB_DEPLOY_REPOSITORY}/actions/workflows/${GITHUB_DEPLOY_WORKFLOW}/runs?event=workflow_dispatch&status=completed&per_page=20"
response="$(
    curl \
        --silent \
        --show-error \
        --fail \
        --retry 2 \
        --connect-timeout 10 \
        --max-time 30 \
        --header 'Accept: application/vnd.github+json' \
        --header 'X-GitHub-Api-Version: 2022-11-28' \
        "$api_url"
)" || die "Unable to query the public GitHub Actions API."

run_record="$(
    jq -r \
        --arg actor "$GITHUB_DEPLOY_ACTOR" \
        --arg branch "$GITHUB_DEPLOY_WORKFLOW_BRANCH" \
        --arg baseline "$baseline" \
        '
          [
            .workflow_runs[]
            | select(
                .event == "workflow_dispatch"
                and .status == "completed"
                and .conclusion == "success"
                and .head_branch == $branch
                and .actor.login == $actor
                and .triggering_actor.login == $actor
                and .created_at > $baseline
              )
          ]
          | sort_by(.id)
          | last
          | if . == null then empty
            else [.id, .head_sha, .html_url, .created_at] | @tsv
            end
        ' <<< "$response"
)" || die "GitHub returned an unexpected workflow-runs response."

[[ -n "$run_record" ]] || exit 0
IFS=$'\t' read -r run_id run_sha run_url run_created_at <<< "$run_record"
[[ "$run_id" =~ ^[0-9]+$ ]] || die "GitHub returned an invalid workflow run ID."
[[ "$run_sha" =~ ^[0-9a-fA-F]{40}$ ]] ||
    die "GitHub returned an invalid workflow commit."
[[ "$run_url" == https://github.com/* ]] ||
    die "GitHub returned an unexpected workflow URL."

claimed_run=0
if [[ -s "$claim_file" ]]; then
    claimed_run="$(tr -d '\r\n' < "$claim_file")"
    [[ "$claimed_run" =~ ^[0-9]+$ ]] ||
        die "Recorded GitHub workflow run ID is invalid."
fi
(( run_id > claimed_run )) || exit 0

# Claim before deployment. A failed deployment therefore requires a fresh,
# deliberate workflow dispatch instead of retrying forever from the timer.
claim_temporary="$STATE_ROOT/.github-dispatch-run.$$"
printf '%s\n' "$run_id" > "$claim_temporary"
chown root:root "$claim_temporary"
chmod 0640 "$claim_temporary"
mv -f "$claim_temporary" "$claim_file"

printf '%s [%s] Accepted GitHub deployment request %s from %s (%s, %s).\n' \
    "$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
    "$DEPLOYMENT_NAME" \
    "$run_id" \
    "$GITHUB_DEPLOY_ACTOR" \
    "$run_created_at" \
    "$run_url"

exec "$DEPLOY_COMMAND" --if-changed
