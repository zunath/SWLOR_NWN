"""Keep the crafting minigame parameters that tools/SWLOR.RecipeParameters writes into generated recipe files.

The generators here write each recipe chain from the catalog; the parameter tool then appends a `.<Key>Parameters(...)`
call to each chain. A generator that rewrote its file without them would strip every recipe's settings, so it reads the
calls back from the current file by RecipeType and emits them again at the end of the same chain.
"""
import re
from pathlib import Path

_CHAIN = re.compile(r"_builder\.Create\(RecipeType\.(\w+),(.*?);", re.S)
_CALL = re.compile(r"\.(\w+Parameters)\(([^)]*)\)")


def existing_calls(path):
    """RecipeType name to its chain's `.<Key>Parameters(...)` call in the file at `path`, without the dot."""
    path = Path(path)
    if not path.exists():
        return {}
    text = path.read_text(encoding="utf-8-sig")
    calls = {}
    for enum, body in _CHAIN.findall(text):
        match = _CALL.search(body)
        if match:
            calls[enum] = f"{match[1]}({match[2]})"
    return calls
