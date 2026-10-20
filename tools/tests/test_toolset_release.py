import argparse
import json
from pathlib import Path
import re
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import toolset_release as release

ROOT = release.ROOT
REVISION = 'a' * 40
OTHER_REVISION = 'b' * 40
GOOD_STATUS = """github.com
  ✓ Logged in to github.com account zunath (keyring)
  - Active account: true
  - Git operations protocol: https
  - Token scopes: 'gist', 'read:org', 'repo', 'workflow'
"""


def git_answers(branch='master', dirty='', head=REVISION, origin=REVISION):
    def git(*args):
        if args[:2] == ('rev-parse', '--abbrev-ref'):
            return branch
        if args[0] == 'status':
            return dirty
        if args[0] == 'fetch':
            return ''
        if args == ('rev-parse', 'HEAD'):
            return head
        if args == ('rev-parse', 'refs/remotes/origin/master'):
            return origin
        raise AssertionError(args)
    return git


class CheckoutGuardTests(unittest.TestCase):
    def test_clean_master_equal_to_origin_is_accepted(self):
        with patch.object(release, 'git', git_answers()):
            self.assertEqual(release.check_checkout(), REVISION)

    def test_each_violation_is_refused(self):
        for name, answers, message in (('branch', git_answers(branch='feature'), 'feature'),
                                       ('dirty', git_answers(dirty=' M file'), 'uncommitted'),
                                       ('behind', git_answers(origin=OTHER_REVISION), 'origin/master')):
            with self.subTest(name), patch.object(release, 'git', answers):
                with self.assertRaisesRegex(RuntimeError, message):
                    release.check_checkout()

    def test_dev_checkout_override_only_warns(self):
        with patch.object(release, 'git', git_answers(branch='feature')), patch('builtins.print') as output:
            self.assertEqual(release.check_checkout(allow_dev_checkout=True), REVISION)
        self.assertIn('WARNING', output.call_args[0][0])


class GhLoginTests(unittest.TestCase):
    def check(self, status):
        with patch.object(release, 'run_gh', return_value=(0, status, '')):
            return release.check_gh_login()

    def test_owner_with_repo_scope_is_accepted(self):
        self.assertEqual(self.check(GOOD_STATUS), 'zunath')

    def test_other_account_missing_scope_and_logged_out_are_refused(self):
        with self.assertRaisesRegex(RuntimeError, 'logged in as someone'):
            self.check(GOOD_STATUS.replace('account zunath', 'account someone'))
        with self.assertRaisesRegex(RuntimeError, 'repo scope'):
            self.check(GOOD_STATUS.replace("'repo', ", ''))
        with self.assertRaisesRegex(RuntimeError, 'not logged in'):
            self.check('You are not logged into any GitHub hosts.')

    def test_only_the_active_account_counts(self):
        status = GOOD_STATUS.replace('Active account: true', 'Active account: false') + GOOD_STATUS.replace('account zunath', 'account other')
        with self.assertRaisesRegex(RuntimeError, 'logged in as other'):
            self.check(status)


class FakeGitHub:
    """Stands in for gh: records every write and answers release/ref lookups from the given state."""
    def __init__(self, releases=None, tag_sha=None):
        self.releases = releases or {}
        self.tag_sha = tag_sha
        self.writes = []

    def view(self, tag):
        return self.releases.get(tag)

    def run(self, arguments, check=True):
        arguments = [str(a) for a in arguments]
        if arguments[0] == 'api' and arguments[1].startswith('repos/') and '--jq' in arguments:
            if self.tag_sha is None:
                return 1, '', 'gh: Not Found (HTTP 404)'
            return 0, self.tag_sha + '\n', ''
        self.writes.append(arguments)
        return '' if check else (0, '', '')


class PublishTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        for name, data in (('SWLOR.Toolset-win-Setup.exe', b'setup'), ('SWLOR.Toolset-win-Portable.zip', b'portable'),
                           ('SWLOR.Toolset-linux.AppImage', b'appimage'), ('SWLOR.Toolset-linux-x64.tar.gz', b'tar'),
                           ('SWLOR.Toolset-1.0.0-win-full.nupkg', b'nupkg'), ('releases.win.json', b'{}'),
                           ('releases.linux.json', b'{}'), ('workspace.zip.001', b'part'), ('toolset-feed.json', b'{}')):
            (self.directory / name).write_bytes(data)

    def publish(self, version='1.0.0', dry_run=False, **state):
        github = FakeGitHub(**state)
        messages = []
        with patch.object(release, 'run_gh', github.run), patch.object(release, 'gh_release', github.view):
            release.publish_release(version, self.directory, REVISION, dry_run=dry_run, log=messages.append)
        return github, messages

    def commands(self, github, *prefix):
        return [w for w in github.writes if w[:len(prefix)] == list(prefix)]

    def test_stable_release_creates_versioned_non_latest_then_moves_fixed_release(self):
        github, _ = self.publish()
        create_versioned, = self.commands(github, 'release', 'create', 'toolset-v1.0.0')
        self.assertIn('--latest=false', create_versioned)
        self.assertNotIn('--prerelease', create_versioned)
        self.assertEqual(create_versioned[create_versioned.index('--target') + 1], REVISION)
        self.assertEqual(len([a for a in create_versioned if Path(a).parent == self.directory]), 9)
        tag, = [w for w in github.writes if w[:2] == ['api', '--method'] and 'refs' in w[3]]
        self.assertEqual(tag[2], 'POST')
        self.assertIn('ref=refs/tags/toolset-latest', tag)
        create_current, = self.commands(github, 'release', 'create', 'toolset-latest')
        self.assertIn('--latest=false', create_current)
        self.assertIn('--verify-tag', create_current)
        order = [w[:3] for w in github.writes]
        self.assertLess(order.index(create_versioned[:3]), order.index(create_current[:3]))

    def test_no_command_can_mark_a_toolset_release_latest(self):
        for version in ('1.0.0', '1.1.0-beta'):
            github, _ = self.publish(version)
            for command in github.writes:
                self.assertNotIn('--latest', command)
                if command[:2] in (['release', 'create'], ['release', 'edit']):
                    self.assertIn('--latest=false', command)

    def test_fixed_release_uploads_only_downloads_with_clobber(self):
        github, _ = self.publish()
        upload, = self.commands(github, 'release', 'upload', 'toolset-latest')
        names = sorted(Path(a).name for a in upload if Path(a).parent == self.directory)
        self.assertEqual(names, ['SWLOR.Toolset-linux-x64.tar.gz', 'SWLOR.Toolset-linux.AppImage', 'SWLOR.Toolset-win-Portable.zip',
                                 'SWLOR.Toolset-win-Setup.exe', 'toolset-feed.json'])
        self.assertIn('--clobber', upload)

    def test_prerelease_never_touches_the_fixed_release(self):
        github, messages = self.publish('1.1.0-beta')
        create, = self.commands(github, 'release', 'create')
        self.assertEqual(create[2], 'toolset-v1.1.0-beta')
        self.assertIn('--prerelease', create)
        self.assertEqual(len(github.writes), 1)
        self.assertTrue(any('unchanged' in m for m in messages))

    def test_existing_fixed_release_is_edited_tag_moved_and_stale_assets_removed(self):
        remote = {'assets': [{'name': 'SWLOR.Toolset-win-Setup.exe', 'size': 1}, {'name': 'old-1.0.0.exe', 'size': 1}], 'isImmutable': False}
        github, _ = self.publish('1.2.0', releases={'toolset-latest': remote}, tag_sha=OTHER_REVISION)
        patch_tag, = [w for w in github.writes if w[:3] == ['api', '--method', 'PATCH']]
        self.assertIn('sha=' + REVISION, patch_tag)
        self.assertIn('force=true', patch_tag)
        edit, = self.commands(github, 'release', 'edit', 'toolset-latest')
        self.assertIn('--latest=false', edit)
        self.assertEqual(self.commands(github, 'release', 'delete-asset'), [['release', 'delete-asset', 'toolset-latest', 'old-1.0.0.exe', '--repo', release.REPO, '--yes']])
        self.assertEqual(self.commands(github, 'release', 'create', 'toolset-latest'), [])

    def test_tag_already_on_the_published_commit_is_not_rewritten(self):
        github, _ = self.publish(tag_sha=REVISION, releases={'toolset-latest': {'assets': [], 'isImmutable': False}})
        self.assertEqual([w for w in github.writes if w[0] == 'api'], [])

    def test_immutable_fixed_release_is_refused(self):
        with self.assertRaisesRegex(RuntimeError, 'immutable'):
            self.publish(releases={'toolset-latest': {'assets': [], 'isImmutable': True}})

    def versioned_state(self, **overrides):
        assets = []
        for path in sorted(self.directory.iterdir()):
            assets.append({'name': path.name, 'size': path.stat().st_size, 'digest': 'sha256:' + release.file_sha256(path)})
        assets = [dict(a, **overrides.get(a['name'], {})) for a in assets]
        return {'assets': assets, 'isImmutable': False}

    def test_rerun_with_identical_assets_skips_the_versioned_release(self):
        github, messages = self.publish(releases={'toolset-v1.0.0': self.versioned_state()})
        self.assertEqual(self.commands(github, 'release', 'create', 'toolset-v1.0.0'), [])
        self.assertEqual(self.commands(github, 'release', 'upload', 'toolset-v1.0.0'), [])
        self.assertTrue(any('skipped' in m for m in messages))
        self.assertTrue(self.commands(github, 'release', 'create', 'toolset-latest'))

    def test_different_assets_are_refused_before_anything_is_written(self):
        for change in ({'size': 1}, {'digest': 'sha256:' + '0' * 64}):
            with self.subTest(change):
                state = self.versioned_state(**{'toolset-feed.json': change})
                github = FakeGitHub(releases={'toolset-v1.0.0': state})
                with patch.object(release, 'run_gh', github.run), patch.object(release, 'gh_release', github.view):
                    with self.assertRaisesRegex(RuntimeError, 'different assets'):
                        release.publish_release('1.0.0', self.directory, REVISION, log=lambda m: None)
                self.assertEqual(github.writes, [])

    def test_interrupted_release_gets_only_the_missing_assets(self):
        state = self.versioned_state()
        state['assets'] = [a for a in state['assets'] if a['name'] != 'toolset-feed.json']
        github, _ = self.publish(releases={'toolset-v1.0.0': state})
        upload, = self.commands(github, 'release', 'upload', 'toolset-v1.0.0')
        self.assertEqual([Path(a).name for a in upload if Path(a).parent == self.directory], ['toolset-feed.json'])

    def test_dry_run_issues_no_writes(self):
        github, messages = self.publish(dry_run=True)
        self.assertEqual(github.writes, [])
        self.assertTrue(any(m.startswith('[dry-run] gh release create toolset-v1.0.0') for m in messages))
        self.assertTrue(any(m.startswith('[dry-run] gh release upload toolset-latest') for m in messages))

    def test_release_without_a_feed_is_refused(self):
        (self.directory / 'toolset-feed.json').unlink()
        with self.assertRaisesRegex(RuntimeError, 'toolset-feed.json'):
            self.publish()

    def test_invalid_version_and_revision_are_refused(self):
        with self.assertRaisesRegex(RuntimeError, 'semantic'):
            release.publish_release('v1', self.directory, REVISION)
        with self.assertRaisesRegex(RuntimeError, 'commit hash'):
            release.publish_release('1.0.0', self.directory, 'master')


