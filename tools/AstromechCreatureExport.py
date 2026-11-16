"""Turn Meshy astromech chassis and tool GLBs into rigid, segmented NWN creature models that share one authored animation set.

Blender 4.0: blender -b --factory-startup --python-exit-code 1 --python tools/AstromechCreatureExport.py --
  --mode export --manifest PATH --output EMPTY
Python:      python -B tools/AstromechCreatureExport.py --mode package --export PATH --crunch nwn_crunch.exe --output EMPTY

Every chassis is cut by region into rigid meshes (head, body, left leg, right leg) that hang on their own nodes under
one shared supermodel. The supermodel holds the node tree as dummies and every animation clip; the chassis files hold
geometry only. Clips work for chassis of any proportions because only `rootdummy`, `toolr` and `tooll` carry position
keys; every other animated node carries orientation keys. Two shared tool parts slide in and out of the body: the
shock prod on `toolr` and the blaster on `tooll`. A tool rests at its socket fully extended and is retracted by
sliding it back by its length plus 0.02 m, so the socket sits one tool length in front of the body face.

Node tree (supermodel and chassis): <model> > rootdummy > torso > {body, head > headconjure, impact, handconjure, rhand,
lhand, toolr_socket > toolr, tooll_socket > tooll}; rootdummy > {legl, legr}.

Manifest (schema_version 1, paths relative to the manifest, every source needs its sha256):
  supermodel: model name of the shared animation model (c_astromech)
  tools: [{name (prod or blaster), source (GLB), sha256, texture (<= 13 chars), texture_size (512 or 1024),
           tip_axis (+x or -x: the end of the source's long X axis that is the working tip), length (metres in game)}]
  chassis: [{model (<= 16 chars), source (GLB), sha256, material (<= 16 chars; the dye material and PLT name),
             texture_size (512 or 1024), height (metres), yaw_degrees (turn applied to the source, default 180: Meshy
             faces -Y), segments {head_above_z, leg_beyond_x, hub [abs x, y, z], dome_axis [x, y]} in source units,
             sockets {toolr, tooll}: [x, y, z] in game metres relative to the torso node,
             tint {accent_hue [lo, hi], accent_min_saturation, dark_max_value}}]
Segmentation is by triangle centroid: above head_above_z and inside leg_beyond_x is the head, outside leg_beyond_x is a
leg (game -X is the left leg), everything else is the body. The torso node and the leg pivots sit on the hub; the head
pivots on the dome axis at head_above_z.

Tint: the chassis base colour is classified per pixel into three dye layers (accent = cloth1 layer 4, dark parts = metal2
layer 3, everything else = cloth2 layer 5) and written as <material>.plt with the luminance as shade. The normal map is
<material>_n. NO specular or roughness map is written, because the mask holds a metal layer and the palette must drive
metal. <material>.mtr is a STARTER material for tools/GenerateTintMapAssets.py in the HAK repository: place the PLT,
the starter MTR and the _n texture in the creature HAK folder and run the generator, which replaces the starter with the
final fs_plt_tinter_nm material in sw_tint_mtr and packs the PLT into a tint mask DDS.

Output per chassis: <model>/resources/ (ASCII MDL, PLT, starter MTR, _n TGA), review_tint.png, review_segments.png and
report.json. The shared supermodel and the tool textures and MTRs go to <supermodel>/resources/. Package output is
compiled MDL, PLT, MTR and DDS only. Verify in the client; nothing here deploys or edits module data.
"""
from __future__ import annotations

import argparse, hashlib, json, math, re, shutil, struct, subprocess, sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import MeshyCreatureExport as meshy

REPO = Path(__file__).resolve().parents[1]
MAX_MESH_ELEMENTS = 65535
TOOL_NAMES = ('prod', 'blaster')
TOOL_NODES = {'prod': 'toolr', 'blaster': 'tooll'}
RETRACT_GAP = 0.02
MODEL_LIMIT, TEXTURE_LIMIT = 16, 13
LAYER_DARK, LAYER_ACCENT, LAYER_BODY = 3, 4, 5
PLT_HEADER = b'PLT V1  ' + (10).to_bytes(4, 'little') + bytes(4)
LAYER_REVIEW_COLORS = {LAYER_DARK: (0.25, 0.30, 0.75), LAYER_ACCENT: (0.95, 0.55, 0.10), LAYER_BODY: (0.85, 0.85, 0.85)}
SEGMENT_REVIEW_COLORS = {'head': (0.9, 0.2, 0.2), 'body': (0.2, 0.4, 0.9), 'legl': (0.2, 0.8, 0.3), 'legr': (0.95, 0.85, 0.2),
                         'toolr': (0.9, 0.2, 0.9), 'tooll': (0.2, 0.9, 0.9)}


# ---------------------------------------------------------------------------------------------------------------------
# Manifest validation (plain Python)
# ---------------------------------------------------------------------------------------------------------------------

def is_number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def require_keys(data, label, required, optional=()):
    if not isinstance(data, dict):
        raise ValueError(f'{label} must be an object')
    missing = [k for k in required if k not in data]
    unknown = [k for k in data if k not in required and k not in optional]
    if missing or unknown:
        raise ValueError(f'{label}: missing {missing}, unknown {unknown}')


def require_name(value, label, limit):
    if not isinstance(value, str) or not re.fullmatch(r'[a-z0-9_]+', value) or len(value) > limit:
        raise ValueError(f'{label} must be lowercase letters, digits or underscores, at most {limit} characters')


def require_vector(value, label, size):
    if not isinstance(value, list) or len(value) != size or not all(is_number(v) for v in value):
        raise ValueError(f'{label} must be a list of {size} finite numbers')


def require_source(item, label, manifest_dir):
    source = item['source']
    if not isinstance(source, str) or not source or Path(source).is_absolute():
        raise ValueError(f'{label}.source must be a relative path')
    if not isinstance(item['sha256'], str) or not re.fullmatch(r'[0-9a-f]{64}', item['sha256']):
        raise ValueError(f'{label}.sha256 must be 64 lowercase hex characters')
    if manifest_dir is not None:
        path = manifest_dir / source
        if not path.is_file():
            raise ValueError(f'{label}.source does not exist: {source}')
        if hashlib.sha256(path.read_bytes()).hexdigest() != item['sha256']:
            raise ValueError(f'{label}: source GLB does not match its sha256')


def validate_tool(tool, label, manifest_dir):
    require_keys(tool, label, ('name', 'source', 'sha256', 'texture', 'texture_size', 'tip_axis', 'length'))
    if tool['name'] not in TOOL_NAMES:
        raise ValueError(f'{label}.name must be one of {TOOL_NAMES}')
    require_name(tool['texture'], f'{label}.texture', TEXTURE_LIMIT)
    if tool['texture_size'] not in (512, 1024):
        raise ValueError(f'{label}.texture_size must be 512 or 1024')
    if tool['tip_axis'] not in ('+x', '-x'):
        raise ValueError(f'{label}.tip_axis must be +x or -x')
    if not is_number(tool['length']) or not 0.05 <= tool['length'] <= 2:
        raise ValueError(f'{label}.length must be between 0.05 and 2 metres')
    require_source(tool, label, manifest_dir)


