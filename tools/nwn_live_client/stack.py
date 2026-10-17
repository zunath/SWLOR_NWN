"""Private SWLOR server plus a Linux NWN:EE client for live-server client evidence.

The stack runs entirely in Docker under a unique Compose project: a tmpfs Redis, a
private server built from this checkout, and the Linux client on an Xvfb display
inside its own container. Input and captures stay inside that display, so the host
desktop, focus, audio and Windows client are never touched. The procedure is in
SWLOR.Game.Server/Readmes/InEngineVisualVerification.md ("Server behavior").
"""
from __future__ import annotations

import csv
import datetime
import hashlib
import io
import json
import math
import os
import re
import secrets
import shutil
import struct
import subprocess
import tempfile
import time
from pathlib import Path

PACKAGE_DIR = Path(__file__).resolve().parent
REPO_ROOT = PACKAGE_DIR.parents[1]
COMPOSE_FILE = PACKAGE_DIR / 'compose.yml'
SERVER_DOCKER_DIR = REPO_ROOT / 'SWLOR.Game.Server' / 'Docker'
SCRIPTS = ('run-client.sh', 'server-entry.sh', 'ui.sh')

PROJECT_PREFIX = 'swlor-live-'
SERVER_PORT = 5121
# Character select "Play" button at the profile's 1280x720 window and UI scale 1.0.
PLAY_BUTTON = (742, 591)
# First row of the character list; a DM's Play stays disabled until its avatar row is selected.
FIRST_CHARACTER_ROW = (436, 135)
WINDOW_SIZE = (1280, 720)
STEAM_INSTALL = Path('C:/Program Files (x86)/Steam/steamapps/common/Neverwinter Nights')
# nwserverLog connection lines; "Joined as" is written when the client connects, before character select.
JOIN_PATTERN = re.compile(r'\) Joined as (Player|Game Master) \d+')
LEFT_PATTERN = re.compile(r' Left as a (Player|Game Master) ')
ENTRY_PATTERN = re.compile(r': Connected to server\r?$', re.M)
LOADING_TIP_PATTERN = re.compile(r'^\[CHAT WINDOW TEXT\] \[[^]]+\] Loading Screen: ', re.M)


class LiveStackError(RuntimeError):
    """A prerequisite, start-up or teardown step failed."""


def log(message: str) -> None:
    print(f'[nwn-live] {message}', flush=True)


# ---------------------------------------------------------------- pure helpers

def project_name(label: str | None) -> str:
    """Return the Compose project name, always prefixed so ownership is unambiguous."""
    suffix = label.lower() if label else secrets.token_hex(3)
    if suffix.startswith(PROJECT_PREFIX):
        suffix = suffix[len(PROJECT_PREFIX):]
    name = PROJECT_PREFIX + suffix
    if not re.fullmatch(r'[a-z0-9][a-z0-9_-]{0,62}', name):
        raise LiveStackError(f'Invalid project name {name!r}: use lowercase letters, digits, "-" and "_".')
    return name


def validate_public_cdkey(key: str) -> str:
    key = key.strip().upper()
    if not re.fullmatch(r'[A-Z0-9]{8}', key):
        raise LiveStackError('The public CD key must be the 8-character key from a server log '
                             '"Joined as Player" line, never the private key in cdkey.ini.')
    return key


def read_gff_string(path: Path, label: str) -> str | None:
    """Read a top-level CExoString field from a GFF V3.2 file such as a .bic."""
    data = path.read_bytes()
    if len(data) < 56 or data[4:8] != b'V3.2':
        raise LiveStackError(f'{path} is not a GFF V3.2 file.')
    try:
        return _read_gff_string(path, data, label)
    except struct.error as error:
        raise LiveStackError(f'{path} is damaged: {error}.') from None


def _read_gff_string(path: Path, data: bytes, label: str) -> str | None:
    (struct_offset, _, field_offset, _, label_offset, _, field_data_offset, _,
     field_indices_offset, _, _, _) = struct.unpack_from('<12I', data, 8)
    _, struct_data, field_count = struct.unpack_from('<3I', data, struct_offset)
    if field_count == 1:
        indices = [struct_data]
    else:
        indices = struct.unpack_from(f'<{field_count}I', data, field_indices_offset + struct_data)
    for index in indices:
        field_type, label_index, field_data = struct.unpack_from('<3I', data, field_offset + index * 12)
        start = label_offset + label_index * 16
        if data[start:start + 16].rstrip(b'\0').decode('ascii', 'replace') != label:
            continue
        if field_type != 10:
            raise LiveStackError(f'{path}: field {label} is not a string.')
        length = struct.unpack_from('<I', data, field_data_offset + field_data)[0]
        offset = field_data_offset + field_data + 4
        return data[offset:offset + length].decode('utf-8', 'replace')
    return None


def character_uuid(path: Path) -> str:
    uuid = read_gff_string(path, 'UUID')
    if not uuid or not re.fullmatch(r'[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}', uuid):
        raise LiveStackError(f'{path} has no object UUID. Log the character into any NWN:EE game once '
                             'so the engine assigns one, or pass --no-seed and seed the Player record yourself.')
    return uuid


def render_env_file(values: dict[str, str]) -> str:
    """Render a Compose env file with literal single-quoted values."""
    lines = []
    for key, value in values.items():
        if not re.fullmatch(r'[A-Z_][A-Z0-9_]*', key):
            raise LiveStackError(f'Invalid environment variable name {key!r}.')
        if any(character in value for character in "'\r\n"):
            raise LiveStackError(f'Value for {key} cannot contain quotes or line breaks.')
        lines.append(f"{key}='{value}'")
    return '\n'.join(lines) + '\n'


def parse_server_env(pairs: list[str]) -> dict[str, str]:
    values = {}
    for pair in pairs:
        key, separator, value = pair.partition('=')
        if not separator:
            raise LiveStackError(f'--server-env expects KEY=VALUE, got {pair!r}.')
        values[key] = value
    render_env_file(values)
    return values


