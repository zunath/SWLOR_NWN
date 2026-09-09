"""Run in Blender 4.0 to include rifle attachment-space checks."""
import copy
import math
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from ImportRifleModel import validate_rifle
from ImportBlasterModel import validate_attachment


class RifleImportTests(unittest.TestCase):
    def setUp(self):
        self.config = dict(schema_version=1, base_item=7, middle_slot=151,
                           texture="rf_gs02", material_mode="opaque", scale=9.6,
                           rotation_degrees=[180, 0, 90], translation=[0, 0, 0],
                           texture_size=1024, source_material="ranged_gs02_a01_v01",
                           preserve_emission=True,
                           attachment=dict(source_muzzle_axis=[0, 1, 0], source_grip_axis=[0, 0, 1]),
                           sources=dict(model="rifle_gs02_a01_v01.gr2", diffuse="d.dds", normal="n.dds", specular="s.dds"),
                           sha256={key: "a" * 64 for key in ("model", "diffuse", "normal", "specular")})

    def test_rifle_base_item_and_reviewed_material_required(self):
        validate_rifle(self.config)
        for key, value in (("base_item", 11), ("base_item", 6), ("source_material", ""), ("preserve_emission", False)):
            config = copy.deepcopy(self.config)
            config[key] = value
            with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                validate_rifle(config)

    def test_cannons_and_pistols_cannot_enter_rifle_pipeline(self):
        for name in ("as_a01.gr2", "as_a0x.gr2", "assaultcannon_high35_a01_v02.gr2", "blaster_high41.gr2"):
            config = copy.deepcopy(self.config)
            config["sources"]["model"] = name
            with self.subTest(name=name), self.assertRaises(ValueError):
                validate_rifle(config)

    def test_rifle_axes_reject_pistol_and_upside_down_fit(self):
        try:
            from mathutils import Euler
        except ImportError:
            self.skipTest("Run in Blender for attachment transforms")
        rotation = Euler(tuple(math.radians(v) for v in self.config["rotation_degrees"])).to_matrix().to_4x4()
        validate_attachment(rotation, self.config, "rifle")
        for angles in ((90, 180, 0), (0, 0, -90)):
            rotation = Euler(tuple(math.radians(v) for v in angles)).to_matrix().to_4x4()
            with self.subTest(angles=angles), self.assertRaises(ValueError):
                validate_attachment(rotation, self.config, "rifle")


if __name__ == "__main__":
    unittest.main(argv=[sys.argv[0]])
