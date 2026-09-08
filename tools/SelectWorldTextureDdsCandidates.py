"""Narrow an audited TGA-to-DDS allowlist without changing any source assets.

Requires Pillow. HakList paths are resolved relative to the supplied build config,
then rebased from its sibling SWLOR_Haks checkout to --source-root. DDS estimates
include the standard 128-byte header and every mip level through 1x1.

The required allowlist is the human-reviewed scope: these metadata checks do not
replace review of model emitter/lightmap uses or installed game resource metadata.
"""

import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath, PureWindowsPath
import re

from PIL import Image


EXCLUDED_HAKS = {"sw_ability", "sw_ui", "sw_item", "sw_weapon", "sw_load", "sw_vfx"}
# Legacy minimap names also use mi*/mz* without an underscore; their defining
# SET may exist only in the installed game, beyond the loose HAK metadata scan.
EXCLUDED_PREFIXES = (
    "mi", "mz", "iit_", "iki_", "iw", "ia", "ir_", "isk_", "fxpa_",
    "ihelm_", "if_", "pj_", "spi_",
)
MAP_SUFFIXES = (
    "_n", "_s", "_r", "_m", "_h", "_i", "_normal", "_norm", "_normals",
    "_specular", "_spec", "_roughness", "_rough", "_metallic", "_metalness",
    "_height", "_bump", "_gloss", "_glossiness", "_ao", "_opacity", "_alpha",
    "_illum", "_illumination", "_emissive",
)
TEXTURE_REFERENCE = re.compile(
    r"^\s*(texture\d+|bumpmaptexture|bumpyshinytexture|envmaptexture)\s+(\S+)",
    re.IGNORECASE,
)
SET_TEXTURE_REFERENCE = re.compile(r"^\s*(imagemap2d|envmap)\s*=\s*(\S+)", re.IGNORECASE)


def configured_directories(source_root: Path, config: Path) -> list[Path]:
    """Honor configured paths, including nested directories, without escaping roots."""
    source_root = source_root.resolve(strict=True)
    config = config.resolve(strict=True)
    nominal_root = (config.parent.parent / "SWLOR_Haks").resolve()
    entries = json.loads(config.read_text(encoding="utf-8-sig"))["HakList"]
    if not isinstance(entries, list) or not entries:
        raise ValueError("HakList must contain at least one directory")
    directories = set()
    for entry in entries:
        configured_path = Path(entry["Path"].replace("\\", "/"))
        resolved = (config.parent / configured_path).resolve()
        try:
            relative = resolved.relative_to(nominal_root)
        except ValueError as error:
            raise ValueError(f"Configured HAK path escapes SWLOR_Haks: {configured_path}") from error
        directory = (source_root / relative).resolve(strict=True)
        if not relative.parts or not directory.is_relative_to(source_root) or not directory.is_dir():
            raise ValueError(f"Invalid configured HAK directory: {configured_path}")
        directories.add(directory)
    return sorted(directories, key=lambda path: path.as_posix().casefold())


def resource_files(source_root: Path, directories: list[Path]) -> list[Path]:
    files = set()
    for directory in directories:
        for path in directory.rglob("*"):
            if not path.is_file():
                continue
            resolved = path.resolve(strict=True)
            if not resolved.is_relative_to(directory) or not resolved.is_relative_to(source_root):
                raise ValueError(f"Resource escapes configured directory: {path}")
            files.add(path)
    return sorted(files, key=lambda path: path.as_posix().casefold())


def normalized_resref(value: str) -> str:
    return Path(value.strip('\"\'').replace("\\", "/")).stem.casefold()


def protected_resources(files: list[Path]) -> tuple[set[str], set[str]]:
    """Resolve special texture uses across HAKs, since NWN resrefs are global."""
    protected = set()
    txi_siblings = set()
    for path in files:
        extension = path.suffix.casefold()
        if extension in {".dds", ".plt"}:
            protected.add(path.stem.casefold())
        elif extension == ".txi":
            # Sidecars resolve by resource name even when packed in another HAK.
            protected.add(path.stem.casefold())
            txi_siblings.add(path.with_suffix("").as_posix().casefold())
        if extension not in {".mtr", ".txi", ".set"}:
            continue
        for line in path.read_text(encoding="utf-8-sig", errors="replace").splitlines():
            if extension == ".set":
                match = SET_TEXTURE_REFERENCE.match(line)
                if match:
                    reference = normalized_resref(match[2])
                    protected.add(reference)
                    if match[1].casefold() == "envmap":
                        protected.update(f"{reference}{face}" for face in range(6))
                continue
            match = TEXTURE_REFERENCE.match(line)
            if not match:
                continue
            directive, reference = match[1].casefold(), normalized_resref(match[2])
            if directive.startswith("texture"):
                # Slot zero is the ordinary diffuse color texture.
                if extension == ".mtr" and int(directive[7:]) > 0:
                    protected.add(reference)
            elif extension == ".txi":
                protected.add(reference)
                if directive in {"envmaptexture", "bumpyshinytexture"}:
                    protected.update(f"{reference}{face}" for face in range(6))
    return protected, txi_siblings