def client_settings(player_name: str) -> str:
    """Client settings.tml: silent, no movies, windowed 1280x720 at UI scale 1.0."""
    if not re.fullmatch(r'[A-Za-z0-9]{3,24}', player_name):
        raise LiveStackError('The player name must be 3-24 letters or digits.')
    return f'''[client.identity]
name = "{player_name}"
[sound.effects]
volume = 0.0
[sound.music]
volume = 0.0
[sound.streams]
volume = 0.0
[sound.voices]
volume = 0.0
[graphics.intro.splash]
enabled = false
[graphics.movies]
enabled = false
[graphics.movies.intro]
enabled = false
[graphics.window]
mode = "windowed"
[graphics.window.windowed]
width = {WINDOW_SIZE[0]}
height = {WINDOW_SIZE[1]}
[graphics.video.vsync]
mode = "off"
[game.auto-pause.on-focus-loss]
enabled = false
[ui]
scale = 1.0
'''


CLIENT_ALIASES = '''[Alias]
HD0=/profile/
CACHE=/profile/cache
CRASHREPORT=/profile/crashreport
CURRENTGAME=/profile/currentgame
DATABASE=/profile/database
DEVELOPMENT=/profile/development
DMVAULT=/profile/dmvault
LOCALVAULT=/profile/localvault
LOGS=/profile/logs
MODELCOMPILER=/profile/modelcompiler
MODULES=/profile/modules
NWSYNC=/profile/nwsync
OVERRIDE=/profile/override
PORTRAITS=/profile/portraits
SAVES=/profile/saves
SCREENSHOTS=/profile/screenshots
SERVERVAULT=/profile/servervault
TEMP=/profile/temp
TEMPCLIENT=/profile/tempclient
HAK=/hak
TLK=/tlk
'''

# Puts the whole chat window, including combat and feedback lines, in logs/nwclientLog1.txt.
CLIENT_PLAYER_INI = '[Game Options]\nClientEntireChatWindowLogging=1\n'


def is_within(path: Path, root: Path) -> bool:
    try:
        path.resolve().relative_to(root.resolve())
        return True
    except ValueError:
        return False


def count_matches(path: Path, pattern: re.Pattern[str]) -> int:
    if not path.is_file():
        return 0
    return len(pattern.findall(path.read_text('utf-8', 'replace')))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def utc_now() -> str:
    return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec='seconds')


# ---------------------------------------------------------------- discovery

def main_checkout() -> Path:
    """The primary checkout, which holds debugserver/ when this is a git worktree."""
    try:
        common = subprocess.run(['git', '-C', str(REPO_ROOT), 'rev-parse', '--path-format=absolute',
                                 '--git-common-dir'], capture_output=True, text=True, check=True).stdout.strip()
        return Path(common).parent
    except (OSError, subprocess.CalledProcessError):
        return REPO_ROOT


DEFAULT_MODULE = 'Star Wars LOR v2.mod'


def repository_roots() -> set[Path]:
    """This checkout, the main checkout, and every linked worktree of the repository."""
    roots = {REPO_ROOT, main_checkout()}
    try:
        listing = output(['git', '-C', str(REPO_ROOT), 'worktree', 'list', '--porcelain'], timeout=60)
    except LiveStackError:
        return roots
    roots.update(Path(line[len('worktree '):]) for line in listing.splitlines() if line.startswith('worktree '))
    return roots


def deploy_home() -> Path | None:
    """The first debugserver/ (this checkout, then the main checkout) holding haks, TLK and module."""
    for candidate in (REPO_ROOT / 'debugserver', main_checkout() / 'debugserver'):
        if (any((candidate / 'hak').glob('*.hak')) and any((candidate / 'tlk').glob('*.tlk'))
                and (candidate / 'modules' / DEFAULT_MODULE).is_file()):
            return candidate
    return None


def server_image() -> str:
    """The NWNX image the repository's engine-test stack runs."""
    text = (SERVER_DOCKER_DIR / 'docker-compose.enginetests.yml').read_text('utf-8')
    match = re.search(r'image:\s*(zunath/nwn-dotnet:\S+)', text)
    if not match:
        raise LiveStackError('Cannot find the server image in docker-compose.enginetests.yml.')
    return match.group(1)


def client_image_tag() -> str:
    dockerfile = (PACKAGE_DIR / 'Dockerfile.client').read_bytes().replace(b'\r\n', b'\n')
    return 'swlor-nwn-live-client:' + hashlib.sha256(dockerfile).hexdigest()[:12]


# ---------------------------------------------------------------- docker

def run(command: list[str], *, check: bool = True, capture: bool = False, timeout: float = 600,
        stdin: bytes | None = None) -> subprocess.CompletedProcess:
    try:
        result = subprocess.run(command, input=stdin, capture_output=capture, timeout=timeout)
    except subprocess.TimeoutExpired as error:
        raise LiveStackError(f'Timed out after {timeout:.0f}s: {" ".join(command[:6])}') from error
    if check and result.returncode != 0:
        detail = result.stderr.decode('utf-8', 'replace').strip() if capture and result.stderr else ''
        raise LiveStackError(f'Command failed ({result.returncode}): {" ".join(command[:8])}\n{detail}')
    return result


def output(command: list[str], timeout: float = 120) -> str:
    return run(command, capture=True, timeout=timeout).stdout.decode('utf-8', 'replace').strip()


def project_resources(project: str) -> dict[str, list[str]]:
    label = f'label=com.docker.compose.project={project}'
    return {
        'containers': output(['docker', 'ps', '-aq', '--filter', label]).split(),
        'volumes': output(['docker', 'volume', 'ls', '-q', '--filter', label]).split(),
        'networks': output(['docker', 'network', 'ls', '-q', '--filter', label]).split(),
    }


