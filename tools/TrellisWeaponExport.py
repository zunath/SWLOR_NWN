"""Stage textured TRELLIS/glTF 2 rigid weapons; never deploy or edit item data.

Blender 4.0: blender -b --factory-startup --python-exit-code 1 --python
 tools/TrellisWeaponExport.py -- --mode inspect|export --manifest PATH --output EMPTY.
Python: python -B tools/TrellisWeaponExport.py --mode package --export PATH
 --crunch crunch.exe --compiler cleanmodels.exe --output EMPTY.

Schema 1 assets: asset, source (relative GLB), sha256, base_item. Export also
requires model, texture (<=14 characters), texture_size (512/1024), scale,
orientation (proper 3x3 rotation), source_grip, target_grip, attachment with
review_notes and two independent source/target axis_pairs, and icon_size (the
actual native canvas, not merely baseitems inventory slots). Simple large shields
must preserve both native impact hooks with attachment_dummies entries containing
name (imp_shield_1/2), position and unit XYZW quaternion in fitted root space.
Transforms refer
to imported Blender WORLD coordinates, after glTF axis conversion. Composite
exports require bottom/middle/top coverage in parts or composite_assemblies.
Parts support plane_y/clip_side or clip_min_y/clip_max_y interval bounds.
Optional aliases:[] and
empty_parts[{model,base_item,icon_size}] can clear explicitly audited simple-model
IDs only. Composite weapons require real bottom, middle and top geometry.
normal_basis_policy defaults to 'reproject'. Explicit 'authored_tangents' patches
only uniquely owned native tangent/sign streams and keeps staged map pixels;
mixed-sign glTF triangles have undefined shading, so this selects NWN's native
smooth-W-to-sign convention and requires close native visual qualification.
double_sided_policy='closed_surface' explicitly permits culling conversion only
after exact position-welded shells prove closed and consistently wound, with
any inward shell enclosed by a nonintersecting outward shell. Source normals are preserved and opposed-normal diagnostics are reported.
ModelPart1/2/3 are bottom/middle/top. Splits are uncapped and require review of
all shared consumers; no automatic fit, seam repair, decimation or deployment.
Intermediate TGA/BLEND files stay in staging. Package output is compiled
MDL + MTR + compressed DDS only. Native hand/attack/icon verification is separate.
"""
from __future__ import annotations

import argparse, base64, hashlib, importlib.util, io, json, math, os, re, shlex, shutil, stat, struct, subprocess, sys
sys.dont_write_bytecode=True
from pathlib import Path



def glb_document(path):
    data = bytes(path) if isinstance(path, (bytes, bytearray)) else Path(path).read_bytes()
    if len(data) < 20:
        raise ValueError('Truncated GLB')
    magic, version, length = struct.unpack_from('<III', data)
    if magic != 0x46546C67 or version != 2 or length != len(data):
        raise ValueError('Expected complete glTF 2 GLB')
    offset, doc, binary = 12, None, None
    while offset < length:
        if offset + 8 > length:
            raise ValueError('Truncated GLB chunk header')
        size, kind = struct.unpack_from('<II', data, offset)
        if size % 4 or offset + 8 + size > length:
            raise ValueError('Invalid GLB chunk bounds/alignment')
        chunk = data[offset+8:offset+8+size]
        if kind == 0x4E4F534A:
            if doc is not None or offset != 12:
                raise ValueError('Expected one first JSON chunk')
            doc = json.loads(chunk)
        elif kind == 0x004E4942:
            if binary is not None:
                raise ValueError('Duplicate binary chunk')
            binary = chunk
        else:
            raise ValueError('Unsupported GLB chunk')
        offset += 8 + size
    if doc is None or doc.get('asset', {}).get('version') != '2.0':
        raise ValueError('GLB JSON glTF 2 asset missing')
    # Source SHA must bind every input byte. No untracked external dependencies.
    if any(b.get('uri') for b in doc.get('buffers', [])) or any(
            'uri' in image and not image['uri'].startswith('data:')
            for image in doc.get('images', [])):
        raise ValueError('Use a self-contained GLB with embedded buffers/images')
    return doc, binary


def write_glb(path, document, binary):
    raw = json.dumps(document).encode('utf-8'); raw += b' ' * (-len(raw) % 4)
    chunks = struct.pack('<II', len(raw), 0x4E4F534A) + raw
    if binary is not None:
        chunks += struct.pack('<II', len(binary), 0x004E4942) + binary
    path.write_bytes(struct.pack('<III', 0x46546C67, 2, 12+len(chunks)) + chunks)


def number(value):
    return type(value) in (int, float) and math.isfinite(value)


def vector3(value, label):
    if not isinstance(value, list) or len(value) != 3 or not all(number(x) for x in value):
        raise ValueError(label + ' must have three finite numbers')
    return value


def validate_orientation(rows):
    if not isinstance(rows, list) or len(rows) != 3:
        raise ValueError('orientation must be a 3x3 proper rotation')
    for row in rows:
        vector3(row, 'orientation row')
    for i in range(3):
        for j in range(3):
            if abs(sum(rows[i][k]*rows[j][k] for k in range(3)) - (i == j)) > 1e-6:
                raise ValueError('orientation must be orthonormal; scale belongs in scale')
    a,b,c=rows
    determinant = a[0]*(b[1]*c[2]-b[2]*c[1])-a[1]*(b[0]*c[2]-b[2]*c[0])+a[2]*(b[0]*c[1]-b[1]*c[0])
    if abs(determinant-1) > 1e-6:
        raise ValueError('orientation must preserve handedness')


def native_icon_size(value):
    if not isinstance(value, list) or len(value) != 2 or any(
            type(n) is not int or n < 4 or n > 512 or n & (n-1) for n in value):
        raise ValueError('icon_size must be explicit native power-of-two canvas dimensions, 4..512')
    return tuple(value)


def material_response():
    """TRELLIS maps need NWN-specific albedo/roughness correction; see TrellisNwnMaterial.py."""
    return load_module('trellis_nwn_material',Path(__file__).with_name('TrellisNwnMaterial.py'))


def metallic_to_specularity(metal):
    # Inverse of inc_material metallicness = clamp(3 * specularity - .6, 0, 1); non-metal is low dielectric.
    return material_response().metal_specularity(metal)


def validate_material(mat, allow_double_sided=False):
    if mat.get('alphaMode', 'OPAQUE') != 'OPAQUE' or (mat.get('doubleSided', False) and not allow_double_sided):
        raise ValueError('Only opaque, single-sided rigid materials are supported')
    if mat.get('extensions') or mat.get('occlusionTexture'):
        raise ValueError('Material extensions/AO require explicit conversion, not silent omission')
    pbr=mat.get('pbrMetallicRoughness', {})
    for key, default, length in [('baseColorFactor',[1,1,1,1],4), ('emissiveFactor',[0,0,0],3)]:
        value=(pbr if key=='baseColorFactor' else mat).get(key, default)
        if not isinstance(value,list) or len(value)!=length or any(not number(x) or not 0<=x<=1 for x in value):
            raise ValueError('Invalid material '+key)
    if pbr.get('baseColorFactor',[1,1,1,1])[3] != 1:
        raise ValueError('Nonopaque base color factor')
    for key in ['metallicFactor','roughnessFactor']:
        value=pbr.get(key,1)
        if not number(value) or not 0<=value<=1:
            raise ValueError('Invalid material '+key)
    scale=mat.get('normalTexture',{}).get('scale',1)
    if not number(scale) or scale<0:
        raise ValueError('Invalid normal scale')


def normalize_normal(normal):
    length=np.linalg.norm(normal,axis=-1,keepdims=True)
    if not np.isfinite(normal).all() or np.any(length<1e-8):
        raise ValueError('Normal map contains a zero/nonfinite vector')
    normalized=normal/length
    if np.any(normalized[...,2]<-1e-6):
        raise ValueError('BC5 normal encoding cannot preserve negative tangent-space Z')
    return normalized


def prepare_normal_map(pixels, scale, limit):
    normal=normalize_normal(pixels[...,:3]*2-1)  # Validate every source texel before filtering can hide it.
    normal[...,:2]*=scale
    normal=normalize_normal(normal)  # Filter unit directions so steep texels are not overweighted.
    return normalize_normal(resize(normal,limit))


def validate_texture_samplers(doc):
    for texture in doc.get('textures',[]):
        if 'sampler' not in texture:continue
        index=texture['sampler'];samplers=doc.get('samplers',[])
        if type(index) is not int or not 0<=index<len(samplers):raise ValueError('Invalid texture sampler index')
        sampler=samplers[index]
        if sampler.get('extensions'):raise ValueError('Texture sampler extensions require explicit conversion')
        # NWN material exports use repeat wrapping, linear magnification and trilinear minification.
        expected={'wrapS':10497,'wrapT':10497,'magFilter':9729,'minFilter':9987}
        for key,value in expected.items():
            if key in sampler and (type(sampler[key]) is not int or sampler[key]!=value):
                raise ValueError('Unsupported texture sampler '+key+'; explicit conversion required')


def validate_glb_geometry(doc, allow_double_sided=False):
    validate_texture_samplers(doc)
    if doc.get('animations') or doc.get('skins') or doc.get('extensionsRequired'):
        raise ValueError('Animated/skinned/extended GLBs require a different importer')
    if not doc.get('meshes') or not doc.get('materials'):
        raise ValueError('Expected textured rigid GLB meshes and materials')
    for mesh in doc['meshes']:
        for primitive in mesh.get('primitives', []):
            attributes=primitive.get('attributes',{})
            if primitive.get('mode',4)!=4 or primitive.get('targets') or primitive.get('extensions'):
                raise ValueError('Only static triangle primitives are supported')
            if not {'POSITION','NORMAL','TEXCOORD_0'} <= attributes.keys():
                raise ValueError('Source positions, normals and UV0 are required')
            if any(k not in {'POSITION','NORMAL','TEXCOORD_0','TANGENT'} for k in attributes):
                raise ValueError('Extra UV sets/vertex colors/skin attributes require explicit conversion')
            if type(primitive.get('material')) is not int or not 0<=primitive['material']<len(doc['materials']):
                raise ValueError('Every primitive must reference a source material')
    for mat in doc['materials']:
        validate_material(mat,allow_double_sided)


def finite3(value, label):
    if not isinstance(value, list) or len(value) != 3 or not all(isinstance(x,(int,float)) and math.isfinite(x) for x in value):
        raise ValueError(label+' must have three finite numbers')
    return Vector(vector3(value,label))


def list_index(value, items, label):
    """glTF references must be exact nonnegative indices; Python negative indexing would pick another entry."""
    if type(value) is not int or not 0<=value<len(items):raise ValueError('Invalid glTF '+label+' index')
    return value


def image_bytes(doc, binary, index, source):
    entry = doc['images'][list_index(index,doc.get('images',[]),'image')]
    if 'bufferView' in entry:
        index_=entry['bufferView'];views=doc.get('bufferViews',[])
        if type(index_) is not int or not 0<=index_<len(views):raise ValueError('Invalid image bufferView index')
        view = views[index_]
        if view.get('buffer',0) != 0 or binary is None: raise ValueError('Only GLB buffer0 images supported')
        start = view.get('byteOffset',0);length=view.get('byteLength')
        if any(type(x) is not int or x<0 for x in (start,length)) or start+length>len(binary):raise ValueError('Image bufferView out of bounds')
        end=start+length
        data = binary[start:end]
    else:
        uri = entry['uri']
        if uri.startswith('data:'): data = base64.b64decode(uri.split(',',1)[1],validate=True)
        else:
            path = (source.parent / uri).resolve()
            if not path.is_relative_to(source.parent.resolve()): raise ValueError('External image escapes source folder')
            data = path.read_bytes()
    suffix = '.png' if data.startswith(b'\x89PNG') else '.jpg' if data.startswith(b'\xff\xd8') else None
    if suffix is None: raise ValueError('Expected PNG/JPEG source images')
    return data, suffix


