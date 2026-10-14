"""An undisplayed desktop for one network-isolated native client."""
import ctypes
from ctypes import wintypes as w
import uuid


_process_desktops = {}


class SecurityAttributes(ctypes.Structure):
    _fields_ = [('length', w.DWORD), ('descriptor', w.LPVOID), ('inherit', w.BOOL)]


class PrivateDesktop:
    def __init__(self, appcontainer_sid):
        self.user = ctypes.WinDLL('user32', use_last_error=True)
        self.kernel = ctypes.WinDLL('kernel32', use_last_error=True)
        self.security = ctypes.WinDLL('advapi32', use_last_error=True)
        self.user.CreateDesktopW.argtypes = [w.LPCWSTR, w.LPCWSTR, w.LPVOID, w.DWORD, w.DWORD, ctypes.POINTER(SecurityAttributes)]
        self.user.CreateDesktopW.restype = w.HANDLE
        self.user.CloseDesktop.argtypes = [w.HANDLE]
        self.user.CloseDesktop.restype = w.BOOL
        self.user.GetThreadDesktop.argtypes = [w.DWORD]
        self.user.GetThreadDesktop.restype = w.HANDLE
        self.user.GetUserObjectInformationW.argtypes = [w.HANDLE, ctypes.c_int, w.LPVOID, w.DWORD, ctypes.POINTER(w.DWORD)]
        self.user.GetUserObjectInformationW.restype = w.BOOL
        self.security.ConvertStringSecurityDescriptorToSecurityDescriptorW.argtypes = [w.LPCWSTR, w.DWORD, ctypes.POINTER(w.LPVOID), ctypes.POINTER(w.DWORD)]
        self.security.ConvertStringSecurityDescriptorToSecurityDescriptorW.restype = w.BOOL
        self.kernel.LocalFree.argtypes = [w.LPVOID]
        self.name = 'swlor-review-'+uuid.uuid4().hex
        self.handle = None
        self.pid = None
        descriptor = w.LPVOID()
        # USER initialization requires a complete desktop grant. This desktop is
        # private to this package and is never switched onto the user's display.
        sddl = f'D:P(A;;GA;;;SY)(A;;GA;;;IU)(A;;GA;;;{appcontainer_sid})S:(ML;;NW;;;LW)'
        if not self.security.ConvertStringSecurityDescriptorToSecurityDescriptorW(sddl, 1, ctypes.byref(descriptor), None):
            raise ctypes.WinError(ctypes.get_last_error())
        try:
            attributes = SecurityAttributes(ctypes.sizeof(SecurityAttributes), descriptor, False)
            self.handle = self.user.CreateDesktopW(self.name, None, None, 0, 0x10000000, ctypes.byref(attributes))
            if not self.handle:
                raise ctypes.WinError(ctypes.get_last_error())
        finally:
            self.kernel.LocalFree(descriptor)

    @property
    def path(self):
        return 'WinSta0\\'+self.name

    def bind(self, pid, profile):
        self.pid = pid
        _process_desktops[pid] = (self.handle, profile)

    def verify_thread(self, thread_id):
        handle = self.user.GetThreadDesktop(thread_id)
        if not handle:
            # A suspended or console-only thread has not initialized USER yet.
            return None
        name = ctypes.create_unicode_buffer(256)
        if not self.user.GetUserObjectInformationW(handle, 2, name, ctypes.sizeof(name), None):
            raise ctypes.WinError(ctypes.get_last_error())
        if name.value != self.name:
            raise RuntimeError('Native client thread is not on its private desktop')
        return name.value

    def close(self):
        if self.pid is not None:
            _process_desktops.pop(self.pid, None)
        if self.handle:
            if not self.user.CloseDesktop(self.handle):
                raise ctypes.WinError(ctypes.get_last_error())
            self.handle = None


def process_desktop(pid):
    record = _process_desktops.get(pid)
    return record[0] if record else None


def process_profile(pid):
    record = _process_desktops.get(pid)
    return record[1] if record else None
