"""Run with: python -m unittest discover -s tools/tests -p test_fix_weapon_inventory_icons.py"""
import importlib.util
import struct
import tempfile
import unittest
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('fix_weapon_inventory_icons', ROOT / 'tools/FixWeaponInventoryIcons.py')
icons = importlib.util.module_from_spec(spec)
spec.loader.exec_module(icons)
codec = icons._codec()


def icon_info(width, height):
    header = bytearray(128)
    header[:4] = b'DDS '
    struct.pack_into('<IIIII', header, 4, 124, 0x000A1007, height, width, 0)
    struct.pack_into('<I', header, 28, 1)
    struct.pack_into('<I', header, 76, 32)
    struct.pack_into('<I', header, 80, 4)
    header[84:88] = b'DXT5'
    return {'header': bytes(header), 'fourcc': 'DXT5', 'mips': 1, 'size': (width, height)}


def bar(width, height, top, bottom):
    """Opaque vertical bar between rows top and bottom (top-down) on a transparent canvas."""
    rgba = np.zeros((height, width, 4), np.float32)
    rgba[top:bottom + 1, width // 2 - 2:width // 2 + 2] = (.7, .7, .75, 1)
    return rgba


class InventoryIconTests(unittest.TestCase):
    def test_stub_is_small_bottom_left_tga_of_the_art(self):
        data = icons.tga_bytes(icons.thumbnail(bar(64, 128, 0, 63)))
        width, height, depth, descriptor = struct.unpack_from('<HHBB', data, 12)
        self.assertEqual((width, height, depth, descriptor), (8, 8, 32, 8))
        self.assertEqual(len(data), 18 + 8 * 8 * 4)
        alpha = np.frombuffer(data[18:], np.uint8).reshape(8, 8, 4)[..., 3]
        # Bottom-left origin: the art in the top half lands in the last stored rows.
        self.assertGreater(alpha[4:].sum(), 0)
        self.assertEqual(alpha[:4].sum(), 0)

    def test_dds_icon_round_trip_keeps_orientation(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'iwswgs_t_011.dds'
            art = bar(64, 256, 10, 100)
            icons.write_icon(codec, path, art, icon_info(64, 256))
            decoded, _ = icons.read_icon(codec, path)
            rows = np.where((decoded[..., 3] > .5).any(1))[0]
            self.assertEqual((rows.min(), rows.max()), (10, 100))

    def test_reframe_maps_rows_and_keeps_center(self):
        out = icons.reframe(bar(64, 256, 10, 249), (10, 249), (4, 155))
        rows = np.where((out[..., 3] > .5).any(1))[0]
        cols = np.where((out[..., 3] > .5).any(0))[0]
        self.assertLessEqual(abs(int(rows.min()) - 4), 1)
        self.assertLessEqual(abs(int(rows.max()) - 155), 1)
        self.assertLessEqual(abs((cols.min() + cols.max()) / 2 - 31.5), 1)

    def test_transparent_texels_take_neighbor_color(self):
        art = bar(16, 16, 0, 15)
        bled = icons.bleed_transparent_color(art)
        np.testing.assert_allclose(bled[..., 3], art[..., 3])
        self.assertGreater(float(bled[0, 0, :3].sum()), 0)


if __name__ == '__main__':
    unittest.main()