class Stack:
    """One run directory and its Compose project."""

    def __init__(self, run_dir: Path):
        self.run_dir = run_dir.resolve()
        self.state_path = self.run_dir / 'state.json'
        if not self.state_path.is_file():
            raise LiveStackError(f'{self.run_dir} has no state.json; it was not created by this tool.')
        self.state = json.loads(self.state_path.read_text('utf-8'))
        self.project = self.state['project']
        self.home = self.run_dir / 'home'
        self.profile = self.run_dir / 'profile'
        self.evidence = self.run_dir / 'evidence'

    @property
    def compose_file(self) -> Path:
        # The run's own snapshot, so later commands use the definition that started it.
        return self.run_dir / 'compose.yml'

    def save(self) -> None:
        # Write then replace, so an interrupted save never leaves a half-written recovery record.
        temporary = self.state_path.with_name('state.json.tmp')
        temporary.write_text(json.dumps(self.state, indent=2) + '\n', 'utf-8')
        os.replace(temporary, self.state_path)

    def compose_command(self, *arguments: str) -> str:
        """The full Compose command line for this run, for recovery instructions."""
        return (f'docker compose -p {self.project} -f "{self.compose_file}" --env-file "{self.run_dir / "compose.env"}" '
                f'--profile client ' + ' '.join(arguments))

    def ensure_owner(self, require_reservation: bool = True) -> bool:
        """Refuse to act when the project name is no longer this run's; True when this run holds it.

        Containers are found by project name, so after a released name is reused by
        another run, a stale run directory would otherwise drive that run's stack.
        """
        holder = reservation_holder(self.project)
        if holder is None:
            if require_reservation:
                raise LiveStackError(f'Project {self.project} has no reservation; this run was torn down.')
            return False
        if not same_path(holder, str(self.run_dir)):
            raise LiveStackError(f'Project {self.project} is now reserved by run {holder}; '
                                 f'refusing to act on it from {self.run_dir}.')
        return True

    def compose(self, *arguments: str, client: bool = False, check: bool = True, capture: bool = False,
                timeout: float = 600) -> subprocess.CompletedProcess:
        command = ['docker', 'compose', '-p', self.project, '-f', str(self.compose_file),
                   '--env-file', str(self.run_dir / 'compose.env')]
        if client:
            command += ['--profile', 'client']
        return run(command + list(arguments), check=check, capture=capture, timeout=timeout)

    def running(self, service: str) -> bool:
        result = self.compose('ps', '--status', 'running', '-q', service, client=service == 'client',
                              check=False, capture=True, timeout=60)
        return result.returncode == 0 and bool(result.stdout.strip())

    @property
    def server_log(self) -> Path:
        return self.home / 'logs' / 'nwserverLog1.txt'

    def joined_players(self) -> int:
        return count_matches(self.server_log, JOIN_PATTERN) - count_matches(self.server_log, LEFT_PATTERN)

    def ui(self, *arguments: str, timeout: float = 180) -> str:
        if not self.running('client'):
            raise LiveStackError('The client container is not running.')
        result = self.compose('exec', '-T', 'client', '/bin/bash', '/tool/ui.sh', *arguments, client=True,
                              check=False, capture=True, timeout=timeout)
        text = (result.stdout + result.stderr).decode('utf-8', 'replace').strip()
        if result.returncode != 0:
            raise LiveStackError(f'ui.sh {" ".join(arguments)} failed: {text}')
        return text

    def console(self, command: str) -> None:
        """Append one line to the nwserver console (for example "status" or "kick <name>")."""
        if '\n' in command or '\r' in command:
            raise LiveStackError('Console commands are single lines.')
        if not self.running('swlor-server'):
            raise LiveStackError('The server container is not running.')
        self.compose('exec', '-T', 'swlor-server', '/bin/sh', '-c', 'printf "%s\\n" "$1" >> /nwn/run/console.in',
                     'console', command, capture=True, timeout=60)


# ---------------------------------------------------------------- commands

