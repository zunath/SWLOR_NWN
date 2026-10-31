"""Restore authored normal/tangent seams in compiled rigid NWN models without averaging."""
from __future__ import annotations

import hashlib
import struct
from pathlib import Path

import numpy as np

MAX_VERTICES = 65535
STREAMS = ((444, 3), (452, 2), (468, 3), (488, 3), (496, 1))


def producer_sha256():
    return globals().get('SNAPSHOT_SHA256') or hashlib.sha256(Path(__file__).read_bytes()).hexdigest()


def restore_source_normals(mesh, source, normal_helper):
    """Bypass Blender's custom-normal quantization before ASCII compilation."""
    vertices,uvs,faces,_=mesh;indices=np.asarray(faces)
    positions=np.asarray(vertices)[indices];uv=np.asarray(uvs)[indices]
    # Blender's glTF importer already converts V to the native/Blender UV frame.
    native_uv=uv
    normals=[]
    for index,weights in normal_helper.match_faces(source,{'positions':positions,'uv':native_uv}):
        normals.extend(normal_helper.unit(weights@source['normals'][index]).tolist())
    count=len(faces)*3
    return positions.reshape(-1,3).tolist(),uv.reshape(-1,2).tolist(),np.arange(count).reshape(-1,3).tolist(),normals


def mesh_headers(data):
    def u(offset):
        if offset < 0 or offset + 4 > len(data):
            raise ValueError('Native model pointer out of bounds')
        return struct.unpack_from('<I', data, offset)[0]

    if len(data) < 244 or data[:4] != bytes(4) or len(data) != 12 + u(4) + u(8):
        raise ValueError('Expected a complete compiled native model')
    if u(136):
        raise ValueError('Tangent seam splitting supports static rigid models only')
    raw = 12 + u(4)
    pending, seen, result = [u(84)], set(), []
    while pending:
        pointer = pending.pop()
        if pointer in seen:
            raise ValueError('Cyclic native node graph')
        seen.add(pointer)
        node = 12 + pointer
        if node + 112 > raw:
            raise ValueError('Native node escapes model data')
        children, count = u(node + 72), u(node + 76)
        if count > 100000 or 12 + children + count * 4 > raw:
            raise ValueError('Native child array escapes model data')
        pending.extend(u(12 + children + index * 4) for index in range(count))
        flags = u(node + 108)
        if flags == 1:
            continue
        if flags != 33:
            raise ValueError('Tangent seam splitting supports rigid trimeshes and dummies only')
        mesh = node + 112
        if mesh + 508 > raw or not u(mesh + 108) or u(mesh + 436) != 3 or u(mesh + 440):
            raise ValueError('Expected a rendered noninterleaved triangle-list mesh')
        if any(struct.unpack_from('<i', data, mesh + field)[0] != -1
               for field in (456, 460, 464, 472, 476, 480, 484, 492)):
            raise ValueError('Unsupported additional native vertex streams')
        if any(u(mesh + field) for field in (384, 396)):
            raise ValueError('Unsupported native vertex remapping arrays')
        if u(mesh + 408) != 1 or u(mesh + 420) != 1:
            raise ValueError('Expected one native draw-index chunk')
        faces, face_count = u(mesh + 8), u(mesh + 12)
        vertex_count = struct.unpack_from('<H', data, mesh + 448)[0]
        if not vertex_count or not face_count or 12 + faces + face_count * 32 > raw:
            raise ValueError('Native face array escapes model data')
        count_pointer, index_pointer = 12 + u(mesh + 404), 12 + u(mesh + 416)
        if max(count_pointer, index_pointer) + 4 > raw or u(count_pointer) != face_count * 3:
            raise ValueError('Native draw-index count differs from face count')
        draw_offset = u(index_pointer)
        if raw + draw_offset + face_count * 6 > len(data):
            raise ValueError('Native draw indices escape raw data')
        faces_array = np.array([struct.unpack_from('<3H', data, 12 + faces + index * 32 + 26)
                                for index in range(face_count)], dtype=np.uint16)
        draw = np.frombuffer(data, dtype='<u2', count=face_count * 3, offset=raw + draw_offset).reshape(-1, 3)
        if int(faces_array.max()) >= vertex_count or not np.array_equal(draw, faces_array):
            raise ValueError('Native draw indices differ from face indices')
        streams = {}
        for field, columns in STREAMS:
            offset = struct.unpack_from('<i', data, mesh + field)[0]
            if offset < 0 or raw + offset + vertex_count * columns * 4 > len(data):
                raise ValueError('Native vertex stream escapes raw data')
            values = np.frombuffer(data, dtype='<f4', count=vertex_count * columns,
                                   offset=raw + offset).reshape(vertex_count, columns).copy()
            if not np.isfinite(values).all():
                raise ValueError('Nonfinite native vertex data')
            streams[field] = values
        result.append({'header': mesh, 'face_offset': 12 + faces, 'faces': faces_array,
                       'count': vertex_count, 'draw_pointer': index_pointer, 'streams': streams})
    return raw, result


