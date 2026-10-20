"""Optional ComfyUI bridge for SWLOR's separate shield workflow.

Link this directory into a task-owned ComfyUI custom_nodes directory. Set
SWLOR_BLENDER to Blender4.0's executable (or provide blender on PATH), and
SWLOR_SHIELD_CACHE to an absolute task-owned staging directory. Do not modify a
shared ComfyUI installation or enable this package in an unrelated service.
"""
import contextlib
import hashlib
from functools import lru_cache
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import uuid

SURFACE_SCRIPT=Path(__file__).resolve().parents[1]/'TrellisShieldSurface.py'


@lru_cache(maxsize=8)
def blender_version(executable, executable_sha256):
    result=subprocess.run([executable,'--version'],capture_output=True,text=True,
                          timeout=15,check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    version=result.stdout.strip()
    if not version.splitlines() or not version.splitlines()[0].startswith('Blender 4.0.'):
        raise ValueError('Use tested Blender 4.0; executable reports: '+version)
    return version


def blender_identity(executable):
    path=Path(executable).resolve()
    digest=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(4*1024*1024),b''):digest.update(block)
    fingerprint=digest.hexdigest()
    return {'path':str(path),'sha256':fingerprint,'version':blender_version(str(path),fingerprint)}


def sync_directory(directory):
    """Flush a directory entry on POSIX; Windows exposes no directory fsync."""
    if os.name=='nt':return
    descriptor=os.open(directory,os.O_RDONLY|getattr(os,'O_DIRECTORY',0))
    try:os.fsync(descriptor)
    finally:os.close(descriptor)


def create_durable_directories(directory):
    """Create missing ancestors and persist each new entry in its parent, top-down."""
    missing=[];path=Path(directory)
    while not os.path.lexists(path) and path.parent!=path:missing.append(path);path=path.parent
    Path(directory).mkdir(parents=True,exist_ok=True)
    for path in reversed(missing):sync_directory(path.parent)


@contextlib.contextmanager
def key_lock(directory):
    """Exclusive, blocking per-cache-key lock held by an open lock file."""
    handle=(Path(directory)/'.lock').open('a+b')
    try:
        if os.name=='nt':
            import msvcrt
            while True:
                try:handle.seek(0);msvcrt.locking(handle.fileno(),msvcrt.LK_LOCK,1);break
                except OSError:continue
        else:
            import fcntl
            fcntl.flock(handle.fileno(),fcntl.LOCK_EX)
        yield
    finally:
        if os.name=='nt':
            import msvcrt
            try:handle.seek(0);msvcrt.locking(handle.fileno(),msvcrt.LK_UNLCK,1)
            except OSError:pass
        handle.close()


def require_same_script(script_sha,outputs):
    """The cache key names the hashed surface script; discard output made by a replaced script."""
    try:same=hashlib.sha256(SURFACE_SCRIPT.read_bytes()).hexdigest()==script_sha
    except OSError:same=False
    if not same:
        discard_outputs(outputs)
        raise ValueError('Shield surface script changed or could not be verified during reconstruction')


def discard_outputs(outputs):
    for stale in outputs:
        if stale.exists():stale.unlink()


def require_same_blender(executable,runtime,outputs):
    """The cache key names the hashed build; discard output made by a replaced executable."""
    try:same=blender_identity(executable)['sha256']==runtime['sha256']
    except Exception:same=False  # An unprovable identity counts as a mismatch.
    if not same:
        discard_outputs(outputs)
        raise ValueError('Blender executable changed or could not be verified during shield reconstruction')


def probe_cache_access(cache,missing):
    """Exercise cache access and best-effort cleanup, reporting removal failures."""
    created=[];probe_file=None
    try:
        for directory in reversed(missing):
            try:directory.mkdir()
            except FileExistsError:
                if not directory.is_dir():raise
            else:created.append(directory)
        probe=cache/('.swlor-preflight-'+uuid.uuid4().hex)
        probe.mkdir();created.append(probe)
        candidate=probe/'write-test'
        with candidate.open('xb') as stream:
            probe_file=candidate;stream.write(b'SWLOR shield cache preflight\n')
    finally:
        pending=([(probe_file,False)] if probe_file is not None else [])+[(path,True) for path in reversed(created)]
        first_error=None
        for attempt in range(2):
            failed=[]
            for path,is_directory in pending:
                try:
                    if is_directory:path.rmdir()
                    else:path.unlink()
                except FileNotFoundError:pass
                except OSError as error:
                    if first_error is None:first_error=error
                    failed.append((path,is_directory))
            pending=failed
            if not pending:break
        if first_error is not None:raise first_error


def settings():
    executable=os.environ.get('SWLOR_BLENDER','blender')
    found=shutil.which(executable)
    if found is None:raise ValueError('Set SWLOR_BLENDER to the installed Blender4.0 executable')
    cache=Path(os.environ.get('SWLOR_SHIELD_CACHE',''))
    if not cache.is_absolute():raise ValueError('Set SWLOR_SHIELD_CACHE to an absolute task-owned staging directory')
    ancestor=cache;missing=[]
    while not ancestor.exists():
        missing.append(ancestor)
        parent=ancestor.parent
        if parent==ancestor:raise ValueError('Shield cache has no existing ancestor: '+str(cache))
        ancestor=parent
    if not ancestor.is_dir():raise ValueError('Shield cache must resolve beneath a directory: '+str(cache))
    if not SURFACE_SCRIPT.is_file():raise ValueError('Link this package from repository tools/ so TrellisShieldSurface.py remains beside it')
    try:probe_cache_access(cache,missing)
    except OSError as error:raise ValueError('Shield cache must allow directory/file creation and removal: '+str(cache)) from error
    return found,cache,blender_identity(found)


