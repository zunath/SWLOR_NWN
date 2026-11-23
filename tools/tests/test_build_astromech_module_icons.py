"""Astromech module icons: the numbers the generators give the modules, and the icons drawn for them.

A module keeps the humanoid piece's base item, so its icon number is the only thing that tells the client to draw a
droid part. These tests also hold the other half: no humanoid piece carries one of the numbers."""
import importlib
import re
import struct
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image

TOOLS = Path(__file__).resolve().parents[1]
REPO = TOOLS.parent
HAKS = REPO / "SWLOR_Haks"
sys.path.insert(0, str(TOOLS))
icons = importlib.import_module("BuildAstromechModuleIcons")
mod = icons.modules

BASE_ITEM = re.compile(r'"BaseItem": \{\s*"type": "int",\s*"value": (\d+)')
MODEL_PART = re.compile(r'"x?ModelPart[123]": \{\s*"type": "(?:byte|word)",\s*"value": (\d+)')
DYE = r'"%s": \{\s*"type": "byte",\s*"value": (\d+)'


def blueprint(base_item: int, parts: dict, extended: bool = True) -> str:
    def field(name: str, kind: str, value: int) -> str:
        return f'  "{name}": {{\r\n    "type": "{kind}",\r\n    "value": {value}\r\n  }}'
    fields = [field("BaseItem", "int", base_item)]
    fields += [field(f"ModelPart{part}", "byte", value) for part, value in parts.items()]
    if extended:
        fields += [field(f"xModelPart{part}", "word", value) for part, value in parts.items()]
    return "{\r\n" + ",\r\n".join(fields) + "\r\n}"


def numbers(text: str) -> set:
    return {int(value) for value in MODEL_PART.findall(text)}


class ModuleIconNumberTests(unittest.TestCase):
    def test_a_module_takes_one_number_of_its_kind_in_every_model_field(self):
        for base_item in mod.MODULE_ICONS:
            parts = {1: 61, 2: 12, 3: 44} if base_item in mod.THREE_PART_ITEMS else {1: 302}
            found = numbers(mod.with_module_icon(blueprint(base_item, parts), "some_piece"))
            self.assertEqual(len(found), 1, base_item)
            self.assertIn(found.pop(), mod.module_icon_numbers(base_item), base_item)

    def test_the_number_follows_the_source_and_spreads_over_the_variants(self):
        text = blueprint(19, {1: 7})
        self.assertEqual(mod.with_module_icon(text, "piece_a"), mod.with_module_icon(text, "piece_a"))
        used = {numbers(mod.with_module_icon(text, f"piece_{index}")).pop() for index in range(40)}
        self.assertEqual(used, set(mod.module_icon_numbers(19)))

    def test_a_sensor_package_and_a_deflector_emitter_wear_the_dyes_of_their_icon(self):
        for base_item in mod.PALETTE_ICON_ITEMS:
            for source in ("piece_a", "piece_b", "piece_c", "piece_d", "piece_e"):
                text = mod.with_module_icon(blueprint(base_item, {1: 12}), source)
                variant = mod.module_icon_numbers(base_item).index(numbers(text).pop())
                self.assertEqual(re.findall(DYE % "Metal1Color", text), [str(mod.ICON_METAL_DYE)])
                self.assertEqual(re.findall(DYE % "Cloth1Color", text), [str(mod.ICON_GLOW_DYES[variant])])
                names = re.findall(r'^  "(\w+)": ', text, re.MULTILINE)
                self.assertEqual(names, sorted(names))
        dyed = blueprint(17, {1: 12}).replace('  "ModelPart1"', '  "Cloth1Color": {\r\n    "type": "byte",\r\n'
                                              '    "value": 128\r\n  },\r\n  "ModelPart1"', 1)
        self.assertEqual(len(re.findall(DYE % "Cloth1Color", mod.with_module_icon(dyed, "piece"))), 1)
        self.assertNotIn("Color", mod.with_module_icon(blueprint(19, {1: 12}), "piece"))

    def test_plating_and_held_tools_keep_their_own_icons(self):
        for base_item, parts in ((16, {}), (545, {1: 11, 2: 11, 3: 11}), (546, {1: 11, 2: 21, 3: 11})):
            text = blueprint(base_item, parts)
            self.assertEqual(mod.with_module_icon(text, "piece"), text)

    def test_unexpected_model_fields_stop_the_generator(self):
        with self.assertRaises(SystemExit):
            mod.with_module_icon(blueprint(26, {1: 61}), "boots_with_one_part")
        with self.assertRaises(SystemExit):
            mod.with_module_icon(blueprint(17, {1: 5}).replace('"value": 5\r\n  }\r\n}', '"value": 6\r\n  }\r\n}'), "piece")

    def test_the_kinds_do_not_share_an_icon_name(self):
        names = [name.format(number) for base_item, (_, names, _) in icons.ICONS.items()
                 for number in mod.module_icon_numbers(base_item) for name in names]
        self.assertEqual(len(names), len(set(names)))
        self.assertEqual(set(icons.ICONS), set(mod.MODULE_ICONS))

    def test_no_humanoid_blueprint_carries_a_module_icon_number(self):
        taken = []
        modules = 0
        for path in (REPO / "Module/uti").glob("*.uti.json"):
            text = path.read_text(encoding="latin-1")
            base_item = int(BASE_ITEM.search(text)[1])
            if base_item not in mod.MODULE_ICONS:
                continue
            droid = numbers(text) & set(mod.module_icon_numbers(base_item))
            if path.name.startswith("am_"):
                modules += 1
                if numbers(text) - set(mod.module_icon_numbers(base_item)):
                    taken.append(f"{path.name} still shows a humanoid icon")
            elif droid:
                taken.append(f"{path.name} is a humanoid piece with module icon {sorted(droid)}")
        self.assertGreater(modules, 900)
        self.assertEqual(taken, [])


