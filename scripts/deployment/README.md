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
2. Compares the last active commit with the requested commit and builds only
   affected artifact groups. Changes to the Haks submodule or HAK builder
   rebuild changed HAKs/TLK; changes under `Module` or to the module packer
   repack the module; server/API changes rebuild .NET. A documentation- or
   deployment-only commit performs none of those builds.
3. Preserves the persistent NWSync HAK/TLK/module workspace. HakBuilder uses
   its persistent `.md5` sidecars to rebuild only HAKs whose source bytes
   changed. It never replaces that workspace from the running server.
   The Haks checkout enforces CRLF for every terminated line in each `.set`
   resource while preserving an optional unterminated final line; the deployer
   materializes and verifies all of them before packaging, then extracts every
   packaged `.set` and byte-compares it with its source.
4. Runs `NWSYNC_ROOT/build.sh` only when HAK/TLK/module content or that build
   script changed. Otherwise it reuses the active manifest. A manifest build
   failure never stops the game server.
5. Removes obsolete HAKs, verifies the exact configured HAK set, the TLK,
   module, .NET output, manifest, and free space before server downtime.
6. If no runtime payload changed, records the commit and leaves the live
   Compose stack untouched. Otherwise it ensures every required Compose image
   exists, then pre-stages and checksum-verifies only the affected server
   artifact groups while the live server remains online.
7. Runs `docker compose down`, updates `NWN_NWSYNCHASH` when needed, and
   atomically moves the affected pre-staged directories into the server tree.
   The NWSync raw directories remain untouched and populated.
8. Requires `Server: Module loaded` within five minutes, rejects any crash
   marker or container restart, and then requires 120 seconds of stability
   after bringing the complete Compose project back up.
9. Retains the previous live artifacts and `swlor.env` in the deployment cache
   until that health check passes. A failed cutover automatically restores
   them and starts the prior stack. A successful cutover removes that rollback
   set and prunes only dangling Docker images.

The NWSync and server HAK/TLK/module paths must be separate ordinary
directories, not symlinks or bind mounts. The deployer refuses to proceed
while a server artifact path is still mounted or resolves to the same object
as its NWSync counterpart.

HakBuilder uses the persistent `.hak`/`.md5` pairs under `NWSYNC_ROOT`, so
unchanged HAKs do not need to be rebuilt. The `.md5` sidecars are build-cache
metadata and are never copied to or expected in the live server directory.
Module and NWSync reuse is determined from the active Git commit recorded under
`STATE_ROOT`, not from timestamps. `--force` intentionally invalidates the HAK
checksum sidecars and rebuilds every artifact group.
NWSync retains the completed raw HAK/TLK/module set needed to build and serve
its manifests. The live `nwn-server` set remains independent. Immediately
before downtime, the deployer creates one additional temporary,
checksum-verified server set; successful cutover removes the old live and
temporary rollback sets after the stability check.

The NWSync `data` store is append-only in this workflow. It is not
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
GITHUB_DEPLOY_REPOSITORY=zunath/SWLOR_NWN
GITHUB_DEPLOY_WORKFLOW=request-test-deployment.yml
GITHUB_DEPLOY_WORKFLOW_BRANCH=master
GITHUB_DEPLOY_ACTOR=zunath
NWSYNC_ROOT="$VOLUME_ROOT/nwsync"
SERVER_ROOT="$VOLUME_ROOT/nwn-server"
COMPOSE_FILE="$SERVER_ROOT/docker-compose.yml"

DEPLOYMENT_ROOT="$VOLUME_ROOT/deployment/$DEPLOYMENT_NAME"
STATE_ROOT="$DEPLOYMENT_ROOT/state"
CACHE_ROOT="$VOLUME_ROOT/deployment/cache"