def create(options) -> Stack:
    """Validate the inputs and prepare a run directory; no containers start here."""
    if shutil.which('docker') is None:
        raise LiveStackError('docker is not on PATH.')
    output(['docker', 'info', '--format', '{{.ServerVersion}}'], timeout=60)

    deployed = deploy_home()
    def resolved(value: str | None, default: Path | None) -> Path | None:
        # Absolute paths in state.json, so later commands work from any working directory.
        path = Path(value) if value else default
        return path.resolve() if path else None

    hak_dir = resolved(options.hak_dir, deployed / 'hak' if deployed else None)
    tlk_dir = resolved(options.tlk_dir, deployed / 'tlk' if deployed else None)
    module = resolved(options.module, deployed / 'modules' / DEFAULT_MODULE if deployed else None)
    dotnet = resolved(options.dotnet, REPO_ROOT / 'SWLOR.Game.Server' / 'bin' / options.configuration / 'net10.0')
    install = resolved(options.client_install or os.environ.get('NWN_LINUX_CLIENT_INSTALL'), STEAM_INSTALL)
    cdkey = resolved(options.cdkey, Path.home() / 'Documents' / 'Neverwinter Nights' / 'cdkey.ini')
    server_env = resolved(options.server_env_file, SERVER_DOCKER_DIR / 'swlor.env')
    character = resolved(options.character, None)
    dm_avatar = resolved(options.dm_avatar, None)
    mode = 'dm' if dm_avatar else 'player'

    checks = {
        'haks (--hak-dir)': hak_dir is not None and any(hak_dir.glob('*.hak')),
        'TLK (--tlk-dir)': tlk_dir is not None and any(tlk_dir.glob('*.tlk')),
        'module (--module)': module is not None and module.is_file() and module.suffix == '.mod',
        'server build (--dotnet; build with -c Release -p:RunPostBuildEvent=Never)':
            (dotnet / 'SWLOR.Game.Server.dll').is_file()
            and (dotnet / 'SWLOR.Game.Server.runtimeconfig.json').is_file(),
        'Linux client (--client-install)': (install / 'bin' / 'linux-x86' / 'nwmain-linux').is_file(),
        'cdkey.ini (--cdkey)': cdkey.is_file(),
        'server env file (--server-env-file)': server_env.is_file(),
    }
    if mode == 'player':
        checks['character .bic (--character)'] = character is not None and character.is_file()
    else:
        checks['DM avatar .bic (--dm-avatar)'] = dm_avatar.is_file()
        if character:
            raise LiveStackError('Use --character for a player run or --dm-avatar for a DM run, not both.')
    missing = [name for name, ok in checks.items() if not ok]
    if missing:
        raise LiveStackError('Missing or invalid: ' + '; '.join(missing))
    public_key = validate_public_cdkey(options.public_cdkey or os.environ.get('SWLOR_LIVE_PUBLIC_CDKEY', ''))
    extra_env = parse_server_env(options.server_env or [])
    settings_text = (SERVER_DOCKER_DIR / 'settings.tml').read_text('utf-8')
    if not re.search(r'\[masterserver\.key-authentication\]\s*mode\s*=\s*"if-reachable"', settings_text):
        raise LiveStackError('SWLOR.Game.Server/Docker/settings.tml no longer sets '
                             'masterserver.key-authentication mode = "if-reachable".')
    uuid = character_uuid(character) if mode == 'player' and not options.no_seed else None

    project = project_name(options.name)
    if any(project_resources(project).values()):
        raise LiveStackError(f'Compose project {project} already has Docker resources.')
    run_dir = Path(options.run_dir) if options.run_dir else Path(tempfile.gettempdir()) / 'swlor-live' / project
    run_dir = run_dir.resolve()
    for root in repository_roots():
        if is_within(run_dir, root):
            raise LiveStackError(f'The run directory must be outside the repository: {run_dir}')
    if run_dir.exists():
        # Only a directory this run creates exclusively is known to be unshared and to carry
        # no pre-existing access entries.
        raise LiveStackError(f'The run directory must not exist yet: {run_dir}')

    for name, value in (('--max-minutes', options.max_minutes), ('--server-timeout', options.server_timeout)):
        if not (math.isfinite(value) and value > 0):
            raise LiveStackError(f'{name} must be a finite number greater than 0.')
    max_seconds = int(options.max_minutes * 60)
    if not 300 <= max_seconds <= 12 * 3600:
        raise LiveStackError('--max-minutes must be between 5 and 720.')
    if not (math.isfinite(options.settle) and options.settle >= 0):
        raise LiveStackError('--settle must be a finite number of seconds, 0 or more.')
    client_settings_text = client_settings(options.player_name)
    state = {
        'project': project, 'run_dir': str(run_dir), 'mode': mode, 'player_name': options.player_name,
        'public_cdkey': public_key, 'created_utc': utc_now(), 'repo': str(REPO_ROOT),
        'server_image': server_image(), 'client_image': client_image_tag(), 'max_seconds': max_seconds,
        'cdkey_source': str(cdkey),
        'inputs': {'hak_dir': str(hak_dir), 'tlk_dir': str(tlk_dir), 'module': str(module),
                   'dotnet': str(dotnet), 'client_install': str(install),
                   'server_env_file': str(server_env), 'server_env_overrides': sorted(extra_env),
                   # Values can be secrets, so keep only a digest that tells runs apart.
                   'server_env_overrides_sha256': hashlib.sha256(
                       render_env_file(extra_env).encode('utf-8')).hexdigest()},
    }
    if mode == 'player':
        state['character'] = {'source': str(character), 'file': character.name.lower(), 'uuid': uuid}
    else:
        state['dm_avatar'] = {'source': str(dm_avatar), 'file': dm_avatar.name.lower()}

    reserve_project(project, run_dir, state['server_image'])
    try:
        # Exclusive creation claims the directory: a concurrent run choosing it fails here.
        run_dir.parent.mkdir(parents=True, exist_ok=True)
        run_dir.mkdir()
    except FileExistsError:
        release_project(project, run_dir)
        raise LiveStackError(f'The run directory was created by another run: {run_dir}') from None
    except BaseException:
        release_project(project, run_dir)
        raise
    try:
        # The run holds a CD key and a character: keep it private to the caller.
        make_private(run_dir, 0o700)
        for sub in ('home/logs', 'home/app_logs', 'profile/logs', 'profile/dmvault', 'evidence', 'bin'):
            (run_dir / sub).mkdir(parents=True, exist_ok=True)
        if os.name == 'posix':
            # prepare-server-home.sh hands these to UID 1000; keep them readable for a caller with
            # another UID and a strict umask. The 0700 run directory still keeps other users out.
            for sub in ('home', 'home/logs', 'home/app_logs'):
                os.chmod(run_dir / sub, 0o755)
        (run_dir / 'state.json').write_text(json.dumps(state, indent=2) + '\n', 'utf-8')
        stack = Stack(run_dir)
        for script in SCRIPTS:
            text = (PACKAGE_DIR / script).read_text('utf-8').replace('\r\n', '\n')
            (run_dir / 'bin' / script).write_bytes(text.encode('utf-8'))
        stack.state['inputs']['scripts_sha256'] = {script: sha256(run_dir / 'bin' / script) for script in SCRIPTS}
        shutil.copytree(dotnet, run_dir / 'dotnet')
        # The copy is deleted at teardown, so record what identifies the server code that ran.
        stack.state['inputs']['dotnet_sha256'] = build_digest(run_dir / 'dotnet')
        stack.state['inputs']['source_commit'] = source_commit()
        # The deploy home is shared; serve and hash a private copy so a repack cannot swap it.
        (run_dir / 'module').mkdir()
        shutil.copyfile(module, run_dir / 'module' / module.name)
        stack.state['inputs']['module_sha256'] = sha256(run_dir / 'module' / module.name)
        # The TLK is small enough to snapshot too. The haks (about 13 GB) stay shared and
        # read-only; down compares this manifest to detect a redeploy during the run.
        (run_dir / 'tlk').mkdir()
        for tlk in sorted(tlk_dir.glob('*.tlk')):
            shutil.copyfile(tlk, run_dir / 'tlk' / tlk.name)
        stack.state['inputs']['tlk_sha256'] = {
            path.name: sha256(path) for path in sorted((run_dir / 'tlk').glob('*.tlk'))}
        stack.state['inputs']['hak_manifest'] = hak_manifest(hak_dir)
        stack.state['inputs']['client_build'] = client_build(install)
        stack.save()
        (stack.home / 'settings.tml').write_text(settings_text, 'utf-8')
        stack.state['inputs']['server_settings_sha256'] = sha256(stack.home / 'settings.tml')
        shutil.copyfile(COMPOSE_FILE, stack.compose_file)
        stack.state['inputs']['compose_sha256'] = sha256(stack.compose_file)
        if mode == 'player':
            vault = stack.home / 'servervault' / public_key
            vault.mkdir(parents=True)
            shutil.copyfile(character, vault / character.name.lower())
            make_private(vault / character.name.lower(), 0o600)
            stack.state['character']['sha256'] = sha256(vault / character.name.lower())
        else:
            shutil.copyfile(dm_avatar, stack.profile / 'dmvault' / dm_avatar.name.lower())
            make_private(stack.profile / 'dmvault' / dm_avatar.name.lower(), 0o600)
            stack.state['dm_avatar']['sha256'] = sha256(stack.profile / 'dmvault' / dm_avatar.name.lower())
        # The base env file may hold secrets: run from a private copy, keep only its digest.
        shutil.copyfile(server_env, run_dir / 'server.env')
        make_private(run_dir / 'server.env', 0o600)
        stack.state['inputs']['server_env_file_sha256'] = sha256(run_dir / 'server.env')
        stack.save()
        (stack.profile / 'nwn.ini').write_text(CLIENT_ALIASES, 'utf-8')
        (stack.profile / 'nwnplayer.ini').write_text(CLIENT_PLAYER_INI, 'utf-8')
        (stack.profile / 'settings.tml').write_text(client_settings_text, 'utf-8')
        (run_dir / 'server-extra.env').write_text(render_env_file(extra_env), 'utf-8')
        (run_dir / 'compose.env').write_text(render_env_file({
            'SWLOR_LIVE_RUN_DIR': run_dir.as_posix(),
            'SWLOR_LIVE_SERVER_IMAGE': state['server_image'],
            'SWLOR_LIVE_CLIENT_IMAGE': state['client_image'],
            'SWLOR_LIVE_PREPARE_HOME': (SERVER_DOCKER_DIR / 'prepare-server-home.sh').as_posix(),
            'SWLOR_LIVE_SERVER_ENV_FILE': (run_dir / 'server.env').as_posix(),
            'SWLOR_LIVE_SERVER_NAME': f'SWLOR live check {project}',
            'SWLOR_LIVE_MODULE_DIR': (run_dir / 'module').as_posix(),
            'SWLOR_LIVE_MODULE_NAME': module.stem,
            'SWLOR_LIVE_HAK_DIR': hak_dir.resolve().as_posix(),
            'SWLOR_LIVE_TLK_DIR': (run_dir / 'tlk').as_posix(),
            'SWLOR_LIVE_CLIENT_INSTALL': install.resolve().as_posix(),
            'SWLOR_LIVE_SERVER_PORT': str(SERVER_PORT),
            'SWLOR_LIVE_CLIENT_MODE': mode,
            'SWLOR_LIVE_CLIENT_USER': host_user(),
            # A throwaway password for this unpublished server; it leaves with compose.env.
            'SWLOR_LIVE_DM_PASSWORD': secrets.token_hex(12) if mode == 'dm' else '',
            'SWLOR_LIVE_ADMIN_CDKEY': public_key if mode == 'dm' else '',
            'SWLOR_LIVE_MAX_SECONDS': str(max_seconds),
        }), 'utf-8')
    except BaseException:
        try:
            remove_tree(run_dir)
        except OSError as cleanup_error:
            log(f'Could not remove {run_dir} after the failed preparation: {cleanup_error}')
        finally:
            try:
                released = release_project(project, run_dir)
            except LiveStackError as release_error:
                log(str(release_error))
                released = False
            if not released:
                log(f'Could not release the {reservation_name(project)} reservation; remove it with docker rm.')
        raise
    log(f'Run directory {run_dir} (project {project})')
    return stack