def validate_chassis(item, label, manifest_dir):
    require_keys(item, label, ('model', 'source', 'sha256', 'material', 'texture_size', 'height', 'segments', 'sockets', 'tint'),
                 ('yaw_degrees',))
    require_name(item['model'], f'{label}.model', MODEL_LIMIT)
    require_name(item['material'], f'{label}.material', MODEL_LIMIT - 2)
    if item['texture_size'] not in (512, 1024):
        raise ValueError(f'{label}.texture_size must be 512 or 1024')
    if not is_number(item['height']) or not 0.2 <= item['height'] <= 4:
        raise ValueError(f'{label}.height must be between 0.2 and 4 metres')
    if 'yaw_degrees' in item and not is_number(item['yaw_degrees']):
        raise ValueError(f'{label}.yaw_degrees must be a number')
    segments = item['segments']
    require_keys(segments, f'{label}.segments', ('head_above_z', 'leg_beyond_x', 'hub', 'dome_axis'))
    for key in ('head_above_z', 'leg_beyond_x'):
        if not is_number(segments[key]):
            raise ValueError(f'{label}.segments.{key} must be a number')
    if segments['leg_beyond_x'] <= 0:
        raise ValueError(f'{label}.segments.leg_beyond_x must be positive')
    require_vector(segments['hub'], f'{label}.segments.hub', 3)
    require_vector(segments['dome_axis'], f'{label}.segments.dome_axis', 2)
    if segments['hub'][0] <= 0:
        raise ValueError(f'{label}.segments.hub[0] is the absolute leg x and must be positive')
    require_keys(item['sockets'], f'{label}.sockets', ('toolr', 'tooll'))
    for name in ('toolr', 'tooll'):
        require_vector(item['sockets'][name], f'{label}.sockets.{name}', 3)
    tint = item['tint']
    require_keys(tint, f'{label}.tint', ('accent_hue', 'accent_min_saturation', 'dark_max_value'))
    require_vector(tint['accent_hue'], f'{label}.tint.accent_hue', 2)
    if not 0 <= tint['accent_hue'][0] < tint['accent_hue'][1] <= 1:
        raise ValueError(f'{label}.tint.accent_hue must be an increasing range inside 0..1')
    for key in ('accent_min_saturation', 'dark_max_value'):
        if not is_number(tint[key]) or not 0 < tint[key] <= 1:
            raise ValueError(f'{label}.tint.{key} must be in (0, 1]')
    require_source(item, label, manifest_dir)


def validate_manifest(manifest, manifest_dir=None):
    """Reject anything the exporter would have to guess; with a manifest folder, also check the sources and their hashes."""
    require_keys(manifest, 'manifest', ('schema_version', 'supermodel', 'tools', 'chassis'))
    if manifest['schema_version'] != 1:
        raise ValueError('Unsupported manifest schema')
    require_name(manifest['supermodel'], 'supermodel', MODEL_LIMIT)
    tools, chassis = manifest['tools'], manifest['chassis']
    if not isinstance(tools, list) or sorted(str(t.get('name')) if isinstance(t, dict) else '' for t in tools) != sorted(TOOL_NAMES):
        raise ValueError(f'tools must hold exactly one entry for each of {TOOL_NAMES}')
    if not isinstance(chassis, list) or not chassis:
        raise ValueError('chassis must be a non-empty list')
    for index, tool in enumerate(tools):
        validate_tool(tool, f'tools[{index}]', manifest_dir)
    for index, item in enumerate(chassis):
        validate_chassis(item, f'chassis[{index}]', manifest_dir)
    names = [manifest['supermodel']] + [c['model'] for c in chassis]
    resrefs = [c['material'] for c in chassis] + [t['texture'] for t in tools]
    for label, values in (('model', names), ('material or texture', resrefs)):
        if len(set(values)) != len(values):
            raise ValueError(f'every {label} name must be unique')
    for tool in tools:
        for suffix in ('', '_n', '_s', '_r'):
            if len(tool['texture'] + suffix) > 16:
                raise ValueError(f"{tool['texture']}{suffix} exceeds the 16 character resref limit")
    return manifest


# ---------------------------------------------------------------------------------------------------------------------
# Geometry: frame, segmentation, welding (plain Python so it is testable without Blender)
# ---------------------------------------------------------------------------------------------------------------------

def yaw_matrix(degrees):
    """(cos, sin) of a turn about Z, snapped so quarter turns are exact."""
    radians = math.radians(degrees)
    return tuple(0.0 if abs(v) < 1e-12 else round(v) if abs(v - round(v)) < 1e-12 else v for v in (math.cos(radians), math.sin(radians)))


def make_frame(zmin, zmax, height, yaw_degrees):
    if zmax - zmin <= 0:
        raise ValueError('Source has no height')
    cos, sin = yaw_matrix(yaw_degrees)
    return {'cos': cos, 'sin': sin, 'scale': height / (zmax - zmin), 'zmin': zmin}


def to_game(point, frame):
    """Source (front toward -Y after the yaw turn, any origin) to game (front +Y, feet at z 0)."""
    x, y, z = point
    c, s, k = frame['cos'], frame['sin'], frame['scale']
    return ((c * x - s * y) * k, (s * x + c * y) * k, (z - frame['zmin']) * k)


def to_game_normal(normal, frame):
    x, y, z = normal
    c, s = frame['cos'], frame['sin']
    n = (c * x - s * y, s * x + c * y, z)
    length = math.sqrt(sum(v * v for v in n))
    return tuple(v / length for v in n)


def chassis_pivots(frame, segments):
    """Game positions (relative to the root dummy) of the torso, head and leg pivots."""
    hub_x, hub_y, hub_z = segments['hub']
    z = (hub_z - frame['zmin']) * frame['scale']
    side_y = to_game((0.0, hub_y, hub_z), frame)[1]
    dome = to_game((segments['dome_axis'][0], segments['dome_axis'][1], segments['head_above_z']), frame)
    return {'body': (0.0, 0.0, z), 'head': dome, 'legl': (-hub_x * frame['scale'], side_y, z),
            'legr': (hub_x * frame['scale'], side_y, z)}


def segment_name(centroid, frame, segments):
    """Rigid segment that owns a triangle, from its centroid in source coordinates."""
    if abs(centroid[0]) > segments['leg_beyond_x']:
        return 'legl' if to_game(centroid, frame)[0] < 0 else 'legr'
    if centroid[2] > segments['head_above_z']:
        return 'head'
    return 'body'


def new_mesh():
    return {'verts': [], 'normals': [], 'uvs': [], 'faces': [], 'index': {}}


