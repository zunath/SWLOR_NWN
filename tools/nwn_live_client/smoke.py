"""Scripted live smoke run: boot this checkout's server, log a client in, walk the core player loop.

Built on stack.py (the same Docker stack as tools/NwnLiveClient.py), so the isolation rules
of SWLOR.Game.Server/Readmes/InEngineVisualVerification.md apply unchanged: a unique Compose
project, no published host ports, a private profile with a copied cdkey.ini, teardown that
removes every container, key and character copy.

Each scenario step records a status line, a short detail, named checks and, where a client
is attached, a screenshot. results.json holds everything a rerun must reproduce; results.md
is the same for people. Two runs "have the same results" when compare_results() finds no
difference in their step statuses and check outcomes (timings and paths are not compared).
"""
from __future__ import annotations

import argparse
import datetime
import json
import secrets
import subprocess
import sys
import tempfile
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

from . import smoke_steps
from . import stack as live
from .__main__ import parser as live_parser
from .smoke_client import GameClient
from .stack import LiveStackError, Stack

RESULTS_SCHEMA = 1

PASS = 'pass'
FAIL = 'fail'
# Cannot be automated reliably; the reason says what a person does instead.
MANUAL = 'manual'
# An earlier step failed, so this one did not run.
SKIPPED = 'skipped'
# Needs something this run did not have (a client, a key); the detail names it.
BLOCKED = 'blocked'
STATUSES = (PASS, FAIL, MANUAL, SKIPPED, BLOCKED)

# A server-only run never reads a CD key and needs no client install: it passes the stack these
# obvious placeholders, which satisfy its prerequisite checks and are never used.
PLACEHOLDER_PUBLIC_CDKEY = '00000000'
PLACEHOLDER_CDKEY_TEXT = 'Placeholder for a server-only smoke run; this is not a CD key.\n'
PLACEHOLDER_CLIENT_FOLDER = 'placeholder-client-install'

DEFAULT_RESULTS_FOLDER = 'swlor-smoke'
# The server environment of the smoke stack. A test environment makes the staff chat commands
# the in-game steps use (/tpwp, /spawnitem) available to every player, while player commands
# such as /perks keep working; no staff record has to be seeded into the database.
SMOKE_ENVIRONMENT = 'test'


@dataclass
class Check:
    """One named observation inside a step. Only name and passed take part in run comparison."""
    name: str
    passed: bool
    detail: str = ''


@dataclass
class StepResult:
    id: str
    title: str
    status: str = BLOCKED
    detail: str = ''
    checks: list[Check] = field(default_factory=list)
    screenshots: list[str] = field(default_factory=list)
    seconds: float = 0.0

    def check(self, name: str, passed: bool, detail: str = '') -> bool:
        self.checks.append(Check(name, bool(passed), detail))
        return bool(passed)

    @property
    def checks_passed(self) -> bool:
        return all(check.passed for check in self.checks)


@dataclass(frozen=True)
class StepDefinition:
    id: str
    title: str
    # Why the step is recorded as manual when it has no automation, or None when it is automated.
    manual_reason: str | None = None
    # True when the step needs the client; a server-only run records it as blocked.
    needs_client: bool = False


# The scenario of MaintainabilityRefactorSpecs.md "P0-6", in order. A step with a manual_reason
# is recorded as manual with that reason and is not attempted; every step today is automated.
SCENARIO = (
    StepDefinition('boot', 'Server boots from this build; the client reaches the module'),
    StepDefinition('login', 'An existing test character logs in', needs_client=True),
    StepDefinition('combat', 'Ranged and melee attacks against spawned targets; damage numbers appear',
                   needs_client=True),
    StepDefinition('ability', 'Use one active ability with a cooldown', needs_client=True),
    StepDefinition('storage', 'Open the bank and move an item in and out', needs_client=True),
    StepDefinition('craft', 'Craft one item at a crafting station', needs_client=True),
    StepDefinition('quest', 'Advance one quest step (accept a simple quest)', needs_client=True),
    StepDefinition('transfer', 'Transfer to another area and back', needs_client=True),
    StepDefinition('logout', 'Log out cleanly', needs_client=True),
)


