"""Kernel-owned lifetime backstop for an isolated client and its descendants.

https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_limit_information
"""
import ctypes
from ctypes import wintypes as w
from .job_extended_limits import JobExtendedLimits


class ProcessJob:
    def __init__(self):
        self.kernel=ctypes.WinDLL('kernel32',use_last_error=True)
        self.kernel.CreateJobObjectW.argtypes=[w.LPVOID,w.LPCWSTR]
        self.kernel.CreateJobObjectW.restype=w.HANDLE
        self.kernel.SetInformationJobObject.argtypes=[w.HANDLE,ctypes.c_int,w.LPVOID,w.DWORD]
        self.kernel.SetInformationJobObject.restype=w.BOOL
        self.kernel.TerminateJobObject.argtypes=[w.HANDLE,w.UINT]
        self.kernel.TerminateJobObject.restype=w.BOOL
        self.kernel.CloseHandle.argtypes=[w.HANDLE]
        self.kernel.CloseHandle.restype=w.BOOL
        self.handle=self.kernel.CreateJobObjectW(None,None)
        if not self.handle:
            raise ctypes.WinError(ctypes.get_last_error())
        try:
            limits=JobExtendedLimits()
            limits.BasicLimitInformation.LimitFlags=0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            if not self.kernel.SetInformationJobObject(self.handle,9,ctypes.byref(limits),ctypes.sizeof(limits)):
                raise ctypes.WinError(ctypes.get_last_error())
        except BaseException as error:
            try:
                self.close()
            except BaseException as cleanup_error:
                raise BaseExceptionGroup('Job initialization and cleanup failed',[error,cleanup_error])
            raise

    def terminate(self):
        """Request termination through the job independently of TerminateProcess."""
        if self.handle and not self.kernel.TerminateJobObject(self.handle,1):
            raise ctypes.WinError(ctypes.get_last_error())

    def close(self):
        """Closing the last non-inherited job handle kills any remaining members."""
        if self.handle:
            if not self.kernel.CloseHandle(self.handle):
                raise ctypes.WinError(ctypes.get_last_error())
            self.handle=None
