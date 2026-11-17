"""AstromechCreatureExport: manifest validation, clip rules, segmentation, tool placement, tint and PLT output."""
from pathlib import Path
import copy
import hashlib
import importlib.util
import math
import os
import re
import sys
import tempfile
import unittest

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import AstromechCreatureExport as astro

TOOL_LENGTHS = {'prod': 0.40, 'blaster': 0.30}
POSITION_NODES = {'rootdummy', 'toolr', 'tooll'}
EXPECTED_CLIPS = [
    'cpause1', 'cpause2', 'cwalk', 'crun', 'creadyr', 'creadyl', 'ca1slashl', 'ca1slashr', 'ca1stab', 'cclosel', 'ccloseh',
    'creach', 'cdodges', 'cdodgelr', 'cparryl', 'cparryr', 'cdamages', 'cdamagel', 'cdamager', 'ckdbck', 'ckdbckps',
    'ckdbckdie', 'cdead', 'cgustandb', 'cguptokdb', 'ctaunt', 'cconjure1', 'ccastout', 'ccastoutlp', 'cspasm', 'chturnl',
    'chturnr', 'cvictory', 'cwalkinj', 'cgetmid', 'cgetmidlp', 'cgetlow', 'cgetlowlp']
WORK_CLIPS = {'cgetmid', 'cgetmidlp', 'cgetlow', 'cgetlowlp'}


def parse_clips(text):
    """{clip: {'length', 'events', 'nodes': {node: {'position': [(t, ...)], 'orientation': [(t, ...)]}}}}"""
    clips, clip, node, block = {}, None, None, None
    for raw in text.splitlines():
        parts = raw.split()
        if parts[0] == 'newanim':
            clip = clips.setdefault(parts[1], {'length': None, 'events': [], 'nodes': {}})
        elif parts[0] == 'length':
            clip['length'] = float(parts[1])
        elif parts[0] == 'event':
            clip['events'].append((float(parts[1]), parts[2]))
        elif parts[0] == 'node':
            node = clip['nodes'].setdefault(parts[2], {})
        elif parts[0] in ('positionkey', 'orientationkey'):
            block = node.setdefault(parts[0][:-3], [])
        elif parts[0] == 'endlist':
            block = None
        elif block is not None:
            block.append(tuple(float(p) for p in parts))
    return clips


def sha(data):
    return hashlib.sha256(data).hexdigest()


def good_manifest(folder=None):
    """A valid manifest; with a folder, real source files whose hashes match."""
    sources = {'chassis.glb': b'chassis', 'prod.glb': b'prod', 'blaster.glb': b'blaster'}
    if folder is not None:
        for name, data in sources.items():
            (Path(folder) / name).write_bytes(data)
    return {
        'schema_version': 1, 'supermodel': 'c_astromech',
        'tools': [
            {'name': 'prod', 'source': 'prod.glb', 'sha256': sha(sources['prod.glb']), 'texture': 'c_astro_prod',
             'texture_size': 512, 'tip_axis': '+x', 'length': 0.4},
            {'name': 'blaster', 'source': 'blaster.glb', 'sha256': sha(sources['blaster.glb']), 'texture': 'c_astro_blast',
             'texture_size': 512, 'tip_axis': '-x', 'length': 0.3}],
        'chassis': [{
            'model': 'c_astro_01', 'source': 'chassis.glb', 'sha256': sha(sources['chassis.glb']), 'material': 'c_astro_01',
            'texture_size': 1024, 'height': 1.05, 'yaw_degrees': 180,
            'segments': {'head_above_z': 0.225, 'leg_beyond_x': 0.213, 'hub': [0.25, 0.0, 0.166], 'dome_axis': [0.0, 0.04]},
            'sockets': {'toolr': [0.065, 0.59, -0.1], 'tooll': [-0.065, 0.49, -0.1]},
            'tint': {'accent_hue': [0.01, 0.13], 'accent_min_saturation': 0.38, 'dark_max_value': 0.3}}]}


class ManifestTests(unittest.TestCase):
    def rejects(self, mutate, folder=None):
        manifest = good_manifest(folder)
        mutate(manifest)
        with self.assertRaises(ValueError):
            astro.validate_manifest(manifest, Path(folder) if folder else None)

    def test_a_good_manifest_passes_with_and_without_files(self):
        astro.validate_manifest(good_manifest())
        with tempfile.TemporaryDirectory() as folder:
            astro.validate_manifest(good_manifest(folder), Path(folder))

    def test_source_hash_mismatch_and_missing_file_are_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            self.rejects(lambda m: m['chassis'][0].update(sha256='0' * 64), folder)
            self.rejects(lambda m: m['tools'][0].update(source='absent.glb'), folder)

    def test_sha256_is_required_and_must_be_hex(self):
        self.rejects(lambda m: m['chassis'][0].pop('sha256'))
        self.rejects(lambda m: m['tools'][1].update(sha256='ABC'))

    def test_schema_version_and_unknown_keys(self):
        self.rejects(lambda m: m.update(schema_version=2))
        self.rejects(lambda m: m.update(extra=1))
        self.rejects(lambda m: m['chassis'][0].update(extra=1))
        self.rejects(lambda m: m['chassis'][0]['segments'].pop('hub'))

    def test_both_tools_are_required_once(self):
        self.rejects(lambda m: m['tools'].pop())
        self.rejects(lambda m: m['tools'][1].update(name='prod'))
        self.rejects(lambda m: m['tools'][0].update(name='saber'))

    def test_tool_fields(self):
        self.rejects(lambda m: m['tools'][0].update(tip_axis='+y'))
        self.rejects(lambda m: m['tools'][0].update(length=0))
        self.rejects(lambda m: m['tools'][0].update(texture_size=256))

    def test_chassis_fields(self):
        self.rejects(lambda m: m['chassis'][0].update(height=0))
        self.rejects(lambda m: m['chassis'][0]['segments'].update(hub=[0.25, 0.0]))
        self.rejects(lambda m: m['chassis'][0]['segments'].update(hub=[-0.25, 0.0, 0.1]))
        self.rejects(lambda m: m['chassis'][0]['sockets'].pop('tooll'))
        self.rejects(lambda m: m['chassis'][0]['tint'].update(accent_hue=[0.2, 0.1]))
        self.rejects(lambda m: m['chassis'][0]['tint'].update(accent_min_saturation=float('nan')))
        self.rejects(lambda m: m['chassis'][0].update(yaw_degrees='180'))

    def test_name_length_limits(self):
        self.rejects(lambda m: m['chassis'][0].update(model='c_astro_model_long'))
        self.rejects(lambda m: m['chassis'][0].update(material='c_astro_material'))
        self.rejects(lambda m: m['chassis'][0].update(material='c_astro_mat_15x'))  # the _n map would pass 16 characters
        self.rejects(lambda m: m['tools'][0].update(texture='c_astro_prod_xx'))
        self.rejects(lambda m: m.update(supermodel='C_Astromech'))
        astro.validate_manifest(good_manifest() | {'supermodel': 'c_astromech'})

    def test_names_must_be_unique(self):
        def duplicate_chassis(m):
            m['chassis'].append(copy.deepcopy(m['chassis'][0]))
        self.rejects(duplicate_chassis)
        self.rejects(lambda m: m['tools'][1].update(texture='c_astro_prod'))
        self.rejects(lambda m: m['chassis'][0].update(model='c_astromech'))


class ClipTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.text = astro.clips_text('c_astromech', TOOL_LENGTHS)
        cls.clips = parse_clips(cls.text)

    def test_every_clip_is_present_exactly_once(self):
        self.assertEqual(sorted(self.clips), sorted(EXPECTED_CLIPS))
        self.assertEqual(self.text.count('newanim '), len(EXPECTED_CLIPS))
        self.assertEqual(self.text.count('doneanim '), len(EXPECTED_CLIPS))

    def test_every_clip_node_hangs_from_a_declared_parent(self):
        """The compiler drops animation nodes that are not connected to the root, so each node's parent must be
        declared earlier in the same clip."""
        blocks = re.findall(r'newanim (\S+) \S+\n(.*?)doneanim', self.text, re.S)
        self.assertTrue(blocks)
        for name, body in blocks:
            declared = set()
            for node, parent in re.findall(r'node \w+ (\S+)\n\s+parent (\S+)', body):
                if parent != 'NULL':
                    self.assertIn(parent, declared, f'{name}: {node} hangs from undeclared {parent}')
                declared.add(node)
            self.assertTrue({'toolr', 'tooll', 'toolr_socket', 'tooll_socket'} <= declared, name)

    def test_whole_shells_stay_in_one_segment(self):
        """A shell that straddles a threshold goes to the side holding its area-weighted centroid."""
        quad = [((0.0, 0.0, 0.0), (0.0, 0.0, 1.0), (0.0, 0.0)), ((1.0, 0.0, 0.0), (0.0, 0.0, 1.0), (1.0, 0.0)),
                ((1.0, 1.0, 0.0), (0.0, 0.0, 1.0), (1.0, 1.0)), ((0.0, 1.0, 0.0), (0.0, 0.0, 1.0), (0.0, 1.0))]
        far = [((5.0, 0.0, 0.0), (0.0, 0.0, 1.0), (0.0, 0.0)), ((6.0, 0.0, 0.0), (0.0, 0.0, 1.0), (1.0, 0.0)),
               ((6.0, 1.0, 0.0), (0.0, 0.0, 1.0), (1.0, 1.0))]
        triangles = [[quad[0], quad[1], quad[2]], [quad[0], quad[2], quad[3]], far]
        centroids = astro.shell_centroids(triangles)
        self.assertEqual(centroids[0], centroids[1])
        self.assertNotEqual(centroids[0], centroids[2])
        for value, expected in zip(centroids[0], (0.5, 0.5, 0.0)):
            self.assertAlmostEqual(value, expected, places=6)

    def test_only_root_and_tools_carry_position_keys(self):
        for name, clip in self.clips.items():
            for node, tracks in clip['nodes'].items():
                if node in astro.SOCKET_PARENTS:
                    self.assertEqual(tracks, {}, f'{name}/{node}: sockets differ per chassis and are never keyed')
                elif node not in POSITION_NODES and node != 'c_astromech':
                    self.assertNotIn('position', tracks, f'{name}/{node}')
                    self.assertIn('orientation', tracks, f'{name}/{node}')

    def test_tools_and_root_always_have_position_keys(self):
        for name, clip in self.clips.items():
            for node in POSITION_NODES:
                self.assertIn('position', clip['nodes'][node], f'{name}/{node}')

    def test_keys_are_sorted_and_inside_the_clip(self):
        for name, clip in self.clips.items():
            for node, tracks in clip['nodes'].items():
                for kind, keys in tracks.items():
                    times = [k[0] for k in keys]
                    self.assertEqual(times, sorted(times), f'{name}/{node}/{kind}')
                    self.assertEqual(len(set(times)), len(times), f'{name}/{node}/{kind} repeats a time')
                    self.assertGreaterEqual(times[0], 0)
                    self.assertLessEqual(times[-1], clip['length'] + 1e-9, f'{name}/{node}/{kind}')

    def test_every_event_is_inside_the_clip(self):
        for name, clip in self.clips.items():
            for time, _ in clip['events']:
                self.assertTrue(0 <= time <= clip['length'], name)

    def test_melee_clips_hit_at_half_a_second_and_thrust_the_prod(self):
        for name in astro.MELEE_CLIPS:
            clip = self.clips[name]
            self.assertIn((0.5, 'hit'), clip['events'], name)
            keys = {k[0]: k[1:] for k in clip['nodes']['toolr']['position']}
            self.assertGreater(keys[0.5][1], 0, f'{name} thrusts beyond the rest position at the hit')
            self.assertAlmostEqual(keys[0.0][1], astro.retract_offset(TOOL_LENGTHS['prod']))
            self.assertAlmostEqual(keys[1.0][1], astro.retract_offset(TOOL_LENGTHS['prod']))
            self.assertEqual(keys[0.3][1], 0, f'{name} is fully extended at the end of the wind-up')

    def test_ready_clips_hold_the_prod_half_extended(self):
        for name in ('creadyr', 'creadyl'):
            ys = {k[2] for k in self.clips[name]['nodes']['toolr']['position']}
            self.assertEqual(ys, {-TOOL_LENGTHS['prod'] / 2})

    def test_blaster_clips(self):
        conjure = {k[0]: k[2] for k in self.clips['cconjure1']['nodes']['tooll']['position']}
        self.assertAlmostEqual(conjure[0.0], astro.retract_offset(TOOL_LENGTHS['blaster']))
        self.assertEqual(conjure[2.0], 0)
        self.assertIn((0.25, 'cast'), self.clips['ccastout']['events'])
        recoil = {k[0]: k[2] for k in self.clips['ccastout']['nodes']['tooll']['position']}
        self.assertLess(recoil[0.32], 0)
        self.assertEqual({k[2] for k in self.clips['ccastoutlp']['nodes']['tooll']['position']}, {0})

    def test_work_clips_bring_the_tool_arm_out(self):
        # Crafting, gathering and item use play the get-mid and get-low loops: the arm comes out and stays out.
        for name in ('cgetmid', 'cgetlow'):
            keys = {k[0]: k[2] for k in self.clips[name]['nodes']['toolr']['position']}
            self.assertAlmostEqual(keys[0.0], astro.retract_offset(TOOL_LENGTHS['prod']))
            self.assertEqual(keys[max(keys)], 0)
            loop = self.clips[name + 'lp']
            ys = [k[2] for k in loop['nodes']['toolr']['position']]
            self.assertEqual(ys[0], ys[-1])
            self.assertGreater(min(ys), astro.retract_offset(TOOL_LENGTHS['prod']) / 2)
            self.assertIn(name + 'lp', astro.LOOPING_CLIPS)

    def test_tools_are_retracted_in_every_other_clip(self):
        keyed = {'toolr': set(astro.MELEE_CLIPS) | {'creadyr', 'creadyl'} | WORK_CLIPS, 'tooll': {'cconjure1', 'ccastout', 'ccastoutlp'}}
        for name, clip in self.clips.items():
            for node, key in (('toolr', 'prod'), ('tooll', 'blaster')):
                if name in keyed[node]:
                    continue
                self.assertEqual({round(k[2], 6) for k in clip['nodes'][node]['position']},
                                 {round(astro.retract_offset(TOOL_LENGTHS[key]), 6)}, f'{name}/{node}')

    def test_looping_clips_end_where_they_start(self):
        for name in astro.LOOPING_CLIPS:
            for node, tracks in self.clips[name]['nodes'].items():
                for kind, keys in tracks.items():
                    first, last = keys[0], keys[-1]
                    if kind == 'orientation':
                        first = first[:4] + (first[4] % math.tau,)
                        last = last[:4] + (last[4] % math.tau,)
                    self.assertEqual(first[1:], last[1:], f'{name}/{node}/{kind}')

    def test_clip_lengths_follow_the_tool_lengths(self):
        longer = parse_clips(astro.clips_text('c_astromech', {'prod': 0.6, 'blaster': 0.3}))
        keys = {k[0]: k[2] for k in longer['ca1stab']['nodes']['toolr']['position']}
        self.assertAlmostEqual(keys[0.0], -0.62)

    def test_supermodel_text_holds_the_tree_and_the_clips(self):
        text = astro.supermodel_text('c_astromech', TOOL_LENGTHS)
        self.assertIn('setsupermodel c_astromech NULL', text)
        geometry = text.split('endmodelgeom', 1)[0]
        self.assertNotIn('trimesh', text)
        nodes = re.findall(r'^node dummy (\S+)\n  parent (\S+)', geometry, re.M)
        parents = dict(nodes)
        for node, parent in (('toolr', 'toolr_socket'), ('tooll', 'tooll_socket'), ('toolr_socket', 'torso'), ('head', 'torso'),
                             ('legl', 'rootdummy'), ('torso', 'rootdummy'), ('headconjure', 'head')):
            self.assertEqual(parents[node], parent)
        self.assertEqual(text.count('newanim '), len(EXPECTED_CLIPS))
        self.assertTrue(text.rstrip().endswith('donemodel c_astromech'))


