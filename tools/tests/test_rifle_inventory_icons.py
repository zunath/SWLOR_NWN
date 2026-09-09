"""Verify native-compatible DDS inventory artwork for the imported rifles."""
from pathlib import Path
import struct
import unittest

from PIL import Image

WEAPONS = Path(__file__).resolve().parents[2] / "SWLOR_Haks/sw_weapon"
PARTS = (115, 116, 117, 118, 119, 121, 122, 124, 125, 201, 202, 205,
         207, 208, 210, 211, 213, 218, 219, 220, 221, 222, 301, 302,
         303, 305, 307, 308, 309, 311, 313, 318, 319, 419, 420)


class RifleInventoryIconTests(unittest.TestCase):
    def test_imported_icons_use_native_dxt5_with_alpha_without_mipmaps(self):
        for part in PARTS:
            with self.subTest(part=part):
                stem = f"iwbwxl_m_{part % 100 * 10 + part // 100:03}"
                path = WEAPONS / (stem + ".dds")
                data = path.read_bytes()
                self.assertEqual(data[:4], b"DDS ")
                self.assertEqual(data[84:88], b"DXT5")
                self.assertIn(struct.unpack_from("<I", data, 28)[0], (0, 1))
                self.assertEqual(len(data), 128 + 64 * 128)
                self.assertFalse((WEAPONS / (stem + ".tga")).exists())
                with Image.open(path) as image:
                    self.assertEqual(image.size, (64, 128))
                    alpha = image.convert("RGBA").getchannel("A")
                    self.assertIsNotNone(alpha.getbbox())
                    self.assertEqual(alpha.getextrema()[0], 0)


if __name__ == "__main__":
    unittest.main()