def read_image(doc, binary, texture_info, source, cache):
    validate_texture_samplers(doc)
    if texture_info.get('texCoord',0) != 0 or texture_info.get('extensions'):
        raise ValueError('Nondefault texture UV sets/transforms need explicit support')
    texture = doc.get('textures',[])[list_index(texture_info.get('index'),doc.get('textures',[]),'texture')]
    if texture.get('extensions'): raise ValueError('Extended texture format unsupported')
    index = list_index(texture.get('source'),doc.get('images',[]),'image'); data, suffix = image_bytes(doc,binary,index,source)
    path = cache / (str(index)+suffix); path.write_bytes(data)
    img = bpy.data.images.load(str(path),check_existing=False)
    img.colorspace_settings.name = 'Non-Color'
    array = np.empty(img.size[0]*img.size[1]*4,dtype=np.float32)
    img.pixels.foreach_get(array)
    result = array.reshape(img.size[1],img.size[0],4).copy()
    bpy.data.images.remove(img)
    return result


def resize(a, limit):
    h,w = a.shape[:2]
    factor = min(1,limit/max(h,w))
    nh,nw = max(1,round(h*factor)), max(1,round(w*factor))
    if (nh,nw)==(h,w): return a
    # Separable area averaging (correct for linear color/scalar data).
    def axis_area(x, n, axis):
        x = np.moveaxis(x,axis,0); old=x.shape[0]
        edges=np.linspace(0,old,n+1); out=[]
        for lo,hi in zip(edges[:-1],edges[1:]):
            ids=np.arange(math.floor(lo),math.ceil(hi))
            weights=np.minimum(hi,ids+1)-np.maximum(lo,ids)
            out.append(np.tensordot(weights/(hi-lo),x[ids],axes=(0,0)))
        return np.moveaxis(np.asarray(out,dtype=np.float32),0,axis)
    return axis_area(axis_area(a,nh,0),nw,1)


def linear(rgb): return np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4)
def srgb(rgb): return np.where(rgb<=.0031308,rgb*12.92,1.055*np.maximum(rgb,0)**(1/2.4)-.055)


def save_texture(path, rgb):
    if rgb.ndim==2: rgb=np.repeat(rgb[...,None],3,axis=2)
    h,w=rgb.shape[:2]; rgba=np.ones((h,w,4),np.float32); rgba[...,:3]=np.clip(rgb,0,1)
    img=bpy.data.images.new(path.stem,width=w,height=h,alpha=False)
    img.colorspace_settings.name='Non-Color'; img.pixels.foreach_set(rgba.ravel())
    img.filepath_raw=str(path); img.file_format='TARGA_RAW'; img.save()
    bpy.data.images.remove(img)
    return [w,h]


def material_maps(doc,binary,index,source,cache,resources,name,limit):
    mat=doc['materials'][index]; validate_material(mat); pbr=mat.get('pbrMetallicRoughness',{})
    if mat.get('alphaMode','OPAQUE')!='OPAQUE': raise ValueError('Transparent material requires review')
    if mat.get('extensions'): raise ValueError('Material extensions require explicit conversion: '+str(mat['extensions'].keys()))
    if mat.get('occlusionTexture'): raise ValueError('Source AO map present: review conversion rather than silently discard')
    basefactor=np.asarray(pbr.get('baseColorFactor',[1,1,1,1]))
    if basefactor[3]!=1: raise ValueError('Base color alpha factor is not opaque')
    if 'baseColorTexture' in pbr:
        base=read_image(doc,binary,pbr['baseColorTexture'],source,cache)
        base=srgb(resize(linear(base[...,:3])*basefactor[:3],limit))
    else: base=np.broadcast_to(srgb(basefactor[:3]),(4,4,3))
    if 'metallicRoughnessTexture' in pbr:
        mr=read_image(doc,binary,pbr['metallicRoughnessTexture'],source,cache)
        metal=resize(mr[...,2:3],limit)[...,0]*pbr.get('metallicFactor',1)
        rough=resize(mr[...,1:2],limit)[...,0]*pbr.get('roughnessFactor',1)
    else:
        metal=np.full((4,4),pbr.get('metallicFactor',1),np.float32)
        rough=np.full((4,4),pbr.get('roughnessFactor',1),np.float32)
    # TRELLIS metal is dark and satin; NWN renders that near black and matte. Raise metal albedo
    # (at base resolution) and tighten metal roughness before the inverse specularity mapping.
    response=material_response();metal=np.clip(metal,0,1)
    if 'baseColorTexture' not in pbr and metal.shape!=base.shape[:2]:
        base=np.broadcast_to(base[:1,:1],metal.shape+(3,))  # constant color: keep the full metal mask detail
    elif metal.shape[0]>base.shape[0] or metal.shape[1]>base.shape[1]:
        # A coarser base texture would sample away small metal regions; correct at the finer size.
        base=response.resample_nearest(base,(max(base.shape[0],metal.shape[0]),max(base.shape[1],metal.shape[1])))
    base=response.metal_base_color(base,response.resample_nearest(metal,base.shape[:2]))
    rough=response.metal_roughness(rough,metal);spec=response.metal_specularity(metal)
    if 'normalTexture' in mat:
        nt=mat['normalTexture'];normal=prepare_normal_map(read_image(doc,binary,nt,source,cache),nt.get('scale',1),limit)
        normal=normal*.5+.5
    else: normal=np.broadcast_to(np.array([.5,.5,1],np.float32),(4,4,3))
    maps={name:base,name+'_n':normal,name+'_s':spec,name+'_r':rough}
    emissionfactor=np.asarray(mat.get('emissiveFactor',[0,0,0]))
    if np.max(emissionfactor)>0:
        if 'emissiveTexture' in mat:
            em=read_image(doc,binary,mat['emissiveTexture'],source,cache)[...,:3]
            em=resize(linear(em)*emissionfactor,limit)
        else: em=np.broadcast_to(emissionfactor,(4,4,3))
        maps[name+'_e']=srgb(em)
    dims={key:save_texture(resources/(key+'.tga'),value) for key,value in maps.items()}
    lines=['renderhint NormalAndSpecMapped','texture0 '+name,'texture1 '+name+'_n','texture2 '+name+'_s','texture3 '+name+'_r']
    if name+'_e' in maps: lines.append('texture5 '+name+'_e')
    (resources/(name+'.mtr')).write_text('\n'.join(lines)+'\n',encoding='ascii')
    return dims, maps


def preview_material(resources,name,maps):
    mat=bpy.data.materials.new(name+'_nwn_preview');mat.use_nodes=True
    nodes,links=mat.node_tree.nodes,mat.node_tree.links; shader=nodes.get('Principled BSDF')
    def image_node(key,color=False):
        node=nodes.new('ShaderNodeTexImage');node.image=bpy.data.images.load(str(resources/(key+'.tga')),check_existing=False)
        node.image.colorspace_settings.name='sRGB' if color else 'Non-Color'
        return node.outputs['Color']
    links.new(image_node(name,True),shader.inputs['Base Color'])
    links.new(image_node(name+'_r'),shader.inputs['Roughness'])
    mult=nodes.new('ShaderNodeMath');mult.operation='MULTIPLY';mult.inputs[1].default_value=3
    links.new(image_node(name+'_s'),mult.inputs[0])
    sub=nodes.new('ShaderNodeMath');sub.operation='SUBTRACT';sub.inputs[1].default_value=.6;sub.use_clamp=True
    links.new(mult.outputs[0],sub.inputs[0]);links.new(sub.outputs[0],shader.inputs['Metallic'])
    normal=nodes.new('ShaderNodeNormalMap');links.new(image_node(name+'_n'),normal.inputs['Color']);links.new(normal.outputs['Normal'],shader.inputs['Normal'])
    if name+'_e' in maps: links.new(image_node(name+'_e',True),shader.inputs['Emission Color'])
    return mat


def bounds(objects):
    points=[ob.matrix_world @ v.co for ob in objects for v in ob.data.vertices]
    lo=Vector(tuple(min(v[i] for v in points) for i in range(3)))
    hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
    return points,lo,hi


def shared_icon_frame(points, frame):
    low=Vector(vector3(frame['min'],'icon_bounds min'));high=Vector(vector3(frame['max'],'icon_bounds max'))
    if any(high[i]<=low[i] for i in range(3)):raise ValueError('icon_bounds must have positive dimensions')
    if any(p[i]<low[i]-1e-6 or p[i]>high[i]+1e-6 for p in points for i in range(3)):
        raise ValueError('Exported geometry falls outside shared icon_bounds')
    corners=[Vector((x,y,z)) for x in (low.x,high.x) for y in (low.y,high.y) for z in (low.z,high.z)]
    return corners,(low+high)/2,max(high-low)


def render_setup(objects, icon_bounds=None):
    scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.eevee.taa_render_samples=96
    scene.world.color=(.12,.12,.12);scene.view_settings.view_transform='Standard'
    scene.render.film_transparent=True;scene.render.resolution_percentage=100
    scene.render.image_settings.color_mode='RGBA'
    points,lo,hi=bounds(objects);center=(lo+hi)/2;span=max(hi-lo)
    if icon_bounds is not None:points,center,span=shared_icon_frame(points,icon_bounds)
    for direction,energy in [((2,-2,3),350),((-2,0,1),220),((1,3,-1),280)]:
        bpy.ops.object.light_add(type='AREA',location=center+Vector(direction)*span)
        light=bpy.context.object;light.data.energy=energy*span*span;light.data.size=span*2
        light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=center+Vector((2,0,0))*span)
    camera=bpy.context.object;camera.data.type='ORTHO';camera.data.clip_start=max(span*.0001,.00001);camera.data.clip_end=span*100
    scene.camera=camera
    return scene,camera,points,center,span


def render_view(scene,camera,points,center,span,direction,up,path,size=(900,900),roll=0):
    back=Vector(direction).normalized();right=Vector(up).cross(back).normalized();actual_up=back.cross(right).normalized()
    if right.length<.9: raise ValueError('Camera direction and up are parallel')
    rotation=Matrix((right,actual_up,back)).transposed().to_quaternion() @ Quaternion((0,0,1),math.radians(roll))
    camera.location=center+back*span*4;camera.rotation_euler=rotation.to_euler()
    projected=[rotation.inverted() @ (p-center) for p in points]
    width=max(p.x for p in projected)-min(p.x for p in projected)
    height=max(p.y for p in projected)-min(p.y for p in projected)
    # Recenter projected geometry, not merely the 3D AABB.
    dx=(max(p.x for p in projected)+min(p.x for p in projected))/2
    dy=(max(p.y for p in projected)+min(p.y for p in projected))/2
    camera.location+=rotation @ Vector((dx,dy,0))
    camera.data.ortho_scale=max(height,width*size[1]/size[0])*1.16
    scene.render.resolution_x,scene.render.resolution_y=size
    scene.render.image_settings.file_format='TARGA_RAW' if path.suffix=='.tga' else 'PNG'
    scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)


def contact_sheet(paths,path,cell=320):
    arrays=[]
    for source in paths:
        im=bpy.data.images.load(str(source),check_existing=False);im.scale(cell,cell)
        pixels=np.empty(cell*cell*4,np.float32);im.pixels.foreach_get(pixels)
        arrays.append(pixels.reshape(cell,cell,4));bpy.data.images.remove(im)
    if not arrays:return
    rgba=np.concatenate(arrays,axis=1)
    im=bpy.data.images.new(path.stem,width=rgba.shape[1],height=cell,alpha=True)
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(path);im.file_format='PNG';im.save();bpy.data.images.remove(im)


def base_item_row(repo,base_item):
    if type(base_item) is not int or base_item<0:
        raise ValueError('base_item must be a native nonnegative row ID')
    lines=(repo/'SWLOR_Haks/sw_2da/baseitems.2da').read_text().splitlines()
    headers=next(shlex.split(line, posix=False) for line in lines if 'InvSlotWidth' in line)
    rows=[shlex.split(line,posix=False) for line in lines if re.match(r'^'+str(base_item)+r'\s',line)]
    if len(rows)!=1 or len(rows[0])!=len(headers)+1:
        raise ValueError('Missing/ambiguous/incomplete baseitem row '+str(base_item))
    values=dict(zip(headers,rows[0][1:]))
    family=values['ItemClass'].lower();model_type=int(values['ModelType'])
    weapon=model_type in (0,2) and re.fullmatch(r'w[a-z0-9_]{2,9}',family)
    shield=model_type==0 and family=='ashlw'
    if not (weapon or shield):
        raise ValueError('Expected simple/composite weapon or simple large shield baseitem')
    return values


