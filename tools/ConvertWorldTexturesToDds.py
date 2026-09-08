"""Stage explicitly reviewed world textures as NWN-compatible BC1/BC3 DDS.

Requires the existing Pillow and ImageMagick tools. This never changes source
assets. The allowlist is a JSON array of objects containing path (relative TGA
path), source_sha256, width, height, alpha (opaque/nonopaque/binary/graded), and
format (DXT1/DXT5). Only power-of-two textures at least 4x4 qualify here.
ImageMagick cluster-fit is enabled explicitly to improve block-color fitting;
the report records that setting alongside the encoder version and image metrics.
See https://imagemagick.org/defines/#dds:cluster-fit for the encoder option.

Examples:
  python tools/ConvertWorldTexturesToDds.py stage --manifest candidates.json \
      --source-root SWLOR_Haks --output-root staged-dds --report conversions.json
  python tools/ConvertWorldTexturesToDds.py validate --manifest conversions.json \
      --asset-root SWLOR_Haks

Staging requires new output/report paths. A failed batch may leave staged files,
but produces no complete report and never installs them. Existing TXIs are not
rewritten. Review metrics and representative textures before installing outputs.
"""
from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import struct
import subprocess
import tempfile

from PIL import Image, ImageChops, ImageStat


SCHEMA_VERSION = 1
ALPHA_KINDS = {"opaque", "nonopaque", "binary", "graded", "smooth"}


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def resource_path(value: str, extension: str) -> PurePosixPath:
    """Manifest paths are portable relative paths, never filesystem commands."""
    if not isinstance(value, str) or not value or "\\" in value or ":" in value:
        raise ValueError(f"Unsafe resource path: {value!r}")
    path = PurePosixPath(value)
    if (path.is_absolute() or any(part in ("", ".", "..") for part in value.split("/"))
            or path.suffix.lower() != extension or path.as_posix() != value):
        raise ValueError(f"Unsafe resource path: {value!r}")
    return path


def under_root(root: Path, relative: str, extension: str) -> Path:
    resolved_root = root.resolve()
    path = root.joinpath(*resource_path(relative, extension).parts).resolve()
    if not path.is_relative_to(resolved_root):
        raise ValueError(f"Resource escapes its root: {relative} (root={resolved_root}, resolved={path})")
    return path


def staging_path(root: Path, relative: str) -> Path:
    """Resolve the existing parent, never a missing leaf during parallel writes."""
    resource = resource_path(relative, ".dds")
    resolved_root = root.resolve(strict=True)
    parent = root.joinpath(*resource.parts[:-1]).resolve(strict=True)
    if not parent.is_relative_to(resolved_root):
        raise ValueError(f"Resource escapes its root: {relative} (root={resolved_root}, resolved={parent})")
    # open('xb') rejects existing files and links at this leaf. Resolving a
    # nonexistent full path on Windows uses a nonstrict ancestor fallback;
    # avoid that fallback while other workers are creating nearby files.
    return parent / resource.name


def prepare_staging_directories(root: Path, rows: list[dict]) -> None:
    """Create and check every output directory before starting worker threads."""
    resolved_root = root.resolve(strict=True)
    parents = {resource_path(row["path"], ".tga").parent for row in rows}
    for relative in sorted(parents, key=lambda path: (len(path.parts), path.as_posix())):
        parent = resolved_root
        for part in relative.parts:
            directory = parent / part
            if not directory.exists():
                directory.mkdir()
            resolved = directory.resolve(strict=True)
            if not resolved.is_relative_to(resolved_root):
                raise ValueError(f"Staging directory escapes its root: {relative} "
                                 f"(root={resolved_root}, resolved={resolved})")
            parent = resolved


def check_digest(value: str, label: str) -> None:
    if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
        raise ValueError(f"Invalid {label} SHA-256")