def shell_centroids(triangles):
    """Area-weighted centroid of the connected shell each triangle belongs to. Generated models are made of many
    separate shells; assigning whole shells keeps a part from being torn along a segment boundary."""
    parent = list(range(len(triangles)))

    def find(index):
        while parent[index] != index:
            parent[index] = parent[parent[index]]
            index = parent[index]
        return index

    owner = {}
    for index, corners in enumerate(triangles):
        for position, _, _ in corners:
            key = tuple(round(v, 5) for v in position)
            if key in owner:
                parent[find(index)] = find(owner[key])
            else:
                owner[key] = index
    totals = {}
    for index, corners in enumerate(triangles):
        a, b, c = (corner[0] for corner in corners)
        u, v = [b[i] - a[i] for i in range(3)], [c[i] - a[i] for i in range(3)]
        cross = (u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0])
        area = 0.5 * math.sqrt(sum(x * x for x in cross)) + 1e-12
        total = totals.setdefault(find(index), [0.0, 0.0, 0.0, 0.0])
        for i in range(3):
            total[i] += area * (a[i] + b[i] + c[i]) / 3
        total[3] += area
    return [tuple(totals[find(index)][i] / totals[find(index)][3] for i in range(3)) for index in range(len(triangles))]


def weld_triangles(triangles, assign, to_pos, to_normal, origins, centroids=None):
    """Group triangles (three (position, normal, uv) corners each) into meshes by `assign(centroid)`; vertices are
    placed by `to_pos`, made relative to the group's origin and welded by position, UV and normal. `centroids`
    replaces each triangle's own centroid, for example with its shell's."""
    meshes = {name: new_mesh() for name in origins}
    for index, corners in enumerate(triangles):
        centroid = centroids[index] if centroids else tuple(sum(c[0][i] for c in corners) / 3 for i in range(3))
        name = assign(centroid)
        mesh, origin = meshes[name], origins[name]
        face = []
        for position, normal, uv in corners:
            p, n = to_pos(position), to_normal(normal)
            key = (tuple(round(v, 6) for v in p), round(uv[0], 5), round(uv[1], 5), tuple(round(v, 3) for v in n))
            if key not in mesh['index']:
                mesh['index'][key] = len(mesh['verts'])
                mesh['verts'].append(tuple(a - b for a, b in zip(p, origin)))
                mesh['normals'].append(n)
                mesh['uvs'].append((uv[0], uv[1]))
            face.append(mesh['index'][key])
        mesh['faces'].append(tuple(face))
    return meshes


def check_meshes(meshes):
    for name, mesh in meshes.items():
        if not mesh['faces']:
            raise ValueError(f'Segment {name} is empty; check the segmentation thresholds')
        if max(len(mesh['verts']), len(mesh['faces'])) > MAX_MESH_ELEMENTS:
            raise ValueError(f'Segment {name} exceeds the NWN vertex or face limit')


def chassis_meshes(triangles, frame, segments):
    pivots = chassis_pivots(frame, segments)
    meshes = weld_triangles(triangles, lambda c: segment_name(c, frame, segments), lambda p: to_game(p, frame),
                            lambda n: to_game_normal(n, frame), pivots, shell_centroids(triangles))
    check_meshes(meshes)
    return meshes, pivots


def tool_frame(points, tip_axis, length):
    """Position and normal transforms that stand a tool's long X axis along game Y: tip at the origin pointing +Y, body
    extending toward -Y, centred on the other axes."""
    xs = [p[0] for p in points]
    lo, hi = min(xs), max(xs)
    if hi - lo <= 1e-9:
        raise ValueError('Tool source has no length along X')
    scale = length / (hi - lo)
    sign = 1.0 if tip_axis == '+x' else -1.0
    tip = hi if sign > 0 else lo
    cross = [(sign * -p[1]) * scale for p in points]
    heights = [p[2] * scale for p in points]
    center_x, center_z = (min(cross) + max(cross)) / 2, (min(heights) + max(heights)) / 2

    def to_pos(p):
        return (sign * -p[1] * scale - center_x, sign * (p[0] - tip) * scale, p[2] * scale - center_z)

    def to_normal(n):
        return (sign * -n[1], sign * n[0], n[2])

    return to_pos, to_normal


def tool_mesh(triangles, tip_axis, length):
    points = [c[0] for corners in triangles for c in corners]
    to_pos, to_normal = tool_frame(points, tip_axis, length)
    meshes = weld_triangles(triangles, lambda c: 'tool', to_pos, to_normal, {'tool': (0.0, 0.0, 0.0)})
    check_meshes(meshes)
    return meshes['tool']


def mesh_bounds(verts, offset=(0.0, 0.0, 0.0)):
    lo = tuple(min(v[i] for v in verts) + offset[i] for i in range(3))
    hi = tuple(max(v[i] for v in verts) + offset[i] for i in range(3))
    return lo, hi


def retract_offset(length):
    return -(length + RETRACT_GAP)


def retracted_tool_bounds(tool_verts, socket, length):
    return mesh_bounds(tool_verts, (socket[0], socket[1] + retract_offset(length), socket[2]))


def bounds_inside(inner, outer, tolerance=1e-9):
    return all(outer[0][i] - tolerance <= inner[0][i] and inner[1][i] <= outer[1][i] + tolerance for i in range(3))


def bounds_probe_points(bounds):
    """The eight corners and the centre of a box."""
    lo, hi = bounds
    corners = [(x, y, z) for x in (lo[0], hi[0]) for y in (lo[1], hi[1]) for z in (lo[2], hi[2])]
    return corners + [tuple((lo[i] + hi[i]) / 2 for i in range(3))]


RAY_DIRECTIONS = ((1.0, 0.0137, 0.0071), (-1.0, 0.0093, 0.0113), (0.0071, 0.0137, 1.0), (0.0113, -1.0, 0.0093), (0.0057, 1.0, -0.0131))


def ray_crossings(points, verts, faces, direction):
    """Triangle crossings along a ray from each point (Moller-Trumbore, forward hits only)."""
    import numpy as np
    v = np.asarray(verts, np.float64)
    tri = np.asarray(faces, np.int64)
    a, e1, e2 = v[tri[:, 0]], v[tri[:, 1]] - v[tri[:, 0]], v[tri[:, 2]] - v[tri[:, 0]]
    d = np.asarray(direction, np.float64)
    h = np.cross(d, e2)
    det = np.einsum('ij,ij->i', e1, h)
    valid = np.abs(det) > 1e-14
    inverse = np.where(valid, 1.0 / np.where(valid, det, 1.0), 0.0)
    counts = []
    for point in points:
        s = np.asarray(point, np.float64) - a
        u = inverse * np.einsum('ij,ij->i', s, h)
        q = np.cross(s, e1)
        w = inverse * (q @ d)
        t = inverse * np.einsum('ij,ij->i', e2, q)
        counts.append(int((valid & (u >= 0) & (w >= 0) & (u + w <= 1) & (t > 1e-9)).sum()))
    return counts


def points_inside_mesh(points, verts, faces):
    """Whether each point lies inside a mesh: an odd number of crossings on most of five skewed rays. Reliable for
    closed meshes; overlapping shells (door panels) can fool a single ray, so a failure is a prompt to look."""
    votes = [ray_crossings(points, verts, faces, d) for d in RAY_DIRECTIONS]
    return [sum(counts[i] % 2 == 1 for counts in votes) * 2 > len(RAY_DIRECTIONS) for i in range(len(points))]