def require_untextured(mesh):
    attributes=('uvs','vertex_colors','texture','metallic_roughness','normal_map','material','emissive','tangents')
    if any(getattr(mesh,name,None) is not None for name in attributes) or getattr(mesh,'unlit',False):
        raise ValueError('Shield reconstruction must precede UV/material baking; textured inputs are not supported')


def cached_arrays(path):
    import numpy as np
    import io
    record=json.loads(path.with_suffix('.json').read_text(encoding='utf-8'))
    data=path.read_bytes()  # Hash and load the same snapshot.
    if record['output_sha256']!=hashlib.sha256(data).hexdigest():raise ValueError('Cached shield surface changed: '+str(path))
    with np.load(io.BytesIO(data),allow_pickle=False) as arrays:return arrays['vertices'].copy(),arrays['faces'].copy()


def cached_entry(path):
    """Recover interrupted publication; complete entries still reject corruption."""
    report=path.with_suffix('.json')
    if path.is_file() and report.is_file():
        try:return cached_arrays(path)
        except (json.JSONDecodeError,KeyError):pass
    for incomplete in (path,report,path.with_suffix(path.suffix+'.part'),report.with_suffix('.json.part')):
        if incomplete.exists():incomplete.unlink()
    return None


def surface_cache_key(vertices,faces,voxel_size,runtime,script_bytes=None):
    digest=hashlib.sha256()
    def field(name,payload):
        for part in (name.encode(),payload):
            digest.update(len(part).to_bytes(8,'big'));digest.update(part)
    for name,array in (('vertices',vertices),('faces',faces)):
        field(name+'.dtype',array.dtype.str.encode())
        field(name+'.shape',json.dumps(array.shape).encode())
        field(name+'.data',array.tobytes(order='C'))
    field('voxel_size',str(voxel_size).encode())
    field('surface_script',SURFACE_SCRIPT.read_bytes() if script_bytes is None else script_bytes)
    field('bridge',Path(__file__).read_bytes())
    field('runtime',json.dumps(runtime,sort_keys=True).encode())
    return digest.hexdigest()


class SWLORClosedVoxelMesh:
    @classmethod
    def INPUT_TYPES(cls):
        settings()  # Object-info discovery must fail before expensive upstream work.
        return {'required':{'mesh':('MESH',),'voxel_size':('FLOAT',{'default':.003,'min':.0001,'max':.1,'step':.001}),
                            'target_face_count':('INT',{'default':3500,'min':100,'max':100000})}}
    RETURN_TYPES=('MESH','MESH')
    RETURN_NAMES=('closed_high','closed_low')
    FUNCTION='execute'
    CATEGORY='SWLOR/shields'

    @classmethod
    def IS_CHANGED(cls,mesh,voxel_size,target_face_count):
        executable,cache,runtime=settings()
        return hashlib.sha256(SURFACE_SCRIPT.read_bytes()+Path(__file__).read_bytes()+json.dumps((runtime,str(cache)),sort_keys=True).encode()).hexdigest()

    def execute(self,mesh,voxel_size,target_face_count):
        import numpy as np
        import torch
        from comfy_api.latest import Types
        executable,cache,runtime=settings()
        require_untextured(mesh)
        if len(mesh.vertices)!=1 or len(mesh.faces)!=1:raise ValueError('Expected exactly one shield mesh')
        vertices=mesh.vertices[0].detach().cpu().numpy().astype(np.float32)
        faces=mesh.faces[0].detach().cpu().numpy().astype(np.int32)
        script_bytes=SURFACE_SCRIPT.read_bytes();script_sha=hashlib.sha256(script_bytes).hexdigest()
        directory=cache/surface_cache_key(vertices,faces,voxel_size,runtime,script_bytes);create_durable_directories(directory)
        source=directory/'input.npz';high=directory/'closed_high.npz';low=directory/('closed_low_'+str(target_face_count)+'.npz')
        def run(mode,input_path,output_path,flags):
            command=[executable,'-b','--factory-startup','--python-exit-code','1','--python',str(SURFACE_SCRIPT),'--',mode,str(input_path),str(output_path),*flags]
            outputs=(output_path,output_path.with_suffix('.json'))
            try:
                with output_path.with_suffix('.log').open('w',encoding='utf-8') as log:
                    subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,timeout=600,check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
            except BaseException:
                discard_outputs(outputs);raise  # Unverified runs never leave a reusable cache pair.
            require_same_blender(executable,runtime,outputs)
            require_same_script(script_sha,outputs)
        # One run per cache key at a time: a failed run then only ever discards files it produced,
        # and a concurrent run finds the winner's published entry instead of racing it.
        with key_lock(directory):
            high_arrays=cached_entry(high)
            if high_arrays is None:
                np.savez(source,vertices=vertices,faces=faces)
                run('voxel',source,high,['--voxel-size',str(voxel_size)])
                source.unlink()
                high_arrays=cached_arrays(high)
            low_arrays=cached_entry(low)
            if low_arrays is None:
                run('decimate',high,low,['--target-faces',str(target_face_count)])
                low_arrays=cached_arrays(low)
        meshes=tuple(Types.MESH(vertices=torch.from_numpy(v).unsqueeze(0),faces=torch.from_numpy(f).unsqueeze(0)) for v,f in (high_arrays,low_arrays))
        provenance={'high_surface':str(high),'low_surface':str(low),'surface_script_sha256':hashlib.sha256(SURFACE_SCRIPT.read_bytes()).hexdigest(),'blender':runtime}
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