def mip_dimensions(width: int, height: int) -> list[tuple[int, int]]:
    if (type(width) is not int or type(height) is not int or width < 4 or height < 4
            or width & (width - 1) or height & (height - 1)):
        raise ValueError("Conservative conversion requires power-of-two dimensions >= 4")
    levels = [(width, height)]
    while width > 1 or height > 1:
        width, height = max(1, width // 2), max(1, height // 2)
        levels.append((width, height))
    return levels


def expected_dds_size(width: int, height: int, fmt: str) -> int:
    if fmt not in ("DXT1", "DXT5"):
        raise ValueError(f"Unsupported DDS format: {fmt}")
    block_bytes = 8 if fmt == "DXT1" else 16
    return 128 + sum(((w + 3) // 4) * ((h + 3) // 4) * block_bytes
                     for w, h in mip_dimensions(width, height))


def validate_candidates(rows: object) -> list[dict]:
    if not isinstance(rows, list) or not rows:
        raise ValueError("The allowlist must be a nonempty JSON array")
    paths, outputs = set(), set()
    for row in rows:
        if not isinstance(row, dict):
            raise ValueError("Every allowlist entry must be an object")
        if not {"path", "source_sha256", "width", "height", "alpha", "format"} <= row.keys():
            raise ValueError("Incomplete allowlist entry")
        path = resource_path(row["path"], ".tga")
        output = path.with_suffix(".dds").as_posix()
        if path.as_posix().casefold() in paths or output.casefold() in outputs:
            raise ValueError(f"Duplicate conversion path: {path}")
        paths.add(path.as_posix().casefold())
        outputs.add(output.casefold())
        check_digest(row["source_sha256"], "source")
        mip_dimensions(row["width"], row["height"])
        if row["alpha"] not in ALPHA_KINDS:
            raise ValueError(f"Invalid alpha classification: {row['alpha']}")
        fmt = "DXT1" if row["alpha"] == "opaque" else "DXT5"
        if row["format"] != fmt:
            raise ValueError(f"Alpha/format mismatch: {path}")
    return rows


def decode_tga(data: bytes) -> Image.Image:
    if len(data) < 18:
        raise ValueError("Truncated TGA header")
    width, height = struct.unpack_from("<HH", data, 12)
    if not width or not height or data[17] & 0xC0:
        raise ValueError("Invalid or interleaved TGA")
    # Explicit format prevents other Pillow plugins from probing tiny legacy
    # files as formats with mandatory footers. A TGA 2.0 footer is optional.
    try:
        with Image.open(io.BytesIO(data), formats=["TGA"]) as source:
            source.load()
            if source.size != (width, height):
                raise ValueError("TGA dimensions disagree with its header")
            # Pillow honors both horizontal and vertical TGA origin flags.
            # Conversion to RGBA retains RGB values even beneath zero alpha.
            return source.convert("RGBA")
    except (OSError, SyntaxError) as error:
        raise ValueError(f"Cannot decode TGA: {error}") from error


def alpha_kind(image: Image.Image) -> str:
    histogram = image.getchannel("A").histogram()
    if sum(histogram[:255]) == 0:
        return "opaque"
    return "graded" if any(histogram[1:255]) else "binary"


def verify_source(root: Path, row: dict) -> tuple[bytes, Image.Image]:
    path = under_root(root, row["path"], ".tga")
    raw = path.read_bytes()
    if sha256(raw) != row["source_sha256"]:
        raise ValueError(f"Source hash changed: {row['path']}")
    image = decode_tga(raw)
    if image.size != (row["width"], row["height"]):
        raise ValueError(f"Source dimensions changed: {row['path']}")
    actual = alpha_kind(image)
    expected = "graded" if row["alpha"] == "smooth" else row["alpha"]
    if actual != expected and not (expected == "nonopaque" and actual != "opaque"):
        raise ValueError(f"Source alpha classification changed: {row['path']}")
    if ("DXT1" if actual == "opaque" else "DXT5") != row["format"]:
        raise ValueError(f"Source alpha requires a different DDS format: {row['path']}")
    return raw, image


def validate_dds(data: bytes, width: int, height: int, fmt: str) -> None:
    expected_size = expected_dds_size(width, height, fmt)
    if len(data) < 128 or data[:4] != b"DDS ":
        raise ValueError("Incomplete or nonstandard DDS header")
    if struct.unpack_from("<I", data, 4)[0] != 124 or struct.unpack_from("<I", data, 76)[0] != 32:
        raise ValueError("Invalid DDS header sizes")
    if struct.unpack_from("<II", data, 12) != (height, width):
        raise ValueError("DDS dimensions changed")
    flags = struct.unpack_from("<I", data, 8)[0]
    required_flags = 0x1 | 0x2 | 0x4 | 0x1000 | 0x20000 | 0x80000
    if flags & required_flags != required_flags:
        raise ValueError("DDS must declare dimensions, format, linear size, and mip count")
    if struct.unpack_from("<I", data, 80)[0] != 4 or data[84:88] != fmt.encode("ascii"):
        raise ValueError("DDS compressed format changed")
    levels = mip_dimensions(width, height)
    if struct.unpack_from("<I", data, 28)[0] != len(levels):
        raise ValueError("DDS does not contain a complete mip chain")
    block_bytes = 8 if fmt == "DXT1" else 16
    top_size = ((width + 3) // 4) * ((height + 3) // 4) * block_bytes
    if struct.unpack_from("<I", data, 20)[0] != top_size:
        raise ValueError("DDS top-level linear size changed")
    caps, caps2 = struct.unpack_from("<II", data, 108)
    if caps & (0x1000 | 0x8 | 0x400000) != (0x1000 | 0x8 | 0x400000) or caps2 != 0:
        raise ValueError("DDS must be a two-dimensional mipmapped texture")
    if struct.unpack_from("<I", data, 24)[0] != 0:
        raise ValueError("Volume DDS is not supported")
    if len(data) != expected_size:
        raise ValueError(f"DDS payload length {len(data)} != expected full mip chain {expected_size}")


def decode_nwn_dds(data: bytes) -> Image.Image:
    with Image.open(io.BytesIO(data), formats=["DDS"]) as image:
        image.load()
        # Standard DDS scanlines are stored bottom-up for NWN, as in the
        # established portrait converter and the toolset's TextureLoader.
        return image.convert("RGBA").transpose(Image.Transpose.FLIP_TOP_BOTTOM)


def rgb_grid(image: Image.Image) -> str:
    rgb = image.convert("RGB")
    result = bytearray()
    for y in range(8):
        for x in range(8):
            left, top = x * image.width // 8, y * image.height // 8
            right = max(left + 1, (x + 1) * image.width // 8)
            bottom = max(top + 1, (y + 1) * image.height // 8)
            cell = rgb.crop((left, top, right, bottom))
            result.extend(int(value) // (cell.width * cell.height)
                          for value in ImageStat.Stat(cell).sum)
    return result.hex()


def image_metrics(source: Image.Image, decoded: Image.Image) -> dict:
    if source.size != decoded.size:
        raise ValueError("Decoded DDS dimensions changed")
    source_rgb, decoded_rgb = source.convert("RGB"), decoded.convert("RGB")
    difference = ImageChops.difference(source_rgb, decoded_rgb)
    alpha_difference = ImageChops.difference(source.getchannel("A"), decoded.getchannel("A"))
    alternatives = {"intended": decoded_rgb,
                    "vertical_flip": decoded_rgb.transpose(Image.Transpose.FLIP_TOP_BOTTOM),
                    "horizontal_flip": decoded_rgb.transpose(Image.Transpose.FLIP_LEFT_RIGHT),
                    "rotation_180": decoded_rgb.transpose(Image.Transpose.ROTATE_180)}
    orientation = {name: round(sum(ImageStat.Stat(ImageChops.difference(source_rgb, image)).mean) / 3, 6)
                   for name, image in alternatives.items()}
    transparent = source.getchannel("A").point(lambda value: 255 if value == 0 else 0)
    transparent_error = (round(sum(ImageStat.Stat(difference, transparent).mean) / 3, 6)
                         if transparent.getbbox() else None)
    return {"rgb_mae": orientation["intended"],
            "rgb_max_error": max(high for _, high in difference.getextrema()),
            "alpha_mae": round(ImageStat.Stat(alpha_difference).mean[0], 6),
            "alpha_max_error": alpha_difference.getextrema()[1],
            "transparent_rgb_mae": transparent_error,
            "orientation_rgb_mae": orientation}


def convert_one(source_root: Path, output_root: Path, row: dict, magick: str) -> dict:
    raw, source = verify_source(source_root, row)
    output_name = PurePosixPath(row["path"]).with_suffix(".dds").as_posix()
    destination = staging_path(output_root, output_name)
    if destination.exists():
        raise ValueError(f"Refusing to overwrite staged output: {output_name}")
    levels = mip_dimensions(row["width"], row["height"])
    with tempfile.TemporaryDirectory(prefix="world-dds-") as temporary:
        scratch = Path(temporary)
        # Preserve hidden RGB; do not flatten, premultiply, or composite alpha.
        stored = source.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        stored.save(scratch / "source.png")
        subprocess.run([magick, str(scratch / "source.png"),
                        "-define", "dds:compression=" + row["format"].lower(),
                        "-define", "dds:cluster-fit=true",
                        "-define", "dds:mipmaps=" + str(len(levels) - 1),
                        str(scratch / "texture.dds")], check=True,
                       capture_output=True, timeout=300)
        encoded = (scratch / "texture.dds").read_bytes()
    validate_dds(encoded, row["width"], row["height"], row["format"])
    decoded = decode_nwn_dds(encoded)
    if row["format"] == "DXT1" and alpha_kind(decoded) != "opaque":
        raise ValueError(f"DDS introduced transparency: {row['path']}")
    result = {"path": row["path"], "source_sha256": sha256(raw),
              "source_bytes": len(raw), "width": source.width, "height": source.height,
              "alpha": alpha_kind(source), "format": row["format"],
              "output": output_name, "output_sha256": sha256(encoded),
              "output_bytes": len(encoded), "mip_count": len(levels),
              "source_display_rgb_grid": rgb_grid(source), "decoded_display_rgb_grid": rgb_grid(decoded),
              "decoded_rgba_sha256": sha256(decoded.tobytes()),
              "metrics": image_metrics(source, decoded)}
    # Recheck the real parent immediately before the exclusive write.
    destination = staging_path(output_root, output_name)
    with destination.open("xb") as stream:
        stream.write(encoded)
    return result


def stage(manifest: Path, source_root: Path, output_root: Path, report: Path,
          workers: int = 8, magick: str = "magick") -> dict:
    raw_manifest = manifest.read_bytes()
    rows = validate_candidates(json.loads(raw_manifest))
    source_root, output_root, report = source_root.resolve(), output_root.resolve(), report.resolve()
    if not 1 <= workers <= 32:
        raise ValueError("Workers must be between 1 and 32")
    if output_root.is_relative_to(source_root) or source_root.is_relative_to(output_root):
        raise ValueError("Source and staging roots must not overlap")
    if report.is_relative_to(source_root):
        raise ValueError("Report must be outside the source tree")
    if output_root.exists() or report.exists():
        raise ValueError("Staging root and report must not already exist")
    # Resolve every source before creating the staging directory. Output path
    # syntax was checked by validate_candidates; real output parents are
    # created and resolved serially below, before workers can race with mkdir.
    # Byte hashes are checked again immediately before each conversion.
    for row in rows:
        verify_source(source_root, row)
        source_path = under_root(source_root, row["path"], ".tga")
        if source_path.with_suffix(".dds").exists():
            raise ValueError(f"Existing sibling DDS needs separate review: {row['path']}")
    version = subprocess.run([magick, "-version"], check=True, capture_output=True,
                             text=True, timeout=30).stdout.splitlines()[0]
    output_root.mkdir(parents=True, exist_ok=False)
    output_root = output_root.resolve(strict=True)
    prepare_staging_directories(output_root, rows)
    converted = []
    with ThreadPoolExecutor(max_workers=workers) as pool:
        futures = [pool.submit(convert_one, source_root, output_root, row, magick) for row in rows]
        try:
            for future in as_completed(futures):
                converted.append(future.result())
                if len(converted) % 100 == 0 or len(converted) == len(rows):
                    print(f"Staged {len(converted)}/{len(rows)} textures", flush=True)
        except BaseException:
            for future in futures:
                future.cancel()
            raise
    converted.sort(key=lambda row: row["path"])
    result = {"schema_version": SCHEMA_VERSION, "source_manifest_sha256": sha256(raw_manifest),
              "encoder": version, "encoder_settings": {"dds:cluster-fit": True},
              "orientation": "NWN bottom-up standard DDS",
              "mipmaps": "complete chain through 1x1", "entries": converted,
              "totals": {"count": len(converted),
                         "source_bytes": sum(row["source_bytes"] for row in converted),
                         "output_bytes": sum(row["output_bytes"] for row in converted)}}
    report.parent.mkdir(parents=True, exist_ok=True)
    with report.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    return result


def validate_report(manifest: Path, asset_root: Path) -> dict:
    report = json.loads(manifest.read_bytes())
    if not isinstance(report, dict) or report.get("schema_version") != SCHEMA_VERSION:
        raise ValueError("Unsupported conversion report")
    rows = validate_candidates(report.get("entries"))
    root = asset_root.resolve()
    for row in rows:
        required = {"output", "output_sha256", "output_bytes", "source_bytes", "mip_count",
                    "source_display_rgb_grid", "decoded_display_rgb_grid", "decoded_rgba_sha256", "metrics"}
        if not required <= row.keys():
            raise ValueError(f"Incomplete conversion report: {row['path']}")
        expected_output = PurePosixPath(row["path"]).with_suffix(".dds").as_posix()
        if row["output"] != expected_output:
            raise ValueError(f"Output does not match source resource: {row['path']}")
        check_digest(row["output_sha256"], "output")
        check_digest(row["decoded_rgba_sha256"], "decoded pixels")
        for key in ("source_display_rgb_grid", "decoded_display_rgb_grid"):
            if not isinstance(row[key], str) or re.fullmatch(r"[0-9a-f]{384}", row[key]) is None:
                raise ValueError(f"Invalid RGB grid: {row['path']}")
        if under_root(root, row["path"], ".tga").exists():
            raise ValueError(f"Retained TGA would override converted DDS: {row['path']}")
        data = under_root(root, row["output"], ".dds").read_bytes()
        if sha256(data) != row["output_sha256"] or len(data) != row["output_bytes"]:
            raise ValueError(f"DDS changed: {row['output']}")
        validate_dds(data, row["width"], row["height"], row["format"])
        if row["mip_count"] != len(mip_dimensions(row["width"], row["height"])):
            raise ValueError(f"Report mip count changed: {row['output']}")
        decoded = decode_nwn_dds(data)
        if rgb_grid(decoded) != row["decoded_display_rgb_grid"] or sha256(decoded.tobytes()) != row["decoded_rgba_sha256"]:
            raise ValueError(f"Decoded DDS display changed: {row['output']}")
        if row["format"] == "DXT1" and alpha_kind(decoded) != "opaque":
            raise ValueError(f"Opaque DDS contains transparency: {row['output']}")
    totals = {"count": len(rows), "source_bytes": sum(row["source_bytes"] for row in rows),
              "output_bytes": sum(row["output_bytes"] for row in rows)}
    if report.get("totals") != totals:
        raise ValueError("Conversion report totals do not match its entries")
    return totals


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)
    convert = commands.add_parser("stage", help="Convert only explicitly reviewed, hash-pinned inputs")
    convert.add_argument("--manifest", type=Path, required=True)
    convert.add_argument("--source-root", type=Path, required=True)
    convert.add_argument("--output-root", type=Path, required=True)
    convert.add_argument("--report", type=Path, required=True)
    convert.add_argument("--workers", type=int, default=8)
    convert.add_argument("--magick", default="magick")
    check = commands.add_parser("validate", help="Validate retained DDS assets without original TGAs")
    check.add_argument("--manifest", type=Path, required=True)
    check.add_argument("--asset-root", type=Path, required=True)
    args = parser.parse_args()
    if args.command == "stage":
        result = stage(args.manifest, args.source_root, args.output_root, args.report, args.workers, args.magick)
        print(json.dumps(result["totals"]))
    else:
        print(json.dumps(validate_report(args.manifest, args.asset_root)))


if __name__ == "__main__":
    main()
