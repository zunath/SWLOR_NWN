"""Native draw indices and rendered corners must survive authored tangent seams."""
import math
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import NativeTangentSeams as seams
import TrellisWeaponNormals as normals


def model_fixture():
    model = bytearray(1400)
    root, node, mesh = 232, 344, 456
    def u(offset, value): struct.pack_into('<I', model, offset, value)
    u(72, root); u(root + 72, 1350); u(root + 76, 1); u(root + 108, 1); u(1350, node)
    u(node + 108, 33); u(mesh + 8, 1000); u(mesh + 12, 2); u(mesh + 108, 1); u(mesh + 436, 3)
    for offset in (120, 312): model[mesh + offset:mesh + offset + 9] = b'material\0'
    struct.pack_into('<HH', model, mesh + 448, 4, 1)
    for field in (456, 460, 464, 472, 476, 480, 484, 492): struct.pack_into('<i', model, mesh + field, -1)
    faces = np.array([[0, 1, 2], [0, 2, 3]], dtype='<u2')
    for i, face in enumerate(faces): struct.pack_into('<3H', model, 1000 + i * 32 + 26, *face)
    positions = np.array([[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0]], dtype='<f4')
    uv = positions[:, :2].copy()
    normal = np.tile([0, 0, 1], (4, 1)).astype('<f4')
    tangent = np.tile([1, 0, 0], (4, 1)).astype('<f4')
    sign = np.ones((4, 1), dtype='<f4')
    raw = bytearray()
    for field, values in ((444, positions), (452, uv), (468, normal), (488, tangent), (496, sign)):
        u(mesh + field, len(raw)); raw.extend(values.tobytes())
    u(mesh + 404, 1370); u(mesh + 408, 1); u(mesh + 412, 1); u(1370, 6)
    u(mesh + 416, 1374); u(mesh + 420, 1); u(mesh + 424, 1); u(1374, len(raw)); raw.extend(faces.tobytes())
    data = struct.pack('<III', 0, len(model), len(raw)) + model + raw
    source = {'positions': positions[faces].astype(float), 'uv': uv[faces].astype(float),
              'normals': normal[faces].astype(float), 'tangents': tangent[faces].astype(float),
              'signs': np.ones((2, 3))}
    source['tangents'][1] = [0, 1, 0]
    return data, source


class NativeTangentSeamTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.path = Path(self.temporary.name) / 'weapon.mdl'
        self.original, self.source = model_fixture()
        self.path.write_bytes(self.original)

    def preserve(self):
        return seams.preserve_authored_tangent_seams(self.path, {'material': self.source}, normals)

    def test_rendered_corners_and_draw_indices_survive_distinct_tangents(self):
        before = normals.native_meshes(self.path)[0]
        report = self.preserve()
        after = normals.native_meshes(self.path)[0]
        for key in ('positions', 'normals', 'uv'):
            np.testing.assert_array_equal(before[key], after[key])
        np.testing.assert_allclose(after['tangents'], self.source['tangents'], atol=1e-7)
        self.assertEqual(2, report['meshes'][0]['split_vertices'])
        self.assertFalse(report['tangent_seams_averaged'])
        data = self.path.read_bytes()
        raw = 12 + struct.unpack_from('<I', data, 4)[0]
        mesh = 12 + 456
        pointer = 12 + struct.unpack_from('<I', data, mesh + 416)[0]
        offset = struct.unpack_from('<I', data, pointer)[0]
        draw = np.frombuffer(data, dtype='<u2', count=6, offset=raw + offset).reshape(2, 3)
        np.testing.assert_array_equal(draw, after['vertex_indices'])
        self.assertEqual(len(data), raw + struct.unpack_from('<I', data, 8)[0])
        self.assertNotEqual(draw[0, 0], draw[1, 0])

    def test_matching_tangents_share_vertices(self):
        self.source['tangents'][1] = [1, 0, 0]
        self.assertEqual(0, self.preserve()['meshes'][0]['split_vertices'])

    def test_ascii_export_restores_source_corners_without_moving_or_flipping_faces(self):
        source=self.source
        source['normals'][1]=[0,math.sin(.1),math.cos(.1)]
        vertices=source['positions'].reshape(-1,3);uv=source['uv'].reshape(-1,2).copy()
        mesh=(vertices.tolist(),uv.tolist(),np.arange(6).reshape(2,3).tolist(),[[0,0,1]]*6)
        result=seams.restore_source_normals(mesh,source,normals);faces=np.asarray(result[2])
        np.testing.assert_array_equal(np.asarray(result[0])[faces],source['positions'])
        np.testing.assert_array_equal(np.asarray(result[1])[faces],uv.reshape(2,3,2))
        np.testing.assert_allclose(np.asarray(result[3])[faces],source['normals'],atol=1e-12)

    def test_interpolated_frame_is_orthogonalized_before_comparison(self):
        # A new clipping vertex can have a nonorthogonal interpolated tangent.
        # The shader projects it into the retained normal's tangent plane.
        self.source['tangents'][:] = [1, 0, .2]
        report = self.preserve()
        np.testing.assert_allclose(normals.native_meshes(self.path)[0]['tangents'],
                                   np.tile([1, 0, 0], (2, 3, 1)), atol=1e-7)
        self.assertTrue(report['authored_normal_corners_restored'])

    def test_opposite_handedness_is_preserved(self):
        self.source['signs'][1] = -1
        self.preserve()
        np.testing.assert_array_equal(normals.native_meshes(self.path)[0]['signs'], self.source['signs'])

    def test_vertex_limit_fails_without_modifying_input(self):
        with patch.object(seams, 'MAX_VERTICES', 4), self.assertRaisesRegex(ValueError, 'vertex limit'):
            self.preserve()
        self.assertEqual(self.original, self.path.read_bytes())

    def test_compiler_normal_welding_is_repaired_at_the_authored_corners(self):
        self.source['normals'][:] = [0, math.sin(math.radians(1)), math.cos(math.radians(1))]
        before=normals.native_meshes(self.path)[0];report=self.preserve()
        after=normals.native_meshes(self.path)[0]
        np.testing.assert_allclose(after['normals'],self.source['normals'],atol=1e-7)
        for key in ('positions','uv'):np.testing.assert_array_equal(before[key],after[key])
        self.assertAlmostEqual(report['meshes'][0]['max_compiler_normal_error_degrees'],1.,places=6)

    def test_undefined_authored_normals_fail_without_modifying_input(self):
        self.source['normals'][:]=0
        with self.assertRaisesRegex(ValueError,'Degenerate'):
            self.preserve()
        self.assertEqual(self.original, self.path.read_bytes())

    def test_ambiguous_handedness_fails_without_modifying_input(self):
        self.source['signs'][0, 0] = 0
        with self.assertRaisesRegex(ValueError, 'sign changes'):
            self.preserve()
        self.assertEqual(self.original, self.path.read_bytes())

    def test_unsupported_or_inconsistent_buffers_are_rejected(self):
        cases = [(136, 1), (12 + 456 + 456, 0), (12 + 1370, 9)]
        for offset, value in cases:
            with self.subTest(offset=offset):
                data = bytearray(self.original); struct.pack_into('<I', data, offset, value)
                self.path.write_bytes(data)
                with self.assertRaises(ValueError): self.preserve()
                self.assertEqual(data, self.path.read_bytes())

    def test_draw_buffer_must_match_face_records(self):
        data = bytearray(self.original); struct.pack_into('<H', data, len(data) - 12, 3)
        self.path.write_bytes(data)
        with self.assertRaisesRegex(ValueError, 'draw indices differ'):
            self.preserve()
        self.assertEqual(data, self.path.read_bytes())

    def test_compaction_discards_old_streams_but_keeps_every_shaded_corner_exact(self):
        self.preserve()
        before = normals.native_meshes(self.path)
        data = self.path.read_bytes() + b'unreferenced old buffers' * 100
        data = bytearray(data);struct.pack_into('<I',data,8,len(data)-12-struct.unpack_from('<I',data,4)[0])
        self.path.write_bytes(data)
        report = seams.compact_model(self.path)
        self.assertGreater(report['bytes_saved'],2000)
        after = normals.native_meshes(self.path)
        for a,b in zip(before,after):
            for key in ('positions','uv','normals','tangents','signs'):
                self.assertEqual(a[key].tobytes(),b[key].tobytes())
        self.assertEqual(0,seams.compact_model(self.path)['bytes_saved'])

    def test_compaction_merges_identical_vertices_without_averaging_shading_seams(self):
        # Two source positions coincide, but only an identical complete frame may merge.
        data=bytearray(self.original);raw=12+struct.unpack_from('<I',data,4)[0]
        _,headers=seams.mesh_headers(data);header=headers[0]['header']
        for field,columns in seams.STREAMS:
            start=raw+struct.unpack_from('<I',data,header+field)[0]
            data[start+3*columns*4:start+4*columns*4]=data[start:start+columns*4]
        self.path.write_bytes(data);before=normals.native_meshes(self.path)[0]
        report=seams.compact_model(self.path);after=normals.native_meshes(self.path)[0]
        self.assertEqual(1,report['meshes'][0]['identical_vertices_merged'])
        for key in ('positions','uv','normals','tangents','signs'):
            self.assertEqual(before[key].tobytes(),after[key].tobytes())
        # A tangent/sign seam at the coincident position must remain independent.
        struct.pack_into('<f',data,raw+struct.unpack_from('<I',data,header+496)[0]+3*4,-1)
        self.path.write_bytes(data)
        self.assertEqual(0,seams.compact_model(self.path)['meshes'][0]['identical_vertices_merged'])

    def test_compaction_rejects_animated_or_unsupported_models_without_writing(self):
        for offset,value in [(136,1),(12+344+108,5),(12+456+456,0)]:
            data=bytearray(self.original);struct.pack_into('<I',data,offset,value);self.path.write_bytes(data)
            with self.assertRaises(ValueError):seams.compact_model(self.path)
            self.assertEqual(data,self.path.read_bytes())


if __name__ == '__main__':
    unittest.main()