def synthetic_triangles():
    """A body triangle, a dome triangle, a right and a left leg triangle in source coordinates (yaw 180 flips X and Y)."""
    def tri(p):
        return [((p[0] + dx, p[1] + dy, p[2] + dz), (0.0, -1.0, 0.0), (u, v)) for (dx, dy, dz), (u, v) in
                (((0, 0, 0), (0, 0)), ((0.01, 0, 0), (1, 0)), ((0, 0, 0.01), (0, 1)))]
    return [tri((0.0, 0.0, 0.1)), tri((0.0, 0.0, 0.3)), tri((0.3, 0.0, 0.1)), tri((-0.3, 0.0, 0.1))]


class SegmentationTests(unittest.TestCase):
    segments = good_manifest()['chassis'][0]['segments']

    def test_centroids_pick_the_segment(self):
        frame = astro.make_frame(0.0, 0.5, 1.05, 180)
        pick = lambda c: astro.segment_name(c, frame, self.segments)
        self.assertEqual(pick((0.0, 0.0, 0.1)), 'body')
        self.assertEqual(pick((0.0, 0.0, 0.3)), 'head')
        self.assertEqual(pick((0.0, 0.0, 0.225)), 'body')
        self.assertEqual(pick((0.3, 0.0, 0.3)), 'legl')    # the 180 degree turn puts source +X on game -X
        self.assertEqual(pick((-0.3, 0.0, 0.1)), 'legr')
        self.assertEqual(pick((0.213, 0.0, 0.1)), 'body')

    def test_yaw_zero_keeps_the_sides(self):
        frame = astro.make_frame(0.0, 0.5, 1.0, 0)
        self.assertEqual(astro.segment_name((0.3, 0, 0.1), frame, self.segments), 'legr')

    def test_frame_turns_and_scales(self):
        frame = astro.make_frame(-0.1, 0.4, 1.0, 180)
        x, y, z = astro.to_game((0.1, 0.2, 0.4), frame)
        self.assertAlmostEqual(x, -0.2)
        self.assertAlmostEqual(y, -0.4)
        self.assertAlmostEqual(z, 1.0)
        n = astro.to_game_normal((0.0, -1.0, 0.0), frame)
        self.assertAlmostEqual(n[1], 1.0)

    def test_pivots(self):
        frame = astro.make_frame(0.0, 0.5, 1.0, 180)
        pivots = astro.chassis_pivots(frame, self.segments)
        self.assertAlmostEqual(pivots['body'][2], 0.166 * 2)
        self.assertEqual(pivots['legl'][0], -pivots['legr'][0])
        self.assertAlmostEqual(pivots['head'][1], -0.04 * 2)
        self.assertAlmostEqual(pivots['head'][2], 0.225 * 2)

    def test_every_segment_gets_its_triangle_relative_to_its_pivot(self):
        frame = astro.make_frame(0.0, 0.5, 1.0, 180)
        meshes, pivots = astro.chassis_meshes(synthetic_triangles(), frame, self.segments)
        self.assertEqual({k: len(m['faces']) for k, m in meshes.items()}, {'body': 1, 'head': 1, 'legl': 1, 'legr': 1})
        first = meshes['head']['verts'][0]
        self.assertAlmostEqual(first[2], 0.6 - pivots['head'][2])
        self.assertTrue(all(len(m['verts']) == 3 for m in meshes.values()))

    def test_an_empty_segment_fails_loudly(self):
        frame = astro.make_frame(0.0, 0.5, 1.0, 180)
        with self.assertRaisesRegex(ValueError, 'legr is empty'):
            astro.chassis_meshes(synthetic_triangles()[:3], frame, self.segments)

    def test_welding_shares_equal_corners_only(self):
        corner = lambda uv: ((0.0, 0.0, 0.1), (0.0, -1.0, 0.0), uv)
        other = lambda x: ((x, 0.0, 0.1), (0.0, -1.0, 0.0), (0.5, 0.5))
        triangles = [[corner((0, 0)), other(0.01), other(0.02)], [corner((0, 0)), other(0.02), other(0.03)],
                     [corner((1, 1)), other(0.01), other(0.03)]]
        mesh = astro.weld_triangles(triangles, lambda c: 'a', lambda p: p, lambda n: n, {'a': (0, 0, 0)})['a']
        self.assertEqual(len(mesh['verts']), 5)
        self.assertEqual(mesh['faces'][0][0], mesh['faces'][1][0])
        self.assertNotEqual(mesh['faces'][0][0], mesh['faces'][2][0])

    def test_mesh_size_limit(self):
        with self.assertRaises(ValueError):
            astro.check_meshes({'head': {'verts': [(0, 0, 0)] * 65536, 'faces': [(0, 0, 0)]}})


