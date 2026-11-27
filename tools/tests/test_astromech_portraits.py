"""Tests for tools/BuildAstromechPortraits.py using synthetic captures."""
import json
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import BuildAstromechPortraits as portraits  # noqa: E402


def scene(seed=7, size=(360, 480)):
    """A textured backdrop that is the same in both captures."""
    rng = np.random.default_rng(seed)
    base = rng.integers(90, 170, (size[0] // 8, size[1] // 8, 3), dtype=np.uint8)
    return np.asarray(Image.fromarray(base).resize((size[1], size[0]), Image.BILINEAR)).copy()


def disc(shape, centre, radius):
    yy, xx = np.mgrid[0:shape[0], 0:shape[1]]
    return (yy - centre[0]) ** 2 + (xx - centre[1]) ** 2 <= radius ** 2


class SubjectMaskTests(unittest.TestCase):
    def test_the_subject_is_cut_from_the_backdrop(self):
        plate = scene()
        subject = disc(plate.shape, (200, 240), 90)
        visible = plate.copy()
        visible[subject] = (220, 40, 40)
        mask = portraits.subject_mask(visible, plate)
        overlap = (mask & subject).sum() / (mask | subject).sum()
        self.assertGreater(overlap, 0.95)

    def test_a_shadow_in_either_capture_is_not_the_subject(self):
        plate = scene()
        subject = disc(plate.shape, (200, 200), 70)
        cast = disc(plate.shape, (230, 360), 50)        # a shadow only the visible capture has
        moved = disc(plate.shape, (90, 380), 40)        # a shadow only the plate has
        visible = plate.copy()
        visible[cast] = (visible[cast] * 0.6).astype(np.uint8)
        plate = plate.copy()
        plate[moved] = (plate[moved] * 0.6).astype(np.uint8)
        visible[subject] = (30, 60, 200)
        mask = portraits.subject_mask(visible, plate)
        self.assertLess(mask[cast].mean(), 0.05)
        self.assertLess(mask[moved].mean(), 0.05)
        self.assertGreater(mask[subject].mean(), 0.95)

    def test_specks_and_holes_do_not_change_the_subject(self):
        plate = scene()
        subject = disc(plate.shape, (180, 240), 80)
        visible = plate.copy()
        visible[subject] = (240, 240, 60)
        hole = disc(plate.shape, (180, 240), 12)
        visible[hole] = plate[hole]                      # a part of the subject that matches the plate
        speck = disc(plate.shape, (40, 40), 6)
        visible[speck] = (255, 0, 255)
        mask = portraits.subject_mask(visible, plate)
        self.assertGreater(mask[hole].mean(), 0.95)
        self.assertEqual(mask[speck].sum(), 0)

    def test_identical_captures_have_no_subject(self):
        plate = scene()
        self.assertFalse(portraits.subject_mask(plate, plate).any())


class BuildTests(unittest.TestCase):
    def write_pair(self, root, name='astro01', colour=(40, 200, 90)):
        plate = scene()
        visible = plate.copy()
        visible[disc(plate.shape, (220, 240), 100)] = colour
        Image.fromarray(visible).save(root / f'{name}_visible.png')
        Image.fromarray(plate).save(root / f'{name}_plate.png')
        return {'name': name, 'visible': f'{name}_visible.png', 'plate': f'{name}_plate.png'}

    def manifest(self, root, entries, crop=(0, 0, 480, 360)):
        path = root / 'portraits.json'
        path.write_text(json.dumps({'schema_version': 1, 'crop': list(crop), 'portraits': entries}))
        return path

    def test_every_size_is_written_in_the_stored_layout(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            manifest = self.manifest(root, [self.write_pair(root)])
            written = portraits.build(manifest, root / 'out')
            self.assertEqual(sorted(written), ['po_astro01_l', 'po_astro01_m', 'po_astro01_s', 'po_astro01_t'])
            for letter, (width, stored, visible) in portraits.SIZES.items():
                data = (root / 'out' / f'po_astro01_{letter}.dds').read_bytes()
                self.assertEqual(data[:4], b'DDS ')
                self.assertEqual(data[84:88], b'DXT1')
                self.assertEqual(len(data), 128 + width * stored // 2, 'one image level, no mip chain')
                image = np.asarray(Image.open(root / 'out' / f'po_astro01_{letter}.dds').convert('RGB'))
                self.assertEqual(image.shape[:2], (stored, width))
                self.assertLess(image[:stored - visible].max(), 12, 'the unused rows are stored first, in black')
                self.assertGreater(image[stored - visible:].mean(), 20)
                self.assertEqual((root / 'out' / f'po_astro01_{letter}.txi').read_text(), 'mipmap 0\n')

    def test_the_subject_stands_on_the_bottom_of_the_picture(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            manifest = self.manifest(root, [self.write_pair(root, colour=(40, 200, 90))])
            portraits.build(manifest, root / 'out')
            stored = np.asarray(Image.open(root / 'out' / 'po_astro01_l.dds').convert('RGB')).astype(int)
            upright = stored[::-1][:200]
            green = (upright[..., 1] > 150) & (upright[..., 0] < 110)
            rows = np.nonzero(green.any(axis=1))[0]
            self.assertGreater(rows.max(), 190, 'the subject reaches the bottom edge')
            self.assertGreater(rows.min(), 20, 'and leaves room above it')

    def test_bad_manifests_are_refused(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            entry = self.write_pair(root)
            cases = [
                {'schema_version': 2, 'crop': [0, 0, 10, 10], 'portraits': [entry]},
                {'schema_version': 1, 'crop': [10, 0, 5, 10], 'portraits': [entry]},
                {'schema_version': 1, 'crop': [0, 0, 10, 10], 'portraits': []},
                {'schema_version': 1, 'crop': [0, 0, 10, 10], 'portraits': [dict(entry, name='Astro 01')]},
                {'schema_version': 1, 'crop': [0, 0, 10, 10], 'portraits': [dict(entry, name='astromech0001')]},
                {'schema_version': 1, 'crop': [0, 0, 10, 10], 'portraits': [entry, entry]},
                {'schema_version': 1, 'crop': [0, 0, 10, 10], 'portraits': [dict(entry, plate='missing.png')]},
            ]
            for case in cases:
                path = root / 'bad.json'
                path.write_text(json.dumps(case))
                with self.assertRaises(ValueError):
                    portraits.load_manifest(path)

    def test_a_pair_without_a_subject_is_refused(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            plate = scene()
            Image.fromarray(plate).save(root / 'a_visible.png')
            Image.fromarray(plate).save(root / 'a_plate.png')
            manifest = self.manifest(root, [{'name': 'a', 'visible': 'a_visible.png', 'plate': 'a_plate.png'}])
            with self.assertRaises(ValueError):
                portraits.build(manifest, root / 'out')

    def test_an_output_directory_with_files_is_refused(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            manifest = self.manifest(root, [self.write_pair(root)])
            (root / 'out').mkdir()
            (root / 'out' / 'old.txt').write_text('x')
            with self.assertRaises(ValueError):
                portraits.build(manifest, root / 'out')


if __name__ == '__main__':
    unittest.main()
