"""Shield bridge implementation. __init__.py executes these bytes in its own namespace and records
them as BRIDGE_BYTES; never import this file directly."""
import contextlib
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import time
import types
import uuid

SURFACE_SCRIPT=Path(__file__).resolve().parents[1]/'TrellisShieldSurface.py'
# One snapshot of the surface helper for the life of this process: the cache key, the copy Blender runs and the
# normal quantizer all use these bytes, so one workflow can never mix helper versions.
SURFACE_BYTES=SURFACE_SCRIPT.read_bytes()
SURFACE_SHA256=hashlib.sha256(SURFACE_BYTES).hexdigest()
VERSIONS={}


def blender_version(executable, executable_sha256, options=None):
    """Version of the build with this hash, probed once through the given (pinned) invocation."""
    if executable_sha256 not in VERSIONS:
        result=subprocess.run([executable,'--version'],capture_output=True,text=True,timeout=15,check=True,
                              creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0),**(options or {}))
        version=result.stdout.strip()
        if not version.splitlines() or not version.splitlines()[0].startswith('Blender 4.0.'):
            raise ValueError('Use tested Blender 4.0; executable reports: '+version)
        VERSIONS[executable_sha256]=version
    return VERSIONS[executable_sha256]


def open_without_write_sharing(path):
    """Windows read handle that shares only reading: while it is open, nobody can write, rename,
    replace or delete the file, but it can still be read and executed."""
    import ctypes,msvcrt
    from ctypes import wintypes
    kernel32=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel32.CreateFileW.restype=wintypes.HANDLE
    kernel32.CreateFileW.argtypes=[wintypes.LPCWSTR,wintypes.DWORD,wintypes.DWORD,wintypes.LPVOID,wintypes.DWORD,wintypes.DWORD,wintypes.HANDLE]
    handle=kernel32.CreateFileW(str(path),0x80000000,0x1,None,3,0x80,None)  # GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING
    if handle is None or handle==wintypes.HANDLE(-1).value:raise ctypes.WinError(ctypes.get_last_error())
    return os.fdopen(msvcrt.open_osfhandle(handle,os.O_RDONLY),'rb')


@contextlib.contextmanager
def pinned_blender(executable):
    """Hold the Blender executable so the bytes that are hashed are the bytes that run.

    Yields (argv0, subprocess options, identity, unchanged). On Windows the held handle blocks every write,
    rename and delete of the file. On Linux Blender is executed from the held descriptor itself, so a
    replaced path is never run. unchanged() re-hashes the held file and compares its size and mtime, which
    catches an in-place write (a running executable cannot be written, so a change shows up afterwards)."""
    path=Path(executable).resolve();descriptor=None
    if os.name=='nt':
        stream=open_without_write_sharing(path);argv0=str(path);options={}
    elif os.path.isdir('/proc/self/fd'):
        descriptor=os.open(path,os.O_RDONLY);stream=os.fdopen(descriptor,'rb',closefd=False)
        argv0='/proc/self/fd/'+str(descriptor);options={'pass_fds':(descriptor,)}
    else:raise ValueError('Shield reconstruction binds Blender through Windows or Linux file handles')
    try:
        def state():
            info=os.fstat(stream.fileno());return info.st_size,info.st_mtime_ns
        def held_sha256():
            stream.seek(0);digest=hashlib.sha256()
            for block in iter(lambda:stream.read(4*1024*1024),b''):digest.update(block)
            return digest.hexdigest()
        before=state();fingerprint=held_sha256()
        identity={'path':str(path),'sha256':fingerprint,'version':blender_version(argv0,fingerprint,options)}
        yield argv0,options,identity,lambda:state()==before and held_sha256()==fingerprint
    finally:
        stream.close()
        if descriptor is not None:os.close(descriptor)


def blender_identity(executable):
    with pinned_blender(executable) as (_,_,identity,_):return identity


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
                except OSError:time.sleep(1)  # LK_LOCK already retries for ~10 s; back off between rounds.
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


def require_same_script(path,script_sha):
    """The cache key names the hashed surface script; the private copy Blender ran must still be it."""
    try:same=hashlib.sha256(Path(path).read_bytes()).hexdigest()==script_sha
    except OSError:same=False
    if not same:raise ValueError('Shield surface script changed or could not be verified during reconstruction')


