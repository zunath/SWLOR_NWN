"""Stage textured TRELLIS/glTF 2 placeables (crafting stations); never deploy or edit module data.

Blender 4.0: blender -b --factory-startup --python-exit-code 1 --python
 tools/TrellisPlaceableExport.py -- --mode inspect|export --manifest PATH --output EMPTY.
Python: python -B tools/TrellisPlaceableExport.py --mode package --export PATH
 --crunch crunch.exe --compiler cleanmodels.exe --output EMPTY.

This is the placeable counterpart of TrellisWeaponExport.py and reuses its GLB, material, closed-surface,
normal-reprojection, compile and DDS code. A placeable differs from a weapon in its fit (a ground origin and a
footprint instead of a grip), its extra resources (a PWK walkmesh with use nodes, no icons) and its 2DA row.

Schema 1 assets (all paths relative to the manifest):
  asset, source (GLB), sha256 (of that GLB), model (resref, 16 characters), short (effect dummy prefix, 8),
  texture (14 characters), texture_size (512 or 1024), scalar_texture_size (specular and roughness maps, optional);
  yaw_degrees or orientation (proper 3x3 rotation of the imported Blender world model);
  fit: exactly one of width/depth/height (meters along the rotated X/Y/Z extent) or scale;
  center_xy (where the bounding box center lands in model space; the bottom always sits on Z=0);
  max_size [w, d, h] (hard limit on the fitted bounding box);
  walkmesh {prefix, inset, simplify, min_use_clearance, use_nodes [[x, y], ...]}: at most two use nodes, none keeps the
    engine's default approach;
  emission {threshold, softness, gain, saturation_min?}: a glow map from the brightest base-color texels;
  double_sided_policy: reject, closed_surface (exact closure, as weapons) or near_closed (outward-wound shells with at
    most max_defect_fraction open edges and max_opposed_fraction of the area wound against its normals);
  normal_basis_policy: reproject, or authored_tangents (mixed tangent handedness) with authored_angle_limit_degrees
    0.5..2 for compiler normal quantization;
  reference: the replaced station (bounds, footprint, use nodes), recorded in the report and the review renders only.
Output per asset: resources/ (ASCII MDL, MTR, TGA maps), walkmesh/<model>.pwk, review renders and report.json. Package
output is compiled MDL + MTR + DDS + PWK only. Native verification is separate.
"""
from __future__ import annotations

import argparse, hashlib, importlib.util, json, math, re, subprocess, sys
sys.dont_write_bytecode = True
from pathlib import Path

MODEL_RE = re.compile(r'[a-z][a-z0-9_]{0,15}')
SHORT_RE = re.compile(r'[a-z][a-z0-9]{0,7}')
TEXTURE_RE = re.compile(r'[a-z][a-z0-9_]{0,13}')
PWK_PREFIX_RE = re.compile(r'[a-z0-9]{3}_')
SURFACE_NONWALK = 7
DUMMIES = ('impact', 'ground', 'hand', 'head', 'head_hit')


# ---------------------------------------------------------------------------
# Pure helpers (no Blender): geometry, MDL/PWK text, validation.
# ---------------------------------------------------------------------------

def finite(value):
    return type(value) in (int, float) and math.isfinite(value)


def vector(value, length, label):
    if not isinstance(value, list) or len(value) != length or not all(finite(x) for x in value):
        raise ValueError(f'{label} must be {length} finite numbers')
    return [float(x) for x in value]


def convex_hull(points):
    """Counter-clockwise convex hull (Andrew's monotone chain) of 2D points, collinear points removed."""
    pts = sorted({(round(float(x), 6), round(float(y), 6)) for x, y in points})
    if len(pts) < 3:
        raise ValueError('A footprint needs at least three distinct points')

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])

    lower, upper = [], []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    hull = lower[:-1] + upper[:-1]
    if len(hull) < 3:
        raise ValueError('Footprint points are collinear')
    return hull


def simplify_convex(poly, tolerance):
    """Drop hull vertices that deviate less than `tolerance` meters from the chord of their neighbours (repeatedly,
    smallest deviation first). A subset of a convex polygon's vertices stays convex."""
    poly = list(poly)
    while len(poly) > 3:
        best, index = None, None
        for i in range(len(poly)):
            a, b, c = poly[i - 1], poly[i], poly[(i + 1) % len(poly)]
            chord = math.hypot(c[0] - a[0], c[1] - a[1])
            deviation = abs((c[0] - a[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (c[1] - a[1])) / chord if chord else 0.0
            if best is None or deviation < best:
                best, index = deviation, i
        if best >= tolerance:
            break
        del poly[index]
    return poly


def polygon_area(poly):
    return sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(poly, poly[1:] + poly[:1])) / 2


def inset_polygon(poly, inset):
    """Shrink a convex CCW polygon by moving every edge inward by `inset` meters."""
    if inset <= 0:
        return list(poly)
    lines = []
    for a, b in zip(poly, poly[1:] + poly[:1]):
        dx, dy = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dy)
        nx, ny = -dy / length, dx / length  # inward for CCW
        lines.append((nx, ny, nx * a[0] + ny * a[1] + inset))
    result = []
    for (a1, b1, c1), (a2, b2, c2) in zip(lines[-1:] + lines[:-1], lines):
        det = a1 * b2 - a2 * b1
        if abs(det) < 1e-9:
            raise ValueError('Cannot inset a footprint with parallel adjacent edges')
        result.append(((c1 * b2 - c2 * b1) / det, (a1 * c2 - a2 * c1) / det))
    if polygon_area(result) <= 0 or polygon_area(result) > polygon_area(poly):
        raise ValueError('Footprint inset collapses the polygon')
    return result


def signed_distance(point, poly):
    """Distance from a point to a convex CCW polygon: negative inside, positive outside."""
    best, inside = float('inf'), True
    for a, b in zip(poly, poly[1:] + poly[:1]):
        ex, ey = b[0] - a[0], b[1] - a[1]
        length2 = ex * ex + ey * ey
        t = max(0.0, min(1.0, ((point[0] - a[0]) * ex + (point[1] - a[1]) * ey) / length2))
        best = min(best, math.hypot(point[0] - (a[0] + t * ex), point[1] - (a[1] + t * ey)))
        if ex * (point[1] - a[1]) - ey * (point[0] - a[0]) < 0:
            inside = False
    return -best if inside else best


