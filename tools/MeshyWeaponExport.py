"""Export opaque Meshy GLBs through the native weapon pipeline with explicit two-sided surfaces."""
from __future__ import annotations

import hashlib
import argparse
import json
import sys
from pathlib import Path

import numpy as np


def producer_sha256():
    return globals().get('SNAPSHOT_SHA256') or hashlib.sha256(Path(__file__).read_bytes()).hexdigest()


def duplicate_backfaces(mesh):
    vertices, uv, faces, normals = mesh
    count = len(vertices)
    return (list(vertices) + list(vertices), list(uv) + list(uv),
            list(faces) + [(c + count, b + count, a + count) for a, b, c in faces],
            list(normals) + [tuple(-value for value in normal) for normal in normals])


def expand_source_basis(path, materials, metadata):
    path = Path(path)
    with np.load(path, allow_pickle=False) as source:
        arrays = {key: source[key] for key in source.files}
    for material in materials:
        for key in ('positions', 'uv', 'normals', 'tangents', 'signs'):
            name = material + '__' + key
            front = arrays[name]
            back = front[:, ::-1].copy()
            if key in ('normals', 'tangents', 'signs'):
                back *= -1
            arrays[name] = np.concatenate((front, back))
    np.savez_compressed(path, **arrays)
    metadata.update(sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                    double_sided_basis_producer_sha256=producer_sha256(),
                    explicit_backface_materials=sorted(materials))


def main():
    sys.path.insert(0,str(Path(__file__).resolve().parent))
    import TrellisWeaponExport
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    if '--mode' in argv and argv[argv.index('--mode')+1]=='prepare':
        parser=argparse.ArgumentParser(description=__doc__)
        parser.add_argument('--mode',choices=['prepare'],required=True)
        parser.add_argument('--manifest',type=Path,required=True)
        parser.add_argument('--output',type=Path,required=True)
        parser.add_argument('--repo',type=Path,default=Path(__file__).resolve().parents[1])
        args=parser.parse_args(argv)
        prepare(args.manifest,args.output,args.repo)
    else:TrellisWeaponExport.main()


def prepare(manifest,output,repo):
    """Apply the reviewed uniform fit and emit explicit Mikk tangents before native export."""
    import TrellisWeaponExport as exporter
    exporter.load_blender()
    bpy=exporter.bpy
    manifest,output,repo=(Path(p).resolve() for p in (manifest,output,repo))
    if output.is_relative_to(repo) or (output.exists() and any(output.iterdir())):
        raise ValueError('Use an empty staging output outside the repository')
    config,manifest_sha=exporter.load_manifest(manifest)
    exporter.validate_manifest(config,repo,manifest,'inspect')
    output.mkdir(parents=True,exist_ok=True)
    assets=[];reports=[]
    for asset in config['assets']:
        source=Path(asset['_source']);source_bytes=source.read_bytes()
        if hashlib.sha256(source_bytes).hexdigest()!=asset['sha256']:
            raise ValueError('Source GLB changed after manifest validation')
        document,binary=exporter.glb_document(source_bytes)
        exporter.validate_glb_geometry(document,allow_double_sided=True)
        exporter.validate_orientation(asset.get('orientation'))
        scale=asset.get('scale')
        if not exporter.number(scale) or scale<=0:raise ValueError('A reviewed positive uniform scale is required')
        rotation=exporter.Matrix(asset['orientation']).to_4x4()
        exporter.validate_attachment(asset,rotation)
        fit=exporter.Matrix.Translation(exporter.finite3(asset.get('target_grip'),'target_grip'))@rotation@exporter.Matrix.Scale(scale,4)@exporter.Matrix.Translation(-exporter.finite3(asset.get('source_grip'),'source_grip'))
        folder=output/asset['asset'];folder.mkdir()
        frozen=folder/'input.glb';frozen.write_bytes(source_bytes)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(frozen))
        exporter.remove_imported_lights()
        objects=[ob for ob in bpy.context.scene.objects if ob.type=='MESH']
        if not objects or any(ob.type=='ARMATURE' for ob in bpy.context.scene.objects):
            raise ValueError('Meshy preparation requires rigid mesh objects')
        # Freeze the full hierarchy before detaching or moving any parent mesh.
        source_world = {ob: ob.matrix_world.copy() for ob in objects}
        bpy.ops.object.select_all(action='DESELECT')
        for ob in objects:
            if ob.modifiers or not ob.data.uv_layers:raise ValueError('Source modifiers or missing UVs require explicit authoring')
            linear=np.asarray(source_world[ob].to_3x3(),float)
            gram=linear.T@linear;scale2=float(np.trace(gram)/3)
            if scale2<=0 or np.linalg.det(linear)<=0 or not np.allclose(gram/scale2,np.eye(3),atol=1e-7):
                raise ValueError('Source node transforms must be proper and uniform')
            world=fit@source_world[ob];ob.parent=None;ob.matrix_world=world;ob.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        destination=folder/'source.glb'
        bpy.ops.export_scene.gltf(filepath=str(destination),export_format='GLB',use_selection=True,
            export_texcoords=True,export_normals=True,export_tangents=True,export_materials='EXPORT',
            export_yup=True,export_animations=False)
        prepared_bytes=destination.read_bytes();prepared,_=exporter.glb_document(prepared_bytes)
        def triangles(doc):
            return sum(doc['accessors'][primitive.get('indices',primitive['attributes']['POSITION'])]['count']//3
                       for mesh in doc['meshes'] for primitive in mesh['primitives'])
        if triangles(document)!=triangles(prepared):raise ValueError('Preparation changed source triangle count')
        if any('TANGENT' not in primitive['attributes'] for mesh in prepared['meshes'] for primitive in mesh['primitives']):
            raise ValueError('Preparation failed to produce explicit source tangents')
        original_fit={key:asset[key] for key in ('scale','orientation','source_grip','target_grip')}
        result={key:value for key,value in asset.items() if not key.startswith('_')}
        result.update(source=destination.relative_to(output).as_posix(),sha256=hashlib.sha256(prepared_bytes).hexdigest(),
            scale=1.,orientation=[[1,0,0],[0,1,0],[0,0,1]],source_grip=[0,0,0],target_grip=[0,0,0],
            double_sided_policy='duplicate_backfaces',normal_basis_policy='authored_tangent_seams')
        result['attachment']=json.loads(json.dumps(asset['attachment']))
        for pair in result['attachment']['axis_pairs']:pair['source']=list(pair['target'])
        assets.append(result)
        reports.append({'asset':asset['asset'],'source_sha256':asset['sha256'],'prepared_sha256':result['sha256'],
            'applied_fit':original_fit,'source_triangles':triangles(document),
            'materials_before':len(document['materials']),'materials_after':len(prepared['materials'])})
    result={key:value for key,value in config.items() if key!='assets'};result['assets']=assets
    (output/'fit.json').write_text(json.dumps(result,indent=2)+'\n')
    (output/'preparation.json').write_text(json.dumps({'source_manifest_sha256':manifest_sha,
        'producer_sha256':producer_sha256(),'assets':reports},indent=2)+'\n')
    print('MESHY_PREPARE_COMPLETE '+str(output/'fit.json'))


if __name__ == '__main__':
    main()
