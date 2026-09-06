"""Guard the complete Huge removal and each retained Large fallback."""
import csv
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from portrait_resources import PortraitResources

HAKS = Path(__file__).resolve().parents[2] / 'SWLOR_Haks'


class PortraitFallbackTests(unittest.TestCase):
    def test_every_removed_huge_has_its_recorded_large_fallback(self):
        with (HAKS / 'portrait_huge_removals.csv').open(newline='', encoding='utf-8') as stream:
            removed = list(csv.DictReader(stream))
        self.assertEqual(len(removed), 1027)
        self.assertEqual(len({row['file'].lower() for row in removed}), 1027)
        files = {path.name.lower(): path for path in (HAKS / 'sw_portrait').iterdir()
                 if path.suffix.lower() in ('.tga', '.dds')}
        resources = PortraitResources(HAKS / 'sw_portrait')
        self.assertFalse([name for name in files if Path(name).stem.endswith('h')])
        for row in removed:
            with self.subTest(portrait=row['file']):
                self.assertNotIn(row['file'].lower(), files)
                self.assertNotIn(Path(row['file']).with_suffix('.dds').name.lower(), files)
                self.assertEqual(row['fallback'].lower(), row['file'][:-5].lower() + 'l.tga')
                resources.verify(row['fallback'], row['fallback_sha256'])


if __name__ == '__main__':
    unittest.main()