def build_pwk(model, footprint, use_nodes, prefix):
    """ASCII PWK: one flat nonwalkable convex footprint at Z=0 and engine-readable use nodes.

    The engine reads a use node only when its name is a four-character prefix plus pwk_use01/02 and it is
    not left at the model origin (PlaceableWalkmeshUseNodeTests)."""
    if not PWK_PREFIX_RE.fullmatch(prefix):
        raise ValueError('walkmesh prefix must be three lowercase characters or digits plus an underscore')
    if len(use_nodes) > 2:
        raise ValueError('The engine reads at most two use nodes (01 and 02)')
    if polygon_area(footprint) <= 0:
        raise ValueError('Footprint must be counter-clockwise')
    lines = ['# pwk file generated by tools/TrellisPlaceableExport.py', '#', '#NWmax PWKMESH  ASCII',
             'node trimesh PWK', f'  parent {model}_pwk', '  position 0.0 0.0 0.0', '  orientation 1.0 0.0 0.0 0.0',
             '  wirecolor 0.541176 0.0313726 0.431373', '  bitmap NULL', f'  verts {len(footprint)}']
    lines += [f'    {x:.6f} {y:.6f} 0.0' for x, y in footprint]
    faces = [(0, i, i + 1) for i in range(1, len(footprint) - 1)]
    lines += [f'  faces {len(faces)}']
    lines += [f'    {a} {b} {c}  1  0 0 0  {SURFACE_NONWALK}' for a, b, c in faces]
    lines += ['  tverts 1', '    0.0 0.0 0', 'endnode']
    for number, (x, y) in enumerate(use_nodes, 1):
        if x == 0 and y == 0:
            raise ValueError('A use node at the model origin is ignored by the engine')
        lines += [f'node dummy {prefix}pwk_use{number:02d}', f'  parent {model}_pwk', f'  position {x:.6f} {y:.6f} 0.0',
                  '  orientation 1.0 0.0 0.0 0.0', '  wirecolor 0.345098 0.694118 0.101961', 'endnode']
    return '\n'.join(lines) + '\n'


def write_mdl_text(model, short, meshes, lo, hi):
    """ASCII placeable MDL. `meshes` is a list of (texture, (verts, uvs, faces, normals)).

    Static classification CHARACTER matches the shipped placeables; the five effect dummies mirror the replaced
    stations (impact, ground, hand, head, head_hit)."""
    cx, cy, height = (lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, hi[2]
    anchors = {'impact': (cx, cy, height * .5), 'ground': (cx, cy, 0.0), 'hand': (cx, cy, height * .8),
               'head': (cx, cy, height), 'head_hit': (cx, cy, height * .75)}
    lines = ['#MAXMODEL ASCII', f'newmodel {model}', f'setsupermodel {model} NULL', 'classification CHARACTER',
             'setanimationscale 1', f'beginmodelgeom {model}', f'node dummy {model}', '  parent NULL', 'endnode']
    for name in DUMMIES:
        x, y, z = anchors[name]
        lines += [f'node dummy {short}_{name}', f'  parent {model}', f'  position {x:.9g} {y:.9g} {z:.9g}',
                  '  orientation 0 0 1 0', 'endnode']
    for index, (texture, (verts, uvs, faces, normals)) in enumerate(meshes):
        if not verts or not faces or max(len(verts), len(faces)) > 65535:
            raise ValueError('Empty mesh or NWN vertex/face limit exceeded: ' + texture)
        if not (len(uvs) == len(verts) == len(normals)):
            raise ValueError('Every exported vertex needs a UV and a normal')
        if any(not math.isfinite(n) for v in verts for n in v) or any(not math.isfinite(n) for uv in uvs for n in uv):
            raise ValueError('Non-finite vertex or UV')
        if any(min(f) < 0 or max(f) >= len(verts) for f in faces):
            raise ValueError('Invalid face index')
        if any(abs(sum(c * c for c in n) - 1) > .001 for n in normals):
            raise ValueError('Every exported vertex needs a unit normal')
        lines += [f'node trimesh mesh{index}', f'  parent {model}', '  position 0 0 0', '  orientation 0 0 1 0',
                  '  ambient 1 1 1', '  diffuse 1 1 1', '  specular 0 0 0', '  shininess 20', '  shadow 1', '  render 1',
                  '  tilefade 0', f'  bitmap {texture}', f'  materialname {texture}',
                  '  renderhint NormalAndSpecMapped', f'  verts {len(verts)}']
        lines += ['    ' + ' '.join(f'{n:.9g}' for n in v) for v in verts]
        lines += [f'  normals {len(normals)}'] + ['    ' + ' '.join(f'{n:.9g}' for n in v) for v in normals]
        lines += [f'  tverts {len(uvs)}'] + [f'    {u:.9g} {v:.9g} 0' for u, v in uvs]
        lines += [f'  faces {len(faces)}'] + [f'    {a} {b} {c} 1 {a} {b} {c} 0' for a, b, c in faces]
        lines += ['endnode']
    lines += [f'endmodelgeom {model}', f'donemodel {model}']
    return '\n'.join(lines) + '\n'


def emission_mask(color, threshold, softness, np, saturation_min=None):
    """Smooth 0..1 mask of glowing texels from an sRGB base color (H,W,3): glow without a hand-painted map.

    By default a texel glows when its luminance passes `threshold` (white-hot metal, plasma). With `saturation_min` the
    brightest channel passes `threshold` and the color is saturated at least that much instead, which picks out colored
    lights (a purple hologram strip) without lighting pale surfaces."""
    def smooth(x):
        x = np.clip(x, 0, 1)
        return x * x * (3 - 2 * x)

    if saturation_min is None:
        luminance = color[..., 0] * .2126 + color[..., 1] * .7152 + color[..., 2] * .0722
        return smooth((luminance - threshold) / max(softness, 1e-6))
    high, low = color.max(axis=-1), color.min(axis=-1)
    saturation = (high - low) / np.maximum(high, 1e-6)
    return smooth((high - threshold) / max(softness, 1e-6)) * smooth((saturation - saturation_min) / .15)


def rotation_rows(item):
    if 'orientation' in item and 'yaw_degrees' in item:
        raise ValueError('Use orientation or yaw_degrees, not both')
    if 'orientation' in item:
        rows = item['orientation']
        if not isinstance(rows, list) or len(rows) != 3:
            raise ValueError('orientation must be a 3x3 proper rotation')
        for row in rows:
            vector(row, 3, 'orientation row')
        for i in range(3):
            for j in range(3):
                if abs(sum(rows[i][k] * rows[j][k] for k in range(3)) - (i == j)) > 1e-6:
                    raise ValueError('orientation must be orthonormal; scale belongs in fit')
        a, b, c = rows
        det = a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])
        if abs(det - 1) > 1e-6:
            raise ValueError('orientation must preserve handedness')
        return [list(map(float, r)) for r in rows]
    yaw = math.radians(item.get('yaw_degrees', 0))
    if not finite(yaw):
        raise ValueError('yaw_degrees must be finite')
    c, s = math.cos(yaw), math.sin(yaw)
    return [[c, -s, 0.0], [s, c, 0.0], [0.0, 0.0, 1.0]]


