"""Pure-Python checks for the placeable (crafting station) exporter; Blender stages are covered by the export run itself.

Run with: python -m unittest discover -s tools/tests -p test_trellis_placeable_export.py
"""
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('placeable_export', ROOT / 'tools/TrellisPlaceableExport.py')
exporter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exporter)
try:
    import numpy as np
except ImportError:
    np = None

SQUARE = [(-1.0, -0.5), (1.0, -0.5), (1.0, 0.5), (-1.0, 0.5)]


def item(folder, **overrides):
    source = Path(folder) / 'source.glb'
    source.write_bytes(b'glTF')
    value = {
        'asset': 'forge', 'source': 'source.glb', 'sha256': hashlib.sha256(b'glTF').hexdigest(), 'model': 'swc_stn_forge',
        'short': 'forge', 'texture': 'stn_forge', 'texture_size': 1024, 'yaw_degrees': -90, 'fit': {'width': 1.5},
        'max_size': [1.6, 1.0, 1.5], 'walkmesh': {'prefix': 'stn_', 'inset': 0.1, 'use_nodes': [[0.0, 0.66]]}}
    value.update(overrides)
    return value


class GeometryTests(unittest.TestCase):
    def test_convex_hull_is_counter_clockwise_and_drops_interior_and_collinear_points(self):
        points = SQUARE + [(0.0, 0.0), (0.0, -0.5), (0.5, 0.5)]
        hull = exporter.convex_hull(points)
        self.assertEqual(sorted(hull), sorted(SQUARE))
        self.assertGreater(exporter.polygon_area(hull), 0)
        self.assertAlmostEqual(exporter.polygon_area(hull), 2.0)

    def test_hull_rejects_collinear_input(self):
        with self.assertRaises(ValueError):
            exporter.convex_hull([(0, 0), (1, 1), (2, 2)])

    def test_inset_moves_every_edge_inward_by_the_distance(self):
        inset = exporter.inset_polygon(exporter.convex_hull(SQUARE), 0.1)
        xs, ys = [p[0] for p in inset], [p[1] for p in inset]
        self.assertAlmostEqual(min(xs), -0.9)
        self.assertAlmostEqual(max(xs), 0.9)
        self.assertAlmostEqual(min(ys), -0.4)
        self.assertAlmostEqual(max(ys), 0.4)

    def test_inset_that_collapses_the_polygon_fails(self):
        with self.assertRaises(ValueError):
            exporter.inset_polygon(exporter.convex_hull(SQUARE), 0.6)

    def test_simplify_removes_shallow_vertices_but_keeps_corners(self):
        hull = exporter.convex_hull(SQUARE + [(0.0, -0.52)])
        self.assertEqual(len(hull), 5)
        simplified = exporter.simplify_convex(hull, 0.05)
        self.assertEqual(sorted(simplified), sorted(SQUARE))
        self.assertEqual(len(exporter.simplify_convex(hull, 0.01)), 5)

    def test_signed_distance_is_negative_inside_and_positive_outside(self):
        hull = exporter.convex_hull(SQUARE)
        self.assertAlmostEqual(exporter.signed_distance((0, 0), hull), -0.5)
        self.assertAlmostEqual(exporter.signed_distance((0, 0.8), hull), 0.3)
        self.assertAlmostEqual(exporter.signed_distance((2.0, 0.5), hull), 1.0)

    def test_yaw_minus_ninety_turns_model_minus_x_toward_plus_y(self):
        rows = exporter.rotation_rows({'yaw_degrees': -90})
        x, y = (sum(rows[i][k] * v for k, v in enumerate((-1.0, 0.0, 0.0))) for i in (0, 1))
        self.assertAlmostEqual(x, 0.0)
        self.assertAlmostEqual(y, 1.0)

    def test_orientation_must_be_a_proper_rotation(self):
        with self.assertRaises(ValueError):
            exporter.rotation_rows({'orientation': [[2, 0, 0], [0, 1, 0], [0, 0, 1]]})
        with self.assertRaises(ValueError):
            exporter.rotation_rows({'orientation': [[-1, 0, 0], [0, 1, 0], [0, 0, 1]]})
        with self.assertRaises(ValueError):
            exporter.rotation_rows({'orientation': [[1, 0, 0], [0, 1, 0], [0, 0, 1]], 'yaw_degrees': 5})


