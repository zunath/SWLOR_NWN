"""Blender 4.0 shield surface repair BEFORE UV/PBR baking; never repairs textured GLBs.

CLI: blender -b --factory-startup --python-exit-code 1 --python this.py --
     voxel|decimate INPUT.npz OUTPUT.npz [--voxel-size .003] [--target-faces 3500]

NPZs contain float32 vertices[N,3] and integer faces[M,3]. Use the continuous
TRELLIS UDF high surface as input: direct raw decode can retain perforated sheets.
Closed topology alone is insufficient; visually review body, rim, back and grip.
The separate shield workflow rebakes the original material field after repair.
"""
import argparse
import collections
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import numpy as np


def arrays(vertices, faces):
    vertices=np.asarray(vertices,dtype=np.float32)
    faces=np.asarray(faces)
    if vertices.ndim!=2 or vertices.shape[1]!=3 or not len(vertices) or not np.isfinite(vertices).all():
        raise ValueError('Expected nonempty finite vertices[N,3]')
    if faces.ndim!=2 or faces.shape[1]!=3 or not len(faces) or faces.dtype.kind not in 'iu':
        raise ValueError('Expected nonempty integer triangle faces[M,3]')
    if faces.min()<0 or faces.max()>=len(vertices):raise ValueError('Face index out of bounds')
    return vertices,faces.astype(np.int32)


def winding_number(point, triangles):
    """Generalized winding number: 1 inside an outward closed shell, -1 inside an inward one, 0 outside."""
    a,b,c=(triangles-point).transpose(1,0,2);la,lb,lc=(np.linalg.norm(x,axis=1) for x in (a,b,c))
    det=np.einsum('ij,ij->i',a,np.cross(b,c))
    den=la*lb*lc+np.einsum('ij,ij->i',a,b)*lc+np.einsum('ij,ij->i',b,c)*la+np.einsum('ij,ij->i',c,a)*lb
    return float(np.arctan2(det,den).sum()/(2*np.pi))


def edges_cross_triangles(edges, triangles, chunk=256, inclusive=False):
    """True when any segment passes through the interior of any triangle (Moller-Trumbore, strict; the
    parallel test is relative to the primitive sizes, so it holds at any scale). Both dimensions are batched, so each working array holds at most chunk*chunk*3 values."""
    for tstart in range(0,len(triangles),chunk):
        tris=triangles[tstart:tstart+chunk];v0=tris[:,0];e1=tris[:,1]-v0;e2=tris[:,2]-v0;eps=1e-9
        for start in range(0,len(edges),chunk):
            a=edges[start:start+chunk,0][:,None];d=(edges[start:start+chunk,1]-edges[start:start+chunk,0])[:,None]
            p=np.cross(d,e2[None]);det=np.einsum('ijk,jk->ij',p,e1)
            scale=np.linalg.norm(d,axis=-1)*(np.linalg.norm(e1,axis=1)*np.linalg.norm(e2,axis=1))[None]
            ok=np.abs(det)>eps*scale;inv=np.where(ok,1/np.where(ok,det,1),0)
            s=a-v0[None];u=np.einsum('ijk,ijk->ij',s,p)*inv;q=np.cross(s,e1[None]);v=np.einsum('ijk,ijk->ij',d,q)*inv
            t=np.einsum('jk,ijk->ij',e2,q)*inv
            lo,hi=(-eps,1+eps) if inclusive else (eps,1-eps)
            if np.any(ok&(u>lo)&(v>lo)&(u+v<hi)&(t>lo)&(t<hi)):return True
    return False


def points_touch_triangles(points, triangles, tolerance=1e-7, chunk=256):
    """True when any point lies on (within tolerance of) any triangle, edges and vertices included."""
    for tstart in range(0,len(triangles),chunk):
        tris=triangles[tstart:tstart+chunk];a=tris[:,0];ab=tris[:,1]-a;ac=tris[:,2]-a
        normal=np.cross(ab,ac);area=np.linalg.norm(normal,axis=1);unit=normal/np.maximum(area,1e-30)[:,None]
        d00=np.einsum('ij,ij->i',ab,ab);d01=np.einsum('ij,ij->i',ab,ac);d11=np.einsum('ij,ij->i',ac,ac);den=d00*d11-d01*d01
        for start in range(0,len(points),chunk):
            ap=points[start:start+chunk][:,None]-a[None];plane=np.abs(np.einsum('ijk,jk->ij',ap,unit))
            d20=np.einsum('ijk,jk->ij',ap,ab);d21=np.einsum('ijk,jk->ij',ap,ac)
            v=(d11*d20-d01*d21)/den;w=(d00*d21-d01*d20)/den;slack=tolerance/np.sqrt(np.maximum(d00,d11))
            if np.any((plane<=tolerance)&(v>=-slack)&(w>=-slack)&(v+w<=1+slack)):return True
    return False


