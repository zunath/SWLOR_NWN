"""Native IO_COUNTERS layout from the Windows SDK."""
import ctypes


class JobIoCounters(ctypes.Structure):
    _fields_ = [(name,ctypes.c_ulonglong) for name in
                ('ReadOperationCount','WriteOperationCount','OtherOperationCount',
                 'ReadTransferCount','WriteTransferCount','OtherTransferCount')]
