import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
import argparse
import zipfile
from types import SimpleNamespace
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import toolset_common as common
import ToolsetDistribution as distribution
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'scripts'))
import SetupRepository as setup

class ToolsetDistributionTests(unittest.TestCase):
    def test_linux_runtime_uses_native_names(self):
        with patch.object(common, 'os', SimpleNamespace(name='posix', environ=os.environ, pathsep=':')), patch.object(common.platform, 'system', return_value='Linux'), patch.object(common.platform, 'machine', return_value='x86_64'):
            self.assertEqual(common.runtime_id(), 'linux-x64')
            self.assertEqual(common.executable('SWLOR.CLI'), 'SWLOR.CLI')

    def test_unsupported_architecture_has_a_clear_error(self):
        with patch.object(common.platform, 'machine', return_value='aarch64'):
            with self.assertRaisesRegex(RuntimeError, 'x64'):
                common.runtime_id()

    def test_output_cannot_overwrite_repository_or_existing_directory(self):
        with self.assertRaisesRegex(RuntimeError, 'outside'):
            common.new_output(common.ROOT / 'output')
        with tempfile.TemporaryDirectory() as temporary:
            with self.assertRaises(FileExistsError):
                common.new_output(temporary)

    def test_file_hash_matches_sha256(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'data'
            path.write_bytes(b'compiler binary')
            self.assertEqual(common.file_hash(path), hashlib.sha256(b'compiler binary').hexdigest())

    def test_native_environment_does_not_mutate_parent(self):
        with patch.dict(os.environ, {'PATH': '/bin', 'LD_LIBRARY_PATH': '/existing'}, clear=True):
            with patch.object(common, 'os', SimpleNamespace(name='posix', environ=os.environ, pathsep=':')):
                environment = common.native_environment('/native')
            self.assertEqual(environment['LD_LIBRARY_PATH'], '/native:/existing')
            self.assertEqual(environment['SWLOR_NATIVE_TOOLS'], '/native')
            self.assertEqual(os.environ['PATH'], '/bin')

    def test_linux_download_rejects_incorrect_hash_before_extraction(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary) / 'cache'
            def download(url, destination):
                destination.write_bytes(b'wrong archive')
            with patch.object(common, 'native_cache', return_value=cache), patch.object(common, 'os', SimpleNamespace(name='posix', environ=os.environ, pathsep=':')), patch.object(common, 'system_library', return_value=('lib.so', Path('/unused'))), patch.object(common.urllib.request, 'urlretrieve', side_effect=download):
                with self.assertRaisesRegex(RuntimeError, 'SHA-256'):
                    common.prepare_native_tools()
            self.assertFalse(cache.exists())

    def test_linux_cache_is_reused_only_with_all_runtime_libraries(self):
        with tempfile.TemporaryDirectory() as temporary:
            cache = Path(temporary)
            names = (*common.NATIVE_NAMES, 'libnwnscriptcomp.so', 'libpcre.so.3', 'libsqlite3.so.0')
            for name in names:
                (cache / name).write_bytes(name.encode())
            hashes = {name: common.file_hash(cache / name) for name in names}
            marker = cache / 'native-tools.json'
            marker.write_text(json.dumps({'Version': common.NATIVE_VERSION, 'Files': hashes}))
            with patch.object(common, 'native_cache', return_value=cache), patch.object(common, 'os', SimpleNamespace(name='posix', environ=os.environ, pathsep=':')), patch.object(common, 'system_library', side_effect=RuntimeError('cache rejected')):
                self.assertEqual(common.prepare_native_tools(), cache)
                (cache / 'libnwnscriptcomp.so').write_bytes(b'changed')
                with self.assertRaisesRegex(RuntimeError, 'cache rejected'):
                    common.prepare_native_tools()

    def test_linux_packaging_copies_native_libraries_and_executable_modes(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source, target = root / 'source', root / 'target'
            source.mkdir()
            target.mkdir()
            native = source / 'nwn_erf'
            native.write_bytes(b'ELF')
            native.chmod(0o755)
            (source / 'libsqlite3.so.0').write_bytes(b'SQLite')
            (source / 'notes.txt').write_text('notice')
            distribution.copy_tools(source, target)
            self.assertEqual((target / 'nwn_erf').read_bytes(), b'ELF')
            self.assertTrue((target / 'libsqlite3.so.0').is_file())
            if os.name != 'nt':
                self.assertEqual((target / 'nwn_erf').stat().st_mode & 0o777, 0o755)

    def test_windows_setup_and_packaging_report_missing_native_dlls(self):
        for missing in ('pcre64.dll', 'libnwnscriptcomp.dll'):
            with self.subTest(missing=missing), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                tools = root / 'tools/SWLOR.CLI'
                tools.mkdir(parents=True)
                for name in common.WINDOWS_NATIVE_FILES:
                    if name != missing:
                        (tools / name).write_bytes(b'utility')
                with patch.object(common, 'ROOT', root), patch.object(common, 'os', SimpleNamespace(name='nt')):
                    with self.assertRaisesRegex(RuntimeError, missing):
                        common.prepare_native_tools()
                with patch.object(setup, 'ROOT', root), patch.object(setup, 'os', SimpleNamespace(name='nt')), patch.object(setup.shutil, 'which', return_value='tool'), patch.object(setup, 'run', return_value='10.0.402'):
                    failures = setup.check('Toolset')
                    self.assertEqual(len(failures), 1)
                    self.assertIn(missing, failures[0])

    def test_revision_pinned_packaging_rejects_local_edits_before_building(self):
        with patch.object(distribution.content, 'git', return_value=' M source'), patch.object(distribution, 'prepare_native_tools') as prepare:
            with self.assertRaisesRegex(RuntimeError, 'Commit or discard'):
                distribution.publish(argparse.Namespace(skip_workspace=False))
            prepare.assert_not_called()

    def test_apply_has_no_forced_timeout_but_read_only_checks_are_bounded(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive, executable = root / 'contribution.zip', root / 'toolset'
            executable.write_bytes(b'executable')
            with zipfile.ZipFile(archive, 'w') as bundle:
                bundle.writestr('contribution.json', json.dumps({'AssetRevision': '2' * 40}))
            for apply in (False, True):
                args = argparse.Namespace(archive=str(archive), toolset=str(executable), apply=apply)
                with patch.object(distribution.content, 'git', side_effect=['feature', '']), patch.object(distribution.content, 'revisions', return_value=('1' * 40, '2' * 40)), patch.object(distribution, 'run') as run:
                    distribution.import_contribution(args)
                    self.assertEqual(run.call_args.kwargs['timeout'], None if apply else 120)

if __name__ == '__main__':
    unittest.main()
