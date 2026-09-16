"""Regenerate mip alpha baselines from historical TGA pixels, never repaired DDS.

Requires Git history for the manifest's source_revision, Pillow, and ImageMagick.
Run with the ImageMagick version recorded in the manifest. Changes in the mip
filter or DXT5 encoder require review of both the assets and their baselines.
This only updates the test manifest; it does not rewrite HAK source assets.
"""
import hashlib
import io
import json
from pathlib import Path
import struct
import subprocess
import tempfile

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools/tests/world_texture_alpha.json"


def main():
    baseline = json.loads(MANIFEST.read_text())
    version = subprocess.check_output(["magick", "-version"], text=True).splitlines()[0]
    baseline["mip_generation"] = {
        "encoder": version,
        "contract": "Original TGA pixel alpha (ignore erroneous extension alpha type); "
                    "flip vertically; ImageMagick DDS DXT5 with default full mip chain.",
        "comparison": "SHA-256 of decoded alpha bytes at each mip level, starting at level 0."
    }
    with tempfile.TemporaryDirectory() as scratch:
        png, dds = Path(scratch) / "source.png", Path(scratch) / "reference.dds"
        for expected in baseline["textures"]:
            source = str(Path(expected["texture"]).with_suffix(".tga")).replace("\\", "/")
            data = subprocess.check_output([
                "git", "-C", str(ROOT / "SWLOR_Haks"), "show",
                baseline["source_revision"] + ":" + source])
            if data[-18:] == b"TRUEVISION-XFILE.\0":
                extension = struct.unpack_from("<I", data, len(data) - 26)[0]
                if extension:
                    data = data[:extension]
            with Image.open(io.BytesIO(data)) as original:
                image = original.convert("RGBA").transpose(Image.Transpose.FLIP_TOP_BOTTOM)
            assert image.size == (expected["width"], expected["height"]), source
            image.save(png)
            subprocess.run(["magick", str(png), "-define", "dds:compression=dxt5", str(dds)],
                           check=True)
            encoded = dds.read_bytes()
            assert encoded[84:88] == b"DXT5", source
            levels = max(image.size).bit_length()
            assert struct.unpack_from("<I", encoded, 28)[0] == levels, source
            hashes, cursor = [], 128
            for level in range(levels):
                w, h = max(1, image.width >> level), max(1, image.height >> level)
                size = ((w + 3) // 4) * ((h + 3) // 4) * 16
                header = bytearray(encoded[:128])
                struct.pack_into("<III", header, 12, h, w, size)
                struct.pack_into("<I", header, 28, 1)
                with Image.open(io.BytesIO(header + encoded[cursor:cursor + size])) as mip:
                    hashes.append(hashlib.sha256(mip.getchannel("A").tobytes()).hexdigest())
                cursor += size
            assert cursor == len(encoded), source
            expected["mip_alpha_sha256"] = hashes
    MANIFEST.write_text(json.dumps(baseline, indent=2) + "\n")


if __name__ == "__main__":
    main()
