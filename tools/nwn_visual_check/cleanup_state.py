"""Owned cleanup records shared by later invocations and repository copies."""
from pathlib import Path
import json
import os
import re
import tempfile
import ctypes
from ctypes import wintypes as w
from . import profile
from .profile import reject_links


def state_root():
    return Path(os.environ['LOCALAPPDATA'])/'SWLOR/NwnVisualChecks/cleanup'


def validate_name(name):
    if not isinstance(name,str) or not re.fullmatch('swlor-review-[0-9a-f]{32}',name):
        raise ValueError('Cleanup state must name an owned visual-verification container')


def _write_record(filename, payload):
    root=state_root()
    reject_links(root)
    root.mkdir(parents=True,exist_ok=True)
    temporary=None
    try:
        with tempfile.NamedTemporaryFile(mode='w',encoding='utf8',dir=root,prefix='.write-',suffix='.tmp',delete=False) as stream:
            temporary=Path(stream.name)
            json.dump(payload,stream)
            stream.flush()
            os.fsync(stream.fileno())
        temporary.replace(root/filename)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def _forget_record(filename):
    root=state_root()
    reject_links(root)
    (root/filename).unlink(missing_ok=True)


def retain_container(name):
    """Atomically retain only the tool's unique container name."""
    validate_name(name)
    _write_record(name+'.json',{'name':name})


def forget_container(name):
    validate_name(name)
    _forget_record(name+'.json')


def validate_token(token):
    if not isinstance(token,str) or not re.fullmatch('[0-9a-f]{32}',token):
        raise ValueError('Cleanup state must contain the owned profile token')


def _profile_record(path, pid=None):
    path=Path(path).absolute()
    profile.validate_owned(path)
    path=path.resolve()
    token=json.loads((path/profile.MARKER).read_text(encoding='utf8'))['token']
    validate_token(token)
    if pid is not None and (type(pid) is not int or not 0<pid<=0xffffffff):
        raise ValueError('Cleanup state has an invalid process ID')
    return {'profile':str(path),'token':token,'pid':pid}


def retain_profile(path, pid=None):
    """Persist ownership when a live client prevents an immediate removal."""
    record=_profile_record(path,pid)
    _write_record('profile-'+record['token']+'.json',record)
    return record['token']


def remove_owned_profile(path, cleanup=None):
    """Snapshot ownership, publishing recovery state only when removal fails."""
    record=_profile_record(path)
    try:
        (cleanup or profile.cleanup)(Path(record['profile']))
    except BaseException as error:
        try:
            _write_record('profile-'+record['token']+'.json',record)
        except BaseException as journal_error:
            raise BaseExceptionGroup('Profile cleanup and ownership recording failed',[error,journal_error])
        raise
    forget_profile(record['token'])


def forget_profile(token):
    validate_token(token)
    _forget_record('profile-'+token+'.json')


def _records():
    root=state_root()
    reject_links(root)
    result=[]
    if not root.exists():
        return result
    for path in sorted(root.glob('*.json')):
        try:
            record=json.loads(path.read_text(encoding='utf8'))
        except FileNotFoundError:
            continue  # Another invocation finished this record.
        if not isinstance(record,dict):
            raise ValueError('Cleanup record is not an ownership object')
        if 'name' in record:
            if set(record)!={'name'}:
                raise ValueError('Container cleanup record has unexpected fields')
            validate_name(record['name'])
            expected=record['name']+'.json'
        else:
            if set(record)!={'profile','token','pid'}:
                raise ValueError('Profile cleanup record has unexpected fields')
            validate_token(record.get('token'))
            candidate=Path(record.get('profile',''))
            if not candidate.is_absolute() or candidate.parent.resolve()!=Path(tempfile.gettempdir()).resolve() or not candidate.name.startswith('swlor-nwn-'):
                raise ValueError('Cleanup record does not identify an owned temporary profile')
            pid=record.get('pid')
            if pid is not None and (type(pid) is not int or not 0<pid<=0xffffffff):
                raise ValueError('Cleanup record has an invalid process ID')
            expected='profile-'+record['token']+'.json'
        if path.name!=expected:
            raise ValueError('Cleanup filename does not match its ownership record')
        result.append(record)
    return result


def pending_containers():
    return [record['name'] for record in _records() if 'name' in record]


def pending_profiles():
    return [record for record in _records() if 'profile' in record]


def process_is_running(pid):
    """Read process state without terminating or modifying a possibly reused PID."""
    if pid is None:
        return False
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.OpenProcess.argtypes=[w.DWORD,w.BOOL,w.DWORD]
    kernel.OpenProcess.restype=w.HANDLE
    kernel.WaitForSingleObject.argtypes=[w.HANDLE,w.DWORD]
    kernel.WaitForSingleObject.restype=w.DWORD
    kernel.CloseHandle.argtypes=[w.HANDLE]
    kernel.CloseHandle.restype=w.BOOL
    handle=kernel.OpenProcess(0x100000,False,pid)
    if not handle:
        error=ctypes.get_last_error()
        if error==87:  # ERROR_INVALID_PARAMETER: no such PID.
            return False
        raise ctypes.WinError(error)
    try:
        status=kernel.WaitForSingleObject(handle,0)
        if status==0:
            return False
        if status==258:
            return True
        raise ctypes.WinError(ctypes.get_last_error())
    finally:
        if not kernel.CloseHandle(handle):
            raise ctypes.WinError(ctypes.get_last_error())


def recover_profile(record):
    """Delete only an exited client's directory with the original ownership token."""
    path=Path(record['profile'])
    if not path.exists():
        forget_profile(record['token'])
        return
    if process_is_running(record.get('pid')):
        raise RuntimeError('Profile cleanup deferred while its recorded PID is active')
    if path.exists():
        profile.validate_owned(path)
        actual=json.loads((path/profile.MARKER).read_text(encoding='utf8'))['token']
        if actual!=record['token']:
            raise ValueError('Retained profile ownership token changed')
        profile.cleanup(path)
    forget_profile(record['token'])
