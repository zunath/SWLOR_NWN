#!/usr/bin/env bash
#
# Prepares a server home for scripts/run-engine-tests.sh on Linux, where the Windows deploy
# flow (Visual Studio post-build deploy, BuildHaks.cmd, PackModule.cmd) is unavailable - for
# example in Claude Code cloud sessions. It stages:
#   - swlor.env and settings.tml from SWLOR.Game.Server/Docker (kept if already present)
#   - server-only HAKs and the TLK, built by SWLOR.CLI --hak
#   - the module, packed by SWLOR.CLI --pack
#   - the Docker images docker-compose.enginetests.yml uses
#
# Server-only HAKs: the dedicated server never reads models, textures or sounds. Beamdog's
# own server data ships every .mdl, .tga, .dds and .plt as a zero-byte entry and leaves the
# sounds out. These HAKs keep every other resource and replace those types (plus .wav and
# .bmu) with zero-byte files, so about 11 GB of HAKs becomes about 0.4 GB. Never give them
# to players or deploy them to a real server.
#
# The home defaults to <repo>/enginetests-home, which run-engine-tests.sh uses automatically.
# Steps whose inputs have not changed since the last run are skipped; --force redoes them.
#
# Requires: dotnet SDK, docker compose, python3, the full SWLOR_Haks checkout, and the native
# NWN tools that Setup.sh (or .claude/hooks/session-start.sh in cloud sessions) prepares.
#
# Usage:
#   scripts/prepare-engine-test-home.sh [--server-home <dir>] [--force] [--skip-images]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SERVER_HOME="${SWLOR_ENGINE_TEST_SERVER_HOME:-$REPO_ROOT/enginetests-home}"
FORCE=0
SKIP_IMAGES=0
# Bump when the staging rules change so existing homes rebuild.
STAMP_VERSION=2

while [ $# -gt 0 ]; do
    case "$1" in
        --server-home) SERVER_HOME="$2"; shift 2 ;;
        --force) FORCE=1; shift ;;
        --skip-images) SKIP_IMAGES=1; shift ;;
        *) echo "Unknown argument: $1" >&2; exit 2 ;;
    esac
done

DOCKER_DIR="$REPO_ROOT/SWLOR.Game.Server/Docker"
COMPOSE_FILE="$DOCKER_DIR/docker-compose.enginetests.yml"
HAKS="$REPO_ROOT/SWLOR_Haks"
CLI_DLL="$REPO_ROOT/SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll"
STAMPS="$SERVER_HOME/.prepare-stamps"

section() { echo ""; echo "=== $1 ==="; }

# Prints a fingerprint of a git checkout's content at <path>...: HEAD's objects plus any local
# edits and untracked files, so a rebuild happens whenever the inputs differ.
fingerprint() {
    local repo="$1"
    shift
    {
        echo "$STAMP_VERSION"
        git -C "$repo" ls-tree HEAD -- "$@"
        git -C "$repo" diff HEAD --binary -- "$@"
        git -C "$repo" ls-files --others --exclude-standard -z -- "$@" \
            | (cd "$repo" && xargs -0 -r sha256sum)
    } | sha256sum | cut -d' ' -f1
}

# The code and configuration that turn sources into HAKs and the module.
BUILDER_INPUTS=(SWLOR.CLI SWLOR.NWN.Formats tools/native-tools.json)

is_current() { [ "$FORCE" -eq 0 ] && [ -f "$STAMPS/$1" ] && [ "$(cat "$STAMPS/$1")" = "$2" ]; }

# Succeeds when every configured HAK and the TLK are in the home.
haks_complete() {
    python3 - "$REPO_ROOT" "$SERVER_HOME" <<'HAKS_COMPLETE'
import json, os, sys
repo, home = sys.argv[1:]
config = json.load(open(os.path.join(repo, "Build", "hakbuilder.json")))
paths = [os.path.join(home, "hak", hak["Name"] + ".hak") for hak in config["HakList"]]
paths.append(os.path.join(home, "tlk", os.path.basename(config["TlkPath"])))
sys.exit(0 if all(os.path.isfile(path) for path in paths) else 1)
HAKS_COMPLETE
}

mkdir -p "$SERVER_HOME" "$STAMPS"
SERVER_HOME="$(cd "$SERVER_HOME" && pwd)"

section "Server configuration ($SERVER_HOME)"
for file in swlor.env settings.tml; do
    if [ -f "$SERVER_HOME/$file" ]; then
        echo "Keeping existing $file"
    else
        cp "$DOCKER_DIR/$file" "$SERVER_HOME/$file"
        echo "Copied $file"
    fi
