"""Check Appearance Editor picture naming, tint baking and cloak tiles without Blender or NWN."""
import re
import struct
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


class PlainTextureTests(unittest.TestCase):
    def write(self, folder):
        source = Path(folder) / "fringe.tga"
        pixels = np.zeros((2, 2, 4), np.uint8)
        pixels[..., 0] = 200
        pixels[0, 0, 3] = 255
        Image.fromarray(pixels, "RGBA").save(source)
        return source

    def test_a_see_through_material_keeps_its_cutouts(self):
        from appearance_previews.tint import plain_texture
        with tempfile.TemporaryDirectory() as folder:
            out = Path(folder) / "fringe_a.png"
            plain_texture(self.write(folder), out, keep_alpha=True)
            saved = Image.open(out)
            self.assertEqual(saved.mode, "RGBA")
            self.assertEqual(sorted(a for *_, a in saved.getdata()), [0, 0, 0, 255])

    def test_other_alpha_is_a_shine_mask_and_stays_solid(self):
        from appearance_previews.tint import plain_texture
        with tempfile.TemporaryDirectory() as folder:
            out = Path(folder) / "fringe_c.png"
            plain_texture(self.write(folder), out)
            self.assertEqual(Image.open(out).mode, "RGB")

    def test_materials_mark_cutouts_with_transparency_or_punchthrough(self):
        from appearance_previews.__main__ import is_see_through
        self.assertTrue(is_see_through("texture0 loki-head\ntransparency 1\n"))
        self.assertTrue(is_see_through("blending punchthrough\n"))
        self.assertFalse(is_see_through("texture0 n_sithsoldier03\nrenderhint NormalTangents\n"))
        self.assertFalse(is_see_through("transparency 0\n"))


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


# ItemClass of every BaseItem the Appearance Editor offers weapon parts for (SWLOR_Haks sw_2da/baseitems.2da).
ITEM_CLASSES = {
    1: "WSwLs", 7: "WBwXl", 11: "WBwSh", 12: "WDbSw", 13: "WSwGs", 22: "WSwDg", 50: "WDbQs", 56: "AShLw",
    58: "WPlSs", 59: "WThSh", 61: "WBwSh", 310: "WSwDp", 511: "WDbLsbr", 512: "WSwGlsbr", 514: "WBwSh",
    525: "WSwLs", 537: "WDbSw", 541: "WPG",
}


def write_base_items(root: Path):
    rows = [f"{row} 1 item{row} {item_class}" for row, item_class in ITEM_CLASSES.items()]
    (root / "baseitems.2da").write_text("\n".join(["2DA V2.0", "", "Name label ItemClass"] + rows), encoding="latin-1")


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

        self.assertIn("wswls_t_011", keys)
        self.assertIn("ashlw_011", keys)
        for key in keys:
            self.assertRegex(key, r"^(p[fm]h0_[a-z]+\d{3}|helm_\d{3}|w[a-z]+_[tmb]_\d{3}|w[a-z]+_\d{3}|ashlw_\d{3})$")
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
        write_base_items(root)
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

    def test_a_part_that_became_a_duplicate_loses_its_old_picture_on_a_partial_run(self):
        from appearance_previews.__main__ import write_pictures
        with tempfile.TemporaryDirectory() as folder:
            renders, output = Path(folder, "renders"), Path(folder, "out")
            renders.mkdir()
            output.mkdir()
            for key in ("pmh0_chest001", "pme0_chest001"):
                for view in VIEWS:
                    Image.new("RGBA", (64, 64), (200, 0, 0, 255)).save(renders / f"{key}_{view}.png")
            old, unrelated = output / "apme0_chest001f.dds", output / "apme0_chest002f.dds"
            old.write_bytes(b"old")
            unrelated.write_bytes(b"other")
            write_pictures(["pme0_chest001"], renders, output, 96)
            self.assertFalse(old.exists(), "the human picture must show instead")
            self.assertTrue(unrelated.exists())

    def test_weapon_parts_publish_only_their_front_picture(self):
        from appearance_previews.__main__ import write_pictures
        with tempfile.TemporaryDirectory() as folder:
            renders, output = Path(folder, "renders"), Path(folder, "out")
            renders.mkdir()
            output.mkdir()
            for key in ("wswls_t_011", "pmh0_chest001"):
                for view in VIEWS:
                    Image.new("RGBA", (64, 64), (200, 0, 0, 255)).save(renders / f"{key}_{view}.png")
            kept, _, wanted = write_pictures(["wswls_t_011", "pmh0_chest001"], renders, output, 96,
                                             frozenset({"wswls_t_011"}))
            self.assertEqual(kept, 4)
            self.assertIn("awswls_t_011f.dds", wanted)
            self.assertNotIn("awswls_t_011b.dds", wanted)
            self.assertNotIn("awswls_t_011a.dds", wanted)
            self.assertIn("apmh0_chest001b.dds", wanted)


