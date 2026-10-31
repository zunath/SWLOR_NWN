"""Author capped native parts from an explicitly fitted, tangent-bearing Meshy GLB.

Run inside Blender 4.0. Reviewed joint_envelopes contain y, center_xz,
radii_xz and depth measured from the existing native joint sleeves. Source
surfaces keep their position/UV field; only cut faces and joint sleeves are new.
Partial sources, such as a dedicated electroblade hilt, may author one role;
their final export still requires an explicit complete composite assembly.
All output and per-model fit data stay in private staging.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import sys

import numpy as np


def convex_hull(points):
    points=sorted(set(tuple(float(v) for v in point) for point in points))
    if len(points)<3:return []
    def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    def half(values):
        result=[]
        for p in values:
            while len(result)>1 and cross(result[-2],result[-1],p)<=1e-14:result.pop()
            result.append(p)
        return result
    return half(points)[:-1]+half(points[::-1])[:-1]


def cap_triangles(points,y,outward):
    hull=convex_hull(points)
    if len(hull)<3:return []
    center=np.mean(hull,axis=0);origin=(center[0],y,center[1]);triangles=[]
    for a,b in zip(hull,hull[1:]+hull[:1]):
        triangle=[origin,(a[0],y,a[1]),(b[0],y,b[1])]
        if outward>0:triangle.reverse()
        triangles.append(triangle)
    return triangles


def sleeve_triangles(joint,bound,outward,segments=32):
    """Closed elliptical sleeve ending exactly at the shared native boundary."""
    center=joint['center_xz'];rx,rz=joint['radii_xz'];depth=joint['depth']
    low,high=sorted((bound,bound-outward*depth));rings=[]
    for y in (low,high):
        rings.append([(center[0]+rx*math.cos(2*math.pi*i/segments),y,
                       center[1]+rz*math.sin(2*math.pi*i/segments)) for i in range(segments)])
    result=[]
    for i in range(segments):
        j=(i+1)%segments;a,b=rings[0][i],rings[0][j];c,d=rings[1][i],rings[1][j]
        result.extend(([a,c,b],[b,c,d]))
    for ring,y,normal in ((rings[0],low,-1),(rings[1],high,1)):
        result.extend(cap_triangles([(p[0],p[2]) for p in ring],y,normal))
    return result


def flat_basis(triangles):
    positions=np.asarray(triangles,dtype=float)
    edge=positions[:,1]-positions[:,0]
    normal=np.cross(edge,positions[:,2]-positions[:,0])
    def unit(v):
        length=np.linalg.norm(v,axis=-1,keepdims=True)
        if np.any(length<1e-12):raise ValueError('Degenerate authored interface triangle')
        return v/length
    normal=unit(normal);tangent=unit(edge)
    return {'positions':positions,'normals':np.repeat(normal[:,None,:],3,axis=1),
            'tangents':np.repeat(tangent[:,None,:],3,axis=1),'signs':np.ones((len(positions),3)),
            'uv':np.tile([[0,1],[1,1],[0,0]],(len(positions),1,1))}


def clip_source_basis(source, intervals):
    """Carry triangle ownership through cuts instead of rematching nearby faces."""
    import TrellisWeaponNormals as normal_helper
    result={key:[] for key in ('positions','uv','normals','tangents','signs')}
    for low,high in intervals:
        for index,positions in enumerate(source['positions']):
            polygon=list(np.eye(3))
            for bound,side in ((low,1),(high,-1)):
                if not math.isfinite(bound) or not polygon:continue
                distances=[side*(weight@positions[:,1]-bound) for weight in polygon]
                if side<0 and all(value==0 for value in distances):polygon=[];break
                clipped=[]
                for i,a in enumerate(polygon):
                    b=polygon[(i+1)%len(polygon)];da=distances[i];db=distances[(i+1)%len(polygon)]
                    inside_a,inside_b=da>=0,db>=0
                    if inside_a and inside_b:clipped.append(b)
                    elif inside_a or inside_b:
                        crossing=a+(b-a)*(da/(da-db))
                        clipped.extend([crossing,b] if inside_b else [crossing])
                polygon=clipped
            for i in range(1,len(polygon)-1):
                weights=np.asarray([polygon[0],polygon[i],polygon[i+1]])
                points=weights@positions
                if np.linalg.norm(np.cross(points[1]-points[0],points[2]-points[0]))==0:continue
                normal=normal_helper.unit(weights@source['normals'][index])
                tangent=weights@source['tangents'][index]
                tangent=normal_helper.unit(tangent-normal*np.sum(tangent*normal,axis=-1,keepdims=True))
                signs=weights@source['signs'][index]
                if np.any(abs(signs)<.99999):raise ValueError('Clipped source has ambiguous tangent handedness')
                for key,value in (('positions',points),('uv',weights@source['uv'][index]),
                                  ('normals',normal),('tangents',tangent),('signs',np.sign(signs))):
                    result[key].append(value)
    return {key:np.asarray(value) for key,value in result.items()}


def validate_joints(joints):
    seen=set()
    for joint in joints:
        if set(joint)!={'y','center_xz','radii_xz','depth'}:raise ValueError('Explicit measured joint fields required')
        values=[joint['y'],joint['depth'],*joint['center_xz'],*joint['radii_xz']]
        if len(joint['center_xz'])!=2 or len(joint['radii_xz'])!=2 or any(type(v) not in (int,float) or not math.isfinite(v) for v in values):
            raise ValueError('Finite measured joint envelope required')
        if joint['depth']<=0 or min(joint['radii_xz'])<=0 or joint['y'] in seen:raise ValueError('Positive unique joint envelope required')
        seen.add(joint['y'])


def write_source(document,binary,groups,path,exporter,mesh_groups=None):
    doc=copy.deepcopy(document);raw=bytearray(binary)
    conversion=np.array([[1,0,0],[0,0,1],[0,-1,0]],float)
    meshes=[];nodes=[]
    def accessor(values,kind):
        values=np.asarray(values,dtype='<f4');raw.extend(bytes((-len(raw))%4));offset=len(raw);raw.extend(values.tobytes())
        views=doc.setdefault('bufferViews',[]);views.append({'buffer':0,'byteOffset':offset,'byteLength':values.nbytes})
        accessors=doc.setdefault('accessors',[]);entry={'bufferView':len(views)-1,'componentType':5126,'count':len(values),'type':kind}
        if kind=='VEC3':entry.update(min=values.min(0).tolist(),max=values.max(0).tolist())
        accessors.append(entry);return len(accessors)-1
    for name,selected in (mesh_groups or [('native',groups)]):
        primitives=[]
        for material,group in selected:
            p,n,t=(group[key].reshape(-1,3)@conversion.T for key in ('positions','normals','tangents'))
            uv=group['uv'].reshape(-1,2).copy();uv[:,1]=1-uv[:,1]
            tangent=np.column_stack((t,group['signs'].reshape(-1)))
            attributes={'POSITION':accessor(p,'VEC3'),'NORMAL':accessor(n,'VEC3'),
                        'TEXCOORD_0':accessor(uv,'VEC2'),'TANGENT':accessor(tangent,'VEC4')}
            primitives.append({'attributes':attributes,'material':material,'mode':4})
        nodes.append({'mesh':len(meshes),'name':name});meshes.append({'primitives':primitives})
    doc['buffers']=[{'byteLength':len(raw)}];doc['meshes']=meshes
    doc['nodes']=nodes;doc['scenes']=[{'nodes':list(range(len(nodes)))}];doc['scene']=0
    for key in ('animations','skins','cameras','extensions','extensionsUsed','extensionsRequired'):doc.pop(key,None)
    exporter.write_glb(path,doc,bytes(raw))


def author(manifest,output,repo):
    import TrellisWeaponExport as e
    import TrellisWeaponNormals as normals
    e.load_blender()
    manifest,output,repo=(Path(p).resolve() for p in (manifest,output,repo))
    if output.is_relative_to(repo) or (output.exists() and any(output.iterdir())):raise ValueError('Use empty private staging outside the repository')
    config,_=e.load_manifest(manifest);e.validate_manifest(config,repo,manifest,'author')
    output.mkdir(parents=True,exist_ok=True);assets=[];assemblies=[];reports=[]
    identity=[[1,0,0],[0,1,0],[0,0,1]]
    for asset in config['assets']:
        if asset['_model_type']!=2:raise ValueError('Native part authoring requires composite weapons')
        if asset['scale']!=1 or asset['orientation']!=identity or any(asset['source_grip']) or any(asset['target_grip']):
            raise ValueError('Apply the reviewed fit with MeshyWeaponExport prepare first')
        joints=asset.get('joint_envelopes',[]);validate_joints(joints)
        source=Path(asset['_source']);source_bytes=source.read_bytes()
        if hashlib.sha256(source_bytes).hexdigest()!=asset['sha256']:
            raise ValueError('Source GLB changed after manifest validation: '+str(source))
        document,binary=e.glb_document(source_bytes);e.validate_glb_geometry(document,True)
        if any('TANGENT' not in p['attributes'] for m in document['meshes'] for p in m['primitives']):raise ValueError('Explicit prepared source tangents required')
        folder=output/asset['asset'];folder.mkdir();names={i:'source_'+str(i) for i in range(len(document['materials']))}
        basis_path=folder/'source_basis.npz';normals.source_basis(document,binary,asset,names,basis_path)
        with np.load(basis_path,allow_pickle=False) as data:
            sources={i:{key:data[name+'__'+key] for key in ('positions','uv','normals','tangents','signs')} for i,name in names.items()}
        cap_material=len(document['materials']);document['materials'].append({'name':'Authored native interfaces','doubleSided':False,
            'pbrMetallicRoughness':{'baseColorFactor':[.22,.25,.28,1],'metallicFactor':.85,'roughnessFactor':.34}})
        models={};part_reports=[];mesh_groups=[];native_parts=[]
        for part in asset['parts']:
            role=part['model'].rsplit('_',2)[-2];models[role]=part['model'];groups=[];cut_points={};source_triangles=0
            intervals=e.part_intervals(part)
            for material,source_basis in sources.items():
                clipped=clip_source_basis(source_basis,intervals);positions=clipped['positions']
                if not len(positions):continue
                groups.append((material,clipped));source_triangles+=len(positions)
                for low,high in intervals:
                    for bound,outward in ((low,-1),(high,1)):
                        if not math.isfinite(bound):continue
                        points=positions.reshape(-1,3);rim=points[np.abs(points[:,1]-bound)<1e-7]
                        cut_points.setdefault((bound,outward),[]).extend(rim[:,[0,2]].tolist())
            if not groups:raise ValueError('Empty authored native part '+part['model'])
            interfaces=[]
            for (bound,outward),points in cut_points.items():
                interfaces.extend(cap_triangles(points,bound,outward))
                joint=next((joint for joint in joints if abs(joint['y']-bound)<1e-9),None)
                if joint is None:raise ValueError('Missing reviewed joint envelope at '+str(bound))
                interfaces.extend(sleeve_triangles(joint,bound,outward))
            if interfaces:groups.append((cap_material,flat_basis(interfaces)))
            mesh_groups.append((part['model'],groups))
            native_part={key:copy.deepcopy(value) for key,value in part.items() if key in ('model','aliases')}
            native_part['source_nodes']=[part['model']];native_parts.append(native_part)
            part_reports.append({'model':part['model'],'source_triangles':source_triangles,'authored_interface_triangles':len(interfaces)})
        frame=asset.get('part_icon_bounds')
        if frame is None:raise ValueError('Explicit part_icon_bounds required for the complete assembly')
        destination=folder/'parts.glb';write_source(document,binary,[],destination,e,mesh_groups)
        result={key:copy.deepcopy(value) for key,value in asset.items() if not key.startswith('_')}
        result.update(source=destination.relative_to(output).as_posix(),sha256=hashlib.sha256(destination.read_bytes()).hexdigest(),parts=native_parts)
        result.pop('joint_envelopes',None);assets.append(result)
        if set(models)=={'b','m','t'}:
            assemblies.append({'base_item':asset['base_item'],'bottom':models['b'],'middle':models['m'],'top':models['t'],'icon_bounds':frame})
        reports.append({'asset':asset['asset'],'source_sha256':asset['sha256'],'parts':part_reports,'joint_envelopes':joints})
    input_assemblies=copy.deepcopy(config.get('composite_assemblies',[]))
    input_base_items={assembly['base_item'] for assembly in input_assemblies}
    result={key:copy.deepcopy(value) for key,value in config.items()
            if key not in ('assets','composite_assemblies')}
    result['assets']=assets
    result['composite_assemblies']=input_assemblies+[
        assembly for assembly in assemblies if assembly['base_item'] not in input_base_items]
    (output/'fit.json').write_text(json.dumps(result,indent=2)+'\n')
    (output/'authoring.json').write_text(json.dumps({'producer_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'assets':reports},indent=2)+'\n')
    print('MESHY_PARTS_COMPLETE '+str(output/'fit.json'))


def main():
    sys.path.insert(0,str(Path(__file__).resolve().parent))
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--manifest',type=Path,required=True)
    parser.add_argument('--output',type=Path,required=True);parser.add_argument('--repo',type=Path,default=Path(__file__).resolve().parents[1])
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    args=parser.parse_args(argv);author(args.manifest,args.output,args.repo)


if __name__=='__main__':main()
