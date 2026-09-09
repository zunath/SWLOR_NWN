"""Render a rifle against the human right hand in the rifle xbowshot pose.
Run inside Blender 4.0. --hand must be an ASCII pmh0_handr001 model;
--fit is optional for a scene already using NWN attachment coordinates.
"""
import argparse
import json
import math
import re
import sys
from pathlib import Path


def hand_pose_transforms(skeleton_text, animation_text, pose='xbowrdy'):
    """Resolve both hand geometry hooks relative to the weapon hook at pose start.

    NWN node matching is case-insensitive (e.g. Lbicep_g versus lbicep_g).
    Compose the full skeleton hierarchy before converting to weapon space.
    """
    from mathutils import Matrix, Quaternion, Vector
    def nodes(text):
        return {name.lower(): body for _, name, body in
                re.findall(r'node (\w+) (\S+)\n(.*?)endnode', text, re.S)}
    def rotation(values):
        return Quaternion(Vector(values[:3]), values[3]) if any(values[:3]) else Quaternion()
    skeleton = nodes(skeleton_text.split('endmodelgeom')[0])
    local = {}
    for name, body in skeleton.items():
        position = re.search(r'^\s*position (.+)$', body, re.M)
        orientation = re.search(r'^\s*orientation (.+)$', body, re.M)
        local[name] = (Vector(tuple(map(float, position[1].split()))) if position else Vector(),
                       rotation(list(map(float, orientation[1].split()))) if orientation else Quaternion())
    match = re.search(r'newanim ' + re.escape(pose) + r' .*?\n(.*?)doneanim', animation_text, re.S)
    if not match:
        raise ValueError(f'Missing rifle pose: {pose}')
    for name, body in nodes(match[1]).items():
        if name not in local:
            continue
        position, orientation = local[name]
        for channel in ('position', 'orientation'):
            key = re.search(channel + r'key \d+\s*\n([^\n]+)', body)
            if key:
                values = list(map(float, key[1].split()))[1:]
                if channel == 'position':
                    position = Vector(values)
                else:
                    orientation = rotation(values)
        local[name] = position, orientation
    cache = {'null': Matrix.Identity(4)}
    def world(name):
        if name not in cache:
            position, orientation = local[name]
            parent = re.search(r'parent (\S+)', skeleton[name])[1].lower()
            cache[name] = world(parent) @ Matrix.Translation(position) @ orientation.to_matrix().to_4x4()
        return cache[name]
    weapon = world('rhand').inverted()
    return {side: weapon @ world(side + 'hand_g') for side in ('r', 'l')}


def required_block(pattern, text, description):
    match = re.search(pattern, text, re.S | re.I)
    if not match:
        raise ValueError(f"Missing {description}")
    return match[1]


def hand_geometry(text, side):
    from mathutils import Quaternion, Vector
    model = 'pmh0_handr001' if side == 'right' else 'pmh0_handl001'
    if not re.search(r'newmodel\s+' + model + r'\b', text, re.I):
        raise ValueError(f'Expected ASCII human {side}-hand reference {model}')
    block = required_block(r'node trimesh \S+\s*\n(.*?)endnode', text, f'{side}-hand trimesh')
    lines = block.splitlines()
    verts = faces = None
    position, orientation = Vector(), Quaternion()
    for i, line in enumerate(lines):
        parts = line.split()
        if not parts:
            continue
        key = parts[0].lower()
        if key in ('verts', 'faces'):
            count = int(parts[1])
            rows = lines[i + 1:i + 1 + count]
            if len(rows) != count:
                raise ValueError(f'Incomplete {side}-hand {key} rows')
            if key == 'verts':
                verts = [tuple(map(float, row.split())) for row in rows]
            else:
                faces = [tuple(map(int, row.split()[:3])) for row in rows]
        elif key == 'position':
            position = Vector(tuple(map(float, parts[1:])))
        elif key == 'orientation':
            orientation = Quaternion(Vector(tuple(map(float, parts[1:4]))), float(parts[4]))
    if not verts or not faces:
        raise ValueError(f'{side}-hand reference trimesh has no verts/faces rows')
    return verts, faces, position, orientation


