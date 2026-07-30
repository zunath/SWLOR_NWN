# SWLOR production deployment

This directory contains the manual-first deployment system. The checked-in
example currently describes the test host:

- Git source: `https://github.com/zunath/SWLOR_NWN`
- Current branch: `origin/feature/combat-upgrade`
- Source checkout: `/mnt/swlor-web-vol/deployment-source`
- NWSync repository: `/mnt/swlor-web-vol/nwsync`
- NWN Compose project: `/mnt/swlor-web-vol/nwn-server`

The executable contains no environment-specific deployment paths. Test and
production each receive their own root-owned `/etc/swlor-deploy.conf`. Change
that host configuration when its paths, branch, Compose layout, thresholds, or
health marker differ.

## Safety model

The deployment:

1. Takes an exclusive `flock`, refuses dirty/diverged source, and performs only
   a fast-forward from the configured GitHub branch.
2. Builds the .NET 10 server, changed HAKs, TLK, packed module, and NWSync
   manifest in a versioned staging release while the live server stays online.
3. Verifies expected artifacts and free space before touching live files.
4. Pre-publishes immutable NWSync content while leaving the old `latest`
   manifest active.
5. Ensures supporting Compose services and the server image exist before
   downtime.
6. Gives `swlor-server` 120 seconds to stop, switches the complete release, and
   starts only that service.
7. Requires `Server: Module loaded` within five minutes and then 30 seconds
   without a restart.
8. Automatically restores the prior files and NWSync pointer if cutover or
   health validation fails.
9. Retains only the active and previous release. Hard links avoid duplicate
   storage for unchanged files. Only dangling Docker images are pruned.

The live NWSync `data` store is append-only in this workflow. It is not
automatically pruned because doing so safely depends on the installed
`nwsync_prune` version and the manifests that must remain available to
players. Review its help and disk usage separately before enabling pruning.

## Initial source checkout

Run as root. If the directory already contains the correct checkout, do not
clone over it.

```bash
VOLUME_ROOT=/mnt/swlor-web-vol
SOURCE_ROOT="$VOLUME_ROOT/deployment-source"

install -d -o root -g root -m 0750 "$SOURCE_ROOT"
git clone \
  --branch feature/combat-upgrade \
  --single-branch \
  --recurse-submodules \
  --shallow-submodules \
  https://github.com/zunath/SWLOR_NWN.git \
  "$SOURCE_ROOT"

chown -R root:root "$SOURCE_ROOT"
chmod 0750 "$SOURCE_ROOT"
```

## Per-host configuration

Create a host-specific copy before running the installer. For the test host:

```bash
BOOTSTRAP_ROOT=/root/swlor-deployment-bootstrap
HOST_CONFIG="$BOOTSTRAP_ROOT/swlor-test.conf"

cp "$BOOTSTRAP_ROOT/swlor-deploy.conf.example" "$HOST_CONFIG"
chown root:root "$HOST_CONFIG"
chmod 0600 "$HOST_CONFIG"

# Review every value before installation.
nano "$HOST_CONFIG"
```

The relationships between paths are defined once:

```bash
DEPLOYMENT_NAME=swlor-test
VOLUME_ROOT=/mnt/swlor-web-vol

SOURCE_ROOT="$VOLUME_ROOT/deployment-source"
NWSYNC_ROOT="$VOLUME_ROOT/nwsync"
SERVER_ROOT="$VOLUME_ROOT/nwn-server"
COMPOSE_FILE="$SERVER_ROOT/docker-compose.yml"

DEPLOYMENT_ROOT="$VOLUME_ROOT/deployment/$DEPLOYMENT_NAME"
RELEASE_ROOT="$DEPLOYMENT_ROOT/releases"
STATE_ROOT="$DEPLOYMENT_ROOT/state"
CACHE_ROOT="$VOLUME_ROOT/deployment/cache"
```

