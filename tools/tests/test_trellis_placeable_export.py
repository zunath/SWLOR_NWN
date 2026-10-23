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

    def test_saturation_mode_picks_bright_colored_light_and_ignores_pale_or_dark_texels(self):
        color = np.array([[[0.7, 0.2, 0.95], [0.95, 0.9, 0.95], [0.2, 0.05, 0.3], [0.5, 0.5, 0.5]]])
        mask = exporter.emission_mask(color, 0.55, 0.15, np, 0.45)
        self.assertAlmostEqual(float(mask[0, 0]), 1.0)
        self.assertEqual(float(mask[0, 1]), 0.0)  # bright but nearly white
        self.assertEqual(float(mask[0, 2]), 0.0)  # saturated but dark
        self.assertEqual(float(mask[0, 3]), 0.0)

    @unittest.skipIf(np is None, 'numpy not installed')
    def test_color_gain_scales_linear_light_and_clips(self):
        from types import SimpleNamespace
        weapon = SimpleNamespace(np=np, linear=lambda x: x ** 2.2, srgb=lambda x: x ** (1 / 2.2))
        base = np.full((2, 2, 3), .5)
        self.assertIs(exporter.apply_color_gain(weapon, base, 1), base)
        brighter = exporter.apply_color_gain(weapon, base, 2)
        self.assertTrue(np.allclose(brighter, (0.5 ** 2.2 * 2) ** (1 / 2.2)))
        self.assertGreater(float(brighter.min()), .5)
        self.assertTrue(np.allclose(exporter.apply_color_gain(weapon, np.ones((1, 1, 3)), 3), 1))
        self.assertLess(float(exporter.apply_color_gain(weapon, base, .5).max()), .5)

    @unittest.skipIf(np is None, 'numpy not installed')
    def test_greeble_draws_panel_detail_on_flat_cells_only_and_is_deterministic(self):
        from types import SimpleNamespace
        weapon = SimpleNamespace(np=np)
        base = np.full((256, 256, 3), .6)
        flat = np.zeros((256, 256, 3))
        flat[..., 2] = 1
        flat = flat * .5 + .5
        self.assertEqual(exporter.apply_greeble(weapon, base, flat, 0, 'a')[0].shape, base.shape)
        self.assertIs(exporter.apply_greeble(weapon, base, flat, 0, 'a')[0], base)
        painted, bevelled = exporter.apply_greeble(weapon, base, flat, 1, 'a')
        again, _ = exporter.apply_greeble(weapon, base, flat, 1, 'a')
        self.assertTrue(np.array_equal(painted, again))
        self.assertGreater(int((painted != .6).any(-1).sum()), 500)
        self.assertGreater(float(np.abs(bevelled - flat).max()), .05)
        self.assertGreater(float(bevelled[..., 2].min()), .5)
        tilted = flat.copy()
        tilted[..., 0] = .9
        untouched, _ = exporter.apply_greeble(weapon, base, tilted, 1, 'a')
        self.assertTrue(np.array_equal(untouched, base))
        small, _ = exporter.apply_greeble(weapon, base[:32, :32], flat[:32, :32], 1, 'a')
        self.assertTrue(np.array_equal(small, base[:32, :32]))

    @unittest.skipIf(np is None, 'numpy not installed')
    def test_wear_darkens_seams_deterministically_and_leaves_clean_input_alone(self):
        from types import SimpleNamespace
        weapon = SimpleNamespace(np=np)
        base = np.full((64, 64, 3), .8)
        normal = np.zeros((64, 64, 3))
        normal[..., 2] = 1
        normal = normal * .5 + .5
        normal[30:34, :, 1] = .9
        self.assertIs(exporter.apply_wear(weapon, base, normal, 0, 'stn_a'), base)
        worn = exporter.apply_wear(weapon, base, normal, .8, 'stn_a')
        self.assertTrue(np.array_equal(worn, exporter.apply_wear(weapon, base, normal, .8, 'stn_a')))
        self.assertFalse(np.array_equal(worn, exporter.apply_wear(weapon, base, normal, .8, 'stn_b')))
        self.assertLess(float(worn.max()), float(base.max()) + 1e-9)
        self.assertLess(float(worn[30:34].mean()), float(worn[:10].mean()))
        self.assertTrue(np.array_equal(exporter.apply_wear(weapon, base, normal[:8, :8], .8, 'x'), base))

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


