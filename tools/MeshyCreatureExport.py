"""Turn a textured Meshy (glTF 2) creature into an NWN skinned creature model that plays an existing skeleton's animations.

Blender 4.0: blender -b --factory-startup --python-exit-code 1 --python tools/MeshyCreatureExport.py --
  --mode export --manifest PATH --output EMPTY
Python:      python -B tools/MeshyCreatureExport.py --mode package --export PATH --crunch nwn_crunch.exe --output EMPTY

The new model keeps every node of a reference creature (an ASCII decompile of the model the creature used before, or of
its stock counterpart) under the reference's names, so the reference's supermodel animations drive it. The reference's
rigid body-part meshes are dropped and replaced by one skin mesh made from the Meshy source:
  1. The source is turned so it faces +Y (NWN creature forward) and scaled uniformly to the reference's height. The
     reference's joints then spread in X and Y about the animated root until its segments span the source, so the
     source keeps its proportions and its legs, arms, head and tail sit on the matching segments.
  2. Each source vertex takes the bone of the nearest reference segment, blended with that bone's parent and children
     over `blend` meters, at most four bones a vertex.
Only the root dummy carries position keys in the stock creature animation sets checked so far (c_wolf, c_direwolf, a_da
via a_ba), so the bind pose comes from the reference and the source only has to match its silhouette.

Manifest (schema 1, paths relative to the manifest):
  creatures: [{model (16 chars), source (GLB), sha256, reference (ASCII MDL), supermodel, texture (<= 13 chars),
               texture_size (512 or 1024), yaw_degrees (turn applied to the imported source, default 180: Meshy faces -Y),
               blend (meters, default 0.08), bone_map {segment: bone} for reference meshes that are not animated bones,
               fit: skeleton (default; uniform source scale, joints spread in X/Y to the mesh) or mesh (source stretched
               per axis onto the reference bounds), anchor (the animated root node, default the *rootdummy node),
               spread_from {x, y}: bounds (default) or feet (the paw stance; use y feet for long-tailed quadrupeds),
               emission {threshold, softness, saturation_min?, gain?}: a glow map from the brightest (optionally
               saturated) base-color texels, as TrellisPlaceableExport does,
               regions [{axis, below, bones}]: vertices with a fitted coordinate below `below` (meters, model space) may only
               take these bones, for parts that sit beside the wrong segment (a tail hanging beside the hind legs)}]
Output per creature: resources/ (ASCII MDL, MTR, TGA maps), review_side.png, review_front.png and report.json. Package output is compiled MDL,
MTR and DDS only. Verify in the client; nothing here deploys or edits module data.
"""
from __future__ import annotations

import argparse, hashlib, importlib.util, json, math, re, shutil, struct, subprocess, sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
MAX_BONES_PER_VERTEX = 4


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


# ---------------------------------------------------------------------------------------------------------------------
# Reference skeleton (ASCII MDL geometry section)
# ---------------------------------------------------------------------------------------------------------------------

def parse_reference(text):
    """Nodes of the geometry section in file order: name, type, parent, position, orientation (axis-angle), and for
    meshes their verts and faces in node space."""
    geometry = text.split('newanim', 1)[0]
    nodes, current, block, count = [], None, None, 0
    for raw in geometry.splitlines():
        parts = raw.split()
        if not parts:
            continue
        if count:
            if block == 'verts':
                current['verts'].append(tuple(float(x) for x in parts[:3]))
            elif block == 'faces':
                current['faces'].append(tuple(int(x) for x in parts[:3]))
            count -= 1
            continue
        key = parts[0].lower()
        if key == 'node':
            current = {'type': parts[1].lower(), 'name': parts[2], 'parent': None, 'position': (0.0, 0.0, 0.0),
                       'orientation': (0.0, 0.0, 1.0, 0.0), 'verts': [], 'faces': []}
        elif current is None:
            continue
        elif key == 'endnode':
            nodes.append(current)
            current = None
        elif key == 'parent':
            current['parent'] = None if parts[1].upper() == 'NULL' else parts[1]
        elif key == 'position':
            current['position'] = tuple(float(x) for x in parts[1:4])
        elif key == 'orientation':
            current['orientation'] = tuple(float(x) for x in parts[1:5])
        elif key in ('verts', 'faces', 'tverts', 'normals', 'colors', 'tangents', 'weights', 'constraints') and len(parts) > 1:
            block, count = key, int(parts[1])
    if not nodes or nodes[0]['parent'] is not None:
        raise ValueError('Reference must start with its root dummy')
    return nodes