@dataclass
class RunResults:
    label: str
    mode: str
    started_utc: str
    steps: list[StepResult]
    commit: str = ''
    build_revision: str = ''
    module_sha256: str = ''
    server_image: str = ''
    character: str = ''
    finished_utc: str = ''
    seconds: float = 0.0
    teardown: dict = field(default_factory=dict)
    error: str = ''

    @property
    def complete(self) -> bool:
        """True when every scenario step ran and passed and the run ended cleanly.

        Manual and blocked steps are not complete, and neither is a run whose setup failed
        before any step was recorded.
        """
        return (not self.error and self.teardown_clean
                and [step.id for step in self.steps] == [definition.id for definition in SCENARIO]
                and all(step.status == PASS for step in self.steps))

    @property
    def teardown_clean(self) -> bool:
        return bool(self.teardown.get('clean'))

    @property
    def failed(self) -> bool:
        return any(step.status == FAIL for step in self.steps) or bool(self.error) or not self.teardown_clean


# ---------------------------------------------------------------- pure helpers

def utc_now() -> str:
    return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec='seconds')


def run_label(base: str, number: int, total: int) -> str:
    """The Compose-safe label of one run: the base, plus the run number when several run in a row."""
    return base if total == 1 else f'{base}-{number}'


def screenshot_label(step_id: str, name: str) -> str:
    """A capture label that sorts in scenario order, for example "02-login-in-area"."""
    index = [definition.id for definition in SCENARIO].index(step_id) + 1
    return f'{index:02d}-{step_id}-{name}'


def results_to_json(results: RunResults) -> dict:
    return {
        'schema': RESULTS_SCHEMA,
        'label': results.label, 'mode': results.mode, 'complete': results.complete,
        'started_utc': results.started_utc, 'finished_utc': results.finished_utc, 'seconds': results.seconds,
        'commit': results.commit, 'build_revision': results.build_revision,
        'module_sha256': results.module_sha256, 'server_image': results.server_image,
        'character': results.character, 'error': results.error,
        'steps': [{
            'id': step.id, 'title': step.title, 'status': step.status, 'detail': step.detail,
            'seconds': round(step.seconds, 1), 'screenshots': step.screenshots,
            'checks': [{'name': check.name, 'passed': check.passed, 'detail': check.detail}
                       for check in step.checks],
        } for step in results.steps],
        'teardown': results.teardown,
    }


def results_to_markdown(results: RunResults) -> str:
    lines = [f'# Smoke run {results.label}', '',
             f'- Mode: {results.mode}; complete: {"yes" if results.complete else "no"}',
             f'- Started {results.started_utc}, finished {results.finished_utc} ({results.seconds:.0f} s)',
             f'- Branch commit {results.commit or "unknown"}; server build revision '
             f'{results.build_revision or "unknown"}',
             f'- Module SHA-256 {results.module_sha256 or "unknown"}',
             f'- Teardown clean: {"yes" if results.teardown_clean else "NO"}']
    if results.error:
        lines.append(f'- Run error: {results.error}')
    lines += ['', '| # | Step | Status | Detail |', '|---|---|---|---|']
    for number, step in enumerate(results.steps, 1):
        lines.append(f'| {number} | {step.title} | {step.status.upper()} | {step.detail} |')
    for step in results.steps:
        if not step.checks and not step.screenshots:
            continue
        lines += ['', f'## {step.title}']
        lines += [f'- {"pass" if check.passed else "FAIL"}: {check.name}'
                  + (f' ({check.detail})' if check.detail else '') for check in step.checks]
        lines += [f'- screenshot: {path}' for path in step.screenshots]
    return '\n'.join(lines) + '\n'