class SourceTests(unittest.TestCase):
    def test_no_function_reads_a_name_the_module_never_defines(self):
        """Blender-only code paths are not run by these tests; a misspelled or out-of-scope name there fails at export time."""
        import builtins
        import symtable
        source = (ROOT / 'tools/TrellisPlaceableExport.py').read_text(encoding='utf-8')
        table = symtable.symtable(source, 'TrellisPlaceableExport.py', 'exec')
        defined = {s.get_name() for s in table.get_symbols() if s.is_assigned() or s.is_imported() or s.is_namespace()}
        missing = set()

        def walk(scope):
            for symbol in scope.get_symbols():
                name = symbol.get_name()
                if scope.get_type() == 'function' and symbol.is_global() and name not in defined and not hasattr(builtins, name):
                    missing.add(f'{scope.get_name()}: {name}')
            for child in scope.get_children():
                walk(child)

        walk(table)
        self.assertEqual(missing - {'main: __file__'}, set())


class ReorientTests(unittest.TestCase):
    @staticmethod
    def document(index_accessors, offsets=(0, 0)):
        """Two primitives over one buffer; each index accessor is 12 triangle-corner u16 values (24 bytes) at its offset."""
        accessors = [{'bufferView': 0, 'byteOffset': offset, 'componentType': 5123, 'count': 12} for offset in offsets]
        primitives = [{'attributes': {'POSITION': 9}, 'indices': index} for index in index_accessors]
        return {'meshes': [{'primitives': primitives}], 'accessors': accessors, 'bufferViews': [{'buffer': 0, 'byteOffset': 0}]}

    def test_separate_index_data_is_accepted(self):
        exporter.require_separate_index_data(self.document((0, 1), offsets=(0, 24)))

    def test_a_shared_index_accessor_is_rejected_before_anything_is_rewritten(self):
        doc = self.document((0, 0), offsets=(0, 24))
        with self.assertRaises(ValueError):
            exporter.require_separate_index_data(doc)
        with self.assertRaises(ValueError):
            exporter.reorient_gltf(object(), doc, bytes(48))

    def test_distinct_accessors_over_overlapping_bytes_are_rejected(self):
        with self.assertRaises(ValueError):
            exporter.require_separate_index_data(self.document((0, 1), offsets=(0, 12)))

    def test_unindexed_primitives_are_rejected(self):
        doc = self.document((0, 1), offsets=(0, 24))
        del doc['meshes'][0]['primitives'][1]['indices']
        with self.assertRaises(ValueError):
            exporter.require_separate_index_data(doc)


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
        for key, value in (('normal_basis_policy', 'guess'), ('double_sided_policy', 'ignore'), ('authored_angle_limit_degrees', 120), ('authored_angle_limit_degrees', 0.1)):
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.check(**{key: value})

    def test_emission_needs_threshold_and_softness_and_numeric_optionals(self):
        self.assertIn('emission', self.check(emission={'threshold': .8, 'softness': .1}))
        self.assertIn('emission', self.check(emission={'threshold': .8, 'softness': .1, 'gain': 2, 'saturation_min': .4}))
        for emission in ({}, {'softness': .1}, {'threshold': .8}, {'threshold': .8, 'softness': .1, 'gain': 'x'},
                         {'threshold': .8, 'softness': .1, 'saturation_min': float('nan')}, [1, 2]):
            with self.subTest(emission=emission), self.assertRaises(ValueError):
                self.check(emission=emission)

    def test_color_gain_must_be_a_sensible_number(self):
        self.assertIn('color_gain', self.check(color_gain=1.6))
        for gain in (0, .1, 3.5, float('nan'), '2'):
            with self.subTest(gain=gain), self.assertRaises(ValueError):
                self.check(color_gain=gain)

    def test_greeble_must_be_a_fraction(self):
        self.assertIn('greeble', self.check(greeble=.7))
        for greeble in (-.1, 1.5, float('nan'), '1'):
            with self.subTest(greeble=greeble), self.assertRaises(ValueError):
                self.check(greeble=greeble)

    def test_wear_must_be_a_fraction(self):
        self.assertIn('wear', self.check(wear=.5))
        for wear in (-.1, 1.5, float('nan'), '1'):
            with self.subTest(wear=wear), self.assertRaises(ValueError):
                self.check(wear=wear)

    def test_material_texture_names_keep_room_for_the_map_suffixes(self):
        self.assertEqual(exporter.material_texture_name('a', 'stn_forge', 1, 0), 'stn_forge')
        self.assertEqual(exporter.material_texture_name('a', 'stn_forge', 2, 1), 'stn_forge_1')
        with self.assertRaises(ValueError):
            exporter.material_texture_name('a', 'stn_forge_long', 2, 1)
        self.assertEqual(exporter.material_texture_name('a', 'stn_forge_long', 1, 0), 'stn_forge_long')

    def test_an_export_cannot_skip_the_existing_resource_check(self):
        with self.assertRaises(ValueError):
            exporter.require_resource_check('export', False, None)
        exporter.require_resource_check('export', True, None)
        exporter.require_resource_check('export', False, set())
        exporter.require_resource_check('inspect', False, None)

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