def validate_model_name(model,item_class,model_type):
    pattern=re.escape(item_class)+(r'_[bmt]_\d{3}' if model_type==2 else r'_\d{3}')
    if not isinstance(model,str) or len(model)>15 or not re.fullmatch(pattern,model):raise ValueError('Model resource does not match baseitem family: '+model)


def attachment_dummy_nodes(config,model):
    if 'attachment_dummies' not in config:
        if config['_item_class']=='ashlw':
            raise ValueError('Expected both native shield impact attachment_dummies')
        return ''
    dummies=config['attachment_dummies']
    if config['_item_class']!='ashlw' or config['_model_type']!=0:
        raise ValueError('attachment_dummies requires a simple large shield')
    if not isinstance(dummies,list) or len(dummies)!=2 or any(not isinstance(d,dict) for d in dummies):
        raise ValueError('Expected both native shield impact attachment_dummies')
    if {d.get('name') for d in dummies}!={'imp_shield_1','imp_shield_2'}:
        raise ValueError('Expected unique imp_shield_1 and imp_shield_2 attachment_dummies')
    lines=[]
    for dummy in dummies:
        position=vector3(dummy.get('position'),'attachment dummy position')
        q=dummy.get('quaternion')
        if not isinstance(q,list) or len(q)!=4 or not all(number(v) for v in q):
            raise ValueError('attachment dummy quaternion must be finite XYZW')
        length=math.sqrt(sum(v*v for v in q))
        if abs(length-1)>1e-4:raise ValueError('attachment dummy quaternion must be unit length')
        q=[v/length for v in q]
        if q[3]<0:q=[-v for v in q]
        sine=math.sqrt(sum(v*v for v in q[:3]))
        axis=[v/sine for v in q[:3]] if sine>1e-12 else [0.,0.,1.]
        angle=2*math.atan2(sine,q[3]) if sine>1e-12 else 0.
        lines += [f"node dummy {dummy['name']}",f'  parent {model}',
                  '  position '+' '.join(f'{v:.9g}' for v in position),
                  '  orientation '+' '.join(f'{v:.9g}' for v in [*axis,angle]),'endnode']
    return '\n'.join(lines)+'\n'


def compact_subset(exported,triangles,material_index):
    verts,uvs,faces,normals=exported;chosen=[];removed=0
    for tri,face in zip(triangles,faces):
        if tri.material_index!=material_index:continue
        a,b,c=(Vector(verts[i]) for i in face)
        if (b-a).cross(c-a).length_squared==0:removed+=1;continue
        chosen.append(face)
    if not chosen:raise ValueError('Material subset has no nondegenerate triangles')
    used=sorted({i for face in chosen for i in face});mapping={old:new for new,old in enumerate(used)}
    return ([verts[i] for i in used],[uvs[i] for i in used],[tuple(mapping[i] for i in f) for f in chosen],[normals[i] for i in used]),removed


def component_reports(objects):
    reports=[]
    for ob in objects:
        vertices=[ob.matrix_world @ v.co for v in ob.data.vertices]
        parents=list(range(len(vertices))); keys={}
        span=max(max(v[i] for v in vertices)-min(v[i] for v in vertices) for i in range(3))
        tolerance=max(span*1e-7,1e-10)
        def find(a):
            while parents[a]!=a:
                parents[a]=parents[parents[a]];a=parents[a]
            return a
        def union(a,b):parents[find(a)]=find(b)
        for i,v in enumerate(vertices):
            key=tuple(round(c/tolerance) for c in v)
            if key in keys:union(i,keys[key])
            else:keys[key]=i
        for tri in ob.data.loop_triangles:
            union(tri.vertices[0],tri.vertices[1]);union(tri.vertices[0],tri.vertices[2])
        groups={}
        for i in range(len(vertices)):groups.setdefault(find(i),{'vertices':[],'triangles':0,'area':0})['vertices'].append(i)
        for tri in ob.data.loop_triangles:
            g=groups[find(tri.vertices[0])];g['triangles']+=1
            a,b,c=(vertices[i] for i in tri.vertices);g['area']+=(b-a).cross(c-a).length/2
        for rank,g in enumerate(sorted(groups.values(),key=lambda x:x['triangles'],reverse=True)):
            points=[vertices[i] for i in g['vertices']]
            lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)]
            reports.append({'object':ob.name,'component_rank':rank,'vertices':len(points),'triangles':g['triangles'],'surface_area':g['area'],'bounds_min':lo,'bounds_max':hi,'center':[(a+b)/2 for a,b in zip(lo,hi)]})
    return reports


SOURCE_MATERIAL_INDEX = '_trellis_source_material_index'


def tag_import_materials(document):
    """Carry GLB material identity through importer variants and Blender renames."""
    for index,material in enumerate(document.get('materials',[])):
        material['name']=f'trellis_material_{index}'
        extras=material.get('extras')
        material['extras']={**(extras if isinstance(extras,dict) else {}),SOURCE_MATERIAL_INDEX:index}


def imported_material_index(material,material_count):
    index=material.get(SOURCE_MATERIAL_INDEX) if material is not None else None
    if type(index) is not int or not 0<=index<material_count:
        raise ValueError('Imported material has no valid GLB material identity')
    return index


def ray_in_wedge(direction,u,w,tol=1e-9):
    """+1 when the ray from the wedge apex along direction (which lies in the wedge's plane) is inside the wedge spanned
    by u and w, -1 when the opposite ray is, 0 when neither is (boundary rays count as inside)."""
    uu,uw,ww=u@u,u@w,w@w;du,dw=direction@u,direction@w;det=uu*ww-uw*uw
    a=(du*ww-dw*uw)/det*math.sqrt(uu);b=(dw*uu-du*uw)/det*math.sqrt(ww)  # coefficients scaled to lengths
    if a>=-tol and b>=-tol and max(a,b)>tol:return 1
    if a<=tol and b<=tol and min(a,b)<-tol:return -1
    return 0


def faces_meet_beyond_shared(points,first,second,shared,tol=1e-9):
    """Exact test for two faces of one shell that share one vertex or one edge: do they meet anywhere else?"""
    norm=np.linalg.norm
    if len(shared)==2:
        a,b=(points[i] for i in shared);c=points[next(i for i in first if i not in shared)];d=points[next(i for i in second if i not in shared)]
        n1=np.cross(b-a,c-a);n2=np.cross(b-a,d-a)
        # Planes through a common edge meet only on that edge unless they coincide and fold onto the same side.
        return bool(norm(np.cross(n1,n2))<=tol*norm(n1)*norm(n2) and n1@n2>0)
    v=next(iter(shared));o1=[points[i]-points[v] for i in first if i!=v];o2=[points[i]-points[v] for i in second if i!=v]
    n1=np.cross(*o1);n2=np.cross(*o2);line=np.cross(n1,n2)
    if norm(line)>tol*norm(n1)*norm(n2):
        # Non-coplanar: each face meets the planes' common line in a segment starting at the shared vertex; the faces
        # meet beyond it exactly when both segments leave in the same direction.
        line=line/norm(line);side=ray_in_wedge(line,*o1);return side!=0 and side==ray_in_wedge(line,*o2)
    e1=o1[0]/norm(o1[0]);e2=np.cross(n1/norm(n1),e1)
    def arc(u,w):
        x,y=(math.atan2(t@e2,t@e1) for t in (u,w));span=(y-x)%(2*math.pi)
        return (x,span) if span<math.pi else (y,2*math.pi-span)
    def inside(angle,start,span):return tol<(angle-start)%(2*math.pi)<span-tol
    (s1,l1),(s2,l2)=arc(*o1),arc(*o2)
    def same(x,y):return abs((x-y+math.pi)%(2*math.pi)-math.pi)<=tol
    # Overlapping wedges, or any common boundary ray (faces then touch along a segment from the vertex).
    return inside(s2,s1,l1) or inside(s1,s2,l2) or any(same(x,y) for x in (s1,s1+l1) for y in (s2,s2+l2))


def triangles_touch(t1,t2,tol=1e-9):
    """Closed-triangle contact for paired arrays (n,3,3) by Moller's interval test. Each triangle's vertices are
    classified against the other's plane with a distance tolerance scaled to the pair's size; a pair that neither
    plane separates meets exactly when the two triangles' intervals on the planes' common line overlap. Coplanar
    pairs use 2D separating axes. No determinant threshold drops shallow crossings."""
    scale=np.maximum(np.linalg.norm(t1[:,[1,2,0]]-t1,axis=2).max(1),np.linalg.norm(t2[:,[1,2,0]]-t2,axis=2).max(1));eps=tol*scale
    def unit_normal(t):
        n=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);return n/np.maximum(np.linalg.norm(n,axis=1),1e-300)[:,None]
    n1=unit_normal(t1);n2=unit_normal(t2)
    d1=np.einsum('nkj,nj->nk',t1-t2[:,:1],n2);d2=np.einsum('nkj,nj->nk',t2-t1[:,:1],n1)  # signed plane distances
    s1=np.where(np.abs(d1)<=eps[:,None],0,np.sign(d1));s2=np.where(np.abs(d2)<=eps[:,None],0,np.sign(d2))
    separated=(np.abs(s1.sum(1))==3)|(np.abs(s2.sum(1))==3)
    line=np.cross(n1,n2);length=np.linalg.norm(line,axis=1)
    coplanar=~separated&((np.all(s1==0,1)&np.all(s2==0,1))|(length<=1e-12))
    touch=np.zeros(len(t1),bool);general=~separated&~coplanar
    if general.any():
        direction=line[general]/length[general,None]
        def interval(t,d,s):
            # Points of the triangle on the other plane: on-plane vertices and sign-changing edge crossings.
            p=np.einsum('nkj,nj->nk',t,direction);points=[np.where(s[:,k]==0,p[:,k],np.nan) for k in range(3)]
            for a,b in ((0,1),(1,2),(2,0)):
                change=s[:,a]*s[:,b]<0;f=np.where(change,d[:,a]/np.where(change,d[:,a]-d[:,b],1),np.nan)
                points.append(p[:,a]+(p[:,b]-p[:,a])*f)
            points=np.stack(points,1);missing=np.isnan(points)
            return np.where(missing,np.inf,points).min(1),np.where(missing,-np.inf,points).max(1)
        lo1,hi1=interval(t1[general],d1[general],s1[general]);lo2,hi2=interval(t2[general],d2[general],s2[general])
        touch[general]=(lo1<=hi1)&(lo2<=hi2)&(np.maximum(lo1,lo2)<=np.minimum(hi1,hi2)+eps[general])
    if coplanar.any():
        axis=np.argmax(np.abs(n1[coplanar]),axis=1);keep=np.array([[1,2],[0,2],[0,1]])[axis]
        a=np.take_along_axis(t1[coplanar],keep[:,None,:],2);b=np.take_along_axis(t2[coplanar],keep[:,None,:],2);apart=np.zeros(len(a),bool)
        for tri in (a,b):
            for i in range(3):
                edge=tri[:,(i+1)%3]-tri[:,i];normal=np.stack((-edge[:,1],edge[:,0]),1)
                pa=np.einsum('nkj,nj->nk',a,normal);pb=np.einsum('nkj,nj->nk',b,normal);slack=eps[coplanar]*np.linalg.norm(normal,axis=1)
                apart|=(pa.max(1)<pb.min(1)-slack)|(pb.max(1)<pa.min(1)-slack)
        touch[coplanar]=~apart
    return touch


