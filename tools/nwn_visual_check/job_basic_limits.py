"""Native JOBOBJECT_BASIC_LIMIT_INFORMATION layout from the Windows SDK."""
import ctypes
from ctypes import wintypes as w


class JobBasicLimits(ctypes.Structure):
    _fields_ = [('PerProcessUserTimeLimit',ctypes.c_longlong),
                ('PerJobUserTimeLimit',ctypes.c_longlong),('LimitFlags',w.DWORD),
                ('MinimumWorkingSetSize',ctypes.c_size_t),('MaximumWorkingSetSize',ctypes.c_size_t),
                ('ActiveProcessLimit',w.DWORD),('Affinity',ctypes.c_size_t),
                ('PriorityClass',w.DWORD),('SchedulingClass',w.DWORD)]
