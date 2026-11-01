"""Audit pistol artwork: python -m unittest discover -s tools/tests -p test_pistol_inventory_icons.py.

Requires Pillow and an initialized SWLOR_Haks submodule; no authoring inputs.
This verifies resources, not the native client's inventory discovery behavior.
"""
import hashlib
from pathlib import Path
import re
import unittest

from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parents[2]
WEAPONS = ROOT / "SWLOR_Haks/sw_weapon"
CATALOG = ROOT / "SWLOR.Game.Server/Feature/AppearanceDefinition/ItemAppearance/PistolAppearanceDefinition.cs"


class PistolInventoryIconTests(unittest.TestCase):
    def test_catalog_icons_exist_are_visible_and_unique(self):
        text = re.sub(r"//[^\n]*", "", CATALOG.read_text())
        middle = re.search(r"MiddleParts.*?\{(.*?)\};", text, re.S)
        self.assertIsNotNone(middle, "Could not read pistol MiddleParts")
        parts = [int(value) for value in re.findall(r"\b\d+\b", middle.group(1))]
        self.assertTrue(parts, "Pistol catalog must not be empty")
        seen = {}
        for part in parts:
            with self.subTest(part=part):
                slot = part % 100 * 10 + part // 100
                stem = f"iwbwsh_m_{slot:03}"
                self.assertTrue((WEAPONS / f"wbwsh_m_{slot:03}.mdl").is_file())
                dds = WEAPONS / f"{stem}.dds"
                tga = WEAPONS / f"{stem}.tga"
                # Existing legacy TGA artwork remains auditable. New/replaced icons
                # ship DDS; the workflow requires a separate native-client check.
                path = dds if dds.is_file() else tga
                self.assertTrue(path.is_file(), f"{stem}: missing inventory artwork")
                with Image.open(path) as source:
                    icon = source.convert("RGBA")
                if path.suffix == ".dds":
                    header = path.read_bytes()[:128]
                    self.assertEqual(header[:4], b"DDS ")
                    self.assertEqual(header[84:88], b"DXT5",
                                     "Native CResDDS does not support uncompressed RGBA icons; use DXT5 with alpha")
                    icon = ImageOps.flip(icon)
                    if tga.exists():
                        with Image.open(tga) as registration:
                            self.assertEqual(registration.size, (8, 8),
                                             f"{stem}: only a registration thumbnail may accompany DDS artwork")
                            self.assertIsNotNone(registration.convert("RGBA").getchannel("A").getbbox())
                        data = tga.read_bytes()
                        self.assertEqual(len(data), 18 + 8 * 8 * 4)
                        self.assertEqual(data[2], 2, "Registration must be an uncompressed TGA")
                        self.assertEqual(data[16:18], bytes((32, 8)),
                                         "Registration must use 32-bit pixels and bottom-left origin")
                self.assertEqual(icon.size, (64, 64))
                alpha = icon.getchannel("A")
                self.assertIsNotNone(alpha.getbbox(), "Middle icon must contain visible artwork")
                self.assertEqual(alpha.getextrema()[0], 0, "Icon needs a transparent background")
                # Ignore hidden RGB: invisible pixels cannot make reused artwork unique.
                visible = Image.alpha_composite(Image.new("RGBA", icon.size), icon)
                digest = hashlib.sha256(visible.tobytes()).hexdigest()
                self.assertNotIn(digest, seen, f"Part #{part} reuses Part #{seen.get(digest)} artwork")
                seen[digest] = part

    def test_dds_slots_without_stock_registration_have_tga_stubs(self):
        for part in (109, 110, 111, 112, 114, 117, 118, 119, 120, 307, 308, 309):
            with self.subTest(part=part):
                stem = f"iwbwsh_m_{part % 100 * 10 + part // 100:03}"
                self.assertTrue((WEAPONS / f"{stem}.dds").is_file())
                self.assertTrue((WEAPONS / f"{stem}.tga").is_file(),
                                f"{stem}: native inventory discovery needs a TGA registration")

    def test_empty_end_parts_do_not_cover_middle_artwork(self):
        for end in ("b", "t"):
            with self.subTest(end=end):
                with Image.open(WEAPONS / f"iwbwsh_{end}_011.tga") as source:
                    self.assertEqual(source.size, (64, 64))
                    self.assertIsNone(source.convert("RGBA").getchannel("A").getbbox())


if __name__ == "__main__":
    unittest.main()