def shell_crosses_itself(vertices,faces,batch=200000):
    """True when two faces of one shell meet anywhere beyond the vertex or edge they share. A bounding-box sweep finds
    candidate pairs, an exact closed-triangle test keeps those in contact, and vertex- or edge-sharing pairs are then
    classified exactly, so coplanar folds and crossings right beside a shared vertex are all caught. Candidates are
    produced and tested in batches of at most `batch` pairs, so a dense fan around one apex stays in bounded memory."""
    if not faces:return False
    index=sorted({i for face in faces for i in face});origin=np.asarray(vertices[faces[0][0]],dtype=np.float64)
    points={i:np.asarray(vertices[i],dtype=np.float64)-origin for i in index}  # shell-local precision
    tris=np.asarray([[points[i] for i in face] for face in faces]);pad=1e-9*max(float(np.ptp(tris.reshape(-1,3),axis=0).max()),1e-30)
    lo=tris.min(1)-pad;hi=tris.max(1)+pad;order=np.argsort(lo[:,0]);sorted_lo=lo[order,0];F=np.asarray(faces)
    def candidates():
        first=[];second=[];size=0
        for k,i in enumerate(order):
            j=order[k+1:np.searchsorted(sorted_lo,hi[i,0],side='right')]
            j=j[np.all((lo[j,1:]<=hi[i,1:])&(lo[i,1:]<=hi[j,1:]),axis=1)]
            while len(j):
                take=j[:batch-size];j=j[len(take):];first.append(np.full(len(take),i));second.append(take);size+=len(take)
                if size>=batch:yield np.concatenate(first),np.concatenate(second);first=[];second=[];size=0
        if size:yield np.concatenate(first),np.concatenate(second)
    return any(batch_crosses(points,faces,F,tris,first,second) for first,second in candidates())


def batch_crosses(points,faces,F,tris,first,second):
    """Exact crossing test for one batch of candidate face pairs."""
    eq=F[first][:,:,None]==F[second][:,None,:];count=eq.sum((1,2))
    if np.any(count==3):return True
    # Faces sharing nothing cross exactly when their closed triangles touch; faces sharing a vertex or edge always
    # touch there, so they go straight to the exact shared-boundary classification below.
    apart=count==0
    if apart.any() and triangles_touch(tris[first[apart]],tris[second[apart]]).any():return True
    I=first[~apart];J=second[~apart];eq=eq[~apart];count=count[~apart]
    rows=np.arange(len(I));ti=tris[I];tj=tris[J];norm=lambda x:np.linalg.norm(x,axis=-1);tol=1e-9
    # Edge-sharing pairs meet off their edge only when coplanar and folded onto the same side.
    e=count==2;ri=np.argmin(eq.any(2),1);rj=np.argmin(eq.any(1),1)
    a=ti[rows,(ri+1)%3];b=ti[rows,(ri+2)%3];n1=np.cross(b-a,ti[rows,ri]-a);n2=np.cross(b-a,tj[rows,rj]-a)
    if np.any(e&(norm(np.cross(n1,n2))<=tol*norm(n1)*norm(n2))&(np.einsum('ij,ij->i',n1,n2)>0)):return True
    # Vertex-sharing pairs: the faces meet beyond the vertex when both contain the same ray along their common line.
    v=count==1;ri=np.argmax(eq.any(2),1);rj=np.argmax(eq.any(1),1);apex=ti[rows,ri]
    u1=ti[rows,(ri+1)%3]-apex;w1=ti[rows,(ri+2)%3]-apex;u2=tj[rows,(rj+1)%3]-apex;w2=tj[rows,(rj+2)%3]-apex
    n1=np.cross(u1,w1);n2=np.cross(u2,w2);line=np.cross(n1,n2);flat=norm(line)<=tol*norm(n1)*norm(n2)
    line=line/np.maximum(norm(line),1e-300)[:,None]
    def side(u,w):
        uu,uw,ww=(np.einsum('ij,ij->i',x,y) for x,y in ((u,u),(u,w),(w,w)));du=np.einsum('ij,ij->i',line,u);dw=np.einsum('ij,ij->i',line,w)
        det=np.where(np.abs(uu*ww-uw*uw)>0,uu*ww-uw*uw,1);c1=(du*ww-dw*uw)/det*np.sqrt(uu);c2=(dw*uu-du*uw)/det*np.sqrt(ww)
        return np.where((c1>=-tol)&(c2>=-tol)&(np.maximum(c1,c2)>tol),1,np.where((c1<=tol)&(c2<=tol)&(np.minimum(c1,c2)<-tol),-1,0))
    s1=side(u1,w1);s2=side(u2,w2)
    if np.any(v&~flat&(s1!=0)&(s1==s2)):return True
    for k in np.flatnonzero(v&flat):  # coplanar fans are rare: classify their wedges one by one
        i,j=I[k],J[k]
        if faces_meet_beyond_shared(points,faces[i],faces[j],set(faces[i])&set(faces[j])):return True
    return False


def closed_surface_report(objects,double_sided_materials=None,material_count=None):
    """Prove culling safe on complete components containing affected source faces."""
    vertices=[];faces=[];lookup={};affected=[];face_areas=[];opposed_faces=[];degenerate=[]
    for ob in objects:
        ob.data.calc_loop_triangles();ob.data.calc_normals_split()
        normal_transform=ob.matrix_world.to_3x3().inverted().transposed()
        for triangle in ob.data.loop_triangles:
            if double_sided_materials is None:affected.append(True)
            else:
                material=ob.data.materials[triangle.material_index] if triangle.material_index<len(ob.data.materials) else None
                affected.append(imported_material_index(material,material_count) in double_sided_materials)
            ids=[]
            for index in triangle.vertices:
                point=tuple(ob.matrix_world@ob.data.vertices[index].co)
                if point not in lookup:lookup[point]=len(vertices);vertices.append(Vector(point))
                ids.append(lookup[point])
            a,b,c=(vertices[i] for i in ids);cross=(b-a).cross(c-a)
            degenerate.append(cross.length_squared==0)
            mean=sum((normal_transform@ob.data.corner_normals[i].vector for i in triangle.loops),Vector((0,0,0)))
            face_areas.append(cross.length/2);opposed_faces.append(cross.dot(mean)<=0)
            faces.append(ids)
    edges={};parents=list(range(len(faces)))
    def find(index):
        while parents[index]!=index:parents[index]=parents[parents[index]];index=parents[index]
        return index
    for index,face in enumerate(faces):
        for a,b in zip(face,face[1:]+face[:1]):edges.setdefault(tuple(sorted((a,b))),[]).append((index,1 if a<b else -1))
    for values in edges.values():
        for index,_ in values[1:]:parents[find(index)]=find(values[0][0])
    selected={find(i) for i,changed in enumerate(affected) if changed}
    invalid={find(values[0][0]) for values in edges.values() if len(values)!=2 or sum(sign for _,sign in values)!=0}
    degenerate_roots={find(i) for i,bad in enumerate(degenerate) if bad}
    if selected & degenerate_roots:raise ValueError('Closed-surface conversion rejects degenerate faces')
    if (not faces and double_sided_materials is None) or selected & invalid:
        raise ValueError('Double-sided source is not an exactly closed, consistently wound surface')
    all_volumes={};all_groups={};origins={}
    for index,face in enumerate(faces):
        root=find(index);origin=origins.setdefault(root,vertices[face[0]])
        # Signed volume about a point on the shell itself: a far-translated small shell keeps its precision.
        a,b,c=(vertices[i]-origin for i in face)
        all_volumes[root]=all_volumes.get(root,0)+a.dot(b.cross(c))/6
        all_groups.setdefault(root,[]).append(face)
    volumes={root:all_volumes[root] for root in selected}
    if any(v==0 for v in volumes.values()):raise ValueError('Double-sided source contains a zero-volume shell')
    # TRELLIS can retain closed inward cavity shells. They are safe to cull only
    # when fully enclosed by a nonintersecting outward shell, never exposed sheets.
    from mathutils.bvhtree import BVHTree
    # Unchanged outward shells may enclose an affected cavity, but unrelated open
    # components neither provide enclosure evidence nor block the conversion.
    enclosing={root:volume for root,volume in all_volumes.items() if volume>0 and root not in invalid|degenerate_roots}
    groups={root:all_groups[root] for root in selected|set(enclosing)}
    trees={root:BVHTree.FromPolygons(vertices,group,all_triangles=True) for root,group in groups.items()}
    # A shell that crosses itself keeps back faces in view, so culling it is unsafe even when every edge
    # pairs up and its volume is nonzero.
    for root in selected:
        if shell_crosses_itself(vertices,groups[root]):raise ValueError('Double-sided source shell intersects itself')
    inward=[root for root,volume in volumes.items() if volume<0]
    # Validate each cavity against every other closed component together: it must not touch
    # any of them, and the aggregate signed winding of all of them around it must be exactly one
    # (solid material). Nested cavities, cavities in cavities and crossing cavities all fail.
    # Every valid closed component counts, including unselected inward shells (existing voids).
    closed=[root for root in all_groups if root not in invalid|degenerate_roots]
    for root in closed:
        if root not in trees:trees[root]=BVHTree.FromPolygons(vertices,all_groups[root],all_triangles=True)
    for root in inward:
        others=[other for other in closed if other!=root]
        if not others or any(trees[root].overlap(trees[other]) for other in others):
            raise ValueError('Inward shell is not proven enclosed by a nonintersecting outward shell')
        point=np.asarray(vertices[groups[root][0][0]],dtype=np.float64)
        triangles=np.asarray([[vertices[i] for i in face] for other in others for face in all_groups[other]],dtype=np.float64)-point
        a,b,c=triangles[:,0],triangles[:,1],triangles[:,2]
        la,lb,lc=(np.linalg.norm(v,axis=1) for v in (a,b,c))
        numerator=np.einsum('ij,ij->i',a,np.cross(b,c))
        denominator=la*lb*lc+np.einsum('ij,ij->i',a,b)*lc+np.einsum('ij,ij->i',b,c)*la+np.einsum('ij,ij->i',c,a)*lb
        winding=float(np.sum(2*np.arctan2(numerator,denominator))/(4*math.pi))
        if abs(winding-1)>=1e-5:raise ValueError('Inward shell is not proven enclosed by a nonintersecting outward shell')
    selected_faces=[i for i in range(len(faces)) if find(i) in selected]
    area=sum(face_areas[i] for i in selected_faces);opposed=sum(opposed_faces[i] for i in selected_faces)
    opposed_area=sum(face_areas[i] for i in selected_faces if opposed_faces[i])
    return {'conversion':'doubleSided=true to native single-sided; closed coherent outer shells and enclosed cavities',
            'inward_shells_validated_as_internal':len(inward),
            'triangles':len(selected_faces),'unaffected_triangles':len(faces)-len(selected_faces),'shells':len(volumes),'signed_shell_volumes':list(volumes.values()),
            'boundary_edges':0,'nonmanifold_edges':0,'inconsistent_winding_edges':0,
            'opposed_mean_corner_normal_faces':opposed,'opposed_mean_corner_normal_area_fraction':opposed_area/area if area else 0.,
            'authored_normals_preserved':True}


def normalize_clipped_normals(mesh):
    normals=[]
    for normal in mesh[3]:
        value=Vector(normal)
        if not all(math.isfinite(x) for x in value) or value.length<1e-8:
            raise ValueError('Clipping produced an undefined corner normal')
        normals.append(tuple(value.normalized()))
    return mesh[0],mesh[1],mesh[2],normals


def clip_mesh_y(mesh, plane, side, normalize_normals=True):
    if side not in ('above','below'):raise ValueError('clip_side must be above or below')
    vertices,uvs,faces,normals=mesh;outv=[];outu=[];outf=[];outn=[];lookup={}
    sign=1 if side=='above' else -1
    def distance(corner):return sign*(corner[0][1]-plane)
    def crossing(a,b):
        t=(plane-a[0][1])/(b[0][1]-a[0][1])
        pos=a[0].lerp(b[0],t);pos.y=plane
        uv=a[1].lerp(b[1],t);normal=a[2].lerp(b[2],t)
        return pos,uv,normal
    def index(corner):
        key=tuple(tuple(value) for value in corner)
        if key not in lookup:
            lookup[key]=len(outv);outv.append(key[0]);outu.append(key[1]);outn.append(key[2])
        return lookup[key]
    for face in faces:
        polygon=[(Vector(vertices[i]),Vector(uvs[i]),Vector(normals[i])) for i in face];clipped=[]
        # Shared-plane faces belong to the above part only. Keep on-plane corners
        # inclusive for crossing faces so both parts retain the same seam data.
        if side=='below' and all(distance(corner)==0 for corner in polygon):continue
        for a,b in zip(polygon,polygon[1:]+polygon[:1]):
            ina=distance(a)>=0;inb=distance(b)>=0
            if ina and inb:clipped.append(b)
            elif ina:clipped.append(crossing(a,b))
            elif inb:clipped.extend([crossing(a,b),b])
        for i in range(1,len(clipped)-1):
            corners=[clipped[0],clipped[i],clipped[i+1]]
            if (corners[1][0]-corners[0][0]).cross(corners[2][0]-corners[0][0]).length_squared==0:continue
            outf.append(tuple(index(corner) for corner in corners))
    result=(outv,outu,outf,outn)
    return normalize_clipped_normals(result) if normalize_normals else result


