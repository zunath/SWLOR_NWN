#!/usr/bin/env python3
"""Build astromech chassis portraits from game client captures.

The portrait of a chassis is the game's own render of it, so it always matches what players see. Each portrait needs
two captures taken from the same camera in a still scene: one with the chassis and one with it hidden (the plate).
The difference between them is the chassis; it is cut out, set on a drawn backdrop and written as the four portrait
sizes the haks carry (l, m, s, t) in the stored layout the game expects.

Manifest (JSON):
  {"schema_version": 1,
   "crop": [left, top, right, bottom],          # region of the captures to search, clear of interface panels
   "portraits": [{"name": "astro01", "visible": "astro01_visible.png", "plate": "astro01_plate.png"}, ...]}

Paths are relative to the manifest. Output files are po_<name>_<size>.dds and .txi in --output, which must be empty
or new. Captures and the manifest stay local; commit only the generated files.

Usage:
  python -B tools/BuildAstromechPortraits.py --manifest <portraits.json> --output <new directory>
"""
import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SCHEMA_VERSION = 1
# size letter -> (stored width, stored height, visible height)
SIZES = {'l': (128, 256, 200), 'm': (64, 128, 100), 's': (32, 64, 50), 't': (16, 32, 25)}
SUPERSAMPLE = 4
DIFFERENCE_THRESHOLD = 26          # summed channel difference that marks a changed pixel
MINIMUM_SUBJECT_FRACTION = 0.03    # of the crop; below this the pair did not capture a chassis
SHADOW_RATIO = (0.35, 0.97)        # visible / plate per channel for a cast shadow
SHADOW_SPREAD = 0.14               # how far the three channel ratios may differ in a shadow
BACKDROP_TOP = (20, 27, 40)
BACKDROP_BOTTOM = (62, 78, 100)
GLOW = (120, 150, 190)
SUBJECT_WIDTH_FRACTION = 1.30     # a chassis is wider than a portrait, so its sides may run off the frame
SUBJECT_HEIGHT_FRACTION = 0.80


def load_manifest(path):
    path = Path(path)
    data = json.loads(path.read_text(encoding='utf-8'))
    if data.get('schema_version') != SCHEMA_VERSION:
        raise ValueError(f'schema_version must be {SCHEMA_VERSION}')
    crop = data.get('crop')
    if (not isinstance(crop, list) or len(crop) != 4 or not all(isinstance(v, int) for v in crop)
            or crop[0] >= crop[2] or crop[1] >= crop[3] or min(crop) < 0):
        raise ValueError('crop must be [left, top, right, bottom] in pixels')
    portraits = data.get('portraits')
    if not isinstance(portraits, list) or not portraits:
        raise ValueError('portraits must be a non-empty list')
    names = set()
    for entry in portraits:
        name = entry.get('name', '')
        if not name or not name.isalnum() or not name.islower() and not name.isdigit():
            raise ValueError(f'portrait name {name!r} must be lowercase letters and digits')
        if len(f'po_{name}_l') > 16:
            raise ValueError(f'portrait name {name!r} makes a resource name longer than 16 characters')
        if name in names:
            raise ValueError(f'portrait name {name!r} is listed twice')
        names.add(name)
        for key in ('visible', 'plate'):
            if not (path.parent / entry.get(key, '')).is_file():
                raise ValueError(f'{name}: {key} capture not found')
    return data, path.parent


def subject_mask(visible, plate):
    """Boolean mask of the pixels that differ between the two captures, reduced to the one solid subject."""
    difference = np.abs(visible.astype(np.int16) - plate.astype(np.int16)).sum(axis=2)
    # Shadows also differ between the two captures (the chassis casts one, and hiding it can move
    # others), but only as an even darkening or lightening of what was already there: every channel
    # changes by about the same ratio. Leave those pixels out.
    ratio = (visible.astype(np.float32) + 1.0) / (plate.astype(np.float32) + 1.0)
    low, high = ratio.min(axis=2), ratio.max(axis=2)
    even = high - low < SHADOW_SPREAD * np.maximum(1.0, low)
    darker = (high < SHADOW_RATIO[1]) & (low > SHADOW_RATIO[0])
    lighter = (low > 1.0 / SHADOW_RATIO[1]) & (high < 1.0 / SHADOW_RATIO[0])
    shadow = even & (darker | lighter)
    raw = Image.fromarray((((difference > DIFFERENCE_THRESHOLD) & ~shadow) * 255).astype(np.uint8))
    # Close pinholes, then drop specks, before picking the subject.
    raw = raw.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(5))
    raw = raw.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    mask = np.asarray(raw) > 127
    if not mask.any():
        return mask
    return fill_holes(largest_component(mask))