def client_build(install: Path) -> dict[str, list[int]]:
    """Size and modification time of the client binary and its build stamps, to detect an update."""
    files = (install / 'bin' / 'linux-x86' / 'nwmain-linux', install / 'bin' / 'linux-x86' / 'build.txt',
             install / 'databuild.txt')
    return {path.relative_to(install).as_posix(): [path.stat().st_size, path.stat().st_mtime_ns]
            for path in files if path.is_file()}


def reservation_name(project: str) -> str:
    return project + '-reservation'


def reserve_project(project: str, run_dir: Path, image: str) -> None:
    """Claim the project name daemon-wide, for every user and temp folder sharing this Docker.

    The claim is a created, never-started container: the daemon reserves container names
    atomically, so a second run asking for the same name is refused. It uses no CPU or memory.
    """
    result = run(['docker', 'create', '--name', reservation_name(project), '--network', 'none',
                  '--label', f'swlor.live.run_dir={run_dir}', '--entrypoint', 'true', image],
                 check=False, capture=True, timeout=120)
    if result.returncode != 0:
        holder = reservation_holder(project)
        if not holder:
            raise LiveStackError(f'Could not reserve project {project}: '
                                 + result.stderr.decode('utf-8', 'replace').strip())
        raise LiveStackError(f'Project {project} is reserved by run {holder}. Choose another --name, or run '
                             f'down on that run (docker rm {reservation_name(project)} only once it is gone).')


def reservation(project: str) -> tuple[str, str] | None:
    """The reservation container's immutable ID and recorded run directory, or None."""
    owner = run(['docker', 'container', 'inspect', '-f', '{{.Id}} {{index .Config.Labels "swlor.live.run_dir"}}',
                 reservation_name(project)], check=False, capture=True, timeout=60)
    if owner.returncode != 0:
        # Only Docker's own not-found answer means there is no reservation; anything else
        # (an unreachable daemon, a permission error) must not be read as absence.
        if b'No such' in owner.stderr:
            return None
        raise LiveStackError(f'Could not inspect the {reservation_name(project)} reservation: '
                             + owner.stderr.decode('utf-8', 'replace').strip())
    container_id, _, run_dir = owner.stdout.decode('utf-8', 'replace').strip().partition(' ')
    return container_id, run_dir