def comparable(results: dict) -> dict:
    """The part of a results.json that two runs of the same scenario must share, by step id."""
    return {step['id']: (step['status'], [(check['name'], check['passed']) for check in step['checks']])
            for step in results['steps']}


def compare_results(first: dict, second: dict) -> list[str]:
    """Differences between two results.json documents; empty when the runs agree."""
    differences = []
    if first.get('mode') != second.get('mode'):
        differences.append(f'mode: {first.get("mode")} != {second.get("mode")}')
    left, right = comparable(first), comparable(second)
    for step in [definition.id for definition in SCENARIO]:
        if step not in left or step not in right:
            if step in left or step in right:
                differences.append(f'{step}: missing from one run')
            continue
        if left[step][0] != right[step][0]:
            differences.append(f'{step}: status {left[step][0]} != {right[step][0]}')
        elif left[step][1] != right[step][1]:
            differences.append(f'{step}: checks differ ({left[step][1]} != {right[step][1]})')
    return differences


def error_logs(home: Path) -> list[str]:
    """Names of the server's error log files under a stack home; none when nothing was logged."""
    folder = home / 'app_logs' / 'Error'
    return sorted(path.name for path in folder.glob('*.log')) if folder.is_dir() else []


# What the server build compiles: this project, the API project it references and the shared
# MSBuild files. Anything else in the checkout (tools, module data, guides) cannot change the DLL.
BUILD_INPUT_FOLDERS = ('SWLOR.Game.Server/', 'SWLOR.NWN.API/')
BUILD_INPUT_FILES = ('Directory.Build.props', 'Directory.Build.targets', 'global.json')


def uncommitted_paths(status: bytes) -> list[str]:
    """Paths from "git status --porcelain=v1 -z" output, with a rename's old path included."""
    entries = status.decode('utf-8', 'replace').split('\0')
    paths, index = [], 0
    while index < len(entries):
        entry = entries[index]
        index += 1
        if len(entry) < 4:
            continue
        paths.append(entry[3:])
        # A rename or copy is followed by its source path as a separate entry.
        if entry[0] in 'RC' or entry[1] in 'RC':
            if index < len(entries) and entries[index]:
                paths.append(entries[index])
            index += 1
    return paths


def is_build_input(path: str) -> bool:
    if path.lower().endswith('.md'):
        return False
    return path.startswith(BUILD_INPUT_FOLDERS) or path in BUILD_INPUT_FILES


def stale_build_inputs(repo: Path, assembly: Path) -> list[str]:
    """Uncommitted server build inputs that the assembly may not contain.

    The assembly's revision stamp is the commit it was built at, so edits made after that
    commit still carry the current head. An edit is covered only when the assembly is newer
    than the file; a deleted file is always reported, since its time of removal is unknown.
    """
    status = subprocess.run(['git', '-C', str(repo), 'status', '--porcelain=v1', '-z', '--untracked-files=all'],
                            capture_output=True, check=True, timeout=120).stdout
    built = assembly.stat().st_mtime_ns
    stale = []
    for path in uncommitted_paths(status):
        if not is_build_input(path):
            continue
        target = repo / path
        if not target.exists():
            stale.append(f'{path} (deleted)')
        elif target.stat().st_mtime_ns > built:
            stale.append(f'{path} (newer than the build)')
    return sorted(stale)


def relative_or_absolute(path: Path, base: Path) -> str:
    """A path relative to base with forward slashes, or the absolute path when it lies elsewhere."""
    try:
        return path.relative_to(base).as_posix()
    except ValueError:
        return str(path)


def require_pillow() -> None:
    """Fail early, with the install command, when the screenshot analysis cannot import Pillow."""
    try:
        __import__('PIL')
    except ImportError:
        raise LiveStackError('The smoke run reads screenshots with Pillow, which is not installed. Install it '
                             'with: python -m pip install -r tools/nwn_visual_check/requirements.txt') from None