class ToolTests(unittest.TestCase):
    def tool_triangles(self):
        """A bar along X from -0.5 to 0.5 with a bump toward +Z and a side offset in Y."""
        pts = [(-0.5, 0.0, 0.0), (0.5, 0.0, 0.0), (0.5, 0.2, 0.1), (-0.5, 0.2, 0.1)]
        normal, uv = (0.0, 0.0, 1.0), (0.0, 0.0)
        return [[(pts[0], normal, uv), (pts[1], normal, uv), (pts[2], normal, (1.0, 0.0))],
                [(pts[0], normal, uv), (pts[2], normal, uv), (pts[3], normal, (0.0, 1.0))]]

    def test_tip_at_the_origin_and_body_toward_minus_y(self):
        for axis in ('+x', '-x'):
            mesh = astro.tool_mesh(self.tool_triangles(), axis, 0.4)
            lo, hi = astro.mesh_bounds(mesh['verts'])
            self.assertAlmostEqual(hi[1], 0.0)
            self.assertAlmostEqual(lo[1], -0.4)
            self.assertAlmostEqual((lo[0] + hi[0]) / 2, 0.0)
            self.assertAlmostEqual((lo[2] + hi[2]) / 2, 0.0)

    def test_the_tip_end_lands_at_the_origin(self):
        points = [(-0.5, 0.0, 0.0), (0.5, 0.0, 0.0)]
        to_pos, _ = astro.tool_frame(points, '-x', 1.0)
        self.assertAlmostEqual(to_pos((-0.5, 0.0, 0.0))[1], 0.0)
        self.assertAlmostEqual(to_pos((0.5, 0.0, 0.0))[1], -1.0)
        to_pos, _ = astro.tool_frame(points, '+x', 1.0)
        self.assertAlmostEqual(to_pos((0.5, 0.0, 0.0))[1], 0.0)

    def test_rotation_keeps_handedness(self):
        for axis in ('+x', '-x'):
            to_pos, _ = astro.tool_frame([(-1.0, 0, 0), (1.0, 0, 0)], axis, 2.0)
            o, a, b, c = (np.array(to_pos(p)) for p in ((0, 0, 0), (1, 0, 0), (0, 1, 0), (0, 0, 1)))
            self.assertGreater(np.dot(np.cross(a - o, b - o), c - o), 0)

    def test_retracted_bounds_and_the_inside_check(self):
        verts = [(-0.03, -0.4, -0.03), (0.03, 0.0, 0.03)]
        bounds = astro.retracted_tool_bounds(verts, (0.1, 0.59, -0.1), 0.4)
        self.assertAlmostEqual(bounds[1][1], 0.59 - 0.42)
        outer = ((-0.2, -0.3, -0.5), (0.2, 0.2, 0.1))
        self.assertTrue(astro.bounds_inside(bounds, outer))
        self.assertFalse(astro.bounds_inside(astro.retracted_tool_bounds(verts, (0.1, 0.7, -0.1), 0.4), outer))

    def test_probe_points_in_a_closed_mesh(self):
        corners = [(x, y, z) for x in (0, 1) for y in (0, 1) for z in (0, 1)]
        faces = [(0, 1, 3), (0, 3, 2), (4, 6, 7), (4, 7, 5), (0, 4, 5), (0, 5, 1), (2, 3, 7), (2, 7, 6), (0, 2, 6), (0, 6, 4),
                 (1, 5, 7), (1, 7, 3)]
        inside = astro.points_inside_mesh([(0.5, 0.5, 0.5), (0.2, 0.7, 0.3), (1.5, 0.5, 0.5), (0.5, -0.2, 0.5)], corners, faces)
        self.assertEqual(inside, [True, True, False, False])
        self.assertEqual(len(astro.bounds_probe_points(((0, 0, 0), (1, 1, 1)))), 9)


