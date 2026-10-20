"""Optional ComfyUI bridge for SWLOR's separate shield workflow.

Link this directory into a task-owned ComfyUI custom_nodes directory. Set
SWLOR_BLENDER to Blender4.0's executable (or provide blender on PATH), and
SWLOR_SHIELD_CACHE to an absolute task-owned staging directory. Do not modify a
shared ComfyUI installation or enable this package in an unrelated service.

The nodes live in bridge.py. This loader reads it once and executes that same
buffer, so BRIDGE_BYTES (part of every surface cache key) always names the
implementation that is running, never a later copy on disk.
"""
from pathlib import Path as _Path

_IMPLEMENTATION=_Path(__file__).resolve().with_name('bridge.py')
BRIDGE_BYTES=_IMPLEMENTATION.read_bytes()
exec(compile(BRIDGE_BYTES,str(_IMPLEMENTATION),'exec'),globals())
