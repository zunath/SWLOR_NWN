"""Make DDS weapon/shield inventory icons show on the stock client and fit their inventory slot.

Stock NWN:EE 8193.37-17 (CNWCItem::UpdateIcons) accepts an inventory icon resref only
when a TGA of that name exists in some resource layer; otherwise it shows the base
item's generic icon. Once the TGA check passes, the inventory draws the same-named DDS
(verified in the native client with a magenta stub TGA beside a DDS icon). --stubs
writes that registration TGA: an 8x8 thumbnail of the DDS art, bottom-left origin.

The inventory shows only the top-left InvSlotWidth x InvSlotHeight * 32 pixels of an
icon. --reframe scales art that a render placed past that area back into the stock
frame (uniform scale about the canvas center column, rows mapped FROM onto TO),
keeping layer joins aligned across a family.

  python -B tools/FixWeaponInventoryIcons.py audit --haks SWLOR_Haks [--nwn-root DIR]
  python -B tools/FixWeaponInventoryIcons.py stubs --haks SWLOR_Haks --names LIST.txt
  python -B tools/FixWeaponInventoryIcons.py reframe --haks SWLOR_Haks --names LIST.txt --rows FROM0,FROM1,TO0,TO1
"""
from __future__ import annotations

import argparse
import importlib.util
import re
import shlex
import struct
import subprocess
import sys
from pathlib import Path

import numpy as np

STUB_SIZE = 8
ICON = re.compile(r'i(w[a-z]{4}_[bmt]_\d{3}|w[a-z]{4}_\d{3}|ashlw_\d{3}|ashsw_\d{3}|ashto_\d{3})')