NWSYNC_BUILD_SCRIPT="$NWSYNC_ROOT/build.sh"
NWSYNC_HAK_ROOT="$NWSYNC_ROOT/hak"
NWSYNC_TLK_ROOT="$NWSYNC_ROOT/tlk"
NWSYNC_MODULE_ROOT="$NWSYNC_ROOT/modules"
SERVER_HAK_ROOT="$SERVER_ROOT/hak"
SERVER_TLK_ROOT="$SERVER_ROOT/tlk"
SERVER_MODULE_ROOT="$SERVER_ROOT/modules"
SERVER_DOTNET_ROOT="$SERVER_ROOT/dotnet"
SERVER_ENV_FILE="$SERVER_ROOT/swlor.env"
NWSYNC_HASH_VARIABLE=NWN_NWSYNCHASH
COMPOSE_PROJECT_NAME=nwnserver
```

For production, make another copy of the example and change
`DEPLOYMENT_NAME`, `VOLUME_ROOT`, `SOURCE_ROOT`, `NWSYNC_ROOT`, `SERVER_ROOT`,
`COMPOSE_FILE`, `GIT_REMOTE`, `BRANCH`, `COMPOSE_PROJECT_NAME`, service/image
names, GitHub workflow/branch, disk thresholds, and health settings as needed.
Use a separate production request workflow so a test deployment request cannot
also signal production. No changes to `swlor-deploy.sh` or `install.sh` are
required.

`CACHE_ROOT`, all three NWSync artifact directories, all four server artifact
directories, and their parent paths must be on the same filesystem. This makes
the cutover and rollback directory moves atomic. The independent server copy
is completed and checksum-verified before downtime.

Keep `COMPOSE_PROJECT_NAME` equal to the host's established Compose project
name. Changing it selects different project-scoped containers, networks, and
named volumes. The test host uses `nwnserver`.

## One-time unlink migration

The old host layout bind-mounted `hak`, `tlk`, and `modules` from NWSync into
the server. Perform this migration once before installing this version of the
deployer. Run it in `screen`, and do not use a forced or lazy unmount.

First verify the exact paths and take the full game Compose project down:

```bash
VOLUME_ROOT=/mnt/swlor-web-vol
NWSYNC_ROOT="$VOLUME_ROOT/nwsync"
SERVER_ROOT="$VOLUME_ROOT/nwn-server"
COMPOSE_FILE="$SERVER_ROOT/docker-compose.yml"

for artifact_name in hak tlk modules; do
  server_path="$SERVER_ROOT/$artifact_name"
  printf '%s -> target=%s source=%s\n' \
    "$server_path" \
    "$(findmnt -T "$server_path" -n -o TARGET)" \
    "$(findmnt -T "$server_path" -n -o SOURCE)"
done

docker compose \
  --project-directory "$SERVER_ROOT" \
  --file "$COMPOSE_FILE" \
  down --timeout 120
```

Each reported target must exactly match its server path before the following
unmount. Stop and investigate instead of using `umount -f` or `umount -l` if
any unmount reports that the target is busy.

```bash
MIGRATION_BACKUP="$VOLUME_ROOT/upgrade-backups/swlor-content-unlink-$(date -u +%Y%m%dT%H%M%SZ)"
install -d -o root -g root -m 0700 "$MIGRATION_BACKUP"
install -o root -g root -m 0600 /etc/fstab "$MIGRATION_BACKUP/fstab"

for artifact_name in hak tlk modules; do
  server_path="$SERVER_ROOT/$artifact_name"
  test "$(findmnt -T "$server_path" -n -o TARGET)" = "$server_path"
  umount -- "$server_path"
done

FSTAB_TEMP=/etc/fstab.swlor-unlink
awk \
  -v nwsync="$NWSYNC_ROOT" \
  -v server="$SERVER_ROOT" \
  '
    !($1 == nwsync "/hak"     && $2 == server "/hak") &&
    !($1 == nwsync "/tlk"     && $2 == server "/tlk") &&
    !($1 == nwsync "/modules" && $2 == server "/modules")
  ' \
  /etc/fstab > "$FSTAB_TEMP"
chown --reference=/etc/fstab "$FSTAB_TEMP"
chmod --reference=/etc/fstab "$FSTAB_TEMP"
mv -f "$FSTAB_TEMP" /etc/fstab
systemctl daemon-reload
```

The unmount reveals the old underlying server directories. Replace their
contents with the current known-good NWSync set, verify an exact match, and
bring the unchanged server back up:

```bash
for artifact_name in hak tlk modules; do
  install -d -o root -g root -m 0755 "$SERVER_ROOT/$artifact_name"
  rsync --archive --delete \
    "$NWSYNC_ROOT/$artifact_name/" \
    "$SERVER_ROOT/$artifact_name/"
  test -z "$(
    rsync --archive --delete --dry-run --itemize-changes \
      "$NWSYNC_ROOT/$artifact_name/" \
      "$SERVER_ROOT/$artifact_name/"
  )"
done

for artifact_name in hak tlk modules; do
  nwsync_path="$NWSYNC_ROOT/$artifact_name"
  server_path="$SERVER_ROOT/$artifact_name"
  test "$(stat -Lc '%d:%i' "$nwsync_path")" != \
       "$(stat -Lc '%d:%i' "$server_path")"
  test "$(findmnt -T "$server_path" -n -o TARGET)" != "$server_path"
done

docker compose \
  --project-directory "$SERVER_ROOT" \
  --file "$COMPOSE_FILE" \
  up -d