# ---------------------------------------------------------------------------------------------------------------------
# ASCII output
# ---------------------------------------------------------------------------------------------------------------------

def fmt(values):
    return ' '.join(f'{v:.7g}' for v in values)


def write_mdl(model, supermodel, nodes, root_name, mesh, texture):
    """Reference nodes become dummies under the new names' root; one skin carries the geometry."""
    verts, normals, uvs, faces, weights = mesh
    if not verts or not faces or max(len(verts), len(faces)) > 65535:
        raise ValueError('Empty mesh or NWN vertex/face limit exceeded')
    if not (len(verts) == len(normals) == len(uvs) == len(weights)):
        raise ValueError('Every vertex needs a normal, a UV and weights')
    if any(not math.isfinite(c) for row in verts + normals + uvs for c in row):
        raise ValueError('Non-finite vertex data')
    if any(abs(sum(c * c for c in n) - 1) > .001 for n in normals):
        raise ValueError('Every vertex needs a unit normal')
    lines = ['#MAXMODEL ASCII', f'newmodel {model}', f'setsupermodel {model} {supermodel}', 'classification CHARACTER',
             'setanimationscale 1', f'beginmodelgeom {model}']
    for node in nodes:
        name = model if node['name'] == root_name else node['name']
        parent = 'NULL' if node['parent'] is None else (model if node['parent'] == root_name else node['parent'])
        lines += [f'node dummy {name}', f'  parent {parent}']
        if node['parent'] is not None:
            lines += [f"  position {fmt(node['position'])}", f"  orientation {fmt(node['orientation'])}"]
        lines += ['endnode']
    lines += [f'node skin {model}_skin', f'  parent {model}', '  position 0 0 0', '  orientation 0 0 1 0',
              '  ambient 1 1 1', '  diffuse 1 1 1', '  specular 0 0 0', '  shininess 1', '  shadow 1', '  render 1',
              '  tilefade 0', f'  bitmap {texture}', f'  materialname {texture}', '  renderhint NormalAndSpecMapped',
              f'  verts {len(verts)}']
    lines += ['    ' + fmt(v) for v in verts]
    lines += [f'  normals {len(normals)}'] + ['    ' + fmt(n) for n in normals]
    lines += [f'  tverts {len(uvs)}'] + [f'    {u:.7g} {v:.7g} 0' for u, v in uvs]
    lines += [f'  faces {len(faces)}'] + [f'    {a} {b} {c} 1 {a} {b} {c} 0' for a, b, c in faces]
    lines += [f'  weights {len(weights)}'] + ['    ' + ' '.join(f'{bone} {w:.6f}' for bone, w in row) for row in weights]
    lines += ['endnode', f'endmodelgeom {model}', f'donemodel {model}']
    return '\n'.join(lines) + '\n'


# ---------------------------------------------------------------------------------------------------------------------
# Blender export
# ---------------------------------------------------------------------------------------------------------------------

def world_matrices(nodes, Matrix, Quaternion, Vector):
    by_name, world = {n['name']: n for n in nodes}, {}

    def resolve(name):
        if name not in world:
            node = by_name[name]
            x, y, z, angle = node['orientation']
            axis = Vector((x, y, z))
            rotation = Quaternion(axis.normalized(), angle).to_matrix().to_4x4() if axis.length > 1e-9 else Matrix.Identity(4)
            local = Matrix.Translation(Vector(node['position'])) @ rotation
            world[name] = (resolve(node['parent']) if node['parent'] else Matrix.Identity(4)) @ local
        return world[name]

    for node in nodes:
        resolve(node['name'])
    return world


