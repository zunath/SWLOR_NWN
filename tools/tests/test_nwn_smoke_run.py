"""Pure logic of the scripted smoke run; the Docker run itself is the smoke baseline."""
from pathlib import Path
import json
import os
import subprocess
import sys
import tempfile
import unittest
from types import SimpleNamespace
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from nwn_live_client import smoke
from nwn_live_client.stack import LiveStackError


def options(**overrides):
    values = dict(
        name='smoke-test', results_root=None, repeat=1, server_only=True, character='agantarn.bic',
        public_cdkey=None, cdkey=None, dotnet=None, configuration='Release', module=None, hak_dir=None,
        tlk_dir=None, client_install=None, server_timeout=900.0, max_minutes=120.0, settle=45.0)
    values.update(overrides)
    return smoke.argparse.Namespace(**values)


def document(statuses, checks=None, mode='server-only'):
    """A minimal results.json with the given status per scenario step."""
    checks = checks or {}
    return {'mode': mode, 'steps': [
        {'id': definition.id, 'status': statuses.get(definition.id, smoke.MANUAL),
         'checks': [{'name': name, 'passed': passed, 'detail': 'varies'}
                    for name, passed in checks.get(definition.id, [])]}
        for definition in smoke.SCENARIO]}


class ScenarioTests(unittest.TestCase):
    def test_scenario_follows_the_p0_6_spec(self):
        self.assertEqual(['boot', 'login', 'combat', 'ability', 'storage', 'craft', 'quest', 'transfer', 'logout'],
                         [definition.id for definition in smoke.SCENARIO])
        self.assertEqual(len(smoke.SCENARIO), len({definition.id for definition in smoke.SCENARIO}))

    def test_unautomated_steps_carry_a_reason_and_automated_ones_do_not(self):
        automated = {'boot', 'login', 'logout'}
        for definition in smoke.SCENARIO:
            self.assertEqual(definition.id not in automated, definition.manual_reason is not None, definition.id)

    def test_screenshot_labels_sort_in_scenario_order_and_are_valid_capture_labels(self):
        labels = [smoke.screenshot_label(definition.id, 'shot') for definition in smoke.SCENARIO]
        self.assertEqual(sorted(labels), labels)
        self.assertEqual('02-login-in-area', smoke.screenshot_label('login', 'in-area'))
        for label in labels:
            self.assertRegex(label, r'^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$')

    def test_run_labels_are_distinct_and_compose_safe_for_repeated_runs(self):
        self.assertEqual('smoke', smoke.run_label('smoke', 1, 1))
        self.assertEqual(['smoke-1', 'smoke-2'], [smoke.run_label('smoke', n, 2) for n in (1, 2)])
        for label in ('smoke-1', 'smoke-abc123-2'):
            self.assertRegex('swlor-live-' + label, r'^[a-z0-9][a-z0-9_-]{0,62}$')


