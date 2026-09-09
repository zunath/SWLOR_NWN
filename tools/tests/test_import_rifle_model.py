"""Run in Blender 4.0 to include rifle attachment-space checks."""
import copy
import math
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from ImportRifleModel import validate_rifle
from ImportBlasterModel import validate_attachment
from RenderRifleGrip import hand_pose_transforms


class RifleImportTests(unittest.TestCase):
    def test_manifest_argument_forms_and_missing_values(self):
        from ImportRifleModel import parse_manifest
        from contextlib import redirect_stderr
        from io import StringIO
        for args in (['--manifest', 'rifle.json'], ['--manifest=rifle.json', '--output', 'out']):
            self.assertEqual(parse_manifest(args), Path('rifle.json'))
        for args in ([], ['--manifest']):
            with redirect_stderr(StringIO()), self.assertRaises(SystemExit) as error:
                parse_manifest(args)
            self.assertEqual(error.exception.code, 2)

    def test_emission_requires_matching_dimensions(self):
        import numpy as np
        from ImportBlasterModel import validate_emission_dimensions
        validate_emission_dimensions(np.zeros((4, 4, 3)), np.zeros((4, 4, 4)))
        with self.assertRaisesRegex(ValueError, 'same resolution'):
            validate_emission_dimensions(np.zeros((2, 2, 3)), np.zeros((4, 4, 4)))

    def test_hand_geometry_is_case_insensitive_and_requires_own_rows(self):
        from RenderRifleGrip import hand_geometry, required_block
        rows = 'verts 3\n0 0 0\n1 0 0\n0 1 0\nfaces 1\n0 1 2\n'
        for side, model in (('right', 'PMH0_HANDR001'), ('left', 'PMH0_HANDL001')):
            prefix = f'NEWMODEL {model}\nNODE TRIMESH HAND\n'
            verts, faces, _, _ = hand_geometry(prefix + rows + 'ENDNODE', side)
            self.assertEqual(len(verts), 3)
            self.assertEqual(faces, [(0, 1, 2)])
            for incomplete in ('verts 1\n0 0 0\n', 'faces 1\n0 0 0\n', ''):
                with self.assertRaisesRegex(ValueError, 'verts/faces'):
                    hand_geometry(prefix + incomplete + 'ENDNODE', side)
            with self.assertRaisesRegex(ValueError, 'trimesh'):
                hand_geometry(f'newmodel {model}', side)
        self.assertEqual(required_block(r'node dummy rhand\n(.*?)endnode',
                                       'NODE DUMMY RHAND\nkey\nENDNODE', 'hook'), 'key\n')
        with self.assertRaisesRegex(ValueError, 'Missing hook'):
            required_block(r'node dummy rhand\n(.*?)endnode', '', 'hook')

    def setUp(self):
        self.config = dict(schema_version=1, base_item=7, middle_slot=151,
                           texture="rf_gs02", material_mode="opaque", scale=9.6,
                           rotation_degrees=[90, 180, 0], translation=[-.00521167, .0362191, -.0580922],
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

    def test_rifle_axes_reject_untransformed_reference_vertices(self):
        try:
            from mathutils import Euler
        except ImportError:
            self.skipTest("Run in Blender for attachment transforms")
        rotation = Euler(tuple(math.radians(v) for v in self.config["rotation_degrees"])).to_matrix().to_4x4()
        validate_attachment(rotation, self.config, "rifle")
        for angles in ((180, 0, 90), (0, 0, -90)):
            rotation = Euler(tuple(math.radians(v) for v in angles)).to_matrix().to_4x4()
            with self.subTest(angles=angles), self.assertRaises(ValueError):
                validate_attachment(rotation, self.config, "rifle")

    def test_two_hand_fixture_applies_case_insensitive_ancestor_pose(self):
        try:
            from mathutils import Vector
        except ImportError:
            self.skipTest("Run in Blender for skeletal transforms")
        skeleton = """node dummy Rig
  parent NULL
endnode
node dummy Rbicep_g
  parent Rig
endnode
node dummy rhand_g
  parent Rbicep_g
endnode
node dummy rhand
  parent rhand_g
endnode
node dummy Lbicep_g
  parent Rig
  position 1 0 0
endnode
node dummy lhand_g
  parent Lbicep_g
endnode
endmodelgeom Rig
"""
        for name in ('lbicep_g', 'LBICEP_G', 'Lbicep_g'):
            animation = f"""newanim xbowrdy Rig
node dummy {name}
  parent Rig
  positionkey 1
    0 2 3 4
endnode
doneanim xbowrdy Rig
"""
            with self.subTest(name=name):
                transforms = hand_pose_transforms(skeleton, animation)
                self.assertLess((transforms['l'] @ Vector() - Vector((2, 3, 4))).length, 1e-6)
                self.assertLess((transforms['r'] @ Vector()).length, 1e-6)


if __name__ == "__main__":
    unittest.main(argv=[sys.argv[0]])
