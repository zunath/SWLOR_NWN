"""Regression coverage for conservative world-texture selection."""

import hashlib
import json
from pathlib import Path
import sys
import subprocess
import tempfile
import unittest

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import SelectWorldTextureDdsCandidates as selector


class WorldTextureSelectionTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)
        self.source = self.root / "assets"
        self.source.mkdir()
        self.config = self.root / "Build" / "hakbuilder.json"
        self.config.parent.mkdir()
        self.configure("world", "other", "sw_ui")

    def configure(self, *paths):
        for path in paths:
            (self.source / path).mkdir(parents=True, exist_ok=True)
        self.config.write_text(json.dumps({"HakList": [
            {"Name": "unused-name", "Path": f"../SWLOR_Haks/{path}/"} for path in paths
        ]}), encoding="utf-8")

    def texture(self, relative, color=(30, 80, 140, 255), size=(128, 128)):
        path = self.source / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", size, color).save(path, format="TGA")
        return path

    def text(self, relative, contents):
        path = self.source / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(contents, encoding="utf-8")

    def selected(self, **kwargs):
        return selector.select_candidates(self.source, self.config, workers=2, **kwargs)

    def test_semantic_minimaps_are_held_across_haks_case_and_whitespace(self):
        self.texture("world/unusualmap.tga")
        self.texture("world/ordinary.tga")
        self.text("other/tiles.SET", "[TILE0]\n\t iMaGeMaP2d \t = \t UnusualMap \nImageMap2D=\n")
        self.assertEqual([row["path"] for row in self.selected()], ["world/ordinary.tga"])

    def test_legacy_minimap_prefixes_are_held_without_loose_set_references(self):
        for name in ("micn01_v24", "mits01_c2_01", "mztr01_a01", "MiMixed", "MZMixed"):
            self.texture(f"world/{name}.tga")
        self.texture("world/ordinary.tga")
        self.assertEqual([row["path"] for row in self.selected()], ["world/ordinary.tga"])

    def test_set_environment_map_and_numbered_cube_faces_are_held(self):
        for name in ("sky", "sky0", "sky1", "sky2", "sky3", "sky4", "sky5", "ordinary"):
            self.texture(f"world/{name}.tga")
        self.text("other/tiles.SET", "[GENERAL]\n\t EnVmAp \t = \t SKY \n")
        self.assertEqual([row["path"] for row in self.selected()], ["world/ordinary.tga"])

    def test_small_textures_and_scattered_icon_prefixes_are_held(self):
        self.texture("world/plaintexture.tga", size=(64, 64))
        self.texture("world/narrowtexture.tga", size=(128, 64))
        for name in ("ihelm_test", "if_test", "pj_test", "spi_maze"):
            self.texture(f"world/{name}.tga")
        self.texture("world/ordinary.tga")
        self.assertEqual([row["path"] for row in self.selected()], ["world/ordinary.tga"])

    def test_full_semantic_auxiliary_map_suffixes_are_held(self):
        for name in ("Aegis_normal", "Aegis_specular", "stone_ROUGHNESS", "stone_ao"):
            self.texture(f"world/{name}.tga")
        self.texture("world/Aegis_diffuse.tga")
        self.assertEqual([row["path"] for row in self.selected()], ["world/Aegis_diffuse.tga"])

    def test_dds_plt_collisions_and_txi_siblings_are_case_insensitive(self):
        for name in ("dds", "palette", "animated", "ordinary"):
            self.texture(f"world/{name}.tga")
        self.text("other/DDS.DDS", "")
        self.text("other/PALETTE.PLT", "")
        self.text("world/ANIMATED.TXI", "proceduretype cycle\n")
        self.assertEqual([row["path"] for row in self.selected()], ["world/ordinary.tga"])

    def test_txi_sidecars_hold_same_resref_across_haks_case_insensitively(self):
        self.texture("world/animated.tga")
        self.texture("world/nested/reflective.tga")
        self.texture("world/ordinary.tga")
        self.text("other/ANIMATED.TXI", "proceduretype cycle\n")
        self.text("other/nested/REFLECTIVE.txi", "blending additive\n")
        self.assertEqual([row["path"] for row in self.selected()], ["world/ordinary.tga"])

    def test_special_material_slots_bumps_and_cube_faces_are_held(self):
        names = ["diffuse", "normal", "bump", "sky", "shiny", "shiny0"]
        names.extend(f"sky{face}" for face in range(7))
        for name in names:
            self.texture(f"world/{name}.tga")
        self.text("other/material.MTR", "texture0 diffuse\nTeXtUrE1 Normal\n")
        self.text("other/effect.TXI", "BumpMapTexture bump\nEnvMapTexture SKY\nbumpyshinytexture Shiny\n")
        self.assertEqual([row["path"] for row in self.selected()], ["world/diffuse.tga", "world/sky6.tga"])

    def test_actual_alpha_bytes_control_format_even_with_zero_descriptor_bits(self):
        opaque = self.texture("world/opaque.tga")
        binary = self.texture("world/binary.tga", (30, 80, 140, 0))
        smooth = self.texture("world/smooth.tga", (30, 80, 140, 127))
        for path in (binary, smooth):
            data = bytearray(path.read_bytes())
            data[17] &= 0xF0
            path.write_bytes(data)
        rows = {row["path"]: row for row in self.selected()}
        self.assertEqual(rows["world/opaque.tga"]["alpha"], "opaque")
        self.assertEqual(rows["world/opaque.tga"]["format"], "DXT1")
        self.assertEqual(rows["world/binary.tga"]["alpha"], "binary")
        self.assertEqual(rows["world/smooth.tga"]["alpha"], "smooth")
        self.assertEqual(rows["world/binary.tga"]["format"], "DXT5")
        self.assertEqual(rows["world/smooth.tga"]["format"], "DXT5")
        self.assertEqual(rows["world/opaque.tga"]["source_sha256"], hashlib.sha256(opaque.read_bytes()).hexdigest())
        self.assertEqual(rows["world/opaque.tga"]["expected_dds_bytes"], 11064)
        self.assertEqual(rows["world/smooth.tga"]["expected_dds_bytes"], 22000)

    def test_only_configured_nested_directories_are_selected(self):
        self.configure("world/nested")
        self.texture("world/nested/stone.tga")
        self.texture("world/outside.tga")
        self.texture("unused/other.tga")
        self.text("unused/STONE.DDS", "")
        self.assertEqual([row["path"] for row in self.selected()], ["world/nested/stone.tga"])

    def test_configured_path_cannot_escape_checkout(self):
        self.config.write_text(json.dumps({"HakList": [{"Path": "../SWLOR_Haks/../../outside"}]}))
        with self.assertRaisesRegex(ValueError, "escapes"):
            self.selected()

    def test_missing_configured_directory_fails_closed(self):
        self.config.write_text(json.dumps({"HakList": [{"Path": "../SWLOR_Haks/missing"}]}))
        with self.assertRaises(FileNotFoundError):
            self.selected()

    def test_allowlist_only_narrows_selection_and_rejects_traversal(self):
        self.texture("world/stone.tga")
        self.texture("world/other.tga")
        self.texture("world/mi_held.tga")
        allowlist = self.root / "approved.json"
        allowlist.write_text(json.dumps(["WORLD/STONE.TGA", "world/mi_held.tga"]))
        self.assertEqual([row["path"] for row in self.selected(allowlist=allowlist)], ["world/stone.tga"])
        for unsafe in ("../outside.tga", "/outside.tga", "C:\\outside.tga"):
            with self.subTest(unsafe=unsafe):
                allowlist.write_text(json.dumps([unsafe]))
                with self.assertRaisesRegex(ValueError, "relative TGA"):
                    self.selected(allowlist=allowlist)

    def test_conversion_report_entries_can_supply_reviewed_scope(self):
        self.texture("world/stone.tga")
        self.texture("world/other.tga")
        allowlist = self.root / "report.json"
        allowlist.write_text(json.dumps({"entries": [{"path": "world/stone.tga"}]}))
        self.assertEqual([row["path"] for row in self.selected(allowlist=allowlist)], ["world/stone.tga"])

    def test_cli_requires_reviewed_allowlist(self):
        output = self.root / "output.json"
        result = subprocess.run([sys.executable, selector.__file__, "--source-root", str(self.source),
                                 "--config", str(self.config), "--output", str(output)],
                                capture_output=True, text=True, timeout=30)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("--allowlist", result.stderr)
        self.assertFalse(output.exists())

    def test_categories_dimensions_palettes_and_files_that_grow_are_held(self):
        for relative in ("sw_ui/ordinary.tga", "world/mi_map.tga", "world/iit_icon.tga",
                         "world/stone_n.tga", "world/name_is_too_long_texture.tga"):
            self.texture(relative)
        self.texture("world/nonpower.tga", size=(15, 16))
        self.texture("world/tiny.tga", size=(4, 4))
        palette = self.source / "world" / "palette.tga"
        Image.new("P", (128, 128), 0).save(palette, format="TGA")
        Image.new("RGBA", (128, 128), (30, 80, 140, 255)).save(
            self.source / "world" / "compressed.tga", format="TGA", compression="tga_rle")
        self.texture("world/ordinary.tga")
        self.assertEqual([row["path"] for row in self.selected()], ["world/ordinary.tga"])


if __name__ == "__main__":
    unittest.main()