def largest_component(mask):
    """The thickest connected region of a boolean mask: the one that survives the most erosion."""
    # fromarray shares the read-only array; a copy is needed before the fill can write to it.
    image = Image.fromarray((mask * 255).astype(np.uint8)).copy()
    core = image
    while True:
        eroded = core.filter(ImageFilter.MinFilter(9))
        if not np.asarray(eroded).any():
            break
        core = eroded
    ys, xs = np.nonzero(np.asarray(core))
    ImageDraw.floodfill(image, (int(xs[0]), int(ys[0])), 128)
    return np.asarray(image) == 128


def fill_holes(mask):
    """Add every background pixel that the outside cannot reach."""
    padded = np.zeros((mask.shape[0] + 2, mask.shape[1] + 2), np.uint8)
    padded[1:-1, 1:-1] = mask * 255
    image = Image.fromarray(padded).copy()
    ImageDraw.floodfill(image, (0, 0), 128)
    outside = np.asarray(image)[1:-1, 1:-1] == 128
    return ~outside


def cut_out(visible_path, plate_path, crop):
    visible = np.asarray(Image.open(visible_path).convert('RGB'))
    plate = np.asarray(Image.open(plate_path).convert('RGB'))
    if visible.shape != plate.shape:
        raise ValueError('the two captures differ in size')
    left, top, right, bottom = crop
    if right > visible.shape[1] or bottom > visible.shape[0]:
        raise ValueError('crop lies outside the captures')
    visible = visible[top:bottom, left:right]
    plate = plate[top:bottom, left:right]
    mask = subject_mask(visible, plate)
    if mask.mean() < MINIMUM_SUBJECT_FRACTION:
        raise ValueError('the captures show no subject: the pair is nearly identical')
    ys, xs = np.nonzero(mask)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    alpha = Image.fromarray((mask[y0:y1, x0:x1] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))
    subject = Image.fromarray(visible[y0:y1, x0:x1]).convert('RGBA')
    subject.putalpha(alpha)
    return subject


def backdrop(width, height):
    rows = np.linspace(0.0, 1.0, height)[:, None, None]
    image = np.asarray(BACKDROP_TOP, np.float32) * (1 - rows) + np.asarray(BACKDROP_BOTTOM, np.float32) * rows
    image = np.repeat(image, width, axis=1)
    yy, xx = np.mgrid[0:height, 0:width]
    distance = np.hypot((xx - width / 2) / (width * 0.62), (yy - height * 0.58) / (height * 0.5))
    glow = np.clip(1.0 - distance, 0.0, 1.0)[..., None] ** 2 * 0.45
    image = image * (1 - glow) + np.asarray(GLOW, np.float32) * glow
    return Image.fromarray(np.clip(np.rint(image), 0, 255).astype(np.uint8)).convert('RGBA')


def compose(subject):
    """The upright portrait at the largest visible size, supersampled."""
    width, _, visible = SIZES['l']
    width *= SUPERSAMPLE
    height = visible * SUPERSAMPLE
    canvas = backdrop(width, height)
    scale = min(width * SUBJECT_WIDTH_FRACTION / subject.width, height * SUBJECT_HEIGHT_FRACTION / subject.height)
    size = (max(1, round(subject.width * scale)), max(1, round(subject.height * scale)))
    placed = subject.resize(size, Image.LANCZOS)
    # The capture cuts the chassis off at the bottom edge, so it stands on the bottom of the portrait.
    left = (width - size[0]) // 2
    if left < 0:
        placed = placed.crop((-left, 0, -left + width, size[1]))
        left = 0
    canvas.alpha_composite(placed, (left, height - size[1]))
    return canvas.convert('RGB')


def stored_image(portrait, letter):
    """The portrait at one size in the layout the game stores: full canvas, picture upside down at the bottom."""
    width, stored_height, visible = SIZES[letter]
    picture = portrait.resize((width, visible), Image.LANCZOS)
    canvas = Image.new('RGB', (width, stored_height), (0, 0, 0))
    canvas.paste(picture, (0, 0))
    return canvas.transpose(Image.FLIP_TOP_BOTTOM)


def build(manifest_path, output):
    data, root = load_manifest(manifest_path)
    output = Path(output)
    if output.exists() and any(output.iterdir()):
        raise ValueError('output directory must be empty or new')
    output.mkdir(parents=True, exist_ok=True)
    written = []
    for entry in data['portraits']:
        subject = cut_out(root / entry['visible'], root / entry['plate'], data['crop'])
        portrait = compose(subject)
        for letter in SIZES:
            name = f"po_{entry['name']}_{letter}"
            stored_image(portrait, letter).save(output / f'{name}.dds', pixel_format='DXT1')
            (output / f'{name}.txi').write_text('mipmap 0\n', newline='\n')
            written.append(name)
        portrait.resize((SIZES['l'][0] * 2, SIZES['l'][2] * 2), Image.LANCZOS).save(
            output / f"review_{entry['name']}.png")
    return written


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    parser.add_argument('--manifest', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args(argv)
    try:
        written = build(args.manifest, args.output)
    except ValueError as error:
        print(f'error: {error}', file=sys.stderr)
        return 1
    print(f'wrote {len(written)} portrait files to {args.output}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
