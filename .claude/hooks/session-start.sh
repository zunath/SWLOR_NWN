#!/bin/bash
# Prepares a Claude Code cloud session (Linux) for SWLOR development.
# Does nothing on local machines. Safe to run more than once.
#
# Sets up:
#   - the native Linux NWN tools (nwn_erf, nwn_gff, nwn_tlk, nwn_script_comp) that SWLOR.CLI and
#     the toolset use, in the same verified cache Setup.sh prepares
#   - Wine plus a binfmt handler for the Windows-only tools: CleanModels, and the bundled .exe
#     tools some tests and scripts launch directly
#   - Xvfb on :99 with Mesa's software OpenGL, for the toolset's native viewport test
#   - PowerShell 7 as pwsh, powershell and powershell.exe for the .ps1 tools and tests
#   - Python packages used by tools/
#   - NuGet packages for the solution
#   - environment variables, including where the game data and display are
#
# In the background, session-start-haks.sh checks out SWLOR_Haks and session-start-game-data.sh
# copies the dedicated-server game data to /opt/nwn. Engine tests:
# scripts/prepare-engine-test-home.sh.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

REPO="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
export DEBIAN_FRONTEND=noninteractive DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 WINEDEBUG=-all \
  AVALONIA_TELEMETRY_OPTOUT=1

log() { echo "[session-start] $*" >&2; }
# Where tools/toolset_common.py and NativeToolPath keep the native NWN tools.
NATIVE_TOOLS="${XDG_DATA_HOME:-$HOME/.local/share}/SWLOR.Toolset/NativeTools/linux-x64"

# Session environment first, so a failed install step below cannot leave it unset.
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
    echo 'export WINEDEBUG=-all'
    echo 'export PYTHONDONTWRITEBYTECODE=1'
    # Avalonia's build telemetry host is blocked by the network policy.
    echo 'export AVALONIA_TELEMETRY_OPTOUT=1'
    echo "export PATH=\"\$PATH:$HOME/.dotnet/tools:$NATIVE_TOOLS\""
    # Dedicated-server game data from session-start-game-data.sh, for the toolset and the script
    # compiler. NWN_HOME stands in for the user directory the compiler insists on.
    echo 'export NWN_INSTALL_PATH=/opt/nwn'
    echo 'export NWN_ROOT=/opt/nwn'
    echo "export NWN_HOME=\"$HOME/.local/share/Neverwinter Nights\""
    echo 'export DISPLAY=:99'
  } >> "$CLAUDE_ENV_FILE"
fi

# Virtual display for the toolset's native viewport test (OpenGL through Mesa llvmpipe). Started
# unless the process named in its lock file is alive; a killed Xvfb leaves both files behind.
start_xvfb() {
  if ! { [ -e /tmp/.X99-lock ] && kill -0 "$(tr -d ' ' </tmp/.X99-lock)" 2>/dev/null; }; then
    rm -f /tmp/.X99-lock /tmp/.X11-unix/X99
    setsid nohup Xvfb :99 -screen 0 1920x1080x24 -nolisten tcp >/tmp/xvfb.log 2>&1 </dev/null &
  fi
}

# SessionStart also fires after /clear and compaction. Once a full run has prepared this
# container with the current setup inputs, only the environment above and the display need
# refreshing. The marker holds a fingerprint of those inputs, so changing one reruns setup.
DONE=/tmp/swlor-session-start.done
inputs=$(cat "$REPO/.claude/hooks/session-start.sh" "$REPO/tools/native-tools.json" \
  "$REPO/tools/toolset_common.py" "$REPO/tools/nwn_visual_check/requirements.txt" 2>/dev/null |
  sha256sum | cut -d' ' -f1)
if [ "$(cat "$DONE" 2>/dev/null)" = "$inputs" ]; then
  start_xvfb
  exit 0
fi
# A required step that fails clears this, leaving the marker unwritten so the next session start
# tries again.
complete=1

