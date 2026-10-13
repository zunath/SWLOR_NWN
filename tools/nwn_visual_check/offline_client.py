"""Windows native client isolation with no network capabilities.

The container has zero capabilities and no loopback exemption. Only its owned
workspace profile receives access. The installed application is read unchanged.
"""
import ctypes
from ctypes import wintypes as w
from pathlib import Path
import json
import re
import subprocess
import time
import uuid
import sys
from .profile import validate_profile
from .result import ClientRun
from .private_desktop import PrivateDesktop

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
    if sys.platform != "win32":
        raise RuntimeError("Native client qualification requires Windows AppContainer isolation")

def run(command, *, profile, cwd, timeout, on_start=None, until=None):
    require_supported()
    profile = Path(profile)
    validate_profile(profile, command, timeout)
    profile = profile.resolve()
    firewall=subprocess.run(['netsh','advfirewall','show','allprofiles'],check=True,capture_output=True,text=True).stdout
    if len(re.findall(r'^State\s+ON\s*$',firewall,re.M))!=3:
        raise RuntimeError('All Windows firewall profiles must enforce network isolation')
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
    sid_text=w.LPWSTR();attributes=None;process=PROCESS_INFORMATION();grant=False;capture_handle=None;input_handle=None;initialized=False;desktop=None
    evidence={'profile_name':name,'capabilities':[],'loopback_exemption':False,'timed_out':False}
    try:
        if not advapi.ConvertSidToStringSidW(sid,ctypes.byref(sid_text)):raise ctypes.WinError(ctypes.get_last_error())
        evidence['appcontainer_sid']=sid_text.value
        desktop=PrivateDesktop(sid_text.value)
        evidence.update(private_desktop=desktop.name,desktop_switched=False)
        profile.mkdir(parents=True,exist_ok=True)
        subprocess.run(['icacls',str(profile),'/grant',f'*{sid_text.value}:(OI)(CI)F','/T','/Q'],check=True,capture_output=True)
        grant=True
        subprocess.run(['icacls',str(profile),'/setintegritylevel','(OI)(CI)L','/T','/Q'],check=True,capture_output=True)
        size=ctypes.c_size_t()
        kernel.InitializeProcThreadAttributeList(None,1,0,ctypes.byref(size))
        attributes=ctypes.create_string_buffer(size.value)
        if not kernel.InitializeProcThreadAttributeList(attributes,1,0,ctypes.byref(size)):raise ctypes.WinError(ctypes.get_last_error())
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
        startup.lpAttributeList=ctypes.cast(attributes,w.LPVOID)
        line=ctypes.create_unicode_buffer(subprocess.list2cmdline(command))
        if not kernel.CreateProcessW(str(command[0]),line,None,None,True,0x80000|4,None,str(cwd),ctypes.byref(startup),ctypes.byref(process)):
            raise ctypes.WinError(ctypes.get_last_error())
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
        kernel.ResumeThread.argtypes=[w.HANDLE]
        if kernel.ResumeThread(process.hThread)==0xffffffff:raise ctypes.WinError(ctypes.get_last_error())
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
                kernel.TerminateProcess(process.hProcess,1)
                kernel.WaitForSingleObject(process.hProcess,5000)
                break
            if time.monotonic()>=deadline:
                evidence['timed_out']=True
                kernel.TerminateProcess(process.hProcess,1)
                kernel.WaitForSingleObject(process.hProcess,5000)
                break
        code=w.DWORD();kernel.GetExitCodeProcess(process.hProcess,ctypes.byref(code));evidence['exit_code']=code.value
    except BaseException as error:
        evidence['failure']=str(error)
        raise
    finally:
        if process.hProcess:
            if kernel.WaitForSingleObject(process.hProcess,0)==258:
                kernel.TerminateProcess(process.hProcess,1);kernel.WaitForSingleObject(process.hProcess,5000)
            evidence['process_exited'] = kernel.WaitForSingleObject(process.hProcess,0) == 0
            kernel.CloseHandle(process.hThread);kernel.CloseHandle(process.hProcess)
        if capture_handle:kernel.CloseHandle(capture_handle)
        if input_handle:kernel.CloseHandle(input_handle)
        if initialized:kernel.DeleteProcThreadAttributeList(attributes)
        if desktop:desktop.close()
        if grant:subprocess.run(['icacls',str(profile),'/remove:g',f'*{sid_text.value}','/T','/Q'],check=True,capture_output=True)
        if sid_text:kernel.LocalFree(ctypes.cast(sid_text,w.LPVOID))
        advapi.FreeSid(sid)
        hr=userenv.DeleteAppContainerProfile(name)
        if hr:raise OSError(f'DeleteAppContainerProfile: {hr:#x}')
        (profile/'isolation.json').write_text(json.dumps(evidence,indent=2)+'\n')
    if evidence.get('pid') and not evidence.get('process_exited'):
        raise RuntimeError('Owned client failed to exit')
    output=(profile/'process-output.log').read_text(encoding='utf-8',errors='replace')
    if evidence['timed_out']:
        raise subprocess.TimeoutExpired(command,timeout,output=output)
    return ClientRun(evidence['exit_code'],output,True,evidence.get('stopped_on_result',False))