def preserve_authored_tangent_seams(path, sources, normal_helper):
    path = Path(path)
    before = path.read_bytes()
    raw, headers = mesh_headers(before)
    meshes = normal_helper.native_meshes(path)
    if len(headers) != len(meshes):
        raise ValueError('Native rendered-mesh traversal changed')
    after = bytearray(before)
    allowed = set(range(8, 12))
    reports = []

    def append(values):
        after.extend(bytes((-len(after)) % 4))
        offset = len(after) - raw
        after.extend(np.asarray(values, dtype='<f4').tobytes())
        return offset

    for info, mesh in zip(headers, meshes):
        if not np.array_equal(info['faces'], mesh['vertex_indices']):
            raise ValueError('Native mesh traversal disagrees about face indices')
        source = sources[mesh['material']]
        assignments, first, source_indices = {}, {}, list(range(info['count']))
        tangents = info['streams'][488].copy().tolist()
        authored_normals = info['streams'][468].copy().tolist()
        signs = info['streams'][496].copy().tolist()
        faces = info['faces'].copy()
        max_normal_angle = 0.
        expected_normals=[];expected_tangents=[];expected_signs=[]
        for face, (index, weights) in enumerate(normal_helper.match_faces(source, mesh)):
            normal = normal_helper.unit(mesh['normals'][face])
            source_normal = normal_helper.unit(weights @ source['normals'][index])
            tangent = normal_helper.unit(weights @ source['tangents'][index])
            sign = weights @ source['signs'][index]
            if np.any(abs(sign) < .99999):
                raise ValueError('Authored tangent sign changes within a native face')
            # Clipping interpolates the authored N and T independently. Compare
            # orthonormal frames as the normal-map shader does. The compiler
            # may weld close normals; restore source corners independently.
            source_tangent = normal_helper.unit(tangent - source_normal * np.sum(
                tangent * source_normal, axis=-1, keepdims=True))
            max_normal_angle = max(max_normal_angle, float(np.degrees(np.arccos(np.clip(
                np.sum(normal * source_normal, axis=-1), -1, 1))).max()))
            expected_normals.append(source_normal);expected_tangents.append(source_tangent);expected_signs.append(np.sign(sign))
            for corner, old_vertex in enumerate(info['faces'][face]):
                old_vertex = int(old_vertex)
                candidate = np.asarray([*source_normal[corner], *source_tangent[corner], np.sign(sign[corner])], dtype='<f4')
                key = (old_vertex, candidate.tobytes())
                if key not in assignments:
                    if old_vertex not in first:
                        vertex = old_vertex
                        first[old_vertex] = key
                    else:
                        vertex = len(source_indices)
                        if vertex >= MAX_VERTICES:
                            raise ValueError('Tangent seams exceed the native vertex limit')
                        source_indices.append(old_vertex)
                        tangents.append(tangents[old_vertex].copy())
                        authored_normals.append(authored_normals[old_vertex].copy())
                        signs.append(signs[old_vertex].copy())
                    assignments[key] = vertex
                    authored_normals[vertex] = candidate[:3].tolist()
                    tangents[vertex] = candidate[3:6].tolist()
                    signs[vertex] = candidate[6:].tolist()
                faces[face, corner] = assignments[key]
        streams = {field: values[source_indices] for field, values in info['streams'].items()}
        streams[468] = np.asarray(authored_normals)
        streams[488], streams[496] = np.asarray(tangents), np.asarray(signs)
        for field,expected in ((468,expected_normals),(488,expected_tangents)):
            if not np.array_equal(streams[field].astype('<f4')[faces],np.asarray(expected,dtype='<f4')):
                raise ValueError('Authored corner basis changed during seam splitting')
        if not np.array_equal(streams[496].astype('<f4')[faces][...,0],np.asarray(expected_signs,dtype='<f4')):
            raise ValueError('Authored corner signs changed during seam splitting')
        header = info['header']
        for field, _ in STREAMS:
            struct.pack_into('<I', after, header + field, append(streams[field]))
            allowed.update(range(header + field, header + field + 4))
        after.extend(bytes((-len(after)) % 4))
        draw_offset = len(after) - raw
        after.extend(faces.astype('<u2').tobytes())
        struct.pack_into('<I', after, info['draw_pointer'], draw_offset)
        allowed.update(range(info['draw_pointer'], info['draw_pointer'] + 4))
        struct.pack_into('<H', after, header + 448, len(source_indices))
        allowed.update(range(header + 448, header + 450))
        for index, vertices in enumerate(faces):
            offset = info['face_offset'] + index * 32 + 26
            struct.pack_into('<3H', after, offset, *vertices)
            allowed.update(range(offset, offset + 6))
        for field in (444, 452):
            if not np.array_equal(info['streams'][field][info['faces']], streams[field][faces]):
                raise ValueError('Shading seam splitting changed rendered position/UV corners')
        reports.append({'material': mesh['material'], 'vertices_before': info['count'],
                        'vertices_after': len(source_indices), 'split_vertices': len(source_indices) - info['count'],
                        'triangles': len(faces), 'max_compiler_normal_error_degrees': max_normal_angle})
    struct.pack_into('<I', after, 8, len(after) - raw)
    changed = {i for i, (old, new) in enumerate(zip(before, after)) if old != new}
    if not changed <= allowed:
        raise ValueError('Tangent seam splitting changed unrelated native metadata')
    mesh_headers(after)
    path.write_bytes(after)
    return {'model': path.name, 'policy': 'authored_tangent_seams', 'producer_sha256': producer_sha256(),
            'before_sha256': hashlib.sha256(before).hexdigest(), 'after_sha256': hashlib.sha256(after).hexdigest(),
            'rendered_position_uv_corners_preserved': True, 'authored_normal_corners_restored': True,
            'native_draw_indices_updated': True,
            'tangent_seams_averaged': False, 'native_close_view_verification_required': True, 'meshes': reports}
