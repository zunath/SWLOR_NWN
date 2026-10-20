#!/bin/bash
# Checks out SWLOR_Haks in a Claude Code cloud session (Linux). Registered as an async hook
# because the full checkout takes about 10 minutes: the session starts while it runs.
# Progress goes to /tmp/swlor-haks-checkout.log, and /tmp/swlor-haks-checkout.done appears
# once the checkout is usable. Does nothing on local machines. Safe to run more than once,
# and resumes a checkout that an earlier run did not finish.
#
# The full checkout is about 11 GB of files, and the disk is limited. Clone without blobs,
# check out, then delete the blobs the checkout fetched (about 5 GB). The clone is partial,
# so git downloads any blob it needs again later (diff, checkout of another commit).
#
# Async hooks ignore their "timeout" setting, so .claude/settings.json runs this under coreutils
# timeout; a stopped run resumes on the next session start.
#
# Set SWLOR_HAKS_SPARSE to a list of hak folders to check out only those (faster, but tests
# that read other folders fail).
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

REPO="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
HAKS="$REPO/SWLOR_Haks"
HAKS_SPARSE="${SWLOR_HAKS_SPARSE:-}"
LOG=/tmp/swlor-haks-checkout.log
DONE=/tmp/swlor-haks-checkout.done

exec 9>/tmp/swlor-haks-checkout.lock
if ! flock -n 9; then
  echo "[session-start-haks] Another run is already checking out SWLOR_Haks" >&2
  exit 0
fi
rm -f "$DONE"
exec >>"$LOG" 2>&1
log() { echo "[$(date -u +%H:%M:%S)] $*"; }
trap 'log "Stopped before finishing; the next session start resumes"; exit 143' TERM INT HUP