def import_source(bpy, path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if len(meshes) != 1:
        raise ValueError(f'Expected one source mesh, found {len(meshes)}')
    obj = meshes[0]
    obj.data.transform(obj.matrix_world)
    obj.matrix_world.identity()
    if len(obj.data.materials) != 1 or len(obj.data.uv_layers) != 1:
        raise ValueError('Expected one material and one UV map')
    return obj


def material_maps(weapon, source, cache, texture, limit):
    """Base color, normal, specularity and roughness maps read from the GLB's own material (factors applied, packed
    metallic-roughness channels B and G), the same conversion the TRELLIS weapon and placeable exporters use."""
    np = weapon.np
    doc, binary = weapon.glb_document(source)
    if len(doc.get('materials', [])) != 1:
        raise ValueError('Expected one source material')
    mat = doc['materials'][0]
    weapon.validate_material(mat, allow_double_sided=True)
    if mat.get('occlusionTexture') or mat.get('emissiveTexture'):
        raise ValueError('Occlusion and emissive source maps need explicit conversion')
    cache.mkdir(parents=True, exist_ok=True)
    pbr = mat.get('pbrMetallicRoughness', {})
    factor = np.asarray(pbr.get('baseColorFactor', [1, 1, 1, 1]))
    if 'baseColorTexture' not in pbr:
        raise ValueError('Source material has no base color texture')
    base = weapon.read_image(doc, binary, pbr['baseColorTexture'], source, cache)
    maps = {texture: weapon.srgb(weapon.resize(weapon.linear(base[..., :3]) * factor[:3], limit))}
    if 'normalTexture' in mat:
        info = mat['normalTexture']
        raw = weapon.read_image(doc, binary, info, source, cache)
        maps[texture + '_n'] = weapon.prepare_normal_map(raw, info.get('scale', 1), limit) * .5 + .5
    else:
        maps[texture + '_n'] = np.broadcast_to(np.array([.5, .5, 1], np.float32), (4, 4, 3))
    scalar = limit // 2
    if 'metallicRoughnessTexture' in pbr:
        mr = weapon.read_image(doc, binary, pbr['metallicRoughnessTexture'], source, cache)
        metal = weapon.resize(mr[..., 2:3], scalar)[..., 0] * pbr.get('metallicFactor', 1)
        rough = weapon.resize(mr[..., 1:2], scalar)[..., 0] * pbr.get('roughnessFactor', 1)
    else:
        metal = np.full((4, 4), pbr.get('metallicFactor', 1), np.float32)
        rough = np.full((4, 4), pbr.get('roughnessFactor', 1), np.float32)
    maps[texture + '_s'] = weapon.metallic_to_specularity(np.clip(metal, 0, 1))
    maps[texture + '_r'] = np.clip(rough, 0, 1)
    return maps


def foot_stance(np, points, band=.08):
    """Stance of the points within `band` of the height above the lowest one: mean lateral foot offset, the distance
    between the front and back foot groups (split at the middle of their Y range), and the Y midpoint between them."""
    height = points[:, 2].max() - points[:, 2].min()
    low = points[points[:, 2] < points[:, 2].min() + band * height]
    middle = (low[:, 1].min() + low[:, 1].max()) / 2
    front, back = low[low[:, 1] >= middle, 1].mean(), low[low[:, 1] < middle, 1].mean()
    width = np.abs(low[:, 0] - (low[:, 0].min() + low[:, 0].max()) / 2).mean()
    return width, front - back, (front + back) / 2


def export_creature(item, manifest_dir, out, weapon):
    bpy, np, Matrix, Quaternion, Vector = weapon.bpy, weapon.np, weapon.Matrix, weapon.Quaternion, weapon.Vector
    from mathutils.bvhtree import BVHTree
    source = manifest_dir / item['source']
    if hashlib.sha256(source.read_bytes()).hexdigest() != item['sha256']:
        raise ValueError('Source GLB does not match its sha256')
    reference_text = (manifest_dir / item['reference']).read_text(encoding='latin-1')
    nodes = parse_reference(reference_text)
    root_name = nodes[0]['name']
    world = world_matrices(nodes, Matrix, Quaternion, Vector)
    parents = {n['name']: n['parent'] for n in nodes}
    bone_map = {k.lower(): v for k, v in item.get('bone_map', {}).items()}

    # Reference segments in model space, each owned by a bone.
    seg_verts, seg_faces, seg_bone = [], [], []
    for node in nodes:
        if node['type'] not in ('trimesh', 'skin') or not node['faces']:
            continue
        bone = bone_map.get(node['name'].lower(), node['name'])
        base = len(seg_verts)
        seg_verts += [world[node['name']] @ Vector(v) for v in node['verts']]
        seg_faces += [tuple(base + i for i in f) for f in node['faces']]
        seg_bone += [bone] * len(node['faces'])
    if not seg_faces:
        raise ValueError('Reference has no body-part meshes to fit against')
    bones = sorted(set(seg_bone))
    missing = [b for b in bones if b not in parents]
    if missing:
        raise ValueError('bone_map names unknown nodes: ' + ', '.join(missing))
    ref = np.array([tuple(v) for v in seg_verts])
    ref_lo, ref_hi = ref.min(0), ref.max(0)

    obj = import_source(bpy, source)
    mesh = obj.data
    mesh.transform(Matrix.Rotation(math.radians(item.get('yaw_degrees', 180)), 4, 'Z'))
    src = np.array([tuple(v.co) for v in mesh.vertices])
    lo, hi = src.min(0), src.max(0)
    fit = item.get('fit', 'skeleton')
    if fit == 'skeleton':
        # Keep the source's proportions (uniform scale to the reference height) and move the joints instead: every node
        # below the animated anchor spreads in X and Y about the anchor until the segments span the mesh. Heights stay,
        # because the anchor's animated position keys put the body at the reference's height.
        scale = np.full(3, (ref_hi[2] - ref_lo[2]) / (hi[2] - lo[2]))
        spread = np.array([*((hi - lo)[:2] * scale[:2] / (ref_hi - ref_lo)[:2]), 1.0])
        feet = item.get('spread_from', {})
        src_feet, ref_feet = foot_stance(np, (src - lo) * scale), foot_stance(np, ref - ref_lo)
        for axis in (0, 1):
            if feet.get('xy'[axis], 'bounds') == 'feet':
                spread[axis] = src_feet[axis] / ref_feet[axis]
            elif feet.get('xy'[axis], 'bounds') != 'bounds':
                raise ValueError('spread_from values are bounds or feet')
        anchor_name = item.get('anchor') or next(n['name'] for n in nodes if n['name'].lower().endswith('rootdummy'))
        anchor = np.array(tuple(world[anchor_name].translation))
        moved = {}
        for node in nodes:
            position = np.array(tuple(world[node['name']].translation))
            moved[node['name']] = position if node['name'] in (root_name, anchor_name) else anchor + spread * (position - anchor)
        for node in nodes:
            if node['parent'] is not None:
                rotation = world[node['parent']].to_3x3()
                local = rotation.transposed() @ Vector(moved[node['name']] - moved[node['parent']])
                node['position'] = tuple(local)
        seg_verts = [Vector(anchor + spread * (np.array(tuple(v)) - anchor)) for v in seg_verts]
        ref = np.array([tuple(v) for v in seg_verts])
        ref_lo, ref_hi = ref.min(0), ref.max(0)
    elif fit == 'mesh':
        scale = (ref_hi - ref_lo) / (hi - lo)  # stretch the source onto the reference bounds per axis
        spread = np.ones(3)
    else:
        raise ValueError('fit must be skeleton or mesh')
    center_ref, center_src = (ref_lo + ref_hi) / 2, (lo + hi) / 2
    if fit == 'skeleton' and item.get('spread_from', {}).get('y') == 'feet':
        # Line the paws up instead of the bounds, so a long tail or muzzle does not shift the legs.
        center_ref[1] = foot_stance(np, ref)[2]
        center_src[1] = foot_stance(np, src)[2]
    fitted = (src - center_src) * scale + center_ref
    fitted[:, 2] = (src[:, 2] - lo[2]) * scale[2] + ref_lo[2]  # keep the feet on the reference's floor
    for v, co in zip(mesh.vertices, fitted):
        v.co = Vector(co)
    mesh.update()

    # Nearest reference segment per vertex, then blend with that bone's family.
    whole = BVHTree.FromPolygons(seg_verts, seg_faces)
    per_bone = {}
    for bone in bones:
        idx = [i for i, b in enumerate(seg_bone) if b == bone]
        per_bone[bone] = BVHTree.FromPolygons(seg_verts, [seg_faces[i] for i in idx])
    children = {b: [c for c in bones if parents.get(c) == b] for b in bones}

    def skin_parent(bone):
        parent = parents.get(bone)
        while parent is not None and parent not in per_bone:
            parent = parents.get(parent)
        return parent

    regions = item.get('regions', [])
    for region in regions:
        if region['axis'] not in 'xyz' or set(region['bones']) - set(bones):
            raise ValueError('Region needs axis x, y or z and skinned bones only')

    blend = item.get('blend', .08)
    vertex_weights = []
    for v in mesh.vertices:
        allowed = None
        for region in regions:
            if v.co['xyz'.index(region['axis'])] < region['below']:
                allowed = set(region['bones'])
                break
        if allowed:
            primary = min(allowed, key=lambda b: per_bone[b].find_nearest(v.co)[3])
        else:
            primary = seg_bone[whole.find_nearest(v.co)[2]]
        family = {primary, *children[primary]}
        if skin_parent(primary):
            family.add(skin_parent(primary))
        if allowed:
            family &= allowed
        distances = {b: per_bone[b].find_nearest(v.co)[3] for b in family}
        nearest = min(distances.values())
        raw = {b: max(0.0, 1 - (d - nearest) / blend) ** 2 for b, d in distances.items()}
        top = sorted(((w, b) for b, w in raw.items() if w > 1e-3), reverse=True)[:MAX_BONES_PER_VERTEX]
        total = sum(w for w, _ in top)
        vertex_weights.append([(b, w / total) for w, b in top])

    # Split vertices by UV and normal (loop data), as the engine stores one UV and normal per vertex.
    mesh.calc_loop_triangles()
    uv_layer = mesh.uv_layers[0].data
    mesh.calc_normals_split()  # Blender 4.0
    corner_normals = [tuple(l.normal) for l in mesh.loops]
    key_index, verts, normals, uvs, weights, faces = {}, [], [], [], [], []
    for tri in mesh.loop_triangles:
        corners = []
        for loop in tri.loops:
            vi = mesh.loops[loop].vertex_index
            n = Vector(corner_normals[loop]).normalized()
            uv = tuple(uv_layer[loop].uv)
            key = (vi, round(uv[0], 6), round(uv[1], 6), round(n.x, 4), round(n.y, 4), round(n.z, 4))
            if key not in key_index:
                key_index[key] = len(verts)
                verts.append(tuple(mesh.vertices[vi].co))
                normals.append(tuple(n))
                uvs.append(uv)
                weights.append(vertex_weights[vi])
            corners.append(key_index[key])
        if len(set(corners)) == 3:
            faces.append(tuple(corners))

    resources = out / 'resources'
    resources.mkdir(parents=True)
    texture = item['texture']
    (resources / (item['model'] + '.mdl')).write_text(
        write_mdl(item['model'], item['supermodel'], nodes, root_name, (verts, normals, uvs, faces, weights), texture),
        encoding='ascii')

    maps = material_maps(weapon, source, out / 'image_cache', texture, item['texture_size'])
    base = maps[texture]
    emission = item.get('emission')
    if emission:
        placeable = load_module('trellis_placeable_export', REPO / 'tools/TrellisPlaceableExport.py')
        mask = placeable.emission_mask(np.asarray(base), emission['threshold'], emission['softness'], np,
                                       emission.get('saturation_min'))
        if float(mask.max()) <= 0:
            raise ValueError('Emission settings select no texels')
        maps[texture + '_e'] = np.clip(np.asarray(base) * mask[..., None] * emission.get('gain', 1), 0, 1)
    for name, pixels in maps.items():
        weapon.save_texture(resources / (name + '.tga'), np.asarray(pixels))
    mtr = f'renderhint NormalAndSpecMapped\ntexture0 {texture}\ntexture1 {texture}_n\ntexture2 {texture}_s\ntexture3 {texture}_r\n'
    if emission:
        mtr += f'texture5 {texture}_e\n'
    (resources / (texture + '.mtr')).write_text(mtr, encoding='ascii')

    usage = {b: 0 for b in bones}
    for row in vertex_weights:
        usage[row[0][0]] += 1
    report = {'model': item['model'], 'source_sha256': item['sha256'], 'reference': item['reference'],
              'supermodel': item['supermodel'], 'vertices': len(verts), 'faces': len(faces),
              'scale': round(float(scale[0]), 4), 'joint_spread': [round(float(x), 4) for x in spread], 'reference_bounds': [ref_lo.tolist(), ref_hi.tolist()],
              'primary_bone_vertices': usage}
    (out / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    review(bpy, Vector, obj, seg_verts, seg_faces, out / 'review.png')
    return report


def review(bpy, Vector, obj, seg_verts, seg_faces, path):
    """Side and front views of the fitted source (textured) with the fitted reference segments drawn over it."""
    ref_mesh = bpy.data.meshes.new('reference')
    ref_mesh.from_pydata([tuple(v) for v in seg_verts], [], seg_faces)
    ref = bpy.data.objects.new('reference', ref_mesh)
    bpy.context.scene.collection.objects.link(ref)
    ref.modifiers.new('wire', 'WIREFRAME').thickness = .004
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x, scene.render.resolution_y = 900, 900
    scene.world = bpy.data.worlds.new('w')
    scene.world.color = (.35, .35, .38)
    scene.view_settings.view_transform = 'Standard'
    light = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN'))
    light.data.energy = 3
    light.rotation_euler = (.7, .2, .8)
    scene.collection.objects.link(light)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
    cam.data.type = 'ORTHO'
    scene.collection.objects.link(cam)
    scene.camera = cam
    pts = [v.co for v in obj.data.vertices] + list(seg_verts)
    lo = Vector([min(p[i] for p in pts) for i in range(3)])
    hi = Vector([max(p[i] for p in pts) for i in range(3)])
    center = (lo + hi) / 2
    cam.data.ortho_scale = max(hi - lo) * 1.1
    for label, rotation, offset in (('side', (math.pi / 2, 0, math.pi / 2), Vector((10, 0, 0))),
                                    ('front', (math.pi / 2, 0, math.pi), Vector((0, 10, 0)))):
        cam.location = center + offset
        cam.rotation_euler = rotation
        scene.render.filepath = str(path.with_name(f'{path.stem}_{label}.png'))
        bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------------------------------------------------------------
# Package (ordinary Python): compile the MDL with CleanModels and the maps with NWN Crunch
# ---------------------------------------------------------------------------------------------------------------------

CRUNCH_OPTIONS = {
    'color': ['-dropEmptyAlpha', '-DXT1', '-gamma', '2.2'],
    'normal': ['-dropEmptyAlpha', '-DXN', '-normalize', '-renormalize', '-uniformMetrics', '-gamma', '1.0'],
    'scalar': ['-dropEmptyAlpha', '-DXT5A', '-setAtoY', '-gamma', '1.0'],
}


def compiler_path(explicit):
    import os
    if explicit:
        return Path(explicit)
    if os.environ.get('CLEANMODELS'):
        return Path(os.environ['CLEANMODELS'])
    return Path(os.environ['LOCALAPPDATA']) / 'SWLOR/tools/cleanmodels-qt/1.1.7/cleanmodels-qt/cleanmodels.exe'


def flatten_resources(export):
    """Every exported resource by name, and each texture's role from the MTR slot that binds it."""
    # Flatten the per-creature folders: a name two creatures share must hold the same bytes, or one would replace the other.
    sources = {}
    for path in sorted(export.glob('*/resources/*')):
        if path.name.lower() in sources and sources[path.name.lower()].read_bytes() != path.read_bytes():
            raise ValueError(f'{path.name} differs between {sources[path.name.lower()].parent.parent.name} and '
                             f'{path.parent.parent.name}')
        sources.setdefault(path.name.lower(), path)
    # Texture roles come from the MTR slots that bind them, never from the file name.
    slot_roles = {'0': 'color', '1': 'normal', '2': 'scalar', '3': 'scalar', '5': 'color'}
    roles = {}
    for path in sources.values():
        if path.suffix == '.mtr':
            for slot, name in re.findall(r'^texture(\d+)\s+(\S+)', path.read_text(), re.M):
                role = slot_roles.get(slot)
                if role is None or roles.setdefault(name.lower(), role) != role:
                    raise ValueError(f'{path.name}: texture{slot} {name} has no single known role')
    return sources, roles


def package(args, out):
    export = args.export.resolve()
    ascii_dir, runtime, logs = out / 'ascii', out / 'runtime_resources', out / 'logs'
    for d in (ascii_dir, runtime, logs):
        d.mkdir(parents=True)
    sources, roles = flatten_resources(export)
    for path in sources.values():
        if path.suffix in ('.mdl', '.mtr'):
            shutil.copy2(path, ascii_dir / path.name)
            if path.suffix == '.mtr':
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
                                 '-yflip', '-dxtQuality', 'uber'] + CRUNCH_OPTIONS[role], capture_output=True, text=True, timeout=300)
        (logs / (path.stem + '.log')).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or not target.is_file():
            raise ValueError('Crunch failed ' + path.name)
    compiled = out / 'compiled'
    compiled.mkdir()
    shutil.copytree(runtime, ascii_dir, dirs_exist_ok=True)
    result = subprocess.run([str(compiler_path(args.compiler)), 'compile', '--overwrite', str(ascii_dir), str(compiled)],
                            capture_output=True, text=True, timeout=600)
    (logs / 'compile.log').write_text(result.stdout + result.stderr)
    models = sorted(compiled.glob('*.mdl'))
    if result.returncode != 0 or len(models) != len(list(ascii_dir.glob('*.mdl'))):
        raise ValueError('Compile failed; see ' + str(logs / 'compile.log'))
    for model in models:
        data = model.read_bytes()
        if data[:4] != b'\0\0\0\0':
            raise ValueError('Not a binary model: ' + model.name)
        shutil.copy2(model, runtime / model.name)
    report = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(runtime.iterdir())}
    (out / 'package_report.json').write_text(json.dumps(report, indent=2) + '\n')
    print('CREATURE_PACKAGE_COMPLETE ' + str(runtime))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mode', choices=['export', 'package'], required=True)
    parser.add_argument('--manifest', type=Path)
    parser.add_argument('--export', type=Path)
    parser.add_argument('--crunch', type=Path)
    parser.add_argument('--compiler', type=Path)
    parser.add_argument('--only', help='Export only this model')
    parser.add_argument('--output', type=Path, required=True)
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else sys.argv[1:]
    args = parser.parse_args(argv)
    out = args.output.resolve()
    if out.is_relative_to(REPO):
        raise ValueError('Keep staging output outside the repository')
    if out.exists() and any(out.iterdir()):
        raise ValueError('Output directory must be empty')
    out.mkdir(parents=True, exist_ok=True)
    if args.mode == 'package':
        if not args.export or not args.crunch:
            parser.error('package requires --export and --crunch')
        package(args, out)
        return
    if not args.manifest:
        parser.error('export requires --manifest')
    manifest = json.loads(args.manifest.read_text())
    if manifest.get('schema_version') != 1:
        raise ValueError('Unsupported manifest schema')
    weapon = load_module('trellis_weapon_export', REPO / 'tools/TrellisWeaponExport.py')
    weapon.load_blender()
    # Check every entry, source hashes included, before exporting any.
    manifest_dir = args.manifest.resolve().parent
    models, textures = set(), set()
    for item in manifest['creatures']:
        if item['model'].lower() in models or item['texture'].lower() in textures:
            raise ValueError(item['model'] + ': every creature needs its own model and texture name')
        models.add(item['model'].lower())
        textures.add(item['texture'].lower())
        if len(item['model']) > 16 or len(item['texture']) > 13 or item['texture_size'] not in (512, 1024):
            raise ValueError(item['model'] + ': model <= 16, texture <= 13 characters, texture_size 512 or 1024')
        if hashlib.sha256((manifest_dir / item['source']).read_bytes()).hexdigest() != item['sha256']:
            raise ValueError(item['model'] + ': source GLB does not match its sha256')
    # Any later failure removes the whole output (it started empty), so a retry never finds a half-written folder.
    reports = []
    try:
        for item in manifest['creatures']:
            if args.only and item['model'] != args.only:
                continue
            reports.append(export_creature(item, manifest_dir, out / item['model'], weapon))
    except BaseException:
        shutil.rmtree(out, ignore_errors=True)
        raise
    (out / 'reports.json').write_text(json.dumps(reports, indent=2) + '\n')
    print('CREATURE_EXPORT_COMPLETE ' + str(out))


if __name__ == '__main__':
    main()
