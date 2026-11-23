"""Astromech inventory icons: the plating's palette icon and the held tools' part icons."""
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
sys.path.insert(0, str(TOOLS))
icons = importlib.import_module("BuildAstromechIcons")


def render(path: Path, color, box=(100, 60, 400, 460), size=512):
    """A fake render: one colored block on a transparent picture."""
    image = np.zeros((size, size, 4), np.uint8)
    left, top, right, bottom = box
    image[top:bottom, left:right] = tuple(color) + (255,)
    Image.fromarray(image).save(path)
    return path


class PlatingIconTests(unittest.TestCase):
    def test_the_plating_torso_is_one_number_everywhere(self):
        body = (REPO / "SWLOR.Game.Server/Service/AstromechService/AstromechBody.cs").read_text(encoding="utf-8")
        catalog = (REPO / "SWLOR.Game.Server/Feature/AppearanceDefinition/ItemAppearance/"
                   "AstromechArmorAppearanceDefinition.cs").read_text(encoding="utf-8")
        self.assertEqual(int(re.search(r"const int PlatingTorso = (\d+);", body)[1]), icons.PLATING_TORSO)
        self.assertEqual(re.search(r"int\[\] Torso \{ get; \} = \{ (\d+) \};", catalog)[1], str(icons.PLATING_TORSO))
        self.assertEqual(icons.modules.PLATING_TORSO, icons.PLATING_TORSO)

    def test_the_bakes_separate_brightness_from_dye(self):
        shade = np.array([[0, 128, 300]])
        layer = np.array([[3, 4, 5]])
        grey, metal = icons.bake_shade(shade, layer, None)
        self.assertEqual(grey.tolist(), [[[0, 0, 0], [128, 128, 128], [255, 255, 255]]])
        self.assertFalse(metal.any())
        flat, _ = icons.bake_layers(shade, layer, None)
        self.assertEqual([tuple(color) for color in flat[0]], [icons.LAYER_COLORS[index] for index in (3, 4, 5)])
        other, _ = icons.bake_layers(shade, np.array([[0, 9, 2]]), None)
        self.assertTrue((other == icons.LAYER_COLORS[icons.MAIN_LAYER]).all(), "any other layer is main paint")

    def test_the_icon_is_a_palette_picture_inside_the_inventory_slot(self):
        with tempfile.TemporaryDirectory() as folder:
            folder = Path(folder)
            shade = render(folder / "shade.png", (140, 140, 140))
            # Lit flat colors: the hue, not the brightness, names the layer.
            layers = render(folder / "layers.png", (0, 0, 90))
            texels = icons.plating_icon(shade, layers)
        width, height = icons.PART_ICON_SIZE
        self.assertEqual(texels.shape, (height, width, 2))
        drawn = ~((texels[..., 0] == 255) & (texels[..., 1] == 0))
        rows, cols = np.where(drawn.any(1))[0], np.where(drawn.any(0))[0]
        left, top, right, bottom = icons.PLATING_BOX
        self.assertGreaterEqual(rows.min(), top)
        self.assertLess(rows.max(), bottom)
        self.assertLess(bottom, 96, "an armor icon shows 64x96")
        self.assertGreaterEqual(cols.min(), left)
        self.assertLess(cols.max(), right)
        self.assertEqual(set(texels[drawn][:, 1].tolist()), {icons.MAIN_LAYER})
        self.assertTrue((texels[drawn][:, 0] < 255).all(), "shade 255 on layer 0 is the see-through texel")
        self.assertAlmostEqual(float(np.median(texels[drawn][:, 0])), icons.SHADE_MIDTONE, delta=2)

    def test_a_plt_stores_its_rows_bottom_up(self):
        texels = np.zeros((4, 2, 2), np.uint8)
        texels[0, 0] = (7, 5)
        data = icons.plt_bytes(texels)
        self.assertEqual(data[:8], b"PLT V1  ")
        self.assertEqual(struct.unpack_from("<II", data, 16), (2, 4))
        self.assertEqual(len(data), 24 + 2 * 4 * 2)
        self.assertEqual(tuple(data[24 + 3 * 2 * 2:24 + 3 * 2 * 2 + 2]), (7, 5), "the top row is stored last")


class ToolIconTests(unittest.TestCase):
    def test_a_tool_stands_upright_inside_its_one_by_two_slot(self):
        with tempfile.TemporaryDirectory() as folder:
            diagonal = np.zeros((512, 512, 4), np.uint8)
            for step in range(60, 450):          # a bar on the rising diagonal, as the renderer lays a long part
                diagonal[512 - step - 12:512 - step + 12, step - 12:step + 12] = (90, 90, 90, 255)
            path = Path(folder) / "tool.png"
            Image.fromarray(diagonal).save(path)
            rgba = icons.tool_icon(path)
        self.assertEqual(rgba.shape, (icons.TOOL_CANVAS[1], icons.TOOL_CANVAS[0], 4))
        drawn = rgba[..., 3] > 0.15
        rows, cols = np.where(drawn.any(1))[0], np.where(drawn.any(0))[0]
        self.assertLess(rows.max(), 64)
        self.assertLess(cols.max(), 32)
        self.assertGreater(rows.max() - rows.min(), 3 * (cols.max() - cols.min()), "the bar stands upright")

    def test_every_tool_part_number_gets_an_icon_and_its_registration_stub(self):
        haks = REPO / "SWLOR_Haks"
        folder = haks / icons.body.TOOL_FOLDER
        if not folder.is_dir():
            self.skipTest("the haks checkout has no weapon folder")
        for base, slots in icons.body.tool_part_numbers().items():
            item_class = icons.body.TOOL_ITEMS[base][0]
            for slot, numbers in slots.items():
                for number in numbers:
                    stem = folder / f"i{item_class}_{slot}_{number:03d}"
                    self.assertTrue(stem.with_suffix(".dds").is_file(), stem.name)
                    stub = stem.with_suffix(".tga").read_bytes()
                    self.assertEqual(struct.unpack_from("<HH", stub, 12), (8, 8), f"{stem.name}.tga is the 8x8 stub")


if __name__ == "__main__":
    unittest.main()
