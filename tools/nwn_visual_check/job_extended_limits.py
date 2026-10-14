"""Native JOBOBJECT_EXTENDED_LIMIT_INFORMATION layout from the Windows SDK."""
import ctypes
from .job_basic_limits import JobBasicLimits
from .job_io_counters import JobIoCounters


class JobExtendedLimits(ctypes.Structure):
    _fields_ = [('BasicLimitInformation',JobBasicLimits),('IoInfo',JobIoCounters),
                ('ProcessMemoryLimit',ctypes.c_size_t),('JobMemoryLimit',ctypes.c_size_t),
                ('PeakProcessMemoryUsed',ctypes.c_size_t),('PeakJobMemoryUsed',ctypes.c_size_t)]