def head_commit(repo: Path) -> str:
    try:
        return subprocess.run(['git', '-C', str(repo), 'rev-parse', 'HEAD'], capture_output=True, text=True,
                              check=True, timeout=60).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        return ''


def default_results_root() -> Path:
    return Path(tempfile.gettempdir()) / DEFAULT_RESULTS_FOLDER


def default_character() -> Path | None:
    """The established local test character, when this machine has it."""
    candidate = Path.home() / 'Documents' / 'Neverwinter Nights' / 'localvault' / 'agantarn.bic'
    return candidate if candidate.is_file() else None


# ---------------------------------------------------------------- run

class SmokeRun:
    """One smoke run: a stack, the scenario's steps and the results written next to the run directory."""

    def __init__(self, options: argparse.Namespace, label: str, results_dir: Path):
        self.options = options
        self.results_dir = results_dir
        self.server_only = options.server_only
        self.results = RunResults(label=label, mode='server-only' if self.server_only else 'full',
                                  started_utc=utc_now(), steps=[], commit=head_commit(live.REPO_ROOT))
        self.stack: Stack | None = None
        self.client: GameClient | None = None
        # Appended to screenshot labels; attach mode sets it so a rerun never reuses a label.
        self.label_suffix = ''
        # True when the stack could not be prepared, so no scenario step ran (exit code 2).
        self.setup_failed = False
        self._clock = time.monotonic()

    # -- stack ------------------------------------------------------

    def up_options(self) -> argparse.Namespace:
        """The "up" arguments of NwnLiveClient.py, built by its own parser so they cannot drift."""
        options = self.options
        arguments = ['up', '--name', self.results.label, '--run-dir', str(self.results_dir / 'run'),
                     '--configuration', options.configuration, '--server-timeout', str(options.server_timeout),
                     '--max-minutes', str(options.max_minutes), '--settle', str(options.settle),
                     '--server-env', f'SWLOR_ENVIRONMENT={SMOKE_ENVIRONMENT}']
        for flag, value in (('--character', options.character), ('--public-cdkey', options.public_cdkey),
                            ('--cdkey', options.cdkey), ('--dotnet', options.dotnet), ('--module', options.module),
                            ('--hak-dir', options.hak_dir), ('--tlk-dir', options.tlk_dir),
                            ('--client-install', options.client_install)):
            if value:
                arguments += [flag, str(value)]
        return live_parser().parse_args(arguments)

    def create_stack(self) -> Stack:
        up = self.up_options()
        if self.server_only:
            # No client ever starts, so no CD key is read: placeholders satisfy the tool's checks.
            placeholder = self.results_dir / 'placeholder-cdkey.ini'
            placeholder.write_text(PLACEHOLDER_CDKEY_TEXT, 'utf-8')
            up.cdkey = str(placeholder)
            up.public_cdkey = up.public_cdkey or PLACEHOLDER_PUBLIC_CDKEY
            if not up.client_install:
                # The tool checks for the Linux client binary even though no client container starts.
                binary = self.results_dir / PLACEHOLDER_CLIENT_FOLDER / 'bin' / 'linux-x86' / 'nwmain-linux'
                binary.parent.mkdir(parents=True, exist_ok=True)
                binary.write_text('placeholder for a server-only smoke run\n', 'utf-8')
                up.client_install = str(binary.parents[2])
        stack = live.create(up)
        self.stack = stack
        self.client = GameClient(stack)
        state = stack.state
        self.results.build_revision = state['inputs'].get('build_source_revision', '')
        self.results.module_sha256 = state['inputs'].get('module_sha256', '')
        self.results.server_image = state.get('server_image', '')
        self.results.character = state.get('character', {}).get('file', '')
        return stack

    def redis(self, *arguments: str) -> str:
        assert self.stack is not None
        # -e makes redis-cli exit non-zero on an error reply, so compose() raises for it.
        result = self.stack.compose('exec', '-T', 'redis', 'redis-cli', '-e', *arguments, capture=True, timeout=60)
        return result.stdout.decode('utf-8', 'replace').strip()

    def bring_up(self, step: StepResult) -> None:
        """Start Redis and the server, seed the Player record, then the client unless server-only.

        This is stack.start() split into the pieces each check reports on.
        """
        stack = self.stack
        assert stack is not None
        revision, commit = self.results.build_revision, self.results.commit
        step.check('build matches the branch head', bool(revision) and revision == commit,
                   f'build {revision or "unknown"}, branch head {commit or "unknown"}')
        assembly = Path(stack.state['inputs']['dotnet']) / 'SWLOR.Game.Server.dll'
        try:
            stale = stale_build_inputs(live.REPO_ROOT, assembly)
            step.check('build includes every uncommitted source change', not stale, ', '.join(stale[:5]))
        except (OSError, subprocess.SubprocessError) as error:
            step.check('build includes every uncommitted source change', False, f'could not tell: {error}')
        if not self.server_only:
            live.ensure_client_image(stack.state['client_image'])
        stack.ensure_owner()
        live.log('Starting Redis and the server')
        stack.compose('up', '-d', '--no-build', timeout=900)
        live.record_image_ids(stack, ('redis', 'swlor-server'))
        live.wait_for_server(stack, self.options.server_timeout)
        step.check('server loads the module', True, 'OnModuleLoad logged "Server is starting up."')
        step.check('redis answers', self.redis('PING') == 'PONG')
        uuid = stack.state.get('character', {}).get('uuid')
        if uuid:
            live.seed_player(stack, uuid)
        errors = error_logs(stack.home)
        step.check('no server error log written during boot', not errors, ', '.join(errors))
        services = ['redis', 'swlor-server']
        if not self.server_only:
            stack.ensure_owner()
            live.start_client(stack, Path(stack.state['cdkey_source']))
            live.record_image_ids(stack, ('client',))
            step.check('client connects to the server', stack.joined_players() > 0)
            services.append('client')
        # Last, so a server that stopped right after loading the module cannot pass.
        stopped = [service for service in services if not stack.running(service)]
        step.check('containers still running after start-up', not stopped, ', '.join(stopped))

    # -- steps ------------------------------------------------------

    def capture(self, step: StepResult, name: str) -> None:
        assert self.stack is not None
        path = live.capture(self.stack, screenshot_label(step.id, name) + self.label_suffix)
        step.screenshots.append(relative_or_absolute(path, self.results_dir))

    def step_boot(self, step: StepResult) -> None:
        self.bring_up(step)
        if not step.checks_passed:
            return
        if self.server_only:
            step.status = BLOCKED
            step.detail = 'Server-only run: the client half of this step did not run.'
        else:
            self.capture(step, 'connected')
            step.detail = 'Server loaded the module and the client connected.'

    def step_login(self, step: StepResult) -> None:
        assert self.stack is not None
        stack = self.stack
        live.play(stack, self.options.settle)
        step.check('character entered the module', live.module_entries(stack) > 0)
        uuid = stack.state['character']['uuid']
        record = json.loads(self.redis('JSON.GET', f'Player:{uuid}', '.'))
        step.check('player record exists', record.get('Id') == uuid)
        step.check('first-login setup finished', record.get('CharacterInitializationPending') is False,
                   f'Version {record.get("Version")}')
        self.capture(step, 'in-area')
        step.detail = 'Character entered the module and its record was initialized.'

    def step_combat(self, step: StepResult) -> None:
        smoke_steps.combat(self, step)

    def step_ability(self, step: StepResult) -> None:
        smoke_steps.ability(self, step)

    def step_storage(self, step: StepResult) -> None:
        smoke_steps.storage(self, step)

    def step_craft(self, step: StepResult) -> None:
        smoke_steps.craft(self, step)

    def step_quest(self, step: StepResult) -> None:
        smoke_steps.quest(self, step)

    def step_transfer(self, step: StepResult) -> None:
        smoke_steps.transfer(self, step)

    def step_logout(self, step: StepResult) -> None:
        stack = self.stack
        assert stack is not None
        # boot_players treats "nobody joined" as success, so require a player first.
        step.check('a player is joined before logout', stack.joined_players() > 0)
        # The client exits when it is kicked, so this is the last frame the run can take.
        self.capture(step, 'before-logout')
        step.check('player booted through the server console', live.boot_players(stack))
        step.check('no player remains joined', stack.joined_players() <= 0)
        step.detail = 'Character was logged out through the server console.'

    # -- orchestration ----------------------------------------------

    def run_step(self, definition: StepDefinition, action: Callable[[StepResult], None] | None) -> StepResult:
        step = StepResult(definition.id, definition.title)
        self.results.steps.append(step)
        started = time.monotonic()
        try:
            if definition.manual_reason is not None:
                step.status, step.detail = MANUAL, definition.manual_reason
            elif definition.needs_client and self.server_only:
                step.status, step.detail = BLOCKED, 'Server-only run: no client is attached.'
            else:
                assert action is not None, f'step {definition.id} has no action'
                step.status = PASS
                action(step)
                # A step may downgrade itself (server-only boot); otherwise its checks decide.
                if step.status == PASS and not step.checks_passed:
                    step.status = FAIL
                    step.detail = 'Failed checks: ' + ', '.join(check.name for check in step.checks if not check.passed)
                elif step.status == BLOCKED and not step.checks_passed:
                    step.status = FAIL
        except Exception as error:
            # Any failure of a step, including one in the screenshot analysis, is that step's result.
            step.status = FAIL
            step.detail = str(error) if isinstance(error, LiveStackError) else f'{type(error).__name__}: {error}'
        step.seconds = time.monotonic() - started
        live.log(f'Step {definition.id}: {step.status}' + (f' - {step.detail}' if step.detail else ''))
        return step

    def execute(self) -> RunResults:
        actions = self.actions()
        try:
            self.create_stack()
        except Exception as error:
            self.setup_failed = True
            self.results.error = f'The stack could not be prepared: {error}'
            self.finish()
            return self.results
        try:
            failed_earlier = False
            for definition in SCENARIO:
                if failed_earlier and definition.manual_reason is None:
                    self.results.steps.append(
                        StepResult(definition.id, definition.title, SKIPPED, 'An earlier step failed.'))
                    continue
                step = self.run_step(definition, actions.get(definition.id))
                failed_earlier = failed_earlier or step.status == FAIL
        except BaseException as error:
            self.results.error = f'The run was interrupted: {type(error).__name__}: {error}'
            raise
        finally:
            self.finish()
        return self.results

    def finish(self) -> None:
        """Tear the stack down, then write results.json and results.md."""
        if self.stack is not None:
            try:
                report = live.destroy(self.stack)
            except Exception as error:
                report = {'clean': False, 'errors': [f'Teardown failed: {error}']}
            self.results.teardown = {key: report.get(key) for key in (
                'clean', 'player_booted', 'remaining_containers', 'remaining_volumes', 'remaining_networks',
                'profile_removed', 'home_removed', 'key_and_character_copies_remaining', 'errors')}
        self.write_results()

    def write_results(self) -> None:
        self.results.finished_utc = utc_now()
        self.results.seconds = round(time.monotonic() - self._clock, 1)
        self.results_dir.mkdir(parents=True, exist_ok=True)
        (self.results_dir / 'results.json').write_text(json.dumps(results_to_json(self.results), indent=2) + '\n',
                                                       'utf-8')
        (self.results_dir / 'results.md').write_text(results_to_markdown(self.results), 'utf-8')
        live.log(f'Results: {self.results_dir / "results.md"}')

    def attach(self, stack: Stack, only: list[str]) -> RunResults:
        """Run chosen steps against a stack that is already up, and leave it running.

        For developing and diagnosing steps: start a stack with NwnLiveClient.py up (and the
        smoke server environment), then run steps one at a time. No teardown happens here.
        """
        self.stack = stack
        self.client = GameClient(stack)
        self.label_suffix = f'-a{int(time.time())}'
        self.results.build_revision = stack.state['inputs'].get('build_source_revision', '')
        actions = self.actions()
        unknown = [name for name in only if name not in actions]
        if unknown:
            raise LiveStackError(f'Unknown step(s): {", ".join(unknown)}. Steps: {", ".join(actions)}.')
        for definition in SCENARIO:
            if definition.id in only:
                self.run_step(definition, actions[definition.id])
        self.write_results()
        return self.results

    def actions(self) -> dict:
        return {'boot': self.step_boot, 'login': self.step_login, 'combat': self.step_combat,
                'ability': self.step_ability, 'storage': self.step_storage, 'craft': self.step_craft,
                'quest': self.step_quest, 'transfer': self.step_transfer, 'logout': self.step_logout}


