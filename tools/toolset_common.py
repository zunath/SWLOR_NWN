"""Shared Windows/Linux toolset build operations."""
import ctypes.util
import hashlib
import json
import os
from pathlib import Path
import platform
import shutil
import sys
import subprocess
import tempfile
import urllib.request
import zipfile

if sys.version_info < (3, 11):
    raise SystemExit('Install Python 3.11 or newer to use release and maintainer commands.')

ROOT = Path(__file__).resolve().parents[1]
_NATIVE_CONFIG = json.loads((ROOT / 'tools/native-tools.json').read_text(encoding='utf-8'))
NATIVE_VERSION = _NATIVE_CONFIG['Version']
LINUX_URL = _NATIVE_CONFIG['LinuxUrl']
LINUX_SHA256 = _NATIVE_CONFIG['LinuxSha256']
NATIVE_NAMES = tuple(_NATIVE_CONFIG['NativeNames'])
WINDOWS_NATIVE_FILES = tuple(_NATIVE_CONFIG['WindowsFiles'])


def runtime_id():
    if platform.machine().lower() not in ('x86_64', 'amd64'):
        raise RuntimeError('The toolset currently supports x64 Windows and Linux.')
    if os.name == 'nt':
        return 'win-x64'
    if platform.system() == 'Linux':
        return 'linux-x64'
    raise RuntimeError('The toolset currently supports Windows and Linux.')


def executable(name):
    return name + '.exe' if os.name == 'nt' else name


def run(arguments, cwd=ROOT, env=None, capture=False, timeout=1200):
    result = subprocess.run([str(arg) for arg in arguments], cwd=cwd, env=env,
                            text=True, capture_output=capture, timeout=timeout)
    if result.returncode:
        detail = (result.stderr or result.stdout or '').strip() if capture else ''
        raise RuntimeError(f'Command failed ({result.returncode}): {arguments[0]} {detail}')
    return result.stdout.strip() if capture else None


def file_hash(path):
    with Path(path).open('rb') as source:
        return hashlib.file_digest(source, 'sha256').hexdigest()


def native_cache():
    base = Path(os.environ.get('LOCALAPPDATA', Path.home() / 'AppData/Local')) if os.name == 'nt' else Path(os.environ.get('XDG_DATA_HOME', Path.home() / '.local/share'))
    return base / 'SWLOR.Toolset/NativeTools' / runtime_id()


def native_environment(directory):
    environment = os.environ.copy()
    environment['SWLOR_NATIVE_TOOLS'] = str(directory)
    environment['PATH'] = str(directory) + os.pathsep + environment.get('PATH', '')
    if os.name != 'nt':
        environment['LD_LIBRARY_PATH'] = str(directory) + (':' + environment['LD_LIBRARY_PATH'] if environment.get('LD_LIBRARY_PATH') else '')
    return environment


def system_library(name):
    soname = ctypes.util.find_library(name)
    if not soname:
        raise RuntimeError('Linux native tools require libpcre3 and libsqlite3-0. Install them with your package manager first.')
    result = run(['ldconfig', '-p'], capture=True)
    for line in result.splitlines():
        if line.strip().startswith(soname + ' ') and 'x86-64' in line:
            return soname, Path(line.split(' => ', 1)[1])
    raise RuntimeError(f'Cannot locate x64 native library {soname}.')


def prepare_native_tools():
    if os.name == 'nt':
        directory = ROOT / 'tools/SWLOR.CLI'
        for name in WINDOWS_NATIVE_FILES:
            if not (directory / name).is_file():
                raise RuntimeError(f'Missing bundled utility {name}; restore tools/SWLOR.CLI from Git.')
        return directory
    directory = native_cache()
    marker = directory / 'native-tools.json'
    if marker.is_file():
        data = json.loads(marker.read_text())
        if data.get('Version') == NATIVE_VERSION and set((*NATIVE_NAMES, 'libnwnscriptcomp.so', 'libpcre.so.3', 'libsqlite3.so.0')).issubset(data.get('Files', {})) and all((directory / name).is_file() and file_hash(directory / name) == checksum for name, checksum in data.get('Files', {}).items()) and all((directory / name).is_file() for name in NATIVE_NAMES):
            return directory
    libraries = [system_library(name) for name in ('pcre', 'sqlite3')]
    directory.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='.swlor-native-', dir=directory.parent) as temporary:
        temporary = Path(temporary)
        archive = temporary / 'neverwinter.zip'
        print(f'Downloading verified neverwinter.nim {NATIVE_VERSION} Linux tools...', flush=True)
        urllib.request.urlretrieve(LINUX_URL, archive)
        if file_hash(archive) != LINUX_SHA256:
            raise RuntimeError('The native tools download failed its SHA-256 check.')
        staging = temporary / 'tools'
        staging.mkdir()
        with zipfile.ZipFile(archive) as bundle:
            for name in (*NATIVE_NAMES, 'libnwnscriptcomp.so'):
                (staging / name).write_bytes(bundle.read(name))
                (staging / name).chmod(0o755)
        for soname, source in libraries:
            shutil.copyfile(source, staging / soname)
        for package in ('libpcre3', 'libsqlite3-0'):
            notice = Path('/usr/share/doc') / package / 'copyright'
            if notice.is_file():
                shutil.copyfile(notice, staging / (package + '-copyright.txt'))
        environment = native_environment(staging)
        run([staging / 'nwn_script_comp', '--help'], env=environment, capture=True, timeout=30)
        hashes = {path.name: file_hash(path) for path in staging.iterdir() if path.is_file()}
        (staging / 'native-tools.json').write_text(json.dumps({'Version': NATIVE_VERSION, 'Source': LINUX_URL, 'ArchiveSha256': LINUX_SHA256, 'Files': hashes}, indent=2) + '\n')
        if directory.exists():
            # Refuse to discard a cache that may be in use. It can be removed explicitly after closing the toolset.
            raise RuntimeError(f'The native tool cache is incomplete. Close the toolset, remove {directory}, and rerun setup.')
        staging.rename(directory)
    return directory


def new_output(directory):
    directory = Path(directory).resolve()
    if directory == ROOT or ROOT in directory.parents:
        raise RuntimeError('Choose an output directory outside the source checkout.')
    directory.mkdir(parents=True, exist_ok=False)
    return directory