def reservation_holder(project: str) -> str | None:
    """The run directory recorded on the project's reservation, or None when there is none."""
    found = reservation(project)
    return found[1] or None if found else None


def same_path(first: str, second: str) -> bool:
    return os.path.normcase(first) == os.path.normcase(second)


def release_project(project: str, run_dir: Path) -> bool:
    """Remove this run's reservation by ID; True when no reservation of this run remains.

    Removing by ID after checking the owner means a reservation another run created under
    the same name in the meantime is never removed.
    """
    found = reservation(project)
    if found is None:
        return True
    container_id, holder = found
    if not same_path(holder, str(run_dir)):
        return True
    removal = run(['docker', 'rm', container_id], check=False, capture=True, timeout=60)
    # Only a completed removal or Docker's own "no such container" proves it is gone; an
    # unreachable daemon also fails, and must not be read as success.
    return removal.returncode == 0 or b'No such container' in removal.stderr


def build_digest(directory: Path) -> str:
    """One SHA-256 over every file's relative path and content, identifying the server build that ran."""
    digest = hashlib.sha256()
    for path in sorted(p for p in directory.rglob('*') if p.is_file()):
        digest.update(path.relative_to(directory).as_posix().encode('utf-8') + b'\0')
        digest.update(sha256(path).encode('ascii') + b'\n')
    return digest.hexdigest()


def source_commit() -> str:
    """The checkout's HEAD, marked when it has uncommitted changes."""
    try:
        head = output(['git', '-C', str(REPO_ROOT), 'rev-parse', 'HEAD'], timeout=60)
        # Untracked sources count: the SDK's default globs compile them into the build.
        dirty = output(['git', '-C', str(REPO_ROOT), 'status', '--porcelain'], timeout=120)
    except LiveStackError:
        return 'unknown'
    return head + (' (uncommitted changes)' if dirty else '')


def hak_manifest(hak_dir: Path) -> dict[str, list[int]]:
    """Size and modification time of every file in the shared hak folder (including .md5 sidecars)."""
    return {path.name: [path.stat().st_size, path.stat().st_mtime_ns]
            for path in sorted(hak_dir.iterdir()) if path.is_file()}


def start(stack: Stack, server_timeout: float) -> None:
    """Start Redis and the server, seed the Player record, then start the client."""
    ensure_client_image(stack.state['client_image'])
    # The reservation is a stopped container, so a prune during preparation could free the
    # name for another run: confirm it is still this run's before touching the project.
    stack.ensure_owner()
    log('Starting Redis and the server')
    stack.compose('up', '-d', '--no-build', timeout=900)
    record_image_ids(stack, ('redis', 'swlor-server'))
    wait_for_server(stack, server_timeout)
    uuid = stack.state.get('character', {}).get('uuid')
    if uuid:
        seed_player(stack, uuid)
    stack.ensure_owner()
    start_client(stack, Path(stack.state['cdkey_source']))
    record_image_ids(stack, ('client',))
    # The module-load marker comes before the rest of start-up, so confirm the server survived it.
    if not stack.running('swlor-server'):
        raise LiveStackError('The server container stopped after loading the module. See the server logs.')


def record_image_ids(stack: Stack, services: tuple[str, ...]) -> None:
    """Keep the image ID each service container was created from.

    A tag such as redismod:latest can be retagged or pulled by another session at any time,
    so read the immutable image of the container itself rather than the current tag.
    """
    ids = stack.state.setdefault('image_ids', {})
    for service in services:
        container = stack.compose('ps', '-a', '-q', service, client=service == 'client', capture=True,
                                  timeout=60).stdout.decode('utf-8', 'replace').split()
        if container:
            ids[service] = output(['docker', 'container', 'inspect', '--format', '{{.Image}}', container[0]],
                                  timeout=60)
    stack.save()


def ensure_client_image(tag: str) -> None:
    if run(['docker', 'image', 'inspect', tag], check=False, capture=True, timeout=60).returncode == 0:
        return
    log(f'Building {tag}')
    # The Dockerfile needs no build context, so nothing from the checkout is uploaded.
    run(['docker', 'build', '-t', tag, '-'], stdin=(PACKAGE_DIR / 'Dockerfile.client').read_bytes(),
        timeout=1800)


def wait_for_server(stack: Stack, timeout: float) -> None:
    """Wait for SWLOR's OnModuleLoad "Server is starting up." line."""
    log('Waiting for the module to load')
    deadline = time.monotonic() + timeout
    server_logs = stack.home / 'app_logs' / 'Server'
    while time.monotonic() < deadline:
        for path in server_logs.glob('Server_*.log'):
            if 'Server is starting up.' in path.read_text('utf-8', 'replace'):
                log('Server module loaded')
                return
        if not stack.running('swlor-server'):
            raise LiveStackError('The server container stopped during start-up. '
                                 f'See {stack.home / "logs"} and {stack.home / "app_logs" / "Error"}.')
        time.sleep(5)
    raise LiveStackError(f'The server did not finish loading within {timeout:.0f}s.')


def seed_player(stack: Stack, uuid: str) -> None:
    """Create the Player record a fresh database needs before a saved character may enter.

    PlayerInitialization boots a character with neither an initialized record nor the
    creation intent that PlayerCreation records, so seed that intent: the server then
    runs normal first-login setup on this run's copy of the character.
    """
    record = json.dumps({'Id': uuid, 'EntityType': 'Player', 'DateCreated': utc_now(),
                         'CharacterInitializationPending': True}, separators=(',', ':'))
    result = stack.compose('exec', '-T', 'redis', 'redis-cli', 'JSON.SET', f'Player:{uuid}', '.', record, 'NX',
                           capture=True, timeout=60)
    created = result.stdout.decode('utf-8', 'replace').strip() == 'OK'
    log(f'Player record {uuid} ' + ('seeded' if created else 'already existed'))