For production, make another copy of the example and change
`DEPLOYMENT_NAME`, `VOLUME_ROOT`, `SOURCE_ROOT`, `NWSYNC_ROOT`, `SERVER_ROOT`,
`COMPOSE_FILE`, `GIT_REMOTE`, `BRANCH`, `COMPOSE_PROJECT_NAME`, service/image
names, disk thresholds, and health settings as needed. No changes to
`swlor-deploy.sh` or `install.sh` are required.

## Install

Install using that customized configuration:

```bash
BOOTSTRAP_ROOT=/root/swlor-deployment-bootstrap
HOST_CONFIG="$BOOTSTRAP_ROOT/swlor-test.conf"

bash "$BOOTSTRAP_ROOT/install.sh" "$HOST_CONFIG"

# Load path variables for the verification commands below.
source /etc/swlor-deploy.conf

stat -c '%n owner=%U:%G mode=%a' \
  "$SOURCE_ROOT" \
  /usr/local/sbin/swlor-deploy \
  /etc/swlor-deploy.conf \
  /etc/systemd/system/swlor-deploy.service \
  /etc/systemd/system/swlor-deploy.timer

systemctl is-enabled swlor-deploy.timer
```

Expected modes are:

| Path | Owner | Mode |
|---|---|---:|
| `$SOURCE_ROOT` | `root:root` | `750` |
| `/usr/local/sbin/swlor-deploy` | `root:root` | `750` |
| `/etc/swlor-deploy.conf` | `root:root` | `640` |
| systemd unit files | `root:root` | `644` |
| `$RELEASE_ROOT`, `$STATE_ROOT`, `$CACHE_ROOT` | `root:root` | `750` |
| `$LOG_FILE` | `root:root` | `640` |

The installer preserves an existing `/etc/swlor-deploy.conf` and never enables
the polling timer.

## Manual operation

```bash
# Deploy only if the configured remote branch has a new commit:
/usr/local/sbin/swlor-deploy

# Intentionally rebuild the currently active commit:
/usr/local/sbin/swlor-deploy --force

# Display release pointers, containers, and free space:
/usr/local/sbin/swlor-deploy --status

# Switch to the retained prior release:
/usr/local/sbin/swlor-deploy --rollback

# Follow the durable deployment log:
source /etc/swlor-deploy.conf
tail -F "$LOG_FILE"
```

The first deployment captures the pre-automation live files as a hard-linked
baseline. This makes automatic rollback available during the first cutover.

## Optional polling

The installed timer is disabled by default.

```bash
# Enable five-minute polling:
systemctl enable --now swlor-deploy.timer

# Inspect it:
systemctl list-timers swlor-deploy.timer --no-pager
journalctl -u swlor-deploy.service -n 100 --no-pager

# Return to manual-only deployment:
systemctl disable --now swlor-deploy.timer
```

The same deployment lock protects manual and timer-triggered runs from
overlapping.

## Branch switch after release

Update the dedicated checkout and configuration together:

```bash
source /etc/swlor-deploy.conf
NEW_BRANCH=main

git -C "$SOURCE_ROOT" fetch \
  "$GIT_REMOTE" "$NEW_BRANCH:refs/remotes/$GIT_REMOTE/$NEW_BRANCH"
git -C "$SOURCE_ROOT" switch \
  --create "$NEW_BRANCH" \
  --track "$GIT_REMOTE/$NEW_BRANCH"

sed -i "s#^BRANCH=.*#BRANCH=$NEW_BRANCH#" /etc/swlor-deploy.conf
```

Replace `main` with the actual release branch. The deploy command will refuse
to run if the checked-out branch and configured branch disagree.

## Recovery information

```bash
source /etc/swlor-deploy.conf

readlink -f "$STATE_ROOT/current"
readlink -f "$STATE_ROOT/previous"
docker compose \
  --project-directory "$SERVER_ROOT" \
  --file "$COMPOSE_FILE" \
  logs --tail 200 swlor-server
```