# ---------------------------------------------------------------- command line

def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(
        prog='SmokeTest.py',
        description='Scripted live smoke run. Boots this checkout\'s server in a private Docker stack, walks the '
                    'core player loop with the Linux client, records a pass/fail line and screenshot per step, '
                    'and tears everything down. Results go outside the repository.')
    commands = root.add_subparsers(dest='command', required=True)

    run = commands.add_parser('run', help='Run the scenario (once, or --repeat times in a row).')
    run.add_argument('--name', default=None, help='Run label; the Compose project is "swlor-live-<label>" '
                                                  '(default: smoke-<random>).')
    run.add_argument('--results-root', type=Path, default=None,
                     help=f'Folder for results, outside the repository (default: the temp folder\'s '
                          f'{DEFAULT_RESULTS_FOLDER}).')
    run.add_argument('--repeat', type=int, default=1,
                     help='Run the scenario this many times in a row and compare the runs.')
    run.add_argument('--server-only', action='store_true',
                     help='Boot and check the server without a client. Reads no CD key; the client steps are '
                          'recorded as blocked.')
    run.add_argument('--character', help='Test character .bic (default: localvault/agantarn.bic of the NWN user '
                                         'folder).')
    run.add_argument('--public-cdkey', help='8-character public CD key (or SWLOR_LIVE_PUBLIC_CDKEY).')
    run.add_argument('--cdkey', help='cdkey.ini to copy into the run profile (see NwnLiveClient.py up).')
    run.add_argument('--dotnet', help='Server build output (default: Release build of this checkout).')
    run.add_argument('--configuration', default='Release')
    run.add_argument('--module', help='.mod file (default: the main checkout\'s deployed module).')
    run.add_argument('--hak-dir', help='Deployed hak folder (default: the main checkout\'s).')
    run.add_argument('--tlk-dir', help='Deployed TLK folder (default: the main checkout\'s).')
    run.add_argument('--client-install', help='NWN:EE install containing the Linux client.')
    run.add_argument('--server-timeout', type=float, default=900, help='Seconds to wait for the module to load.')
    run.add_argument('--max-minutes', type=float, default=120, help='Lifetime limit of the server and client.')
    run.add_argument('--settle', type=float, default=45, help='Seconds without an area transition before the '
                                                              'character counts as settled.')

    attach = commands.add_parser('attach', help='Run chosen steps against a stack that is already up (development).')
    attach.add_argument('run_dir', type=Path, help='Run directory printed by NwnLiveClient.py up.')
    attach.add_argument('--only', required=True, help='Comma-separated step ids, for example combat,ability.')
    attach.add_argument('--results-dir', type=Path, help='Where to write results (default: <run dir>/attach).')

    compare = commands.add_parser('compare', help='Compare two results.json files.')
    compare.add_argument('first', type=Path)
    compare.add_argument('second', type=Path)
    return root