def edges_touch_edges(first, second, tolerance=1e-7, chunk=256):
    """True when any segment of one set comes within tolerance of any segment of the other
    (covers coplanar X-shaped edge crossings that triangle-ray tests miss)."""
    for start in range(0,len(second),chunk):
        q0=second[start:start+chunk,0][None];d2=(second[start:start+chunk,1]-second[start:start+chunk,0])[None]
        for begin in range(0,len(first),chunk):
            p0=first[begin:begin+chunk,0][:,None];d1=(first[begin:begin+chunk,1]-first[begin:begin+chunk,0])[:,None]
            r=p0-q0;dot=lambda x,y:(x*y).sum(-1)
            a=dot(d1,d1)+0*r[...,0];e=dot(d2,d2)+0*r[...,0];f=dot(d2,r);c=dot(d1,r);b=dot(d1,d2);den=a*e-b*b
            s=np.clip(np.where(den>1e-30,(b*f-c*e)/np.where(den>1e-30,den,1),0),0,1)
            t=np.clip((b*s+f)/np.maximum(e,1e-30),0,1)
            s=np.clip((b*t-c)/np.maximum(a,1e-30),0,1)
            gap=p0+d1*s[...,None]-(q0+d2*t[...,None])
            if np.any(dot(gap,gap)<=tolerance*tolerance):return True
    return False


def overlapping_pairs(boxes_a, boxes_b, tolerance, max_cells=64, chunk=1024):
    """Index pairs whose axis-aligned boxes overlap (within tolerance). Primitives spanning at most max_cells
    grid cells go through a uniform grid; larger ones are compared against every box with a chunked
    vectorised test, so one long primitive never rasterises a huge box at the median cell size."""
    lo=np.minimum(boxes_a[:,0].min(axis=0),boxes_b[:,0].min(axis=0));extent=np.concatenate([boxes_a[:,1]-boxes_a[:,0],boxes_b[:,1]-boxes_b[:,0]])
    cell=max(float(np.median(extent.max(axis=1))),tolerance*10,1e-9)
    def cells(boxes):
        low=np.floor((boxes[:,0]-tolerance-lo)/cell).astype(np.int64);high=np.floor((boxes[:,1]+tolerance-lo)/cell).astype(np.int64)
        return low,high,np.prod((high-low+1).astype(np.float64),axis=1)>max_cells
    def overlap(x,y):return np.all(x[:,None,0]<=y[None,:,1]+tolerance,axis=2)&np.all(y[None,:,0]<=x[:,None,1]+tolerance,axis=2)
    alo,ahi,abig=cells(boxes_a);blo,bhi,bbig=cells(boxes_b)
    grid={}
    for index in np.flatnonzero(~abig):
        l,h=alo[index],ahi[index]
        for x in range(l[0],h[0]+1):
            for y in range(l[1],h[1]+1):
                for z in range(l[2],h[2]+1):grid.setdefault((x,y,z),[]).append(index)
    pairs=set()
    for index in np.flatnonzero(~bbig):
        l,h=blo[index],bhi[index]
        for x in range(l[0],h[0]+1):
            for y in range(l[1],h[1]+1):
                for z in range(l[2],h[2]+1):
                    for other in grid.get((x,y,z),()):pairs.add((int(other),int(index)))
    found=[np.asarray(sorted(pairs),np.int64).reshape(-1,2)]
    # Oversized primitives on either side meet everything on the other side through the bounded fallback.
    for big,others,flip in ((np.flatnonzero(abig),np.arange(len(boxes_b)),False),(np.flatnonzero(bbig),np.flatnonzero(~abig),True)):
        for start in range(0,len(big),chunk):
            rows=big[start:start+chunk]
            for ostart in range(0,len(others),chunk):
                cols=others[ostart:ostart+chunk]
                hit=overlap(boxes_b[rows],boxes_a[cols]) if flip else overlap(boxes_a[rows],boxes_b[cols])
                r,c=np.nonzero(hit)
                found.append(np.stack([cols[c],rows[r]],axis=1) if flip else np.stack([rows[r],cols[c]],axis=1))
    candidates=np.concatenate(found)
    if not len(candidates):return np.zeros((0,),np.int64),np.zeros((0,),np.int64)
    a,b=candidates.T
    keep=np.all(boxes_a[a,0]<=boxes_b[b,1]+tolerance,axis=1)&np.all(boxes_b[b,0]<=boxes_a[a,1]+tolerance,axis=1)
    return a[keep],b[keep]


