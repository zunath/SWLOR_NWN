"""Preserve glTF baked normal directions through the compiled NWN tangent basis.

The compiler owns the final tangent/sign streams. Metadata records the immutable
source triangle basis in native coordinates; packaging matches real native faces
before reprojecting normal texels. No SWTOR channel unpacking or Z clamping.
"""
from __future__ import annotations
import hashlib,json,math,struct
from pathlib import Path
import numpy as np


def unit(value):
    length=np.linalg.norm(value,axis=-1,keepdims=True)
    if not np.isfinite(value).all() or np.any(length<1e-12):
        raise ValueError('Degenerate/nonfinite tangent basis')
    return value/length


def basis(normal,tangent,sign):
    normal=unit(normal)
    tangent=unit(tangent-normal*np.sum(tangent*normal,axis=-1,keepdims=True))
    sign=np.asarray(sign)
    if np.any(abs(sign)<.5):raise ValueError('Ambiguous interpolated tangent handedness')
    return tangent,np.cross(normal,tangent)*np.sign(sign)[...,None],normal


def reproject_normals(encoded,source_normal,source_tangent,source_sign,target_normal,target_tangent,target_sign):
    """RGB is linear [0,1]; return the exact target-space unit direction."""
    st,sb,sn=basis(source_normal,source_tangent,source_sign)
    tt,tb,tn=basis(target_normal,target_tangent,target_sign)
    decoded=unit(encoded*2-1)
    world=unit(st*decoded[...,0,None]+sb*decoded[...,1,None]+sn*decoded[...,2,None])
    mapped=unit(np.stack((np.sum(world*tt,axis=-1),np.sum(world*tb,axis=-1),np.sum(world*tn,axis=-1)),axis=-1))
    if np.any(mapped[...,2]<-1e-6):
        raise ValueError(f'Native positive-Z normal map cannot preserve {np.count_nonzero(mapped[...,2]<-1e-6)} source directions; minimum Z={mapped[...,2].min():.8g}')
    return mapped


