"""Windows native client isolation with no network capabilities.

The container has zero capabilities and no loopback exemption. Only its owned
workspace profile receives access. The installed application is read unchanged.
"""
import ctypes
from ctypes import wintypes as w
from pathlib import Path
import json
import subprocess
import time
import uuid
import sys
from .profile import validate_profile, cleanup as cleanup_profile
from .firewall import require_firewall
from .result import ClientRun
from .private_desktop import PrivateDesktop
from .process_job import ProcessJob

_retained_processes = {}

class STARTUPINFO(ctypes.Structure):
    _fields_ = [("cb",w.DWORD),("lpReserved",w.LPWSTR),("lpDesktop",w.LPWSTR),("lpTitle",w.LPWSTR),
                ("dwX",w.DWORD),("dwY",w.DWORD),("dwXSize",w.DWORD),("dwYSize",w.DWORD),
                ("dwXCountChars",w.DWORD),("dwYCountChars",w.DWORD),("dwFillAttribute",w.DWORD),
                ("dwFlags",w.DWORD),("wShowWindow",w.WORD),("cbReserved2",w.WORD),
                ("lpReserved2",ctypes.POINTER(w.BYTE)),("hStdInput",w.HANDLE),
                ("hStdOutput",w.HANDLE),("hStdError",w.HANDLE)]
class STARTUPINFOEX(ctypes.Structure):
    _fields_ = [("StartupInfo",STARTUPINFO),("lpAttributeList",w.LPVOID)]
class PROCESS_INFORMATION(ctypes.Structure):
    _fields_ = [("hProcess",w.HANDLE),("hThread",w.HANDLE),("dwProcessId",w.DWORD),("dwThreadId",w.DWORD)]
class SECURITY_CAPABILITIES(ctypes.Structure):
    _fields_ = [("AppContainerSid",w.LPVOID),("Capabilities",w.LPVOID),("CapabilityCount",w.DWORD),("Reserved",w.DWORD)]
class SECURITY_ATTRIBUTES(ctypes.Structure):
    _fields_ = [("nLength",w.DWORD),("lpSecurityDescriptor",w.LPVOID),("bInheritHandle",w.BOOL)]

def require_supported():
    if sys.platform != "win32" or sys.getwindowsversion().major < 10:
        raise RuntimeError("Native client qualification requires Windows 10+ AppContainer isolation")

def _delete_appcontainer_profile(userenv, name):
    """Unregister the temporary container, reporting native deletion failures."""
    result = userenv.DeleteAppContainerProfile(name)
    if result:
        raise OSError(f'DeleteAppContainerProfile: {result:#x}')


def _terminate_process(kernel, handle):
    """Return whether the direct termination request was accepted."""
    return bool(kernel.TerminateProcess(handle,1))


def _resume_thread(kernel, handle):
    """Declare the unsigned Win32 return before testing its failure sentinel."""
    kernel.ResumeThread.argtypes=[w.HANDLE]
    kernel.ResumeThread.restype=w.DWORD
    if kernel.ResumeThread(handle)==0xffffffff:
        raise ctypes.WinError(ctypes.get_last_error())


def has_retained_profile(profile):
    """Consult live ownership state even when isolation evidence could not be written."""
    return any(record[4] == Path(profile).resolve() for record in _retained_processes.values())


def retry_retained_processes():
    """Retry shutdown with retained handles if Windows previously refused exit."""
    errors=[]
    for pid,(kernel,process,job,desktop,profile) in list(_retained_processes.items()):
        try:
            if kernel.WaitForSingleObject(process.hProcess,0)!=0:
                _terminate_process(kernel,process.hProcess)
                job.close()
                if kernel.WaitForSingleObject(process.hProcess,5000)!=0:
                    raise RuntimeError(f'Owned process {pid} has not exited')
            if profile.exists():
                cleanup_profile(profile)
            job.close()
            if desktop:
                desktop.close()
            if process.hThread:
                if not kernel.CloseHandle(process.hThread):
                    raise ctypes.WinError(ctypes.get_last_error())
                process.hThread=None
            if not kernel.CloseHandle(process.hProcess):
                raise ctypes.WinError(ctypes.get_last_error())
            process.hProcess=None
            del _retained_processes[pid]
        except BaseException as error:
            errors.append(error)
    if errors:
        raise BaseExceptionGroup('Retained owned processes still need cleanup',errors)


