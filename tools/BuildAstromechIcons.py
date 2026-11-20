"""Draw the astromech's inventory icons from its own models.

  python -B tools/BuildAstromechIcons.py --hak-dir <packed haks> --work <scratch> [--haks SWLOR_Haks]

Astromech gear copies humanoid blueprints, so its icons were the humanoid pieces' icons. Two of them were wrong on
the dynamic body, and this tool draws them from the game models with the Appearance Editor's renderer
(tools/appearance_previews, Blender 4.0):

- Chassis Plating. Plating is an armor item without part models, and an armor icon is built from its part numbers,
  so it showed the bare torso of an armor with no parts. Plating now carries torso PLATING_TORSO, a number no body
  has a model for (the chassis still shows through), and this tool draws that number's icon: a chassis body as a
  palette icon, so the icon wears the plating's own dyes (cloth 2 main paint, cloth 1 accents, metal 2 trim). The
  Appearance Editor's picture of that number comes from tools/GenerateAppearancePreviews.py like any part picture.
- Shock prod and blaster mount. A weapon icon is three stacked part icons. The tool's bottom part gets the picture
  of the held tool and the other parts are empty, one set per part number an `am_w_*` blueprint uses. Every icon is
  a 32x64 DDS, the size of the 1x2 slot (the client squeezes a larger canvas into the slot), with the 8x8 TGA the
  client needs to accept it (FixWeaponInventoryIcons.py).

The packed haks must hold the current part and tool models (AstromechDynamicBody.py, then pack). Nothing is rendered
from a file outside them.
"""
from __future__ import annotations

import argparse
import importlib.util
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / 'tools'))

import AstromechDynamicBody as body  # noqa: E402
import GenerateAstromechModules as modules  # noqa: E402

PLATING_TORSO = modules.PLATING_TORSO
PLATING_MODEL = 'pmr0_chest001'          # the body the plating icon is drawn from
PLATING_VIEW = 'a'
PART_ICON_SIZE = (64, 128)               # armor part icons; the inventory shows the top 64x96
PLATING_BOX = (4, 6, 60, 90)             # where the body sits in that area
# Tint layers of the chassis material -> the flat color they are rendered in to read them back.
LAYER_COLORS = {3: (255, 0, 0), 4: (0, 255, 0), 5: (0, 0, 255)}
MAIN_LAYER = 5
PLT_BACKGROUND = (255, 0)                # shade, layer of a see-through palette texel
SHADE_MIDTONE = 122                      # where the middle of the picture's brightness lands in the palette ramp

TOOL_CANVAS = (32, 64)                   # a 1x2 item: the client fits the whole icon into the slot, so the icon is slot sized
TOOL_BOX = (2, 2, 30, 62)
TOOL_TEMPLATE = 'iwdbqs_b_011.dds'       # any single-level DXT5 weapon icon: its header is reused
TOOL_GAMMA = 0.75                        # the tools are dark metal; lift them so they read on the inventory grid
PART_ICON_FOLDER = 'sw_pt_root'