class ResultsTests(unittest.TestCase):
    def test_runs_that_differ_only_in_timings_and_details_agree(self):
        first = document({'boot': smoke.PASS}, {'boot': [('server loads the module', True)]})
        second = document({'boot': smoke.PASS}, {'boot': [('server loads the module', True)]})
        second['steps'][0]['seconds'] = 999
        self.assertEqual([], smoke.compare_results(first, second))

    def test_a_status_or_check_difference_is_reported(self):
        first = document({'boot': smoke.PASS}, {'boot': [('redis answers', True)]})
        failed = document({'boot': smoke.FAIL}, {'boot': [('redis answers', True)]})
        flipped = document({'boot': smoke.PASS}, {'boot': [('redis answers', False)]})
        self.assertEqual(['boot: status pass != fail'], smoke.compare_results(first, failed))
        self.assertEqual(1, len(smoke.compare_results(first, flipped)))
        self.assertIn('checks differ', smoke.compare_results(first, flipped)[0])

    def test_a_mode_difference_is_reported(self):
        self.assertEqual(['mode: full != server-only'],
                         smoke.compare_results(document({}, mode='full'), document({})))

    def test_results_serialize_to_json_and_markdown(self):
        step = smoke.StepResult('boot', 'Server boots', smoke.FAIL, 'ok')
        step.check('redis answers', True)
        step.check('build matches the branch head', False, 'stale')
        results = smoke.RunResults('smoke', 'full', '2026-01-01T00:00:00+00:00', [step],
                                   teardown={'clean': True})
        data = json.loads(json.dumps(smoke.results_to_json(results)))
        self.assertEqual('boot', data['steps'][0]['id'])
        self.assertEqual([('redis answers', True), ('build matches the branch head', False)],
                         [(c['name'], c['passed']) for c in data['steps'][0]['checks']])
        self.assertFalse(data['complete'])
        text = smoke.results_to_markdown(results)
        self.assertIn('| 1 | Server boots | FAIL | ok |', text)
        self.assertIn('- FAIL: build matches the branch head (stale)', text)

    def test_only_a_fully_passing_run_is_complete_and_a_dirty_teardown_fails_the_run(self):
        passing = [smoke.StepResult(d.id, d.title, smoke.PASS) for d in smoke.SCENARIO]
        results = smoke.RunResults('smoke', 'full', 'now', passing, teardown={'clean': True})
        self.assertTrue(results.complete)
        self.assertFalse(results.failed)
        results.teardown = {'clean': False}
        self.assertTrue(results.failed)
        results.teardown = {'clean': True}
        results.steps[3].status = smoke.MANUAL
        self.assertFalse(results.complete)
        self.assertFalse(results.failed)
        results.steps[3].status = smoke.FAIL
        self.assertTrue(results.failed)

    def test_a_run_that_recorded_no_steps_or_an_error_is_never_complete(self):
        setup_failed = smoke.RunResults('smoke', 'full', 'now', [], teardown={'clean': True},
                                        error='The stack could not be prepared: docker is not on PATH.')
        self.assertFalse(setup_failed.complete)
        self.assertTrue(setup_failed.failed)
        no_steps = smoke.RunResults('smoke', 'full', 'now', [], teardown={'clean': True})
        self.assertFalse(no_steps.complete)
        passing = [smoke.StepResult(d.id, d.title, smoke.PASS) for d in smoke.SCENARIO]
        self.assertFalse(smoke.RunResults('smoke', 'full', 'now', passing[:-1], teardown={'clean': True}).complete)
        self.assertFalse(smoke.RunResults('smoke', 'full', 'now', passing, teardown={'clean': False}).complete)


class ErrorLogTests(unittest.TestCase):
    def test_error_logs_are_listed_by_name_and_absence_means_none(self):
        with tempfile.TemporaryDirectory() as folder:
            home = Path(folder)
            self.assertEqual([], smoke.error_logs(home))
            (home / 'app_logs' / 'Error').mkdir(parents=True)
            self.assertEqual([], smoke.error_logs(home))
            (home / 'app_logs' / 'Error' / 'Error_20261027.log').write_text('boom', 'utf-8')
            (home / 'app_logs' / 'Error' / 'notes.txt').write_text('ignored', 'utf-8')
            self.assertEqual(['Error_20261027.log'], smoke.error_logs(home))