# ---------------------------------------------------------------------------------------------------------------------
# Tint and PLT (NumPy)
# ---------------------------------------------------------------------------------------------------------------------

def classify_tint(rgb, tint):
    """Per-pixel dye layer and shade bytes from a base colour (rows h, columns w, three floats 0..1)."""
    import numpy as np
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    sat = np.where(mx > 1e-5, (mx - mn) / np.maximum(mx, 1e-5), 0)
    d = np.maximum(mx - mn, 1e-5)
    hue = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) / 6
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    low, high = tint['accent_hue']
    accent = (sat > tint['accent_min_saturation']) & (mx > tint['dark_max_value']) & (hue > low) & (hue < high)
    dark = (mx < tint['dark_max_value']) & ~accent
    layer = np.where(accent, LAYER_ACCENT, np.where(dark, LAYER_DARK, LAYER_BODY)).astype(np.uint8)
    shade = np.clip(np.where(accent, lum * 1.5, np.where(dark, lum * 2.2, lum * 1.05)), 0, 1)
    return np.rint(shade * 255).astype(np.uint8), layer


def layer_coverage(layer):
    return {'accent_cloth1': float((layer == LAYER_ACCENT).mean()), 'dark_metal2': float((layer == LAYER_DARK).mean()),
            'body_cloth2': float((layer == LAYER_BODY).mean())}


def plt_bytes(shade, layer):
    """PLT V1 file: header, width, height, then one (shade, layer) byte pair per texel, rows in texture storage order
    (row 0 is sampled at v = 0, the bottom row of a Blender image)."""
    import numpy as np
    if shade.shape != layer.shape or shade.ndim != 2:
        raise ValueError('Shade and layer must be equal two-dimensional arrays')
    if int(layer.max()) > 9:
        raise ValueError('PLT layer ids run 0 to 9')
    height, width = shade.shape
    data = np.stack([shade.astype(np.uint8), layer.astype(np.uint8)], axis=-1)
    return PLT_HEADER + struct.pack('<II', width, height) + data.tobytes()


def starter_mtr(material):
    """Authored source material that makes GenerateTintMapAssets emit fs_plt_tinter_nm with this normal map and no
    specular or roughness map."""
    return f'renderhint NormalAndSpecMapped\ntexture1 {material}_n\ntexture2 null\ntexture3 null\n'


def tool_mtr(texture):
    return f'renderhint NormalAndSpecMapped\ntexture0 {texture}\ntexture1 {texture}_n\ntexture2 {texture}_s\ntexture3 {texture}_r\n'


# ---------------------------------------------------------------------------------------------------------------------
# ASCII model output (plain Python)
# ---------------------------------------------------------------------------------------------------------------------

def f(values):
    return ' '.join('%.6g' % v for v in values)


def dummy(name, parent, position=None):
    lines = [f'node dummy {name}', f'  parent {parent}']
    if position is not None:
        lines += [f'  position {f(position)}', '  orientation 0 0 1 0']
    return lines + ['endnode']


def trimesh(name, parent, position, mesh, material):
    lines = [f'node trimesh {name}', f'  parent {parent}', f'  position {f(position)}', '  orientation 0 0 1 0',
             '  ambient 1 1 1', '  diffuse 1 1 1', '  specular 0 0 0', '  shininess 1', '  shadow 1', '  render 1',
             '  tilefade 0', f'  bitmap {material}', f'  materialname {material}', '  renderhint NormalAndSpecMapped',
             f"  verts {len(mesh['verts'])}"]
    lines += ['    ' + f(v) for v in mesh['verts']]
    lines += [f"  normals {len(mesh['normals'])}"] + ['    ' + f(n) for n in mesh['normals']]
    lines += [f"  tverts {len(mesh['uvs'])}"] + ['    %.6g %.6g 0' % uv for uv in mesh['uvs']]
    lines += [f"  faces {len(mesh['faces'])}"] + [f'    {a} {b} {c} 1 {a} {b} {c} 0' for a, b, c in mesh['faces']]
    return lines + ['endnode']


def model_header(model, supermodel):
    return ['#MAXMODEL ASCII', f'newmodel {model}', f'setsupermodel {model} {supermodel}', 'classification CHARACTER',
            'setanimationscale 1', f'beginmodelgeom {model}']


def supermodel_geometry(model):
    """The shared node tree as dummies at neutral positions; the chassis files own the real positions."""
    lines = dummy(model, 'NULL') + dummy('rootdummy', model, (0, 0, 0)) + dummy('torso', 'rootdummy', (0, 0, 0.2))
    lines += dummy('body', 'torso', (0, 0, 0)) + dummy('head', 'torso', (0, 0, 0.3)) + dummy('headconjure', 'head', (0, 0, 0.35))
    lines += dummy('impact', 'torso', (0, 0.05, -0.15)) + dummy('handconjure', 'torso', (0, 0.35, -0.1))
    lines += dummy('rhand', 'torso', (0.13, 0.27, -0.2)) + dummy('lhand', 'torso', (-0.13, 0.27, -0.2))
    for tool, x in (('toolr', 0.1), ('tooll', -0.1)):
        lines += dummy(tool + '_socket', 'torso', (x, 0.4, -0.1)) + dummy(tool, tool + '_socket', (0, 0, 0))
    lines += dummy('legl', 'rootdummy', (-0.25, 0, 0.2)) + dummy('legr', 'rootdummy', (0.25, 0, 0.2))
    return lines


def supermodel_text(model, tool_lengths):
    lines = model_header(model, 'NULL') + supermodel_geometry(model) + [f'endmodelgeom {model}']
    lines += clips_text(model, tool_lengths).splitlines()
    return '\n'.join(lines + [f'donemodel {model}']) + '\n'


def chassis_text(item, supermodel, pivots, meshes, tool_meshes):
    """Chassis geometry only: no animations, the supermodel supplies them."""
    model, material = item['model'], item['material']
    torso = pivots['body']
    head_local = tuple(a - b for a, b in zip(pivots['head'], torso))
    lines = model_header(model, supermodel) + dummy(model, 'NULL') + dummy('rootdummy', model, (0, 0, 0))
    lines += dummy('torso', 'rootdummy', torso) + trimesh('body', 'torso', (0, 0, 0), meshes['body'], material)
    lines += trimesh('head', 'torso', head_local, meshes['head'], material) + dummy('headconjure', 'head', (0, 0, 0.35))
    lines += dummy('impact', 'torso', (0, 0.05, -0.15)) + dummy('handconjure', 'torso', (0, 0.35, -0.1))
    lines += dummy('rhand', 'torso', (0.13, 0.27, -0.2)) + dummy('lhand', 'torso', (-0.13, 0.27, -0.2))
    for tool, name in (('prod', 'toolr'), ('blaster', 'tooll')):
        lines += dummy(name + '_socket', 'torso', item['sockets'][name])
        lines += trimesh(name, name + '_socket', (0, 0, 0), tool_meshes[tool]['mesh'], tool_meshes[tool]['texture'])
    lines += trimesh('legl', 'rootdummy', pivots['legl'], meshes['legl'], material)
    lines += trimesh('legr', 'rootdummy', pivots['legr'], meshes['legr'], material)
    return '\n'.join(lines + [f'endmodelgeom {model}', f'donemodel {model}']) + '\n'