class TintAndPltTests(unittest.TestCase):
    tint = good_manifest()['chassis'][0]['tint']

    def test_pixels_fall_into_the_three_layers(self):
        rgb = np.array([[[0.9, 0.45, 0.1], [0.1, 0.1, 0.12], [0.7, 0.7, 0.72], [0.2, 0.5, 0.9]]], np.float32)
        shade, layer = astro.classify_tint(rgb, self.tint)
        self.assertEqual(layer.tolist(), [[astro.LAYER_ACCENT, astro.LAYER_DARK, astro.LAYER_BODY, astro.LAYER_BODY]])
        self.assertEqual(shade.dtype, np.uint8)
        self.assertEqual(shade.ravel()[:2].tolist(), [128, 128])   # single-texel layers are shifted to the midtone
        self.assertAlmostEqual(float(np.median(shade[0, 2:])), 128, delta=1)
        coverage = astro.layer_coverage(layer)
        self.assertAlmostEqual(sum(coverage.values()), 1.0)

    def read_plt(self, raw):
        """The reader in GenerateTintMapAssets.read_plt, offset 24, one (shade, layer) pair per texel."""
        self.assertEqual(raw[:8], b'PLT V1  ')
        import struct
        width, height = struct.unpack_from('<II', raw, 16)
        self.assertEqual(len(raw), 24 + width * height * 2)
        pixels = np.frombuffer(raw, dtype=np.uint8, offset=24).reshape(height, width, 2)
        self.assertFalse((pixels[:, :, 1] > 9).any())
        return width, height, pixels[:, :, 0], pixels[:, :, 1]

    def test_plt_round_trip(self):
        shade = np.arange(12, dtype=np.uint8).reshape(3, 4) * 20
        layer = (np.arange(12, dtype=np.uint8).reshape(3, 4) % 10).astype(np.uint8)
        width, height, shade_back, layer_back = self.read_plt(astro.plt_bytes(shade, layer))
        self.assertEqual((width, height), (4, 3))
        self.assertTrue((shade_back == shade).all() and (layer_back == layer).all())

    def test_plt_rejects_bad_layers_and_shapes(self):
        with self.assertRaises(ValueError):
            astro.plt_bytes(np.zeros((2, 2), np.uint8), np.full((2, 2), 10, np.uint8))
        with self.assertRaises(ValueError):
            astro.plt_bytes(np.zeros((2, 2), np.uint8), np.zeros((2, 3), np.uint8))

    def test_plt_matches_the_hak_generator_reader_when_available(self):
        root = os.environ.get('SWLOR_HAKS_ROOT')
        generator = Path(root) / 'tools/GenerateTintMapAssets.py' if root else None
        if not generator or not generator.is_file():
            self.skipTest('Set SWLOR_HAKS_ROOT to a SWLOR_Haks checkout to compare with GenerateTintMapAssets.read_plt')
        spec = importlib.util.spec_from_file_location('hak_tint_generator', generator)
        module = importlib.util.module_from_spec(spec)
        sys.path.insert(0, str(generator.parent))
        try:
            spec.loader.exec_module(module)
        except Exception as error:
            self.skipTest(f'Hak generator did not import: {error}')
        shade = np.arange(64, dtype=np.uint8).reshape(8, 8)
        layer = (np.arange(64, dtype=np.uint8).reshape(8, 8) % 10).astype(np.uint8)
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'c_test.plt'
            path.write_bytes(astro.plt_bytes(shade, layer))
            width, height, shade_back, layer_back, _ = module.read_plt(path)
        self.assertEqual((width, height), (8, 8))
        self.assertTrue((shade_back == shade).all() and (layer_back == layer).all())

    def test_starter_material_binds_the_normal_map_and_no_specular_or_roughness(self):
        text = astro.starter_mtr('c_astro_01')
        self.assertIn('renderhint NormalAndSpecMapped', text)
        self.assertIn('texture1 c_astro_01_n', text)
        self.assertNotIn('c_astro_01_s', text)
        self.assertNotIn('c_astro_01_r', text)
        self.assertEqual(re.findall(r'^texture[23] (\S+)$', text, re.M), ['null', 'null'])

    def test_tool_material_binds_all_four_maps(self):
        text = astro.tool_mtr('c_astro_prod')
        for suffix in ('', '_n', '_s', '_r'):
            self.assertIn(f'c_astro_prod{suffix}\n', text)


