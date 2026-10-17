"""Optional ComfyUI bridge for SWLOR's separate shield workflow.

Link this directory into a task-owned ComfyUI custom_nodes directory. Set
SWLOR_BLENDER to Blender4.0's executable (or provide blender on PATH), and
SWLOR_SHIELD_CACHE to an absolute task-owned staging directory. Do not modify a
shared ComfyUI installation or enable this package in an unrelated service.
"""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess

SURFACE_SCRIPT=Path(__file__).resolve().parents[1]/'TrellisShieldSurface.py'


def settings():
    executable=os.environ.get('SWLOR_BLENDER','blender')
    found=shutil.which(executable)
    if found is None:raise ValueError('Set SWLOR_BLENDER to the installed Blender4.0 executable')
    cache=Path(os.environ.get('SWLOR_SHIELD_CACHE',''))
    if not cache.is_absolute():raise ValueError('Set SWLOR_SHIELD_CACHE to an absolute task-owned staging directory')
    if not SURFACE_SCRIPT.is_file():raise ValueError('Link this package from repository tools/ so TrellisShieldSurface.py remains beside it')
    return found,cache


def require_untextured(mesh):
    attributes=('uvs','vertex_colors','texture','metallic_roughness','normal_map','material','emissive','tangents')
    if any(getattr(mesh,name,None) is not None for name in attributes) or getattr(mesh,'unlit',False):
        raise ValueError('Shield reconstruction must precede UV/material baking; textured inputs are not supported')


def cached_arrays(path):
    import numpy as np
    record=json.loads(path.with_suffix('.json').read_text())
    if record['output_sha256']!=hashlib.sha256(path.read_bytes()).hexdigest():raise ValueError('Cached shield surface changed: '+str(path))
    with np.load(path,allow_pickle=False) as data:return data['vertices'].copy(),data['faces'].copy()


class SWLORClosedVoxelMesh:
    @classmethod
    def INPUT_TYPES(cls):
        return {'required':{'mesh':('MESH',),'voxel_size':('FLOAT',{'default':.003,'min':.0001,'max':.1,'step':.001}),
                            'target_face_count':('INT',{'default':3500,'min':100,'max':100000})}}
    RETURN_TYPES=('MESH','MESH')
    RETURN_NAMES=('closed_high','closed_low')
    FUNCTION='execute'
    CATEGORY='SWLOR/shields'

    @classmethod
    def IS_CHANGED(cls,mesh,voxel_size,target_face_count):
        executable,cache=settings()
        return hashlib.sha256(SURFACE_SCRIPT.read_bytes()+Path(__file__).read_bytes()+str((executable,cache)).encode()).hexdigest()

    def execute(self,mesh,voxel_size,target_face_count):
        import numpy as np
        import torch
        from comfy_api.latest import Types
        executable,cache=settings()
        require_untextured(mesh)
        if len(mesh.vertices)!=1 or len(mesh.faces)!=1:raise ValueError('Expected exactly one shield mesh')
        vertices=mesh.vertices[0].detach().cpu().numpy().astype(np.float32)
        faces=mesh.faces[0].detach().cpu().numpy().astype(np.int32)
        digest=hashlib.sha256()
        for part in (vertices.tobytes(),faces.tobytes(),str(voxel_size).encode(),SURFACE_SCRIPT.read_bytes()):digest.update(part)
        directory=cache/digest.hexdigest();directory.mkdir(parents=True,exist_ok=True)
        source=directory/'input.npz';high=directory/'closed_high.npz';low=directory/('closed_low_'+str(target_face_count)+'.npz')
        def run(mode,input_path,output_path,flags):
            command=[executable,'-b','--factory-startup','--python-exit-code','1','--python',str(SURFACE_SCRIPT),'--',mode,str(input_path),str(output_path),*flags]
            with output_path.with_suffix('.log').open('w',encoding='utf-8') as log:
                subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,timeout=600,check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        if not high.is_file():
            np.savez(source,vertices=vertices,faces=faces)
            run('voxel',source,high,['--voxel-size',str(voxel_size)])
            source.unlink()
        high_arrays=cached_arrays(high)
        if not low.is_file():run('decimate',high,low,['--target-faces',str(target_face_count)])
        low_arrays=cached_arrays(low)
        meshes=tuple(Types.MESH(vertices=torch.from_numpy(v).unsqueeze(0),faces=torch.from_numpy(f).unsqueeze(0)) for v,f in (high_arrays,low_arrays))
        provenance={'high_surface':str(high),'low_surface':str(low),'surface_script_sha256':hashlib.sha256(SURFACE_SCRIPT.read_bytes()).hexdigest()}
        return {'ui':{'text':[json.dumps(provenance,sort_keys=True)]},'result':meshes}


class SWLORNormalUNorm8:
    @classmethod
    def INPUT_TYPES(cls):return {'required':{'images':('IMAGE',)}}
    RETURN_TYPES=('IMAGE',)
    FUNCTION='execute'
    CATEGORY='SWLOR/shields'

    def execute(self,images):
        import torch
        spec=importlib.util.spec_from_file_location('swlor_shield_surface',SURFACE_SCRIPT)
        surface=importlib.util.module_from_spec(spec);spec.loader.exec_module(surface)
        quantized=surface.normal_unorm8(images.detach().cpu().numpy())
        # GLB writing truncates float*255; stay inside the chosen byte interval.
        result=(torch.from_numpy(quantized).to(device=images.device,dtype=images.dtype)+.25)/255
        return (result.clamp(0,1),)


NODE_CLASS_MAPPINGS={'SWLORClosedVoxelMesh':SWLORClosedVoxelMesh,'SWLORNormalUNorm8':SWLORNormalUNorm8}
NODE_DISPLAY_NAME_MAPPINGS={'SWLORClosedVoxelMesh':'SWLOR Closed Shield Surface','SWLORNormalUNorm8':'SWLOR Normal UNORM8'}