def run(command, *, profile, cwd, timeout, on_start=None, until=None):
    require_supported()
    retry_retained_processes()
    profile = Path(profile)
    validate_profile(profile, command, timeout)
    profile = profile.resolve()
    require_firewall()
    userenv=ctypes.WinDLL('userenv',use_last_error=True)
    advapi=ctypes.WinDLL('advapi32',use_last_error=True)
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    userenv.CreateAppContainerProfile.argtypes=[w.LPCWSTR,w.LPCWSTR,w.LPCWSTR,w.LPVOID,w.DWORD,ctypes.POINTER(w.LPVOID)]
    userenv.CreateAppContainerProfile.restype=w.LONG
    userenv.DeleteAppContainerProfile.argtypes=[w.LPCWSTR]
    userenv.DeleteAppContainerProfile.restype=w.LONG
    advapi.ConvertSidToStringSidW.argtypes=[w.LPVOID,ctypes.POINTER(w.LPWSTR)]
    advapi.ConvertSidToStringSidW.restype=w.BOOL
    advapi.FreeSid.argtypes=[w.LPVOID]
    kernel.LocalFree.argtypes=[w.LPVOID]
    kernel.InitializeProcThreadAttributeList.argtypes=[w.LPVOID,w.DWORD,w.DWORD,ctypes.POINTER(ctypes.c_size_t)]
    kernel.InitializeProcThreadAttributeList.restype=w.BOOL
    kernel.UpdateProcThreadAttribute.argtypes=[w.LPVOID,w.DWORD,ctypes.c_size_t,w.LPVOID,ctypes.c_size_t,w.LPVOID,w.LPVOID]
    kernel.UpdateProcThreadAttribute.restype=w.BOOL
    kernel.DeleteProcThreadAttributeList.argtypes=[w.LPVOID]
    kernel.CreateProcessW.argtypes=[w.LPCWSTR,w.LPWSTR,w.LPVOID,w.LPVOID,w.BOOL,w.DWORD,w.LPVOID,w.LPCWSTR,ctypes.POINTER(STARTUPINFOEX),ctypes.POINTER(PROCESS_INFORMATION)]
    kernel.CreateProcessW.restype=w.BOOL
    kernel.WaitForSingleObject.argtypes=[w.HANDLE,w.DWORD]
    kernel.GetExitCodeProcess.argtypes=[w.HANDLE,ctypes.POINTER(w.DWORD)]
    kernel.TerminateProcess.argtypes=[w.HANDLE,w.UINT]
    kernel.CloseHandle.argtypes=[w.HANDLE]
    kernel.CreateFileW.argtypes=[w.LPCWSTR,w.DWORD,w.DWORD,ctypes.POINTER(SECURITY_ATTRIBUTES),w.DWORD,w.DWORD,w.HANDLE]
    kernel.CreateFileW.restype=w.HANDLE
    advapi.OpenProcessToken.argtypes=[w.HANDLE,w.DWORD,ctypes.POINTER(w.HANDLE)]
    advapi.GetTokenInformation.argtypes=[w.HANDLE,w.DWORD,w.LPVOID,w.DWORD,ctypes.POINTER(w.DWORD)]
    sid=w.LPVOID();name='swlor-review-'+uuid.uuid4().hex
    hr=userenv.CreateAppContainerProfile(name,name,'Owned temporary offline qualification probe',None,0,ctypes.byref(sid))
    if hr:raise OSError(f'CreateAppContainerProfile: {hr:#x}')
    sid_text=w.LPWSTR();attributes=None;process=PROCESS_INFORMATION();grant=False;capture_handle=None;input_handle=None;initialized=False;desktop=None;job=None
    evidence={'profile_name':name,'capabilities':[],'loopback_exemption':False,'timed_out':False}
    run_error = None
    try:
        if not advapi.ConvertSidToStringSidW(sid,ctypes.byref(sid_text)):raise ctypes.WinError(ctypes.get_last_error())
        evidence['appcontainer_sid']=sid_text.value
        desktop=PrivateDesktop(sid_text.value)
        evidence.update(private_desktop=desktop.name,desktop_switched=False)
        profile.mkdir(parents=True,exist_ok=True)
        grant=True  # Recursive grants can partially succeed before icacls reports an error.
        subprocess.run(['icacls',str(profile),'/grant',f'*{sid_text.value}:(OI)(CI)F','/T','/Q'],check=True,capture_output=True)
        subprocess.run(['icacls',str(profile),'/setintegritylevel','(OI)(CI)L','/T','/Q'],check=True,capture_output=True)
        job=ProcessJob()
        evidence['job_kill_on_close']=True
        size=ctypes.c_size_t()
        kernel.InitializeProcThreadAttributeList(None,3,0,ctypes.byref(size))
        attributes=ctypes.create_string_buffer(size.value)
        if not kernel.InitializeProcThreadAttributeList(attributes,3,0,ctypes.byref(size)):raise ctypes.WinError(ctypes.get_last_error())
        initialized=True
        caps=SECURITY_CAPABILITIES(sid,None,0,0)
        if not kernel.UpdateProcThreadAttribute(attributes,0,0x20009,ctypes.byref(caps),ctypes.sizeof(caps),None,None):raise ctypes.WinError(ctypes.get_last_error())
        startup=STARTUPINFOEX();startup.StartupInfo.cb=ctypes.sizeof(startup)
        startup.StartupInfo.lpDesktop=desktop.path
        security=SECURITY_ATTRIBUTES(ctypes.sizeof(SECURITY_ATTRIBUTES),None,True)
        capture_handle=kernel.CreateFileW(str(profile/'process-output.log'),0x40000000,3,ctypes.byref(security),2,0,None)
        if capture_handle==w.HANDLE(-1).value:
            capture_handle=None;raise ctypes.WinError(ctypes.get_last_error())
        startup.StartupInfo.dwFlags=1|0x100;startup.StartupInfo.wShowWindow=4
        startup.StartupInfo.hStdOutput=capture_handle;startup.StartupInfo.hStdError=capture_handle
        input_handle=kernel.CreateFileW('NUL',0x80000000,3,ctypes.byref(security),3,0,None)
        if input_handle==w.HANDLE(-1).value:
            input_handle=None;raise ctypes.WinError(ctypes.get_last_error())
        startup.StartupInfo.hStdInput=input_handle
        # stdout/stderr share one handle. Inherit only it and stdin, never the job
        # or any unrelated inheritable handle held by the calling interpreter.
        allowed_handles=(w.HANDLE*2)(input_handle,capture_handle)
        if not kernel.UpdateProcThreadAttribute(attributes,0,0x20002,allowed_handles,ctypes.sizeof(allowed_handles),None,None):
            raise ctypes.WinError(ctypes.get_last_error())
        jobs=(w.HANDLE*1)(job.handle)
        # JOB_LIST assigns the kill-on-close job atomically as the process is created.
        if not kernel.UpdateProcThreadAttribute(attributes,0,0x2000d,jobs,ctypes.sizeof(jobs),None,None):
            raise ctypes.WinError(ctypes.get_last_error())
        evidence['inherited_handles']='stdin and combined stdout/stderr only'
        startup.lpAttributeList=ctypes.cast(attributes,w.LPVOID)
        line=ctypes.create_unicode_buffer(subprocess.list2cmdline(command))
        if not kernel.CreateProcessW(str(command[0]),line,None,None,True,0x80000|4,None,str(cwd),ctypes.byref(startup),ctypes.byref(process)):
            raise ctypes.WinError(ctypes.get_last_error())
        evidence['job_assigned_at_creation']=True
        token=w.HANDLE()
        if not advapi.OpenProcessToken(process.hProcess,8,ctypes.byref(token)):raise ctypes.WinError(ctypes.get_last_error())
        try:
            actual=w.DWORD();length=w.DWORD()
            if not advapi.GetTokenInformation(token,29,ctypes.byref(actual),4,ctypes.byref(length)):raise ctypes.WinError(ctypes.get_last_error())
            if actual.value!=1:raise RuntimeError('Child is not an AppContainer')
            buf=ctypes.create_string_buffer(1024)
            if not advapi.GetTokenInformation(token,30,buf,len(buf),ctypes.byref(length)):raise ctypes.WinError(ctypes.get_last_error())
            count=ctypes.cast(buf,ctypes.POINTER(w.DWORD)).contents.value
            if count:raise RuntimeError('Child unexpectedly has capabilities')
            evidence['token_is_appcontainer']=True;evidence['token_capability_count']=count
        finally:kernel.CloseHandle(token)
        _resume_thread(kernel,process.hThread)
        evidence['pid']=process.dwProcessId
        desktop.bind(process.dwProcessId,profile)
        deadline=time.monotonic()+timeout
        if on_start:on_start(process.dwProcessId)
        while kernel.WaitForSingleObject(process.hProcess,250)==258:
            if not evidence.get('verified_thread_desktop'):
                actual_desktop=desktop.verify_thread(process.dwThreadId)
                if actual_desktop:evidence['verified_thread_desktop']=actual_desktop
            if until is not None and until():
                evidence['stopped_on_result']=True
                break
            if time.monotonic()>=deadline:
                evidence['timed_out']=True
                break
    except BaseException as error:
        run_error = error
        evidence['failure']=str(error)
        raise
    finally:
        cleanup_errors = []

        def clean(step, action):
            """Attempt every release even when an earlier release fails."""
            try:
                action()
            except BaseException as error:
                cleanup_errors.append(error)
                evidence.setdefault('cleanup_errors', []).append({'step':step,'error':str(error)})

        def stop_owned_process():
            """Keep the process handle until direct/job/kill-on-close shutdown settles."""
            if kernel.WaitForSingleObject(process.hProcess,0)!=0:
                if _terminate_process(kernel,process.hProcess):
                    kernel.WaitForSingleObject(process.hProcess,5000)
                if kernel.WaitForSingleObject(process.hProcess,0)!=0:
                    evidence['termination_fallback']='job'
                    try:
                        job.terminate()
                    except OSError as error:
                        evidence['job_termination_error']=str(error)
                    kernel.WaitForSingleObject(process.hProcess,5000)
                if kernel.WaitForSingleObject(process.hProcess,0)!=0:
                    evidence['termination_fallback']='kill-on-close'
                    job.close()
                    kernel.WaitForSingleObject(process.hProcess,5000)
            evidence['process_exited'] = kernel.WaitForSingleObject(process.hProcess,0) == 0
            if not evidence['process_exited']:
                raise RuntimeError('Owned client failed to exit; control handles retained')
            code=w.DWORD()
            if not kernel.GetExitCodeProcess(process.hProcess,ctypes.byref(code)):
                raise ctypes.WinError(ctypes.get_last_error())
            evidence['exit_code']=code.value

        if process.hProcess:
            clean('stop process', stop_owned_process)
            if evidence.get('process_exited'):
                clean('close process job', job.close)
                clean('close thread handle', lambda: kernel.CloseHandle(process.hThread))
                clean('close process handle', lambda: kernel.CloseHandle(process.hProcess))
            else:
                _retained_processes[process.dwProcessId]=(kernel,process,job,desktop,profile)
                evidence['process_control_retained']=True
        elif job:
            clean('close process job', job.close)
        if capture_handle:
            clean('close capture handle', lambda: kernel.CloseHandle(capture_handle))
        if input_handle:
            clean('close input handle', lambda: kernel.CloseHandle(input_handle))
        if initialized:
            clean('delete process attributes', lambda: kernel.DeleteProcThreadAttributeList(attributes))
        if desktop and not evidence.get('process_control_retained'):
            clean('close desktop', desktop.close)
        if grant:
            clean('revoke profile ACL', lambda: subprocess.run(
                ['icacls',str(profile),'/remove:g',f'*{sid_text.value}','/T','/Q'],
                check=True,capture_output=True))
        if sid_text:
            clean('free SID text', lambda: kernel.LocalFree(ctypes.cast(sid_text,w.LPVOID)))
        clean('free SID', lambda: advapi.FreeSid(sid))
        clean('delete AppContainer profile', lambda: _delete_appcontainer_profile(userenv,name))
        clean('write isolation evidence', lambda: (profile/'isolation.json').write_text(
            json.dumps(evidence,indent=2)+'\n'))
        if cleanup_errors:
            errors = ([run_error] if run_error is not None else []) + cleanup_errors
            raise BaseExceptionGroup('Native client run/cleanup failed', errors)
    if evidence.get('pid') and not evidence.get('process_exited'):
        raise RuntimeError('Owned client failed to exit')
    output=(profile/'process-output.log').read_text(encoding='utf-8',errors='replace')
    if evidence['timed_out']:
        raise subprocess.TimeoutExpired(command,timeout,output=output)
    return ClientRun(evidence['exit_code'],output,True,evidence.get('stopped_on_result',False))
