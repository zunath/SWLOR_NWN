"""Run under Python, or Blender's Python for the numpy texture tests."""
import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('blaster', ROOT / 'tools/ImportBlasterModel.py')
blaster = importlib.util.module_from_spec(spec)
spec.loader.exec_module(blaster)
try:
    import numpy as np
except ImportError:
    np = None


class BlasterImportTests(unittest.TestCase):
    def setUp(self):
        self.config = dict(schema_version=1, base_item=11, middle_slot=44,
                           texture='blstr_h41', material_mode='opaque', scale=8,
                           rotation_degrees=[90, 180, 0], translation=[0, 0, -.04],
                           attachment=dict(source_muzzle_axis=[0, 1, 0], source_grip_axis=[0, 0, 1]),
                           texture_size=1024,
                           sources={k: k for k in ('model', 'diffuse', 'normal', 'specular')},
                           sha256={k: '0'*64 for k in ('model', 'diffuse', 'normal', 'specular')})

    def test_valid_pistol_manifest(self):
        blaster.validate_manifest(self.config)

    def test_rejects_wrong_base_item_and_invalid_native_slots(self):
        for key, value in [('base_item', 61), ('middle_slot', 0), ('middle_slot', 256),
                           ('middle_slot', 4.4), ('middle_slot', True)]:
            config = copy.deepcopy(self.config)
            config[key] = value
            with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                blaster.validate_manifest(config)

    def test_rejects_unsafe_texture_names_and_invalid_transform(self):
        for key, value in [('texture', '../pistol'), ('texture', 'a'*15),
                           ('scale', 0), ('scale', float('nan')),
                           ('rotation_degrees', [0, 0]), ('translation', [0, 0, float('inf')]),
                           ('material_mode', 'transparent')]:
            config = copy.deepcopy(self.config)
            config[key] = value
            with self.subTest(key=key, value=value), self.assertRaises(ValueError):
                blaster.validate_manifest(config)

    @unittest.skipIf(np is None, 'Run with Blender Python for mathutils')
    def test_attachment_rejects_previous_sideways_transform(self):
        import math
        from mathutils import Euler
        correct = Euler(tuple(math.radians(n) for n in (90, 180, 0))).to_matrix().to_4x4()
        blaster.validate_attachment(correct, self.config)
        sideways = Euler((math.pi, 0, 0)).to_matrix().to_4x4()
        with self.assertRaises(ValueError):
            blaster.validate_attachment(sideways, self.config)

    def test_attachment_requires_nonzero_landmarks(self):
        self.config['attachment']['source_muzzle_axis'] = [0, 0, 0]
        with self.assertRaises(ValueError):
            blaster.validate_manifest(self.config)

    def test_requires_source_hashes(self):
        self.config['sha256'].pop('normal')
        with self.assertRaises(ValueError):
            blaster.validate_manifest(self.config)

    def test_export_rejects_out_of_range_face(self):
        with tempfile.TemporaryDirectory() as directory, self.assertRaises(ValueError):
            blaster.write_mdl(Path(directory)/'test.mdl', 'test', 'test',
                              [([(0, 0, 0)], [(0, 0)], [(0, 1, 2)])])

    def test_export_rejects_missing_uvs(self):
        with tempfile.TemporaryDirectory() as directory, self.assertRaises(ValueError):
            blaster.write_mdl(Path(directory)/'test.mdl', 'test', 'test',
                              [([(0, 0, 0)], [], [(0, 0, 0)])])

    @unittest.skipIf(np is None, 'Run with Blender Python to cover texture conversion')
    def test_normal_unpack_uses_alpha_and_inverted_green(self):
        pixels = np.array([[[0, .5, 1, .5], [1, .2, 0, .8]]], dtype=float)
        result = blaster.unpack_normal(pixels, np)
        np.testing.assert_allclose(result[0, 0], [.5, .5, 1])
        np.testing.assert_allclose(result[0, 1], [.8, .8, (1+(.28**.5))/2])
        pixels[..., 0] = .42
        pixels[..., 2] = .37
        np.testing.assert_allclose(blaster.unpack_normal(pixels, np), result)

    @unittest.skipIf(np is None, 'Run with Blender Python to cover texture conversion')
    def test_quantization_overshoot_produces_finite_unit_normal(self):
        result = blaster.unpack_normal(np.array([[[0, 0, 0, 1.0]]]), np) * 2 - 1
        self.assertTrue(np.isfinite(result).all())
        np.testing.assert_allclose(np.linalg.norm(result, axis=-1), 1)


if __name__ == '__main__':
    unittest.main(argv=['blaster-tests'])