def start_client(stack: Stack, cdkey: Path) -> None:
    target = stack.profile / 'cdkey.ini'
    target.unlink(missing_ok=True)
    # Create the copy owner-only before writing the key into it.
    descriptor = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, 'wb') as copy:
        copy.write(cdkey.read_bytes())
    log('Starting the client')
    stack.compose('up', '-d', '--no-deps', '--no-build', 'client', client=True, timeout=300)
    stack.state['client_started_utc'] = utc_now()
    stack.save()
    stack.ui('wait-window', '180', timeout=240)
    log('Client window is up')


def module_entries(stack: Stack) -> int:
    """Characters that entered the module, from SWLOR's OnModuleEnter audit line (players and DMs)."""
    return sum(count_matches(path, ENTRY_PATTERN) for path in (stack.home / 'app_logs' / 'Connection').glob('*.log'))


def loading_tips(stack: Stack) -> int:
    """SWLOR's loading-screen tip, written to the chat log when an area transition starts."""
    return count_matches(stack.profile / 'logs' / 'nwclientLog1.txt', LOADING_TIP_PATTERN)


def play(stack: Stack, settle: float, timeout: float = 600) -> None:
    """Click Play until the character enters, then wait for area transitions to settle.

    The server's "Joined as" line is written at connection, before character select,
    and character select can take minutes to appear while the client loads the haks.
    Clicking Play before then does nothing, so click every 20 seconds until the entry.
    """
    if not (math.isfinite(settle) and settle >= 0):
        raise LiveStackError('--settle must be a finite number of seconds, 0 or more.')
    entered = module_entries(stack)
    deadline = time.monotonic() + timeout
    next_click = 0.0
    attempt = 0
    while module_entries(stack) <= entered:
        now = time.monotonic()
        if now >= deadline:
            raise LiveStackError(f'The character did not enter within {timeout:.0f}s. '
                                 'Capture the screen to see where the client is.')
        if now >= next_click:
            attempt += 1
            log(f'Clicking Play (attempt {attempt})')
            if stack.state['mode'] == 'dm':
                stack.ui('click', str(FIRST_CHARACTER_ROW[0]), str(FIRST_CHARACTER_ROW[1]))
            stack.ui('click', str(PLAY_BUTTON[0]), str(PLAY_BUTTON[1]))
            next_click = time.monotonic() + 20
        time.sleep(1)
    log(f'Character entered; waiting until no area transition has started for {settle:.0f}s')
    # A new character is set up and may be moved to its starting area right away.
    tips = loading_tips(stack)
    quiet_since = time.monotonic()
    # Settling has its own window, so a late entry still gets the full quiet period.
    deadline = quiet_since + settle + timeout
    while True:
        # An OnModuleEnter handler can boot the character right after the entry is logged.
        if stack.joined_players() <= 0:
            raise LiveStackError('The character entered but was disconnected. See the server and app logs.')
        # A crashed server cannot log the leave, so check both containers as well.
        for service in ('swlor-server', 'client'):
            if not stack.running(service):
                raise LiveStackError(f'The {service} container stopped while the area was settling.')
        if time.monotonic() - quiet_since >= settle:
            break
        if time.monotonic() >= deadline:
            raise LiveStackError('Area transitions did not settle.')
        current = loading_tips(stack)
        if current != tips:
            tips, quiet_since = current, time.monotonic()
        time.sleep(1)
    log('Settled. Inspect each capture: a slow first render can still show the loading screen.')


def capture(stack: Stack, label: str) -> Path:
    stack.ui('capture', label)
    path = stack.evidence / f'{label}.png'
    if not path.is_file():
        raise LiveStackError(f'The capture {path} was not written.')
    digest = sha256(path)
    # A durable record next to the images, so a replaced PNG can be told from the original.
    with (stack.evidence / 'captures.jsonl').open('a', encoding='utf-8') as manifest:
        manifest.write(json.dumps({'label': label, 'file': path.name, 'utc': utc_now(), 'sha256': digest}) + '\n')
    log(f'{path} sha256={digest}')
    return path


def boot_players(stack: Stack, timeout: float = 30) -> bool:
    """Kick the player through the server console so the CD key is released cleanly."""
    if stack.joined_players() <= 0:
        return True
    if not stack.running('swlor-server'):
        # A server that died with the player joined cannot release the CD key cleanly.
        log('The server is not running, so the player could not be booted.')
        return False
    stack.console(f'kick {stack.state["player_name"]}')
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if stack.joined_players() <= 0:
            log('Player booted')
            return True
        time.sleep(1)
    log('The server did not log the player leaving after the kick.')
    return False


def remove_tree(path: Path) -> None:
    if not path.exists():
        return

    def retry(function, target, _):
        os.chmod(target, 0o700)
        function(target)

    shutil.rmtree(path, onexc=retry)


def make_private(path: Path, mode: int) -> None:
    """Owner-only access: POSIX mode bits, or a non-inherited Windows ACL on a directory.

    The Windows ACL grants only the caller, SYSTEM and Administrators (the same principals
    as a user profile), so a --run-dir under a shared folder such as C:\\Temp is private too.
    Files created inside inherit it, and Docker Desktop bind mounts still work.
    """
    if os.name == 'posix':
        os.chmod(path, mode)
    elif path.is_dir():
        user_sid = next(csv.reader(io.StringIO(output(['whoami', '/user', '/fo', 'csv', '/nh']))))[1]
        run(['icacls', str(path), '/inheritance:r', '/grant:r', f'*{user_sid}:(OI)(CI)F',
             '*S-1-5-18:(OI)(CI)F', '*S-1-5-32-544:(OI)(CI)F'], capture=True, timeout=60)


def host_user() -> str:
    """The caller's UID:GID on Linux and macOS hosts; Docker Desktop on Windows maps any UID."""
    return f'{os.getuid()}:{os.getgid()}' if hasattr(os, 'getuid') else '1000:1000'


