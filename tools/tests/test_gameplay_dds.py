"""DDS export regressions and decoded-pixel audit for the shipped icon corpus."""
import csv
import hashlib
import os
from pathlib import Path
import shutil
import struct
import sys
import subprocess
from unittest.mock import patch
import tempfile
import unittest

from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools'))
from ConvertGameplayIconsToDds import export, main


@unittest.skipUnless(shutil.which('magick'), 'Requires ImageMagick')
class ExportTests(unittest.TestCase):
    def test_asymmetric_opaque_and_alpha_sources_preserve_facing_and_compression(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for origin in (0, 1):
                for alpha in (False, True):
                    with self.subTest(origin=origin, alpha=alpha):
                        image = Image.new('RGBA', (32, 32), 'red')
                        image.paste('green', (0, 0, 16, 16))
                        image.paste('blue', (16, 16, 32, 32))
                        if alpha:
                            image.putpixel((31, 0), (255, 0, 0, 0))
                        source = root / 'asymmetric.tga'
                        image.save(source)
                        # Exercise both TGA origins with equivalent logical pixels.
                        data = bytearray(source.read_bytes())
                        if bool(data[17] & 32) != bool(origin):
                            stride = 32 * 4
                            rows = [data[18+y*stride:18+(y+1)*stride] for y in range(32)]
                            data[18:18+32*stride] = b''.join(reversed(rows))
                            data[17] ^= 32
                            source.write_bytes(data)
                        before = source.read_bytes()
                        row = export(source, root, root, 'magick')
                        self.assertEqual(source.read_bytes(), before)
                        self.assertEqual(row['Format'], 'DXT5' if alpha else 'DXT1')
                        dds = (root / 'asymmetric.dds').read_bytes()
                        self.assertEqual(len(dds), 1152 if alpha else 640)
                        self.assertEqual(struct.unpack_from('<I', dds, 28)[0], 1)
                        with Image.open(root / 'asymmetric.dds') as decoded:
                            visible = decoded.convert('RGBA').transpose(Image.Transpose.FLIP_TOP_BOTTOM)
                        for point in ((4,4),(24,4),(24,24)):
                            self.assertLess(max(abs(a-b) for a,b in zip(image.getpixel(point),visible.getpixel(point))), 10)
                        self.assertEqual(visible.getpixel((31,0))[3], 0 if alpha else 255)

    def test_case_only_rename_keeps_cached_runtime_pair(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / 'icons_source/production'
            runtime = root / 'icons'
            source.mkdir(parents=True)
            Image.new('RGB', (32, 32), 'red').save(source / 'ife_case.tga')
            manifest = source.parent / 'dds-conversions.csv'
            args = ['--source', str(source), '--output', str(runtime), '--manifest', str(manifest)]
            main(args)
            before = (runtime / 'ife_case.dds').read_bytes()
            (source / 'ife_case.tga').rename(source / 'IFE_CASE.tga')
            with patch('ConvertGameplayIconsToDds.export', side_effect=AssertionError('cache was missed')):
                main(args)
            self.assertEqual((runtime / 'IFE_CASE.dds').read_bytes(), before)
            self.assertEqual((runtime / 'IFE_CASE.txi').read_bytes(), b'mipmap 0\n')
            with manifest.open(newline='', encoding='utf-8') as stream:
                self.assertEqual(next(csv.DictReader(stream))['IconResRef'], 'IFE_CASE')
            self.assertEqual(len(list(runtime.glob('*.dds'))), 1)
            self.assertEqual(len(list(runtime.glob('*.txi'))), 1)

    @unittest.skipUnless(shutil.which('powershell'), 'Requires PowerShell')
    def test_required_audit_rejects_unmanifested_runtime_resources(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / 'icons_source/production'
            runtime = root / 'icons'
            source.mkdir(parents=True)
            Image.new('RGB', (32, 32), 'red').save(source / 'ife_valid.tga')
            main(['--source', str(source), '--output', str(runtime), '--manifest', str(source.parent / 'dds-conversions.csv')])
            helper = str(ROOT / 'tools/GameplayIconAssets.ps1').replace("'", "''")
            runtime_arg = str(runtime).replace("'", "''")
            command = f"$ErrorActionPreference = 'Stop'; . '{helper}'; Test-GameplayIconDdsExports '{runtime_arg}'"
            def audit():
                # Do not inherit another PowerShell version's module search path.
                environment = {key: value for key, value in os.environ.items() if key.upper() != 'PSMODULEPATH'}
                return subprocess.run(['powershell', '-NoProfile', '-Command', command], capture_output=True, text=True, env=environment)
            valid = audit()
            self.assertEqual(valid.returncode, 0, valid.stderr)
            for extension in ('.dds', '.txi'):
                with self.subTest(extension=extension):
                    orphan = runtime / ('ife_orphan' + extension)
                    orphan.write_bytes((runtime / ('ife_valid' + extension)).read_bytes())
                    invalid = audit()
                    self.assertNotEqual(invalid.returncode, 0)
                    self.assertIn('Unmanifested runtime icon', invalid.stderr)
                    orphan.unlink()


class CorpusTests(unittest.TestCase):
    def test_all_exported_assets_match_sources_and_preserve_decoded_orientation_alpha_and_size(self):
        assets = ROOT / 'SWLOR_Haks'
        source = assets / 'sw_ability_source/production'
        runtime = assets / 'sw_ability'
        self.assertEqual(list(runtime.glob('*.tga')), [])
        with (assets / 'sw_ability_source/dds-conversions.csv').open(newline='', encoding='utf-8') as stream:
            rows = list(csv.DictReader(stream))
        self.assertEqual({r['IconResRef'] for r in rows}, {p.stem for p in source.glob('*.tga')})
        for extension in ('.dds', '.txi'):
            self.assertEqual({r['IconResRef'].casefold() for r in rows}, {p.stem.casefold() for p in runtime.glob('*' + extension)})
        self.assertGreater(len(rows), 12000)
        for row in rows:
            with self.subTest(icon=row['IconResRef']):
                original = source / (row['IconResRef'] + '.tga')
                target = runtime / (row['IconResRef'] + '.dds')
                self.assertEqual(hashlib.sha256(original.read_bytes()).hexdigest(), row['SourceSHA256'])
                self.assertEqual(hashlib.sha256(target.read_bytes()).hexdigest(), row['DdsSHA256'])
                self.assertEqual(target.with_suffix('.txi').read_bytes(), b'mipmap 0\n')
                with Image.open(original) as image: a = image.convert('RGBA')
                with Image.open(target) as image: b = image.convert('RGBA').transpose(Image.Transpose.FLIP_TOP_BOTTOM)
                self.assertEqual(a.size, b.size)
                delta = ImageChops.difference(a,b)
                # Compression is lossy; the bound guards corrupt exports and inverted
                # asymmetric art. Compare visible colors (ignore fully clear pixels).
                visible = a.getchannel('A').point(lambda v: 255 if v else 0)
                means = ImageStat.Stat(delta, visible).mean
                self.assertLessEqual(max(means[:3]), 25)
                # BC3's six-alpha mode has up to 51 units between interpolated
                # levels; nearest-level rounding can differ by ceil(51/2)=26.
                self.assertLessEqual(delta.getchannel('A').getextrema()[1], 26)


if __name__ == '__main__':
    unittest.main()