def discard_outputs(outputs):
    for stale in outputs:
        if stale.exists():stale.unlink()


def run_surface(blender,script_bytes,script_sha,directory,mode,input_path,output_path,flags):
    """Run one reconstruction step: the pinned Blender executes a private copy of the hashed script bytes.
    Outputs of a run that fails or cannot be tied to those bytes are discarded, never cached."""
    argv0,options,_,unchanged=blender
    outputs=(output_path,output_path.with_suffix('.json'));script=Path(directory)/('surface-'+uuid.uuid4().hex+'.py')
    try:
        with script.open('xb') as stream:stream.write(script_bytes)
        command=[argv0,'-b','--factory-startup','--python-exit-code','1','--python',str(script),'--',mode,str(input_path),str(output_path),*flags]
        with output_path.with_suffix('.log').open('w',encoding='utf-8') as log:
            subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,timeout=600,check=True,
                           creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0),**options)
        if not unchanged():raise ValueError('Blender executable changed or could not be verified during shield reconstruction')
        require_same_script(script,script_sha)
    except BaseException:
        discard_outputs(outputs);raise
    finally:
        if script.exists():script.unlink()


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


def settings(identify=True):
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
    return found,cache,(blender_identity(found) if identify else None)


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
    field('surface_script',SURFACE_BYTES if script_bytes is None else script_bytes)
    field('bridge',BRIDGE_BYTES)
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
        # Never let ComfyUI reuse a node result under an identity computed before execution: NaN never equals itself,
        # so execute() always runs, and its disk cache is keyed by the Blender build it pins and the helper it runs.
        return float('nan')

    def execute(self,mesh,voxel_size,target_face_count):
        import numpy as np
        import torch
        from comfy_api.latest import Types
        executable,cache,_=settings(identify=False)
        require_untextured(mesh)
        if len(mesh.vertices)!=1 or len(mesh.faces)!=1:raise ValueError('Expected exactly one shield mesh')
        vertices=mesh.vertices[0].detach().cpu().numpy().astype(np.float32)
        faces=mesh.faces[0].detach().cpu().numpy().astype(np.int32)
        script_bytes=SURFACE_BYTES;script_sha=SURFACE_SHA256
        # The executable stays pinned from hashing through the last run, so the cache key names what ran.
        with pinned_blender(executable) as blender:
            runtime=blender[2]
            directory=cache/surface_cache_key(vertices,faces,voxel_size,runtime,script_bytes);create_durable_directories(directory)
            source=directory/'input.npz';high=directory/'closed_high.npz';low=directory/('closed_low_'+str(target_face_count)+'.npz')
            def run(mode,input_path,output_path,flags):run_surface(blender,script_bytes,script_sha,directory,mode,input_path,output_path,flags)
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
        provenance={'high_surface':str(high),'low_surface':str(low),'surface_script_sha256':script_sha,'blender':runtime}
        return {'ui':{'text':[json.dumps(provenance,sort_keys=True)]},'result':meshes}


def surface_module():
    """The surface helper compiled from SURFACE_BYTES, the snapshot every node in this process uses."""
    module=types.ModuleType('swlor_shield_surface');module.__file__=str(SURFACE_SCRIPT)
    exec(compile(SURFACE_BYTES,str(SURFACE_SCRIPT),'exec'),module.__dict__)
    return module


class SWLORNormalUNorm8:
    @classmethod
    def INPUT_TYPES(cls):return {'required':{'images':('IMAGE',)}}
    RETURN_TYPES=('IMAGE',)
    FUNCTION='execute'
    CATEGORY='SWLOR/shields'

    def execute(self,images):
        import torch
        quantized=surface_module().normal_unorm8(images.detach().cpu().numpy())
        # GLB writing truncates float*255; stay inside the chosen byte interval.
        result=(torch.from_numpy(quantized).to(device=images.device,dtype=images.dtype)+.25)/255
        return (result.clamp(0,1),)


NODE_CLASS_MAPPINGS={'SWLORClosedVoxelMesh':SWLORClosedVoxelMesh,'SWLORNormalUNorm8':SWLORNormalUNorm8}
NODE_DISPLAY_NAME_MAPPINGS={'SWLORClosedVoxelMesh':'SWLOR Closed Shield Surface','SWLORNormalUNorm8':'SWLOR Normal UNORM8'}