def load(name: str):
    spec = importlib.util.spec_from_file_location(name, REPO / 'tools' / f'{name}.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# ---------------------------------------------------------------- rendering
def render(jobs, hak_dir: Path, work: Path, blender, bake=None) -> Path:
    """Render jobs with the Appearance Editor's renderer; returns the folder of <key>_<view>.png files."""
    from appearance_previews import __main__ as previews
    from appearance_previews.jobs import Job

    saved = previews.build, previews.bake
    previews.build = lambda two_da, exists: [Job(key, model, weapon=weapon) for key, model, weapon in jobs]
    if bake:
        previews.bake = bake
    try:
        arguments = ['--hak-dir', str(hak_dir), '--output', str(work / 'pictures'), '--work', str(work),
                     '--render-size', '512']
        if blender:
            arguments += ['--blender', str(blender)]
        if previews.main(arguments):
            raise SystemExit(f'Rendering failed; see {work}')
    finally:
        previews.build, previews.bake = saved
    return work / 'renders'


def bake_shade(shade, layer, palette, dyes=None):
    """The tint map's shade alone, so the render's brightness is the palette shade under the picture's light."""
    grey = np.clip(shade, 0, 255).astype(np.uint8)
    return np.stack([grey] * 3, -1), np.zeros_like(grey)


def bake_layers(shade, layer, palette, dyes=None):
    """Each tint layer in its own flat color, so the render says which dye every pixel takes."""
    colors = np.full(layer.shape + (3,), LAYER_COLORS[MAIN_LAYER], np.uint8)
    for index, color in LAYER_COLORS.items():
        colors[layer == index] = color
    return colors, np.zeros(layer.shape, np.uint8)


# ---------------------------------------------------------------- plating icon
def content_box(alpha: np.ndarray):
    rows, cols = np.where(alpha.any(1))[0], np.where(alpha.any(0))[0]
    if not len(rows):
        raise SystemExit('The render is empty')
    return int(cols.min()), int(rows.min()), int(cols.max()) + 1, int(rows.max()) + 1


def fit(size, box):
    """Largest size with the picture's proportions inside the box, and where it sits centered."""
    left, top, right, bottom = box
    scale = min((right - left) / size[0], (bottom - top) / size[1])
    width, height = max(1, round(size[0] * scale)), max(1, round(size[1] * scale))
    return (width, height), (left + (right - left - width) // 2, top + (bottom - top - height) // 2)


def plating_icon(shade_png: Path, layer_png: Path) -> np.ndarray:
    """(height, width, 2) palette texels, top-down: shade and tint layer."""
    shade = Image.open(shade_png).convert('RGBA')
    layers = Image.open(layer_png).convert('RGBA')
    box = content_box(np.asarray(shade)[..., 3] > 127)
    shade, layers = shade.crop(box), layers.crop(box)
    size, corner = fit(shade.size, PLATING_BOX)
    small = np.asarray(shade.resize(size, Image.LANCZOS)).astype(np.float32)
    flat = np.asarray(layers.resize(size, Image.BOX)).astype(np.float32)
    visible = small[..., 3] > 127
    light = small[..., :3] @ np.array([0.299, 0.587, 0.114], np.float32)
    # Center the picture's brightness on the palette midtone so a dye shows as the color it is.
    light = np.clip(light * (SHADE_MIDTONE / max(1.0, float(np.median(light[visible])))), 8, 246)
    channel = np.argmax(flat[..., :3], -1)
    by_channel = {int(np.argmax(color)): index for index, color in LAYER_COLORS.items()}
    layer = np.vectorize(by_channel.get)(channel)

    width, height = PART_ICON_SIZE
    texels = np.empty((height, width, 2), np.uint8)
    texels[...] = PLT_BACKGROUND
    area = texels[corner[1]:corner[1] + size[1], corner[0]:corner[0] + size[0]]
    area[visible, 0] = np.rint(light[visible]).astype(np.uint8)
    area[visible, 1] = layer[visible].astype(np.uint8)
    return texels


def plt_bytes(texels: np.ndarray) -> bytes:
    """A PLT: the 24-byte header, then (shade, layer) texels with the rows stored bottom-up."""
    height, width = texels.shape[:2]
    return b'PLT V1  ' + struct.pack('<IIII', 10, 0, width, height) + texels[::-1].tobytes()


# ---------------------------------------------------------------- tool icons
def tool_icon(png: Path) -> np.ndarray:
    """(height, width, 4) RGBA 0-1, top-down: the held tool upright on the slot-sized icon canvas."""
    image = Image.open(png).convert('RGBA')
    # The renderer lays a long part on the rising diagonal; stand it up, working end at the top.
    image = image.rotate(45, resample=Image.BICUBIC, expand=True)
    image = image.crop(content_box(np.asarray(image)[..., 3] > 16))
    size, corner = fit(image.size, TOOL_BOX)
    canvas = Image.new('RGBA', TOOL_CANVAS, (0, 0, 0, 0))
    canvas.alpha_composite(image.resize(size, Image.LANCZOS), corner)
    rgba = np.asarray(canvas).astype(np.float32) / 255.0
    rgba[..., :3] = rgba[..., :3] ** TOOL_GAMMA
    return rgba


def slot_sized(info: dict, size=TOOL_CANVAS) -> dict:
    """The template's DDS header for an icon of the given size (the tool canvas by default)."""
    width, height = size
    header = bytearray(info['header'])
    struct.pack_into('<III', header, 12, height, width, ((width + 3) // 4) * ((height + 3) // 4) * 16)
    return {**info, 'header': bytes(header), 'size': tuple(size)}


def write_tool_icons(folder: Path, pictures: dict) -> int:
    """One icon set per part number the tool blueprints use: the picture on the bottom part, nothing on the others."""
    icons = load('FixWeaponInventoryIcons')
    codec = icons._codec()
    _, info = codec.read_dds(folder / TOOL_TEMPLATE)
    if info['fourcc'] != 'DXT5' or info['mips'] != 1:
        raise SystemExit(f'{TOOL_TEMPLATE} is not a single-level DXT5 icon')
    info = slot_sized(info)
    empty = np.zeros((TOOL_CANVAS[1], TOOL_CANVAS[0], 4), np.float32)
    written = 0
    for base, slots in body.tool_part_numbers().items():
        item_class = body.TOOL_ITEMS[base][0]
        for old in folder.glob(f'i{item_class}_*'):
            old.unlink()
        for slot, numbers in slots.items():
            for number in sorted(numbers):
                art = pictures[item_class] if slot == 'b' else empty
                path = folder / f'i{item_class}_{slot}_{number:03d}.dds'
                icons.write_icon(codec, path, art, info)
                path.with_suffix('.tga').write_bytes(icons.tga_bytes(icons.thumbnail(art)))
                written += 1
    return written


# ---------------------------------------------------------------- driver
def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--hak-dir', type=Path, required=True, help='Packed haks holding the current models')
    parser.add_argument('--work', type=Path, required=True, help='Empty scratch folder')
    parser.add_argument('--haks', type=Path, default=REPO / 'SWLOR_Haks')
    parser.add_argument('--blender')
    args = parser.parse_args(argv)
    if args.work.exists() and any(args.work.iterdir()):
        raise SystemExit(f'{args.work} must be empty')

    plating = [(PLATING_MODEL, PLATING_MODEL, '')]
    shade = render(plating, args.hak_dir, args.work / 'shade', args.blender, bake_shade)
    layers = render(plating, args.hak_dir, args.work / 'layers', args.blender, bake_layers)
    texels = plating_icon(shade / f'{PLATING_MODEL}_{PLATING_VIEW}.png', layers / f'{PLATING_MODEL}_{PLATING_VIEW}.png')
    for gender in 'mf':
        (args.haks / PART_ICON_FOLDER / f'ip{gender}_chest{PLATING_TORSO:03d}.plt').write_bytes(plt_bytes(texels))

    tools = [(f'{item_class}_b', f'{item_class}_b_{min(body.tool_part_numbers()[base]["b"]):03d}', 'weapon')
             for base, (item_class, *_) in body.TOOL_ITEMS.items()]
    renders = render(tools, args.hak_dir, args.work / 'tools', args.blender)
    pictures = {key[:-2]: tool_icon(renders / f'{key}_f.png') for key, _, _ in tools}
    written = write_tool_icons(args.haks / body.TOOL_FOLDER, pictures)
    print(f'Plating icon for torso {PLATING_TORSO} and {written} tool part icons written under {args.haks}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