class ModuleIconArtTests(unittest.TestCase):
    @staticmethod
    def picture(box, size=(200, 200), slant=False):
        image = np.zeros((size[1], size[0], 4), np.uint8)
        if slant:
            for step in range(140):
                image[30 + step:50 + step, 20 + step:40 + step] = (200, 200, 200, 255)
        else:
            left, top, right, bottom = box
            image[top:bottom, left:right] = (200, 200, 200, 255)
        return Image.fromarray(image)

    def test_a_picture_fills_its_slot_inside_the_margin(self):
        for cells in ((1, 1), (1, 2), (2, 1), (2, 2), (2, 3)):
            art = icons.icon_art(self.picture((40, 20, 160, 180)), cells)
            self.assertEqual(art.shape, (cells[1] * 32, cells[0] * 32, 4))
            rows, cols = np.nonzero(art[..., 3] > 0.5)
            self.assertGreaterEqual(min(rows.min(), cols.min()), icons.MARGIN - 1)
            self.assertLessEqual(rows.max(), cells[1] * 32 - icons.MARGIN)
            self.assertLessEqual(cols.max(), cells[0] * 32 - icons.MARGIN)
            self.assertTrue(rows.max() - rows.min() >= cells[1] * 32 - 2 * icons.MARGIN - 3
                            or cols.max() - cols.min() >= cells[0] * 32 - 2 * icons.MARGIN - 3)

    def test_a_slanted_part_lies_level_in_a_wide_slot(self):
        art = icons.icon_art(self.picture(None, slant=True), (2, 1))
        rows, cols = np.nonzero(art[..., 3] > 0.5)
        self.assertGreater(cols.max() - cols.min(), 50)
        self.assertLess(rows.max() - rows.min(), 16)

    def test_boots_draw_the_picture_on_the_bottom_part_only(self):
        art = icons.icon_art(self.picture((40, 20, 160, 180)), (2, 2))
        files = icons.icon_files(26, 200, art)
        self.assertEqual(list(files), ["iit_boots_b_200", "iit_boots_m_200", "iit_boots_t_200"])
        self.assertTrue(files["iit_boots_b_200"][..., 3].any())
        self.assertFalse(files["iit_boots_m_200"].any() or files["iit_boots_t_200"].any())

    def test_a_palette_icon_is_the_picture_in_shades_of_its_two_dyes(self):
        rows = np.ones((1100, 256, 4), np.float32)
        shades = np.linspace(0, 1, 256, dtype=np.float32)
        rows[icons.LAYER_ROWS[icons.METAL_LAYER] + 60, :, :3] = shades[:, None] * (1.0, 1.0, 1.0)
        rows[icons.LAYER_ROWS[icons.GLOW_LAYER] + 148, :, :3] = shades[:, None] * (0.1, 0.3, 1.0)
        art = np.zeros((96, 64, 4), np.float32)
        art[10:40, 10:50] = (0.5, 0.5, 0.5, 1.0)       # metal
        art[50:60, 20:40] = (0.08, 0.24, 0.8, 1.0)     # glow
        art[70:80, 20:40] = (0.9, 0.9, 0.9, 0.2)       # too faint to show
        texels = icons.palette_icon(art, (64, 128), rows, 60, 148)
        self.assertEqual(texels.shape, (128, 64, 2))
        self.assertEqual(texels[20, 20, 1], icons.METAL_LAYER)
        self.assertAlmostEqual(int(texels[20, 20, 0]), 0.5 * 255, delta=1)
        self.assertEqual(texels[55, 30, 1], icons.GLOW_LAYER)
        self.assertAlmostEqual(int(texels[55, 30, 0]), 0.8 * 255, delta=2)
        for row, col in ((0, 0), (75, 30), (120, 30)):
            self.assertEqual(tuple(texels[row, col]), icons.plating.PLT_BACKGROUND)

    def test_a_reflective_dye_is_refused(self):
        rows = np.ones((1100, 256, 4), np.float32)
        rows[icons.LAYER_ROWS[icons.GLOW_LAYER] + 59, 100:, 3] = 0.0
        rows[:, icons.SEE_THROUGH_SHADE, 3] = 0.0      # as in the real palette, and no reason to refuse a dye
        icons.palette_icon(np.ones((64, 64, 4), np.float32), (64, 64), rows, 60, 148)
        art = np.ones((64, 64, 4), np.float32) * (0.9, 0.5, 0.1, 1.0)
        with self.assertRaises(SystemExit):
            icons.palette_icon(art, (64, 64), rows, 60, 59)

    @unittest.skipUnless((HAKS / icons.PALETTE).is_file(), "the haks are not checked out")
    def test_the_icon_dyes_are_matte(self):
        rows = icons.dye_rows(HAKS / icons.PALETTE)
        icons.matte_ramp(rows, icons.METAL_LAYER, mod.ICON_METAL_DYE)
        for dye in mod.ICON_GLOW_DYES:
            icons.matte_ramp(rows, icons.GLOW_LAYER, dye)
        self.assertEqual(len(set(mod.ICON_GLOW_DYES)), mod.ICON_VARIANTS)

    def test_a_humanoid_icon_of_the_same_name_is_never_replaced(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "iit_neck_140.dds"
            self.assertFalse(icons.humanoid_icon(path))
            path.with_suffix(".tga").write_bytes(b"full size art")
            self.assertTrue(icons.humanoid_icon(path))
            path.write_bytes(b"ours")
            self.assertFalse(icons.humanoid_icon(path))
            path.with_suffix(".plt").write_bytes(b"palette art")
            self.assertTrue(icons.humanoid_icon(path))


class CloakRowTests(unittest.TestCase):
    TABLE = ("2DA V2.0\r\n\r\n"
             "           LABEL                                    MODEL TEXTURE ICON HIDEWING ENVMAP  \r\n"
             "0          ****                                     0     0       0    0        ****    \r\n"
             '99         "Non-appearing Cloak"                    0     1       1    0        ****    \r\n'
             "200        ****                                     ****  ****    **** ****     ****    \r\n"
             '201        "Someone\'s Cloak"                        3     3       5    0        default \r\n')

    def test_an_icon_number_gets_a_row_with_no_model(self):
        table = icons.cloak_rows(self.TABLE, [200])
        row = table.split("\r\n")[5].split()
        self.assertEqual(row, ["200", '"Deflector', "Emitter", '1"', "0", "1", "200", "0", "****"])
        self.assertEqual(icons.cloak_rows(table, [200]), table)
        self.assertEqual(table.split("\r\n")[:5], self.TABLE.split("\r\n")[:5])

    def test_a_row_in_use_is_never_taken(self):
        with self.assertRaises(SystemExit):
            icons.cloak_rows(self.TABLE, [201])


@unittest.skipUnless((HAKS / "sw_item").is_dir() and (HAKS / icons.SOURCES).is_dir(), "the haks are not checked out")
class WrittenIconTests(unittest.TestCase):
    def test_every_module_number_has_its_slot_sized_icon(self):
        for base_item, (picture, _) in mod.MODULE_ICONS.items():
            folder, names, cells = icons.ICONS[base_item]
            for variant, number in enumerate(mod.module_icon_numbers(base_item), 1):
                self.assertTrue((HAKS / icons.SOURCES / f"{picture}_{variant}.png").is_file(), (picture, variant))
                if base_item in mod.PALETTE_ICON_ITEMS:
                    path = HAKS / folder / f"{names[0].format(number)}.plt"
                    data = path.read_bytes()
                    self.assertEqual(data[:8], b"PLT V1  ", path.name)
                    self.assertEqual(struct.unpack_from("<II", data, 16), icons.PALETTE_SIZES[base_item], path.name)
                    self.assertFalse(path.with_suffix(".dds").exists() or path.with_suffix(".tga").exists(), path.name)
                    layers = set(np.frombuffer(data, np.uint8, offset=24)[1::2].tolist())
                    self.assertEqual(layers, {0, icons.METAL_LAYER, icons.GLOW_LAYER}, path.name)
                    continue
                for name in names:
                    path = HAKS / folder / f"{name.format(number)}.dds"
                    data = path.read_bytes()
                    height, width = int.from_bytes(data[12:16], "little"), int.from_bytes(data[16:20], "little")
                    self.assertEqual((width, height), (cells[0] * 32, cells[1] * 32), path.name)
                    with Image.open(path.with_suffix(".tga")) as stub:
                        self.assertEqual(stub.size, (8, 8), path.name)
                    self.assertFalse(path.with_suffix(".plt").exists(), path.name)

    def test_the_cloak_numbers_are_rows_with_an_icon_and_no_model(self):
        rows = {line.split()[0]: line.split() for line in
                (HAKS / icons.CLOAK_TABLE).read_text(encoding="latin-1").splitlines()[3:] if line.split()}
        for number in mod.module_icon_numbers(icons.CLOAK_BASE_ITEM):
            self.assertEqual(rows[str(number)][-8:-5], ["0", "1", str(number)], number)


if __name__ == "__main__":
    unittest.main()