```

At this point both raw artifact sets intentionally exist. Every deployment
updates the persistent NWSync set, generates its manifest, and copies that
completed set into a temporary cutover directory. Only the temporary cutover
directory is moved into the server tree; NWSync remains populated.

## Install

Install using that customized configuration:

```bash
BOOTSTRAP_ROOT=/root/swlor-deployment-bootstrap
HOST_CONFIG="$BOOTSTRAP_ROOT/swlor-test.conf"

# On an existing installation, add/review the four GITHUB_DEPLOY_* settings in
# /etc/swlor-deploy.conf first; the installer preserves that file.
bash "$BOOTSTRAP_ROOT/install.sh" "$HOST_CONFIG"

# Load path variables for the verification commands below.
source /etc/swlor-deploy.conf

stat -c '%n owner=%U:%G mode=%a' \
  "$SOURCE_ROOT" \
  /usr/local/sbin/swlor-deploy \
  /usr/local/sbin/swlor-deploy-dispatch \
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
| `/usr/local/sbin/swlor-deploy-dispatch` | `root:root` | `750` |
| `/etc/swlor-deploy.conf` | `root:root` | `640` |
| systemd unit files | `root:root` | `644` |
| `$STATE_ROOT`, `$CACHE_ROOT` | `root:root` | `750` |
| `$LOG_FILE` | `root:root` | `640` |

The installer preserves an existing `/etc/swlor-deploy.conf`, creates a
timestamp baseline that prevents old workflow runs from being replayed, and
never enables the polling timer.

## Manual operation

```bash
# Deploy only if the configured remote branch has a new commit:
/usr/local/sbin/swlor-deploy

# Intentionally rebuild the currently active commit:
/usr/local/sbin/swlor-deploy --force

# Display recorded commits, NWSync/server manifest hashes, path separation,
# containers, and free space:
/usr/local/sbin/swlor-deploy --status

# Follow the durable deployment log:
source /etc/swlor-deploy.conf
tail -F "$LOG_FILE"
```

## Owner-only GitHub deployment requests

The workflow
`.github/workflows/request-test-deployment.yml` is a credential-free signal.
It does not connect to the server and receives no SSH key, webhook secret, or
deployment token. It succeeds only when both GitHub's original actor and
triggering actor are exactly `zunath`.

The server makes an outbound, unauthenticated request to GitHub's public
workflow-runs API every three minutes. It accepts only a new, successful
`workflow_dispatch` run that matches all four configured values:

```bash
GITHUB_DEPLOY_REPOSITORY=zunath/SWLOR_NWN
GITHUB_DEPLOY_WORKFLOW=request-test-deployment.yml
GITHUB_DEPLOY_WORKFLOW_BRANCH=master
GITHUB_DEPLOY_ACTOR=zunath
```

The server claims a workflow run before deploying. If deployment fails, the
timer will not retry that request indefinitely; fix the problem and manually
dispatch a new run.

GitHub exposes a workflow's **Run workflow** button only after the workflow file
exists on the repository's default branch. Merge this file to `master` before
trying to use the button. The deployment source may still track a different
branch, such as `feature/combat-upgrade`; the host's `BRANCH` setting controls
what is deployed.

After installing the current scripts and configuration, enable the disabled
timer:

```bash
# Enable outbound GitHub request polling:
systemctl enable --now swlor-deploy.timer

# Inspect it:
systemctl list-timers swlor-deploy.timer --no-pager
journalctl -u swlor-deploy.service -n 100 --no-pager

# Return to SSH/manual-only deployment:
systemctl disable --now swlor-deploy.timer
```

To deploy from GitHub, open **Actions**, select **Request test-server
deployment**, choose **Run workflow**, leave the workflow ref on `master`, and
confirm. A successful request is normally consumed within three minutes.
Follow progress on the host with:

```bash
source /etc/swlor-deploy.conf
tail -F "$LOG_FILE"
journalctl -fu swlor-deploy.service
```

The same deployment lock protects SSH/manual and GitHub-requested runs from
overlapping. An accepted request runs `swlor-deploy --if-changed`, so requesting
an already-active commit is a safe no-op.

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
grep "^${NWSYNC_HASH_VARIABLE}=" "$SERVER_ENV_FILE"
docker compose \
  --project-directory "$SERVER_ROOT" \
  --file "$COMPOSE_FILE" \
  logs --tail 200 swlor-server
```

Cutover failures are rolled back automatically. If rollback itself fails, the
deployer preserves its exact recovery directory under `CACHE_ROOT` and logs
that path instead of deleting the old artifact set. Do not manually delete
that directory.

To recover from a bad change that passed startup health, revert it on the
configured branch and deploy the resulting commit so all artifacts and the
manifest are rebuilt. For disaster recovery, restore the host backup.