def validate_item(item, manifest_dir, haks_names=None, inspect_only=False):
    """Validate one manifest asset and return it with `_source` resolved. Raises ValueError."""
    asset = item.get('asset', '')
    if not re.fullmatch(r'[a-z][a-z0-9_]{0,47}', asset):
        raise ValueError('asset must be a lowercase identifier: ' + repr(asset))
    source = (manifest_dir / item.get('source', '')).resolve()
    if not source.is_file():
        raise ValueError(f'{asset}: missing source GLB {source}')
    if hashlib.sha256(source.read_bytes()).hexdigest() != item.get('sha256'):
        raise ValueError(f'{asset}: source GLB sha256 does not match the manifest')
    item['_source'] = str(source)
    if inspect_only:
        return item
    for key, pattern in (('model', MODEL_RE), ('short', SHORT_RE), ('texture', TEXTURE_RE)):
        if not isinstance(item.get(key), str) or not pattern.fullmatch(item[key]):
            raise ValueError(f'{asset}: {key} does not match {pattern.pattern}')
    if item.get('texture_size') not in (512, 1024):
        raise ValueError(f'{asset}: texture_size must be 512 or 1024')
    if item.get('scalar_texture_size', item['texture_size']) not in (256, 512, 1024):
        raise ValueError(f'{asset}: scalar_texture_size must be 256, 512 or 1024')
    if haks_names is not None:
        taken = [n for n in (item['model'], item['texture']) if n in haks_names]
        taken += [item['texture'] + s for s in ('_n', '_s', '_r', '_e') if item['texture'] + s in haks_names]
        if taken:
            raise ValueError(f'{asset}: resource names already exist in the haks: {taken}')
    rotation_rows(item)
    fit = item.get('fit')
    if not isinstance(fit, dict) or len(fit) != 1 or next(iter(fit)) not in ('width', 'depth', 'height', 'scale'):
        raise ValueError(f'{asset}: fit needs exactly one of width, depth, height or scale')
    if not finite(next(iter(fit.values()))) or next(iter(fit.values())) <= 0:
        raise ValueError(f'{asset}: fit value must be a positive number')
    vector(item.get('center_xy', [0, 0]), 2, f'{asset}: center_xy')
    vector(item.get('max_size'), 3, f'{asset}: max_size')
    walk = item.get('walkmesh')
    if not isinstance(walk, dict) or not PWK_PREFIX_RE.fullmatch(walk.get('prefix', '')):
        raise ValueError(f'{asset}: walkmesh.prefix must be three characters plus an underscore')
    nodes = walk.get('use_nodes')
    if not isinstance(nodes, list) or len(nodes) > 2:
        raise ValueError(f'{asset}: walkmesh.use_nodes is a list of at most two [x, y] positions (empty keeps the engine default approach)')
    for node in nodes:
        vector(node, 2, f'{asset}: use node')
    if not 0 <= walk.get('inset', 0) <= .3 or not finite(walk.get('min_use_clearance', .1)) or not 0 <= walk.get('simplify', .05) <= .2:
        raise ValueError(f'{asset}: walkmesh.inset must be 0..0.3, simplify 0..0.2 and min_use_clearance finite')
    emission = item.get('emission')
    if emission is not None and (not isinstance(emission, dict) or not all(
            finite(emission.get(k, 0)) for k in ('threshold', 'softness', 'gain', 'saturation_min'))):
        raise ValueError(f'{asset}: emission needs numeric threshold, softness and gain (and optionally saturation_min)')
    for key in ('max_defect_fraction', 'max_opposed_fraction'):
        if not finite(item.get(key, 0)) or not 0 <= item.get(key, 0) <= .25:
            raise ValueError(f'{asset}: {key} must be 0..0.25')
    if item.get('normal_basis_policy', 'reproject') not in ('reproject', 'authored_tangents'):
        raise ValueError(f'{asset}: unknown normal_basis_policy')
    if not finite(item.get('authored_angle_limit_degrees', .5)) or not .5 <= item.get('authored_angle_limit_degrees', .5) <= 2:
        raise ValueError(f'{asset}: authored_angle_limit_degrees must be 0.5..2')
    if item.get('double_sided_policy', 'reject') not in ('reject', 'closed_surface', 'near_closed'):
        raise ValueError(f'{asset}: unknown double_sided_policy')
    return item


def load_manifest(path):
    data = Path(path).read_bytes()
    return json.loads(data.decode('utf-8-sig')), hashlib.sha256(data).hexdigest()


def validate_manifest(config, manifest_path, haks_names=None, inspect_only=False):
    if config.get('schema_version') != 1 or not isinstance(config.get('assets'), list) or not config['assets']:
        raise ValueError('Expected schema_version 1 and nonempty assets')
    names = set()
    for item in config['assets']:
        validate_item(item, Path(manifest_path).resolve().parent, haks_names, inspect_only)
        for key in ('asset',) if inspect_only else ('asset', 'model', 'texture'):
            if (key, item[key]) in names:
                raise ValueError('Duplicate ' + key + ' ' + item[key])
            names.add((key, item[key]))
    return config


def haks_resource_names(repo):
    """Lowercase resource basenames already tracked in SWLOR_Haks, or None when it is not a git checkout."""
    folder = Path(repo) / 'SWLOR_Haks'
    if not (folder / '.git').exists():
        return None
    result = subprocess.run(['git', '-C', str(folder), 'ls-files', '-z'], capture_output=True, check=False)
    if result.returncode != 0:
        return None
    names = set()
    for entry in result.stdout.split(b'\0'):
        name = entry.decode('utf-8', 'replace').rsplit('/', 1)[-1]
        if name and '.' in name:
            names.add(name.rsplit('.', 1)[0].lower())
    return names


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


# ---------------------------------------------------------------------------
# Blender stages (inspect/export). `weapon` is the loaded TrellisWeaponExport module.
# ---------------------------------------------------------------------------

CLAMP, REPEAT, LINEAR, TRILINEAR = 33071, 10497, 9729, 9987


def accept_clamped_samplers(weapon, doc, binary):
    """ComfyUI 0.39 writes clamp-to-edge, non-mipmapped samplers; the weapon exporter only accepts the repeat,
    trilinear convention it was tested with. A clamped sampler is identical to a repeating one while every UV lies
    inside 0..1, and NWN always mips and repeats, so accept it only on that proof and rewrite it to the accepted form.
    Returns a report dict (empty when nothing changed)."""
    samplers = doc.get('samplers', [])
    accepted = {'wrapS': REPEAT, 'wrapT': REPEAT, 'magFilter': LINEAR, 'minFilter': TRILINEAR}
    changed = [i for i, s in enumerate(samplers) if any(s.get(k, v) != v for k, v in accepted.items())]
    if not changed:
        return {}
    for i in changed:
        sampler = samplers[i]
        unsupported = (sampler.get('extensions') or sampler.get('wrapS', REPEAT) not in (CLAMP, REPEAT)
                       or sampler.get('wrapT', REPEAT) not in (CLAMP, REPEAT) or sampler.get('magFilter', LINEAR) != LINEAR
                       or sampler.get('minFilter', TRILINEAR) not in (LINEAR, TRILINEAR))
        if unsupported:
            raise ValueError('Unsupported texture sampler; explicit conversion required')
    normals = weapon.load_module_snapshot('trellis_weapon_normals', Path(weapon.__file__).with_name('TrellisWeaponNormals.py'))
    low, high = 1e9, -1e9
    for mesh in doc.get('meshes', []):
        for primitive in mesh.get('primitives', []):
            uv = normals.accessor(doc, binary, primitive['attributes']['TEXCOORD_0'])
            low, high = min(low, float(uv.min())), max(high, float(uv.max()))
    if low < -1e-6 or high > 1 + 1e-6:
        raise ValueError(f'Clamped sampler needs UVs inside 0..1; found {low:.4f}..{high:.4f}')
    for i in changed:
        samplers[i] = dict(accepted)
    return {'clamp_to_repeat': True, 'uv_min': low, 'uv_max': high, 'samplers': changed}


DEFECT_FRACTION = 0.005
OPPOSED_FRACTION = 0.03