class StaleBuildTests(unittest.TestCase):
    def test_porcelain_output_lists_paths_including_a_renames_source(self):
        status = b' M SWLOR.Game.Server/A.cs\0?? tools/new file.py\0R  SWLOR.NWN.API/New.cs\0SWLOR.NWN.API/Old.cs\0'
        self.assertEqual(['SWLOR.Game.Server/A.cs', 'tools/new file.py', 'SWLOR.NWN.API/New.cs',
                          'SWLOR.NWN.API/Old.cs'], smoke.uncommitted_paths(status))
        self.assertEqual([], smoke.uncommitted_paths(b''))

    def test_only_files_the_server_build_compiles_count(self):
        for path in ('SWLOR.Game.Server/Service/Combat.cs', 'SWLOR.NWN.API/NWScript.cs', 'Directory.Build.props',
                     'global.json'):
            self.assertTrue(smoke.is_build_input(path), path)
        for path in ('tools/SmokeTest.py', 'Module/are/x.are.json', 'SWLOR.Game.Server/Readmes/Guide.md',
                     'SWLOR.Game.Server.Tests/CombatTests.cs', 'README.md'):
            self.assertFalse(smoke.is_build_input(path), path)

    def test_uncommitted_edits_newer_than_the_assembly_are_reported(self):
        with tempfile.TemporaryDirectory() as folder:
            repo = Path(folder)

            def git(*arguments):
                subprocess.run(['git', '-C', str(repo), '-c', 'user.name=t', '-c', 'user.email=t@example.com',
                                *arguments], check=True, capture_output=True)
            git('init', '-q')
            for name in ('SWLOR.Game.Server/A.cs', 'SWLOR.Game.Server/Gone.cs', 'tools/x.py', 'NOTES.md'):
                (repo / name).parent.mkdir(parents=True, exist_ok=True)
                (repo / name).write_text('one', 'utf-8')
            git('add', '-A')
            git('commit', '-q', '-m', 'first')
            assembly = repo / 'built.dll'
            assembly.write_text('dll', 'utf-8')
            old = os.stat(assembly).st_mtime_ns - 10_000_000_000
            os.utime(assembly, ns=(old, old))
            self.assertEqual([], smoke.stale_build_inputs(repo, assembly))
            (repo / 'SWLOR.Game.Server/A.cs').write_text('two', 'utf-8')
            (repo / 'SWLOR.Game.Server/Gone.cs').unlink()
            (repo / 'SWLOR.NWN.API').mkdir()
            (repo / 'SWLOR.NWN.API/B.cs').write_text('new', 'utf-8')
            (repo / 'tools/x.py').write_text('two', 'utf-8')
            (repo / 'NOTES.md').write_text('two', 'utf-8')
            self.assertEqual(['SWLOR.Game.Server/A.cs (newer than the build)', 'SWLOR.Game.Server/Gone.cs (deleted)',
                              'SWLOR.NWN.API/B.cs (newer than the build)'], smoke.stale_build_inputs(repo, assembly))
            # A build made after the edits covers them; only the deletion cannot be proven.
            fresh = os.stat(repo / 'SWLOR.NWN.API/B.cs').st_mtime_ns + 1_000_000_000
            os.utime(assembly, ns=(fresh, fresh))
            self.assertEqual(['SWLOR.Game.Server/Gone.cs (deleted)'], smoke.stale_build_inputs(repo, assembly))


class BringUpTests(unittest.TestCase):
    def fake_run(self, folder, running, server_only=True):
        run = smoke.SmokeRun(options(server_only=server_only), 'smoke-test', Path(folder))
        run.results.commit = run.results.build_revision = 'a' * 40
        run.redis = lambda *arguments: 'PONG'
        run.stack = SimpleNamespace(
            state={'inputs': {'dotnet': folder}}, home=Path(folder), running=running,
            ensure_owner=lambda: None, compose=lambda *arguments, **keywords: None, joined_players=lambda: 0)
        return run

    def bring_up(self, run):
        step = smoke.StepResult('boot', 'Server boots')
        with patch.object(smoke.live, 'wait_for_server'), patch.object(smoke.live, 'record_image_ids'), \
                patch.object(smoke.live, 'log'), patch.object(smoke, 'stale_build_inputs', return_value=[]):
            run.bring_up(step)
        return {check.name: check.passed for check in step.checks}

    def test_a_server_that_stopped_after_loading_the_module_fails_a_server_only_boot(self):
        with tempfile.TemporaryDirectory() as folder:
            checks = self.bring_up(self.fake_run(folder, lambda service: service != 'swlor-server'))
        self.assertFalse(checks['containers still running after start-up'])
        self.assertTrue(checks['redis answers'])

    def test_a_healthy_server_only_boot_passes_every_check_and_starts_no_client(self):
        with tempfile.TemporaryDirectory() as folder:
            checks = self.bring_up(self.fake_run(folder, lambda service: service != 'client'))
        self.assertTrue(all(checks.values()), checks)
        self.assertNotIn('client connects to the server', checks)

    def test_an_edited_source_newer_than_the_build_fails_the_boot_step(self):
        with tempfile.TemporaryDirectory() as folder:
            run = self.fake_run(folder, lambda service: True)
            step = smoke.StepResult('boot', 'Server boots')
            with patch.object(smoke.live, 'wait_for_server'), patch.object(smoke.live, 'record_image_ids'), \
                    patch.object(smoke.live, 'log'), patch.object(
                        smoke, 'stale_build_inputs', return_value=['SWLOR.Game.Server/A.cs (newer than the build)']):
                run.bring_up(step)
        failed = [check.name for check in step.checks if not check.passed]
        self.assertEqual(['build includes every uncommitted source change'], failed)


