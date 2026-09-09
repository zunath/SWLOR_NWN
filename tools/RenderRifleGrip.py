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


def main():
    import bpy
    from mathutils import Matrix, Quaternion, Vector
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',type=Path,required=True)
    parser.add_argument('--animation',type=Path,required=True,help='ASCII a_ba_med_weap rifle animation reference')
    parser.add_argument('--hand',type=Path,required=True)
    parser.add_argument('--fit',type=Path)
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    output=args.output.resolve();output.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(args.source.resolve()))
    scene=bpy.context.scene
    if args.fit:
        fit=Matrix(json.loads(args.fit.read_text())['matrix'])
        for ob in scene.objects:ob.matrix_world=fit @ ob.matrix_world
    text=args.hand.read_text()
    if 'newmodel pmh0_handr001' not in text:raise ValueError('Expected ASCII human right-hand reference pmh0_handr001')
    block=re.search(r'node trimesh Hand\s*\n(.*?)endnode',text,re.S)[1]
    lines=block.splitlines();position=Vector((0,0,0));orientation=Quaternion()
    for i,line in enumerate(lines):
        p=line.split()
        if not p:continue
        if p[0]=='verts':verts=[tuple(map(float,l.split())) for l in lines[i+1:i+1+int(p[1])]]
        if p[0]=='faces':faces=[tuple(map(int,l.split()))[:3] for l in lines[i+1:i+1+int(p[1])]]
        if p[0]=='position':position=Vector(tuple(map(float,p[1:])))
        if p[0]=='orientation':orientation=Quaternion(Vector(tuple(map(float,p[1:4]))),float(p[4]))
    mesh=bpy.data.meshes.new('Human right-hand reference');mesh.from_pydata(verts,[],faces);mesh.update()
    hand=bpy.data.objects.new('Human right-hand reference',mesh);bpy.context.collection.objects.link(hand)
    # Apply the rifle animation hook rotation; the pistol bowshot hook is different.
    animation=args.animation.read_text()
    shot=re.search(r'newanim xbowshot .*?\n(.*?)doneanim',animation,re.S)[1]
    hook_node=re.search(r'node dummy rhand\s*\n(.*?)endnode',shot,re.S)[1]
    axis_angle=list(map(float,re.search(r'orientationkey \d+\s*\n([^\n]+)',hook_node)[1].split()))[1:]
    hook=Matrix.Translation(Vector((.0110681,0,-.0961281))) @ Quaternion(Vector(axis_angle[:3]),axis_angle[3]).to_matrix().to_4x4()
    hand.matrix_world=hook.inverted() @ Matrix.Translation(position) @ orientation.to_matrix().to_4x4()
    material=bpy.data.materials.new('Reference hand');material.use_nodes=True
    shader=material.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(.32,.17,.085,1);shader.inputs['Roughness'].default_value=.75
    mesh.materials.append(material)
    scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=64
    scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    camera=scene.camera;camera.data.type='ORTHO';camera.data.ortho_scale=.40
    center=Vector((0,.025,-.05))
    for side,name in ((1,'grip-preview.png'),(-1,'grip-preview-reverse.png')):
        camera.location=center+Vector((side,-.03,.06))
        camera.rotation_euler=((center-camera.location).to_track_quat('-Z','Y') @ Quaternion((0,0,1),-side*math.pi/2)).to_euler()
        scene.render.filepath=str(output/name);bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output/'grip-review.blend'))

if __name__=='__main__':main()