class LocalBuildTests(unittest.TestCase):
    def test_platform_release_folders_merge_without_overwriting(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for platform, name in (('win', 'a.exe'), ('linux', 'b.AppImage')):
                (root / platform).mkdir()
                (root / platform / name).write_bytes(b'x')
            merged = release.merge_platform_releases([root / 'win', root / 'linux'], root / 'merged')
            self.assertEqual([p.name for p in merged], ['a.exe', 'b.AppImage'])
            (root / 'linux' / 'a.exe').write_bytes(b'y')
            with self.assertRaisesRegex(RuntimeError, 'same release file'):
                release.merge_platform_releases([root / 'win', root / 'linux'], root / 'again')

    def test_signing_requires_environment_variables_and_an_existing_pfx(self):
        with self.assertRaisesRegex(RuntimeError, 'SWLOR_SIGN_PFX'):
            release.check_signing_environment({})
        with self.assertRaisesRegex(RuntimeError, 'not found'):
            release.check_signing_environment({release.SIGN_PFX_VARIABLE: 'missing.pfx', release.SIGN_PASSWORD_VARIABLE: 'secret'})
        with tempfile.TemporaryDirectory() as temporary:
            pfx = Path(temporary) / 'cert.pfx'
            pfx.write_bytes(b'pfx')
            release.check_signing_environment({release.SIGN_PFX_VARIABLE: str(pfx), release.SIGN_PASSWORD_VARIABLE: 'secret'})

    def local_arguments(self, **overrides):
        values = dict(version='1.0.0', output=None, publish=False, asset_descriptor=None, sign_windows=False, skip_tests=False, allow_dev_checkout=False)
        values.update(overrides)
        return argparse.Namespace(**values)

    def test_publish_cannot_waive_the_checkout_guards(self):
        with patch.object(release.os, 'name', 'nt'):
            with self.assertRaisesRegex(RuntimeError, 'dry runs only'):
                release.release_local(self.local_arguments(publish=True, allow_dev_checkout=True))

    def test_a_refused_checkout_builds_nothing(self):
        with patch.object(release.os, 'name', 'nt'), patch.object(release, 'run_gh', return_value=(0, GOOD_STATUS, '')), \
                patch.object(release, 'git', git_answers(branch='feature')), patch.object(release, 'build_windows') as windows, \
                patch.object(release, 'build_linux') as linux:
            with self.assertRaisesRegex(RuntimeError, 'not master'):
                release.release_local(self.local_arguments())
        windows.assert_not_called()
        linux.assert_not_called()

    def test_a_refused_gh_login_builds_nothing(self):
        with patch.object(release.os, 'name', 'nt'), patch.object(release, 'run_gh', return_value=(1, '', 'not logged in')), \
                patch.object(release, 'git') as git, patch.object(release, 'build_windows') as windows:
            with self.assertRaisesRegex(RuntimeError, 'not logged in'):
                release.release_local(self.local_arguments())
        git.assert_not_called()
        windows.assert_not_called()

    def test_container_script_runs_the_workflow_linux_steps(self):
        script = release.linux_container_script(run_tests=False)
        for expected in ('prepare-tools', 'publish --version "$TOOLSET_VERSION" --skip-workspace', '--verify-install', 'xvfb-run -a', '--desktop', 'cp /work/build/release/* /out/'):
            self.assertIn(expected, script)
        self.assertNotIn('unittest', script)
        self.assertIn('unittest', release.linux_container_script(run_tests=True))

    def test_container_base_image_is_pinned_by_digest(self):
        self.assertRegex(release.DOCKERFILE.read_text(), r'(?m)^FROM mcr\.microsoft\.com/dotnet/sdk:\S+@sha256:[0-9a-f]{64}$')


class RepositoryWiringTests(unittest.TestCase):
    workflow = (ROOT / '.github/workflows/release-toolset.yml').read_text(encoding='utf-8')

    def test_workflow_test_filters_match_the_local_release_gate(self):
        filters = re.findall(r'--filter "([^"]+)"', self.workflow)
        self.assertEqual(filters, [release.class_filter(release.TOOLSET_TEST_CLASSES), release.class_filter(release.SETUP_TEST_CLASSES)])
        self.assertIn('python -B -m unittest ' + ' '.join(release.PYTHON_TEST_MODULES), self.workflow)

    def test_workflow_publishes_through_the_shared_non_latest_code(self):
        self.assertIn('release-publish', self.workflow)
        self.assertNotIn('gh release create', self.workflow)
        self.assertNotIn('--latest', self.workflow)

    def test_nothing_in_the_toolset_links_or_downloads_through_github_latest(self):
        for relative in ('SWLOR.Toolset.Domain/Distribution/ToolsetFeed.cs', 'SWLOR.Toolset/README.md', 'README.md',
                         'tools/ToolsetDistribution.py', 'tools/toolset_release.py', '.github/workflows/release-toolset.yml'):
            text = (ROOT / relative).read_text(encoding='utf-8')
            self.assertNotIn('releases/latest', text, relative)
        feed = (ROOT / 'SWLOR.Toolset.Domain/Distribution/ToolsetFeed.cs').read_text(encoding='utf-8')
        self.assertIn('releases/download/toolset-latest/toolset-feed.json', feed)

    def test_local_entry_point_calls_the_shared_implementation(self):
        wrapper = (ROOT / 'ReleaseToolset.cmd').read_text(encoding='utf-8')
        self.assertIn(r'tools\ToolsetDistribution.py" release-local', wrapper)


if __name__ == '__main__':
    unittest.main()