class WeaponJobTests(unittest.TestCase):
    def test_composite_part_ids_name_the_engine_models(self):
        # Part id = colour * 100 + model; resref = <ItemClass>_<t|m|b>_<model * 10 + colour:D3>.
        self.assertEqual(jobs.composite_weapon_model("wswls", "t", 101), "wswls_t_011")
        self.assertEqual(jobs.composite_weapon_model("wswls", "b", 325), "wswls_b_253")
        self.assertEqual(jobs.composite_weapon_model("wpg", "m", 110), "wpg_m_101")
        self.assertEqual(jobs.composite_weapon_model("wswglsbr", "t", 114), "wswglsbr_t_141")
        self.assertEqual(jobs.composite_weapon_model("wswls", "t", 100), "")
        self.assertEqual(jobs.composite_weapon_model("wswls", "t", 7), "")

    def test_simple_part_ids_name_the_engine_models(self):
        self.assertEqual(jobs.simple_weapon_model("ashlw", 11), "ashlw_011")
        self.assertEqual(jobs.simple_weapon_model("wthsh", 255), "wthsh_255")
        self.assertEqual(jobs.simple_weapon_model("ashlw", 0), "")

    def test_definitions_are_read_as_the_editor_registers_them(self):
        definitions = jobs.weapon_definitions()
        self.assertEqual(definitions["Longsword"], "LongswordAppearanceDefinition")
        for base_item in ("Pistol", "Sling", "LegacyPistol"):
            self.assertEqual(definitions[base_item], "PistolAppearanceDefinition")
        ids = jobs.base_item_ids()
        self.assertEqual({ids[name] for name in definitions}, set(ITEM_CLASSES))

    def test_definition_lists_skip_comments(self):
        folder = jobs.DEFINITIONS / "ItemAppearance"
        simple, lists = jobs.weapon_definition(folder / "LongswordAppearanceDefinition.cs")
        self.assertFalse(simple)
        self.assertEqual(lists["TopParts"][:2], [101, 102])
        self.assertIn(901, lists["BottomParts"])
        simple, lists = jobs.weapon_definition(folder / "LargeShieldAppearanceDefinition.cs")
        self.assertTrue(simple)
        self.assertEqual(lists["SimpleParts"][0], 11)

    def test_every_weapon_part_has_a_picture_resref(self):
        with tempfile.TemporaryDirectory() as directory:
            write_base_items(Path(directory))
            models = jobs.weapon_models(Path(directory))
        found = dict(models)
        self.assertTrue(found["PowerGlove"].startswith("wpg_"))
        self.assertIn(("Longsword", "wswls_t_011"), models)
        self.assertIn(("LargeShield", "ashlw_011"), models)
        for _, model in models:
            for view in VIEWS:
                self.assertEqual(preview_resref(model, view), f"a{model}{view}")

    def test_build_marks_weapon_jobs_and_skips_missing_models(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ("appearance.2da", "cloakmodel.2da", "cloaktint.2da"):
                (root / name).write_text("2DA V2.0" + chr(10) + chr(10) + "LABEL" + chr(10), encoding="latin-1")
            write_base_items(root)
            built = jobs.build(root, lambda name: name in {"wswls_t_011.mdl", "ashlw_011.mdl"})
        self.assertEqual(sorted((j.key, j.model, j.weapon) for j in built),
                         [("ashlw_011", "ashlw_011", "shield"), ("wswls_t_011", "wswls_t_011", "weapon")])

    def test_item_classes_match_the_haks(self):
        two_da = REPO / "SWLOR_Haks/sw_2da/baseitems.2da"
        if not two_da.is_file():
            self.skipTest("SWLOR_Haks sw_2da is not checked out")
        classes = jobs.item_classes(two_da.parent)
        self.assertEqual({row: classes[row] for row in ITEM_CLASSES}, {r: c.lower() for r, c in ITEM_CLASSES.items()})


class EmitterTests(unittest.TestCase):
    def test_textured_emitters_are_read_with_their_size(self):
        from appearance_previews.__main__ import mesh_emitters
        model = chr(10).join([
            "node trimesh blade", "  bitmap null", "  render 0", "endnode",
            "node emitter nwnemitter02", "  texture lsbr_015", "  sizeStart 0.22", "  sizeStart_y 1.21", "endnode",
            "node emitter spark", "  texture NULL", "  sizeStart 0.1", "  sizeStart_y 0.1", "endnode", ""])
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder, "m.mdl")
            path.write_text(model)
            self.assertEqual(mesh_emitters(path),
                             [{"node": "nwnemitter02", "texture": "lsbr_015", "width": 0.22, "length": 1.21}])


