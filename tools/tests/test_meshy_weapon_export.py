"""Meshy two-sided surfaces retain winding and mapped normal directions."""
from pathlib import Path
import hashlib
import json
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import MeshyWeaponExport as meshy
import TrellisWeaponNormals as normals

try:
    import bpy
    HAS_BLENDER = True
except ImportError:
    HAS_BLENDER = False


def source_basis():
    return {'positions': np.array([[[0., 0., 0.], [1., 0., 0.], [0., 1., 0.]]]),
            'uv': np.array([[[0., 0.], [1., 0.], [0., 1.]]]),
            'normals': np.tile([0., 0., 1.], (1, 3, 1)),
            'tangents': np.tile([1., 0., 0.], (1, 3, 1)), 'signs': np.ones((1, 3))}


class MeshyWeaponExportTests(unittest.TestCase):
    @unittest.skipUnless(HAS_BLENDER, 'Run Blender for nested source-node transforms')
    def test_preparation_applies_fit_once_to_parented_meshes(self):
        from mathutils import Vector
        import TrellisWeaponExport as exporter
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            repo = root / 'repo'
            table = repo / 'SWLOR_Haks/sw_2da'
            table.mkdir(parents=True)
            (table / 'baseitems.2da').write_text(
                '2DA V2.0\n\nItemClass ModelType InvSlotWidth InvSlotHeight\n59 wthsh 0 1 1\n')
            bpy.ops.wm.read_factory_settings(use_empty=True)
            material = bpy.data.materials.new('opaque')
            material.use_nodes = True
            objects = []
            for name in ('parent', 'child'):
                mesh = bpy.data.meshes.new(name)
                mesh.from_pydata([(0, 0, 0), (.1, 0, 0), (0, .1, .1)], [], [(0, 1, 2)])
                uv = mesh.uv_layers.new()
                for loop, coordinate in zip(uv.data, ((0, 0), (1, 0), (0, 1))):
                    loop.uv = coordinate
                ob = bpy.data.objects.new(name, mesh)
                bpy.context.collection.objects.link(ob)
                mesh.materials.append(material)
                objects.append(ob)
            objects[0].location = (.1, .2, .3)
            objects[1].parent = objects[0]
            objects[1].location = (.3, .4, .5)
            bpy.context.view_layer.update()
            fit_offset = Vector((.7, -.2, .4))
            expected = sorted(tuple(2 * (ob.matrix_world @ vertex.co) + fit_offset)
                              for ob in objects for vertex in ob.data.vertices)
            source = root / 'source.glb'
            bpy.ops.export_scene.gltf(filepath=str(source), export_format='GLB',
                                      export_texcoords=True, export_normals=True, export_yup=True)
            manifest = root / 'fit.json'
            manifest.write_text(json.dumps({'schema_version': 1, 'assets': [{
                'asset': 'nested', 'source': str(source),
                'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                'base_item': 59, 'model': 'wthsh_011', 'texture': 'nested',
                'texture_size': 512, 'icon_size': [32, 32], 'scale': 2.,
                'orientation': np.eye(3).tolist(), 'source_grip': [0, 0, 0],
                'target_grip': list(fit_offset), 'attachment': {
                    'review_notes': 'Synthetic rigid node hierarchy.',
                    'axis_pairs': [{'source': [0, 1, 0], 'target': [0, 1, 0]},
                                   {'source': [1, 0, 0], 'target': [1, 0, 0]}]}}]}))
            meshy.prepare(manifest, root / 'prepared', repo)
            bpy.ops.wm.read_factory_settings(use_empty=True)
            bpy.ops.import_scene.gltf(filepath=str(root / 'prepared/nested/source.glb'))
            actual = sorted(tuple(ob.matrix_world @ vertex.co)
                            for ob in bpy.context.scene.objects if ob.type == 'MESH'
                            for vertex in ob.data.vertices)
            np.testing.assert_allclose(actual, expected, atol=1e-6)

    def test_explicit_backfaces_reverse_winding_and_keep_front_geometry(self):
        source = source_basis()
        mesh = (source['positions'][0].tolist(), source['uv'][0].tolist(), [(0, 1, 2)],
                source['normals'][0].tolist())
        result = meshy.duplicate_backfaces(mesh)
        self.assertEqual([(0, 1, 2), (5, 4, 3)], result[2])
        np.testing.assert_array_equal(np.asarray(result[0])[:3], source['positions'][0])
        np.testing.assert_array_equal(np.asarray(result[1])[:3], source['uv'][0])
        np.testing.assert_array_equal(np.asarray(result[3])[3:], -source['normals'][0])

    def test_backface_basis_negates_the_complete_mapped_world_normal(self):
        source = source_basis()
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'basis.npz'
            np.savez_compressed(path, **{'metal__' + k: v for k, v in source.items()})
            metadata = {}
            meshy.expand_source_basis(path, ['metal'], metadata)
            with np.load(path, allow_pickle=False) as result:
                n, t, s = (result['metal__' + k] for k in ('normals', 'tangents', 'signs'))
                tangent, bitangent, normal = normals.basis(n, t, s)
                vector = normals.unit(np.array([.2, .3, .9]))
                world = tangent * vector[0] + bitangent * vector[1] + normal * vector[2]
                np.testing.assert_allclose(world[1], -world[0, ::-1], atol=1e-12)
                front = source['positions'][0]
                back = result['metal__positions'][1]
                self.assertLess(np.cross(front[1]-front[0], front[2]-front[0]) @
                                np.cross(back[1]-back[0], back[2]-back[0]), 0)
            self.assertEqual(['metal'], metadata['explicit_backface_materials'])
            self.assertEqual(meshy.producer_sha256(), metadata['double_sided_basis_producer_sha256'])

    def test_unselected_material_is_unchanged(self):
        source = source_basis()
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / 'basis.npz'
            arrays = {material + '__' + k: v for material in ('metal', 'grip') for k, v in source.items()}
            np.savez_compressed(path, **arrays)
            meshy.expand_source_basis(path, ['metal'], {})
            with np.load(path, allow_pickle=False) as result:
                for key, value in source.items(): np.testing.assert_array_equal(value, result['grip__' + key])

    def test_clipped_front_and_back_have_separate_source_ownership(self):
        front = source_basis()
        source = {key: np.concatenate((value, -value[:, ::-1] if key in ('normals', 'tangents', 'signs')
                                       else value[:, ::-1])) for key, value in front.items()}
        clipped = front['positions'] * .5
        for positions, expected in ((clipped, 0), (clipped[:, ::-1], 1)):
            with self.subTest(expected=expected):
                target = {'positions': positions, 'uv': positions[..., :2]}
                matches = normals.match_faces(source, target)
                self.assertEqual(expected, matches[0][0])
                np.testing.assert_allclose(matches[0][1] @ source['positions'][expected], positions[0],atol=1e-12)

    def test_reversed_triangle_is_not_a_source_subdivision(self):
        source = source_basis()
        positions = source['positions'][:, ::-1]
        with self.assertRaisesRegex(ValueError, 'no exact source'):
            normals.match_faces(source, {'positions': positions, 'uv': positions[..., :2]})


if __name__ == '__main__':
    unittest.main(argv=[__file__] if HAS_BLENDER else None)