class ClientStepTests(unittest.TestCase):
    def test_boot_takes_a_screenshot_once_the_client_is_connected(self):
        with tempfile.TemporaryDirectory() as folder:
            run = smoke.SmokeRun(options(server_only=False), 'smoke-test', Path(folder))
            step = smoke.StepResult('boot', 'Server boots')
            captured = []
            run.bring_up = lambda step: None
            run.capture = lambda step, name: captured.append(name)
            run.step_boot(step)
        self.assertEqual(['connected'], captured)

    def test_logout_captures_before_the_player_is_booted_because_the_client_exits_afterwards(self):
        with tempfile.TemporaryDirectory() as folder:
            run = smoke.SmokeRun(options(server_only=False), 'smoke-test', Path(folder))
            run.stack = SimpleNamespace(joined_players=lambda: 0)
            order = []
            run.capture = lambda step, name: order.append('capture')
            step = smoke.StepResult('logout', 'Log out cleanly')
            with patch.object(smoke.live, 'boot_players', side_effect=lambda stack: order.append('boot') or True):
                run.step_logout(step)
        self.assertEqual(['capture', 'boot'], order)


class RunStepTests(unittest.TestCase):
    def run_one(self, step_id, action, server_only=False):
        run = smoke.SmokeRun(options(server_only=server_only), 'smoke-test', Path(tempfile.gettempdir()))
        definition = next(d for d in smoke.SCENARIO if d.id == step_id)
        return run.run_step(definition, action)

    def test_checks_decide_the_status_of_an_automated_step(self):
        self.assertEqual(smoke.PASS, self.run_one('boot', lambda step: step.check('a', True)).status)
        failed = self.run_one('boot', lambda step: step.check('a', False))
        self.assertEqual(smoke.FAIL, failed.status)
        self.assertIn('a', failed.detail)

    def test_a_step_that_raises_a_stack_error_fails_with_its_message(self):
        def action(step):
            raise LiveStackError('the container stopped')
        step = self.run_one('boot', action)
        self.assertEqual((smoke.FAIL, 'the container stopped'), (step.status, step.detail))

    def test_a_step_without_automation_is_manual_with_its_reason_and_never_runs(self):
        step = self.run_one('combat', lambda step: self.fail('a manual step must not run'))
        self.assertEqual(smoke.MANUAL, step.status)
        self.assertTrue(step.detail)

    def test_client_steps_are_blocked_in_a_server_only_run(self):
        step = self.run_one('login', lambda step: self.fail('no client is attached'), server_only=True)
        self.assertEqual(smoke.BLOCKED, step.status)

    def test_a_step_may_downgrade_itself_unless_a_check_failed(self):
        def blocked(step):
            step.check('a', True)
            step.status = smoke.BLOCKED
        def blocked_and_failing(step):
            step.check('a', False)
            step.status = smoke.BLOCKED
        self.assertEqual(smoke.BLOCKED, self.run_one('boot', blocked).status)
        self.assertEqual(smoke.FAIL, self.run_one('boot', blocked_and_failing).status)