class BiowareDdsTests(unittest.TestCase):
    def test_bioware_dds_decodes_like_the_standard_file(self):
        from appearance_previews.tint import open_texture
        with tempfile.TemporaryDirectory() as folder:
            standard = Path(folder, "standard.dds")
            source = Image.new("RGB", (8, 8))
            source.putdata([(x * 30, y * 30, 90) for y in range(8) for x in range(8)])
            source.save(standard, "DDS", pixel_format="DXT1")
            data = standard.read_bytes()
            self.assertEqual(data[84:88], b"DXT1")
            blocks = data[128:128 + 32]  # top mipmap: an 8x8 DXT1 image is 4 blocks of 8 bytes
            bioware = Path(folder, "bioware.dds")
            bioware.write_bytes(struct.pack("<IIIIf", 8, 8, 3, 32, 1.0) + blocks)
            expected = np.asarray(Image.open(standard).convert("RGB"))
            self.assertTrue(np.array_equal(np.asarray(open_texture(bioware).convert("RGB")), expected))

    def test_other_unreadable_files_still_fail(self):
        from appearance_previews.tint import open_texture
        with tempfile.TemporaryDirectory() as folder:
            junk = Path(folder, "junk.dds")
            junk.write_bytes(b"not a texture at all")
            with self.assertRaises((OSError, ValueError)):
                open_texture(junk)


class BaseGameDataTests(unittest.TestCase):
    def test_key_and_bif_resources_are_found_and_extracted(self):
        from appearance_previews.resources import HakIndex
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / "data").mkdir()
            payloads = [b"model bytes", b"texture bytes", b"script"]
            types = [2002, 3, 2009]  # mdl, tga, nss (not read)
            table, offset = b"", 20 + 16 * len(payloads)
            for index, (payload, restype) in enumerate(zip(payloads, types)):
                table += struct.pack("<IIII", index, offset, len(payload), restype)
                offset += len(payload)
            bif = b"BIFFV1  " + struct.pack("<III", len(payloads), 0, 20) + table + b"".join(payloads)
            (root / "data/test.bif").write_bytes(bif)
            name = b"data" + bytes([92]) + b"test.bif" + bytes([0])
            names = [b"wswls_t_011", b"wswls_t_011", b"nw_script"]
            name_offset = 24 + 12
            key_table_offset = name_offset + len(name)
            key = b"KEY V1  " + struct.pack("<IIII", 1, len(names), 24, key_table_offset)
            key += struct.pack("<IIHH", len(bif), name_offset, len(name), 1) + name
            for index, (resref, restype) in enumerate(zip(names, types)):
                key += struct.pack("<16sHI", resref, restype, index)
            (root / "data/test.key").write_bytes(key)
            (root / "haks").mkdir()
            index = HakIndex(root / "haks", [], root)
            self.assertTrue(index.exists("WSWLS_T_011.mdl"))
            self.assertTrue(index.exists("wswls_t_011.tga"))
            self.assertFalse(index.exists("nw_script.nss"))
            out = root / "out"
            extracted = index.extract(["wswls_t_011.mdl", "wswls_t_011.tga", "missing.mdl"], out)
            self.assertEqual(sorted(extracted), ["wswls_t_011.mdl", "wswls_t_011.tga"])
            self.assertEqual((out / "wswls_t_011.mdl").read_bytes(), b"model bytes")
            self.assertEqual((out / "wswls_t_011.tga").read_bytes(), b"texture bytes")


