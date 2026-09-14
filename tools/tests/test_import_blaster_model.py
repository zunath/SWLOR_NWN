"""Run under Python, or Blender's Python for the numpy texture tests."""
import copy
import importlib.util
import os
import struct
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

try:
    import mathutils
except ImportError:
    mathutils = None

try:
    import bpy
except ImportError:
    bpy = None


class BlasterImportTests(unittest.TestCase):
    @unittest.skipIf(bpy is None or mathutils is None, 'Run with Blender Python for mesh export')
    def test_uv_seam_retains_transformed_custom_normals(self):
        import bpy
        from mathutils import Matrix, Vector
        mesh = bpy.data.meshes.new('normal-seam-test')
        try:
            mesh.from_pydata([(0, 0, 0), (1, 0, 0), (0, 1, 0), (0, 0, 1)], [],
                             [(0, 1, 2), (0, 3, 1)])
            mesh.uv_layers.new()
            for i, uv in enumerate([(0, 0), (1, 0), (0, 1), (.5, 0), (1, 1), (.5, 1)]):
                mesh.uv_layers.active.data[i].uv = uv
            for face in mesh.polygons:
                face.use_smooth = True
            mesh.use_auto_smooth = True
            mesh.normals_split_custom_set([(0, .6, .8)] * 6)
            transform = Matrix.Rotation(.5, 4, 'X') @ Matrix.Scale(3, 4)
            verts, uvs, faces, normals = blaster.export_mesh(mesh, transform)
            self.assertEqual(len(verts), 6)
            expected = (transform.to_3x3().inverted().transposed() @ Vector((0, .6, .8))).normalized()
            for normal in normals:
                self.assertLess((Vector(normal) - expected).length, .0002)
        finally:
            bpy.data.meshes.remove(mesh)

    def test_export_preserves_explicit_normals(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'seamtest.mdl'
            blaster.write_mdl(path, 'seamtest', 'NULL', [(
                [(0, 0, 0), (1, 0, 0), (0, 1, 0)],
                [(0, 0), (1, 0), (0, 1)], [(0, 1, 2)], [(0, .6, .8)] * 3)])
            self.assertIn('normals 3\n    0 0.6 0.8', path.read_text())
            # Opt-in native round trip verifies the compiler honors custom normals,
            # rather than silently regenerating this triangle's (0, 0, 1) normal.
            if os.environ.get('NWN_EE_NWMAIN'):
                spec = importlib.util.spec_from_file_location('compiler', ROOT / 'tools/CompileBlasterModels.py')
                compiler = importlib.util.module_from_spec(spec)
                spec.loader.exec_module(compiler)
                output = Path(directory) / 'compiled'
                compiler.compile_models(os.environ['NWN_EE_NWMAIN'], directory, output, ['seamtest'])
                data = (output / path.name).read_bytes()
                raw = 12 + struct.unpack_from('<I', data, 4)[0]
                # NWN binary trimesh header: normal-array offset is at byte 468.
                mesh = data.index(b'blaster0\0') - 32 + 112
                offset = struct.unpack_from('<I', data, mesh + 468)[0]
                for i in range(3):
                    for actual, expected in zip(struct.unpack_from('<3f', data, raw + offset + i * 12), (0, .6, .8)):
                        self.assertAlmostEqual(actual, expected, places=6)

    def test_export_rejects_invalid_normals(self):
        for normals in ([], [(0, 0, 0)] * 3, [(float('nan'), 0, 1)] * 3):
            with tempfile.TemporaryDirectory() as directory, self.assertRaises(ValueError):
                blaster.write_mdl(Path(directory)/'test.mdl', 'test', 'test', [(
                    [(0, 0, 0), (1, 0, 0), (0, 1, 0)],
                    [(0, 0), (1, 0), (0, 1)], [(0, 1, 2)], normals)])

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

    @unittest.skipIf(mathutils is None, 'Run with Blender Python for mathutils')
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
