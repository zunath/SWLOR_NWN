#!/usr/bin/env bash
#
# Fetches the SWLOR_Haks content that scripts/prepare-engine-test-home.sh needs to build the server-only HAKs the
# engine tests boot with, without cloning the 27 GB repository. Used by .github/workflows/engine-tests.yml:
#
#   bash scripts/ci/fetch-engine-test-haks.sh
#
# Run it from a checkout that has no SWLOR_Haks content (a plain checkout leaves the submodule directory empty). It
# replaces ./SWLOR_Haks with a throw-away repository at the commit this checkout pins, and needs git 2.30 or newer,
# python3 and network access to the submodule URL in .gitmodules (SWLOR_HAKS_URL overrides it; on Windows git also
# needs -c http.sslBackend=schannel, see Readmes/AgentEnvironment.md).
#
# What it fetches, and why that is enough:
#   - The module lists all 119 HAKs (Build/hakbuilder.json), so the server needs every one of them. The dedicated
#     server never reads models, textures or sounds, though, and prepare-engine-test-home.sh replaces those file types
#     (stubbed_types there) with zero-byte files. They are 147,000 of the 207,000 files and nearly all of the 11 GB.
#   - So this is a blobless depth-1 fetch of the pinned commit (4 MB of trees) and a sparse non-cone checkout of the
#     HAK folders and the TLK folder with those types excluded: about 59,000 files, 300 MB, 50 MB over the wire.
#   - The excluded files stay in the index as skip-worktree entries. prepare-engine-test-home.sh takes their names from
#     the commit's tree and writes the zero-byte stubs itself, so the staged HAKs hold the same entries as from a full
#     checkout. Do not write files at those paths and do not run a command that needs their content (git checkout of
#     them, git diff against a modified one): the missing blobs would be fetched, 11 GB of them.
#
# Never use the result for anything that reads models, textures or sounds (client work, shipping HAKs).

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/../.."
PREPARE_SCRIPT=scripts/prepare-engine-test-home.sh

sha="$(git rev-parse HEAD:SWLOR_Haks)"
url="${SWLOR_HAKS_URL:-$(git config -f .gitmodules submodule.SWLOR_Haks.url)}"

# The file types prepare-engine-test-home.sh replaces with zero-byte files, read from that script so the two cannot
# drift apart.
stubbed_types="$(sed -n -E 's/^export STUBBED_TYPES="([^"]*)".*$/\1/p' "$PREPARE_SCRIPT")"
if [ -z "$stubbed_types" ]; then
    echo "$PREPARE_SCRIPT no longer exports STUBBED_TYPES; update $0." >&2
    exit 1
fi

# The folders the module needs: every HAK in the builder config, plus the folder of the TLK.
mapfile -t folders < <(python3 - Build/hakbuilder.json <<'LIST_FOLDERS' | tr -d '\r'
import json, os, sys
config = json.load(open(sys.argv[1]))
names = [hak["Name"] for hak in config["HakList"]]
names.append(os.path.basename(os.path.dirname(config["TlkPath"])))
print("\n".join(names))
LIST_FOLDERS
)
if [ "${#folders[@]}" -lt 100 ]; then
    echo "Build/hakbuilder.json lists only ${#folders[@]} folders; refusing to continue." >&2
    exit 1
fi

echo "SWLOR_Haks $sha from $url"
echo "Folders: ${#folders[@]}; left out: $stubbed_types"
rm -rf SWLOR_Haks
git init -q SWLOR_Haks
git -C SWLOR_Haks config core.longpaths true
git -C SWLOR_Haks remote add origin "$url"

# Cone mode cannot exclude a file type, so the patterns are gitignore-style: the folders, then the exclusions, which
# win because the last matching pattern decides. The two root files keep git from fetching them on its own later.
{
    printf '/%s\n' .gitattributes .gitignore
    printf '/%s/\n' "${folders[@]}"
    for type in $stubbed_types; do printf '!*%s\n' "$type"; done
} | git -C SWLOR_Haks sparse-checkout set --no-cone --stdin

start=$SECONDS
git -C SWLOR_Haks fetch -q --depth 1 --filter=blob:none origin "$sha"
git -C SWLOR_Haks -c advice.detachedHead=false checkout -q --detach FETCH_HEAD
test "$(git -C SWLOR_Haks rev-parse HEAD)" = "$sha"
echo "Fetched and checked out in $((SECONDS - start)) s"

test -f SWLOR_Haks/sw_tlk/sw_tlk.tlk || { echo "SWLOR_Haks/sw_tlk/sw_tlk.tlk is missing" >&2; exit 1; }
echo "Checked out $(git -C SWLOR_Haks ls-files -t | grep -c '^H') files; $(git -C SWLOR_Haks ls-files -t | grep -c '^S') left out" \
     "($(du -sm --exclude=.git SWLOR_Haks | cut -f1) MB on disk)"
# prepare-engine-test-home.sh checks that every folder is complete before it builds anything.