def main():
    import bpy
    from mathutils import Matrix, Quaternion, Vector
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',type=Path,required=True)
    parser.add_argument('--animation',type=Path,required=True,help='ASCII a_ba_med_weap rifle animation reference')
    parser.add_argument('--hand',type=Path,required=True)
    parser.add_argument('--skeleton',type=Path,help='ASCII pmh0 skeleton for both-hand review')
    parser.add_argument('--left-hand',type=Path,help='ASCII pmh0_handl001; requires --skeleton')
    parser.add_argument('--pose',default='xbowrdy',choices=('xbowrdy','xbowshot'))
    parser.add_argument('--fit',type=Path)
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    if bool(args.skeleton) != bool(args.left_hand):
        raise ValueError('Supply both --skeleton and --left-hand for two-hand review')
    output=args.output.resolve();output.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(args.source.resolve()))
    scene=bpy.context.scene
    if args.fit:
        fit=Matrix(json.loads(args.fit.read_text())['matrix'])
        for ob in scene.objects:ob.matrix_world=fit @ ob.matrix_world
    verts,faces,position,orientation=hand_geometry(args.hand.read_text(), 'right')
    mesh=bpy.data.meshes.new('Human right-hand reference');mesh.from_pydata(verts,[],faces);mesh.update()
    hand=bpy.data.objects.new('Human right-hand reference',mesh);bpy.context.collection.objects.link(hand)
    # Apply the rifle animation hook rotation; the pistol bowshot hook is different.
    animation=args.animation.read_text()
    shot=required_block(r'newanim xbowshot .*?\n(.*?)doneanim',animation,'xbowshot animation')
    hook_node=required_block(r'node dummy rhand\s*\n(.*?)endnode',shot,'rhand animation node')
    key=required_block(r'orientationkey \d+\s*\n([^\n]+)',hook_node,'rhand orientation key')
    axis_angle=list(map(float,key.split()))[1:]
    if len(axis_angle) != 4:
        raise ValueError('Expected four axis-angle values in rhand orientation key')
    hook=Matrix.Translation(Vector((.0110681,0,-.0961281))) @ Quaternion(Vector(axis_angle[:3]),axis_angle[3]).to_matrix().to_4x4()
    hand.matrix_world=hook.inverted() @ Matrix.Translation(position) @ orientation.to_matrix().to_4x4()
    material=bpy.data.materials.new('Reference hand');material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(.32,.17,.085,1);shader.inputs['Roughness'].default_value=.75
    mesh.materials.append(material)
    if args.skeleton:
        poses=hand_pose_transforms(args.skeleton.read_text(),animation,args.pose)
        hand.matrix_world=poses['r'] @ Matrix.Translation(position) @ orientation.to_matrix().to_4x4()
        left_verts,left_faces,left_position,left_orientation=hand_geometry(args.left_hand.read_text(), 'left')
        left_mesh=bpy.data.meshes.new('Human left-hand reference');left_mesh.from_pydata(left_verts,[],left_faces);left_mesh.update()
        left=bpy.data.objects.new('Human left-hand reference',left_mesh);bpy.context.collection.objects.link(left)
        left.matrix_world=poses['l'] @ Matrix.Translation(left_position) @ left_orientation.to_matrix().to_4x4()
        left_mesh.materials.append(material)
    scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=64
    scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    camera=scene.camera;camera.data.type='ORTHO';camera.data.ortho_scale=.40
    center=Vector((0,.025,-.05))
    if args.skeleton:
        center=Vector((0,.025,-.15));camera.data.ortho_scale=.7
    for side,name in ((1,'grip-preview.png'),(-1,'grip-preview-reverse.png')):
        camera.location=center+Vector((side,-.03,.06))
        camera.rotation_euler=((center-camera.location).to_track_quat('-Z','Y') @ Quaternion((0,0,1),-side*math.pi/2)).to_euler()
        scene.render.filepath=str(output/name);bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'grip-review.blend'))

if __name__=='__main__':main()