def shell_analysis(weapon, objects):
    """Weld positions and return (vertices, faces, face origin (object, polygon), directed and undirected edge counts, shell id per face)."""
    np = weapon.np
    lookup, vertices, faces, origin = {}, [], [], []
    for ob in objects:
        world = ob.matrix_world
        for triangle in ob.data.loop_triangles:
            ids = []
            for index in triangle.vertices:
                point = tuple(round(c, 6) for c in (world @ ob.data.vertices[index].co))
                if point not in lookup:
                    lookup[point] = len(vertices)
                    vertices.append(point)
                ids.append(lookup[point])
            if len(set(ids)) == 3:
                faces.append(ids)
                origin.append((ob, triangle.polygon_index))
    vertices, faces = np.asarray(vertices, np.float64), np.asarray(faces)
    directed, undirected = {}, {}
    for a, b, c in faces:
        for edge in ((a, b), (b, c), (c, a)):
            directed[edge] = directed.get(edge, 0) + 1
    for (a, b), count in directed.items():
        key = (a, b) if a < b else (b, a)
        undirected[key] = undirected.get(key, 0) + count
    parent = list(range(len(faces)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    owner = {}
    for index, (a, b, c) in enumerate(faces):
        for edge in ((a, b), (b, c), (c, a)):
            key = edge if edge[0] < edge[1] else (edge[1], edge[0])
            if key in owner:
                parent[find(index)] = find(owner[key])
            else:
                owner[key] = index
    roots = np.asarray([find(i) for i in range(len(faces))])
    return vertices, faces, origin, directed, undirected, roots


def shell_volume(np, vertices, chosen):
    corners = vertices[chosen]
    base = corners[:, 0, :].mean(axis=0)
    a, b, c = corners[:, 0] - base, corners[:, 1] - base, corners[:, 2] - base
    return float(np.einsum('ij,ij->i', a, np.cross(b, c)).sum() / 6)


def enclosed_by(vertices, outward_faces, point):
    """True when rays from `point` cross the outward shells an odd number of times (it lies inside one)."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    tree = BVHTree.FromPolygons([Vector(v) for v in vertices.tolist()], [tuple(int(i) for i in f) for f in outward_faces], all_triangles=True)
    votes = 0
    for direction in ((0, 0, 1), (0.31, 0.17, 0.93), (-0.23, 0.41, 0.88)):
        step = Vector(direction).normalized()
        origin, crossings = Vector(point) + step * 1e-4, 0
        while crossings < 200:
            hit = tree.ray_cast(origin, step)
            if hit[0] is None:
                break
            crossings += 1
            origin = hit[0] + step * 1e-4
        votes += crossings % 2
    return votes >= 2


def opposed_fraction(weapon, objects):
    """Area fraction of triangles whose winding opposes their authored corner normals."""
    from mathutils import Vector
    total = opposed = 0.0
    for ob in objects:
        ob.data.calc_normals_split()
        for triangle in ob.data.loop_triangles:
            a, b, c = (ob.matrix_world @ ob.data.vertices[i].co for i in triangle.vertices)
            cross = (b - a).cross(c - a)
            mean = sum((ob.data.corner_normals[i].vector for i in triangle.loops), Vector((0, 0, 0)))
            area = cross.length / 2
            total += area
            if cross.dot(mean) < 0:
                opposed += area
    return opposed / total if total else 0.0


def near_closed_report(weapon, objects, max_defect_fraction, max_opposed=OPPOSED_FRACTION):
    """Culling-safety proof for TRELLIS shells that are closed except for a few stray edges.

    The weapon exporter's closed_surface policy needs every edge shared by exactly two oppositely wound faces.
    Decimated TRELLIS stations typically have a handful of boundary or non-manifold edges. Weld positions, then require
    the open (boundary) and inconsistently wound edges to be at most `max_defect_fraction` of all edges and every
    substantial shell to have positive signed volume (outward winding), so single-sided culling hides only back faces,
    never the visible surface, and at most `max_opposed` (3 percent by default) of the surface area may be wound against
    the authored corner normals, which TRELLIS bakes outward. The TRELLIS winding is kept as generated: recalculating it
    on welded topology agreed with the authored normals worse than the original (measured on the refinery and galley).
    Non-manifold edges, where machine parts intersect, are reported but harmless to culling.
    A substantial inward-wound shell is accepted only when a ray test proves it is an internal cavity inside an outward
    shell; such cavities are never visible and are deleted from the export (counted in the report).
    The report keeps the counts so the PR can state what was accepted; a visual review still decides."""
    np = weapon.np
    vertices, faces, origin, directed, undirected, roots = shell_analysis(weapon, objects)
    removed_cavities = []
    total = len(faces)
    volumes = {int(root): shell_volume(np, vertices, faces[roots == root]) for root in set(roots.tolist())}
    counts = {int(root): int((roots == root).sum()) for root in volumes}
    inward = [root for root in volumes if volumes[root] <= 0 and counts[root] > total * .01]
    if inward:
        outward = [root for root in volumes if volumes[root] > 0 and counts[root] > total * .01]
        outward_faces = faces[np.isin(roots, outward)]
        for root in inward:
            probe = vertices[faces[roots == root][0][0]]
            if not len(outward_faces) or not enclosed_by(vertices, outward_faces, probe):
                raise ValueError('Near-closed policy: an inward-wound shell is not proven to be an enclosed cavity: '
                                 + str(sorted(({'triangles': counts[r], 'signed_volume': volumes[r]} for r in volumes), key=lambda s: -s['triangles'])[:6]))
            removed_cavities.append({'triangles': counts[root], 'signed_volume': volumes[root]})
        doomed = {}
        for index in np.flatnonzero(np.isin(roots, inward)):
            ob, polygon = origin[int(index)]
            doomed.setdefault(ob, set()).add(polygon)
        import bmesh
        for ob, polygons in doomed.items():
            mesh = bmesh.new()
            mesh.from_mesh(ob.data)
            mesh.faces.ensure_lookup_table()
            bmesh.ops.delete(mesh, geom=[mesh.faces[i] for i in sorted(polygons)], context='FACES')
            mesh.to_mesh(ob.data)
            mesh.free()
            ob.data.update()
            ob.data.calc_loop_triangles()
        vertices, faces, origin, directed, undirected, roots = shell_analysis(weapon, objects)
        total = len(faces)
        volumes = {int(root): shell_volume(np, vertices, faces[roots == root]) for root in set(roots.tolist())}
        counts = {int(root): int((roots == root).sum()) for root in volumes}
    shells = [{'triangles': counts[r], 'signed_volume': volumes[r]} for r in volumes]
    boundary = sum(1 for count in undirected.values() if count == 1)
    nonmanifold = sum(1 for count in undirected.values() if count > 2)
    # Inconsistent winding only counts on manifold edges: a junction of three or more faces always repeats a direction.
    same_direction = sum(1 for (u, v), count in directed.items()
                         if count == 2 and undirected[(u, v) if u < v else (v, u)] == 2)
    defects = boundary + same_direction
    bad = [s for s in shells if s['signed_volume'] <= 0 and s['triangles'] > total * .01]
    if not shells or max(shells, key=lambda s: s['triangles'])['signed_volume'] <= 0 or bad:
        raise ValueError('Near-closed policy needs outward (positive volume) winding for every substantial shell: '
                         + str(sorted(shells, key=lambda s: -s['triangles'])[:6]))
    if defects > max_defect_fraction * max(len(undirected), 1):
        raise ValueError(f'Near-closed policy: {defects} open or inconsistent edges of {len(undirected)} exceed {max_defect_fraction:.4f} (boundary {boundary}, same-direction {same_direction})')
    opposed = opposed_fraction(weapon, objects)
    if opposed > max_opposed:
        raise ValueError(f'Near-closed policy: {opposed:.3%} of the surface area is wound against its authored normals (limit {max_opposed:.1%})')
    return {'conversion': 'doubleSided=true to native single-sided; outward-wound near-closed shells', 'triangles': int(total),
            'edges': len(undirected), 'boundary_edges': boundary, 'nonmanifold_edges': nonmanifold,
            'same_direction_edges': same_direction, 'defect_fraction': defects / max(len(undirected), 1),
            'opposed_area_fraction': opposed,
            'max_defect_fraction': max_defect_fraction, 'shells': sorted(shells, key=lambda s: -s['triangles'])[:8],
            'shell_count': len(shells), 'enclosed_cavities_removed': removed_cavities}


def repair_exported_normals(weapon, fit, doc, binary, exported):
    """Blender's glTF import can leave a corner normal at zero (it merges coincident vertices that carry different custom
    normals). Take such a vertex's normal from the glTF vertex at the same source position instead, carried through the
    fit rotation like every other normal; fall back to the area-weighted face normal. Returns (mesh, count)."""
    np, Vector = weapon.np, weapon.Vector
    verts, uvs, faces, normals = exported
    bad = [i for i, n in enumerate(normals) if abs(sum(c * c for c in n) - 1) > .001]
    if not bad:
        return exported, 0
    helper = weapon.load_module_snapshot('trellis_weapon_normals', Path(weapon.__file__).with_name('TrellisWeaponNormals.py'))
    prim = doc['meshes'][0]['primitives'][0]
    position = helper.accessor(doc, binary, prim['attributes']['POSITION']).astype(float)
    normal = helper.accessor(doc, binary, prim['attributes']['NORMAL']).astype(float)
    table = {}
    for p, n in zip(position, normal):
        table.setdefault(tuple(np.round(p, 4)), []).append(n)
    inverse = fit.inverted()
    transform = fit.to_3x3().inverted().transposed()
    fixed = list(normals)
    for i in bad:
        source = inverse @ Vector(verts[i])
        key = tuple(np.round([source.x, source.z, -source.y], 4))
        candidates = table.get(key)
        if candidates:
            n = np.mean(candidates, axis=0)
            n = Vector((n[0], -n[2], n[1]))
            n = (transform @ n).normalized() if n.length else Vector((0, 0, 1))
        else:
            n = Vector((0, 0, 0))
            for face in faces:
                if i in face:
                    a, b, c = (Vector(verts[j]) for j in face)
                    n += (b - a).cross(c - a)
            n = n.normalized() if n.length else Vector((0, 0, 1))
        fixed[i] = tuple(n)
    return (verts, uvs, faces, fixed), len(bad)


def import_glb(weapon, item, out, mode):
    """Import the self-contained GLB with identity-tagged materials; return (doc, binary, objects, source_sha)."""
    bpy = weapon.bpy
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.world = bpy.data.worlds.new('Studio')
    source = Path(item['_source'])
    source_bytes = source.read_bytes()
    source_sha = hashlib.sha256(source_bytes).hexdigest()
    if item.get('sha256') and source_sha != item['sha256']:
        raise ValueError('Source GLB changed after manifest validation: ' + str(source))
    doc, binary = weapon.glb_document(source_bytes)
    item['_sampler_normalization'] = accept_clamped_samplers(weapon, doc, binary)
    cache = out / 'source_images'
    cache.mkdir()
    policy = item.get('double_sided_policy', 'reject')
    if mode == 'export':
        weapon.validate_glb_geometry(doc, policy in ('closed_surface', 'near_closed'))
    imported = json.loads(json.dumps(doc))
    weapon.tag_import_materials(imported)
    import_path = cache / 'import.glb'
    weapon.write_glb(import_path, imported, binary)
    bpy.ops.import_scene.gltf(filepath=str(import_path))
    weapon.remove_imported_lights()
    objects = [ob for ob in bpy.context.scene.objects if ob.type == 'MESH']
    if not objects or any(ob.type == 'ARMATURE' for ob in bpy.context.scene.objects):
        raise ValueError('Expected static rigid placeable meshes')
    for ob in objects:
        if ob.modifiers or not ob.data.uv_layers:
            raise ValueError('Modifiers or missing UVs require explicit handling')
        if ob.matrix_world.to_3x3().determinant() <= 0:
            raise ValueError('Mirrored source transform requires explicit winding review')
        ob.data.calc_loop_triangles()
    return doc, binary, cache, objects, source_sha


def fit_transform(weapon, item, objects):
    """Rotation, uniform scale and translation placing the bottom of the bounding box on Z=0 at center_xy."""
    Matrix, Vector = weapon.Matrix, weapon.Vector
    rows = rotation_rows(item)
    rotation = Matrix(rows).to_4x4()
    points = [rotation @ (ob.matrix_world @ v.co) for ob in objects for v in ob.data.vertices]
    lo = Vector([min(p[i] for p in points) for i in range(3)])
    hi = Vector([max(p[i] for p in points) for i in range(3)])
    key, value = next(iter(item['fit'].items()))
    extent = {'width': hi.x - lo.x, 'depth': hi.y - lo.y, 'height': hi.z - lo.z}
    scale = value if key == 'scale' else value / extent[key]
    center = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z)) * scale
    cx, cy = item.get('center_xy', [0, 0])
    translation = Vector((cx, cy, 0.0)) - center
    fit = Matrix.Translation(translation) @ rotation @ Matrix.Scale(scale, 4)
    return fit, rows, scale, list(translation)


NEGATIVE_Z_TOLERANCE = 0.02


def snap_negative_z(np, pixels, tolerance=NEGATIVE_Z_TOLERANCE):
    """Raise texels whose tangent-space Z is a few quantization steps below zero onto the horizon.

    The baked 8-bit normal map has a handful of grazing texels at z of about -0.004; BC5 stores only X and Y, so
    those cannot be kept as-is. Anything below -tolerance is a real back-facing normal and stays an error (the weapon
    exporter's rule). Returns (pixels, snapped_count)."""
    normal = pixels[..., :3] * 2 - 1
    length = np.linalg.norm(normal, axis=-1, keepdims=True)
    unit = normal / np.maximum(length, 1e-8)
    bad = unit[..., 2] < -1e-6
    if not bad.any():
        return pixels, 0
    if (unit[..., 2] < -tolerance).any():
        raise ValueError(f'Normal map has texels with tangent-space Z below -{tolerance}; the bake needs inspection')
    xy = unit[..., :2]
    xy_length = np.linalg.norm(xy, axis=-1, keepdims=True)
    snapped = np.where(bad[..., None], np.concatenate([xy / np.maximum(xy_length, 1e-8), np.zeros_like(xy_length)], axis=-1), unit)
    out = pixels.copy()
    out[..., :3] = snapped * .5 + .5
    return out, int(bad.sum())


def material_maps(weapon, item, doc, binary, index, source, cache, resources, name):
    """Same PBR conversion as the weapon exporter, with independent color/normal and scalar map sizes and an
    optional brightness-derived emissive map."""
    np = weapon.np
    mat = doc['materials'][index]
    weapon.validate_material(mat)
    pbr = mat.get('pbrMetallicRoughness', {})
    if mat.get('extensions') or mat.get('occlusionTexture'):
        raise ValueError('Material extensions or AO need explicit conversion')
    limit = item['texture_size']
    scalar_limit = item.get('scalar_texture_size', limit)
    factor = np.asarray(pbr.get('baseColorFactor', [1, 1, 1, 1]))
    if 'baseColorTexture' in pbr:
        base = weapon.read_image(doc, binary, pbr['baseColorTexture'], source, cache)
        base = weapon.srgb(weapon.resize(weapon.linear(base[..., :3]) * factor[:3], limit))
    else:
        base = np.broadcast_to(weapon.srgb(factor[:3]), (4, 4, 3))
    if 'metallicRoughnessTexture' in pbr:
        mr = weapon.read_image(doc, binary, pbr['metallicRoughnessTexture'], source, cache)
        metal = weapon.resize(mr[..., 2:3], scalar_limit)[..., 0] * pbr.get('metallicFactor', 1)
        rough = weapon.resize(mr[..., 1:2], scalar_limit)[..., 0] * pbr.get('roughnessFactor', 1)
    else:
        metal = np.full((4, 4), pbr.get('metallicFactor', 1), np.float32)
        rough = np.full((4, 4), pbr.get('roughnessFactor', 1), np.float32)
    spec = weapon.metallic_to_specularity(np.clip(metal, 0, 1))
    if 'normalTexture' in mat:
        nt = mat['normalTexture']
        raw, snapped = snap_negative_z(np, weapon.read_image(doc, binary, nt, source, cache))
        item.setdefault('_normal_z_snapped', {})[name] = snapped
        normal = weapon.prepare_normal_map(raw, nt.get('scale', 1), limit) * .5 + .5
    else:
        normal = np.broadcast_to(np.array([.5, .5, 1], np.float32), (4, 4, 3))
    maps = {name: base, name + '_n': normal, name + '_s': spec, name + '_r': rough}
    emission = item.get('emission')
    if emission:
        mask = emission_mask(np.asarray(base), emission['threshold'], emission['softness'], np, emission.get('saturation_min'))
        glow = np.clip(np.asarray(base) * mask[..., None] * emission.get('gain', 1), 0, 1)
        if float(mask.max()) > 0:
            maps[name + '_e'] = glow
    dims = {key: weapon.save_texture(resources / (key + '.tga'), value) for key, value in maps.items()}
    lines = ['renderhint NormalAndSpecMapped', 'texture0 ' + name, 'texture1 ' + name + '_n', 'texture2 ' + name + '_s',
             'texture3 ' + name + '_r']
    if name + '_e' in maps:
        lines.append('texture5 ' + name + '_e')
    (resources / (name + '.mtr')).write_text('\n'.join(lines) + '\n', encoding='ascii')
    return dims, maps


def review_renders(weapon, out, model_objects, footprint, use_nodes, reference, prefix):
    """Textured perspective views plus a top view with the new footprint (red), the replaced station's footprint
    (yellow) and use nodes (green), and a 1.8 m figure marker for scale."""
    bpy, np, Vector, Matrix = weapon.bpy, weapon.np, weapon.Vector, weapon.Matrix
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.world.color = (.16, .16, .18)
    scene.view_settings.view_transform = 'Standard'
    scene.render.film_transparent = False
    points, lo, hi = weapon.bounds(model_objects)
    center = (lo + hi) / 2
    span = max(max(hi - lo), 1.9)

    def marker(name, vertices, faces, color, emission=0.0):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        shader = material.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = color
        shader.inputs['Emission Color'].default_value = color
        shader.inputs['Emission Strength'].default_value = emission
        mesh.materials.append(material)
        ob = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(ob)
        return ob

    def outline(name, polygon, z, width, color, emission=0.0):
        verts, faces = [], []
        for (x1, y1), (x2, y2) in zip(polygon, polygon[1:] + polygon[:1]):
            dx, dy = x2 - x1, y2 - y1
            length = math.hypot(dx, dy)
            nx, ny = -dy / length * width / 2, dx / length * width / 2
            base = len(verts)
            verts += [(x1 - nx, y1 - ny, z), (x1 + nx, y1 + ny, z), (x2 + nx, y2 + ny, z), (x2 - nx, y2 - ny, z)]
            faces.append((base, base + 1, base + 2, base + 3))
        return marker(name, verts, faces, color, emission)

    marker('ground', [(-6, -6, -0.004), (6, -6, -0.004), (6, 6, -0.004), (-6, 6, -0.004)], [(0, 1, 2, 3)], (.22, .23, .25, 1))
    outline('footprint', footprint, 0.004, 0.03, (1, .1, .1, 1), 3.0)
    if reference and reference.get('footprint'):
        outline('old_footprint', [tuple(p) for p in reference['footprint']], 0.006, 0.02, (1, .85, .1, 1), 3.0)
    for number, (x, y) in enumerate(use_nodes, 1):
        verts = [(x + dx * .06, y + dy * .06, z) for z, dz in ((0.0, 0), (.25, 1)) for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        marker(f'use{number}', verts, [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], (.1, 1, .2, 1), 3.0)
    figure_x, figure_y = use_nodes[0] if use_nodes else (center.x, lo.y - .5)
    verts = [(figure_x + dx * .05, figure_y + dy * .05, z) for z in (0.0, 1.8) for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    marker('figure_1_8m', verts, [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)], (.55, .6, .7, 1))
    for direction, strength in (((2, -2, 3), 3.0), ((-2, -1, 1.5), 1.2), ((1, 3, 2), 1.0)):
        bpy.ops.object.light_add(type='SUN', location=center + Vector(direction) * span)
        light = bpy.context.object
        light.data.energy = strength
        light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.object.camera_add()
    camera = bpy.context.object
    scene.camera = camera
    scene.render.resolution_x = scene.render.resolution_y = 720
    scene.render.image_settings.file_format = 'PNG'
    paths = []
    for index, angle in enumerate((35, 125, 215, 305)):
        a = math.radians(angle)
        camera.data.type = 'PERSP'
        camera.data.lens = 40
        camera.location = center + Vector((math.sin(a) * 1.7 * span, -math.cos(a) * 1.7 * span, .55 * span))
        camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
        path = out / f'review_{index}.png'
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        paths.append(path)
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = span * 1.5
    camera.location = Vector((center.x, center.y, span * 4))
    camera.rotation_euler = (0, 0, 0)
    camera.data.clip_end = span * 20
    path = out / 'review_top.png'
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    paths.append(path)
    return paths


def export_asset(weapon, item, repo, out, mode, helpers):
    """Inspect or export one placeable; returns the report dict."""
    bpy, np, Vector = weapon.bpy, weapon.np, weapon.Vector
    out.mkdir()
    doc, binary, cache, objects, source_sha = import_glb(weapon, item, out, mode)
    source = Path(item['_source'])
    surface_conversion = None
    if mode == 'export' and any(mat.get('doubleSided', False) for mat in doc['materials']):
        double_sided = {i for i, mat in enumerate(doc['materials']) if mat.get('doubleSided', False)}
        if item.get('double_sided_policy') == 'near_closed':
            surface_conversion = near_closed_report(weapon, objects, item.get('max_defect_fraction', DEFECT_FRACTION), item.get('max_opposed_fraction', OPPOSED_FRACTION))
        else:
            surface_conversion = weapon.closed_surface_report(objects, double_sided, len(doc['materials']))
        for mat in doc['materials']:
            mat['doubleSided'] = False
    points, lo, hi = weapon.bounds(objects)
    report = {'asset': item['asset'], 'source': str(source), 'sha256': source_sha, 'mode': mode,
              'coordinate_system': 'Blender imported world XYZ; glTF importer axis conversion already included',
              'source_bounds_min': list(lo), 'source_bounds_max': list(hi), 'dimensions': list(hi - lo),
              'triangles': sum(len(ob.data.loop_triangles) for ob in objects), 'surface_conversion': surface_conversion,
              'sampler_normalization': item.get('_sampler_normalization'),
              'components': weapon.component_reports(objects)[:12]}
    if mode == 'inspect':
        scene, camera, pts, center, span = weapon.render_setup(objects)
        views = [('view_px', (1, 0, 0), (0, 0, 1)), ('view_nx', (-1, 0, 0), (0, 0, 1)), ('view_py', (0, 1, 0), (0, 0, 1)),
                 ('view_ny', (0, -1, 0), (0, 0, 1)), ('view_top', (0, 0, 1), (0, 1, 0))]
        paths = []
        for name, direction, up in views:
            path = out / (name + '.png')
            weapon.render_view(scene, camera, pts, center, span, direction, up, path, (600, 600))
            paths.append(path)
        weapon.contact_sheet(paths, out / 'views.png', 300)
        (out / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
        return report

    fit, rows, scale, translation = fit_transform(weapon, item, objects)
    for ob in objects:
        ob.matrix_world = fit @ ob.matrix_world
    points, lo, hi = weapon.bounds(objects)
    size = [hi[i] - lo[i] for i in range(3)]
    if any(size[i] > item['max_size'][i] + 1e-6 for i in range(3)):
        raise ValueError(f"{item['asset']}: fitted size {[round(v, 3) for v in size]} exceeds max_size {item['max_size']}")
    if abs(lo.z) > 1e-5:
        raise ValueError('Fitted model must rest on Z=0')
    resources = out / 'resources'
    walkmesh = out / 'walkmesh'
    resources.mkdir()
    walkmesh.mkdir()
    texture, material_count = item['texture'], len(doc['materials'])
    names, previews, mapdims, material_dims, mesh_data, mesh_names, removed = {}, {}, {}, {}, [], [], 0
    for ob in objects:
        exported, repaired = repair_exported_normals(weapon, fit, doc, binary, helpers.export_mesh(ob.data, ob.matrix_world))
        item['_zero_normals_repaired'] = item.get('_zero_normals_repaired', 0) + repaired
        for slot in sorted({t.material_index for t in ob.data.loop_triangles}):
            index = weapon.imported_material_index(ob.data.materials[slot], material_count)
            if index not in names:
                name = texture if material_count == 1 else texture + f'_{index}'
                dims, maps = material_maps(weapon, item, doc, binary, index, source, cache, resources, name)
                names[index], previews[index] = name, weapon.preview_material(resources, name, maps)
                mapdims.update(dims)
                material_dims[index] = dims
            mesh, zero = weapon.compact_subset(exported, ob.data.loop_triangles, slot)
            removed += zero
            mesh_data.append(mesh)
            mesh_names.append(names[index])
    merged = {}
    for mesh, name in zip(mesh_data, mesh_names):
        target = merged.setdefault(name, ([], [], [], []))
        offset = len(target[0])
        target[0].extend(mesh[0]); target[1].extend(mesh[1]); target[3].extend(mesh[3])
        target[2].extend(tuple(i + offset for i in f) for f in mesh[2])
    meshes = list(merged.items())
    model, short = item['model'], item['short']
    basis_config = {'orientation': rows, 'scale': scale, 'source_grip': [0, 0, 0], 'target_grip': translation, 'sha256': source_sha}
    normal_helper = weapon.load_module_snapshot('trellis_weapon_normals', Path(weapon.__file__).with_name('TrellisWeaponNormals.py'))
    report['source_normal_basis'] = normal_helper.source_basis(doc, binary, basis_config, names, out / 'source_basis.npz')
    (resources / (model + '.mdl')).write_text(write_mdl_text(model, short, meshes, list(lo), list(hi)), encoding='ascii')
    # Footprint: the convex hull of the whole model in plan, shrunk by `inset`, flat at the ground plane.
    plan = [(v[0], v[1]) for _, mesh in meshes for v in mesh[0]]
    walk = item['walkmesh']
    footprint = inset_polygon(simplify_convex(convex_hull(plan), walk.get('simplify', .05)), walk.get('inset', 0))
    use_nodes = [tuple(n) for n in walk['use_nodes']]
    clearance = walk.get('min_use_clearance', .1)
    for node in use_nodes:
        distance = signed_distance(node, footprint)
        if distance < clearance:
            raise ValueError(f"{item['asset']}: use node {node} is {distance:.3f} m from the footprint; needs >= {clearance} m outside")
    # LF endings, like the other SWLOR walkmeshes (Path.write_text would write CRLF on Windows).
    (walkmesh / (model + '.pwk')).write_bytes(build_pwk(model, footprint, use_nodes, walk['prefix']).encode('ascii'))
    old_objects = objects
    preview_objects = [weapon.mesh_preview(mesh, f'{model}_{i}', previews[next(k for k, v in names.items() if v == name)])
                       for i, (name, mesh) in enumerate(meshes)]
    for ob in old_objects:
        bpy.data.objects.remove(ob, do_unlink=True)
    reference = item.get('reference') or {}
    paths = review_renders(weapon, out, preview_objects, footprint, use_nodes, reference, walk['prefix'])
    weapon.contact_sheet(paths[:4], out / 'review_sheet.png', 360)
    report.update(model=model, bounds_min=list(lo), bounds_max=list(hi), size=size, scale=scale, orientation=rows,
                  export_triangles=sum(len(m[2]) for _, m in meshes), export_vertices=sum(len(m[0]) for _, m in meshes),
                  materials_exported=names, map_dimensions=mapdims, removed_exact_zero_area_triangles=removed,
                  footprint=footprint, footprint_area=polygon_area(footprint), use_nodes=use_nodes,
                  use_node_clearance=[signed_distance(n, footprint) for n in use_nodes], reference=reference,
                  max_size=item['max_size'], emission=item.get('emission'), normal_z_snapped_texels=item.get('_normal_z_snapped'),
                  zero_normals_repaired=item.get('_zero_normals_repaired', 0))
    report['resources_sha256'] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in resources.iterdir()}
    report['walkmesh_sha256'] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in walkmesh.iterdir()}
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'review.blend'))
    (out / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    return report


# ---------------------------------------------------------------------------
# Package stage (plain Python): compile, reproject normals, compress, then validate and add the PWKs.
# ---------------------------------------------------------------------------

def parse_pwk(text):
    """Return (footprint vertices, face surface materials, [(use node name, x, y)]) from an ASCII PWK."""
    lines = [line.split() for line in text.splitlines()]
    vi = next(i for i, line in enumerate(lines) if line[:1] == ['verts'])
    fi = next(i for i, line in enumerate(lines) if line[:1] == ['faces'])
    verts = [tuple(map(float, line)) for line in lines[vi + 1:vi + 1 + int(lines[vi][1])]]
    materials = [int(line[-1]) for line in lines[fi + 1:fi + 1 + int(lines[fi][1])]]
    uses, current = [], None
    for line in lines:
        if line[:2] == ['node', 'dummy']:
            current = line[2]
        elif line[:1] == ['position'] and current is not None:
            uses.append((current, float(line[1]), float(line[2])))
            current = None
    return verts, materials, uses


def validate_pwk(path, model, use_nodes=None):
    verts, materials, uses = parse_pwk(path.read_text())
    if not verts or set(materials) != {SURFACE_NONWALK}:
        raise ValueError('PWK footprint must be nonwalkable surface 7: ' + path.name)
    if any(abs(v[2]) > 1e-9 for v in verts):
        raise ValueError('PWK footprint must lie on Z=0: ' + path.name)
    if f'parent {model}_pwk' not in path.read_text():
        raise ValueError('PWK parent must be <model>_pwk: ' + path.name)
    footprint = [(v[0], v[1]) for v in verts]
    for name, x, y in uses:
        if not re.fullmatch(r'.{4}pwk_use0[12]', name) or (x == 0 and y == 0):
            raise ValueError(f'PWK use node is not engine-readable: {name}')
        if signed_distance((x, y), footprint) <= 0:
            raise ValueError(f'PWK use node {name} lies inside the footprint')
    if use_nodes is not None and len(uses) != use_nodes:
        raise ValueError(f'PWK has {len(uses)} use nodes; the export recorded {use_nodes}: {path.name}')
    return {'vertices': len(verts), 'use_nodes': [(n, x, y) for n, x, y in uses]}


def package(args, repo, out):
    weapon = load_module('trellis_weapon_export', repo / 'tools/TrellisWeaponExport.py')
    export = args.export.resolve()
    manifest = json.loads((export / 'export_manifest.json').read_bytes())
    pwks = {}
    for relative, digest in manifest.get('walkmeshes', {}).items():
        path = export / relative
        if not path.resolve().is_relative_to(export) or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
            raise ValueError('Walkmesh changed or escapes the export: ' + relative)
        pwks[path.stem] = path
    if not pwks:
        raise ValueError('Export has no walkmeshes; re-export')
    weapon.package(args, repo, out)
    runtime = out / 'runtime_resources'
    checks = {}
    for model, path in sorted(pwks.items()):
        if not (runtime / (model + '.mdl')).is_file():
            raise ValueError('Walkmesh has no compiled model: ' + model)
        checks[model] = validate_pwk(path, model, manifest.get('walkmesh_use_nodes', {}).get(model))
        (runtime / path.name).write_bytes(path.read_bytes())
    report_path = out / 'package_report.json'
    report = json.loads(report_path.read_text())
    report['walkmeshes'] = {name: {**checks[name], 'sha256': hashlib.sha256((runtime / (name + '.pwk')).read_bytes()).hexdigest()} for name in checks}
    report['resources'] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in runtime.iterdir()}
    report_path.write_text(json.dumps(report, indent=2) + '\n')
    print('PLACEABLE_PACKAGE_COMPLETE ' + str(runtime))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--manifest', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--mode', choices=['inspect', 'export', 'package'], required=True)
    parser.add_argument('--export', type=Path, help='Recorded export directory for package mode')
    parser.add_argument('--crunch', type=Path)
    parser.add_argument('--compiler', type=Path, help='Pinned CleanModels executable; defaults to CLEANMODELS or the local install')
    parser.add_argument('--allow-existing', action='store_true', help='Re-export models whose resource names are already in the haks')
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else sys.argv[1:]
    args = parser.parse_args(argv)
    repo, out = args.repo.resolve(), args.output.resolve()
    if out.is_relative_to(repo):
        raise ValueError('Keep staging output outside the repository')
    if out.exists() and any(out.iterdir()):
        raise ValueError('Output directory must be empty')
    if args.mode == 'package':
        if args.export is None or args.crunch is None:
            parser.error('package requires --export and --crunch')
        out.mkdir(parents=True, exist_ok=True)
        package(args, repo, out)
        return
    if args.manifest is None:
        parser.error('inspect/export requires --manifest')
    manifest = args.manifest.resolve()
    config, manifest_sha = load_manifest(manifest)
    taken = haks_resource_names(repo) if args.mode == 'export' and not args.allow_existing else None
    validate_manifest(config, manifest, taken, args.mode == 'inspect')
    weapon = load_module('trellis_weapon_export', repo / 'tools/TrellisWeaponExport.py')
    weapon.load_blender()
    helpers = load_module('placeable_export_helpers', repo / 'tools/ImportBlasterModel.py')
    out.mkdir(parents=True, exist_ok=True)
    reports, previews = [], []
    for i, item in enumerate(config['assets']):
        folder = out / f"{i + 1:02d}_{item['asset']}"
        try:
            reports.append(export_asset(weapon, item, repo, folder, args.mode, helpers))
        except ValueError as error:
            raise ValueError(f"{item['asset']}: {error}") from error
        previews.append(folder / ('views.png' if args.mode == 'inspect' else 'review_0.png'))
    weapon.contact_sheet(previews, out / 'contact_sheet.png', 360)
    (out / 'reports.json').write_text(json.dumps(reports, indent=2) + '\n')
    if args.mode == 'export':
        resources = {p.relative_to(out).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in out.glob('*/resources/*')}
        walkmeshes = {p.relative_to(out).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in out.glob('*/walkmesh/*')}
        normal_bases = [{'asset': item['asset'], 'file': f"{i + 1:02d}_{item['asset']}/source_basis.npz",
                         **report['source_normal_basis'], 'models': [item['model']],
                         'normal_basis_policy': item.get('normal_basis_policy', 'reproject'),
                         'authored_angle_limit_degrees': item.get('authored_angle_limit_degrees', .5),
                         'aliases': []} for i, (item, report) in enumerate(zip(config['assets'], reports))]
        (out / 'export_manifest.json').write_text(json.dumps({
            'schema_version': 1, 'source_manifest_sha256': manifest_sha, 'sources': {i['asset']: i['sha256'] for i in config['assets']},
            'resources': resources, 'walkmeshes': walkmeshes,
            'walkmesh_use_nodes': {i['model']: len(i['walkmesh']['use_nodes']) for i in config['assets']},
            'normal_bases': normal_bases}, indent=2) + '\n')
    print('PLACEABLE_TOOL_COMPLETE ' + str(out))


if __name__ == '__main__':
    main()
