"""Bake CU head color, lip overlays and cutouts for the small editor pictures.

These are asset thumbnails; Blender still supplies the preview lighting. The
runtime shaders remain authoritative for in-game appearance.
"""
import re

import numpy as np
from PIL import Image

from .tint import LAYER_BASE, PREVIEW_DYES, bake, open_texture, read_tint_map


def parameters(text):
    return {name: float(value) for name, value in re.findall(
        r"(?im)^\s*parameter\s+float\s+(\w+)\s+([-\d.eE+]+)", text)}


def resources(text):
    return {int(slot): name.lower() for slot, name in re.findall(
        r"(?im)^\s*texture([01689])\s+(\S+)", text) if name.lower() != "null"}


def sample(image, u, v):
    """Bilinear, clamped sampling in NWN's bottom-up UV coordinates."""
    pixels = np.asarray(image.convert("RGBA"), np.float32) / 255.0
    x = np.clip(u * image.width - 0.5, 0, image.width - 1)
    y = np.clip((1.0 - v) * image.height - 0.5, 0, image.height - 1)
    x0, y0 = x.astype(int), y.astype(int)
    x1, y1 = np.minimum(x0 + 1, image.width - 1), np.minimum(y0 + 1, image.height - 1)
    dx, dy = (x - x0)[..., None], (y - y0)[..., None]
    return ((pixels[y0, x0] * (1 - dx) + pixels[y0, x1] * dx) * (1 - dy)
            + (pixels[y1, x0] * (1 - dx) + pixels[y1, x1] * dx) * dy)


def colors(shade, layer, palette, defaults, params):
    shade = shade.copy()
    hair = layer == 1
    shade[hair] = np.clip((shade[hair] / 255.0) ** params.get("hairShadePower", 1) * 255, 0, 255)
    shade = np.clip(shade, 0, 255).astype(int)
    reflectance = np.clip(params.get("metalReflectanceScale", 1), 0, 1)
    atlas_rgba = np.asarray(Image.open(palette).convert("RGBA"), np.float32) / 255.0
    metal_layers = (layer == 2) | (layer == 3)

    def palette_colors(dyes=PREVIEW_DYES):
        rgb, metal = bake(shade, layer, palette, dyes)
        if reflectance != 1:
            rows = np.array([LAYER_BASE[i] + dyes[i] for i in range(10)])
            native = atlas_rgba[atlas_rgba.shape[0] - 1 - rows[layer], shade]
            environment = (1 - native[..., 3]) * reflectance
            coated = native[..., :3] * (1 - environment[..., None]) + 0.666667 * environment[..., None]
            rgb[metal_layers] = np.clip(coated[metal_layers] * 255, 0, 255).astype(np.uint8)
            metal[metal_layers] = np.clip(environment[metal_layers] * 255, 0, 255).astype(np.uint8)
        return rgb, metal

    rgb, metal = palette_colors()
    rgb = rgb.astype(np.float32) / 255.0
    atlas = atlas_rgba[..., :3]
    luma = np.array([0.2126, 0.7152, 0.0722])
    for channel, stored in defaults.items():
        mask = layer == channel
        row = LAYER_BASE[channel]
        reference = atlas[atlas.shape[0] - 1 - row]
        neutral = (reference[np.clip(shade, 0, 255).astype(int)] @ luma) / max(reference[128] @ luma, 1 / 255)
        if stored & 0xFF000000 == 0x01000000:
            selected = np.array([(stored >> 16) & 255, (stored >> 8) & 255, stored & 255]) / 255.0
            rgb[mask] = (selected * neutral[..., None])[mask]
        elif 0 <= stored < 176:
            rows = np.array(PREVIEW_DYES)
            rows[channel] = stored
            replacement, _ = palette_colors(rows)
            rgb[mask] = replacement[mask] / 255.0
    rgb[layer == 0] *= params.get("skinGain", 1)
    saturation = params.get("hairSaturation", 1)
    rgb[hair] = ((rgb[hair] @ luma)[:, None] * (1 - saturation) + rgb[hair] * saturation) * params.get("hairGain", 1)
    return np.clip(rgb, 0, 1), metal


def bake_head(source, palette, text, inputs, defaults):
    shade, layer = read_tint_map(source)
    params = parameters(text)
    rgb, metal = colors(shade, layer, palette, defaults, params)
    height, width = layer.shape
    u, v = np.meshgrid((np.arange(width) + 0.5) / width, 1 - (np.arange(height) + 0.5) / height)
    loaded = {slot: open_texture(path) for slot, path in inputs.items()}
    sampled = {slot: sample(image, u, v) for slot, image in loaded.items()}
    mode = params.get("lipMode", 0)
    if mode > 0.5:
        lu = (u - params.get("lipU0", 0)) / max(params.get("lipUSpan", 1), 1e-6)
        lv = (v - params.get("lipV0", 0)) / max(params.get("lipVSpan", 1), 1e-6)
        inside = (lu >= 0) & (lu <= 1) & (lv >= 0) & (lv <= 1)
        coverage = np.zeros(layer.shape)
        lip_shade = shade
        if 1.5 < mode < 2.5 and 6 in sampled:
            coverage = sampled[6][..., 0]
        else:
            slot = 9 if mode > 2.5 else 8
            if slot in loaded:
                lip = sample(loaded[slot], lu, lv)
                coverage = np.where(inside, lip[..., 0], 0)
                if 2.5 < mode < 3.5:
                    lip_shade = lip[..., 1] * 255
        eligible = (layer == 0) | (layer == 7) | ((mode > 1.5) & (mode < 3.5))
        coverage *= eligible
        base, _ = colors(shade, np.where(layer == 7, 0, layer), palette, defaults, params)
        lips, _ = colors(lip_shade, np.full(layer.shape, 7), palette, defaults, params)
        rgb = base * (1 - coverage[..., None]) + lips * coverage[..., None]
    if params.get("detailDiffuse", 0) > 0.5 and 8 in sampled and 0 in sampled:
        detail = sampled[8][..., 0, None]
        rgb = rgb * (1 - detail) + sampled[0][..., :3] * detail
    alpha = np.ones(layer.shape)
    if params.get("useTexture1Alpha", 0) > 0.5 and 1 in sampled:
        alpha *= sampled[1][..., 3] >= 0.2
    elif params.get("useTexture9Alpha", 0) > 0.5 and 9 in sampled:
        alpha *= sampled[9][..., 0] >= params.get("texture9Cutoff", 0.3)
    mask_mode = params.get("maskMode", 0)
    if mask_mode > 0.5 and 6 in sampled:
        coverage = sampled[6][..., 3 if 1.5 < mask_mode < 2.5 else 0]
        if mask_mode > 3.5:
            coverage = np.where(layer == 1, coverage, 1)
        alpha *= coverage >= params.get("alphaCutoff", 0.3)
    rgba = np.dstack((np.clip(rgb * 255, 0, 255).astype(np.uint8), (alpha * 255).astype(np.uint8)))
    return rgba, metal
