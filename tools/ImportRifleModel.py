"""Import a reviewed rigid GR2 rifle using base item 7 / WBwXl attachment space.

Run in Blender 4.0 with the same CLI arguments as ImportBlasterModel.py.
Rifle manifests require source_material, +X muzzle / -Z grip after transformation,
and preserve_emission=true to retain the SWTOR packed emission channel.
Cannons are deliberately rejected, including both as_a0x and assaultcannon names.
"""
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from ImportBlasterModel import main, validate_manifest


def validate_rifle(config):
    validate_manifest(config, "rifle")
    source = Path(config["sources"]["model"]).name.lower()
    if not source.startswith("rifle_") or not source.endswith(".gr2"):
        raise ValueError("Only rifle_ GR2 meshes belong in the rifle workflow; exclude cannons")
    if not config.get("source_material") or config.get("preserve_emission") is not True:
        raise ValueError("Inspect source_material and explicitly preserve source emission")


if __name__ == "__main__":
    arguments = sys.argv[sys.argv.index("--") + 1:]
    manifest = Path(arguments[arguments.index("--manifest") + 1])
    validate_rifle(json.loads(manifest.read_text(encoding="utf-8-sig")))
    main("rifle")