def _codec():
    spec = importlib.util.spec_from_file_location('trellis_nwn_material', Path(__file__).with_name('TrellisNwnMaterial.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def tga_bytes(rgba_top_down):
    """Uncompressed 32-bit TGA with bottom-left origin (descriptor 8), as NWN icon paths expect."""
    h, w = rgba_top_down.shape[:2]
    pixels = np.clip(np.rint(rgba_top_down[::-1] * 255), 0, 255).astype(np.uint8)[..., [2, 1, 0, 3]]
    return struct.pack('<BBBHHBHHHHBB', 0, 0, 2, 0, 0, 0, 0, 0, w, h, 32, 8) + pixels.tobytes()


def thumbnail(rgba, size=STUB_SIZE):
    """Premultiplied area downscale, so transparent texels add no color."""
    h, w = rgba.shape[:2]
    premul = np.concatenate([rgba[..., :3] * rgba[..., 3:], rgba[..., 3:]], -1)
    rows = np.linspace(0, h, size + 1).astype(int)
    cols = np.linspace(0, w, size + 1).astype(int)
    out = np.zeros((size, size, 4), np.float32)
    for i in range(size):
        for j in range(size):
            out[i, j] = premul[rows[i]:rows[i + 1], cols[j]:cols[j + 1]].reshape(-1, 4).mean(0)
    alpha = out[..., 3:]
    out[..., :3] = np.where(alpha > 0, out[..., :3] / np.maximum(alpha, 1e-6), 0)
    return out


def read_icon(codec, path):
    """DDS icon as top-down RGBA (NWN stores DDS rows bottom-up)."""
    image, info = codec.read_dds(path)
    if info['fourcc'] != 'DXT5' or info['mips'] != 1:
        raise ValueError(f'{path}: inventory icons must be single-level DXT5')
    return image[::-1], info


def write_icon(codec, path, rgba_top_down, info):
    rgba = bleed_transparent_color(rgba_top_down)
    codec.write_dds(path, rgba[::-1], info)


def bleed_transparent_color(rgba):
    """Give invisible texels nearby visible color so DXT blocks spend no precision on them."""
    rgba = rgba.copy()
    visible = rgba[..., 3] > 1 / 255
    if not visible.any():
        return rgba
    color = rgba[..., :3] * visible[..., None]
    weight = visible.astype(np.float32)
    for _ in range(8):
        hole = ~visible
        if not hole.any():
            break
        padded_c = np.pad(color, ((1, 1), (1, 1), (0, 0)))
        padded_w = np.pad(weight, 1)
        total_c = sum(padded_c[1 + dy:padded_c.shape[0] - 1 + dy, 1 + dx:padded_c.shape[1] - 1 + dx]
                      for dy in (-1, 0, 1) for dx in (-1, 0, 1))
        total_w = sum(padded_w[1 + dy:padded_w.shape[0] - 1 + dy, 1 + dx:padded_w.shape[1] - 1 + dx]
                      for dy in (-1, 0, 1) for dx in (-1, 0, 1))
        grow = hole & (total_w > 0)
        color[grow] = total_c[grow] / total_w[grow, None]
        weight[grow] = 1
        visible = visible | grow
    rgba[..., :3] = np.where(rgba[..., 3:] > 1 / 255, rgba[..., :3], color)
    return rgba


def reframe(rgba, rows_from, rows_to):
    """Uniformly scale art so rows_from maps onto rows_to, about the canvas center column."""
    from PIL import Image
    h, w = rgba.shape[:2]
    scale = (rows_to[1] - rows_to[0]) / (rows_from[1] - rows_from[0])
    offset = rows_to[0] - rows_from[0] * scale
    premul = np.concatenate([rgba[..., :3] * rgba[..., 3:], rgba[..., 3:]], -1)
    sw, sh = max(1, round(w * scale)), max(1, round(h * scale))
    channels = [np.asarray(Image.fromarray(premul[..., c].astype(np.float32), 'F').resize((sw, sh), Image.LANCZOS))
                for c in range(4)]
    small = np.clip(np.stack(channels, -1), 0, 1)
    out = np.zeros_like(rgba)
    top, left = round(offset), round(w / 2 - sw / 2)
    y0, x0 = max(0, top), max(0, left)
    y1, x1 = min(h, top + sh), min(w, left + sw)
    out[y0:y1, x0:x1] = small[y0 - top:y1 - top, x0 - left:x1 - left]
    alpha = out[..., 3:]
    out[..., :3] = np.where(alpha > 0, out[..., :3] / np.maximum(alpha, 1e-6), 0)
    return np.clip(out, 0, 1)


def slot_sizes(haks):
    lines = (Path(haks) / 'sw_2da/baseitems.2da').read_text().splitlines()
    headers = next(shlex.split(line, posix=False) for line in lines if 'InvSlotWidth' in line)
    sizes = {}
    for line in lines:
        values = shlex.split(line, posix=False)
        if len(values) == len(headers) + 1 and values[0].isdigit():
            row = dict(zip(headers, values[1:]))
            if row['InvSlotWidth'].isdigit() and row['InvSlotHeight'].isdigit():
                sizes.setdefault(row['ItemClass'].lower(), (int(row['InvSlotWidth']) * 32, int(row['InvSlotHeight']) * 32))
    return sizes


def base_game_tgas(nwn_root):
    """Lower-case TGA resrefs in the installed game's KEY files."""
    names = set()
    for key in (Path(nwn_root) / 'data').glob('*.key'):
        data = key.read_bytes()
        key_count, key_offset = struct.unpack_from('<II', data, 12)[0], struct.unpack_from('<I', data, 20)[0]
        for i in range(key_count):
            resref, restype, _ = struct.unpack_from('<16sHI', data, key_offset + i * 22)
            if restype == 3:
                names.add(resref.rstrip(b'\0').decode('ascii').lower())
    return names


def audit(haks, nwn_root=None, folder='sw_weapon'):
    """Return (DDS icons with no TGA anywhere, DDS icons with art outside their slot)."""
    codec = _codec()
    haks = Path(haks)
    # The index lists every HAK folder, including ones a sparse worktree leaves unchecked-out.
    listed = subprocess.run(['git', '-C', str(haks), 'ls-files', '--', 'sw_*'], capture_output=True, text=True, check=True).stdout
    tgas = {Path(p).stem.lower() for p in listed.splitlines() if p.lower().endswith('.tga')}
    # Stubs written but not yet added to Git count too.
    tgas |= {p.stem.lower() for p in haks.glob('sw_*/*') if p.suffix.lower() == '.tga'}
    if nwn_root:
        tgas |= base_game_tgas(nwn_root)
    sizes = slot_sizes(haks)
    missing, outside = [], []
    for path in sorted((haks / folder).glob('*.dds')):
        stem = path.stem.lower()
        if not ICON.fullmatch(stem):
            continue
        if stem not in tgas:
            missing.append(stem)
        slot = sizes.get(stem[1:6])
        if slot:
            alpha = read_icon(codec, path)[0][..., 3] > .15
            rows, cols = np.where(alpha.any(1))[0], np.where(alpha.any(0))[0]
            if len(rows) and (rows.max() >= slot[1] or cols.max() >= slot[0]):
                outside.append((stem, slot, int(rows.max()) + 1, int(cols.max()) + 1))
    return missing, outside


def write_stubs(haks, names, folder='sw_weapon'):
    codec = _codec()
    for name in names:
        dds = Path(haks) / folder / f'{name}.dds'
        rgba, _ = read_icon(codec, dds)
        dds.with_suffix('.tga').write_bytes(tga_bytes(thumbnail(rgba)))


def reframe_icons(haks, names, rows_from, rows_to, folder='sw_weapon'):
    codec = _codec()
    for name in names:
        dds = Path(haks) / folder / f'{name}.dds'
        rgba, info = read_icon(codec, dds)
        write_icon(codec, dds, reframe(rgba, rows_from, rows_to), info)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('mode', choices=('audit', 'stubs', 'reframe'))
    parser.add_argument('--haks', type=Path, required=True)
    parser.add_argument('--nwn-root', type=Path)
    parser.add_argument('--names', type=Path)
    parser.add_argument('--rows', help='FROM0,FROM1,TO0,TO1 (reframe)')
    args = parser.parse_args(argv)
    if args.mode == 'audit':
        missing, outside = audit(args.haks, args.nwn_root)
        print(f'DDS icons with no TGA in any layer: {len(missing)}')
        print('\n'.join(missing))
        print(f'DDS icons with art outside the inventory slot: {len(outside)}')
        print('\n'.join(f'{n} slot {w}x{h} art {ah} rows x {aw} cols' for n, (w, h), ah, aw in outside))
        return 0
    names = [line.strip() for line in args.names.read_text().splitlines() if line.strip()]
    if args.mode == 'stubs':
        write_stubs(args.haks, names)
    else:
        f0, f1, t0, t1 = (float(v) for v in args.rows.split(','))
        reframe_icons(args.haks, names, (f0, f1), (t0, t1))
    print(f'{args.mode}: {len(names)} icons')
    return 0


if __name__ == '__main__':
    sys.exit(main())