def pair_segment_crosses_triangle(seg, tri, eps=1e-9):
    v0=tri[:,0];e1=tri[:,1]-v0;e2=tri[:,2]-v0;a=seg[:,0];d=seg[:,1]-seg[:,0]
    p=np.cross(d,e2);det=(p*e1).sum(-1)
    ok=np.abs(det)>eps*np.linalg.norm(d,axis=1)*np.linalg.norm(e1,axis=1)*np.linalg.norm(e2,axis=1);inv=np.where(ok,1/np.where(ok,det,1),0)
    sv=a-v0;u=(sv*p).sum(-1)*inv;q=np.cross(sv,e1);v=(d*q).sum(-1)*inv;t=(e2*q).sum(-1)*inv
    return ok&(u>-eps)&(v>-eps)&(u+v<1+eps)&(t>-eps)&(t<1+eps)


def pair_point_on_triangle(point, tri, tolerance):
    a=tri[:,0];ab=tri[:,1]-a;ac=tri[:,2]-a;normal=np.cross(ab,ac);area=np.linalg.norm(normal,axis=1)
    ap=point-a;plane=np.abs((ap*normal).sum(-1))/np.maximum(area,1e-30)
    d00=(ab*ab).sum(-1);d01=(ab*ac).sum(-1);d11=(ac*ac).sum(-1);d20=(ap*ab).sum(-1);d21=(ap*ac).sum(-1);den=np.where(np.abs(d00*d11-d01*d01)>1e-30,d00*d11-d01*d01,1)
    v=(d11*d20-d01*d21)/den;w=(d00*d21-d01*d20)/den;slack=tolerance/np.sqrt(np.maximum(np.maximum(d00,d11),1e-30))
    return (plane<=tolerance)&(v>=-slack)&(w>=-slack)&(v+w<=1+slack)


def pair_segments_touch(first, second, tolerance):
    p0=first[:,0];d1=first[:,1]-first[:,0];q0=second[:,0];d2=second[:,1]-second[:,0];r=p0-q0
    a=(d1*d1).sum(-1);e=(d2*d2).sum(-1);f=(d2*r).sum(-1);c=(d1*r).sum(-1);b=(d1*d2).sum(-1);den=a*e-b*b
    s=np.clip(np.where(den>1e-30,(b*f-c*e)/np.where(den>1e-30,den,1),0),0,1);t=np.clip((b*s+f)/np.maximum(e,1e-30),0,1)
    s=np.clip((b*t-c)/np.maximum(a,1e-30),0,1);gap=p0+d1*s[:,None]-(q0+d2*t[:,None])
    return (gap*gap).sum(-1)<=tolerance*tolerance


def shells_intersect(first, second, tolerance=1e-7):
    """Closed triangle sets intersect or touch when an edge of one crosses (or grazes) a triangle of
    the other, a vertex of one lies on the other, or their edges meet (coplanar contacts included).
    A uniform-grid broad phase keeps the exact tests to spatially overlapping pairs."""
    def edges(tris):return np.concatenate([tris[:,[0,1]],tris[:,[1,2]],tris[:,[2,0]]])
    def boxes(items):return np.stack([items.min(axis=1),items.max(axis=1)],axis=1)
    ea,eb=edges(first),edges(second);ta,tb=boxes(first),boxes(second);ba,bb=boxes(ea),boxes(eb)
    for segs,segboxes,tris,triboxes in ((ea,ba,second,tb),(eb,bb,first,ta)):
        i,j=overlapping_pairs(segboxes,triboxes,tolerance)
        if len(i) and (pair_segment_crosses_triangle(segs[i],tris[j]).any() or pair_point_on_triangle(segs[i,0],tris[j],tolerance).any()):return True
    i,j=overlapping_pairs(ba,bb,tolerance)
    return bool(len(i) and pair_segments_touch(ea[i],eb[j],tolerance).any())


