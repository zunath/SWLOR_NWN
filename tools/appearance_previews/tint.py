"""Bake SWLOR's tint colours into plain textures Blender can render.

Mirrors sw_shader fs_plt_tinter.shd: a tint map stores a shade byte (R) and a dye layer in
ten bins (G); the colour is plt_palette[layer base + palette id, shade]. Legacy PLT textures
store the same (shade, layer) pairs directly. Palette alpha below one is the legacy metal
environment coverage, exported as a metallic mask.
"""
import io
import struct
from pathlib import Path

import numpy as np
from PIL import Image

LAYER_BASE = (0, 176, 352, 528, 704, 704, 880, 880, 1056, 1056)  # skin hair m1 m2 c1 c2 l1 l2 t1 t2
# Fixed preview dyes (palette ids), chosen so every channel reads distinctly in the pictures:
# off-white Cloth 1 (B1), deep red Cloth 2 (H7), tan Leather 1 (P6), near-black Leather 2 (H11),
# steel Metal 1 (B1) and gold Metal 2 (O6).
PREVIEW_DYES = (2, 3, 1, 94, 1, 103, 95, 167, 0, 0)


def read_plt(path: Path):
    data = path.read_bytes()
    if data[:8] != b"PLT V1  ":
        raise ValueError(f"Not a PLT: {path}")
    width, height = struct.unpack_from("<II", data, 16)
    pixels = np.frombuffer(data, np.uint8, width * height * 2, 24).reshape(height, width, 2)
    # PLT rows are stored bottom-up; return top-down like the other decoders.
    return pixels[::-1, :, 0].astype(np.int32), pixels[::-1, :, 1].astype(np.int32)


def read_tint_map(path: Path):
    """Shade and layer arrays (top-down) from a tm_* DDS/TGA or a legacy PLT."""
    if path.suffix.lower() == ".plt":
        return read_plt(path)
    pixels = np.asarray(Image.open(path).convert("RGB")).astype(np.int32)
    return pixels[..., 0], np.clip((pixels[..., 1] / 255.0 * 10.0).astype(np.int32), 0, 9)


def cloak_tile(atlases, index: int):
    """One 512x512 tint map tile (top-down) from the cloak_tints atlases (4x8 tiles each, GL bottom-up)."""
    atlas = Image.open(atlases[index // 32]).convert("RGB")
    tile = index % 32
    width, height = atlas.size[0] // 4, atlas.size[1] // 8
    left, top = (tile % 4) * width, (7 - tile // 4) * height
    pixels = np.asarray(atlas.crop((left, top, left + width, top + height))).astype(np.int32)
    return pixels[..., 0], np.clip((pixels[..., 1] / 255.0 * 10.0).astype(np.int32), 0, 9)


PALETTE_COLORS = 176  # swatches per layer; mirrors TintMapMaterialRegistry.PaletteColorCount
LUMINANCE = np.array([0.2126, 0.7152, 0.0722], np.float32)


def exact_color_ramp(atlas, layer: int, rgb):
    """The 256 shades (RGBA, 0..1) of an exact RGB colour on a layer, as fs_plt_tinter draws it.

    The colour is shaded with the row of its nearest palette swatch (compared at the shade-128
    midtone, the earlier swatch on a tie): the swatch row plus the colour's difference from the
    swatch, scaled by the row's shading. An all-black swatch lends no shading, so the layer's
    first row shades the difference instead.
    """
    rows = atlas[atlas.shape[0] - 1 - (LAYER_BASE[layer] + np.arange(PALETTE_COLORS))]  # palette TGA is bottom-up
    midtones = rows[:, 128, :3]
    target = np.asarray(rgb, np.float32) / 255.0
    nearest = int(np.argmin(((midtones - target) ** 2).sum(axis=1)))
    swatch, midpoint = rows[nearest], midtones[nearest]
    ramp, ramp_midpoint = swatch[:, :3], midpoint
    if float(midpoint @ LUMINANCE) < 0.5 / 255.0:
        ramp, ramp_midpoint = rows[0][:, :3], midtones[0]
    scale = np.maximum((ramp @ LUMINANCE) / max(float(ramp_midpoint @ LUMINANCE), 1.0 / 255.0), 0.0)
    shaded = np.clip(swatch[:, :3] + (target - midpoint) * scale[:, None], 0.0, 1.0)
    return np.concatenate([shaded, swatch[:, 3:4]], axis=1)


def bake(shade, layer, palette: Path, dyes=PREVIEW_DYES, paints=None):
    """RGB (uint8) and metal mask (uint8) images, top-down.

    paints: {layer index: (red, green, blue)} for layers that wear an exact colour instead of a
    preview dye (an astromech chassis in the paint it comes with)."""
    atlas = np.asarray(Image.open(palette).convert("RGBA")).astype(np.float32) / 255.0
    rows = np.array([LAYER_BASE[i] + dyes[i] for i in range(10)])
    ramps = atlas[atlas.shape[0] - 1 - rows].copy()  # palette TGA is bottom-up
    for index, rgb in (paints or {}).items():
        ramps[int(index)] = exact_color_ramp(atlas, int(index), rgb)
    color = ramps[np.clip(layer, 0, 9), np.clip(shade, 0, 255)]
    environment = 1.0 - color[..., 3]
    rgb = color[..., :3] * (1.0 - environment[..., None]) + 0.666667 * environment[..., None]
    return (rgb * 255).clip(0, 255).astype(np.uint8), (environment * 255).clip(0, 255).astype(np.uint8)


def save_for_blender(rgb, metal, color_path: Path, metal_path: Path):
    # NWN samples textures with bottom-up UVs; Blender does too, from the bottom image row.
    Image.fromarray(rgb).transpose(Image.FLIP_TOP_BOTTOM).save(color_path)
    Image.fromarray(metal).transpose(Image.FLIP_TOP_BOTTOM).save(metal_path)


def bioware_dds(data: bytes) -> bytes:
    """A standard DDS file from a Bioware-format DDS (20-byte header: width, height, 3 = DXT1 or
    4 = DXT5, top mipmap size, alpha mean), which the base game still uses for many textures."""
    width, height, channels, size = struct.unpack_from("<IIII", data, 0)
    if channels not in (3, 4) or not width or not height or size != width * height // (2 if channels == 3 else 1):
        raise ValueError("Not a Bioware DDS")
    if len(data) < 20 + size:
        raise ValueError("Truncated Bioware DDS")
    pixel_format = struct.pack("<II4s5I", 32, 0x4, b"DXT1" if channels == 3 else b"DXT5", 0, 0, 0, 0, 0)
    header = struct.pack("<7I44x", 124, 0x1 | 0x2 | 0x4 | 0x1000 | 0x80000, height, width, size, 0, 1)
    header += pixel_format + struct.pack("<5I", 0x1000, 0, 0, 0, 0)
    return b"DDS " + header + data[20:20 + size]


def open_texture(path: Path) -> Image.Image:
    try:
        return Image.open(path)
    except OSError:
        if path.suffix.lower() != ".dds":
            raise
        image = Image.open(io.BytesIO(bioware_dds(path.read_bytes())))
        image.load()
        return image


def plain_texture(path: Path, color_path: Path, keep_alpha=False):
    """keep_alpha: the material draws this texture's alpha as see-through (cutouts)."""
    image = open_texture(path).convert("RGBA" if keep_alpha else "RGB")
    if path.suffix.lower() == ".dds":
        image = image.transpose(Image.FLIP_TOP_BOTTOM)
    image.save(color_path)