# ---------------------------------------------------------------------------------------------------------------------
# CLIP DATA. Edit clips here; nothing below this section's formatter touches geometry.
# Keys: (time, value). rot: (time, axis, degrees); pos: (time, (x, y, z)) absolute local positions.
# + pitch leans the top backward (rotation about +X). yaw: about +Z. roll: about +Y.
# Only rootdummy, toolr and tooll may carry position keys. Tools rest extended at 0 0 0 and retract by sliding back.
# ---------------------------------------------------------------------------------------------------------------------

X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
LIE = 82          # degrees on its back
LIE_Z = 0.27      # lift so the back rests on the ground
THRUST = 0.12     # prod slide beyond its rest position at the hit
ANIM_NODES = [('rootdummy', None), ('torso', 'rootdummy'), ('head', 'torso'), ('legl', 'rootdummy'), ('legr', 'rootdummy'),
              ('toolr', 'toolr_socket'), ('tooll', 'tooll_socket')]
POSITION_NODES = ('rootdummy', 'toolr', 'tooll')
SOCKET_PARENTS = {'toolr_socket': 'torso', 'tooll_socket': 'torso'}
LOOPING_CLIPS = ('cpause1', 'cpause2', 'cwalk', 'cwalkinj', 'crun', 'creadyr', 'creadyl', 'ccastoutlp')
MELEE_CLIPS = ('ca1slashl', 'ca1slashr', 'ca1stab', 'cclosel', 'ccloseh', 'creach')


def slide(y):
    return (0, y, 0)


def clip(name, length, tracks, events=(), transtime=0.25):
    return {'name': name, 'length': length, 'tracks': tracks, 'events': list(events), 'transtime': transtime}


def attack_clip(name, yaw, retracted, reach=0.22, wind=14):
    return clip(name, 1.0, {
        'rootdummy': {'pos': [(0, (0, 0, 0)), (0.3, (0, -0.06, 0)), (0.5, (0, reach, 0)), (0.75, (0, 0.05, 0)), (1.0, (0, 0, 0))]},
        'torso': {'rot': [(0, X, 8), (0.3, X, wind), (0.5, X, -3), (0.75, X, 6), (1.0, X, 8)]},
        'head': {'rot': [(0, Z, 0), (0.3, Z, yaw), (0.5, Z, -yaw * 0.5), (1.0, Z, 0)]},
        'toolr': {'pos': [(0, slide(retracted)), (0.3, slide(0)), (0.5, slide(THRUST)), (0.75, slide(-0.1)), (1.0, slide(retracted))]}},
        events=[(0.5, 'hit')])