def audit_surface(vertices, faces, components=False):
    """Exact-position closure; coincident split vertices cannot conceal bad edges."""
    vertices,faces=arrays(vertices,faces)
    _,inverse=np.unique(vertices,axis=0,return_inverse=True)
    welded=inverse[faces]
    directed=np.stack((welded,np.roll(welded,-1,axis=1)),axis=-1).reshape(-1,2)
    _,edge_ids,counts=np.unique(np.sort(directed,axis=1),axis=0,return_inverse=True,return_counts=True)
    directions=np.where(directed[:,0]<directed[:,1],1,-1)
    winding=np.bincount(edge_ids,weights=directions,minlength=len(counts))
    triangles=vertices[faces].astype(np.float64)
    area2=np.linalg.norm(np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0]),axis=1)
    report={'vertices':len(vertices),'triangles':len(faces),'bad_edges':int(((counts!=2)|(winding!=0)).sum()),
            'zero_area_triangles':int((area2==0).sum()),'bounds_min':vertices.min(axis=0).tolist(),'bounds_max':vertices.max(axis=0).tolist()}
    if components:
        # Shells connect through edges. A flat pair touching a solid at one
        # vertex must not borrow that solid's positive volume.
        parents=list(range(len(faces)))
        def find(index):
            while parents[index]!=index:parents[index]=parents[parents[index]];index=parents[index]
            return index
        order=np.argsort(edge_ids,kind='stable')
        shared=edge_ids[order[1:]]==edge_ids[order[:-1]]
        for first,second in zip(order[1:][shared]//3,order[:-1][shared]//3):
            parents[find(int(second))]=find(int(first))
        groups=collections.defaultdict(list)
        for index in range(len(faces)):groups[find(index)].append(index)
        shells=[]
        for ids in groups.values():
            points=triangles[ids];origin=points.mean(axis=(0,1));shifted=points-origin
            volume=float(np.einsum('ij,ij->i',shifted[:,0],np.cross(shifted[:,1],shifted[:,2])).sum()/6)
            shells.append({'triangles':len(ids),'signed_volume':volume,'area':float(area2[ids].sum()/2)})
        # An inward-wound shell is valid only as a cavity: it must not cross any other shell
        # and must lie in solid material (the other shells wind exactly once around it).
        exposed=0;ids_list=list(groups.values())
        for index,(ids,shell) in enumerate(zip(ids_list,shells)):
            if shell['signed_volume']>=0:continue
            others=[face for other,group in enumerate(ids_list) if other!=index for face in group]
            # Whole-shell containment: the shell must not cross any other shell (no edge of either
            # passes through a triangle of the other), and then one interior sample decides inside.
            # Disjoint closed shells cannot change containment across a connected shell, so one
            # representative point (largest face centroid) decides once crossing is ruled out.
            point=triangles[ids[int(np.argmax(area2[ids]))]].mean(axis=0)
            enclosed=(bool(others) and not shells_intersect(triangles[ids],triangles[others])
                      and abs(winding_number(point,triangles[others])-1)<1e-3)
            shell['enclosed_cavity']=enclosed;exposed+=not enclosed
        report['shells']=shells
        report['zero_volume_shells']=sum(shell['signed_volume']==0 for shell in shells)
        report['exposed_inward_shells']=exposed
    return report


def require_closed(report):
    if report['bad_edges'] or report['zero_area_triangles'] or report.get('zero_volume_shells',0):
        raise ValueError('Expected a closed nondegenerate surface: '+json.dumps(report))
    if report.get('exposed_inward_shells',0):
        raise ValueError('Inward-wound shell is not an enclosed cavity of an outward shell: '+json.dumps(report))


def remove_collapsed_islands(vertices, faces):
    """Remove only entire isolated components reduced to two opposite triangles."""
    vertices,faces=arrays(vertices,faces)
    face_sets=collections.defaultdict(list);vertex_faces=collections.defaultdict(set)
    for index,face in enumerate(faces):
        face_sets[tuple(sorted(map(int,face)))].append(index)
        for vertex in face:vertex_faces[int(vertex)].add(index)
    removed=set()
    for key,indices in face_sets.items():
        if len(indices)!=2 or set.union(*(vertex_faces[vertex] for vertex in key))!=set(indices):continue
        first,second=faces[indices]
        if any(np.array_equal(first,np.roll(second[::-1],shift)) for shift in range(3)):removed.update(indices)
    if removed:faces=faces[[index for index in range(len(faces)) if index not in removed]]
    if not len(faces):raise ValueError('Collapse left no volumetric geometry')
    used,inverse=np.unique(faces,return_inverse=True)
    return vertices[used],inverse.reshape(-1,3).astype(np.int32),len(removed)


def normal_unorm8(images):
    """Round a proven positive-Z float normal map; do not clamp invalid normals."""
    images=np.asarray(images)
    if images.ndim<3 or images.shape[-1]!=3 or not np.isfinite(images).all() or images.min()<0 or images.max()>1:
        raise ValueError('Expected finite RGB normal data in [0,1]')
    if images[...,2].min()<.5:raise ValueError('Negative tangent-space Z before quantization')
    if np.any(np.linalg.norm(images*2-1,axis=-1)<1e-8):raise ValueError('Undefined zero normal before quantization')
    return np.floor(images*255+.5).astype(np.uint8)


def blender():
    import bpy
    if bpy.app.version[:2]!=(4,0):raise ValueError('Use tested Blender 4.0')
    return bpy


def mesh_object(vertices, faces):
    bpy=blender();vertices,faces=arrays(vertices,faces)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mesh=bpy.data.meshes.new('ShieldSurface')
    mesh.vertices.add(len(vertices));mesh.vertices.foreach_set('co',vertices.ravel())
    mesh.loops.add(faces.size);mesh.loops.foreach_set('vertex_index',faces.ravel())
    mesh.polygons.add(len(faces));mesh.polygons.foreach_set('loop_start',np.arange(len(faces),dtype=np.int32)*3)
    mesh.polygons.foreach_set('loop_total',np.full(len(faces),3,dtype=np.int32));mesh.update()
    obj=bpy.data.objects.new('ShieldSurface',mesh);bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    return obj


def mesh_arrays(mesh):
    mesh.calc_loop_triangles()
    vertices=np.empty(len(mesh.vertices)*3,np.float32);mesh.vertices.foreach_get('co',vertices)
    faces=np.empty(len(mesh.loop_triangles)*3,np.int32);mesh.loop_triangles.foreach_get('vertices',faces)
    return vertices.reshape(-1,3),faces.reshape(-1,3)


def voxel_surface(vertices, faces, voxel_size=.003):
    if not np.isfinite(voxel_size) or not .0001<=voxel_size<=.1:raise ValueError('Expected voxel size .0001.. .1 in source coordinates')
    bpy=blender();obj=mesh_object(vertices,faces)
    modifier=obj.modifiers.new('ClosedVoxelSurface','REMESH')
    modifier.mode='VOXEL';modifier.voxel_size=voxel_size;modifier.adaptivity=0
    modifier.use_smooth_shade=True;modifier.use_remove_disconnected=False
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    import bmesh
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    # UDF grid crossings can create coincident points; this is well below a voxel.
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=min(1e-7,voxel_size*1e-4))
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-10)
    bm.to_mesh(obj.data);bm.free()
    output_vertices,output_faces,removed=remove_collapsed_islands(*mesh_arrays(obj.data))
    result=(output_vertices,output_faces);report=audit_surface(*result,components=True)
    report.update(source_vertices=len(vertices),source_triangles=len(faces),voxel_size=voxel_size,collapsed_zero_volume_faces_removed=removed)
    require_closed(report)
    return *result,report