# Deletes the promisor packs in <pack dir> that <snapshot> does not list: the blobs a checkout
# fetched. Packs from the clone and from commit fetches are in the snapshot and stay.
drop_fetched_blobs() {
  local pack
  for pack in "$1"/*.pack; do
    if ! grep -qxF "$pack" "$2" && [ -f "${pack%.pack}.promisor" ]; then
      rm -f "${pack%.pack}".{pack,idx,rev,promisor}
    fi
  done
}

sha=$(git -C "$REPO" rev-parse HEAD:SWLOR_Haks)
module_gitdir=$(git -C "$REPO" rev-parse --path-format=absolute --git-path modules/SWLOR_Haks)
if [ -d "$module_gitdir" ] && [ ! -e "$HAKS/.git" ]; then
  # The repository survived but its worktree did not (git submodule deinit, for example). Without
  # a .git link, git -C "$HAKS" would act on the parent repository, so restore the link first.
  if [ -n "$(ls -A "$HAKS" 2>/dev/null)" ]; then
    log "SWLOR_Haks holds files but no repository link; leaving it alone"
    exit 1
  fi
  log "Restoring the SWLOR_Haks worktree from $module_gitdir"
  # Fetch the pinned commit first, so its commit and tree packs are in the snapshot and kept.
  git --git-dir="$module_gitdir" cat-file -e "$sha^{commit}" 2>/dev/null \
    || git --git-dir="$module_gitdir" fetch -q --depth 1 --filter=blob:none origin "$sha"
  packs="$module_gitdir/objects/pack"
  snapshot=$(mktemp)
  ls "$packs"/*.pack > "$snapshot"
  git -C "$REPO" submodule --quiet update --init SWLOR_Haks
  drop_fetched_blobs "$packs" "$snapshot"
  rm -f "$snapshot"
fi
if [ -d "$module_gitdir" ]; then
  current=$(git -C "$HAKS" rev-parse HEAD 2>/dev/null || true)
  sparse=$(git -C "$HAKS" config --bool core.sparseCheckout || echo false)
  if [ "$current" = "$sha" ] && [ "$sparse" = false ] && [ -z "$HAKS_SPARSE" ]; then
    log "SWLOR_Haks is already at the pinned commit"
  elif git -C "$HAKS" symbolic-ref -q HEAD >/dev/null || [ -n "$(git -C "$HAKS" status --porcelain)" ]; then
    # Someone is working in it; never move their branch, local changes, or untracked files.
    log "SWLOR_Haks is on a branch or has local changes; leaving it as it is (pinned: $sha)"
  else
    git -C "$HAKS" cat-file -e "$sha^{commit}" 2>/dev/null \
      || git -C "$HAKS" fetch -q --depth 1 --filter=blob:none origin "$sha"
    packs=$(git -C "$HAKS" rev-parse --path-format=absolute --git-path objects/pack)
    snapshot=$(mktemp)
    ls "$packs"/*.pack > "$snapshot"
    # Match SWLOR_HAKS_SPARSE as it is now, then the pinned commit.
    if [ -n "$HAKS_SPARSE" ]; then
      # shellcheck disable=SC2086
      git -C "$HAKS" sparse-checkout set --cone $HAKS_SPARSE
    elif [ "$sparse" = true ]; then
      log "SWLOR_HAKS_SPARSE is unset; checking out every folder"
      git -C "$HAKS" sparse-checkout disable
    fi
    if [ "$current" != "$sha" ]; then
      log "Moving SWLOR_Haks from $current to the pinned $sha"
      git -C "$HAKS" -c advice.detachedHead=false checkout -q --detach "$sha"
    fi
    drop_fetched_blobs "$packs" "$snapshot"
    rm -f "$snapshot"
  fi
else
  if [ ! -d "$HAKS/.git" ]; then
    if [ -n "$(ls -A "$HAKS" 2>/dev/null)" ]; then
      log "SWLOR_Haks holds files but no repository; leaving it alone"
      exit 1
    fi
    url=$(git -C "$REPO" config -f .gitmodules submodule.SWLOR_Haks.url)
    log "Cloning $url without blobs"
    git clone -q --filter=blob:none --no-checkout --depth 1 "$url" "$HAKS"
    git -C "$HAKS" cat-file -e "$sha^{commit}" 2>/dev/null \
      || git -C "$HAKS" fetch -q --depth 1 --filter=blob:none origin "$sha"
    # Packs holding commits and trees; everything fetched after this is blobs.
    ls "$HAKS"/.git/objects/pack/*.pack > "$HAKS/.git/swlor-clone-packs"
  fi
  if [ -n "$HAKS_SPARSE" ]; then
    # shellcheck disable=SC2086
    git -C "$HAKS" sparse-checkout set --cone $HAKS_SPARSE
  fi
  log "Checking out $sha${HAKS_SPARSE:+ (sparse: $HAKS_SPARSE)}"
  git -C "$HAKS" -c advice.detachedHead=false checkout -q -f --detach "$sha"
  clone_packs="$HAKS/.git/swlor-clone-packs"
  if [ -f "$clone_packs" ]; then
    drop_fetched_blobs "$HAKS/.git/objects/pack" "$clone_packs"
    rm -f "$clone_packs"
  fi
  git -C "$REPO" submodule --quiet init SWLOR_Haks
  git -C "$REPO" submodule --quiet absorbgitdirs SWLOR_Haks
  log "Checkout complete"
fi

# Git stores the .exe tools without the executable bit. Set it, and stop git reporting it. A sparse
# checkout lists tools outside its folders too, so only the ones on disk are changed.
git -C "$HAKS" config core.fileMode false
(cd "$HAKS" && git ls-files -z '*.exe' | while IFS= read -r -d '' tool; do
  [ ! -f "$tool" ] || chmod +x "$tool"
done)

if [ -f "$HAKS/requirements.txt" ]; then
  # The lock keeps this and session-start.sh from changing the shared site-packages at once.
  PIP_BREAK_SYSTEM_PACKAGES=1 flock /tmp/swlor-pip.lock python3 -m pip install -q \
    --disable-pip-version-check --root-user-action=ignore -r "$HAKS/requirements.txt"
fi

# The pinned model compiler, installed the way tools/SetupModelCompiler.ps1 does (same download
# and checksums, read from that script) but with the executable bit Wine's binfmt handler needs.
# The script is read from the commit, so a sparse checkout without tools/ still gets the compiler.
# tools/CleanModels.py looks in ~/.local/share when LOCALAPPDATA is unset.
setup=$(git -C "$HAKS" show HEAD:tools/SetupModelCompiler.ps1 2>/dev/null | tr -d '\r' || true)
url=$(sed -n "s/^\$url = '\(.*\)'\$/\1/p" <<<"$setup")
mapfile -t hashes < <(grep -oE "\-ne '[0-9A-Fa-f]{64}'" <<<"$setup" | grep -oE '[0-9A-Fa-f]{64}')
version=$(grep -oE '/v[0-9.]+/' <<<"$url" | tr -d '/v' || true)
dest="$HOME/.local/share/SWLOR/tools/cleanmodels-qt/$version"
compiler="$dest/cleanmodels-qt/cleanmodels.exe"
if [ -z "$url" ] || [ -z "$version" ] || [ "${#hashes[@]}" -ne 2 ]; then
  log "Could not read the CleanModels URL and checksums from tools/SetupModelCompiler.ps1; SWLOR_Haks is not ready"
  exit 1
fi
if [ ! -x "$compiler" ]; then
  log "Installing CleanModels from $url"
  mkdir -p "$dest"
  archive="$dest/$(basename "$url")"
  for delay in 0 15 60; do
    sleep "$delay"
    if curl -fsSL --max-time 300 -o "$archive" "$url" &&
       echo "${hashes[0]}  $archive" | sha256sum -c --quiet - &&
       (cd "$dest" && unzip -oq "$archive") &&
       echo "${hashes[1]}  $compiler" | sha256sum -c --quiet -; then
      chmod +x "$compiler"
      break
    fi
    log "CleanModels download or checksum failed"
  done
  if [ ! -x "$compiler" ]; then
    # No ready marker: the model compiler tests need it, and the next session start retries.
    log "CleanModels is not installed; SWLOR_Haks is checked out, but not ready"
    exit 1
  fi
fi

touch "$DONE"
log "SWLOR_Haks is ready"