# --- System packages: libpcre3 for the native NWN tools, Wine for the Windows-only tools, Xvfb
# and Mesa for OpenGL tests ---
packages=()
dpkg -s libpcre3 libsqlite3-0 >/dev/null 2>&1 || packages+=(libpcre3 libsqlite3-0)
[ -x /usr/lib/wine/wine64 ] || packages+=(wine64)
command -v Xvfb >/dev/null || packages+=(xvfb)
dpkg -s libgl1-mesa-dri libglx-mesa0 >/dev/null 2>&1 || packages+=(libgl1-mesa-dri libglx-mesa0)
if [ "${#packages[@]}" -gt 0 ]; then
  log "Installing ${packages[*]}"
  apt-get update -qq
  apt-get install -y -qq --no-install-recommends "${packages[@]}" >/dev/null
fi

start_xvfb
cat > /usr/local/bin/wine-exe <<'EOF'
#!/bin/sh
export WINEDEBUG="${WINEDEBUG:--all}"
exec /usr/lib/wine/wine64 "$@"
EOF
chmod +x /usr/local/bin/wine-exe
if [ ! -e /proc/sys/fs/binfmt_misc/register ]; then
  mount -t binfmt_misc binfmt_misc /proc/sys/fs/binfmt_misc 2>/dev/null || log "Could not mount binfmt_misc"
fi
if [ -e /proc/sys/fs/binfmt_misc/register ] && [ ! -e /proc/sys/fs/binfmt_misc/WinPE ]; then
  echo ':WinPE:M::MZ::/usr/local/bin/wine-exe:' > /proc/sys/fs/binfmt_misc/register
fi
# Git stores the .exe tools without the executable bit. Set it, and stop git reporting it.
git -C "$REPO" config core.fileMode false
(cd "$REPO" && git ls-files -z '*.exe' | xargs -0 -r chmod +x)
# The first run also creates the Wine prefix. Pipe the output: Wine tools fail writing to /dev/null.
"$REPO/tools/SWLOR.CLI/nwn_erf.exe" --version 2>&1 | grep -q 'Version:' ||
  { complete=0; log "nwn_erf.exe did not run under Wine"; }

# --- Native NWN tools: the pinned neverwinter.nim release from tools/native-tools.json, downloaded
# and checksum-verified by the same code Setup.sh uses ---
python3 -I -c 'import sys; sys.path.insert(0, sys.argv[1]); import toolset_common; toolset_common.prepare_native_tools()' \
  "$REPO/tools" >&2 || { complete=0; log "Could not prepare the native NWN tools in $NATIVE_TOOLS"; }

# --- PowerShell 7 ---
if [ ! -x "$HOME/.dotnet/tools/pwsh" ]; then
  log "Installing PowerShell"
  dotnet tool install --global PowerShell >/dev/null
fi
for name in pwsh powershell powershell.exe; do
  ln -sfn "$HOME/.dotnet/tools/pwsh" "/usr/local/bin/$name"
done

# --- Python packages ---
# --upgrade: the image's apt Pillow is built for another Python version and fails to import.
# mathutils is Blender's math module, published standalone, for the model tool tests.
# SWLOR_Haks/requirements.txt is installed by session-start-haks.sh after the checkout.
# The lock keeps this and session-start-haks.sh from changing the shared site-packages at once.
PIP_BREAK_SYSTEM_PACKAGES=1 flock /tmp/swlor-pip.lock python3 -m pip install -q --upgrade \
  --disable-pip-version-check --root-user-action=ignore \
  -r "$REPO/tools/nwn_visual_check/requirements.txt" pefile unicorn mathutils

# --- NuGet packages ---
log "Restoring NuGet packages"
dotnet restore "$REPO/SWLOR.Game.Server.sln" -v q >/dev/null

mkdir -p "$HOME/.local/share/Neverwinter Nights"

[ -e /tmp/swlor-haks-checkout.done ] ||
  log "SWLOR_Haks is checking out in the background; see /tmp/swlor-haks-checkout.log"
[ -e /tmp/swlor-game-data.done ] ||
  log "Game data is copying to /opt/nwn in the background; see /tmp/swlor-game-data.log"
if [ "$complete" = 1 ]; then
  echo "$inputs" > "$DONE"
  log "Done"
else
  log "Done, with the failures above; the next session start retries them"
fi