def decimate_surface(vertices, faces, target_faces=3500):
    if type(target_faces) is not int or not 100<=target_faces<=100000:raise ValueError('Expected100..100000 target faces')
    require_closed(audit_surface(vertices,faces,components=True))
    bpy=blender();obj=mesh_object(vertices,faces)
    modifier=obj.modifiers.new('ClosedCollapse','DECIMATE');modifier.decimate_type='COLLAPSE'
    modifier.ratio=min(1.,target_faces/len(faces));modifier.use_collapse_triangulate=True
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    output_vertices,output_faces,removed=remove_collapsed_islands(*mesh_arrays(obj.data))
    report=audit_surface(output_vertices,output_faces,components=True)
    report.update(source_triangles=len(faces),target_faces=target_faces,collapsed_zero_volume_faces_removed=removed)
    require_closed(report)
    if len(output_faces)>target_faces:raise ValueError(f'Closed shield surface exceeds face budget: {len(output_faces)} > {target_faces}')
    return output_vertices,output_faces,report


def sync_directory(directory):
    """Flush a directory entry on POSIX; Windows exposes no directory fsync."""
    if os.name=='nt':return
    descriptor=os.open(directory,os.O_RDONLY|getattr(os,'O_DIRECTORY',0))
    try:os.fsync(descriptor)
    finally:os.close(descriptor)


