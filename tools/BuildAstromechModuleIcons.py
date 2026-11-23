"""Write the inventory icons of the astromech modules that are not plating.

  python -B tools/BuildAstromechModuleIcons.py [--haks SWLOR_Haks]

An astromech module keeps the base item of the humanoid piece it copies, and a helmet, an amulet, a belt, a ring, a
glove, a bracer, a boot and a cloak pick their inventory icon by model number. A copied blueprint therefore showed a
hood, a necklace or a boot. Each module kind has ICON_VARIANTS icons of its own, under model numbers no humanoid piece
uses (GenerateAstromechModules.MODULE_ICONS, which also puts the numbers on the blueprints):

    Sensor Package, Manipulator Servo, Drive Unit, Power Cell, Logic Module, Expansion Socket, Deflector Emitter

The pictures are `SWLOR_Haks/sw_item_source/astromech/<picture>_<1..4>.png`, with the prompts they were generated
from beside them. This tool fits each one into its inventory slot. A part drawn on a slant is turned level for a slot
wider than it is high.

- Most kinds are written the way IconStandards.md asks for inventory art: a single-level DXT5 DDS the size of the
  slot, and the 8x8 TGA the client needs to accept the name. Boots stack three part icons: the picture is the bottom
  part and the other two are empty.
- A helmet and a cloak draw their icon through the dye palette and show nothing, or a stock cape, for any other kind
  of texture. Their icons are palette textures (PLT) of two layers: the metal of the part takes the item's first
  metal dye and its glow the first cloth dye. The generators give each module the dyes its picture was drawn in
  (ICON_METAL_DYE, ICON_GLOW_DYES), and this tool picks each texel's shade on those dye rows, so the icon shows the
  picture. The dyes must be matte: a dye row whose palette alpha is below one is reflective, and the inventory draws
  it as brown noise, so the tool refuses one. A cloak's number is a cloakmodel.2da row, so the tool also writes those rows, with no model, as the
  non-appearing cloak has.

No humanoid icon is replaced. The tool stops at a name that already holds humanoid art, a helmet number that has a
model, or a cloak row in use, and tools/tests/test_build_astromech_module_icons.py fails if a humanoid blueprint
carries one of the numbers.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / 'tools'))

import BuildAstromechIcons as plating  # noqa: E402
import GenerateAstromechModules as modules  # noqa: E402

SOURCES = 'sw_item_source/astromech'
CLOAK_TABLE = 'sw_2da/cloakmodel.2da'
CLOAK_TEMPLATE_ROW = 99                  # the non-appearing cloak: a row with an icon and no model
CLOAK_BASE_ITEM = 80
MARGIN = 2                               # clear texels between the picture and the slot edge
# base item -> (hak folder, icon names for a model number, slot in cells). A cell is 32 texels.
ICONS = {
    17: ('sw_pt_helm', ('ihelm_{:03d}',), (2, 2)),
    36: ('sw_item', ('iit_glove_{:03d}',), (2, 1)),
    78: ('sw_item', ('iit_bracer_{:03d}',), (2, 1)),
    26: ('sw_item', ('iit_boots_b_{:03d}', 'iit_boots_m_{:03d}', 'iit_boots_t_{:03d}'), (2, 2)),
    21: ('sw_item', ('iit_belt_{:03d}',), (2, 1)),
    19: ('sw_item', ('iit_neck_{:03d}',), (1, 2)),
    52: ('sw_item', ('iit_ring_{:03d}',), (1, 1)),
    80: ('sw_weapon', ('icloak_m_{:03d}',), (2, 3)),
}
# Palette icons. The texture is taller than the slot for a cloak, as the stock cloak icons are: the slot shows its top.
PALETTE = 'sw_item/plt_palette.tga'      # every dye row, 256 shades wide
PALETTE_SIZES = {17: (64, 64), 80: (64, 128)}
METAL_LAYER, GLOW_LAYER = 2, 4           # first metal dye, first cloth dye
LAYER_ROWS = {METAL_LAYER: 352, GLOW_LAYER: 704}   # where each layer's dye rows start in the palette
GLOW_SATURATION, GLOW_BRIGHTNESS = 0.38, 0.30      # a texel this colorful and bright is glow, not metal
SEE_THROUGH_SHADE = 255                  # the last shade of every dye row has no alpha: a texel never takes it
HELMET_MODEL = 'helm_{:03d}.mdl'


def leveled(image: Image.Image) -> Image.Image:
    """The picture turned so its long axis lies level. The parts were drawn lying on a slant, which wastes most of a
    slot that is two cells wide and one high."""
    rows, cols = np.nonzero(np.asarray(image)[..., 3] > 16)
    points = np.stack([cols - cols.mean(), rows.mean() - rows])          # x right, y up
    _, vectors = np.linalg.eigh(np.cov(points))
    x, y = vectors[:, -1]                                                # the long axis
    angle = np.degrees(np.arctan2(y, x))
    angle = (angle + 90) % 180 - 90                                      # an axis has no direction: -90..90
    return image.rotate(-angle, resample=Image.BICUBIC, expand=True)


def icon_art(picture: Image.Image, cells: tuple) -> np.ndarray:
    """(height, width, 4) RGBA 0-1, top-down: the picture as large as the slot allows, centered."""
    width, height = cells[0] * 32, cells[1] * 32
    image = picture.convert('RGBA')
    if width > height:
        image = leveled(image)
    image = image.crop(plating.content_box(np.asarray(image)[..., 3] > 16))
    size, corner = plating.fit(image.size, (MARGIN, MARGIN, width - MARGIN, height - MARGIN))
    canvas = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    # Premultiplied, so the transparent surround adds no dark fringe when the picture shrinks.
    canvas.alpha_composite(image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA'), corner)
    return np.asarray(canvas).astype(np.float32) / 255.0


def icon_files(base_item: int, number: int, art: np.ndarray) -> dict:
    """Icon name -> art for one model number: the picture, and an empty layer for each further part."""
    names = ICONS[base_item][1]
    return {name.format(number): art if index == 0 else np.zeros_like(art) for index, name in enumerate(names)}


def dye_rows(palette: Path) -> np.ndarray:
    """(row, shade, 4) RGBA 0-1: every dye row of the palette, first row first (the TGA is stored bottom-up).
    Alpha below one marks a reflective dye."""
    return (np.asarray(Image.open(palette).convert('RGBA')).astype(np.float32) / 255.0)[::-1]


def matte_ramp(rows: np.ndarray, layer: int, dye: int) -> np.ndarray:
    """(shade, 3) RGB of one dye of a layer, without its last shade, which every dye keeps see-through. A
    reflective dye is refused: the inventory draws it as brown noise."""
    ramp = rows[LAYER_ROWS[layer] + dye, :SEE_THROUGH_SHADE]
    if ramp[:, 3].min() < 0.99:
        raise SystemExit(f'dye {dye} of layer {layer} is reflective; pick a matte dye for the module icons')
    return ramp[:, :3]


def palette_icon(art: np.ndarray, size: tuple, rows: np.ndarray, metal_dye: int, glow_dye: int) -> np.ndarray:
    """(height, width, 2) palette texels, top-down: the picture as shades of its two dyes. Each visible texel is glow
    or metal by its color, and takes the shade of that dye row closest to it."""
    texels = np.empty((size[1], size[0], 2), np.uint8)
    texels[...] = plating.PLT_BACKGROUND
    color = art[..., :3]
    visible = art[..., 3] > 0.5
    high, low = color.max(-1), color.min(-1)
    glow = visible & ((high - low) > GLOW_SATURATION * high) & (high > GLOW_BRIGHTNESS)
    area = texels[:art.shape[0], :art.shape[1]]
    for layer, dye, mask in ((METAL_LAYER, metal_dye, visible & ~glow), (GLOW_LAYER, glow_dye, glow)):
        ramp = matte_ramp(rows, layer, dye)
        distance = ((color[mask][:, None, :] - ramp[None, :, :]) ** 2).sum(-1)
        area[mask, 0] = distance.argmin(1).astype(np.uint8)
        area[mask, 1] = layer
    return texels


def cloak_rows(table: str, numbers) -> str:
    """cloakmodel.2da with a row for each cloak icon number: the template row's values under that number's icon."""
    newline = '\r\n' if '\r\n' in table else '\n'
    lines = table.split(newline)
    header = lines[2]
    icon_at = header.index(' ICON ') + 1
    rows = {line.split()[0]: index for index, line in enumerate(lines) if index > 2 and line.split()}
    template = lines[rows[str(CLOAK_TEMPLATE_ROW)]]
    label = template[template.index('"'):template.rindex('"') + 1]
    for position, number in enumerate(numbers, 1):
        index = rows[str(number)]
        current = lines[index].split()
        if current[1] != '****' and not current[1].startswith('"Deflector'):
            raise SystemExit(f'cloakmodel.2da row {number} is in use: {lines[index].strip()}')
        line = f'{number:<{template.index(label)}}' + template[template.index(label):]
        line = line.replace(label, f'"Deflector Emitter {position}"'.ljust(len(label)))
        lines[index] = line[:icon_at] + f'{number:<5}' + line[icon_at + 5:]
    return newline.join(lines)


def humanoid_icon(path: Path) -> bool:
    """Whether an icon of this name exists that this tool did not write: a palette texture, or a TGA with no DDS."""
    return path.with_suffix('.plt').exists() or (path.with_suffix('.tga').exists() and not path.exists())


def write(haks: Path) -> int:
    icons = plating.load('FixWeaponInventoryIcons')
    codec = icons._codec()
    _, template = codec.read_dds(haks / 'sw_weapon' / plating.TOOL_TEMPLATE)
    rows = dye_rows(haks / PALETTE)
    written = 0
    for base_item, (picture, _) in modules.MODULE_ICONS.items():
        folder, names, cells = ICONS[base_item]
        info = plating.slot_sized(template, (cells[0] * 32, cells[1] * 32))
        for variant, number in enumerate(modules.module_icon_numbers(base_item), 1):
            art = icon_art(Image.open(haks / SOURCES / f'{picture}_{variant}.png'), cells)
            if base_item in modules.PALETTE_ICON_ITEMS:
                if (haks / folder / HELMET_MODEL.format(number)).exists():
                    raise SystemExit(f'helmet {number} has a model; pick another number in MODULE_ICONS')
                texels = palette_icon(art, PALETTE_SIZES[base_item], rows, modules.ICON_METAL_DYE,
                                      modules.ICON_GLOW_DYES[variant - 1])
                (haks / folder / f'{names[0].format(number)}.plt').write_bytes(plating.plt_bytes(texels))
                written += 1
                continue
            for name, layer in icon_files(base_item, number, art).items():
                path = haks / folder / f'{name}.dds'
                if humanoid_icon(path):
                    raise SystemExit(f'{path.stem} is the icon of another item; pick another number in MODULE_ICONS')
                icons.write_icon(codec, path, layer, info)
                path.with_suffix('.tga').write_bytes(icons.tga_bytes(icons.thumbnail(layer)))
                written += 1
    table = haks / CLOAK_TABLE
    text = table.read_bytes().decode('latin-1')
    table.write_bytes(cloak_rows(text, modules.module_icon_numbers(CLOAK_BASE_ITEM)).encode('latin-1'))
    return written


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--haks', type=Path, default=REPO / 'SWLOR_Haks')
    args = parser.parse_args(argv)
    print(f'{write(args.haks)} module icons written under {args.haks}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
