"""NWN material response for TRELLIS/glTF weapon textures, and the fix for shipped maps.

TRELLIS marks most hard surfaces as metal but gives them dark base colors
(median linear luminance 0.085, where real metals reflect 0.5-0.95) and
satin roughness. NWN's standard shader (inc_material: metallicness =
clamp(3 * specularity - 0.6, 0, 1)) then tints both the diffuse remainder and
the environment reflection by that dark albedo, so the metal renders near black
and matte. The exporter's inverse mapping also gave every non-metal texel
specularity 0.2, a strong plastic-like gloss.

metal_base_color() raises metal albedo toward the physical range while keeping
each finish's hue and relative brightness, metal_roughness() tightens metal
roughness so NWN's environment reflection stays sharp, and metal_specularity()
gives non-metals a low dielectric specularity. Specularity and roughness depend only on the
specularity and roughness maps themselves, so palette finishes keep sharing
them; only the per-material base color depends on the metal mask.

Shipped fix (Python 3 with numpy), from the outer worktree root:
  python -B tools/TrellisNwnMaterial.py --haks SWLOR_Haks --materials LIST.txt [--check]
LIST.txt names the MTR files (one per line) under SWLOR_Haks/sw_weapon. The fix
rewrites their base, specularity and roughness DDS in place with the original
dimensions, formats and mip counts, marks them in a reserved header field, and
refuses maps it has already converted.
"""
from __future__ import annotations

import argparse
import re
import struct
import sys
from pathlib import Path

import numpy as np

DIELECTRIC_SPECULARITY = 0.06
# Metal albedo: linear luminance floor and range reached at base luminance 0.30.
METAL_LUMINANCE_FLOOR = 0.18
METAL_LUMINANCE_RANGE = 0.62
METAL_LUMINANCE_KNEE = 0.30
METAL_LUMINANCE_CURVE = 0.6
METAL_SATURATION_DAMPING = 0.6
# Metal roughness: 0.02 + 0.30 * source, kept within 0.06-0.25. NWN picks the environment
# map mip as roughness * 30 - 1, so satin metal above about 0.35 reflects only a flat blur.
METAL_ROUGHNESS_OFFSET = 0.02
METAL_ROUGHNESS_SCALE = 0.30
METAL_ROUGHNESS_RANGE = (0.06, 0.25)
LUMINANCE = np.array([0.2126, 0.7152, 0.0722], np.float32)


def linear(rgb):
    return np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)


def srgb(rgb):
    return np.where(rgb <= .0031308, rgb * 12.92, 1.055 * np.maximum(rgb, 0) ** (1 / 2.4) - .055)


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0, 1)
    return t * t * (3 - 2 * t)


def specularity_to_metallic(spec):
    # inc_material with a specular map: clamp(3 * specularity - 0.6, 0, 1).
    return np.clip(3 * np.asarray(spec, np.float32) - .6, 0, 1)


def metal_specularity(metal):
    """Specularity that reproduces metal; non-metal texels get a low dielectric value."""
    metal = np.clip(np.asarray(metal, np.float32), 0, 1)
    return DIELECTRIC_SPECULARITY + ((metal + .6) / 3 - DIELECTRIC_SPECULARITY) * smoothstep(0, .1, metal)


def metal_roughness(rough, metal):
    rough = np.clip(np.asarray(rough, np.float32), 0, 1)
    target = np.clip(METAL_ROUGHNESS_OFFSET + METAL_ROUGHNESS_SCALE * rough, *METAL_ROUGHNESS_RANGE)
    return rough + (target - rough) * np.clip(metal, 0, 1)