def clip_specs(tool_lengths):
    """Every clip as data. `tool_lengths` maps prod and blaster to their length in metres."""
    prod, blaster = retract_offset(tool_lengths['prod']), retract_offset(tool_lengths['blaster'])
    half = -tool_lengths['prod'] / 2
    lie = {'rootdummy': {'pos': [(0, (0, 0, LIE_Z)), (1.0, (0, 0, LIE_Z))], 'rot': [(0, X, LIE), (1.0, X, LIE)]}}
    clips = [
        clip('cpause1', 4.0, {'head': {'rot': [(0, Z, 0), (1.0, Z, 28), (1.6, Z, 28), (2.8, Z, -22), (3.3, Z, -22), (4.0, Z, 0)]},
                              'torso': {'rot': [(0, X, 0), (2.0, X, 2), (4.0, X, 0)]}}),
        clip('cpause2', 3.0, {'head': {'rot': [(0, Z, 0), (0.4, Z, -40), (0.9, Z, 35), (1.3, Z, 0), (3.0, Z, 0)]},
                              'rootdummy': {'rot': [(0, Y, 0), (0.5, Y, 4), (1.0, Y, -4), (1.5, Y, 0), (3.0, Y, 0)]}}),
        clip('cwalk', 1.0, {
            'rootdummy': {'pos': [(0, (0, 0, 0)), (0.25, (0, 0, 0.01)), (0.5, (0, 0, 0)), (0.75, (0, 0, 0.01)), (1.0, (0, 0, 0))]},
            'torso': {'rot': [(0, X, 5), (0.5, X, 7), (1.0, X, 5)]},
            'legl': {'rot': [(0, X, -2), (0.5, X, 2), (1.0, X, -2)]},
            'legr': {'rot': [(0, X, 2), (0.5, X, -2), (1.0, X, 2)]},
            'head': {'rot': [(0, Z, 6), (0.5, Z, -6), (1.0, Z, 6)]}}, events=[(0.0, 'snd_footstep'), (0.5, 'snd_footstep')]),
        clip('cwalkinj', 1.6, {
            'rootdummy': {'pos': [(0, (0, 0, 0)), (0.4, (0, 0, -0.015)), (0.8, (0, 0, 0)), (1.2, (0, 0, -0.03)), (1.6, (0, 0, 0))],
                          'rot': [(0, Y, 3), (0.6, Y, -6), (1.1, Y, 3), (1.6, Y, 3)]},
            'torso': {'rot': [(0, X, 10), (0.8, X, 13), (1.6, X, 10)]},
            'legl': {'rot': [(0, X, -1), (0.8, X, 1.5), (1.6, X, -1)]},
            'legr': {'rot': [(0, X, 3), (0.8, X, -3), (1.6, X, 3)]},
            'head': {'rot': [(0, X, 8), (0.8, X, 12), (1.6, X, 8)]}}, events=[(0.0, 'snd_footstep'), (0.8, 'snd_footstep')]),
        clip('crun', 0.7, {
            'rootdummy': {'pos': [(0, (0, 0, 0)), (0.175, (0, 0, 0.02)), (0.35, (0, 0, 0)), (0.525, (0, 0, 0.02)), (0.7, (0, 0, 0))]},
            'torso': {'rot': [(0, X, 16), (0.35, X, 19), (0.7, X, 16)]},
            'legl': {'rot': [(0, X, -6), (0.35, X, -3), (0.7, X, -6)]},
            'legr': {'rot': [(0, X, -3), (0.35, X, -6), (0.7, X, -3)]}}, events=[(0.0, 'snd_footstep'), (0.35, 'snd_footstep')]),
    ]
    for name, side in (('creadyr', 1), ('creadyl', -1)):
        clips.append(clip(name, 2.0, {
            'torso': {'rot': [(0, X, 8), (1.0, X, 10), (2.0, X, 8)]},
            'head': {'rot': [(0, Z, 10 * side), (0.3, Z, -6 * side), (0.6, Z, 10 * side), (2.0, Z, 10 * side)]},
            'toolr': {'pos': [(0, slide(half)), (2.0, slide(half))]}}))
    clips += [attack_clip('ca1slashl', 30, prod), attack_clip('ca1slashr', -30, prod), attack_clip('ca1stab', 0, prod, 0.28),
              attack_clip('cclosel', 15, prod, 0.15), attack_clip('ccloseh', -15, prod, 0.15, 20), attack_clip('creach', 0, prod, 0.34, 18)]
    for name, dx in (('cdodges', 0), ('cdodgelr', 0.2), ('cparryl', -0.1), ('cparryr', 0.1)):
        dy = -0.2 if dx == 0 else -0.05
        clips.append(clip(name, 1.0, {
            'rootdummy': {'pos': [(0, (0, 0, 0)), (0.25, (dx, dy, 0)), (0.6, (dx, dy, 0)), (1.0, (0, 0, 0))]},
            'torso': {'rot': [(0, X, 8), (0.25, X, 16), (1.0, X, 8)]}}))
    for name, roll in (('cdamages', 0), ('cdamagel', 9), ('cdamager', -9)):
        clips.append(clip(name, 0.33, {
            'rootdummy': {'rot': [(0, Y, 0), (0.12, Y, roll), (0.33, Y, 0)]},
            'torso': {'rot': [(0, X, 8), (0.1, X, 20), (0.33, X, 8)]},
            'head': {'rot': [(0, Z, 0), (0.1, Z, 25), (0.33, Z, 0)]}}))
    clips += [
        clip('ckdbck', 1.0, {
            'rootdummy': {'pos': [(0, (0, 0, 0)), (0.3, (0, -0.1, 0.1)), (0.6, (0, 0, LIE_Z)), (1.0, (0, 0, LIE_Z))],
                          'rot': [(0, X, 0), (0.3, X, 35), (0.6, X, LIE + 6), (0.75, X, LIE - 4), (1.0, X, LIE)]},
            'head': {'rot': [(0, Z, 0), (0.6, Z, 60), (1.0, Z, 40)]}}, events=[(0.6, 'snd_hitground')]),
        clip('ckdbckps', 1.0, dict(lie, head={'rot': [(0, Z, 40), (0.5, Z, 20), (1.0, Z, 40)]})),
        clip('ckdbckdie', 1.0, dict(lie, head={'rot': [(0, Z, 40), (0.3, Z, -30), (0.6, Z, 15), (1.0, Z, 0)]})),
        clip('cdead', 1.0, lie),
        clip('cgustandb', 1.2, {
            'rootdummy': {'pos': [(0, (0, 0, LIE_Z)), (0.6, (0, -0.05, 0.1)), (1.0, (0, 0, 0)), (1.2, (0, 0, 0))],
                          'rot': [(0, X, LIE), (0.6, X, 30), (1.0, X, -4), (1.2, X, 0)]}}),
        clip('cguptokdb', 1.0, lie),
        clip('ctaunt', 1.7, {
            'rootdummy': {'rot': [(0, Y, 0), (0.3, Y, 9), (0.6, Y, -9), (0.9, Y, 9), (1.2, Y, -9), (1.7, Y, 0)]},
            'head': {'rot': [(0, Z, 0), (0.425, Z, 90), (0.85, Z, 180), (1.275, Z, 270), (1.7, Z, 360)]}}),
        clip('cconjure1', 2.0, {
            'torso': {'rot': [(0, X, 0), (0.5, X, 12), (2.0, X, 12)]},
            'head': {'rot': [(0, Z, 0), (0.5, Z, 45), (1.0, Z, -45), (1.5, Z, 45), (2.0, Z, 0)]},
            'tooll': {'pos': [(0, slide(blaster)), (0.6, slide(0)), (2.0, slide(0))]}}),
        clip('ccastout', 0.5, {
            'torso': {'rot': [(0, X, 12), (0.25, X, -2), (0.5, X, 4)]},
            'tooll': {'pos': [(0, slide(0)), (0.25, slide(0)), (0.32, slide(-0.06)), (0.5, slide(0))]}}, events=[(0.25, 'cast')]),
        clip('ccastoutlp', 1.0, {
            'torso': {'rot': [(0, X, 4), (0.5, X, 6), (1.0, X, 4)]},
            'tooll': {'pos': [(0, slide(0)), (1.0, slide(0))]}}),
        clip('cspasm', 1.0, {
            'rootdummy': {'rot': [(0, Y, 0), (0.1, Y, 6), (0.2, Y, -6), (0.3, Y, 6), (0.4, Y, -6), (1.0, Y, 0)]},
            'head': {'rot': [(0, Z, 0), (0.2, Z, 70), (0.4, Z, -70), (0.6, Z, 70), (1.0, Z, 0)]}}),
        clip('chturnl', 1.5, {'head': {'rot': [(0, Z, 0), (0.5, Z, 55), (1.0, Z, 55), (1.5, Z, 0)]}}),
        clip('chturnr', 1.5, {'head': {'rot': [(0, Z, 0), (0.5, Z, -55), (1.0, Z, -55), (1.5, Z, 0)]}}),
        clip('cvictory', 1.7, {
            'rootdummy': {'pos': [(0, (0, 0, 0)), (0.3, (0, 0, 0.06)), (0.5, (0, 0, 0)), (0.8, (0, 0, 0.06)), (1.0, (0, 0, 0)), (1.7, (0, 0, 0))]},
            'head': {'rot': [(0, Z, 0), (0.85, Z, 180), (1.7, Z, 360)]}}),
    ]
    return clips


def format_clip(spec, model, tool_lengths):
    """ASCII animation block. Every animated node is written, keyed at rest where the clip leaves it alone; tools rest
    retracted unless the clip keys them."""
    length = spec['length']
    lines = [f"newanim {spec['name']} {model}", f'  length {length:.4g}', f"  transtime {spec['transtime']:.4g}",
             f'  animroot {model}']
    lines += ['  event %.4g %s' % e for e in spec['events']]
    lines += [f'  node dummy {model}', '    parent NULL', '  endnode']
    rest = {'rootdummy': (0, 0, 0), 'toolr': slide(retract_offset(tool_lengths['prod'])), 'tooll': slide(retract_offset(tool_lengths['blaster']))}
    for node, parent in ANIM_NODES:
        track = spec['tracks'].get(node, {})
        if parent in SOCKET_PARENTS:
            # an unkeyed socket keeps the tool connected to the root; the compiler drops disconnected nodes
            lines += [f'  node dummy {parent}', f'    parent {SOCKET_PARENTS[parent]}', '  endnode']
        lines += [f'  node dummy {node}', f'    parent {parent or model}']
        if node in POSITION_NODES:
            pos = track.get('pos') or [(0, rest[node]), (length, rest[node])]
            lines += [f'    positionkey {len(pos)}'] + ['      %.4g %s' % (t, f(p)) for t, p in pos] + ['    endlist']
        rot = track.get('rot') or [(0, Z, 0), (length, Z, 0)]
        lines += [f'    orientationkey {len(rot)}'] + ['      %.4g %s %.6g' % (t, f(axis), math.radians(deg)) for t, axis, deg in rot]
        lines += ['    endlist', '  endnode']
    return lines + [f"doneanim {spec['name']} {model}"]