def remove_imported_lights():
    """Source GLB lights and cameras must not reach review renders or shipped icons."""
    for ob in [ob for ob in bpy.context.scene.objects if ob.type in ('LIGHT','CAMERA')]:bpy.data.objects.remove(ob,do_unlink=True)


def mesh_preview(mesh,name,material):
    vertices,uvs,faces,normals=mesh;m=bpy.data.meshes.new(name)
    m.from_pydata(vertices,[],faces);m.update();layer=m.uv_layers.new()
    for loop in m.loops:layer.data[loop.index].uv=uvs[loop.vertex_index]
    for polygon in m.polygons:polygon.use_smooth=True
    m.use_auto_smooth=True;m.normals_split_custom_set([normals[loop.vertex_index] for loop in m.loops])
    m.materials.append(material);ob=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(ob)
    return ob


def clip_part_mesh(mesh, part):
    planes=[]
    if 'clip_side' in part:planes.append((float(part['plane_y']),part['clip_side']))
    if 'clip_min_y' in part:planes.append((float(part['clip_min_y']),'above'))
    if 'clip_max_y' in part:planes.append((float(part['clip_max_y']),'below'))
    result=mesh
    # Keep linear corner attributes through every plane; normalize only the final field.
    for plane,side in planes:result=clip_mesh_y(result,plane,side,normalize_normals=False)
    return normalize_clipped_normals(result) if planes else result


def write_parts(config,resources,helpers,mesh_data,mesh_names,materials):
    default=config["model"]
    parts=config.get('parts',[{'model':default}]);objects=[];reports=[]
    for part in parts:
        model=part['model']
        validate_model_name(model,config['_item_class'],config['_model_type'])
        selected=[];textures=[]
        for mesh,texture in zip(mesh_data,mesh_names):
            result=clip_part_mesh(mesh,part)
            if result[2]:
                selected.append(result);textures.append(texture)
                ob=mesh_preview(result,model+'_'+str(len(selected)),materials[texture]);ob['weapon_part']=model;objects.append(ob)
        if not selected:raise ValueError('Empty selected part '+model)
        mdl=resources/(model+'.mdl');helpers.write_mdl(mdl,model,'material_pending',selected)
        text=mdl.read_text();counter=[0]
        def material_block(match):
            i=counter[0];counter[0]+=1
            return match.group(0).replace('material_pending',textures[i]).replace('blaster'+str(i),'weapon'+str(i),1)
        text=re.sub(r'node trimesh.*?endnode',material_block,text,flags=re.S)
        if counter[0]!=len(textures) or 'material_pending' in text:raise ValueError('Exporter material binding mismatch')
        text=text.replace(f'endmodelgeom {model}',attachment_dummy_nodes(config,model)+f'endmodelgeom {model}')
        mdl.write_text(text,encoding='ascii')
        points=[Vector(v) for mesh in selected for v in mesh[0]]
        reports.append({'model':model,'clip':part,'materials':sorted(set(textures)),'triangles':sum(len(m[2]) for m in selected),'bounds_min':[min(v[i] for v in points) for i in range(3)],'bounds_max':[max(v[i] for v in points) for i in range(3)]})
    return objects,reports


def drop_clipped_materials(resources,names,material_dims,mapdims,retained,basis_path,basis):
    """Materials clipped out of every exported part get no maps or normal provenance."""
    dropped={index:name for index,name in names.items() if name not in retained}
    for index,name in dropped.items():
        for key in material_dims.pop(index):(resources/(key+'.tga')).unlink();mapdims.pop(key)
        (resources/(name+'.mtr')).unlink();del names[index]
    if dropped:
        with np.load(basis_path,allow_pickle=False) as arrays:
            kept={key:arrays[key] for key in arrays.files if key.rsplit('__',1)[0] not in dropped.values()}
        np.savez_compressed(basis_path,**kept)
        basis.update(sha256=hashlib.sha256(basis_path.read_bytes()).hexdigest(),materials=[m for m in basis['materials'] if m not in dropped.values()])
    return sorted(dropped.values())


def validate_attachment(config,rotation):
    attachment=config.get('attachment')
    if not isinstance(attachment,dict) or not attachment.get('review_notes'):
        raise ValueError('Explicit attachment axis pairs and review_notes required before export')
    pairs=attachment.get('axis_pairs',[])
    if len(pairs)<2:raise ValueError('Two independent source/target axes required (long+edge or plane normal+up)')
    source_axes=[]
    for pair in pairs:
        src=finite3(pair['source'],'source axis').normalized();target=finite3(pair['target'],'target axis').normalized()
        if src.length<.99 or target.length<.99:raise ValueError('Zero attachment axis')
        if ((rotation.to_3x3()@src).normalized()).dot(target)<.995:raise ValueError('Rotation does not match reviewed attachment axes')
        source_axes.append(src)
    if source_axes[0].cross(source_axes[1]).length<.2:raise ValueError('Attachment axis pairs must be independent')


def pad_icon(path, size):
    """Keep logical artwork at the native canvas top; never stretch it."""
    im=bpy.data.images.load(str(path),check_existing=False)
    im.colorspace_settings.name='Non-Color'
    width,height=im.size;target_width,target_height=size
    if width>target_width or height>target_height:
        raise ValueError('Native icon canvas is smaller than its logical art area')
    pixels=np.empty(width*height*4,np.float32);im.pixels.foreach_get(pixels)
    pixels=pixels.reshape(height,width,4)
    padded=np.zeros((target_height,target_width,4),np.float32)
    padded[target_height-height:,:width]=pixels
    bpy.data.images.remove(im)
    im=bpy.data.images.new(path.stem,width=target_width,height=target_height,alpha=True)
    im.colorspace_settings.name='Non-Color'
    im.pixels.foreach_set(padded.ravel());im.filepath_raw=str(path);im.file_format='TARGA_RAW';im.save()
    bpy.data.images.remove(im)


def alias_model_identifiers(text, primary, alias):
    """Rename generated ASCII model identity without changing mesh/material data."""
    fields=r'(?:newmodel|setsupermodel|beginmodelgeom|endmodelgeom|donemodel|parent|node[ \t]+dummy)'
    pattern=r'^([ \t]*'+fields+r'[ \t]+)'+re.escape(primary)+r'(?=[ \t\r\n]|$)'
    return re.sub(pattern,lambda match:match[1]+alias,text,flags=re.M)