def metal_base_color(base_srgb, metal):
    """Raise metal texels into physical albedo, keeping hue and relative brightness."""
    color = linear(np.clip(np.asarray(base_srgb, np.float32), 0, 1))
    lum = color @ LUMINANCE
    t = np.clip(lum / METAL_LUMINANCE_KNEE, 0, 1) ** METAL_LUMINANCE_CURVE
    target = METAL_LUMINANCE_FLOOR + METAL_LUMINANCE_RANGE * t
    # Saturated finishes (paint, anodizing) gain less, so they deepen instead of turning neon.
    saturation = (color.max(-1) - color.min(-1)) / np.maximum(color.max(-1), 1e-4)
    gain = 1 - METAL_SATURATION_DAMPING * saturation
    wanted = lum + np.maximum(target - lum, 0) * np.clip(metal, 0, 1) * gain
    safe = np.maximum(lum, 1e-4)
    scaled = color * (wanted / safe)[..., None]
    # Texels with no measurable color stay neutral grey.
    scaled = np.where((lum < 1e-4)[..., None], wanted[..., None], scaled)
    return srgb(np.clip(scaled, 0, 1))


def resample_nearest(image, shape):
    """Nearest-texel resample so a metal mask can drive a base map of another size."""
    h, w = image.shape[:2]
    if (h, w) == tuple(shape):
        return image
    rows = np.minimum((np.arange(shape[0]) + .5) * h / shape[0], h - 1).astype(int)
    cols = np.minimum((np.arange(shape[1]) + .5) * w / shape[1], w - 1).astype(int)
    return image[rows][:, cols]


# ---------------------------------------------------------------- DDS (DXT1 / DXT5 / ATI1)
# NWN stores DDS rows bottom-up; every operation here is per texel or a 2x2 box,
# so maps are processed in storage order and never flipped.

