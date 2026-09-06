"""Exercise manifest completeness and replay through the public CLI."""
import csv
from contextlib import redirect_stdout
import hashlib
import io
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from portrait_tga import flip_horizontal
from portrait_resources import PortraitResources
import NormalizePortraitOrientations as normalizer

SCRIPT = Path(__file__).resolve().parents[1] / 'NormalizePortraitOrientations.py'
FIELDS = ['file', 'canonical', 'original_sha256', 'corrected_sha256', 'canonical_sha256']


def sha(data):
    return hashlib.sha256(data).hexdigest()


class PortraitManifestTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.manifest = self.root / 'corrections.csv'
        header = bytearray(18)
        header[2] = 2
        struct.pack_into('<HH', header, 12, 2, 1)
        header[16] = 24
        self.original = bytes(header) + b'abcdef'
        self.corrected = flip_horizontal(self.original)
        (self.root / 'reference_l.tga').write_bytes(self.original)
        self.rows = []
        # The published repair batch contains 256 targets. This fixture tests
        # the CLI contract independently of its expected-count constant.
        for index in range(256):
            name = f'portrait{index}_m.tga'
            (self.root / name).write_bytes(self.original)
            self.rows.append(dict(file=name, canonical='reference_l.tga',
                                  original_sha256=sha(self.original),
                                  corrected_sha256=sha(self.corrected),
                                  canonical_sha256=sha(self.original)))
        # Manifest tests deliberately change filenames/casing. Assertions must
        # still inspect the exact paths setup created on case-sensitive hosts.
        self.original_paths = tuple(self.root / row['file'] for row in self.rows)

    def write_manifest(self, rows):
        with self.manifest.open('w', newline='', encoding='utf-8') as stream:
            writer = csv.DictWriter(stream, fieldnames=FIELDS)
            writer.writeheader()
            writer.writerows(rows)

    def run_cli(self, *options):
        return subprocess.run([sys.executable, str(SCRIPT), '--portraits', str(self.root),
                               '--manifest', str(self.manifest), *options],
                              capture_output=True, text=True, timeout=30)

    def assert_originals_untouched(self):
        for path in self.original_paths:
            self.assertEqual(path.read_bytes(), self.original)

    def test_empty_and_truncated_manifests_fail_before_any_write(self):
        for count in (0, 1, 255):
            self.write_manifest(self.rows[:count])
            for options in ((), ('--apply',)):
                with self.subTest(count=count, options=options):
                    result = self.run_cli(*options)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn('Incomplete correction manifest', result.stderr)
                    self.assertNotIn('Verified', result.stdout)
                    self.assert_originals_untouched()

    def test_missing_header_and_extra_rows_do_not_report_success(self):
        self.manifest.write_text('', encoding='utf-8')
        self.assertNotEqual(self.run_cli().returncode, 0)
        self.write_manifest(self.rows + [self.rows[0]])
        self.assertNotEqual(self.run_cli('--apply').returncode, 0)
        self.assert_originals_untouched()

    def test_complete_manifest_applies_and_verifies_idempotently(self):
        self.write_manifest(self.rows)
        first = self.run_cli('--apply')
        self.assertEqual(first.returncode, 0, first.stderr)
        self.assertIn('applied 256 lossless flips', first.stdout)
        for row in self.rows:
            self.assertEqual((self.root / row['file']).read_bytes(), self.corrected)
        second = self.run_cli('--apply')
        self.assertEqual(second.returncode, 0, second.stderr)
        self.assertIn('applied 0 lossless flips', second.stdout)
        self.assertEqual(self.run_cli().returncode, 0)

    def test_stale_temporary_file_rejects_entire_batch_before_writes(self):
        self.write_manifest(self.rows)
        stale = (self.root / self.rows[-1]['file']).with_suffix('.tga.portrait-tmp')
        stale.write_bytes(b'recovery data from an interrupted run')
        for _ in range(2):
            result = self.run_cli('--apply')
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('Existing temporary file', result.stderr)
            self.assertIn('no portraits were changed', result.stderr)
            self.assert_originals_untouched()
            self.assertEqual(stale.read_bytes(), b'recovery data from an interrupted run')
        stale.unlink()
        result = self.run_cli('--apply')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('applied 256 lossless flips', result.stdout)

    def use_pending_canonical(self):
        # Put the self-canonical target last so dependents preflight first.
        self.rows[-1]['file'] = 'reference_l.tga'
        for row in self.rows:
            row['canonical_sha256'] = sha(self.corrected)
        self.write_manifest(self.rows)

    def test_pending_self_canonical_applies_and_verifies_idempotently(self):
        self.use_pending_canonical()
        self.assertNotEqual(self.run_cli().returncode, 0)
        self.assert_originals_untouched()
        result = self.run_cli('--apply')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('applied 256 lossless flips', result.stdout)
        for row in self.rows:
            self.assertEqual((self.root / row['file']).read_bytes(), self.corrected)
        result = self.run_cli('--apply')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('applied 0 lossless flips', result.stdout)
        self.assertEqual(self.run_cli().returncode, 0)

    def test_corrected_dependents_accept_pending_canonical_on_apply(self):
        self.use_pending_canonical()
        for row in self.rows[:128]:
            (self.root / row['file']).write_bytes(self.corrected)
        result = self.run_cli('--apply')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('applied 128 lossless flips', result.stdout)
        self.assertEqual(self.run_cli().returncode, 0)

    def test_tampered_pending_canonical_fails_before_any_write(self):
        self.use_pending_canonical()
        # Queue other repairs before encountering the tampered canonical.
        for row in self.rows[:-1]:
            row['canonical'] = 'untouched_l.tga'
            row['canonical_sha256'] = sha(self.original)
        (self.root / 'untouched_l.tga').write_bytes(self.original)
        (self.root / 'reference_l.tga').write_bytes(b'tampered')
        self.write_manifest(self.rows)
        result = self.run_cli('--apply')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Canonical portrait changed', result.stderr)
        for row in self.rows[:-1]:
            self.assertEqual((self.root / row['file']).read_bytes(), self.original)
        self.assertEqual((self.root / 'reference_l.tga').read_bytes(), b'tampered')

    def test_duplicate_targets_fail_before_image_preflight(self):
        self.rows[-1]['file'] = self.rows[0]['file'].upper()
        # This missing reference would fail first without up-front validation.
        (self.root / 'reference_l.tga').unlink()
        self.write_manifest(self.rows)
        result = self.run_cli('--apply')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Unsafe or duplicate manifest filename', result.stderr)
        self.assert_originals_untouched()

    def test_pending_canonical_output_is_validated_before_any_write(self):
        self.use_pending_canonical()
        self.rows[-1]['corrected_sha256'] = '0' * 64
        for row in self.rows:
            row['canonical_sha256'] = '0' * 64
        self.write_manifest(self.rows)
        result = self.run_cli('--apply')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Correction does not match reviewed output', result.stderr)
        self.assert_originals_untouched()

    def converted_resources(self, source):
        name = self.rows[0]['file']
        output = Path(name).with_suffix('.dds').name
        (self.root / name).unlink()
        (self.root / output).write_bytes(b'compressed reviewed image')
        conversion = dict(file=name, output=output, source_sha256=sha(source),
                          output_sha256=sha(b'compressed reviewed image'))
        manifest = self.root / 'conversions.csv'
        with manifest.open('w', newline='', encoding='utf-8') as stream:
            writer = csv.DictWriter(stream, fieldnames=conversion.keys())
            writer.writeheader()
            writer.writerow(conversion)
        return PortraitResources(self.root, manifest, expected_count=1)

    def run_with_resources(self, resources, *options):
        with patch.object(normalizer, 'PortraitResources', return_value=resources), \
                patch.object(sys, 'argv', [str(SCRIPT), '--portraits', str(self.root),
                                          '--manifest', str(self.manifest), *options]), \
                redirect_stdout(io.StringIO()) as output:
            normalizer.main()
        return output.getvalue()

    def test_corrected_dds_verifies_and_apply_is_idempotent(self):
        self.write_manifest(self.rows)
        result = self.run_cli('--apply')
        self.assertEqual(result.returncode, 0, result.stderr)
        resources = self.converted_resources(self.corrected)
        for options in ((), ('--apply',)):
            self.assertIn('applied 0 lossless flips', self.run_with_resources(resources, *options))

    def test_dds_needing_reflection_requires_original_tga(self):
        self.write_manifest(self.rows)
        resources = self.converted_resources(self.original)
        with self.assertRaisesRegex(ValueError, 'Restore the original TGA'):
            self.run_with_resources(resources, '--apply')
        for path in self.original_paths[1:]:
            self.assertEqual(path.read_bytes(), self.original)


if __name__ == '__main__':
    unittest.main()