def clips_text(model, tool_lengths):
    lines = []
    for spec in clip_specs(tool_lengths):
        lines += format_clip(spec, model, tool_lengths)
    return '\n'.join(lines) + '\n'


# ---------------------------------------------------------------------------------------------------------------------
# Blender export
# ---------------------------------------------------------------------------------------------------------------------

def source_triangles(obj):
    """Triangles of an imported source (already in world space) as three (position, normal, uv) corners each."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    mesh.calc_normals_split()  # Blender 4.0
    uv = mesh.uv_layers.active.data
    triangles = []
    for tri in mesh.loop_triangles:
        corners = []
        for loop in tri.loops:
            corners.append((tuple(mesh.vertices[mesh.loops[loop].vertex_index].co), tuple(mesh.loops[loop].normal), tuple(uv[loop].uv)))
        triangles.append(corners)
    return triangles


def check_source_hash(item, manifest_dir):
    if hashlib.sha256((manifest_dir / item['source']).read_bytes()).hexdigest() != item['sha256']:
        raise ValueError(item['source'] + ': source GLB does not match its sha256')


def export_tool(tool, manifest_dir, resources, weapon):
    np = weapon.np
    source = manifest_dir / tool['source']
    check_source_hash(tool, manifest_dir)
    obj = meshy.import_source(weapon.bpy, source)
    mesh = tool_mesh(source_triangles(obj), tool['tip_axis'], tool['length'])
    maps = meshy.material_maps(weapon, source, resources.parent / 'image_cache', tool['texture'], tool['texture_size'])
    for name, pixels in maps.items():
        weapon.save_texture(resources / (name + '.tga'), np.asarray(pixels))
    (resources / (tool['texture'] + '.mtr')).write_text(tool_mtr(tool['texture']), encoding='ascii')
    return {'mesh': mesh, 'texture': tool['texture'], 'length': tool['length']}


def save_png(weapon, path, rgb):
    """Write an RGB float array (row 0 at the bottom, Blender order) as an sRGB PNG."""
    bpy, np = weapon.bpy, weapon.np
    h, w = rgb.shape[:2]
    rgba = np.ones((h, w, 4), np.float32)
    rgba[..., :3] = np.clip(rgb, 0, 1)
    image = bpy.data.images.new(path.stem, width=w, height=h, alpha=False)
    image.colorspace_settings.name = 'Non-Color'
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw = str(path)
    image.file_format = 'PNG'
    image.save()
    bpy.data.images.remove(image)


def review_tint(weapon, path, layer):
    np = weapon.np
    colors = np.zeros(layer.shape + (3,), np.float32)
    for index, color in LAYER_REVIEW_COLORS.items():
        colors[layer == index] = color
    save_png(weapon, path, colors)


def segment_parts(pivots, meshes, tool_meshes, sockets, extended):
    """World-space (name, verts, faces) parts of a chassis; tools extended at their sockets or retracted into the body."""
    torso = pivots['body']
    parts = []
    for name in ('head', 'body', 'legl', 'legr'):
        origin = pivots[name]
        parts.append((name, [tuple(a + b for a, b in zip(v, origin)) for v in meshes[name]['verts']], meshes[name]['faces']))
    for tool, node in (('prod', 'toolr'), ('blaster', 'tooll')):
        slideback = 0.0 if extended else retract_offset(tool_meshes[tool]['length'])
        origin = (torso[0] + sockets[node][0], torso[1] + sockets[node][1] + slideback, torso[2] + sockets[node][2])
        parts.append((node, [tuple(a + b for a, b in zip(v, origin)) for v in tool_meshes[tool]['mesh']['verts']], tool_meshes[tool]['mesh']['faces']))
    return parts


def review_segments(weapon, path, groups):
    """One orthographic render: each group (parts, x offset, quarter turns) in flat segment colours, all seen from +Y."""
    bpy, Vector = weapon.bpy, weapon.Vector
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'FLAT'
    scene.display.shading.color_type = 'OBJECT'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 800
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new('w')
    scene.world.color = (0.2, 0.2, 0.22)
    scene.view_settings.view_transform = 'Standard'
    points = []
    for parts, offset, turn in groups:
        for name, verts, faces in parts:
            placed = [(offset + (v[1] if turn else v[0]), (-v[0] if turn else v[1]), v[2]) for v in verts]
            points += placed
            mesh = bpy.data.meshes.new(name)
            mesh.from_pydata(placed, [], faces)
            obj = bpy.data.objects.new(name, mesh)
            obj.color = SEGMENT_REVIEW_COLORS[name] + (1.0,)
            scene.collection.objects.link(obj)
    lo = Vector([min(p[i] for p in points) for i in range(3)])
    hi = Vector([max(p[i] for p in points) for i in range(3)])
    camera = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = max(hi.x - lo.x, (hi.z - lo.z) * 2.2) * 1.1
    camera.location = ((lo.x + hi.x) / 2, 10, (lo.z + hi.z) / 2)
    camera.rotation_euler = (math.pi / 2, 0, math.pi)
    scene.collection.objects.link(camera)
    scene.camera = camera
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def export_chassis(item, supermodel, tool_meshes, manifest_dir, out, weapon):
    np = weapon.np
    check_source_hash(item, manifest_dir)
    source = manifest_dir / item['source']
    obj = meshy.import_source(weapon.bpy, source)
    triangles = source_triangles(obj)
    zs = [c[0][2] for corners in triangles for c in corners]
    frame = make_frame(min(zs), max(zs), item['height'], item.get('yaw_degrees', 180))
    meshes, pivots = chassis_meshes(triangles, frame, item['segments'])
    resources = out / 'resources'
    resources.mkdir(parents=True)

    body_bounds = mesh_bounds(meshes['body']['verts'])
    tool_bounds, outside, probes_inside = {}, [], {}
    for tool, node in (('prod', 'toolr'), ('blaster', 'tooll')):
        bounds = retracted_tool_bounds(tool_meshes[tool]['mesh']['verts'], item['sockets'][node], tool_meshes[tool]['length'])
        tool_bounds[node] = bounds
        probes_inside[node] = points_inside_mesh(bounds_probe_points(bounds), meshes['body']['verts'], meshes['body']['faces'])
        if not bounds_inside(bounds, body_bounds) or not all(probes_inside[node]):
            outside.append(node)
    if outside:
        raise ValueError(f"{item['model']}: retracted tools {outside} are not inside the body (bounds {body_bounds}, "
                         f'tool bounds {tool_bounds}, probes {probes_inside})')

    (resources / (item['model'] + '.mdl')).write_text(chassis_text(item, supermodel, pivots, meshes, tool_meshes), encoding='ascii')
    maps = meshy.material_maps(weapon, source, out / 'image_cache', item['material'], item['texture_size'])
    shade, layer = classify_tint(np.asarray(maps[item['material']], np.float32), item['tint'])
    (resources / (item['material'] + '.plt')).write_bytes(plt_bytes(shade, layer))
    weapon.save_texture(resources / (item['material'] + '_n.tga'), np.asarray(maps[item['material'] + '_n']))
    (resources / (item['material'] + '.mtr')).write_text(starter_mtr(item['material']), encoding='ascii')
    review_tint(weapon, out / 'review_tint.png', layer)
    review_segments(weapon, out / 'review_segments.png', [
        (segment_parts(pivots, meshes, tool_meshes, item['sockets'], False), 0.9, False),
        (segment_parts(pivots, meshes, tool_meshes, item['sockets'], True), -0.9, True)])
    shutil.rmtree(out / 'image_cache', ignore_errors=True)

    def box(bounds):
        return [[round(v, 4) for v in bounds[0]], [round(v, 4) for v in bounds[1]]]

    report = {'model': item['model'], 'source_sha256': item['sha256'], 'scale': round(frame['scale'], 6),
              'pivots': {k: [round(c, 4) for c in v] for k, v in pivots.items()},
              'segments': {k: {'vertices': len(m['verts']), 'triangles': len(m['faces']), 'bounds': box(mesh_bounds(m['verts']))}
                           for k, m in meshes.items()},
              'tools': {node: {'vertices': len(tool_meshes[tool]['mesh']['verts']), 'triangles': len(tool_meshes[tool]['mesh']['faces']),
                               'socket': item['sockets'][node], 'retracted_bounds_torso_space': box(tool_bounds[node])}
                        for tool, node in (('prod', 'toolr'), ('blaster', 'tooll'))},
              'body_bounds_torso_space': box(body_bounds), 'tools_inside_body': True,
              'retracted_tool_probe_points_inside_body': {k: sum(v) for k, v in probes_inside.items()} | {'of': 9}, 'layer_coverage': layer_coverage(layer),
              'plt_size': [int(layer.shape[1]), int(layer.shape[0])]}
    (out / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    return report


def export_all(manifest, manifest_dir, out, weapon, only=None):
    tool_lengths = {t['name']: t['length'] for t in manifest['tools']}
    shared = out / manifest['supermodel'] / 'resources'
    shared.mkdir(parents=True)
    tool_meshes = {t['name']: export_tool(t, manifest_dir, shared, weapon) for t in manifest['tools']}
    shutil.rmtree(shared.parent / 'image_cache', ignore_errors=True)
    (shared / (manifest['supermodel'] + '.mdl')).write_text(supermodel_text(manifest['supermodel'], tool_lengths), encoding='ascii')
    reports = []
    for item in manifest['chassis']:
        if only and item['model'] != only:
            continue
        reports.append(export_chassis(item, manifest['supermodel'], tool_meshes, manifest_dir, out / item['model'], weapon))
    return reports


# ---------------------------------------------------------------------------------------------------------------------
# Package (ordinary Python): compile the MDL with CleanModels and the maps with NWN Crunch
# ---------------------------------------------------------------------------------------------------------------------

def package(args, out):
    export = args.export.resolve()
    ascii_dir, runtime, logs = out / 'ascii', out / 'runtime_resources', out / 'logs'
    for d in (ascii_dir, runtime, logs):
        d.mkdir(parents=True)
    sources, roles = meshy.flatten_resources(export)
    for path in sources.values():
        if path.suffix in ('.mdl', '.mtr'):
            shutil.copy2(path, ascii_dir / path.name)
            if path.suffix == '.mtr':
                shutil.copy2(path, runtime / path.name)
            continue
        if path.suffix == '.plt':
            shutil.copy2(path, runtime / path.name)
            continue
        if path.suffix != '.tga':
            raise ValueError('Unexpected resource ' + path.name)
        width, height = struct.unpack_from('<HH', path.read_bytes(), 12)
        if width & (width - 1) or height & (height - 1):
            raise ValueError('Power-of-two texture dimensions required: ' + path.name)
        role = roles.get(path.stem.lower())
        if role is None:
            raise ValueError('No material binds ' + path.name)
        target = runtime / (path.stem + '.dds')
        result = subprocess.run([str(args.crunch), '-file', str(path), '-out', str(target), '-fileformat', 'dds', '-unflip',
                                 '-yflip', '-dxtQuality', 'uber'] + meshy.CRUNCH_OPTIONS[role], capture_output=True, text=True, timeout=300)
        (logs / (path.stem + '.log')).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or not target.is_file():
            raise ValueError('Crunch failed ' + path.name)
    compiled = out / 'compiled'
    compiled.mkdir()
    shutil.copytree(runtime, ascii_dir, dirs_exist_ok=True)
    result = subprocess.run([str(meshy.compiler_path(args.compiler)), 'compile', '--overwrite', str(ascii_dir), str(compiled)],
                            capture_output=True, text=True, timeout=600)
    (logs / 'compile.log').write_text(result.stdout + result.stderr)
    models = sorted(compiled.glob('*.mdl'))
    if result.returncode != 0 or len(models) != len(list(ascii_dir.glob('*.mdl'))):
        raise ValueError('Compile failed; see ' + str(logs / 'compile.log'))
    for model in models:
        if model.read_bytes()[:4] != b'\0\0\0\0':
            raise ValueError('Not a binary model: ' + model.name)
        shutil.copy2(model, runtime / model.name)
    report = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(runtime.iterdir())}
    (out / 'package_report.json').write_text(json.dumps(report, indent=2) + '\n')
    print('ASTROMECH_PACKAGE_COMPLETE ' + str(runtime))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mode', choices=['export', 'package'], required=True)
    parser.add_argument('--manifest', type=Path)
    parser.add_argument('--export', type=Path)
    parser.add_argument('--crunch', type=Path)
    parser.add_argument('--compiler', type=Path)
    parser.add_argument('--only', help='Export only this chassis model')
    parser.add_argument('--output', type=Path, required=True)
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else sys.argv[1:]
    args = parser.parse_args(argv)
    out = args.output.resolve()
    if out.is_relative_to(REPO):
        raise ValueError('Keep staging output outside the repository')
    if out.exists() and any(out.iterdir()):
        raise ValueError('Output directory must be empty')
    if args.mode == 'package':
        if not args.export or not args.crunch:
            parser.error('package requires --export and --crunch')
        out.mkdir(parents=True, exist_ok=True)
        package(args, out)
        return
    if not args.manifest:
        parser.error('export requires --manifest')
    manifest_dir = args.manifest.resolve().parent
    manifest = validate_manifest(json.loads(args.manifest.read_text()), manifest_dir)
    weapon = meshy.load_module('trellis_weapon_export', REPO / 'tools/TrellisWeaponExport.py')
    weapon.load_blender()
    out.mkdir(parents=True, exist_ok=True)
    try:
        reports = export_all(manifest, manifest_dir, out, weapon, args.only)
    except BaseException:
        shutil.rmtree(out, ignore_errors=True)
        raise
    (out / 'reports.json').write_text(json.dumps(reports, indent=2) + '\n')
    print('ASTROMECH_EXPORT_COMPLETE ' + str(out))


if __name__ == '__main__':
    main()
