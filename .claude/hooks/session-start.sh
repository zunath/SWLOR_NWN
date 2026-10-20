#!/bin/bash
# Prepares a Claude Code cloud session (Linux) for SWLOR development.
# Does nothing on local machines. Safe to run more than once.
#
# Sets up:
#   - Wine plus a binfmt handler, so the bundled Windows tools (nwn_erf.exe, nwn_gff.exe,
#     nwn_tlk.exe, nwn_script_comp.exe) run when SWLOR.CLI or a script launches them
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

# --- System packages: Wine for the bundled Windows tools, Xvfb and Mesa for OpenGL tests ---
packages=()
[ -x /usr/lib/wine/wine64 ] || packages+=(wine64)
command -v Xvfb >/dev/null || packages+=(xvfb)
dpkg -s libgl1-mesa-dri libglx-mesa0 >/dev/null 2>&1 || packages+=(libgl1-mesa-dri libglx-mesa0)
if [ "${#packages[@]}" -gt 0 ]; then
  log "Installing ${packages[*]}"
  apt-get update -qq
  apt-get install -y -qq --no-install-recommends "${packages[@]}" >/dev/null
fi

# --- Virtual display: the toolset's native viewport test renders through OpenGL (Mesa llvmpipe) ---
if [ ! -e /tmp/.X11-unix/X99 ]; then
  setsid nohup Xvfb :99 -screen 0 1920x1080x24 -nolisten tcp >/tmp/xvfb.log 2>&1 </dev/null &
fi
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
"$REPO/tools/SWLOR.CLI/nwn_erf.exe" --version 2>&1 | grep -q 'Version:' || log "nwn_erf.exe did not run under Wine"

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
PIP_BREAK_SYSTEM_PACKAGES=1 python3 -m pip install -q --upgrade --disable-pip-version-check \
  --root-user-action=ignore -r "$REPO/tools/nwn_visual_check/requirements.txt" pefile unicorn mathutils

# --- NuGet packages ---
log "Restoring NuGet packages"
dotnet restore "$REPO/SWLOR.Game.Server.sln" -v q >/dev/null

mkdir -p "$HOME/.local/share/Neverwinter Nights"

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
    echo 'export WINEDEBUG=-all'
    echo 'export PYTHONDONTWRITEBYTECODE=1'
    # Avalonia's build telemetry host is blocked by the network policy.
    echo 'export AVALONIA_TELEMETRY_OPTOUT=1'
    echo "export PATH=\"\$PATH:$HOME/.dotnet/tools\""
    # Dedicated-server game data from session-start-game-data.sh, for the toolset and the script
    # compiler. NWN_HOME stands in for the user directory the compiler insists on.
    echo 'export NWN_INSTALL_PATH=/opt/nwn'
    echo 'export NWN_ROOT=/opt/nwn'
    echo "export NWN_HOME=\"$HOME/.local/share/Neverwinter Nights\""
    echo 'export DISPLAY=:99'
  } >> "$CLAUDE_ENV_FILE"
fi

[ -e /tmp/swlor-haks-checkout.done ] ||
  log "SWLOR_Haks is checking out in the background; see /tmp/swlor-haks-checkout.log"
[ -e /tmp/swlor-game-data.done ] ||
  log "Game data is copying to /opt/nwn in the background; see /tmp/swlor-game-data.log"
log "Done"
