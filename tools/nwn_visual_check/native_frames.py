"""Read native NWN screenshots without displaying its private desktop."""
import ctypes
from ctypes import wintypes as w
from pathlib import Path
import time


class NativeFrames:
    """Request one fresh render from the owned client; retain no raw duplicates."""

    def __init__(self, process_id, desktop, profile):
        self.window = None
        self.frame = None
        self.directory = Path(profile)/'screenshots'
        self.user = ctypes.WinDLL('user32', use_last_error=True)
        callback = ctypes.WINFUNCTYPE(w.BOOL, w.HWND, w.LPARAM)
        self.user.EnumDesktopWindows.argtypes = [w.HANDLE, callback, w.LPARAM]
        self.user.EnumDesktopWindows.restype = w.BOOL
        self.user.GetWindowThreadProcessId.argtypes = [w.HWND, ctypes.POINTER(w.DWORD)]
        self.user.GetClientRect.argtypes = [w.HWND, ctypes.POINTER(w.RECT)]
        self.user.PostMessageW.argtypes = [w.HWND, w.UINT, w.WPARAM, w.LPARAM]
        self.user.PostMessageW.restype = w.BOOL

        @callback
        def found(hwnd, _):
            pid = w.DWORD()
            self.user.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
            rect = w.RECT()
            self.user.GetClientRect(hwnd, ctypes.byref(rect))
            if pid.value == process_id and (rect.right, rect.bottom) == (1280, 720):
                self.window = hwnd
            return True

        if not self.user.EnumDesktopWindows(desktop, found, 0):
            raise ctypes.WinError(ctypes.get_last_error())
        if self.window is None:
            raise RuntimeError('No owned 1280x720 client window')

    def wait_for_fresh(self, timeout=2):
        from PIL import Image
        self.frame = None
        before = {p: (p.stat().st_mtime_ns, p.stat().st_size) for p in self.directory.glob('*.tga')}
        # Default ScreenCapture binding in the task-owned review profile. These
        # messages target one private HWND; they never enter the user's input queue.
        for message, parameter in ((0x100, 0x01370001), (0x101, 0xc1370001)):
            if not self.user.PostMessageW(self.window, message, 0x2c, parameter):
                raise ctypes.WinError(ctypes.get_last_error())
        deadline = time.monotonic()+timeout
        while time.monotonic() < deadline:
            for path in self.directory.glob('*.tga'):
                stat = path.stat()
                if before.get(path) == (stat.st_mtime_ns, stat.st_size):
                    continue
                if stat.st_size > 16*1024*1024:
                    raise RuntimeError('Native screenshot exceeds the bounded frame size')
                try:
                    with Image.open(path) as image:
                        if image.size != (1280, 720):
                            raise RuntimeError('Native screenshot is not 1280x720')
                        frame = image.convert('RGB')
                    # Decoding can finish before the writer releases its delete lock.
                    # Only the new image produced by this request is disposable.
                    path.unlink()
                except OSError:
                    continue  # The client may still be writing this frame.
                self.frame = frame
                return True
            time.sleep(.025)
        return False

    def save(self, output):
        from PIL import ImageStat
        if self.frame is None and not self.wait_for_fresh():
            raise RuntimeError('Native client produced no fresh screenshot')
        if max(ImageStat.Stat(self.frame).stddev) < 8:
            self.frame.save(output.with_suffix('.blank.png'))
            raise RuntimeError('Blank client frame')
        self.frame.save(output)
        self.frame = None

    def close(self):
        self.frame = None