def load_allowlist(path: Path | None) -> set[str] | None:
    if path is None:
        return None
    rows = json.loads(path.read_text(encoding="utf-8-sig"))
    if isinstance(rows, dict):
        rows = rows.get("entries")
    if not isinstance(rows, list):
        raise ValueError("Allowlist must be a JSON list of relative TGA paths")
    allowed = set()
    for row in rows:
        value = row.get("path") if isinstance(row, dict) else row
        if not isinstance(value, str) or not value:
            raise ValueError(f"Invalid allowlist path: {value!r}")
        relative = PurePosixPath(value.replace("\\", "/"))
        if (relative.is_absolute() or PureWindowsPath(value).drive
                or ".." in relative.parts or relative.suffix.casefold() != ".tga"):
            raise ValueError(f"Allowlist path must be a relative TGA path: {value}")
        allowed.add(relative.as_posix().casefold())
    return allowed


def dds_size(width: int, height: int, alpha: str) -> int:
    size = 128
    block_bytes = 8 if alpha == "opaque" else 16
    while True:
        size += max(1, (width + 3) // 4) * max(1, (height + 3) // 4) * block_bytes
        if width == height == 1:
            return size
        width, height = max(1, width // 2), max(1, height // 2)


def inspect_candidate(source_root: Path, path: Path) -> dict | None:
    data = path.read_bytes()
    with Image.open(io.BytesIO(data)) as texture:
        width, height = texture.size
        if texture.format != "TGA" or texture.mode == "P":
            return None
        # Small textures include icons scattered throughout otherwise ordinary
        # world HAKs. Keep both dimensions at least 128 for this reviewed batch.
        if any(value < 128 or value & (value - 1) for value in (width, height)):
            return None
        # Decode the actual alpha samples, including 32-bit TGAs whose descriptor
        # incorrectly declares zero alpha bits. Pillow retains their alpha byte.
        histogram = texture.convert("RGBA").getchannel("A").histogram()
    alpha = "opaque" if histogram[255] == width * height else (
        "binary" if not any(histogram[1:255]) else "smooth")
    expected = dds_size(width, height, alpha)
    if expected >= len(data):
        return None
    return {
        "path": path.relative_to(source_root).as_posix(),
        "source_sha256": hashlib.sha256(data).hexdigest(),
        "width": width,
        "height": height,
        "alpha": alpha,
        "format": "DXT1" if alpha == "opaque" else "DXT5",
        "source_bytes": len(data),
        "expected_dds_bytes": expected,
    }


def select_candidates(source_root: Path, config: Path, allowlist: Path | None = None,
                      workers: int = 4) -> list[dict]:
    if workers < 1:
        raise ValueError("Workers must be at least one")
    source_root = source_root.resolve(strict=True)
    files = resource_files(source_root, configured_directories(source_root, config))
    protected, txi_siblings = protected_resources(files)
    allowed = load_allowlist(allowlist)
    candidates = []
    for path in files:
        relative = path.relative_to(source_root)
        stem = path.stem.casefold()
        if path.suffix.casefold() != ".tga" or len(stem) > 16:
            continue
        if (relative.parts[0].casefold() in EXCLUDED_HAKS
                or stem.startswith(EXCLUDED_PREFIXES) or stem.endswith(MAP_SUFFIXES)
                or stem in protected or path.with_suffix("").as_posix().casefold() in txi_siblings):
            continue
        if allowed is not None and relative.as_posix().casefold() not in allowed:
            continue
        candidates.append(path)
    with ThreadPoolExecutor(max_workers=workers) as executor:
        rows = executor.map(lambda path: inspect_candidate(source_root, path), candidates)
        return [row for row in rows if row is not None]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", required=True, type=Path)
    parser.add_argument("--config", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path, help="New manifest JSON path")
    parser.add_argument("--allowlist", required=True, type=Path,
                        help="Human-reviewed JSON paths or a conversion report containing entries")
    parser.add_argument("--workers", type=int, default=min(8, os.cpu_count() or 1))
    arguments = parser.parse_args()
    if arguments.output.exists():
        parser.error(f"Output already exists: {arguments.output}")
    rows = select_candidates(arguments.source_root, arguments.config, arguments.allowlist, arguments.workers)
    with arguments.output.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(rows, stream, indent=2)
        stream.write("\n")
    original = sum(row["source_bytes"] for row in rows)
    expected = sum(row["expected_dds_bytes"] for row in rows)
    print(f"Selected {len(rows):,} textures: {original:,} -> {expected:,} bytes "
          f"({original - expected:,} bytes saved)")


if __name__ == "__main__":
    main()
