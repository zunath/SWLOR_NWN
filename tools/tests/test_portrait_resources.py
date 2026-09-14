"""Check conversion provenance, tamper rejection, and complete DDS coverage."""
import csv
import hashlib
from pathlib import Path
import sys
import struct
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from portrait_resources import PortraitResources, EXPECTED_CONVERSION_COUNT


def sha(data):
    return hashlib.sha256(data).hexdigest()


class PortraitResourceTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.portraits = self.root / 'sw_portrait'
        self.portraits.mkdir()
        self.manifest = self.root / 'portrait_dds_conversions.csv'
        self.output = self.portraits / 'example_l.dds'
        self.output.write_bytes(b'converted pixels')
        self.row = dict(file='example_l.tga', output=self.output.name,
                        source_sha256=sha(b'original pixels'),
                        output_sha256=sha(self.output.read_bytes()))
        self.write_manifest([self.row])

    def write_manifest(self, rows):
        with self.manifest.open('w', newline='', encoding='utf-8') as stream:
            writer = csv.DictWriter(stream, fieldnames=self.row.keys())
            writer.writeheader()
            writer.writerows(rows)

    def resources(self, count=1):
        return PortraitResources(self.portraits, expected_count=count)

    def test_converted_resource_checks_bytes_and_source_provenance(self):
        self.resources().verify(self.row['file'], sha(b'original pixels'))
        self.row['source_sha256'] = sha(b'different original')
        self.write_manifest([self.row])
        with self.assertRaisesRegex(ValueError, 'source hash changed'):
            self.resources().verify(self.row['file'], sha(b'original pixels'))

    def test_tampered_or_missing_dds_is_rejected(self):
        self.output.write_bytes(b'changed pixels')
        with self.assertRaisesRegex(ValueError, 'DDS portrait changed or missing'):
            self.resources().verify(self.row['file'], self.row['source_sha256'])
        self.output.unlink()
        with self.assertRaisesRegex(ValueError, 'DDS portrait changed or missing'):
            self.resources().verify(self.row['file'], self.row['source_sha256'])

    def test_retained_tga_cannot_hide_behind_conversion_provenance(self):
        (self.portraits / self.row['file']).write_bytes(b'changed original')
        with self.assertRaisesRegex(ValueError, 'source hash changed'):
            self.resources().verify(self.row['file'], self.row['source_sha256'])

    def test_missing_mapping_fails_closed(self):
        self.manifest.unlink()
        with self.assertRaisesRegex(ValueError, 'no recorded DDS conversion'):
            self.resources().verify(self.row['file'], self.row['source_sha256'])

    def test_incomplete_manifest_is_rejected(self):
        self.write_manifest([])
        with self.assertRaisesRegex(ValueError, 'Incomplete DDS conversion manifest'):
            self.resources()

    def test_duplicate_or_unsafe_names_are_rejected(self):
        for change in ({'file': '../example_l.tga'}, {'output': '..\\example_l.dds'},
                       {'output': 'different_l.dds'}, {'output': 'C:example_l.dds'}):
            with self.subTest(change=change):
                self.write_manifest([{**self.row, **change}])
                with self.assertRaisesRegex(ValueError, 'Unsafe or duplicate'):
                    self.resources()
        self.write_manifest([self.row, {**self.row, 'file': self.row['file'].upper()}])
        with self.assertRaisesRegex(ValueError, 'Unsafe or duplicate'):
            self.resources(count=2)

    def test_legacy_tga_without_manifest_still_verifies(self):
        self.manifest.unlink()
        (self.portraits / self.row['file']).write_bytes(b'original pixels')
        self.resources().verify(self.row['file'], self.row['source_sha256'])


class PortraitConversionCorpusTests(unittest.TestCase):
    def test_every_conversion_matches_shipped_resources(self):
        haks = Path(__file__).resolve().parents[2] / 'SWLOR_Haks'
        resources = PortraitResources(haks / 'sw_portrait')
        self.assertEqual(len(resources.conversions), EXPECTED_CONVERSION_COUNT)
        self.assertFalse([p.name for p in resources.root.iterdir() if p.suffix.lower() == '.tga'])
        actual_dds = {p.name.lower() for p in resources.root.iterdir() if p.suffix.lower() == '.dds'}
        recorded_dds = {r['output'].lower() for r in resources.conversions.values()}
        with (haks / 'portrait_size_repairs.csv').open(newline='', encoding='utf-8') as stream:
            repairs = list(csv.DictReader(stream))
        self.assertEqual(len(repairs), 10)
        repair_names = {row['file'] for row in repairs}
        self.assertEqual(len(repair_names), 10)
        self.assertFalse(repair_names & recorded_dds)
        self.assertSetEqual(actual_dds, recorded_dds | repair_names)
        self.assertFalse([name for name in actual_dds if Path(name).stem.endswith('h')])
        actual_txi = {p.name.lower() for p in resources.root.iterdir() if p.suffix.lower() == '.txi'}
        self.assertSetEqual(actual_txi, {str(Path(name).with_suffix('.txi')) for name in recorded_dds | repair_names})
        for row in repairs:
            with self.subTest(repair=row['file']):
                data = resources.path(row['file']).read_bytes()
                self.assertEqual(sha(data), row['sha256'])
                self.assertEqual(data[:4], b'DDS ')
                self.assertEqual(data[84:88], b'DXT1')
                width, height = int(row['width']), int(row['height'])
                self.assertEqual(struct.unpack_from('<II', data, 12), (height, width))
                self.assertEqual(len(data), 128 + ((width + 3) // 4) * ((height + 3) // 4) * 8)
                self.assertEqual(struct.unpack_from('<I', data, 28)[0], 1)
                self.assertTrue(struct.unpack_from('<I', data, 8)[0] & 0x20000)
                self.assertEqual(resources.path(row['file']).with_suffix('.txi').read_bytes(), b'mipmap 0\n')
        for row in resources.conversions.values():
            with self.subTest(portrait=row['file']):
                self.assertFalse(resources.path(row['file']).exists())
                resources.verify(row['file'], row['source_sha256'])
                output = resources.path(row['output'])
                data = output.read_bytes()
                self.assertEqual(len(data), int(row['output_bytes']))
                self.assertEqual(data[:4], b'DDS ')
                self.assertEqual(struct.unpack_from('<I', data, 4)[0], 124)
                flags = struct.unpack_from('<I', data, 8)[0]
                self.assertTrue(flags & 0x20000, 'DDSD_MIPMAPCOUNT must be explicit')
                self.assertEqual(struct.unpack_from('<I', data, 28)[0], 1)
                height, width = struct.unpack_from('<II', data, 12)
                self.assertEqual((width, height), (int(row['width']), int(row['height'])))
                self.assertGreater(width, 0)
                self.assertGreater(height, 0)
                self.assertIn(row['format'], ('DXT1', 'DXT5'))
                self.assertEqual(data[84:88], row['format'].encode('ascii'))
                block_bytes = 8 if row['format'] == 'DXT1' else 16
                self.assertEqual(len(data), 128 + ((width + 3) // 4) * ((height + 3) // 4) * block_bytes)
                self.assertEqual(output.with_suffix('.txi').read_bytes(), b'mipmap 0\n')


if __name__ == '__main__':
    unittest.main()
