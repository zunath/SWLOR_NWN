"""Exercise manifest completeness and replay through the public CLI."""
import csv
import hashlib
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from portrait_tga import flip_horizontal

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
        (self.root / 'reference_h.tga').write_bytes(self.original)
        self.rows = []
        # The published repair batch contains 256 targets. This fixture tests
        # the CLI contract independently of its expected-count constant.
        for index in range(256):
            name = f'portrait{index}_m.tga'
            (self.root / name).write_bytes(self.original)
            self.rows.append(dict(file=name, canonical='reference_h.tga',
                                  original_sha256=sha(self.original),
                                  corrected_sha256=sha(self.corrected),
                                  canonical_sha256=sha(self.original)))

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
        for row in self.rows:
            self.assertEqual((self.root / row['file']).read_bytes(), self.original)

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


if __name__ == '__main__':
    unittest.main()
