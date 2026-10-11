"""Convert key-colored helmet artwork into tintable 64x64 NWN inventory PLTs.

Each source PNG paints every dye region in one key hue (see KEY_LAYERS) over a
flat #808080 background, shading only by brightness. The hue selects the PLT
layer and the brightness becomes the PLT shade, so the icon takes the item's
dye colors in game. Sources and the generation prompt live in
SWLOR_Haks/sw_item_source/helmets/.
"""
import argparse
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# Key hue in degrees -> PLT layer (0 skin, 1 hair, 2 metal1, 3 metal2,
# 4 cloth1, 5 cloth2, 6 leather1, 7 leather2).
KEY_LAYERS = [(0, 2), (30, 0), (60, 3), (120, 6), (180, 5), (225, 4), (270, 1), (315, 7)]
ICON_SIZE = 64
SUBJECT_SIZE = 58
TARGET_MEDIAN_SHADE = 85
TARGET_PEAK_SHADE = 205


def foreground_mask(rgb, value, saturation):
    """Everything not connected to the border through flat background gray."""
    grayish = (saturation < 0.18) & (np.abs(value - 0.5) < 0.15)
    padded = np.pad(grayish, 1, constant_values=True).astype(np.uint8) * 255
    # fromarray images share the read-only array buffer; floodfill needs a writable copy.
    fill = Image.fromarray(padded).copy()
    ImageDraw.floodfill(fill, (0, 0), 128)
    background = np.asarray(fill)[1:-1, 1:-1] == 128
    mask = Image.fromarray((~background).astype(np.uint8) * 255)
    mask = mask.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))
    return np.asarray(mask) > 127


def hue_degrees(rgb, value, minimum):
    red, green, blue = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    delta = np.maximum(value - minimum, 1e-6)
    hue = np.where(value == red, ((green - blue) / delta) % 6,
                   np.where(value == green, (blue - red) / delta + 2, (red - green) / delta + 4))
    return hue * 60


def fill_empty_cells(layers, has_votes, opaque):
    """Give opaque cells without a confident hue the layer of a neighboring cell."""
    layers, known = layers.copy(), has_votes.copy()
    for _ in range(ICON_SIZE):
        missing = opaque & ~known
        if not missing.any():
            break
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            shifted_known = np.roll(known, (dy, dx), (0, 1))
            take = missing & shifted_known & ~known
            layers[take] = np.roll(layers, (dy, dx), (0, 1))[take]
            known |= take
    return layers


def convert(source, destination):
    rgb = np.asarray(Image.open(source).convert("RGB")).astype(float) / 255
    value, minimum = rgb.max(-1), rgb.min(-1)
    saturation = np.where(value > 0, (value - minimum) / np.maximum(value, 1e-6), 0)
    foreground = foreground_mask(rgb, value, saturation)

    key_hues = np.array([hue for hue, _ in KEY_LAYERS])
    key_layers = np.array([layer for _, layer in KEY_LAYERS])
    distance = np.abs((hue_degrees(rgb, value, minimum)[..., None] - key_hues + 180) % 360 - 180)
    layer = key_layers[distance.argmin(-1)]
    confident = foreground & (saturation > 0.35) & (value > 0.12)

    rows, columns = np.nonzero(foreground)
    top, bottom, left, right = rows.min(), rows.max() + 1, columns.min(), columns.max() + 1
    scale = SUBJECT_SIZE / max(bottom - top, right - left)
    offset_x = (ICON_SIZE - (right - left) * scale) / 2 - left * scale
    offset_y = (ICON_SIZE - (bottom - top) * scale) / 2 - top * scale
    cell_y = np.floor(np.arange(rgb.shape[0]) * scale + offset_y).astype(int)
    cell_x = np.floor(np.arange(rgb.shape[1]) * scale + offset_x).astype(int)
    grid_y, grid_x = np.meshgrid(cell_y, cell_x, indexing="ij")
    inside = (grid_y >= 0) & (grid_y < ICON_SIZE) & (grid_x >= 0) & (grid_x < ICON_SIZE)
    cell = (grid_y * ICON_SIZE + grid_x)[inside]
    cells = ICON_SIZE * ICON_SIZE

    def total(weights):
        return np.bincount(cell, weights[inside].astype(float), cells).reshape(ICON_SIZE, ICON_SIZE)

    pixel_count = total(np.ones_like(value))
    coverage = total(foreground)
    shade_sum = total(value * foreground)
    votes = np.stack([total(confident & (layer == index)) for index in range(10)], -1)

    opaque = coverage / np.maximum(pixel_count, 1) >= 0.5
    layers = fill_empty_cells(votes.argmax(-1), votes.sum(-1) > 0, opaque)
    brightness = shade_sum / np.maximum(coverage, 1e-6)
    low, high = np.percentile(brightness[opaque], [2, 98])
    normalized = np.clip((brightness - low) / max(high - low, 1e-6), 1e-3, 1)
    gamma = np.log(TARGET_MEDIAN_SHADE / TARGET_PEAK_SHADE) / np.log(np.median(normalized[opaque]))
    shade = np.clip(normalized ** gamma * TARGET_PEAK_SHADE, 0, 254)

    pixels = np.zeros((ICON_SIZE, ICON_SIZE, 2), np.uint8)
    pixels[..., 0] = np.where(opaque, np.rint(shade), 255)
    pixels[..., 1] = np.where(opaque, layers, 0)
    header = b"PLT V1  " + struct.pack("<IIII", 8, 0, ICON_SIZE, ICON_SIZE)
    destination.write_bytes(header + pixels[::-1].tobytes())


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--source", type=Path, default=Path("SWLOR_Haks/sw_item_source/helmets"))
    parser.add_argument("--output", type=Path, default=Path("SWLOR_Haks/sw_pt_helm"))
    parser.add_argument("icons", nargs="*", help="Icon resrefs such as ihelm_302; default is every source")
    arguments = parser.parse_args()
    names = arguments.icons or sorted(path.stem for path in arguments.source.glob("ihelm_*.png"))
    for name in names:
        convert(arguments.source / f"{name}.png", arguments.output / f"{name}.plt")
        print(f"{name}.plt")


if __name__ == "__main__":
    main()