done
# The server loads the module that the retained swlor.env names.
MODULE_NAME="$(sed -n -E 's/^NWN_MODULE="?([^"]*)"?[[:space:]]*$/\1/p' "$SERVER_HOME/swlor.env" | tr -d '\r')"
[ -n "$MODULE_NAME" ] || { echo "No NWN_MODULE in $SERVER_HOME/swlor.env." >&2; exit 1; }

section "Building SWLOR.CLI (Release)"
dotnet build "$REPO_ROOT/SWLOR.CLI/SWLOR.CLI.csproj" -c Release -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly

section "Server-only HAKs and TLK"
python3 - "$REPO_ROOT" <<'CHECK_HAKS'
import json, os, sys
repo = sys.argv[1]
config = json.load(open(os.path.join(repo, "Build", "hakbuilder.json")))
missing = [hak["Name"] for hak in config["HakList"]
           if not os.path.isdir(os.path.join(repo, "Build", hak["Path"]))
           or not os.listdir(os.path.join(repo, "Build", hak["Path"]))]
if missing:
    sys.exit(f"SWLOR_Haks is missing HAK folders ({', '.join(missing[:5])}...). "
             "Check out the full submodule: git -C SWLOR_Haks sparse-checkout disable")
CHECK_HAKS
haks_stamp="$(fingerprint "$HAKS" .)-$(fingerprint "$REPO_ROOT" Build/hakbuilder.json "${BUILDER_INPUTS[@]}")"
if is_current haks "$haks_stamp" && haks_complete; then
    echo "HAK sources unchanged since the last build; skipping"
else
    # A build that fails part-way must not leave the old stamp vouching for its output.
    rm -f "$STAMPS/haks"
    work="$SERVER_HOME/.hak-build"
    rm -rf "$work"
    mkdir -p "$work"
    python3 - "$REPO_ROOT" "$work" "$SERVER_HOME" <<'STAGE_HAKS'
import json, os, shutil, sys
repo, work, home = sys.argv[1:]
# Types the dedicated server never reads; Beamdog's server data stubs the first four.
stubbed_types = {".mdl", ".tga", ".dds", ".plt", ".wav", ".bmu"}
config = json.load(open(os.path.join(repo, "Build", "hakbuilder.json")))
haks, kept, stubbed = [], 0, 0
for hak in config["HakList"]:
    source = os.path.normpath(os.path.join(repo, "Build", hak["Path"]))
    target = os.path.join(work, hak["Name"])
    for root, _, files in os.walk(source):
        out = os.path.join(target, os.path.relpath(root, source))
        os.makedirs(out, exist_ok=True)
        for name in files:
            if os.path.splitext(name)[1].lower() in stubbed_types:
                open(os.path.join(out, name), "wb").close()
                stubbed += 1
            else:
                shutil.copyfile(os.path.join(root, name), os.path.join(out, name))
                kept += 1
    haks.append({"Name": hak["Name"], "Path": target + "/", "CompileModels": False})
with open(os.path.join(work, "hakbuilder.json"), "w") as f:
    json.dump({
        "TlkPath": os.path.join(repo, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk"),
        "OutputPath": home.rstrip("/") + "/",
        "EnableChecksumChecking": False,
        "HakList": haks,
    }, f, indent=2)
print(f"Staged {len(haks)} HAKs: {kept} files kept, {stubbed} replaced with zero-byte files")
STAGE_HAKS
    (cd "$work" && dotnet "$CLI_DLL" --hak)
    rm -rf "$work"
    echo "$haks_stamp" > "$STAMPS/haks"
fi
du -sh "$SERVER_HOME/hak" "$SERVER_HOME/tlk"

section "Module ($MODULE_NAME.mod)"
module_stamp="$(fingerprint "$REPO_ROOT" Module "${BUILDER_INPUTS[@]}")"
if is_current module "$module_stamp" && [ -f "$SERVER_HOME/modules/$MODULE_NAME.mod" ]; then
    echo "Module sources unchanged since the last pack; skipping"
else
    rm -f "$STAMPS/module"
    echo "Packing the module (each resource converts through nwn_gff)"
    (cd "$REPO_ROOT/Module" && dotnet "$CLI_DLL" --pack "./$MODULE_NAME.mod" --no-prompt | grep -v '^Packed [0-9,]*/')
    mkdir -p "$SERVER_HOME/modules"
    mv -f "$REPO_ROOT/Module/$MODULE_NAME.mod" "$SERVER_HOME/modules/$MODULE_NAME.mod"
    echo "$module_stamp" > "$STAMPS/module"
fi
ls -la "$SERVER_HOME/modules/$MODULE_NAME.mod"

if [ "$SKIP_IMAGES" -eq 0 ]; then
    section "Docker images"
    if ! docker info >/dev/null 2>&1; then
        if [ "$(id -u)" = 0 ] && command -v dockerd >/dev/null; then
            echo "Starting the Docker daemon (log: $SERVER_HOME/dockerd.log)"
            nohup dockerd >"$SERVER_HOME/dockerd.log" 2>&1 &
            for _ in $(seq 1 30); do docker info >/dev/null 2>&1 && break; sleep 1; done
        fi
        docker info >/dev/null 2>&1 || { echo "Docker is not running." >&2; exit 1; }
    fi
    export SWLOR_ENGINE_TEST_HOME="$SERVER_HOME"
    # Docker Hub rate-limits anonymous pulls; retry with backoff.
    for delay in 0 30 60 120; do
        sleep "$delay"
        if docker compose -f "$COMPOSE_FILE" pull --quiet redis swlor-server; then
            pulled=1
            break
        fi
        echo "Pull failed; retrying" >&2
    done
    if [ "${pulled:-0}" != 1 ]; then
        # Keep going with images an earlier run pulled.
        for image in $(docker compose -f "$COMPOSE_FILE" config --images redis swlor-server); do
            docker image inspect "$image" >/dev/null 2>&1 \
                || { echo "Could not pull the engine test images." >&2; exit 1; }
        done
        echo "Could not pull the engine test images; using the local copies." >&2
    fi
fi

section "Ready"
# A full sweep took about 75 minutes in a 4-core cloud session, close to the 90-minute default.
echo "Run: scripts/run-engine-tests.sh --server-home \"$SERVER_HOME\" --timeout-minutes 120"
