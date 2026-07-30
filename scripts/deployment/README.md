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
2. Builds the .NET 10 server and updates changed HAKs, the TLK, packed module,
   and .NET output directly under the existing `NWSYNC_ROOT`.
3. Runs `NWSYNC_ROOT/build.sh` from `NWSYNC_ROOT`. This is the operation that
   generates and activates the new NWSync manifest; a build failure never stops
   the game server.
4. Verifies every expected HAK, the TLK, module, .NET output, manifest, and free
   space before server downtime.
5. Ensures supporting Compose services and the server image exist before
   downtime.
6. Gives `swlor-server` 120 seconds to stop, copies the completed .NET output
   to the server tree, and starts only that service. HAK/TLK/module files are
   already visible through the configured bind mounts.
7. Requires `Server: Module loaded` within five minutes and then 30 seconds
   without a restart.
8. Removes temporary build work and prunes only dangling Docker images after a
   successful deployment.

HakBuilder uses the `.md5` files beside the existing HAKs, so unchanged HAKs
are not rebuilt or duplicated. The NWSync `data` store is append-only in this
workflow. It is not automatically pruned because doing so safely depends on
the installed `nwsync_prune` version and the manifests that must remain
available to players. Review its help and disk usage separately before
enabling pruning.

This low-space design does not retain a second 13 GB HAK tree for local
artifact rollback. The prior NWSync manifest remains active until `build.sh`
succeeds, and the game server remains running if any build step fails. After
manifest activation, recovery is a rebuild from the desired Git commit or a
restore from the host backup.

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
STATE_ROOT="$DEPLOYMENT_ROOT/state"
CACHE_ROOT="$VOLUME_ROOT/deployment/cache"

NWSYNC_BUILD_SCRIPT="$NWSYNC_ROOT/build.sh"
NWSYNC_DOTNET_ROOT="$NWSYNC_ROOT/dotnet"
SERVER_DOTNET_ROOT="$SERVER_ROOT/dotnet"
SERVER_CONTENT_MODE=bind
```

For production, make another copy of the example and change
`DEPLOYMENT_NAME`, `VOLUME_ROOT`, `SOURCE_ROOT`, `NWSYNC_ROOT`, `SERVER_ROOT`,
`COMPOSE_FILE`, `GIT_REMOTE`, `BRANCH`, `COMPOSE_PROJECT_NAME`, service/image
names, disk thresholds, and health settings as needed. No changes to
`swlor-deploy.sh` or `install.sh` are required.

Set `SERVER_CONTENT_MODE=bind` when `SERVER_ROOT/{hak,modules,tlk}` are bind
mounts backed by `NWSYNC_ROOT`. Set it to `copy` on a host with separate server
content directories; that mode copies the completed content only after the
server has stopped.

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
| `$STATE_ROOT`, `$CACHE_ROOT` | `root:root` | `750` |
| `$LOG_FILE` | `root:root` | `640` |

The installer preserves an existing `/etc/swlor-deploy.conf` and never enables
the polling timer.

## Manual operation

```bash
# Deploy only if the configured remote branch has a new commit:
/usr/local/sbin/swlor-deploy

# Intentionally rebuild the currently active commit:
/usr/local/sbin/swlor-deploy --force

# Display recorded commits, active manifest, containers, and free space:
/usr/local/sbin/swlor-deploy --status

# Follow the durable deployment log:
source /etc/swlor-deploy.conf
tail -F "$LOG_FILE"
```

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

cat "$STATE_ROOT/active-commit"
cat "$STATE_ROOT/previous-commit"
cat "$NWSYNC_ROOT/latest"
docker compose \
  --project-directory "$SERVER_ROOT" \
  --file "$COMPOSE_FILE" \
  logs --tail 200 swlor-server
```

To recover from a bad application change, revert it on the configured branch
and deploy the resulting commit so all artifacts and the manifest are rebuilt.
For disaster recovery, restore the host backup.