def run_command(options: argparse.Namespace) -> int:
    if options.repeat < 1:
        raise LiveStackError('--repeat must be at least 1.')
    if not options.server_only:
        require_pillow()
    root = (options.results_root or default_results_root()).resolve()
    for repository in live.repository_roots():
        if live.is_within(root, repository):
            raise LiveStackError(f'The results folder must be outside the repository: {root}')
    if not options.character:
        found = default_character()
        if found is None:
            raise LiveStackError('Pass --character <test character>.bic; the default test character was not found.')
        options.character = str(found)
    options.name = options.name or f'smoke-{secrets.token_hex(3)}'
    # A label is a folder name and a Compose project suffix: refuse separators and dots before
    # anything is created, so a name can never place results outside the validated root.
    for number in range(1, options.repeat + 1):
        live.project_name(run_label(options.name, number, options.repeat))
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    batch = (root / f'{options.name}-{stamp}').resolve()
    if not live.is_within(batch, root):
        raise LiveStackError(f'The results folder must stay inside {root}: {batch}')
    batch.mkdir(parents=True)
    documents = []
    exit_code = 0
    for number in range(1, options.repeat + 1):
        label = run_label(options.name, number, options.repeat)
        results_dir = batch / label if options.repeat > 1 else batch
        results_dir.mkdir(parents=True, exist_ok=True)
        smoke_run = SmokeRun(options, label, results_dir)
        results = smoke_run.execute()
        documents.append(results_to_json(results))
        if smoke_run.setup_failed:
            # The documented "could not be prepared" code; repeating would fail the same way.
            exit_code = 2
            break
        if results.failed:
            exit_code = 1
    if options.repeat > 1 and exit_code != 2:
        differences = [difference for later in documents[1:] for difference in compare_results(documents[0], later)]
        (batch / 'comparison.json').write_text(json.dumps({'runs': options.repeat, 'differences': differences},
                                                           indent=2) + '\n', 'utf-8')
        live.log('Runs agree.' if not differences else 'Runs differ: ' + '; '.join(differences))
        if differences:
            exit_code = 1
    live.log(f'Results folder: {batch}')
    return exit_code


