"""Guard the Wildlands willow foliage against opaque DDS conversions."""
from pathlib import Path
import struct
import unittest

from PIL import Image


TEXTURE = (Path(__file__).resolve().parents[2]
           / "SWLOR_Haks/sw_plc/plc_wrm_willow.dds")


class WillowTextureTests(unittest.TestCase):
    def test_foliage_dds_preserves_cutouts_and_mipmaps(self):
        data = TEXTURE.read_bytes()
        self.assertEqual(data[:4], b"DDS ")
        self.assertEqual(data[84:88], b"DXT5")
        self.assertEqual(struct.unpack_from("<II", data, 12), (256, 256))
        self.assertEqual(struct.unpack_from("<I", data, 28)[0], 9)
        expected_bytes = 128 + sum(max(1, (256 >> level) // 4) ** 2 * 16
                                   for level in range(9))
        self.assertEqual(len(data), expected_bytes)
        self.assertFalse(TEXTURE.with_suffix(".tga").exists(),
                         "A duplicate TGA can shadow the DDS resource")
        with Image.open(TEXTURE) as image:
            alpha = image.convert("RGBA").getchannel("A")
            histogram = alpha.histogram()
            pixels = image.width * image.height
            # The original has ~58% fully transparent background and ~40%
            # opaque foliage. A decoder honoring its erroneous TGA extension
            # alpha-type 0 instead produces an entirely opaque texture.
            self.assertGreater(histogram[0] / pixels, 0.5)
            self.assertGreater(histogram[255] / pixels, 0.3)


if __name__ == "__main__":
    unittest.main()