def export_one(config,source,repo,out,mode,helpers,icon_size):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.world=bpy.data.worlds.new('Studio')
    # One snapshot of the source owns the hash check, parsed document and report hash,
    # so bytes that change after manifest validation can never be exported as validated.
    source_bytes=Path(source).read_bytes();source_sha=hashlib.sha256(source_bytes).hexdigest()
    if config.get('sha256') and source_sha!=config['sha256']:raise ValueError('Source GLB changed after manifest validation: '+str(source))
    doc,binary=glb_document(source_bytes);out.mkdir();cache=out/'source_images';cache.mkdir()
    # Label and tag materials only in the import copy; source bytes stay untouched.
    source_materials=json.loads(json.dumps(doc.get('materials',[])))
    policy=config.get('double_sided_policy','reject')
    if policy not in ('reject','closed_surface'):raise ValueError('Unknown double_sided_policy')
    if mode=='export':validate_glb_geometry(doc,policy=='closed_surface')
    imported=json.loads(json.dumps(doc))
    tag_import_materials(imported)
    import_path=cache/'import.glb';write_glb(import_path,imported,binary)
    bpy.ops.import_scene.gltf(filepath=str(import_path))
    remove_imported_lights()
    objects=[ob for ob in bpy.context.scene.objects if ob.type=='MESH']
    if not objects or any(ob.type=='ARMATURE' for ob in bpy.context.scene.objects):raise ValueError('Expected static rigid weapon meshes')
    for ob in objects:
        if ob.modifiers or (mode=='export' and not ob.data.uv_layers):raise ValueError('Modifiers or missing export UVs require explicit handling')
        if ob.matrix_world.to_3x3().determinant()<=0:raise ValueError('Mirrored source transform requires explicit winding review')
        ob.data.calc_loop_triangles()
    surface_conversion=None
    if mode=='export' and any(mat.get('doubleSided',False) for mat in doc['materials']):
        double_sided={i for i,mat in enumerate(doc['materials']) if mat.get('doubleSided',False)}
        surface_conversion=closed_surface_report(objects,double_sided,len(doc['materials']))
        for mat in doc['materials']:mat['doubleSided']=False
    points,lo,hi=bounds(objects)
    report={'name':config.get('name',source.stem),'source':str(source),'sha256':source_sha,'mode':mode,'coordinate_system':'Blender imported world XYZ; glTF importer axis conversion already included','source_bounds_min':list(lo),'source_bounds_max':list(hi),'dimensions':list(hi-lo),'materials':source_materials,'surface_conversion':surface_conversion,'components':component_reports(objects),'objects':[{'name':ob.name,'vertices':len(ob.data.vertices),'triangles':len(ob.data.loop_triangles),'materials':[m.name if m else None for m in ob.data.materials],'world_matrix':[list(row) for row in ob.matrix_world]} for ob in objects]}
    # Report and raw scene available even if a later material rule rejects export.
    (out/'report.json').write_text(json.dumps(report,indent=2))
    if mode=='export':
        scale=config.get('scale')
        if not isinstance(scale,(int,float)) or not math.isfinite(scale) or scale<=0:raise ValueError('Explicit positive uniform scale required')
        validate_orientation(config.get('orientation'))
        rotation=Matrix(config['orientation']).to_4x4()
        validate_attachment(config,rotation)
        fit=Matrix.Translation(finite3(config.get('target_grip'),'target_grip')) @ rotation @ Matrix.Scale(scale,4) @ Matrix.Translation(-finite3(config.get('source_grip'),'source_grip'))
        for ob in objects:ob.matrix_world=fit @ ob.matrix_world
        texture=config.get('texture','');limit=config.get('texture_size',1024)
        validate_model_name(config['model'],config['_item_class'],config['_model_type'])
        if not re.fullmatch('[a-z][a-z0-9_]{0,13}',texture):raise ValueError('Texture base must be lowercase and at most14 characters (map suffix space)' )
        if limit not in (512,1024):raise ValueError('texture_size must be512 or1024')
        resources=out/'resources';resources.mkdir();names={};previews={};mapdims={};material_dims={}
        material_count=len(doc['materials'])
        mesh_data=[];mesh_names=[];removed=0
        for ob in objects:
            exported=helpers.export_mesh(ob.data,ob.matrix_world)
            for mat_slot in sorted({t.material_index for t in ob.data.loop_triangles}):
                mat=ob.data.materials[mat_slot]
                index=imported_material_index(mat,material_count)
                if index not in names:
                    name=texture if material_count==1 else texture+f'_{index}'
                    if len(name)>14:raise ValueError('Material texture name exceeds14 characters')
                    dims,maps=material_maps(doc,binary,index,source,cache,resources,name,limit)
                    names[index]=name;mapdims.update(dims);material_dims[index]=dims;previews[index]=preview_material(resources,name,maps)
                mesh,zero=compact_subset(exported,ob.data.loop_triangles,mat_slot);removed+=zero
                mesh_data.append(mesh);mesh_names.append(names[index])
        # Collapse static subsets sharing a material without welding UV/normal seams.
        merged={}
        for mesh,name in zip(mesh_data,mesh_names):
            target=merged.setdefault(name,([],[],[],[]));offset=len(target[0])
            target[0].extend(mesh[0]);target[1].extend(mesh[1]);target[2].extend(tuple(i+offset for i in f) for f in mesh[2]);target[3].extend(mesh[3])
        mesh_names=list(merged);mesh_data=list(merged.values())
        normal_helper=load_module_snapshot('trellis_weapon_normals',Path(__file__).with_name('TrellisWeaponNormals.py'))
        report['source_normal_basis']=normal_helper.source_basis(doc,binary,config,names,out/'source_basis.npz')
        model=config['model']
        old_objects=objects
        objects,part_reports=write_parts(config,resources,helpers,mesh_data,mesh_names,{names[i]:previews[i] for i in names})
        for ob in old_objects:bpy.data.objects.remove(ob,do_unlink=True)
        # Normal reprojection needs a native UV surface for every recorded material.
        report['clipped_away_materials']=drop_clipped_materials(resources,names,material_dims,mapdims,{m for part in part_reports for m in part['materials']},out/'source_basis.npz',report['source_normal_basis'])
        points,lo,hi=bounds(objects)
        report.update(model=model,parts=part_reports,fit=config,bounds_min=list(lo),bounds_max=list(hi),materials_exported=names,map_dimensions=mapdims,removed_exact_zero_area_triangles=removed,export_triangles=sum(p['triangles'] for p in part_reports))
    if mode=='inspect' and not doc.get('materials'):
        neutral=bpy.data.materials.new('Neutral clay inspection only');neutral.use_nodes=True
        shader=neutral.node_tree.nodes.get('Principled BSDF');shader.inputs['Base Color'].default_value=(.18,.22,.26,1);shader.inputs['Roughness'].default_value=.65
        for ob in objects:ob.data.materials.clear();ob.data.materials.append(neutral)
        report['inspection_material_override']='Neutral clay, untextured source has no materials'
    scene,camera,points,center,span=render_setup(objects,config.get('_shared_icon_bounds') if mode=='export' else None)
    if mode=='inspect':views=[('view_x',(1,0,0),(0,0,1)),('view_y',(0,-1,0),(0,0,1)),('view_z',(0,0,1),(0,1,0))]
    else:views=[('view_front',(1,-.15,.08),(0,1,0)),('view_reverse',(-1,-.15,-.08),(0,1,0)),('view_edge',(0,0,1),(0,1,0))]
    paths=[]
    for name,direction,up in views:
        path=out/(name+'.png');render_view(scene,camera,points,center,span,direction,up,path);paths.append(path)
    contact_sheet(paths,out/'views.png')
    if mode=='export':
        for part in part_reports:
            part_model=part['model']
            for ob in objects:ob.hide_render=ob['weapon_part']!=part_model
            icon=resources/('i'+part_model+'.tga')
            render_view(scene,camera,points,center,span,config.get('icon_direction',[1,-.1,.12]),[0,1,0],icon,config['_icon_art_size'],config.get('icon_roll_degrees',0))
            pad_icon(icon,icon_size)
        for ob in objects:ob.hide_render=False
        render_view(scene,camera,points,center,span,config.get('icon_direction',[1,-.1,.12]),[0,1,0],out/'icon_preview.png',tuple(v*8 for v in config['_icon_art_size']),config.get('icon_roll_degrees',0))
        aliases={part['model']:part.get('aliases',[]) for part in config.get('parts',[{'model':config['model']}])}
        if config.get('aliases'):
            if len(part_reports)!=1:raise ValueError('Top-level aliases require a single complete model')
            aliases[part_reports[0]['model']]=aliases.get(part_reports[0]['model'],[])+config['aliases']
        for primary, targets in aliases.items():
            for alias in targets:
                validate_model_name(alias,config['_item_class'],config['_model_type'])
                text=(resources/(primary+'.mdl')).read_text()
                text=alias_model_identifiers(text,primary,alias)
                (resources/(alias+'.mdl')).write_text(text,encoding='ascii')
                (resources/('i'+alias+'.tga')).write_bytes((resources/('i'+primary+'.tga')).read_bytes())
        report['aliases']=config.get('aliases',[])
        report['icon_dimensions']=icon_size
        report['resources_sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in resources.iterdir()}
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'review.blend'))
    (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    return report,paths[0]




def validate_manifest(config, repo, manifest, mode):
    if config.get('schema_version')!=1 or not isinstance(config.get('assets'),list) or not config['assets']:
        raise ValueError('Expected schema_version 1 and nonempty assets')
    seen=set();asset_names=set();composites=[];outputs={};output_assets={};output_selections={}
    for item in config['assets']:
        item.pop('_shared_icon_bounds',None)
        asset=item.get('asset','')
        if not re.fullmatch(r'[a-z][a-z0-9_]{0,47}',asset) or asset in asset_names:
            raise ValueError('Asset identifiers must be unique lowercase names')
        asset_names.add(asset)
        if item.get('normal_basis_policy','reproject') not in ('reproject','authored_tangents'):raise ValueError('Unknown normal_basis_policy')
        source=(manifest.parent/item['source']).resolve()
        if source.suffix.lower()!='.glb' or not source.is_file():raise ValueError('Missing source GLB: '+str(source))
        if not re.fullmatch(r'[0-9a-f]{64}',item.get('sha256','')) or hashlib.sha256(source.read_bytes()).hexdigest()!=item['sha256']:
            raise ValueError('Source SHA-256 mismatch: '+asset)
        item['_source']=str(source)
        row=base_item_row(repo,item['base_item'])
        item['_item_class']=row['ItemClass'].lower();item['_model_type']=int(row['ModelType'])
        item['_icon_art_size']=[int(row['InvSlotWidth'])*32,int(row['InvSlotHeight'])*32]
        if mode=='inspect':continue
        icon=native_icon_size(item.get('icon_size'))
        if item['_item_class']=='ashlw' and icon!=(64,128):
            raise ValueError('Large shield icon_size must be exactly [64, 128]')
        if any(a>b for a,b in zip(item['_icon_art_size'],icon)):
            raise ValueError('icon_size is smaller than baseitems logical inventory area')
        if not number(item.get('scale')) or item['scale']<=0:raise ValueError('Positive uniform scale required')
        validate_orientation(item.get('orientation'))
        vector3(item.get('source_grip'),'source_grip');vector3(item.get('target_grip'),'target_grip')
        if not re.fullmatch(r'[a-z][a-z0-9_]{0,13}',item.get('texture','')):raise ValueError('Meaningful texture resref <=14 characters required')
        if item.get('texture_size') not in (512,1024):raise ValueError('texture_size must be 512 or 1024')
        if not item.get('attachment',{}).get('review_notes'):raise ValueError('Attachment review notes required')
        parts=item.get('parts',[{'model':item['model']}])
        if not parts or item['model'] not in [part['model'] for part in parts]:raise ValueError('Primary model must be an exported part')
        if item['_model_type']==2 and item.get('aliases'):
            raise ValueError('Composite top-level aliases are not allowed; use role-preserving part aliases')
        declared=list(item.get('aliases',[]))
        for part in parts:
            if item['_item_class']=='ashlw' and any(key in part for key in ('clip_min_y','clip_max_y','plane_y','clip_side')):
                raise ValueError('Shields require the complete surface; clipping is not allowed')
            if ('clip_side' in part)!=('plane_y' in part):raise ValueError('Split requires plane_y and clip_side')
            if 'clip_side' in part and (part['clip_side'] not in ('above','below') or not number(part['plane_y'])):
                raise ValueError('Invalid part split')
            bounds=[key for key in ('clip_min_y','clip_max_y') if key in part]
            if bounds and ('clip_side' in part or any(not number(part[key]) for key in bounds)):
                raise ValueError('Use finite clip bounds without plane_y/clip_side')
            if len(bounds)==2 and part['clip_min_y']>=part['clip_max_y']:
                raise ValueError('clip_min_y must be below clip_max_y')
            if item['_model_type']==2 and any(alias.rsplit('_',2)[-2]!=part['model'].rsplit('_',2)[-2] for alias in part.get('aliases',[])):
                raise ValueError('Part aliases must preserve the native part role')
            declared += [part['model']]+part.get('aliases',[])
        if item['_model_type']==2 and len(parts)>1:
            intervals=[]
            for part in parts:
                low=part.get('clip_min_y',-math.inf);high=part.get('clip_max_y',math.inf)
                if part.get('clip_side')=='above':low=part['plane_y']
                elif part.get('clip_side')=='below':high=part['plane_y']
                intervals.append((low,high))
            intervals.sort()
            if intervals[0][0]!=-math.inf or intervals[-1][1]!=math.inf or any(a[1]!=b[0] for a,b in zip(intervals,intervals[1:])):
                raise ValueError('Multi-part source regions must partition the Y axis without gaps or overlaps')
        for model in declared:
            validate_model_name(model,item['_item_class'],item['_model_type'])
            if model in seen:raise ValueError('Repeated output resource '+model)
            seen.add(model)
            outputs[model]=(item['_item_class'],item['_model_type']);output_assets[model]=item
            if item['_model_type']==2:
                source_part=next(p for p in parts if model==p['model'] or model in p.get('aliases',[]))
                clean=lambda value:0. if abs(value)<1e-12 else round(float(value),12)
                matrix=[[clean(item['scale']*v) for v in row] for row in item['orientation']]
                translation=[clean(item['target_grip'][i]-sum(matrix[i][j]*item['source_grip'][j] for j in range(3))) for i in range(3)]
                low=source_part.get('clip_min_y');high=source_part.get('clip_max_y')
                if source_part.get('clip_side')=='above':low=source_part['plane_y']
                elif source_part.get('clip_side')=='below':high=source_part['plane_y']
                selection={'sha256':item['sha256'],'matrix':matrix,'translation':translation,'clip':[None if low is None else clean(low),None if high is None else clean(high)]}
                output_selections[model]=json.dumps(selection,sort_keys=True)
        attachment_dummy_nodes(item,item['model'])
        if item['_model_type']==2:
            composites.append((asset,declared,[part['model'].rsplit('_',2)[-2] for part in parts]))
    if mode=='export':
        covered={model for _,models,roles in composites if sorted(roles)==['b','m','t'] for model in models}
        for assembly in config.get('composite_assemblies',[]):
            row=base_item_row(repo,assembly['base_item']);family=row['ItemClass'].lower()
            if int(row['ModelType'])!=2:raise ValueError('composite_assemblies requires a composite weapon baseitem')
            frame=assembly.get('icon_bounds')
            if not isinstance(frame,dict):raise ValueError('Grouped parts require shared icon_bounds')
            low=vector3(frame.get('min'),'icon_bounds min');high=vector3(frame.get('max'),'icon_bounds max')
            if any(b<=a for a,b in zip(low,high)):raise ValueError('icon_bounds must have positive dimensions')
            settings=None;selections=set();geometry_sources=[]
            for role,key in [('b','bottom'),('m','middle'),('t','top')]:
                model=assembly.get(key,'')
                validate_model_name(model,family,2)
                if model.rsplit('_',2)[-2]!=role or outputs.get(model)!=(family,2):
                    raise ValueError('Composite assembly requires exported '+key+' geometry: '+model)
                selection=output_selections[model]
                if selection in selections:raise ValueError('Grouped roles must not reuse the same source geometry selection')
                selections.add(selection)
                geometry_sources.append(json.loads(selection))
                owner=output_assets[model]
                current=[owner['icon_size'],owner['_icon_art_size'],owner.get('icon_direction',[1,-.1,.12]),owner.get('icon_roll_degrees',0)]
                if settings is not None and current!=settings:raise ValueError('Grouped icon canvas and camera settings must match')
                settings=current
                if '_shared_icon_bounds' in owner and owner['_shared_icon_bounds']!=frame:
                    raise ValueError('Shared part has conflicting icon_bounds')
                owner['_shared_icon_bounds']=frame
                covered.add(model)
            source_groups={}
            for selected in geometry_sources:
                identity=json.dumps({k:v for k,v in selected.items() if k!='clip'},sort_keys=True)
                source_groups.setdefault(identity,[]).append(selected)
            for group in source_groups.values():
                intervals=sorted((-math.inf if selected['clip'][0] is None else selected['clip'][0],math.inf if selected['clip'][1] is None else selected['clip'][1]) for selected in group)
                if any(a[1]>b[0] for a,b in zip(intervals,intervals[1:])):
                    raise ValueError('Grouped parts from one fitted source must not overlap')
                if len(group)==3 and (intervals[0][0]!=-math.inf or intervals[-1][1]!=math.inf or any(a[1]!=b[0] for a,b in zip(intervals,intervals[1:]))):
                    raise ValueError('Grouped parts from one fitted source must cover that source without gaps or overlaps')
        for asset,models,_ in composites:
            if not set(models)<=covered:
                raise ValueError('Composite export requires real bottom, middle and top parts in the asset or composite_assemblies: '+asset)
        for part in config.get('empty_parts',[]):
            row=base_item_row(repo,part['base_item']);model=part['model']
            if row['ItemClass'].lower()=='ashlw':
                raise ValueError('Shields require real geometry; empty_parts is not allowed: '+model)
            if int(row['ModelType'])==2:
                raise ValueError('Composite weapons require real bottom, middle and top parts; empty_parts is not allowed: '+model)
            validate_model_name(model,row['ItemClass'].lower(),int(row['ModelType']))
            icon=native_icon_size(part.get('icon_size'))
            if any(a>b for a,b in zip([int(row['InvSlotWidth'])*32,int(row['InvSlotHeight'])*32],icon)):
                raise ValueError('Empty part icon_size is smaller than baseitems logical inventory area: '+model)
            if model in seen:raise ValueError('Empty part would replace occupied resource '+model)
            seen.add(model)


def load_blender():
    global bpy, np, Matrix, Quaternion, Vector
    import bpy
    import numpy as np
    from mathutils import Matrix, Quaternion, Vector
    if bpy.app.version[:2]!=(4,0):raise ValueError('Use tested Blender 4.0')


def load_module(name, path):
    spec=importlib.util.spec_from_file_location(name,path)
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    return module


def load_module_snapshot(name, path):
    """Execute one in-memory snapshot of a helper and record the hash of exactly those bytes."""
    path=Path(path);source=path.read_bytes()
    module=importlib.util.module_from_spec(importlib.util.spec_from_loader(name,loader=None,origin=str(path)))
    module.__file__=str(path);sys.modules[name]=module
    exec(compile(source,str(path),'exec'),module.__dict__)
    module.SNAPSHOT_SHA256=hashlib.sha256(source).hexdigest()
    return module


def load_compiler(repo):
    """Snapshot the CleanModels helper and the compile wrapper that imports it, so the bytes that compile are the
    bytes recorded. The helper is registered first, so the wrapper's `import CleanModels` binds to that snapshot."""
    helper_path=Path(repo)/'SWLOR_Haks/tools/CleanModels.py';wrapper_path=Path(repo)/'tools/CompileBlasterModels.py'
    helper=load_module_snapshot('CleanModels',helper_path)
    wrapper=load_module_snapshot('weapon_compiler',wrapper_path)
    require(wrapper.CleanModels is helper,'Compile wrapper did not bind the snapshotted CleanModels helper')
    return wrapper,{'wrapper':{'path':str(wrapper_path),'sha256':wrapper.SNAPSHOT_SHA256},'wrapper_helper':{'path':str(helper_path),'sha256':helper.SNAPSHOT_SHA256}}


def compile_with_snapshot(compiler, compiler_path, ascii_dir, out):
    """Compile with a private copy of the executable, and confirm afterwards that the copy is still those bytes, so
    the reported identity is the binary that ran."""
    copy,snapshot=snapshot_tool(compiler_path,out/'compiler_snapshot')
    compiler.compile_models(copy,ascii_dir,out/'compiled',[])
    require(tool_identity(copy)['sha256']==snapshot['sha256'],'Compiler snapshot changed during compilation')
    return snapshot


def require(condition, message):
    if not condition:raise ValueError(message)


def validate_native(path, source, runtime):
    """Check the compiler retained every directed triangle corner and tangent frame."""
    import numpy as np
    data=path.read_bytes()
    require(len(data)>=244 and data[:4]==bytes(4),'Expected compiled binary '+path.name)
    raw=12+struct.unpack_from('<I',data,4)[0]
    require(raw+struct.unpack_from('<I',data,8)[0]==len(data),'Invalid native header bounds')
    def unpack(fmt,offset,end=None):
        require(offset>=12 and offset+struct.calcsize(fmt)<=(len(data) if end is None else end),'Native stream out of bounds')
        return struct.unpack_from(fmt,data,offset)
    def name(offset,size):
        require(offset>=12 and offset+size<=raw,'Native name out of bounds')
        return data[offset:offset+size].split(b'\0',1)[0].decode('ascii')
    nodes={};parents={};pointers=set();pending=[(struct.unpack_from('<I',data,84)[0], 'NULL')]
    while pending:
        pointer,parent=pending.pop();node=12+pointer
        require(pointer not in pointers and len(pointers)<10000,'Cyclic/oversized native node graph')
        pointers.add(pointer)
        label=name(node+32,32);require(label not in nodes,'Duplicate native node name');nodes[label]=node;parents[label]=parent
        offset,count,_=unpack('<III',node+72,raw)
        require(count<=10000,'Oversized native child table')
        pending.extend((unpack('<I',12+offset+4*i,raw)[0], label) for i in range(count))
    source_nodes=re.findall(r'node (dummy|trimesh) (\S+)(.*?)endnode',source.read_text(),re.S)
    require(len({label for _,label,_ in source_nodes})==len(source_nodes),'Duplicate ASCII node name')
    require(set(nodes)=={label for _,label,_ in source_nodes},'Native node set changed')
    blocks=[(label,block) for kind,label,block in source_nodes if kind=='trimesh']
    dummies=[]
    for kind,label,block in source_nodes:
        parent=re.search(r'^\s*parent\s+(\S+)',block,re.M)
        require(parent is not None and parents[label]==parent[1],'Native node parent changed: '+label)
        if kind!='dummy':continue
        node=nodes[label]
        require(unpack('<I',node+108,raw)[0]==1,'Native dummy acquired geometry: '+label)
        key_offset,key_count,_=unpack('<III',node+84,raw)
        data_offset,data_count,_=unpack('<III',node+96,raw)
        require(key_count<=1000 and data_count<=10000,'Oversized native controllers')
        actual={8:np.zeros(3),20:np.asarray([0.,0.,0.,1.]),36:np.ones(1)}
        seen=set()
        for index in range(key_count):
            controller,rows,_,start,columns=unpack('<IHHHBx',12+key_offset+index*12,raw)
            require(controller in actual and controller not in seen and rows==1 and columns==len(actual[controller]),'Unexpected native dummy controller: '+label)
            require(start+columns<=data_count,'Native controller data out of bounds')
            actual[controller]=np.asarray(unpack('<'+'f'*columns,12+data_offset+start*4,raw));seen.add(controller)
        def source_values(key,default):
            match=re.search(r'^\s*'+key+r'\s+([^\n]+)',block,re.M)
            return np.asarray(list(map(float,match[1].split()))) if match else np.asarray(default)
        position=source_values('position',[0,0,0]);orientation=source_values('orientation',[0,0,1,0]);scale=source_values('scale',[1])
        require(len(position)==3 and len(orientation)==4 and len(scale)==1,'Invalid ASCII dummy transform')
        axis=orientation[:3];angle=orientation[3];length=np.linalg.norm(axis)
        require(length>0 or abs(angle)<1e-12,'Invalid ASCII dummy rotation')
        quaternion=np.r_[axis/max(length,1e-30)*math.sin(angle/2),math.cos(angle/2)]
        require(all(np.isfinite(value).all() for value in (*actual.values(),position,quaternion,scale)),'Nonfinite dummy transform')
        position_error=float(np.max(abs(actual[8]-position)))
        rotation_error=float(min(np.max(abs(actual[20]-quaternion)),np.max(abs(actual[20]+quaternion))))
        require(position_error<1e-6 and rotation_error<1e-6 and np.max(abs(actual[36]-scale))<1e-6,'Native dummy transform changed: '+label)
        dummies.append({'node':label,'parent':parents[label],'max_position_error':position_error,'max_quaternion_error':rotation_error})
    meshes=[]
    for label,block in blocks:
        mesh=nodes[label]+112
        count=unpack('<H',mesh+448,raw)[0];fo,fc,_=unpack('<III',mesh+8,raw)
        require(fc>0 and fc<=65535 and count>0,'Invalid native geometry counts')
        require(unpack('<I',mesh+440,raw)[0]==0,'Unexpected interleaved native streams')
        def stream(offset,columns):
            start=unpack('<i',mesh+offset,raw)[0];require(start>=0,'Missing native stream')
            return np.asarray([unpack('<'+'f'*columns,raw+start+i*columns*4) for i in range(count)])
        v,u,n,t,h=stream(444,3),stream(452,2),stream(468,3),stream(488,3),stream(496,1)[:,0]
        f=np.asarray([unpack('<HHH',12+fo+i*32+26,raw) for i in range(fc)])
        require(int(f.max())<count,'Native face index out of bounds');used=np.unique(f)
        require(all(np.isfinite(a).all() for a in (v,u,n,t,h)),'Nonfinite native data')
        require(np.max(abs(np.linalg.norm(n[used],axis=1)-1))<1e-4,'Native normal not unit')
        require(np.max(abs(np.linalg.norm(t[used],axis=1)-1))<1e-4,'Native tangent not unit')
        require(np.max(abs(np.sum(n[used]*t[used],axis=1)))<1e-4,'Native tangent not orthogonal')
        require(set(h[used])<={-1.,1.},'Invalid native tangent handedness')
        expected=re.search(r'^\s*materialname\s+(\S+)',block,re.M)[1]
        require(name(mesh+312,64)==expected and name(mesh+120,64)==expected,'Native material changed')
        require((runtime/(expected+'.mtr')).is_file(),'Native material missing')
        def rows(key,width):
            match=re.search(r'^\s*'+key+r' (\d+)\s*\n',block,re.M)
            require(match is not None,'Missing ASCII '+key)
            return np.asarray([list(map(float,row.split()[:width])) for row in block[match.end():].splitlines()[:int(match[1])]])
        sv,su,sn,sf=rows('verts',3),rows('tverts',2),rows('normals',3),rows('faces',3).astype(int)
        require(len(sf)==fc,'Native triangle count changed')
        centers=v[f].mean(axis=1);matched=set();errors=np.zeros(3)
        for oldface in sf:
            candidates=np.where(np.linalg.norm(centers-sv[oldface].mean(axis=0),axis=1)<1e-7)[0];found=False
            for index in candidates:
                if index in matched:continue
                # Cyclic rotations preserve winding; reversals are deliberately forbidden.
                for shift in range(3):
                    face=np.roll(f[index],shift)
                    error=np.asarray([np.max(abs(sv[oldface]-v[face])),np.max(abs(su[oldface]-u[face])),np.max(abs(sn[oldface]-n[face]))])
                    if all(error<np.asarray([1e-7,1e-6,1e-5])):
                        errors=np.maximum(errors,error);matched.add(index);found=True;break
                if found:break
            require(found,'Native position/UV/normal/winding changed: '+path.name+'/'+label)
        meshes.append({'node':label,'triangles':fc,'max_position_uv_normal_error':errors.tolist()})
    if not blocks:require(len(nodes)==1 and raw==len(data),'Dummy acquired geometry')
    return {'model':path.stem,'meshes':meshes,'dummies':dummies,'sha256':hashlib.sha256(data).hexdigest()}


def dds_header(data, size, mips, fourcc):
    require(len(data)>=128 and data[:4]==b'DDS ','Invalid DDS header')
    height,width=struct.unpack_from('<II',data,12);actual_mips=max(1,struct.unpack_from('<I',data,28)[0])
    require((width,height,actual_mips,data[84:88].decode('ascii'))==(*size,mips,fourcc),'DDS format/dimensions/mips changed')
    block=8 if fourcc in ('DXT1','ATI1') else 16
    length=128+sum(max(1,(max(1,width>>i)+3)//4)*max(1,(max(1,height>>i)+3)//4)*block for i in range(mips))
    require(len(data)==length,'DDS compressed payload size mismatch')


def texture_roles(paths):
    roles={}
    for path in paths:
        if path.suffix=='.mdl':roles['i'+path.stem]='icon'
    for path in paths:
        if path.suffix!='.mtr':continue
        for slot,name in re.findall(r'^texture(\d+)\s+(\S+)',path.read_text(),re.M):
            role={'0':'color','1':'normal','2':'scalar','3':'scalar','5':'color'}.get(slot)
            if role is None or (name in roles and roles[name]!=role):
                raise ValueError('Unsupported/conflicting texture role '+name)
            roles[name]=role
    return roles


def load_manifest(path):
    """One snapshot of the fit manifest owns both the parsed config and its recorded hash."""
    data=Path(path).read_bytes()
    return json.loads(data.decode('utf-8-sig')),hashlib.sha256(data).hexdigest()


def tool_identity(path):
    path=Path(path).resolve();return {'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}


def snapshot_tool(path, folder):
    """Copy an executable once into a private folder; every invocation runs that immutable copy. Bytes and
    mode come from one open handle, so an executable original stays executable on POSIX hosts even if the
    configured path is replaced while the snapshot is taken."""
    source=Path(path).resolve()
    with open(source,'rb') as handle:mode=os.fstat(handle.fileno()).st_mode;data=handle.read()
    folder.mkdir(parents=True,exist_ok=False);copy=folder/source.name;copy.write_bytes(data);os.chmod(copy,stat.S_IMODE(mode))
    return copy,{'path':str(source),'sha256':hashlib.sha256(data).hexdigest()}


def require_basis_producer(record,helper_sha256):
    """Arrays are interpreted by the loaded helper snapshot, so they must come from those same helper bytes."""
    require(record.get('producer_sha256')==helper_sha256,
            'Normal basis was produced by a different TrellisWeaponNormals.py; re-export')


def snapshot_export(export,manifest,staging):
    """Copy each export resource's bytes once, after hashing those same bytes."""
    found={str(p.relative_to(export)).replace('\\','/'):p for p in export.glob('*/resources/*')}
    found.update({str(p.relative_to(export)).replace('\\','/'):p for p in (export/'empty_resources').glob('*')})
    require(set(found)==set(manifest['resources']),'Export resource set differs from recorded manifest')
    paths={}
    for key,path in sorted(found.items()):
        data=path.read_bytes();require(hashlib.sha256(data).hexdigest()==manifest['resources'][key],'Changed export '+key)
        target=staging/key;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);paths[key]=target
    return paths


def package(args, repo, out):
    export=args.export.resolve();manifest_bytes=(export/'export_manifest.json').read_bytes();manifest=json.loads(manifest_bytes)
    # Every later copy, role scan, compile and compression reads only these verified bytes.
    paths=snapshot_export(export,manifest,out/'verified_export')
    ascii_dir=out/'ascii_resources';runtime=out/'runtime_resources';logs=out/'compression_logs'
    for folder in (ascii_dir,runtime,logs):folder.mkdir()
    seen=set();textures=[];roles=texture_roles(list(paths.values()));crunch_identity=None
    # Compile first: only the real native streams define the renderer's final TBN.
    for source in paths.values():
        if source.suffix in ('.mdl','.mtr'):
            shutil.copy2(source,ascii_dir/source.name)
            if source.suffix=='.mtr':shutil.copy2(source,runtime/source.name)
    compiler,wrapper_identity=load_compiler(repo)
    compiler_snapshot=compile_with_snapshot(compiler,compiler.CleanModels.find_compiler(args.compiler),ascii_dir,out)
    compiler_identity={'name':'CleanModels','version':compiler.CleanModels.VERSION,'path':compiler_snapshot['path'],'sha256':compiler_snapshot['sha256'],**wrapper_identity}
    for source in (out/'compiled').glob('*.mdl'):shutil.copy2(source,runtime/source.name)
    helper=load_module_snapshot('trellis_weapon_normals',Path(__file__).with_name('TrellisWeaponNormals.py'))
    import numpy as np
    if not manifest.get('normal_bases'):raise ValueError('Re-export with source tangent provenance before packaging')
    corrected=out/'normal_reprojected';corrected.mkdir();normal_paths={};normal_reports=[]
    for record in manifest['normal_bases']:
        metadata=export/record['file']
        require(metadata.resolve().is_relative_to(export),'Normal metadata escapes export directory')
        metadata_bytes=metadata.read_bytes()
        require(hashlib.sha256(metadata_bytes).hexdigest()==record['sha256'],'Source tangent provenance changed')
        require(manifest['sources'].get(record['asset'])==record['source_sha256'],'Source tangent GLB hash mismatch')
        # Arrays are interpreted by the current helper, so they must have been produced by these same bytes.
        require_basis_producer(record,helper.SNAPSHOT_SHA256)
        with np.load(io.BytesIO(metadata_bytes),allow_pickle=False) as arrays:
            policy=record.get('normal_basis_policy','reproject')
            require(policy in ('reproject','authored_tangents'),'Unknown normal basis policy')
            authored_reports=[]
            if policy=='authored_tangents':
                sources={material:{key:arrays[material+'__'+key] for key in ('positions','normals','tangents','signs','uv')} for material in record['materials']}
                for model in record['models']+record.get('aliases',[]):authored_reports.append(helper.preserve_authored_tangents(runtime/(model+'.mdl'),sources))
            native=[mesh for model in record['models'] for mesh in helper.native_meshes(runtime/(model+'.mdl'))]
            for material in record['materials']:
                group={key:arrays[material+'__'+key] for key in ('positions','normals','tangents','signs','uv')}
                matches=[mesh for mesh in native if mesh['material']==material]
                mtr=(runtime/(material+'.mtr')).read_text();match=re.search(r'^texture1\s+(\S+)',mtr,re.M)
                require(match is not None,'Material has no normal map '+material)
                name=match[1];sources=[p for p in paths.values() if p.name==name+'.tga']
                require(len(sources)==1 and name not in normal_paths,'Normal map has ambiguous material ownership '+name)
                target=corrected/(name+'.tga')
                if policy=='authored_tangents':
                    shutil.copy2(sources[0],target)
                    normal_reports.append({'asset':record['asset'],'material':material,'basis_sha256':record['sha256'],'policy':policy,'normal_tga_preserved_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'stream_proofs':authored_reports,'native_visual_review_required':True})
                else:normal_reports.append({'asset':record['asset'],'material':material,'basis_sha256':record['sha256'],'policy':policy,**helper.reproject_texture(group,matches,sources[0],target)})
                normal_paths[name]=target
    for source in sorted(paths.values()):
        require(source.name not in seen,'Duplicate resource '+source.name);seen.add(source.name)
        if source.suffix in ('.mdl','.mtr'):
            shutil.copy2(source,ascii_dir/source.name)
            if source.suffix=='.mtr':shutil.copy2(source,runtime/source.name)
            continue
        require(source.suffix=='.tga','Unexpected export resource '+source.name)
        if roles.get(source.stem)=='normal':source=normal_paths[source.stem]
        tga=source.read_bytes();width,height=struct.unpack_from('<HH',tga,12)
        require(width>=4 and height>=4 and not width&(width-1) and not height&(height-1),'Power-of-two texture dimensions required for native DDS')
        role=roles.get(source.stem);require(role is not None,'Unbound texture '+source.stem)
        icon=role=='icon';options=['-fileformat','dds','-unflip','-yflip','-dxtQuality','uber']
        if icon:options+=['-DXT5','-gamma','2.2','-mipMode','None'];fmt='DXT5';mips=1
        else:
            mips=int(math.log2(max(width,height)))+1
            if role=='normal':options+=['-dropEmptyAlpha','-DXN','-normalize','-renormalize','-uniformMetrics','-gamma','1.0'];fmt='ATI2'
            elif role=='scalar':options+=['-dropEmptyAlpha','-DXT5A','-setAtoY','-gamma','1.0'];fmt='ATI1'
            else:options+=['-dropEmptyAlpha','-DXT1','-gamma','2.2'];fmt='DXT1'
        target=runtime/(source.stem+'.dds')
        if crunch_identity is None:crunch,crunch_identity=snapshot_tool(args.crunch,out/'crunch_snapshot')
        result=subprocess.run([str(crunch),'-file',str(source),'-out',str(target)]+options,capture_output=True,text=True,timeout=120)
        (logs/(source.stem+'.log')).write_text(result.stdout+result.stderr)
        require(result.returncode==0 and target.is_file(),'Crunch failed '+source.name)
        dds_header(target.read_bytes(),(width,height),mips,fmt)
        shutil.copy2(target,ascii_dir/target.name);textures.append({'file':target.name,'size':[width,height],'format':fmt,'mips':mips})
    for mtr in runtime.glob('*.mtr'):
        for name in re.findall(r'^texture\d+\s+(\S+)',mtr.read_text(),re.M):require((runtime/(name+'.dds')).is_file(),'Missing texture '+name)
    models=[validate_native(p,ascii_dir/p.name,runtime) for p in sorted(runtime.glob('*.mdl'))]
    # Every DDS must come from the one Crunch binary recorded in the report.
    if crunch_identity is not None:require(tool_identity(crunch)['sha256']==crunch_identity['sha256'],'Crunch snapshot changed during packaging')
    report={'export_manifest_sha256':hashlib.sha256(manifest_bytes).hexdigest(),
            'resources':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in runtime.iterdir()},
            'textures':textures,'models':models,'normal_basis_reprojection':normal_reports,'compiler':compiler_identity,'texture_compressor':crunch_identity,'production_copied':False,'native_visual_review_required':True}
    (out/'package_report.json').write_text(json.dumps(report,indent=2)+'\n')
    print('WEAPON_PACKAGE_COMPLETE '+str(runtime))


def main():
    parser=argparse.ArgumentParser(description=__doc__,formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--repo',type=Path,default=Path(__file__).resolve().parents[1])
    parser.add_argument('--manifest',type=Path);parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--mode',choices=['inspect','export','package'],required=True)
    parser.add_argument('--export',type=Path,help='Recorded export directory for package mode')
    parser.add_argument('--crunch',type=Path)
    parser.add_argument('--compiler',type=Path,help='Pinned CleanModels executable; defaults to CLEANMODELS or the local install')
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    args=parser.parse_args(argv);repo=args.repo.resolve();out=args.output.resolve()
    if out.is_relative_to(repo):raise ValueError('Keep staging output outside the repository')
    if out.exists() and any(out.iterdir()):raise ValueError('Output directory must be empty')
    if args.mode=='package':
        if any(getattr(args,key) is None for key in ('export','crunch')):parser.error('package requires --export and --crunch')
        out.mkdir(parents=True,exist_ok=True);package(args,repo,out);return
    if args.manifest is None:parser.error('inspect/export requires --manifest')
    manifest=args.manifest.resolve();config,manifest_sha=load_manifest(manifest)
    validate_manifest(config,repo,manifest,args.mode);load_blender()
    out.mkdir(parents=True,exist_ok=True)
    helpers=load_module('weapon_export',repo/'tools/ImportBlasterModel.py')
    reports=[];previews=[]
    for i,item in enumerate(config['assets']):
        report,preview=export_one(item,Path(item['_source']),repo,out/f"{i+1:02d}_{item['asset']}",args.mode,helpers,item.get('icon_size'))
        reports.append(report);previews.append(preview)
    contact_sheet(previews,out/'contact_sheet.png',256)
    if args.mode=='export' and config.get('empty_parts'):
        resources=out/'empty_resources';resources.mkdir()
        for entry in config['empty_parts']:
            model=entry['model'];helpers.write_mdl(resources/(model+'.mdl'),model,'NULL',[])
            width,height=entry['icon_size'];im=bpy.data.images.new('i'+model,width=width,height=height,alpha=True)
            im.pixels.foreach_set(np.zeros(width*height*4,np.float32));im.filepath_raw=str(resources/('i'+model+'.tga'));im.file_format='TARGA_RAW';im.save();bpy.data.images.remove(im)
    (out/'reports.json').write_text(json.dumps(reports,indent=2)+'\n')
    if args.mode=='export':
        paths=list(out.glob('*/resources/*'))+list((out/'empty_resources').glob('*'))
        resources={p.relative_to(out).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
        (out/'export_manifest.json').write_text(json.dumps({'schema_version':1,'source_manifest_sha256':manifest_sha,
            'sources':{item['asset']:item['sha256'] for item in config['assets']},'resources':resources,
            'normal_bases':[{'asset':item['asset'],'file':f"{i+1:02d}_{item['asset']}/source_basis.npz",**report['source_normal_basis'],'models':[part['model'] for part in report['parts']],'normal_basis_policy':item.get('normal_basis_policy','reproject'), 'aliases':sum([part.get('aliases',[]) for part in item.get('parts',[])],[])+item.get('aliases',[])} for i,(item,report) in enumerate(zip(config['assets'],reports))]},indent=2)+'\n')
    print('WEAPON_TOOL_COMPLETE '+str(out))


if __name__=='__main__':main()
