"""Task-owned profiles; never grant client access to the normal user profile."""
from pathlib import Path
import json
import shutil
import socket
import tempfile
import tomllib
import uuid

MARKER = '.swlor-visual-profile.json'
SETTINGS = """[sound.effects]
volume=0.0
[sound.music]
volume=0.0
[sound.streams]
volume=0.0
[sound.voices]
volume=0.0
[graphics.intro.splash]
enabled=false
[graphics.movies]
enabled=false
[graphics.movies.intro]
enabled=false
[graphics.window]
mode="windowed"
[graphics.window.windowed]
width=1280
height=720
[game.auto-pause.on-focus-loss]
enabled=false
[graphics.video.vsync]
mode="off"
[server.identity]
public=false
[server.net]
accept-relayed-connections=false
port={port}
"""


def reject_links(path):
    path = Path(path).absolute()
    for ancestor in [path, *path.parents]:
        if ancestor.is_symlink() or ancestor.is_junction():
            raise RuntimeError('Profile cannot traverse a reparse point')
    if path.exists():
        for entry in path.rglob('*'):
            if entry.is_symlink() or entry.is_junction():
                raise RuntimeError('Profile cannot contain reparse points')
            # AppContainer ACL/integrity changes affect every name of a hardlinked file.
            if entry.is_file() and entry.stat().st_nlink > 1:
                raise RuntimeError('Profile cannot contain hardlinks to shared files')


def validate_owned(profile):
    profile = Path(profile).absolute()
    reject_links(profile)
    if profile.parent.resolve() != Path(tempfile.gettempdir()).resolve() or not profile.name.startswith('swlor-nwn-'):
        raise RuntimeError('Requires an owned temporary profile')
    marker = json.loads((profile / MARKER).read_text(encoding='utf8'))
    if marker.get('profile') != str(profile.resolve()) or not marker.get('token'):
        raise RuntimeError('Profile ownership marker does not match')


def validate_profile(profile, command, timeout):
    if not isinstance(timeout, (int, float)) or not 0 < timeout <= 600:
        raise ValueError('Client timeout must be between 0 and 600 seconds')
    validate_owned(profile)
    if (profile / 'cdkey.ini').exists():
        raise RuntimeError('Requires a key-free temporary profile')
    if Path(command[0]).name.lower() != 'nwmain.exe':
        return  # Used by the Windows isolation probes, never as a client fallback.
    if command.count('-userdirectory') != 1 or Path(command[command.index('-userdirectory') + 1]).resolve() != profile.resolve():
        raise RuntimeError('Native client must use the validated profile')
    settings = tomllib.loads((profile / 'settings.tml').read_text(encoding='utf8'))
    server = settings.get('server', {})
    port = server.get('net', {}).get('port')
    if type(port) is not int or not 1024 <= port <= 65535 or port == 5121:
        raise RuntimeError('Native client requires an explicit permitted port')
    if server.get('identity', {}).get('public') is not False or server.get('net', {}).get('accept-relayed-connections') is not False:
        raise RuntimeError('Native client requires a private local profile')
    sound = settings.get('sound', {})
    volumes = [sound.get(channel, {}).get('volume') for channel in ('effects','music','streams','voices')]
    if any(type(volume) not in (int, float) or volume != 0 for volume in volumes):
        raise RuntimeError('All four isolated sound channels must be zero')
    graphics = settings.get('graphics', {})
    if graphics.get('intro', {}).get('splash', {}).get('enabled') is not False or graphics.get('movies', {}).get('enabled') is not False or graphics.get('movies', {}).get('intro', {}).get('enabled') is not False:
        raise RuntimeError('Isolated splash and movies must be disabled')
    window = graphics.get('window', {})
    if window.get('mode') != 'windowed' or window.get('windowed', {}).get('width') != 1280 or window.get('windowed', {}).get('height') != 720:
        raise RuntimeError('Native capture requires a 1280x720 window')
    if settings.get('game', {}).get('auto-pause', {}).get('on-focus-loss', {}).get('enabled') is not False:
        raise RuntimeError('Background client must continue rendering without focus')


def create_profile():
    profile = Path(tempfile.mkdtemp(prefix='swlor-nwn-')).resolve()
    try:
        (profile / MARKER).write_text(json.dumps({'profile':str(profile), 'token':uuid.uuid4().hex}))
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as probe:
            probe.bind(('0.0.0.0', 0))
            port = probe.getsockname()[1]
        if port == 5121 or port < 1024:
            raise RuntimeError('No permitted embedded server port')
        (profile / 'settings.tml').write_text(SETTINGS.format(port=port))
        for folder in ('modules','hak','tlk'):
            (profile / folder).mkdir()
        return profile
    except BaseException as error:
        # This is the exact directory freshly allocated above; marker validation
        # cannot be required when creating that marker is what failed.
        try:
            reject_links(profile)
            shutil.rmtree(profile)
        except BaseException as cleanup_error:
            raise BaseExceptionGroup('Profile creation and cleanup failed', [error,cleanup_error])
        raise


def copy_input(source, target):
    source, target = Path(source), Path(target)
    if not source.is_file():
        raise FileNotFoundError(source)
    # Copies are intentional: hardlinks would propagate the low-integrity ACL to source assets.
    shutil.copyfile(source, target)


def log_text(profile):
    path = profile / 'logs/nwclientLog1.txt'
    return path.read_bytes().replace(b'\0', b'').decode('utf8', errors='replace') if path.exists() else ''


def cleanup(profile):
    validate_owned(profile)
    shutil.rmtree(profile)
