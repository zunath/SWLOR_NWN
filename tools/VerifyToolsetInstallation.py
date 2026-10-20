#!/usr/bin/env python3
"""Verify a self-contained installation; optionally exercise a real Linux X11 desktop."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile
import time

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('application', type=Path)
    parser.add_argument('report', type=Path)
    parser.add_argument('--desktop', action='store_true')
    args = parser.parse_args()
    application = args.application.resolve()
    report = args.report.resolve()
    executable = application / ('SWLOR.Toolset.exe' if os.name == 'nt' else 'SWLOR.Toolset')
    environment = os.environ.copy()
    environment['DOTNET_ROOT'] = str(application / 'no-installed-sdk')
    environment['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    environment.pop('SWLOR_NATIVE_TOOLS', None)
    environment.pop('LD_LIBRARY_PATH', None)
    environment['PATH'] = os.pathsep.join([os.path.join(os.environ['SystemRoot'], 'System32'), os.environ['SystemRoot']]) if os.name == 'nt' else '/usr/bin:/bin'
    with tempfile.TemporaryDirectory(prefix='swlor-install-check-') as temporary:
        subprocess.run([executable, '--verify-install', report], cwd=temporary, env=environment, check=True, timeout=60)
        result = json.loads(report.read_text())
        if not result.get('Success'):
            raise RuntimeError('The installed application failed native compilation or packing.')
        if args.desktop:
            if os.name == 'nt' or not environment.get('DISPLAY'):
                raise RuntimeError('--desktop requires a Linux X11 display (xvfb-run is supported).')
            environment['XDG_DATA_HOME'] = str(Path(temporary) / 'data')
            environment['XDG_CONFIG_HOME'] = str(Path(temporary) / 'config')
            with (Path(temporary) / 'desktop.log').open('w+') as log:
                process = subprocess.Popen([executable], cwd=temporary, env=environment, stdout=log, stderr=log)
                try:
                    deadline = time.monotonic() + 30
                    while time.monotonic() < deadline:
                        if process.poll() is not None:
                            log.seek(0)
                            raise RuntimeError('The Linux desktop exited before opening: ' + log.read())
                        windows = subprocess.run(['xwininfo', '-root', '-tree'], capture_output=True, text=True, check=True, timeout=5).stdout
                        if 'SWLOR Toolset' in windows and 'Workspace' in windows:
                            time.sleep(2)
                            if process.poll() is not None:
                                raise RuntimeError('The Linux desktop exited after opening.')
                            result['DesktopOpened'] = True
                            report.write_text(json.dumps(result))
                            break
                        time.sleep(0.25)
                    else:
                        log.seek(0)
                        raise RuntimeError('No toolset workspace window appeared: ' + log.read())
                finally:
                    if process.poll() is None:
                        process.terminate()
                        try:
                            process.wait(timeout=10)
                        except subprocess.TimeoutExpired:
                            process.kill()
                            process.wait(timeout=10)
    print(report.read_text())

if __name__ == '__main__':
    main()