def create_directories(directory):
    """Create missing ancestors and persist each new entry in its parent, top-down."""
    missing=[];path=Path(directory)
    while not os.path.lexists(path) and path.parent!=path:missing.append(path);path=path.parent
    directory.mkdir(parents=True,exist_ok=True)
    for path in reversed(missing):sync_directory(path.parent)


def durable_publish(source,target):
    """Durably publish a fresh destination without replacing an occupied path."""
    if os.name=='nt':
        import ctypes
        from ctypes import wintypes
        move=ctypes.WinDLL('kernel32',use_last_error=True).MoveFileExW
        move.argtypes=[wintypes.LPCWSTR,wintypes.LPCWSTR,wintypes.DWORD]
        move.restype=wintypes.BOOL
        if not move(str(source.absolute()),str(target.absolute()),0x8):
            raise ctypes.WinError(ctypes.get_last_error())
    else:
        os.link(source,target)
        source.unlink()
        sync_directory(target.parent)


def require_npz_output(output):
    if output.suffix.lower()!='.npz':raise ValueError('Shield surface output must use the .npz suffix')


def require_fresh_output(output):
    require_npz_output(output)
    report=output.with_suffix('.json')
    for path in (output,report,output.with_suffix(output.suffix+'.part'),report.with_suffix('.json.part')):
        if os.path.lexists(path):raise FileExistsError('Use fresh surface output/report paths: '+str(path))


def publish_surface(output,vertices,faces,report):
    require_fresh_output(output)
    create_directories(output.parent)
    temporary=output.with_suffix(output.suffix+'.part')
    with temporary.open('xb') as stream:
        np.savez(stream,vertices=vertices,faces=faces)
        stream.flush();os.fsync(stream.fileno())
    durable_publish(temporary,output)
    report=dict(report,output_sha256=hashlib.sha256(output.read_bytes()).hexdigest())
    report_path=output.with_suffix('.json');temporary_report=report_path.with_suffix('.json.part')
    with temporary_report.open('x',encoding='utf-8') as stream:
        json.dump(report,stream,indent=2);stream.write('\n')
        stream.flush();os.fsync(stream.fileno())
    durable_publish(temporary_report,report_path)
    return report


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode',choices=('voxel','decimate'))
    parser.add_argument('input',type=Path);parser.add_argument('output',type=Path)
    parser.add_argument('--voxel-size',type=float,default=.003)
    parser.add_argument('--target-faces',type=int,default=3500)
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    require_fresh_output(args.output)
    # One immutable snapshot owns both the loaded geometry and the recorded input hash.
    input_bytes=args.input.read_bytes()
    with np.load(io.BytesIO(input_bytes),allow_pickle=False) as source:
        if set(source.files)!={'vertices','faces'}:raise ValueError('Expected geometry-only NPZ; do not discard authored attributes')
        vertices,faces=arrays(source['vertices'],source['faces'])
    if args.mode=='voxel':vertices,faces,report=voxel_surface(vertices,faces,args.voxel_size)
    else:vertices,faces,report=decimate_surface(vertices,faces,args.target_faces)
    report['input_sha256']=hashlib.sha256(input_bytes).hexdigest()
    report=publish_surface(args.output,vertices,faces,report)
    print(json.dumps(report),flush=True)


if __name__=='__main__':main()