class BatchProblemTests(unittest.TestCase):
    def test_a_crashed_batch_reports_its_exit_code_and_every_missing_render(self):
        from appearance_previews.__main__ import batch_problems
        with tempfile.TemporaryDirectory() as folder:
            renders = Path(folder)
            for view in VIEWS:
                Image.new("RGBA", (4, 4)).save(renders / f"done_{view}.png")
            batch = [{"key": "done"}, {"key": "lost"}, {"key": "nomesh"}]
            problems = batch_problems(3, "PREVIEW_NO_MESH nomesh" + chr(10), "add-on failed" + chr(10), batch, renders)
            self.assertIn("PREVIEW_NO_MESH nomesh", problems)
            self.assertIn("PREVIEW_BATCH_FAILED exit 3: add-on failed", problems)
            self.assertIn("PREVIEW_MISSING_RENDER lost", problems)
            self.assertFalse(any("done" in line for line in problems))

    def test_a_clean_batch_has_no_problems(self):
        from appearance_previews.__main__ import batch_problems
        with tempfile.TemporaryDirectory() as folder:
            renders = Path(folder)
            for view in VIEWS:
                Image.new("RGBA", (4, 4)).save(renders / f"done_{view}.png")
            self.assertEqual(batch_problems(0, "PREVIEW_RENDERED done" + chr(10), "", [{"key": "done"}], renders), [])

    def test_an_empty_weapon_part_is_not_a_problem(self):
        from appearance_previews.__main__ import batch_problems, empty_parts
        with tempfile.TemporaryDirectory() as folder:
            stdout = "PREVIEW_EMPTY wbwsh_t_011" + chr(10)
            self.assertEqual(batch_problems(0, stdout, "", [{"key": "wbwsh_t_011"}], Path(folder)), [])
            self.assertEqual(empty_parts(stdout), {"wbwsh_t_011"})


class RenderCacheTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        root = Path(self.folder.name)
        self.work, self.renders, haks = root / "work", root / "work" / "renders", root / "haks"
        self.renders.mkdir(parents=True)
        haks.mkdir()
        self.model_hak, self.tint_hak, self.other_hak = haks / "sw_pt_chest.hak", haks / "sw_tint0.hak", haks / "sw_ui.hak"
        for hak in (self.model_hak, self.tint_hak, self.other_hak):
            hak.write_bytes(b"x")

        class Index:
            where = {"pfh0_chest001.mdl": self.model_hak, "tm_a.dds": self.tint_hak, "gui.tga": self.other_hak}
        self.index = Index()
        for view in VIEWS:
            Image.new("RGBA", (4, 4)).save(self.renders / f"pfh0_chest001_{view}.png")

    def tearDown(self):
        self.folder.cleanup()

    def cache(self):
        from appearance_previews.__main__ import RenderCache
        return RenderCache(self.work, self.index, 256)

    def touch(self, hak):
        hak.write_bytes(b"changed")

    def test_a_new_render_size_renders_everything_again(self):
        cache = self.cache()
        cache.record("pfh0_chest001", ["pfh0_chest001.mdl"], complete=True)
        cache.save()
        from appearance_previews.__main__ import RenderCache
        self.assertTrue(RenderCache(self.work, self.index, 256).is_fresh("pfh0_chest001", self.renders))
        self.assertFalse(RenderCache(self.work, self.index, 512).is_fresh("pfh0_chest001", self.renders))

    def test_a_render_is_reused_until_one_of_its_haks_changes(self):
        cache = self.cache()
        self.assertFalse(cache.is_fresh("pfh0_chest001", self.renders), "nothing recorded yet")
        cache.record("pfh0_chest001", ["pfh0_chest001.mdl", "tm_a.dds"], complete=True)
        cache.save()
        self.assertTrue(self.cache().is_fresh("pfh0_chest001", self.renders))
        self.touch(self.other_hak)
        self.assertTrue(self.cache().is_fresh("pfh0_chest001", self.renders), "an unrelated HAK changed")
        self.cache().save()
        self.touch(self.tint_hak)
        self.assertFalse(self.cache().is_fresh("pfh0_chest001", self.renders), "its tint map HAK changed")

    def test_a_render_with_a_missing_texture_is_redone_after_any_hak_change(self):
        cache = self.cache()
        cache.record("pfh0_chest001", ["pfh0_chest001.mdl"], complete=False)
        cache.save()
        self.assertTrue(self.cache().is_fresh("pfh0_chest001", self.renders))
        self.touch(self.other_hak)
        self.assertFalse(self.cache().is_fresh("pfh0_chest001", self.renders))

    def test_changing_the_preview_dyes_redoes_every_render(self):
        from appearance_previews import __main__ as generator
        cache = self.cache()
        cache.record("pfh0_chest001", ["pfh0_chest001.mdl"], complete=True)
        cache.save()
        original = generator.PREVIEW_DYES
        try:
            generator.PREVIEW_DYES = tuple(reversed(original))
            self.assertFalse(self.cache().is_fresh("pfh0_chest001", self.renders))
        finally:
            generator.PREVIEW_DYES = original

    def test_shared_intermediates_are_cleared_when_any_hak_changes(self):
        self.cache().save()
        (self.work / "textures").mkdir()
        self.touch(self.other_hak)
        self.cache()
        self.assertFalse((self.work / "textures").exists())


class ClassificationTests(unittest.TestCase):
    def test_unsupported_classifications_become_character(self):
        from appearance_previews.__main__ import importable_classification
        model = chr(10).join(["newmodel x", "  classification OTHER", ""])
        self.assertIn("classification character", importable_classification(model))
        item = "  classification ITEM" + chr(10)
        self.assertEqual(importable_classification(item), item)


class MissingHakTests(unittest.TestCase):
    def test_a_missing_module_hak_stops_the_run_before_writing_anything(self):
        from appearance_previews import __main__ as generator
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / "haks").mkdir()
            output = root / "out"
            output.mkdir()
            (output / "apmh0_chest001f.dds").write_bytes(b"old")
            original = generator.module_haks
            generator.module_haks = lambda: ["sw_2da", "sw_part_previews"]
            try:
                code = generator.main(["--hak-dir", str(root / "haks"), "--output", str(output), "--work", str(root / "work")])
            finally:
                generator.module_haks = original
            self.assertEqual(code, 2)
            self.assertTrue((output / "apmh0_chest001f.dds").exists(), "existing pictures are kept")


class FailedPartTests(unittest.TestCase):
    def test_problem_lines_name_their_parts(self):
        from appearance_previews.__main__ import failed_keys
        problems = ["PREVIEW_FAILED pmh0_chest001 RuntimeError('x')", "PREVIEW_MISSING_RENDER helm_002",
                    "PREVIEW_NO_MODEL pfh0_head012", "PREVIEW_BATCH_FAILED exit 3: crash"]
        self.assertEqual(failed_keys(problems), {"pmh0_chest001", "helm_002", "pfh0_head012"})


if __name__ == "__main__":
    unittest.main()