def accessor(document,binary,index):
    item=document['accessors'][index]
    if item.get('sparse') or item.get('normalized'):raise ValueError('Sparse/normalized basis accessor unsupported')
    view=document['bufferViews'][item['bufferView']]
    columns={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[item['type']]
    dtype=np.dtype({5126:'<f4',5125:'<u4',5123:'<u2',5121:'u1'}[item['componentType']])
    offset=view.get('byteOffset',0)+item.get('byteOffset',0);stride=view.get('byteStride',columns*dtype.itemsize)
    if view.get('buffer',0)!=0 or offset<0 or offset+(item['count']-1)*stride+columns*dtype.itemsize>len(binary):
        raise ValueError('Source basis accessor out of bounds')
    return np.ndarray((item['count'],columns),dtype=dtype,buffer=binary,offset=offset,strides=(stride,dtype.itemsize)).copy()


def node_matrix(node):
    if 'matrix' in node:return np.asarray(node['matrix'],float).reshape(4,4).T
    x,y,z,w=node.get('rotation',[0,0,0,1]);r=np.asarray([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
        [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
    result=np.eye(4);result[:3,:3]=r@np.diag(node.get('scale',[1,1,1]));result[:3,3]=node.get('translation',[0,0,0]);return result


def source_basis(document,binary,config,material_names,path):
    """Bind source glTF TBN and UVs to the explicit proper, uniform native fit."""
    fit=np.eye(4);fit[:3,:3]=np.asarray(config['orientation'])*config['scale'];fit[:3,3]=np.asarray(config['target_grip'])-fit[:3,:3]@np.asarray(config['source_grip'])
    conversion=np.array([[1,0,0,0],[0,0,-1,0],[0,1,0,0],[0,0,0,1]],float)
    nodes=document.get('nodes',[]);scene=document.get('scenes',[{}])[document.get('scene',0)];pending=[(i,np.eye(4),frozenset()) for i in scene.get('nodes',[])];groups={}
    while pending:
        index,parent,ancestors=pending.pop()
        if index in ancestors:raise ValueError('Cyclic glTF node hierarchy')
        node=nodes[index];world=parent@node_matrix(node)
        pending.extend((i,world,ancestors|{index}) for i in node.get('children',[]))
        if 'mesh' not in node:continue
        matrix=fit@conversion@world;linear=matrix[:3,:3];gram=linear.T@linear;scale=math.sqrt(float(np.trace(gram)/3))
        if not np.allclose(gram,np.eye(3)*scale*scale,atol=1e-7) or np.linalg.det(linear)<=0:
            raise ValueError('Baked normal basis requires proper uniform source-node transforms; apply nonuniform authoring transforms explicitly')
        rotation=linear/scale
        for primitive in document['meshes'][node['mesh']]['primitives']:
            attributes=primitive['attributes'];material=primitive['material'];name=material_names[material]
            p=accessor(document,binary,attributes['POSITION']).astype(float);n=accessor(document,binary,attributes['NORMAL']).astype(float);uv=accessor(document,binary,attributes['TEXCOORD_0']).astype(float)
            faces=accessor(document,binary,primitive['indices']).reshape(-1,3) if 'indices' in primitive else np.arange(len(p)).reshape(-1,3)
            if 'TANGENT' in attributes:t=accessor(document,binary,attributes['TANGENT']).astype(float)
            else:
                # glTF specifies MikkTSpace when tangents are omitted.
                import bpy
                mesh=bpy.data.meshes.new('Source normal basis');mesh.from_pydata(p.tolist(),[],faces.tolist());mesh.update();layer=mesh.uv_layers.new(name='UVMap')
                for poly in mesh.polygons:poly.use_smooth=True
                for loop in mesh.loops:layer.data[loop.index].uv=uv[loop.vertex_index]
                mesh.use_auto_smooth=True;mesh.normals_split_custom_set([n[l.vertex_index] for l in mesh.loops]);mesh.calc_tangents(uvmap='UVMap')
                t=np.zeros((len(p),4));seen={}
                for loop in mesh.loops:
                    value=np.array([*loop.tangent,loop.bitangent_sign]);old=seen.setdefault(loop.vertex_index,value)
                    if not np.allclose(old,value,atol=1e-5):raise ValueError('Missing source tangent requires splitting a Mikk seam')
                    t[loop.vertex_index]=value
                bpy.data.meshes.remove(mesh)
            target=groups.setdefault(name,{k:[] for k in ('positions','normals','tangents','signs','uv')})
            values={'positions':(p@linear.T+matrix[:3,3])[faces],'normals':unit((n@rotation.T)[faces]),'tangents':unit((t[:,:3]@rotation.T)[faces]),'signs':t[:,3][faces],'uv':np.stack((uv[:,0],1-uv[:,1]),axis=-1)[faces]}
            for key,value in values.items():target[key].extend(value)
    arrays={name+'__'+key:np.asarray(value) for name,group in groups.items() for key,value in group.items()}
    np.savez_compressed(path,**arrays)
    return {'path':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'source_sha256':config['sha256'],'materials':sorted(groups),'uv':'native U=glTF U; native V=1-glTF V; source tangent handedness is preserved separately'}


def native_meshes(path):
    data=path.read_bytes();raw=12+struct.unpack_from('<I',data,4)[0]
    def u(offset):return struct.unpack_from('<I',data,offset)[0]
    def stream(mesh,offset,count,columns):
        start=struct.unpack_from('<i',data,mesh+offset)[0]
        if start<0:raise ValueError('Native tangent stream missing')
        return np.frombuffer(data,dtype='<f4',count=count*columns,offset=raw+start).reshape(count,columns).astype(float)
    pending=[u(84)];seen=set();result=[]
    while pending:
        pointer=pending.pop();node=12+pointer
        if pointer in seen:raise ValueError('Cyclic native graph')
        seen.add(pointer);pending.extend(u(12+u(node+72)+i*4) for i in range(u(node+76)))
        if not u(node+108)&32:continue
        mesh=node+112
        if not u(mesh+108):continue
        if u(mesh+440)!=0:raise ValueError('Interleaved native tangent stream unsupported')
        count=struct.unpack_from('<H',data,mesh+448)[0];faces=np.array([struct.unpack_from('<3H',data,12+u(mesh+8)+i*32+26) for i in range(u(mesh+12))])
        material=data[mesh+312:mesh+376].split(b'\0',1)[0].decode('ascii')
        result.append({'material':material,**{key:stream(mesh,offset,count,cols)[faces] for key,offset,cols in [('positions',444,3),('uv',452,2),('normals',468,3),('tangents',488,3),('signs',496,1)]}})
        result[-1]['signs']=result[-1]['signs'][...,0]
        result[-1]['vertex_indices']=faces
        result[-1]['tangent_offset']=raw+struct.unpack_from('<i',data,mesh+488)[0]
        result[-1]['sign_offset']=raw+struct.unpack_from('<i',data,mesh+496)[0]
    return result


def read_tga(path):
    data=path.read_bytes();width,height=struct.unpack_from('<HH',data,12);channels=data[16]//8
    if data[1]!=0 or data[2]!=2 or channels not in (3,4):raise ValueError('Expected raw true-color TGA')
    offset=18+data[0];pixels=np.frombuffer(data,dtype=np.uint8,count=width*height*channels,offset=offset).reshape(height,width,channels).copy()
    if data[17]&32:pixels=pixels[::-1]
    if data[17]&16:pixels=pixels[:,::-1]
    return data,pixels[...,[2,1,0]+([3] if channels==4 else [])].copy()


def write_tga(path,original,pixels):
    data=bytearray(original);channels=pixels.shape[2];p=pixels[...,[2,1,0]+([3] if channels==4 else [])]
    if data[17]&16:p=p[:,::-1]
    if data[17]&32:p=p[::-1]
    offset=18+data[0];data[offset:offset+p.size]=p.tobytes();path.write_bytes(data)


def match_faces(source,target):
    """Match whole source faces or exact uncapped subdivisions using barycentrics."""
    sp=source['positions'];su=source['uv'];centers=sp.mean(axis=1);result=[]
    for ti,tp in enumerate(target['positions']):
        center=tp.mean(axis=0);tu=target['uv'][ti];candidates=np.where(np.linalg.norm(centers-center,axis=1)<3e-6)[0];found=None;matches=[]
        for si in candidates:
            for shift in range(3):
                ids=np.roll(np.arange(3),shift)
                if np.max(abs(sp[si,ids]-tp))<3e-6 and np.max(abs(su[si,ids]-tu))<2e-6:
                    matches.append((int(si),np.eye(3)[ids]));break
        if matches:found=matches[0]
        if found is None:
            # A clipping plane can subdivide a source face without changing its UV field.
            a=sp[:,0];e=sp[:,1]-a;f=sp[:,2]-a;normal=np.cross(e,f);length=np.linalg.norm(normal,axis=1);plane=np.abs(np.sum((center-a)*normal,axis=1))/np.maximum(length,1e-20)
            for si in np.where((plane<3e-6)&(length>1e-15))[0]:
                matrix=np.stack((e[si],f[si]),axis=1);weights=np.linalg.lstsq(matrix,(tp-a[si]).T,rcond=None)[0].T;bary=np.column_stack((1-weights.sum(axis=1),weights))
                if bary.min()>=-2e-5 and np.max(abs(bary@sp[si]-tp))<3e-6 and np.max(abs(bary@su[si]-tu))<2e-6:matches.append((int(si),bary))
            if matches:found=matches[0]
        if found is None:raise ValueError(f'Native face {ti} has no exact source position/UV ownership')
        if len(matches)>1 and all(key in source for key in ('normals','tangents','signs')):
            reference=None
            for si,weights in matches:
                frame=basis(weights@source['normals'][si],weights@source['tangents'][si],weights@source['signs'][si])
                if reference is None:reference=frame
                elif any(not np.allclose(a,b,atol=1e-6,rtol=0) for a,b in zip(reference,frame)):
                    raise ValueError(f'Native face {ti} has ambiguous coincident source tangent ownership')
        result.append(found)
    return result


def pad_native_normals(pixels, coverage):
    """Fill every unused texel from the nearest native-frame texel in Manhattan distance.

    Full coverage prevents any mip footprint from mixing in untouched source-frame RGB.
    Covered texels and alpha are immutable; no wrapped neighbors or extra dependencies.
    """
    height,width=coverage.shape
    if pixels.shape[:2]!=(height,width) or not coverage.any():raise ValueError('Native normal padding needs covered texels')
    xs=np.broadcast_to(np.arange(width),(height,width));ys=np.broadcast_to(np.arange(height)[:,None],(height,width))
    left=np.maximum.accumulate(np.where(coverage,xs,-width),axis=1)
    right=np.minimum.accumulate(np.where(coverage,xs,2*width)[:,::-1],axis=1)[:,::-1]
    owner_x=np.where(xs-left<=right-xs,left,right).clip(0,width-1);owner_y=ys.copy()
    distance=np.abs(xs-owner_x);distance[~coverage.any(axis=1)]=height+width+1
    for rows,previous in [(range(1,height),-1),(range(height-2,-1,-1),1)]:
        for y in rows:
            closer=distance[y+previous]+1<distance[y]
            distance[y,closer]=distance[y+previous,closer]+1
            owner_x[y,closer]=owner_x[y+previous,closer];owner_y[y,closer]=owner_y[y+previous,closer]
    if not coverage[owner_y,owner_x].all():raise ValueError('Incomplete native normal padding')
    result=pixels.copy();empty=~coverage;result[empty,:3]=pixels[owner_y[empty],owner_x[empty],:3]
    return result


def reproject_texture(source,meshes,input_path,output_path):
    original,pixels=read_tga(input_path);height,width=pixels.shape[:2];result=pixels.copy();coverage=np.zeros((height,width),bool);changed=0;overlap=0;max_error=0.;matched=0
    for mesh in meshes:
        for ti,(si,weights) in enumerate(match_faces(source,mesh)):
            matched+=1;uv=mesh['uv'][ti];xmin=max(0,int(np.floor(uv[:,0].min()*width-.5)));xmax=min(width-1,int(np.ceil(uv[:,0].max()*width-.5)));ymin=max(0,int(np.floor(uv[:,1].min()*height-.5)));ymax=min(height-1,int(np.ceil(uv[:,1].max()*height-.5)))
            if uv.min()<-1e-6 or uv.max()>1+1e-6:raise ValueError('Normal reprojection requires nonwrapped UV0 islands')
            if xmin>xmax or ymin>ymax:continue
            yy,xx=np.mgrid[ymin:ymax+1,xmin:xmax+1];q=np.stack(((xx+.5)/width,(yy+.5)/height),axis=-1);a=uv[1]-uv[0];b=uv[2]-uv[0];den=a[0]*b[1]-a[1]*b[0]
            if abs(den)<1e-15:continue
            dif=q-uv[0];bb=(dif[...,0]*b[1]-dif[...,1]*b[0])/den;cc=(a[0]*dif[...,1]-a[1]*dif[...,0])/den;aa=1-bb-cc;mask=(aa>=-1e-9)&(bb>=-1e-9)&(cc>=-1e-9);x=xx[mask];y=yy[mask]
            if not len(x):continue
            bary=np.stack((aa[mask],bb[mask],cc[mask]),axis=-1);old=bary@weights
            mapped=reproject_normals(pixels[y,x,:3]/255.,old@source['normals'][si],old@source['tangents'][si],old@source['signs'][si],bary@mesh['normals'][ti],bary@mesh['tangents'][ti],bary@mesh['signs'][ti])
            encoded=np.clip(np.round((mapped+1)*127.5),0,255).astype(np.uint8);used=coverage[y,x]
            if np.any(used):overlap+=int(np.count_nonzero(np.max(abs(result[y[used],x[used],:3].astype(int)-encoded[used].astype(int)),axis=1)>3))
            result[y,x,:3]=encoded;coverage[y,x]=True;changed+=len(x);decoded=unit(encoded/127.5-1);max_error=max(max_error,float(np.degrees(np.arccos(np.clip(np.sum(decoded*mapped,axis=-1),-1,1))).max()))
    if overlap:raise ValueError(f'Conflicting native normal UV ownership at {overlap} texels in {input_path.name}')
    if matched==0 or not coverage.any():raise ValueError('No native UV surface for '+input_path.name)
    # No untouched source-frame texels may contribute to any generated mip level.
    result=pad_native_normals(result,coverage);filled=np.ones_like(coverage)
    write_tga(output_path,original,result)
    return {'source_tga_sha256':hashlib.sha256(original).hexdigest(),'reprojected_tga_sha256':hashlib.sha256(output_path.read_bytes()).hexdigest(),'native_triangles_matched':matched,'covered_texels':int(coverage.sum()),'dilated_texels':int((filled&~coverage).sum()),'padding':'full_texture_nearest_manhattan','conflicting_uv_texels':overlap,'max_quantization_error_degrees':max_error,'negative_z_clamped':False}


def preserve_authored_tangents(path,sources):
    """Explicit policy: preserve uniquely owned authored frames, never merge conflicts.

    Native positions, indices, UVs, normals, materials and controllers are immutable.
    Only existing tangent/sign float streams are patched; normals remain the
    compiler's retained, quantized authored normals.
    """
    before=path.read_bytes();after=bytearray(before);allowed=set();rows=[]
    for mesh in native_meshes(path):
        source=sources[mesh['material']];mixed_faces=int(np.count_nonzero(np.min(source['signs'],axis=1)!=np.max(source['signs'],axis=1)));owned={};max_tangent_angle=0.;max_normal_angle=0.;conflict_error=0.
        for face,(si,weights) in enumerate(match_faces(source,mesh)):
            st=unit(weights@source['tangents'][si]);sn=unit(weights@source['normals'][si]);sign=weights@source['signs'][si]
            if np.any(abs(sign)<.99999):raise ValueError('Authored tangent sign changes within a native face')
            native_n=unit(mesh['normals'][face]);target=unit(st-native_n*np.sum(st*native_n,axis=-1,keepdims=True))
            max_tangent_angle=max(max_tangent_angle,float(np.degrees(np.arccos(np.clip(np.sum(st*target,axis=-1),-1,1))).max()))
            max_normal_angle=max(max_normal_angle,float(np.degrees(np.arccos(np.clip(np.sum(sn*unit(native_n),axis=-1),-1,1))).max()))
            for corner,vertex in enumerate(mesh['vertex_indices'][face]):
                candidate=np.r_[target[corner],np.sign(sign[corner])]
                if int(vertex) in owned:
                    error=float(np.max(abs(owned[int(vertex)]-candidate)));conflict_error=max(conflict_error,error)
                    if error>1e-6:raise ValueError(f'Authored tangent conflict at compiled vertex {vertex} of {path.name}; do not average')
                else:owned[int(vertex)]=candidate
        # Bound importer normal quantization instead of silently changing the normals.
        if max_normal_angle>.5 or max_tangent_angle>.5:
            raise ValueError('Authored basis differs materially from retained native normals')
        for vertex,value in owned.items():
            tangent=mesh['tangent_offset']+vertex*12;sign=mesh['sign_offset']+vertex*4
            struct.pack_into('<3f',after,tangent,*value[:3]);struct.pack_into('<f',after,sign,value[3]);allowed.update(range(tangent,tangent+12));allowed.update(range(sign,sign+4))
        rows.append({'material':mesh['material'],'source_mixed_sign_faces':mixed_faces,'uniquely_owned_vertices':len(owned),'shared_vertex_conflicts':0,'max_repeated_corner_float_difference':conflict_error,'max_retained_normal_angle_degrees':max_normal_angle,'max_tangent_orthogonalization_angle_degrees':max_tangent_angle})
    changed={i for i,(a,b) in enumerate(zip(before,after)) if a!=b}
    if len(before)!=len(after) or not changed<=allowed:raise ValueError('Authored tangent patch changed bytes outside existing tangent/sign streams')
    path.write_bytes(after)
    return {'model':path.name,'policy':'authored_tangents','before_sha256':hashlib.sha256(before).hexdigest(),'after_sha256':hashlib.sha256(after).hexdigest(),'changed_bytes':len(changed),'only_existing_tangent_sign_streams_changed':True,'native_interpolation':'NWN interpolates tangent.w then uses +1 when W>=0, else -1; glTF mixed-sign triangle tangent semantics are undefined. This explicit native appearance policy does not recover an intended high-poly baked field or guarantee identical glTF shading.','native_close_view_verification_required':True,'meshes':rows}
