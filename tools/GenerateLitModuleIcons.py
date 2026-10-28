"""Generate the lit ("module is on") variant of ship module action icons.

A lit icon is the same 32x32 art with an "active" border: the semantic frame is pushed toward white, a
semantic-colored glow bleeds into the art along the inside of the frame, and the art is lifted slightly.
The semantic frame hue is kept, so the module still reads as its role. The output name swaps the "iit_sm"
prefix for "iit_sl" (see ShipToggleRules.LitTextureName). Output is opaque, 32x32, bottom-left origin TGA.

Usage (from the repository root):
    python tools/GenerateLitModuleIcons.py iit_smcmblaser iit_smshldrepair ...
    python tools/GenerateLitModuleIcons.py --check      # fail if a lit icon is out of date

Then generate each lit icon's six recharge frames:
    powershell -File tools/GenerateCooldownIcons.ps1 -IconResRefs iit_slcmblaser,iit_slshldrepair -Force
"""
import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ICON_DIR = Path(__file__).resolve().parent.parent / "SWLOR_Haks" / "sw_ability"
NORMAL_PREFIX = "iit_sm"
LIT_PREFIX = "iit_sl"

# Frame rings, counted in pixels from the outer edge, as (white mix, semantic mix) of the target color:
# a white-hot rim, the solid semantic ring, a filled-in ring where the normal frame has a dark gap, and a pale inner rail.
RING_TARGETS = ((0.75, 1.0), (0.0, 1.0), (0.0, 1.0), (0.45, 1.0))
# Glow strength on the first rows inside the frame (additive semantic color).
GLOW = (0.50, 0.26, 0.10)
FRAME_RINGS = len(RING_TARGETS)


def lit_name(normal):
    if not normal.lower().startswith(NORMAL_PREFIX):
        raise ValueError(f"{normal} does not start with {NORMAL_PREFIX}")
    return LIT_PREFIX + normal[len(NORMAL_PREFIX):]


def make_lit(normal_path):
    image = Image.open(normal_path).convert("RGB")
    if image.size != (32, 32):
        raise ValueError(f"{normal_path} is not 32x32")
    pixels = np.asarray(image, dtype=np.float32)
    size = pixels.shape[0]
    # The semantic color sits on the frame's second ring, mid-edge.
    semantic = pixels[1, size // 2].copy()

    result = pixels.copy()
    ys, xs = np.mgrid[0:size, 0:size]
    ring = np.minimum.reduce([xs, ys, size - 1 - xs, size - 1 - ys])

    # Art: a light lift so the center reads as energized.
    inner = ring >= FRAME_RINGS
    result[inner] = np.clip(result[inner] * 1.12 + 6, 0, 255)

    # Glow bleeding inward from the frame.
    for step, strength in enumerate(GLOW):
        mask = ring == FRAME_RINGS + step
        result[mask] = np.clip(result[mask] + semantic * strength, 0, 255)

    # Frame: replace the rings with their lit colors, leaving the near-black rounded-corner pixels dark.
    white = np.array([255.0, 255.0, 255.0], dtype=np.float32)
    for index, (whiten, _) in enumerate(RING_TARGETS):
        mask = ring == index
        target = semantic + (white - semantic) * whiten
        weight = np.clip((pixels[mask].max(axis=1) - 30.0) / 60.0, 0.0, 1.0)[:, None]
        # Dark gap pixels of the normal frame (ring 2) count as frame too.
        if index == 2:
            weight = np.clip((pixels[mask].max(axis=1) - 12.0) / 20.0, 0.0, 1.0)[:, None]
        result[mask] = pixels[mask] + (target - pixels[mask]) * weight
    return Image.fromarray(np.clip(result + 0.5, 0, 255).astype(np.uint8), "RGB")


def save_tga(image, path):
    # Pillow writes uncompressed 32-bit TGA with a bottom-left origin by default (descriptor 8).
    image.convert("RGBA").save(path, "TGA", rle=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("icons", nargs="*", help="normal icon resrefs, such as iit_smcmblaser")
    parser.add_argument("--check", action="store_true", help="verify existing lit icons match the generator")
    parser.add_argument("--dir", default=str(ICON_DIR))
    args = parser.parse_args()
    directory = Path(args.dir)
    if not args.icons:
        parser.error("name at least one icon")

    failures = 0
    for icon in args.icons:
        source = directory / f"{icon}.tga"
        target = directory / f"{lit_name(icon)}.tga"
        if not source.exists():
            print(f"missing source {source}", file=sys.stderr)
            failures += 1
            continue
        lit = make_lit(source)
        if args.check:
            if not target.exists() or Image.open(target).convert("RGB").tobytes() != lit.tobytes():
                print(f"out of date: {target.name}", file=sys.stderr)
                failures += 1
            continue
        save_tga(lit, target)
        print(f"wrote {target.name}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
