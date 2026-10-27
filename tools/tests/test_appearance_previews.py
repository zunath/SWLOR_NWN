"""Check Appearance Editor picture naming, tint baking and cloak tiles without Blender or NWN."""
import re
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from appearance_previews import MAX_RESREF, VIEWS, preview_resref  # noqa: E402
from appearance_previews import jobs  # noqa: E402
from appearance_previews.tint import LAYER_BASE, PREVIEW_DYES, bake, cloak_tile  # noqa: E402

REPO = Path(__file__).resolve().parents[2]
CSHARP = REPO / "SWLOR.Game.Server/Feature/AppearanceDefinition/AppearancePreview/AppearancePreview.cs"


class PreviewResrefTests(unittest.TestCase):
    def test_resref_is_a_key_and_view_letter(self):
        self.assertEqual(VIEWS, ("f", "b", "a"))
        self.assertEqual(preview_resref("pfh0_chest001", "f"), "apfh0_chest001f")
        self.assertEqual(preview_resref("pfh0_chest001", "b"), "apfh0_chest001b")
        self.assertEqual(preview_resref("helm_007", "a"), "ahelm_007a")
        self.assertEqual(preview_resref("PMH0_Head012", "f"), "apmh0_head012f")

    def test_sixteen_characters_is_the_limit(self):
        self.assertEqual(MAX_RESREF, 16)
        self.assertEqual(len(preview_resref("pfh0_pelvis999", "f")), 16)
        with self.assertRaisesRegex(ValueError, "too long"):
            preview_resref("pfh0_pelvis1000", "f")

    def test_unknown_view_is_rejected(self):
        for view in ("F", "x", "front", ""):
            with self.assertRaisesRegex(ValueError, "Unknown view"):
                preview_resref("pfh0_chest001", view)


def synthetic_palette(path: Path, transparent_rows=(), half_rows=()):
    """4 shades x 2048 rows. Palette row r (counted from the bottom, like the TGA) stores
    R = r % 256, G = r // 256 and B = 50 * shade, so a sample names its own row."""
    height, width = 2048, 4
    pixels = np.zeros((height, width, 4), np.uint8)
    for y in range(height):
        row = height - 1 - y
        pixels[y, :, 0] = row % 256
        pixels[y, :, 1] = row // 256
        pixels[y, :, 2] = [50 * shade for shade in range(width)]
        pixels[y, :, 3] = 255
        if row in transparent_rows:
            pixels[y, :, 3] = 0
        if row in half_rows:
            pixels[y, :, 3] = 128
    Image.fromarray(pixels, "RGBA").save(path)


def palette_row(pixel):
    return int(pixel[1]) * 256 + int(pixel[0])


class BakeTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)

    def test_each_layer_samples_its_dye_row_counting_from_the_bottom(self):
        palette = self.root / "palette.png"
        synthetic_palette(palette)
        layer = np.arange(10, dtype=np.int32).reshape(2, 5)
        shade = np.array([[0, 1, 2, 3, 0], [1, 2, 3, 0, 1]], dtype=np.int32)
        rgb, metal = bake(shade, layer, palette)
        self.assertEqual(rgb.shape, (2, 5, 3))
        self.assertEqual(rgb.dtype, np.uint8)
        self.assertEqual(metal.shape, (2, 5))
        for y in range(2):
            for x in range(5):
                index = layer[y, x]
                self.assertEqual(palette_row(rgb[y, x]), LAYER_BASE[index] + PREVIEW_DYES[index], (y, x))
                self.assertEqual(int(rgb[y, x, 2]), 50 * shade[y, x], "shade selects the palette column")
        self.assertFalse(metal.any(), "opaque palette rows have no metal environment")

    def test_custom_dyes_and_out_of_range_values_are_clamped(self):
        palette = self.root / "palette.png"
        synthetic_palette(palette)
        dyes = tuple(range(10, 20))
        layer = np.array([[-3, 0, 9, 42]], dtype=np.int32)
        shade = np.array([[-5, 0, 3, 2]], dtype=np.int32)
        rgb, _ = bake(shade, layer, palette, dyes)
        expected_layers = [0, 0, 9, 9]
        for x, index in enumerate(expected_layers):
            self.assertEqual(palette_row(rgb[0, x]), LAYER_BASE[index] + dyes[index])
        self.assertEqual(int(rgb[0, 0, 2]), 0, "negative shades clamp to the first column")

    def test_layer_bases_follow_the_shader_layer_order(self):
        # skin hair metal1 metal2 cloth1 cloth2 leather1 leather2 tattoo1 tattoo2; pairs share a 176-row bank.
        self.assertEqual(LAYER_BASE, (0, 176, 352, 528, 704, 704, 880, 880, 1056, 1056))
        self.assertEqual(len(PREVIEW_DYES), 10)
        self.assertTrue(all(0 <= dye < 176 for dye in PREVIEW_DYES))

    def test_palette_alpha_blends_toward_the_environment_grey(self):
        metal1 = LAYER_BASE[2] + PREVIEW_DYES[2]
        metal2 = LAYER_BASE[3] + PREVIEW_DYES[3]
        palette = self.root / "palette.png"
        synthetic_palette(palette, transparent_rows={metal1}, half_rows={metal2})
        layer = np.array([[2, 3, 0]], dtype=np.int32)
        shade = np.array([[1, 1, 1]], dtype=np.int32)
        rgb, metal = bake(shade, layer, palette)

        # Fully transparent: the colour is replaced by the 2/3 grey environment.
        self.assertEqual(rgb[0, 0].tolist(), [170, 170, 170])
        self.assertEqual(int(metal[0, 0]), 255)

        environment = 1.0 - 128 / 255
        source = [metal2 % 256, metal2 // 256, 50]
        for channel in range(3):
            expected = (source[channel] / 255 * (1 - environment) + 0.666667 * environment) * 255
            self.assertLessEqual(abs(int(rgb[0, 1, channel]) - expected), 1.0, channel)
        self.assertLessEqual(abs(int(metal[0, 1]) - environment * 255), 1.0)

        self.assertEqual(int(metal[0, 2]), 0, "an opaque row keeps its own colour")
        self.assertEqual(palette_row(rgb[0, 2]), LAYER_BASE[0] + PREVIEW_DYES[0])


def synthetic_cloak_atlas(path: Path, offset: int):
    """2048x4096 atlas of 4x8 tiles. Tile t sits at column t % 4, row 7 - t // 4 from the top
    (GL bottom-up). R names the tile; G is 0 in each tile's top half and 255 in its bottom half."""
    width, height = 2048, 4096
    tile_w, tile_h = width // 4, height // 8
    pixels = np.zeros((height, width, 3), np.uint8)
    for tile in range(32):
        left, top = (tile % 4) * tile_w, (7 - tile // 4) * tile_h
        pixels[top:top + tile_h, left:left + tile_w, 0] = (offset + tile) * 3
        pixels[top + tile_h // 2:top + tile_h, left:left + tile_w, 1] = 255
    Image.fromarray(pixels, "RGB").save(path)


class CloakTileTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.directory = tempfile.TemporaryDirectory()
        root = Path(cls.directory.name)
        cls.atlases = [root / "cloak_tints0.png", root / "cloak_tints1.png"]
        synthetic_cloak_atlas(cls.atlases[0], 0)
        synthetic_cloak_atlas(cls.atlases[1], 32)

    @classmethod
    def tearDownClass(cls):
        cls.directory.cleanup()

    def test_tiles_count_from_the_bottom_left_and_continue_in_the_next_atlas(self):
        for index in (0, 1, 3, 4, 27, 28, 31, 32, 45, 63):
            shade, layer = cloak_tile(self.atlases, index)
            self.assertEqual(shade.shape, (512, 512), index)
            self.assertTrue((shade == index * 3).all(), f"tile {index} crops its own cell")
            self.assertEqual(int(layer.min()), 0)
            self.assertEqual(int(layer.max()), 9)

    def test_tiles_are_returned_top_down(self):
        _, layer = cloak_tile(self.atlases, 5)
        self.assertTrue((layer[:256] == 0).all(), "the tile's top half stays on top")
        self.assertTrue((layer[256:] == 9).all())


class NamingParityTests(unittest.TestCase):
    """Job keys must be the keys the game server asks for (AppearancePreview.cs)."""

    @classmethod
    def setUpClass(cls):
        cls.source = CSHARP.read_text(encoding="utf-8")

    def csharp_key(self, method, **values):
        match = re.search(rf"public static string {method}\([^)]*\)[^$]*\$\"([^\"]+)\"", self.source, re.S)
        self.assertIsNotNone(match, method)
        template = re.sub(r"\{(\w+):D(\d)\}", lambda m: "{" + m[1] + ":0" + m[2] + "d}", match[1])
        return template.format(**values)

    def test_resref_format_and_limit_match(self):
        self.assertIn('$"a{key}{ViewLetter(view)}"', self.source)
        self.assertIn(f"MaxResrefLength = {MAX_RESREF};", self.source)
        letters = dict(re.findall(r"AppearancePreviewView\.(\w+) => '(\w)'", self.source))
        self.assertEqual(letters, {"Back": "b", "Angle": "a"})
        self.assertIn("_ => 'f'", self.source)

    def test_model_stems_match(self):
        csharp = set(re.findall(r'AppearanceEditorSlot\.\w+ => "(\w+)"', self.source))
        python = {stem for stems in jobs.ARMOR_STEMS.values() for stem in stems}
        self.assertEqual(csharp, python)

    def test_csharp_key_formats(self):
        self.assertEqual(self.csharp_key("ArmorPartKey", body="pfh0", stem="chest", partId=7), "pfh0_chest007")
        self.assertEqual(self.csharp_key("HeadKey", body="pmh0", headId=12), "pmh0_head012")
        self.assertEqual(self.csharp_key("HelmetKey", helmetId=5), "helm_005")
        self.assertEqual(self.csharp_key("CloakKey", body="pfh0", cloakRow=3), "pfh0_clk003")

    def test_jobs_build_the_keys_the_server_requests(self):
        with tempfile.TemporaryDirectory() as directory:
            built = self.build_jobs(Path(directory))
        keys = {job.key for job in built}
        self.assertTrue(keys)

        general = jobs.int_lists(jobs.DEFINITIONS / "ItemAppearance/GeneralArmorAppearanceDefinition.cs")
        torso = next(part for part in general["Torso"] if part > 0)
        helmet = next(part for part in general["Helmet"] if part > 0)
        cloak = self.cloak_row
        for body in ("pfh0", "pmh0"):
            self.assertIn(self.csharp_key("ArmorPartKey", body=body, stem="chest", partId=torso), keys)
            self.assertIn(self.csharp_key("CloakKey", body=body, cloakRow=cloak), keys)
        self.assertIn(self.csharp_key("HelmetKey", helmetId=helmet), keys)
        heads = [key for key in keys if "_head" in key]
        self.assertTrue(heads)
        for key in heads:
            body, head = re.fullmatch(r"(p[fm]h0)_head(\d{3})", key).groups()
            self.assertEqual(self.csharp_key("HeadKey", body=body, headId=int(head)), key)

        cloak_job = next(job for job in built if job.key == f"pfh0_clk{cloak:03d}")
        self.assertEqual(cloak_job.model, "pfh0_cloak_001")
        self.assertEqual(cloak_job.materials, {"*": f"{jobs.CLOAK_ATLAS_PREFIX}9"})

        for key in keys:
            self.assertRegex(key, r"^(p[fm]h0_[a-z]+\d{3}|helm_\d{3})$")
            for view in VIEWS:
                preview_resref(key, view)  # raises when a key cannot fit a resref

    def build_jobs(self, root: Path):
        """Run jobs.build against a tiny 2DA set: every species uses body letter h."""
        ids = jobs.appearance_ids()
        species = [path.stem.replace("RacialAppearanceDefinition", "").replace("RacialAppearance", "")
                   for path in (jobs.DEFINITIONS / "RacialAppearance").glob("*RacialAppearance*.cs")]
        rows = sorted({ids[name] for name in species if name in ids})
        self.assertTrue(rows, "species definitions map to appearance ids")
        appearance = ["2DA V2.0", "", "LABEL RACE"] + [f"{row} row{row} H" for row in rows]
        (root / "appearance.2da").write_text("\n".join(appearance), encoding="latin-1")

        general = jobs.int_lists(jobs.DEFINITIONS / "ItemAppearance/GeneralArmorAppearanceDefinition.cs")
        self.cloak_row = next(row for row in general["Cloak"] if row > 0)
        cloak_models = ["2DA V2.0", "", "LABEL MODEL TEXTURE", f"{self.cloak_row} cloak 1 ****"]
        (root / "cloakmodel.2da").write_text("\n".join(cloak_models), encoding="latin-1")
        cloak_tints = ["2DA V2.0", "", "LABEL INDEX", f"{self.cloak_row} tint 9"]
        (root / "cloaktint.2da").write_text("\n".join(cloak_tints), encoding="latin-1")
        return jobs.build(root, lambda name: True)


class DuplicateTests(unittest.TestCase):
    def test_human_twin_maps_other_bodies_to_the_human_body(self):
        from appearance_previews.__main__ import human_twin
        self.assertEqual(human_twin("pme0_chest148"), "pmh0_chest148")
        self.assertEqual(human_twin("pfz0_clk012"), "pfh0_clk012")
        self.assertIsNone(human_twin("pmh0_chest148"))
        self.assertIsNone(human_twin("helm_001"))

    def test_only_near_identical_pictures_are_duplicates(self):
        from appearance_previews.__main__ import is_duplicate
        base = Image.new("RGB", (96, 96), (60, 58, 55))
        almost = Image.new("RGB", (96, 96), (62, 60, 57))
        different = Image.new("RGB", (96, 96), (120, 58, 55))
        self.assertTrue(is_duplicate(base, almost))
        self.assertFalse(is_duplicate(base, different))

    def test_write_pictures_skips_duplicates_and_keeps_distinct_parts(self):
        from appearance_previews.__main__ import write_pictures
        with tempfile.TemporaryDirectory() as folder:
            renders, output = Path(folder, "renders"), Path(folder, "out")
            renders.mkdir()
            output.mkdir()
            for key, color in (("pmh0_chest001", (200, 0, 0, 255)), ("pme0_chest001", (200, 0, 0, 255)),
                               ("pme0_chest002", (0, 200, 0, 255))):
                for view in VIEWS:
                    Image.new("RGBA", (64, 64), color).save(renders / f"{key}_{view}.png")
            kept, skipped, wanted = write_pictures(["pmh0_chest001", "pme0_chest001", "pme0_chest002"], renders, output, 96)
            self.assertEqual((kept, skipped), (6, 3))
            self.assertIn("apmh0_chest001f.dds", wanted)
            self.assertNotIn("apme0_chest001f.dds", wanted)
            self.assertIn("apme0_chest002f.dds", wanted)


if __name__ == "__main__":
    unittest.main()