class ChassisTextTests(unittest.TestCase):
    def build(self):
        item = good_manifest()['chassis'][0]
        frame = astro.make_frame(0.0, 0.5, 1.05, 180)
        meshes, pivots = astro.chassis_meshes(synthetic_triangles(), frame, item['segments'])
        tools = {'prod': {'mesh': meshes['body'], 'texture': 'c_astro_prod'}, 'blaster': {'mesh': meshes['body'], 'texture': 'c_astro_blast'}}
        return astro.chassis_text(item, 'c_astromech', pivots, meshes, tools)

    def test_structure_and_no_animation(self):
        text = self.build()
        self.assertIn('setsupermodel c_astro_01 c_astromech', text)
        self.assertNotIn('newanim', text)
        parents = dict(re.findall(r'^node (?:dummy|trimesh) (\S+)\n  parent (\S+)', text, re.M))
        for node, parent in (('rootdummy', 'c_astro_01'), ('torso', 'rootdummy'), ('body', 'torso'), ('head', 'torso'),
                             ('toolr_socket', 'torso'), ('toolr', 'toolr_socket'), ('tooll_socket', 'torso'),
                             ('tooll', 'tooll_socket'), ('legl', 'rootdummy'), ('legr', 'rootdummy'), ('headconjure', 'head')):
            self.assertEqual(parents[node], parent)
        self.assertEqual(text.count('node trimesh'), 6)

    def test_materials(self):
        text = self.build()
        self.assertEqual(text.count('bitmap c_astro_01\n'), 4)
        self.assertEqual(text.count('materialname c_astro_01\n'), 4)
        self.assertIn('bitmap c_astro_prod\n', text)
        self.assertEqual(text.count('renderhint NormalAndSpecMapped'), 6)
        self.assertIn('node dummy toolr_socket\n  parent torso\n  position 0.065 0.59 -0.1', text)
        self.assertTrue(text.isascii())

    def test_the_tool_sits_at_its_socket_origin(self):
        text = self.build()
        self.assertRegex(text, r'node trimesh toolr\n  parent toolr_socket\n  position 0 0 0\n')


def gr2_chassis(folder=None, model='c_astro_02'):
    """A valid gr2 chassis entry; with a folder, real files whose hashes match."""
    files = {'m.gr2': b'model', 'skel.gr2': b'skeleton', 'd.dds': b'diffuse', 'n.dds': b'normal'}
    if folder is not None:
        for name, data in files.items():
            (Path(folder) / name).write_bytes(data)
    return {'kind': 'gr2', 'model': model, 'source': 'm.gr2', 'sha256': sha(files['m.gr2']), 'skeleton': 'skel.gr2',
            'skeleton_sha256': sha(files['skel.gr2']),
            'maps': {'diffuse': 'd.dds', 'normal': 'n.dds'},
            'maps_sha256': {'diffuse': sha(files['d.dds']), 'normal': sha(files['n.dds'])},
            'material': model, 'texture_size': 1024, 'height': 1.05,
            'tint': {'accent_min_saturation': 0.35, 'dark_max_value': 0.25}}


def gr2_manifest(folder=None):
    return {'schema_version': 1, 'supermodel': 'c_astromech', 'tool_lengths': {'prod': 0.4, 'blaster': 0.3},
            'chassis': [gr2_chassis(folder)]}


class Gr2ManifestTests(unittest.TestCase):
    def rejects(self, mutate, folder=None):
        manifest = gr2_manifest(folder)
        mutate(manifest)
        with self.assertRaises(ValueError):
            astro.validate_manifest(manifest, Path(folder) if folder else None)

    def test_a_good_gr2_manifest_passes_with_and_without_files(self):
        astro.validate_manifest(gr2_manifest())
        with tempfile.TemporaryDirectory() as folder:
            astro.validate_manifest(gr2_manifest(folder), Path(folder))

    def test_hash_mismatch_for_any_source_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            self.rejects(lambda m: m['chassis'][0].update(skeleton_sha256='0' * 64), folder)
            self.rejects(lambda m: m['chassis'][0]['maps_sha256'].update(normal='1' * 64), folder)
            self.rejects(lambda m: m['chassis'][0]['maps'].update(diffuse='absent.dds'), folder)

    def test_bad_fields(self):
        self.rejects(lambda m: m['chassis'][0]['maps'].pop('normal'))
        self.rejects(lambda m: m['chassis'][0].update(material='c_astro_material'))
        self.rejects(lambda m: m['chassis'][0].update(texture_size=256))
        self.rejects(lambda m: m['chassis'][0].update(tool_arm='yes'))
        self.rejects(lambda m: m['chassis'][0].update(bone_segments={'Gun9': 'wing'}))
        self.rejects(lambda m: m['chassis'][0].update(sockets={'toolr': [0, 0, 0], 'tooll': [0, 0, 0]}))
        self.rejects(lambda m: m['chassis'][0].update(yaw_degrees='180'))
        self.rejects(lambda m: m['chassis'][0].update(kind='fbx'))
        self.rejects(lambda m: m['chassis'][0]['tint'].pop('dark_max_value'))

    def test_tool_lengths_rules(self):
        self.rejects(lambda m: m.pop('tool_lengths'))
        self.rejects(lambda m: m['tool_lengths'].update(prod=0))
        self.rejects(lambda m: m['tool_lengths'].pop('blaster'))
        self.rejects(lambda m: m.update(tools=good_manifest()['tools']))
        self.assertEqual(astro.manifest_tool_lengths(gr2_manifest()), {'prod': 0.4, 'blaster': 0.3})
        self.assertEqual(astro.manifest_tool_lengths(good_manifest()), {'prod': 0.4, 'blaster': 0.3})

    def test_glb_chassis_still_need_tools(self):
        manifest = good_manifest()
        manifest.pop('tools')
        manifest['tool_lengths'] = {'prod': 0.4, 'blaster': 0.3}
        with self.assertRaises(ValueError):
            astro.validate_manifest(manifest)

    def test_mixed_glb_and_gr2_validates(self):
        manifest = good_manifest()
        manifest['chassis'].append(gr2_chassis())
        astro.validate_manifest(manifest)
        manifest['chassis'].append(gr2_chassis(model='c_astro_03') | {'material': 'c_astro_02'})
        with self.assertRaises(ValueError):
            astro.validate_manifest(manifest)

    def test_tint_options_are_validated(self):
        for tint in ({'panel_hue': [0.2, 0.2]}, {'panel_hue': [0.2]}, {'accent_hue': [0.5, 0.1]}):
            self.rejects(lambda m, t=tint: m['chassis'][0]['tint'].update(t))
        manifest = gr2_manifest()
        manifest['chassis'][0]['tint'].update(panel_hue=[0.93, 0.04], accent_hue=[0, 1])
        astro.validate_manifest(manifest)