class WalkmeshTests(unittest.TestCase):
    def write(self, folder, use_nodes, footprint=None):
        path = Path(folder) / 'swc_stn_forge.pwk'
        path.write_text(exporter.build_pwk('swc_stn_forge', footprint or exporter.convex_hull(SQUARE), use_nodes, 'stn_'), encoding='ascii')
        return path

    def test_pwk_has_one_nonwalkable_footprint_and_engine_readable_use_nodes(self):
        with tempfile.TemporaryDirectory() as folder:
            path = self.write(folder, [(0.0, 0.8), (0.0, -0.8)])
            info = exporter.validate_pwk(path, 'swc_stn_forge', 2)
            self.assertEqual([name for name, _, _ in info['use_nodes']], ['stn_pwk_use01', 'stn_pwk_use02'])
            text = path.read_text()
            self.assertIn('parent swc_stn_forge_pwk', text)
            self.assertIn('node dummy stn_pwk_use01', text)
            # The engine matches the token at a fixed offset: a four-character prefix then pwk_use01.
            self.assertEqual(text[text.index('node dummy stn_pwk_use01') + 11 + 4:][:9], 'pwk_use01')

    def test_pwk_without_use_nodes_keeps_the_engine_default_approach(self):
        with tempfile.TemporaryDirectory() as folder:
            self.assertEqual(exporter.validate_pwk(self.write(folder, []), 'swc_stn_forge', 0)['use_nodes'], [])

    def test_use_node_inside_the_footprint_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                exporter.validate_pwk(self.write(folder, [(0.0, 0.2)]), 'swc_stn_forge')

    def test_use_node_at_the_origin_is_rejected_because_the_engine_ignores_it(self):
        with self.assertRaises(ValueError):
            exporter.build_pwk('swc_stn_forge', exporter.convex_hull(SQUARE), [(0.0, 0.0)], 'stn_')

    def test_use_node_count_must_match_the_export(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                exporter.validate_pwk(self.write(folder, [(0.0, 0.8)]), 'swc_stn_forge', 2)

    def test_prefix_needs_three_characters_and_an_underscore(self):
        for prefix in ('st_', 'stnx', 'STN_', 'stn__'):
            with self.assertRaises(ValueError):
                exporter.build_pwk('swc_stn_forge', exporter.convex_hull(SQUARE), [(0.0, 0.8)], prefix)

    def test_footprint_must_be_counter_clockwise(self):
        with self.assertRaises(ValueError):
            exporter.build_pwk('swc_stn_forge', list(reversed(exporter.convex_hull(SQUARE))), [(0.0, 0.8)], 'stn_')


class MdlTests(unittest.TestCase):
    def mesh(self, count=3):
        verts = [(0, 0, 0), (1, 0, 0), (0, 1, 0)][:count]
        return verts, [(0, 0), (1, 0), (0, 1)][:count], [(0, 1, 2)], [(0, 0, 1)] * count

    def test_text_has_root_dummy_effect_dummies_and_one_material_mesh(self):
        text = exporter.write_mdl_text('swc_stn_forge', 'forge', [('stn_forge', self.mesh())], [-1, -0.5, 0], [1, 0.5, 1.5])
        self.assertIn('classification CHARACTER', text)
        self.assertIn('node dummy swc_stn_forge\n  parent NULL', text)
        for name in exporter.DUMMIES:
            self.assertIn(f'node dummy forge_{name}\n  parent swc_stn_forge', text)
        self.assertEqual(text.count('node trimesh'), 1)
        self.assertIn('materialname stn_forge', text)
        self.assertIn('renderhint NormalAndSpecMapped', text)
        self.assertIn('position 0 0 1.5', text)  # head dummy sits at the top of the model

    def test_ground_dummy_rests_on_the_floor(self):
        text = exporter.write_mdl_text('swc_stn_forge', 'forge', [('stn_forge', self.mesh())], [-1, -0.5, 0], [1, 0.5, 1.5])
        block = text[text.index('node dummy forge_ground'):].split('endnode')[0]
        self.assertIn('position 0 0 0', block)

    def test_mismatched_streams_and_bad_normals_are_rejected(self):
        verts, uvs, faces, normals = self.mesh()
        with self.assertRaises(ValueError):
            exporter.write_mdl_text('m', 's', [('t', (verts, uvs[:2], faces, normals))], [0, 0, 0], [1, 1, 1])
        with self.assertRaises(ValueError):
            exporter.write_mdl_text('m', 's', [('t', (verts, uvs, faces, [(0, 0, 2)] * 3))], [0, 0, 0], [1, 1, 1])
        with self.assertRaises(ValueError):
            exporter.write_mdl_text('m', 's', [('t', (verts, uvs, [(0, 1, 7)], normals))], [0, 0, 0], [1, 1, 1])


@unittest.skipIf(np is None, 'numpy is required')
class MaterialTests(unittest.TestCase):
    def test_emission_mask_is_zero_below_the_threshold_and_one_above_it(self):
        color = np.array([[[0.2, 0.2, 0.2], [0.85, 0.85, 0.85], [1.0, 1.0, 1.0]]])
        mask = exporter.emission_mask(color, 0.8, 0.1, np)
        self.assertEqual(mask[0, 0], 0)
        self.assertAlmostEqual(float(mask[0, 2]), 1.0)
        self.assertTrue(0 < mask[0, 1] < 1)

    def test_snap_negative_z_raises_grazing_texels_onto_the_horizon(self):
        encoded = np.zeros((1, 2, 4), np.float32)
        encoded[0, 0, :3] = (0.5, 0.5, 1.0)
        z = -0.004
        xy = math.sqrt(1 - z * z)
        encoded[0, 1, :3] = (xy * 0.6 * .5 + .5, xy * 0.8 * .5 + .5, z * .5 + .5)
        out, count = exporter.snap_negative_z(np, encoded)
        self.assertEqual(count, 1)
        decoded = out[0, 1, :3] * 2 - 1
        self.assertAlmostEqual(float(decoded[2]), 0.0, places=6)
        self.assertAlmostEqual(float(np.linalg.norm(decoded)), 1.0, places=6)
        np.testing.assert_array_equal(out[0, 0], encoded[0, 0])

    def test_snap_negative_z_rejects_real_back_facing_normals(self):
        encoded = np.zeros((1, 1, 4), np.float32)
        encoded[0, 0, :3] = (0.5, 0.9, 0.3)
        with self.assertRaises(ValueError):
            exporter.snap_negative_z(np, encoded)


class ManifestTests(unittest.TestCase):
    def check(self, **overrides):
        with tempfile.TemporaryDirectory() as folder:
            return exporter.validate_item(item(folder, **overrides), Path(folder))

    def test_complete_item_is_accepted(self):
        self.assertTrue(self.check()['_source'].endswith('source.glb'))

    def test_resource_names_follow_the_native_limits(self):
        for key, bad in (('model', 'swc_stn_a_very_long_name'), ('texture', 'stn_forge_extra_long'), ('short', 'forge_long_short'),
                         ('model', 'Swc_Stn')):
            with self.subTest(key=key, value=bad), self.assertRaises(ValueError):
                self.check(**{key: bad})

    def test_fit_takes_exactly_one_dimension(self):
        for fit in ({}, {'width': 1.5, 'depth': 1.0}, {'size': 1.5}, {'width': -1}):
            with self.subTest(fit=fit), self.assertRaises(ValueError):
                self.check(fit=fit)

    def test_source_hash_must_match(self):
        with self.assertRaises(ValueError):
            self.check(sha256='0' * 64)

    def test_at_most_two_use_nodes_and_none_is_allowed(self):
        self.assertEqual(self.check(walkmesh={'prefix': 'stn_', 'use_nodes': []})['walkmesh']['use_nodes'], [])
        with self.assertRaises(ValueError):
            self.check(walkmesh={'prefix': 'stn_', 'use_nodes': [[0, 1], [0, 2], [0, 3]]})

    def test_existing_hak_resources_block_a_new_export(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                exporter.validate_item(item(folder), Path(folder), {'swc_stn_forge'})
            with self.assertRaises(ValueError):
                exporter.validate_item(item(folder), Path(folder), {'stn_forge_n'})

    def test_policies_are_checked(self):
        for key, value in (('normal_basis_policy', 'guess'), ('double_sided_policy', 'ignore'), ('authored_angle_limit_degrees', 5)):
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.check(**{key: value})

    def test_inspect_only_needs_the_source_and_hash(self):
        with tempfile.TemporaryDirectory() as folder:
            value = item(folder)
            value = {k: value[k] for k in ('asset', 'source', 'sha256')}
            self.assertIn('_source', exporter.validate_item(value, Path(folder), None, True))

    def test_duplicate_models_are_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            first, second = item(folder), item(folder, asset='other', texture='stn_other')
            with self.assertRaises(ValueError):
                exporter.validate_manifest({'schema_version': 1, 'assets': [first, second]}, Path(folder) / 'm.json')


if __name__ == '__main__':
    unittest.main()