def return_home_ownership(stack: Stack) -> None:
    """Give the server home back to the caller before removal.

    prepare-server-home.sh hands the home to the server's UID 1000. A Linux caller with
    another UID could not delete what the server wrote there. Windows needs no change.
    """
    if not hasattr(os, 'getuid') or not stack.home.is_dir():
        return
    run(['docker', 'run', '--rm', '--network', 'none', '--user', '0:0', '--entrypoint', 'chown',
         '-v', f'{stack.home}:/home-to-return', stack.state['server_image'], '-R', host_user(),
         '/home-to-return'], capture=True, timeout=300)


def unchanged(current, recorded) -> bool:
    """Compare a recorded manifest with a fresh one; a read error mid-scan counts as a change."""
    if recorded is None:
        return True
    try:
        return current() == recorded
    except OSError:
        return False


def destroy(stack: Stack) -> dict:
    """Boot the player, remove the project's Docker resources, then the key, profile and home."""
    # The reservation is released only once this run's Docker resources are gone, so a rerun
    # that finds none must leave Docker alone: anything under the name now belongs to another run.
    owns_project = stack.ensure_owner(require_reservation=False)
    report = {'project': stack.project, 'utc': utc_now(), 'player_booted': True}
    # Evidence lost by an earlier teardown cannot be recovered; keep reporting it.
    lost_evidence = stack.state.setdefault('lost_evidence', [])
    errors = list(lost_evidence)
    if owns_project:
        try:
            report['player_booted'] = boot_players(stack)
        except LiveStackError as error:
            report['player_booted'] = False
            errors.append(str(error))
        if not report['player_booted']:
            errors.append('The player was not confirmed booted; the CD key may stay held for 60 seconds.')
        if (stack.run_dir / 'compose.env').is_file():
            stack.compose('stop', '-t', '30', 'client', client=True, check=False, capture=True, timeout=120)
            result = stack.compose('down', '--volumes', '--remove-orphans', '--timeout', '60', client=True,
                                   check=False, capture=True, timeout=600)
            if result.returncode != 0:
                errors.append('compose down failed: ' + result.stderr.decode('utf-8', 'replace').strip())

    (stack.profile / 'cdkey.ini').unlink(missing_ok=True)
    if project_resources(stack.project)['containers']:
        # A surviving container may still use the bind mounts; keep them, and compose.env so down can be rerun.
        if owns_project:
            errors.append('Containers remain after compose down; the profile, home and build were kept. '
                          'Fix the containers, then run down again.')
        else:
            # The reservation is gone (for example pruned with stopped containers), so ownership of
            # these containers cannot be proven: leave them and their possible mounts alone.
            errors.append(f'Project {stack.project} has containers but no reservation; they were not touched '
                          'and the profile, home and build were kept. Confirm they belong to this run, remove '
                          f'them with: {stack.compose_command("down", "--volumes", "--remove-orphans")} '
                          '-- then run down again.')
    else:
        try:
            return_home_ownership(stack)
        except LiveStackError as error:
            errors.append(str(error))
        for source, name in ((stack.profile / 'logs', 'client-logs'), (stack.home / 'logs', 'server-logs'),
                             (stack.home / 'app_logs', 'server-app-logs')):
            try:
                if source.is_dir() and any(source.iterdir()):
                    shutil.copytree(source, stack.evidence / name, dirs_exist_ok=True)
            except (OSError, shutil.Error) as error:
                lost_evidence.append(f'Could not keep {name} in the evidence: {error}')
                errors.append(lost_evidence[-1])
        for path in (stack.profile, stack.home, stack.run_dir / 'dotnet', stack.run_dir / 'module',
                     stack.run_dir / 'tlk', stack.run_dir / 'bin'):
            try:
                remove_tree(path)
            except OSError as error:
                errors.append(f'Could not remove {path}: {error}')

    # The shared inputs are judged once, at the first teardown; a rerun must not hold a later
    # deployment against a run that had already ended.
    checks = stack.state.get('end_of_run_checks')
    if checks is None:
        checks = {
            'hak_folder_unchanged': unchanged(lambda: hak_manifest(Path(stack.state['inputs']['hak_dir'])),
                                              stack.state['inputs'].get('hak_manifest')),
            'client_build_unchanged': unchanged(
                lambda: client_build(Path(stack.state['inputs']['client_install'])),
                stack.state['inputs'].get('client_build')),
        }
        # The run has ended only once no project container remains; until then keep rechecking.
        if not project_resources(stack.project)['containers']:
            stack.state['end_of_run_checks'] = checks
    report.update(checks)
    if not checks['hak_folder_unchanged']:
        errors.append('The shared hak folder changed during the run; captures may mix asset versions. Recapture.')
    if not checks['client_build_unchanged']:
        errors.append('The client install changed during the run (for example a Steam update). Recapture.')

    resources = project_resources(stack.project)
    report['reservation_removed'] = not owns_project
    if not any(resources.values()):
        # Kept while any project resource remains so down can be rerun.
        for name in ('compose.env', 'compose.yml', 'server-extra.env', 'server.env'):
            (stack.run_dir / name).unlink(missing_ok=True)
        if owns_project:
            report['reservation_removed'] = release_project(stack.project, stack.run_dir)
            if not report['reservation_removed']:
                errors.append(f'Could not remove the {reservation_name(stack.project)} reservation container.')
    leftovers = [str(path) for pattern in ('cdkey.ini', '*.bic') for path in stack.run_dir.rglob(pattern)]
    report.update({
        'remaining_containers': len(resources['containers']),
        'remaining_volumes': len(resources['volumes']),
        'remaining_networks': len(resources['networks']),
        'profile_removed': not stack.profile.exists(),
        'home_removed': not stack.home.exists(),
        'key_and_character_copies_remaining': leftovers,
        'errors': errors,
    })
    report['clean'] = (not errors and not any(resources.values()) and not leftovers
                       and report['profile_removed'] and report['home_removed'] and report['reservation_removed'])
    (stack.evidence / 'cleanup.json').write_text(json.dumps(report, indent=2) + '\n', 'utf-8')
    stack.state['destroyed_utc'] = report['utc']
    stack.save()
    return report