class Gr2SegmentTests(unittest.TestCase):
    table = astro.bone_segment_table(None, True)

    def test_default_table_maps_every_geometry_bone(self):
        for bone, segment in (('Head', 'head'), ('Gun3', 'head'), ('HeadTop', 'head'), ('RightKnee', 'right_leg'),
                              ('LeftAnkle', 'left_leg'), ('Tool2', 'toolr'), ('CenterWheel', 'body'), ('Root', 'body'),
                              ('ToolDoor', 'body')):
            self.assertEqual(astro.bone_segment(bone, self.table), segment)

    def test_unknown_bone_is_an_error_naming_it(self):
        with self.assertRaisesRegex(ValueError, 'Tentacle'):
            astro.bone_segment('Tentacle', self.table)

    def test_overrides_and_no_tool_arm(self):
        table = astro.bone_segment_table({'Gun5': 'body', 'Tentacle': 'toolr'}, True)
        self.assertEqual(table['Gun5'], 'body')
        self.assertEqual(table['Tentacle'], 'toolr')
        flat = astro.bone_segment_table({'Tentacle': 'toolr'}, False)
        self.assertEqual({flat[b] for b in ('Tool1', 'Tool3', 'Tentacle')}, {'body'})

    def test_dominant_bone_and_majority_rule(self):
        self.assertEqual(astro.dominant_bone([('A', 0.2), ('B', 0.7), ('C', 0.1)]), 'B')
        self.assertEqual(astro.dominant_bone([('A', 0.5), ('B', 0.5)]), 'A')
        with self.assertRaises(ValueError):
            astro.dominant_bone([])
        self.assertEqual(astro.triangle_segment(['head', 'body', 'body']), 'body')
        self.assertEqual(astro.triangle_segment(['body', 'head', 'body']), 'body')
        self.assertEqual(astro.triangle_segment(['head', 'body', 'toolr']), 'head')

    def test_sides_follow_game_x_not_bone_names(self):
        self.assertEqual(astro.leg_side_names({'right_leg': -0.3, 'left_leg': 0.3}), {'right_leg': 'legl', 'left_leg': 'legr'})
        self.assertEqual(astro.leg_side_names({'right_leg': 0.3, 'left_leg': -0.3}), {'right_leg': 'legr', 'left_leg': 'legl'})
        with self.assertRaises(ValueError):
            astro.leg_side_names({'right_leg': 0.3, 'left_leg': 0.2})
        with self.assertRaises(ValueError):
            astro.leg_side_names({'right_leg': 0.3})

    bones = {'RightHip': (-0.04, -0.01, 0.08), 'LeftHip': (0.04, -0.01, 0.08), 'Head': (0.0, 0.0, 0.10)}

    def test_pivots_come_from_the_skeleton(self):
        frame = astro.make_frame(0.0, 0.15, 1.5, 180)
        pivots = astro.gr2_pivots(frame, self.bones, {'right_leg': 'legl', 'left_leg': 'legr'})
        self.assertEqual(pivots['body'][0], 0.0)
        self.assertAlmostEqual(pivots['body'][1], 0.1)
        self.assertAlmostEqual(pivots['body'][2], 0.8)
        self.assertAlmostEqual(pivots['head'][2], 1.0)
        self.assertEqual(pivots['legl'], astro.to_game((-0.04, -0.01, 0.08), frame))
        with self.assertRaisesRegex(ValueError, 'LeftHip'):
            astro.gr2_pivots(frame, {k: v for k, v in self.bones.items() if k != 'LeftHip'}, {})

    def test_meshes_are_cut_by_bone_and_sides_by_x(self):
        def tri(x, z):
            return [((x + dx, 0.0, z + dz), (0.0, -1.0, 0.0), (u, v)) for (dx, dz), (u, v) in
                    (((0, 0), (0, 0)), ((0.01, 0), (1, 0)), ((0, 0.01), (0, 1)))]
        triangles = [tri(0.0, 0.05), tri(0.0, 0.12), tri(-0.04, 0.03), tri(0.04, 0.03), tri(0.0, 0.07)]
        bones = [['Root'] * 3, ['Head', 'Head', 'Root'], ['RightKnee'] * 3, ['LeftKnee'] * 3, ['Tool1', 'Tool2', 'Root']]
        frame = astro.make_frame(0.0, 0.15, 1.5, 180)
        meshes, pivots, sides, socket = astro.gr2_meshes(triangles, bones, self.table, frame, self.bones)
        self.assertEqual({k: len(m['faces']) for k, m in meshes.items()}, {'head': 1, 'body': 1, 'legl': 1, 'legr': 1, 'toolr': 1})
        self.assertEqual(sides, {'right_leg': 'legr', 'left_leg': 'legl'})  # yaw 180 puts source -X on game +X
        lo, hi = astro.mesh_bounds(meshes['toolr']['verts'])
        self.assertAlmostEqual(hi[1], 0.0)
        self.assertEqual(len(socket), 3)

    def test_front_from_the_tool_arm_and_centre_leg(self):
        yaw, evidence = astro.decide_front((0.0, 0.0), [(0.0, -0.05, 0.0)], (0.0, 0.05))
        self.assertEqual(yaw, 180.0)
        self.assertEqual(evidence['decided_from'], 'tool_arm')
        self.assertEqual(astro.decide_front((0.0, 0.0), [], (0.0, -0.05))[0], 0.0)
        self.assertEqual(astro.decide_front((0.0, 0.0), [(0.05, 0.0, 0.0)], None)[0], 90.0)
        with self.assertRaises(ValueError):
            astro.decide_front((0.0, 0.0), [(0.0, -0.05, 0.0)], (0.0, -0.05))
        with self.assertRaises(ValueError):
            astro.decide_front((0.0, 0.0), [], None)