def attach_steps(selection: str) -> list[str]:
    """The step ids named by --only; empty, unknown and boot (which starts the stack) are refused."""
    names = [name.strip() for name in selection.split(',') if name.strip()]
    if not names:
        raise LiveStackError('--only names no step. Steps: ' + ', '.join(
            definition.id for definition in SCENARIO if definition.id != 'boot') + '.')
    if 'boot' in names:
        raise LiveStackError('The boot step starts the stack, so it cannot run against one that is already up; '
                             'use NwnLiveClient.py up to start it.')
    return names


def attach_command(options: argparse.Namespace) -> int:
    names = attach_steps(options.only)
    require_pillow()
    stack = Stack(options.run_dir)
    stack.ensure_owner()
    values = dict(server_only=False, settle=45.0, character=None)
    results_dir = options.results_dir or stack.run_dir / 'attach'
    run = SmokeRun(argparse.Namespace(**values), f'{stack.project}-attach', results_dir)
    results = run.attach(stack, names)
    for step in results.steps:
        live.log(f'{step.id}: {step.status} {step.detail}')
        for check in step.checks:
            live.log(f'    {"pass" if check.passed else "FAIL"}: {check.name} {check.detail}')
    return 1 if any(step.status == FAIL for step in results.steps) else 0


def main(argv: list[str] | None = None) -> int:
    options = parser().parse_args(argv)
    try:
        if options.command == 'compare':
            differences = compare_results(json.loads(options.first.read_text('utf-8')),
                                          json.loads(options.second.read_text('utf-8')))
            print('\n'.join(differences) if differences else 'The runs agree.')
            return 1 if differences else 0
        if options.command == 'attach':
            return attach_command(options)
        return run_command(options)
    except LiveStackError as error:
        print(f'[nwn-live] ERROR: {error}', file=sys.stderr)
        return 2


if __name__ == '__main__':
    sys.exit(main())