def read_dds(path):
    data = Path(path).read_bytes()
    if data[:4] != b'DDS ':
        raise ValueError(f'{path}: not a standard DDS')
    height, width = struct.unpack_from('<II', data, 12)
    mips = max(1, struct.unpack_from('<I', data, 28)[0])
    fourcc = data[84:88].decode('ascii')
    bw, bh = max(1, (width + 3) // 4), max(1, (height + 3) // 4)
    if fourcc == 'DXT1':
        pixels = _decode_bc1(np.frombuffer(data, np.uint8, bw * bh * 8, 128).reshape(-1, 8))
    elif fourcc == 'ATI1':
        pixels = _decode_bc4(np.frombuffer(data, np.uint8, bw * bh * 8, 128).reshape(-1, 8))[..., None]
    elif fourcc == 'DXT5':
        blocks = np.frombuffer(data, np.uint8, bw * bh * 16, 128).reshape(-1, 16)
        pixels = np.concatenate([_decode_bc1(blocks[:, 8:], four_color=True), _decode_bc4(blocks[:, :8])[..., None]], -1)
    else:
        raise ValueError(f'{path}: unsupported {fourcc}')
    channels = pixels.shape[-1]
    image = pixels.reshape(bh, bw, 4, 4, channels).transpose(0, 2, 1, 3, 4).reshape(bh * 4, bw * 4, channels)
    return image[:height, :width] / 255.0, {'header': data[:128], 'fourcc': fourcc, 'mips': mips, 'size': (width, height)}


# Stamped into the last reserved header dword (dwReserved1[10]) of every converted map;
# readers ignore it, and it marks maps with no dielectric texels left to detect.
CONVERTED_MARK = b'SWNM'
MARK_OFFSET = 72


def is_marked(info):
    return info['header'][MARK_OFFSET:MARK_OFFSET + 4] == CONVERTED_MARK


def write_dds(path, image, info, color=False, mark=False):
    """Encode image (H x W x C, 0-1) with the original header, format and mip count."""
    width, height = info['size']
    if image.shape[:2] != (height, width):
        raise ValueError(f'{path}: dimensions changed')
    level = np.asarray(image, np.float32)
    payload = bytearray()
    for index in range(info['mips']):
        payload += _ENCODERS[info['fourcc']](level)
        if index + 1 < info['mips']:
            level = _downsample(level, color)
    header = bytearray(info['header'])
    if mark:
        header[MARK_OFFSET:MARK_OFFSET + 4] = CONVERTED_MARK
    Path(path).write_bytes(bytes(header) + bytes(payload))


def _downsample(image, color):
    """Box-filter one power-of-two mip level, averaging color in linear light."""
    h, w = image.shape[:2]
    sy, sx = (2 if h > 1 else 1), (2 if w > 1 else 1)
    work = linear(image) if color else image
    work = work.reshape(h // sy, sy, w // sx, sx, -1).mean(axis=(1, 3))
    return srgb(work) if color else work


def _blocks(image):
    h, w = image.shape[:2]
    ph, pw = -h % 4, -w % 4
    image = np.pad(image, ((0, ph), (0, pw)) + ((0, 0),) * (image.ndim - 2), mode='edge')
    bh, bw = image.shape[0] // 4, image.shape[1] // 4
    shape = (bh, 4, bw, 4) + image.shape[2:]
    return image.reshape(shape).swapaxes(1, 2).reshape((bh * bw, 16) + image.shape[2:])


def _encode_bc4(channel):
    values = np.rint(np.clip(_blocks(channel), 0, 1) * 255).astype(np.int16)
    hi, lo = values.max(1), values.min(1)
    same = hi == lo
    lo = np.where(same & (hi > 0), hi - 1, lo)
    hi = np.where(same & (hi == 0), 1, hi)
    palette = np.empty((len(values), 8), np.int16)
    palette[:, 0], palette[:, 1] = hi, lo
    for k in range(1, 7):
        palette[:, k + 1] = ((7 - k) * hi + k * lo + 3) // 7
    index = np.abs(values[:, :, None] - palette[:, None, :]).argmin(2).astype(np.uint64)
    bits = np.bitwise_or.reduce(index << (np.arange(16, dtype=np.uint64) * 3), axis=1)
    out = np.zeros((len(values), 8), np.uint8)
    out[:, 0], out[:, 1] = hi, lo
    out[:, 2:] = bits.astype('<u8').view(np.uint8).reshape(-1, 8)[:, :6]
    return out.tobytes()


def _to565(rgb):
    q = np.rint(np.clip(rgb, 0, 255) * np.array([31, 63, 31]) / 255).astype(np.uint32)
    return (q[..., 0] << 11) | (q[..., 1] << 5) | q[..., 2]


def _from565(c):
    c = c.astype(np.uint32)
    return np.stack([((c >> 11) & 31) * 255 / 31, ((c >> 5) & 63) * 255 / 63, (c & 31) * 255 / 31], -1).astype(np.float32)


def _bc1_fit(colors, c0, c1):
    """Order endpoints for four-color mode and pick nearest indices."""
    swap = c0 < c1
    c0, c1 = np.where(swap, c1, c0), np.where(swap, c0, c1)
    a, b = _from565(c0), _from565(c1)
    palette = np.stack([a, b, (2 * a + b) / 3, (a + 2 * b) / 3], 1)
    dist = ((colors[:, :, None, :] - palette[:, None, :, :]) ** 2).sum(-1)
    index = dist.argmin(2)
    index = np.where((c0 == c1)[:, None], 0, index)
    error = np.take_along_axis(dist, index[..., None], 2)[..., 0].sum(1)
    return c0, c1, index, error


def _encode_bc1(image):
    colors = np.clip(_blocks(image[..., :3]), 0, 1).astype(np.float32) * 255
    mean = colors.mean(1, keepdims=True)
    centered = colors - mean
    cov = np.einsum('nki,nkj->nij', centered, centered)
    # Exact principal axis (eigh sorts eigenvalues ascending); power iteration can lock onto a
    # smaller eigenvector when its seed misses the leading one.
    axis = np.linalg.eigh(cov.astype(np.float64))[1][..., -1].astype(np.float32)
    proj = np.einsum('nki,ni->nk', centered, axis)
    lo = mean[:, 0] + axis * proj.min(1, keepdims=True)
    hi = mean[:, 0] + axis * proj.max(1, keepdims=True)
    best = _bc1_fit(colors, _to565(hi), _to565(lo))
    # Two least-squares refinements of the endpoints for the chosen indices.
    weights = np.array([1, 0, 2 / 3, 1 / 3], np.float32)
    for _ in range(2):
        c0, c1, index, error = best
        w = weights[index]
        aa, bb, ab = (w * w).sum(1), ((1 - w) ** 2).sum(1), (w * (1 - w)).sum(1)
        det = aa * bb - ab * ab
        ok = np.abs(det) > 1e-6
        xa = (w[..., None] * colors).sum(1)
        xb = ((1 - w)[..., None] * colors).sum(1)
        safe = np.where(ok, det, 1)[:, None]
        e0 = (bb[:, None] * xa - ab[:, None] * xb) / safe
        e1 = (aa[:, None] * xb - ab[:, None] * xa) / safe
        trial = _bc1_fit(colors, np.where(ok, _to565(e0), c0), np.where(ok, _to565(e1), c1))
        better = trial[3] < error
        best = tuple(np.where(better[:, None] if part.ndim == 2 else better, t, part)
                     for t, part in zip(trial, best))
    c0, c1, index, _ = best
    bits = np.bitwise_or.reduce(index.astype(np.uint32) << (np.arange(16, dtype=np.uint32) * 2), axis=1)
    out = np.zeros((len(colors), 8), np.uint8)
    out[:, 0:2] = c0.astype('<u2').view(np.uint8).reshape(-1, 2)
    out[:, 2:4] = c1.astype('<u2').view(np.uint8).reshape(-1, 2)
    out[:, 4:8] = bits.astype('<u4').view(np.uint8).reshape(-1, 4)
    return out.tobytes()


def _encode_bc3(image):
    color = np.frombuffer(_encode_bc1(image[..., :3]), np.uint8).reshape(-1, 8)
    alpha = np.frombuffer(_encode_bc4(image[..., 3]), np.uint8).reshape(-1, 8)
    return np.concatenate([alpha, color], 1).tobytes()


_ENCODERS = {'DXT1': _encode_bc1, 'DXT5': _encode_bc3, 'ATI1': lambda image: _encode_bc4(image[..., 0])}


def _decode_bc1(blocks, four_color=False):
    c0 = blocks[:, 0].astype(np.uint32) | (blocks[:, 1].astype(np.uint32) << 8)
    c1 = blocks[:, 2].astype(np.uint32) | (blocks[:, 3].astype(np.uint32) << 8)
    a, b = _from565(c0), _from565(c1)
    four = (c0 > c1)[:, None] | four_color
    palette = np.stack([a, b, np.where(four, (2 * a + b) / 3, (a + b) / 2), np.where(four, (a + 2 * b) / 3, 0)], 1)
    bits = blocks[:, 4:8].copy().view('<u4')[:, 0]
    index = (bits[:, None] >> (2 * np.arange(16, dtype=np.uint32))) & 3
    return np.take_along_axis(palette, index[..., None].astype(np.int64).repeat(3, -1), 1)


def _decode_bc4(blocks):
    a0, a1 = blocks[:, 0].astype(np.float32), blocks[:, 1].astype(np.float32)
    bits = np.zeros(len(blocks), np.uint64)
    for i in range(6):
        bits |= blocks[:, 2 + i].astype(np.uint64) << np.uint64(8 * i)
    index = ((bits[:, None] >> (np.uint64(3) * np.arange(16, dtype=np.uint64))) & np.uint64(7)).astype(np.int64)
    k = np.arange(8, dtype=np.float32)
    big = (a0 > a1)[:, None]
    eight = np.where(k == 0, a0[:, None], np.where(k == 1, a1[:, None], ((8 - k) * a0[:, None] + (k - 1) * a1[:, None]) / 7))
    six = np.where(k == 0, a0[:, None], np.where(k == 1, a1[:, None], np.where(k == 6, 0, np.where(
        k == 7, 255, ((6 - k) * a0[:, None] + (k - 1) * a1[:, None]) / 5))))
    return np.take_along_axis(np.where(big, eight, six), index, 1)


# ---------------------------------------------------------------- shipped-map fix

def material_textures(path):
    text = Path(path).read_text(encoding='ascii')
    slots = dict(re.findall(r'^texture(\d)\s+(\S+)', text, re.M))
    if not all(k in slots for k in '0123') or 'texture5' in text:
        raise ValueError(f'{path}: expected a texture0-3 TRELLIS material without emission')
    return slots['0'], slots['2'], slots['3']


def already_converted(spec, info):
    """Converted maps carry the header mark; maps converted before it existed moved
    dielectric texels from 0.2 to 0.06."""
    return is_marked(info) or float((np.abs(spec - DIELECTRIC_SPECULARITY) < .02).mean()) > .01


def fix_shipped(folder, mtr_names, check=False):
    folder = Path(folder)
    found = {p.name.lower(): p for p in folder.iterdir()}

    def resolve(name, ext):
        path = found.get((name + ext).lower())
        if path is None:
            raise FileNotFoundError(name + ext)
        return path

    plans = [(name, *material_textures(resolve(Path(name).stem, '.mtr'))) for name in mtr_names]
    specs, roughs = {}, {}
    for _, _, spec, rough in plans:
        if spec.lower() not in specs:
            specs[spec.lower()] = read_dds(resolve(spec, '.dds'))
        if rough.lower() not in roughs:
            roughs[rough.lower()] = read_dds(resolve(rough, '.dds'))
    bases = {name: read_dds(resolve(base, '.dds')) for name, base, _, _ in plans}
    done = [name for name, (image, info) in specs.items() if already_converted(image[..., 0], info)]
    done += [name for name, (_, info) in list(roughs.items()) + list(bases.items()) if is_marked(info)]
    if done:
        raise ValueError('Already converted maps: ' + ', '.join(sorted(done)[:10]))
    # Roughness follows its specularity map's metal mask, so each roughness map needs one partner.
    pair = {}
    for _, _, spec, rough in plans:
        if pair.setdefault(rough.lower(), spec.lower()) != spec.lower():
            raise ValueError(f'Roughness map {rough} pairs with more than one specularity map')
    # A base map is corrected once, so every material that names it must use the same metal mask.
    base_pair = {}
    for name, base, spec, _ in plans:
        if base_pair.setdefault(base.lower(), spec.lower()) != spec.lower():
            raise ValueError(f'Base map {base} pairs with more than one specularity map')
    # Convert everything in memory first: a size mismatch must not leave a batch half written and marked.
    report, outputs = [], []
    for name, base, spec, _ in plans:
        image, info = bases[name]
        spec_image = specs[spec.lower()][0][..., 0]
        if spec_image.shape != image.shape[:2]:
            raise ValueError(f'{name}: base and specularity sizes differ')
        metal = specularity_to_metallic(spec_image)
        fixed = metal_base_color(image, metal)
        report.append((name, float((metal > .5).mean()), float(linear(image).mean()), float(linear(fixed).mean())))
        if all(path != resolve(base, '.dds') for path, *_ in outputs):
            outputs.append((resolve(base, '.dds'), fixed, info, True))
    for rough_name, spec_name in pair.items():
        metal = specularity_to_metallic(specs[spec_name][0][..., 0])
        rough_image, rough_info = roughs[rough_name]
        if rough_image.shape[:2] != metal.shape:
            raise ValueError(f'{rough_name}: roughness and specularity sizes differ')
        outputs.append((resolve(rough_name, '.dds'), metal_roughness(rough_image[..., 0], metal)[..., None], rough_info, False))
    for spec_name, (image, info) in specs.items():
        outputs.append((resolve(spec_name, '.dds'), metal_specularity(specularity_to_metallic(image[..., 0]))[..., None], info, False))
    if not check:
        for path, image, info, color in outputs:
            write_dds(path, image, info, color=color, mark=True)
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--haks', type=Path, required=True)
    parser.add_argument('--materials', type=Path, required=True)
    parser.add_argument('--folder', default='sw_weapon')
    parser.add_argument('--check', action='store_true', help='Validate and report without writing')
    args = parser.parse_args(argv)
    names = [line.strip() for line in args.materials.read_text().splitlines() if line.strip()]
    report = fix_shipped(args.haks / args.folder, names, args.check)
    metal = np.array([r[1] for r in report])
    print(f'{"Checked" if args.check else "Converted"} {len(report)} materials; '
          f'median metal coverage {np.median(metal):.2f}; '
          f'median linear base {np.median([r[2] for r in report]):.3f} -> {np.median([r[3] for r in report]):.3f}')


if __name__ == '__main__':
    sys.exit(main())
