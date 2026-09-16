"""Check repaired DDS alpha against statistics from the original TGA pixels.

The source TGAs' extension metadata incorrectly suppressed their stored alpha.
The baseline records the pixel data, without applying that extension override.
Per-mip alpha hashes come from re-encoding those original pixels with the
recorded ImageMagick contract (tools/GenerateWorldTextureMipBaseline.py).
Requires Pillow and initialized HAK sources; no Git history is needed to test.
"""
import hashlib
import io
import json
from pathlib import Path
import struct
import unittest

from PIL import Image


ROOT = Path(__file__).resolve().parents[2] / "SWLOR_Haks"
BASELINE = json.loads(Path(__file__).with_name("world_texture_alpha.json").read_text())


class WorldTextureAlphaTests(unittest.TestCase):
    def test_repaired_textures_keep_source_alpha_and_complete_mipmaps(self):
        for expected in BASELINE["textures"]:
            with self.subTest(texture=expected["texture"]):
                path = ROOT / expected["texture"]
                data = path.read_bytes()
                width, height = expected["width"], expected["height"]
                self.assertEqual(data[:4], b"DDS ")
                self.assertEqual(data[84:88], b"DXT5")
                self.assertEqual(struct.unpack_from("<II", data, 12), (height, width))
                levels = max(width, height).bit_length()
                self.assertEqual(struct.unpack_from("<I", data, 28)[0], levels)
                self.assertEqual(len(expected["mip_alpha_sha256"]), levels)
                self.assertFalse(path.with_suffix(".tga").exists())
                with Image.open(path) as image:
                    histogram = image.getchannel("A").histogram()
                    pixels = width * height
                    self.assertLess(image.getchannel("A").getextrema()[0], 255)
                    self.assertAlmostEqual(sum(i * n for i, n in enumerate(histogram)) / pixels,
                                           expected["alpha_mean"], delta=2)
                    for value, key in ((0, "transparent_fraction"), (255, "opaque_fraction")):
                        self.assertAlmostEqual(histogram[value] / pixels, expected[key], delta=0.05)
                cursor = 128
                for level in range(levels):
                    w, h = max(1, width >> level), max(1, height >> level)
                    size = ((w + 3) // 4) * ((h + 3) // 4) * 16
                    header = bytearray(data[:128])
                    struct.pack_into("<III", header, 12, h, w, size)
                    struct.pack_into("<I", header, 28, 1)
                    with Image.open(io.BytesIO(header + data[cursor:cursor + size])) as mip:
                        mip.load()
                        self.assertEqual(mip.size, (w, h))
                        self.assertEqual(
                            hashlib.sha256(mip.getchannel("A").tobytes()).hexdigest(),
                            expected["mip_alpha_sha256"][level],
                            f"Mip {level} alpha differs from the source-derived DXT5 baseline")
                    cursor += size
                self.assertEqual(cursor, len(data), "Truncated or unexpected mip data")


if __name__ == "__main__":
    unittest.main()