class TintOptionTests(unittest.TestCase):
    base = {'accent_min_saturation': 0.35, 'dark_max_value': 0.25}

    def layers(self, pixels, **tint):
        rgb = np.array([pixels], np.float32)
        return astro.classify_tint(rgb, self.base | tint)[1][0].tolist()

    red, green, grey, black = [0.8, 0.1, 0.1], [0.1, 0.8, 0.1], [0.6, 0.6, 0.6], [0.05, 0.05, 0.05]

    def test_omitted_or_full_accent_hue_means_any_hue_including_red(self):
        expected = [astro.LAYER_ACCENT, astro.LAYER_ACCENT, astro.LAYER_BODY, astro.LAYER_DARK]
        self.assertEqual(self.layers([self.red, self.green, self.grey, self.black]), expected)
        self.assertEqual(self.layers([self.red, self.green, self.grey, self.black], accent_hue=[0, 1]), expected)

    def test_panel_hue_makes_that_paint_the_body_layer(self):
        wrapped = self.layers([self.red, self.green, self.grey, self.black], panel_hue=[0.93, 0.04])
        self.assertEqual(wrapped, [astro.LAYER_BODY, astro.LAYER_ACCENT, astro.LAYER_BODY, astro.LAYER_DARK])
        plain = self.layers([self.red, self.green], panel_hue=[0.25, 0.4])
        self.assertEqual(plain, [astro.LAYER_ACCENT, astro.LAYER_BODY])

    def test_every_layer_is_centred_on_the_palette_midtone(self):
        rng = np.random.default_rng(3)
        rgb = np.concatenate([rng.uniform(0.55, 0.95, (64, 64, 3)), rng.uniform(0.02, 0.2, (64, 64, 3))], axis=0)
        rgb = rgb.astype(np.float32)
        shade, layer = astro.classify_tint(rgb, self.base)
        for index in (astro.LAYER_DARK, astro.LAYER_BODY):
            values = shade[layer == index]
            low, mid, high = np.percentile(values, (5, 50, 95))
            self.assertAlmostEqual(mid, 128, delta=1.5)
            self.assertAlmostEqual(low, 64, delta=2.5)
            self.assertAlmostEqual(high, 191, delta=2.5)

    def test_centre_curve_is_monotone_and_clamped(self):
        values = np.concatenate([np.linspace(0.6, 0.9, 200), [0.0, 1.0]])
        curve = astro.centre_curve(values)
        x = np.linspace(0, 1, 501)
        y = curve(x)
        self.assertTrue((np.diff(y) >= -1e-9).all())
        self.assertGreaterEqual(float(y.min()), astro.SHADE_CLAMP[0] - 1e-9)
        self.assertLessEqual(float(y.max()), astro.SHADE_CLAMP[1] + 1e-9)
        self.assertAlmostEqual(float(curve(np.median(values))), 0.5, places=6)

    def test_degenerate_layers_are_shifted_to_the_midtone(self):
        for values in (np.full(40, 0.9), np.array([0.2, 0.4, 0.3])):
            curve = astro.centre_curve(values)
            self.assertAlmostEqual(float(curve(np.median(values))), 0.5, places=6)
        rgb = np.full((8, 8, 3), 0.7, np.float32)
        self.assertEqual(set(astro.classify_tint(rgb, self.base)[0].ravel().tolist()), {128})


class YawPivotTests(unittest.TestCase):
    segments = good_manifest()['chassis'][0]['segments']

    def test_yaw_180_pivots_are_unchanged(self):
        frame = astro.make_frame(0.0, 0.5, 1.0, 180)
        pivots = astro.chassis_pivots(frame, self.segments)
        self.assertEqual(pivots['legl'], (-0.5, 0.0, 0.332))
        self.assertEqual(pivots['legr'], (0.5, 0.0, 0.332))

    def test_other_yaws_transform_both_hubs_and_sort_by_game_x(self):
        frame = astro.make_frame(0.0, 0.5, 1.0, 170)
        pivots = astro.chassis_pivots(frame, self.segments)
        hubs = [astro.to_game((side * 0.25, 0.0, 0.166), frame) for side in (1, -1)]
        left, right = sorted(hubs)
        self.assertEqual(pivots['legl'], left)
        self.assertEqual(pivots['legr'], right)
        self.assertLess(pivots['legl'][0], 0 < pivots['legr'][0])
        self.assertNotAlmostEqual(pivots['legl'][1], pivots['legr'][1])


if __name__ == '__main__':
    unittest.main()
