#!/usr/bin/env python3
"""Check, prepare, or launch a SWLOR source checkout on Windows and Linux."""
import argparse
import ctypes
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from toolset_common import ROOT, NATIVE_NAMES, WINDOWS_NATIVE_FILES, executable, native_cache, native_environment, prepare_native_tools, run, runtime_id


def check(profile):
    failures = []
    for name in ('git', 'dotnet'):
        if not shutil.which(name):
            failures.append('Install Git.' if name == 'git' else 'Install a stable .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0.')
        else:
            print('OK: ' + name)
    if shutil.which('dotnet'):
        try:
            sdk = run(['dotnet', '--version'], capture=True, timeout=30)
            if not re.fullmatch(r'10\.\d+\.\d+', sdk):
                raise RuntimeError('Unsupported SDK.')
            print('OK: .NET SDK ' + sdk)
        except RuntimeError:
            failures.append('Install a stable .NET 10 SDK. global.json selects the newest installed .NET 10 feature band; previews are not used.')
    if os.name == 'nt':
        for name in WINDOWS_NATIVE_FILES:
            if not (ROOT / 'tools/SWLOR.CLI' / name).is_file():
                failures.append('Restore the bundled utility ' + name + ' from Git.')
    else:
        for library in ('libpcre.so.3', 'libsqlite3.so.0'):
            try:
                ctypes.CDLL(library)
            except OSError:
                failures.append('Install libpcre3 and libsqlite3-0 with your Linux package manager.')
                break
        if profile == 'Toolset':
            for library in ('libfontconfig.so.1', 'libX11.so.6', 'libGL.so.1'):
                try:
                    ctypes.CDLL(library)
                except OSError:
                    failures.append('Install the desktop dependencies: libfontconfig1 libx11-6 libgl1. Use an X11 desktop or Wayland with XWayland.')
                    break
    if profile == 'Server':
        if not shutil.which('docker'):
            failures.append('Install Docker Engine and the Compose plugin on Linux, or Docker Desktop on Windows.')
        else:
            try:
                if run(['docker', 'info', '--format', '{{.OSType}}'], capture=True, timeout=30) != 'linux':
                    failures.append('Configure Docker to run Linux containers.')
                run(['docker', 'compose', 'version'], capture=True, timeout=30)
                print('OK: Docker and Compose')
            except RuntimeError:
                failures.append('Start Docker and verify your account can access the daemon and Docker Compose.')
    return failures


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode', choices=['Check', 'Setup', 'Toolset', 'Server'], default='Check', type=str.title)
    parser.add_argument('--profile', choices=['Toolset', 'Server'], default='Toolset', type=str.title)
    args = parser.parse_args()
    if args.mode == 'Server':
        args.profile = 'Server'
    os.chdir(ROOT)
    try:
        runtime_id()
        failures = check(args.profile)
        if failures:
            for failure in failures:
                print('NEEDS ATTENTION: ' + failure)
            return 1
        if args.mode == 'Check':
            if not (ROOT / 'SWLOR_Haks/sw_tlk/sw_tlk.tlk.json').is_file():
                print('NEEDS ATTENTION: SWLOR assets are missing. Run Setup.cmd or ./Setup.sh.')
                return 1
            if os.name != 'nt' and not all((native_cache() / name).is_file() for name in NATIVE_NAMES):
                print('NEEDS ATTENTION: Linux native tools are missing. Run ./Setup.sh.')
                return 1
            print('Setup checks passed. No files or containers were changed.')
            return 0
        run(['git', 'submodule', 'update', '--init', '--recursive'])
        native = prepare_native_tools()
        environment = native_environment(native)
        project = 'SWLOR.Runner/SWLOR.Runner.csproj' if args.profile == 'Server' else 'SWLOR.Toolset/SWLOR.Toolset.csproj'
        run(['dotnet', 'build', project, '-p:RunPostBuildEvent=Never'], env=environment)
        if args.mode == 'Setup':
            print('Setup complete. Use StartToolset.cmd / ./StartToolset.sh or StartServer.cmd / ./StartServer.sh.')
            return 0
        if args.mode == 'Server':
            run(['dotnet', 'build', 'SWLOR.CLI/SWLOR.CLI.csproj', '-c', 'Release', '-p:RunPostBuildEvent=Never'], env=environment)
            cli = ROOT / 'SWLOR.CLI/bin/Release/net10.0/SWLOR.CLI.dll'
            run(['dotnet', cli, '-p', './Star Wars LOR v2.mod', '--no-prompt'], cwd=ROOT / 'Module', env=environment)
            run(['dotnet', cli, '-o', '--server-output', ROOT / 'SWLOR.Game.Server/bin/Debug/net10.0'], cwd=ROOT / 'Build', env=environment)
        elif os.name != 'nt' and not os.environ.get('DISPLAY'):
            raise RuntimeError('StartToolset.sh needs an X11 desktop or Wayland with XWayland (DISPLAY).')
        # Launch in the foreground. Ctrl+C reaches the launcher and its normal cleanup handlers.
        return subprocess.call(['dotnet', 'run', '--no-build', '--project', project], cwd=ROOT, env=environment)
    except (OSError, RuntimeError, subprocess.SubprocessError) as error:
        print('Setup failed: ' + str(error), file=sys.stderr)
        return 1

if __name__ == '__main__':
    sys.exit(main())