class RunTests(unittest.TestCase):
    def test_the_up_options_come_from_the_tools_own_parser(self):
        with tempfile.TemporaryDirectory() as folder:
            run = smoke.SmokeRun(options(character='C:/x/test.bic', module='C:/m/a.mod'), 'smoke-test', Path(folder))
            up = run.up_options()
        self.assertEqual('smoke-test', up.name)
        self.assertEqual('C:/x/test.bic', up.character)
        self.assertEqual('C:/m/a.mod', up.module)
        self.assertEqual(str(Path(folder) / 'run'), up.run_dir)
        self.assertFalse(up.play)

    def test_a_server_only_run_reads_no_cd_key(self):
        with tempfile.TemporaryDirectory() as folder, patch.object(smoke.live, 'create') as create:
            create.return_value.state = {'inputs': {}}
            run = smoke.SmokeRun(options(cdkey='C:/real/cdkey.ini', public_cdkey=None), 'smoke-test', Path(folder))
            run.create_stack()
            up = create.call_args.args[0]
            self.assertEqual(str(Path(folder) / 'placeholder-cdkey.ini'), up.cdkey)
            self.assertEqual(smoke.PLACEHOLDER_PUBLIC_CDKEY, up.public_cdkey)
            self.assertEqual(smoke.PLACEHOLDER_CDKEY_TEXT, Path(up.cdkey).read_text('utf-8'))
        self.assertNotRegex(smoke.PLACEHOLDER_CDKEY_TEXT, r'[A-Z0-9]{20}')

    def test_a_failed_step_skips_the_automated_steps_after_it_but_keeps_manual_ones(self):
        with tempfile.TemporaryDirectory() as folder:
            run = smoke.SmokeRun(options(server_only=False), 'smoke-test', Path(folder))

            def boot_fails(step):
                raise LiveStackError('no server')
            run.create_stack = lambda: None
            run.step_boot, run.step_login, run.step_logout = boot_fails, boot_fails, boot_fails
            results = run.execute()
            statuses = {step.id: step.status for step in results.steps}
            self.assertEqual(smoke.FAIL, statuses['boot'])
            self.assertEqual(smoke.SKIPPED, statuses['login'])
            self.assertEqual(smoke.SKIPPED, statuses['logout'])
            self.assertEqual(smoke.MANUAL, statuses['combat'])
            written = json.loads((Path(folder) / 'results.json').read_text('utf-8'))
            self.assertEqual(9, len(written['steps']))
            self.assertTrue((Path(folder) / 'results.md').is_file())

    def test_a_stack_that_cannot_be_prepared_is_recorded_and_still_writes_results(self):
        with tempfile.TemporaryDirectory() as folder:
            run = smoke.SmokeRun(options(), 'smoke-test', Path(folder))
            with patch.object(smoke.live, 'create', side_effect=LiveStackError('docker is not on PATH.')):
                results = run.execute()
            self.assertIn('docker is not on PATH.', results.error)
            self.assertTrue(results.failed)
            self.assertTrue((Path(folder) / 'results.json').is_file())

    def test_results_inside_the_repository_are_refused(self):
        inside = smoke.live.REPO_ROOT / 'smoke-results'
        with self.assertRaises(LiveStackError):
            smoke.run_command(options(results_root=inside))

    def test_a_run_name_cannot_place_results_outside_the_results_root(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder) / 'results'
            for name in ('../escape', 'a/b', 'a\\b', '.hidden', 'x' * 80):
                with self.assertRaises(LiveStackError, msg=name):
                    smoke.run_command(options(name=name, results_root=root))
            self.assertFalse(root.exists())

    def test_repeat_must_be_positive(self):
        with self.assertRaises(LiveStackError):
            smoke.run_command(options(repeat=0))


if __name__ == '__main__':
    unittest.main()
