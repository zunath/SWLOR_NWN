"""Preserve the native argv and apply NWNX preload only when executing NWN."""

import json
import os
from pathlib import Path


arguments = json.loads((Path(os.environ["SWLOR_CRASH_CAPTURE_CONTROL"]) / "arguments.json").read_text())
environment = os.environ.copy()
for name in ("LD_PRELOAD", "LD_LIBRARY_PATH"):
    environment[name] = environment.get("NWN_" + name, "")
os.execvpe(arguments[0], arguments, environment)
