#!/usr/bin/env python3
"""Generate the Slicing NUI background and complete circuit-tile sprite set."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = ROOT / "design" / "art" / "slicing_nui_source.png"
DEFAULT_OUTPUT = ROOT / "SWLOR_Haks" / "sw_ui"
PREVIEW = ROOT / "design" / "art" / "slicing_nui_preview.png"
MANIFEST = ROOT / "design" / "art" / "slicing_nui_manifest.json"

SIZE = 72
SCALE = 4
THEMES = {
    "l": {"name": "lockbox", "accent": (232, 151, 42), "secondary": (255, 205, 92)},
    "t": {"name": "terminal", "accent": (40, 195, 225), "secondary": (124, 239, 255)},
}
TYPES = ("s", "c", "j", "x", "e", "o", "b", "q")
STATES = ("u", "p", "s", "d")
BASE_CONNECTIONS = {
    "s": (0, 2),
    "c": (0, 1),
    "j": (0, 1, 2),
    "x": (0, 1, 2, 3),
    "e": (0,),
    "o": (0,),
    "b": (),
    "q": (),
}


def rotate_connections(tile_type: str, orientation: int) -> tuple[int, ...]:
    return tuple((direction + orientation) % 4 for direction in BASE_CONNECTIONS[tile_type])


def scaled_point(point: tuple[float, float]) -> tuple[int, int]:
    return round(point[0] * SCALE), round(point[1] * SCALE)


def draw_tile(theme_key: str, tile_type: str, orientation: int, state: str) -> Image.Image:
    theme = THEMES[theme_key]
    accent = theme["accent"]
    secondary = theme["secondary"]
    image = Image.new("RGB", (SIZE * SCALE, SIZE * SCALE), (8, 13, 18))
    draw = ImageDraw.Draw(image)

    # Layered durasteel plate with clipped corners and circuit-board traces.
    panel = [scaled_point(p) for p in ((7, 2), (65, 2), (70, 7), (70, 65), (65, 70), (7, 70), (2, 65), (2, 7))]
    draw.polygon(panel, fill=(20, 29, 35), outline=(72, 82, 88), width=1 * SCALE)
    draw.line([scaled_point((8, 10)), scaled_point((22, 10)), scaled_point((27, 15))], fill=(40, 52, 58), width=SCALE)
    draw.line([scaled_point((64, 60)), scaled_point((52, 60)), scaled_point((47, 55))], fill=(40, 52, 58), width=SCALE)
    for point in ((10, 60), (61, 12)):
        x, y = scaled_point(point)
        radius = 2 * SCALE
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(55, 65, 70))

    if state == "p":
        line_color = secondary
        glow_color = tuple(max(0, channel // 3) for channel in accent)
    elif state == "s":
        line_color = (245, 245, 225)
        glow_color = tuple(max(0, channel // 3) for channel in accent)
    elif state == "d":
        line_color = (180, 92, 58)
        glow_color = (63, 24, 18)
    else:
        line_color = tuple(max(54, channel // 2) for channel in accent)
        glow_color = tuple(max(0, channel // 6) for channel in accent)

    center = scaled_point((36, 36))
    edge_points = {
        0: scaled_point((36, 0)),
        1: scaled_point((72, 36)),
        2: scaled_point((36, 72)),
        3: scaled_point((0, 36)),
    }
    connections = rotate_connections(tile_type, orientation)
    for direction in connections:
        draw.line((center, edge_points[direction]), fill=glow_color, width=12 * SCALE)
        draw.line((center, edge_points[direction]), fill=line_color, width=5 * SCALE)
        draw.line((center, edge_points[direction]), fill=(230, 240, 235) if state == "p" else line_color, width=SCALE)

    if tile_type not in ("b", "q"):
        radius = 8 * SCALE
        draw.ellipse((center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius),
                     fill=(12, 19, 23), outline=line_color, width=3 * SCALE)
        node_radius = (3 if state != "p" else 4) * SCALE
        node_color = (250, 251, 224) if state == "p" else line_color
        draw.ellipse((center[0] - node_radius, center[1] - node_radius,
                      center[0] + node_radius, center[1] + node_radius), fill=node_color)

    if tile_type == "e":
        # Entry uses a filled directional triangle in addition to its single socket.
        points = [(-7, 6), (7, 6), (0, -8)]
        angle = orientation % 4
        transformed = []
        for x, y in points:
            for _ in range(angle):
                x, y = -y, x
            transformed.append(scaled_point((36 + x, 36 + y)))
        draw.polygon(transformed, fill=line_color)
    elif tile_type == "o":
        radius = 14 * SCALE
        draw.ellipse((center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius),
                     outline=line_color, width=3 * SCALE)
        inner = 5 * SCALE
        draw.rectangle((center[0] - inner, center[1] - inner, center[0] + inner, center[1] + inner),
                       outline=(235, 239, 223), width=2 * SCALE)
    elif tile_type == "b":
        draw.line((scaled_point((20, 20)), scaled_point((52, 52))), fill=(112, 50, 43), width=9 * SCALE)
        draw.line((scaled_point((52, 20)), scaled_point((20, 52))), fill=(112, 50, 43), width=9 * SCALE)
        draw.line((scaled_point((20, 20)), scaled_point((52, 52))), fill=(205, 104, 72), width=3 * SCALE)
        draw.line((scaled_point((52, 20)), scaled_point((20, 52))), fill=(205, 104, 72), width=3 * SCALE)
    elif tile_type == "q":
        for offset, width in ((0, 5), (9, 3), (-10, 2)):
            draw.line((scaled_point((13, 34 + offset)), scaled_point((58, 25 + offset))),
                      fill=line_color, width=width * SCALE)
        draw.rectangle((*scaled_point((28, 29)), *scaled_point((43, 44))), outline=(211, 72, 108), width=3 * SCALE)

    if state == "s":
        draw.line([scaled_point((36, 4)), scaled_point((68, 36)), scaled_point((36, 68)),
                   scaled_point((4, 36)), scaled_point((36, 4))], fill=secondary, width=3 * SCALE)
    elif state == "p":
        # Double corner marks provide a shape cue independent of hue.
        for x, y, sx, sy in ((6, 6, 1, 1), (66, 6, -1, 1), (6, 66, 1, -1), (66, 66, -1, -1)):
            draw.line((scaled_point((x, y)), scaled_point((x + sx * 7, y))), fill=(241, 245, 226), width=2 * SCALE)
            draw.line((scaled_point((x, y)), scaled_point((x, y + sy * 7))), fill=(241, 245, 226), width=2 * SCALE)
    elif state == "d":
        crack = [(14, 8), (22, 24), (18, 34), (29, 42), (23, 62)]
        draw.line([scaled_point(p) for p in crack], fill=(244, 107, 72), width=2 * SCALE)
        draw.polygon([scaled_point(p) for p in ((58, 52), (66, 66), (50, 66))],
                     fill=(151, 53, 38), outline=(255, 147, 89))

    return image.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def make_backgrounds(source: Image.Image, output: Path) -> None:
    source = source.convert("RGB")
    width, height = source.size
    halves = {"l": (0, 0, width // 2, height), "t": (width // 2, 0, width, height)}
    for theme_key, box in halves.items():
        crop = source.crop(box)
        target_ratio = 640 / 96
        crop_width, crop_height = crop.size
        desired_height = round(crop_width / target_ratio)
        if desired_height < crop_height:
            top = max(0, crop_height // 2 - desired_height // 2)
            crop = crop.crop((0, top, crop_width, top + desired_height))
        crop = crop.resize((640, 96), Image.Resampling.LANCZOS)
        crop = ImageEnhance.Contrast(crop).enhance(1.12)
        crop = crop.filter(ImageFilter.UnsharpMask(radius=1.1, percent=90, threshold=3))
        crop.save(output / f"slc_bg_{theme_key}.tga")


def make_preview(sprites: dict[str, Image.Image], backgrounds: dict[str, Image.Image]) -> None:
    preview = Image.new("RGB", (720, 650), (7, 10, 14))
    preview.paste(backgrounds["l"].resize((640, 96)), (40, 20))
    preview.paste(backgrounds["t"].resize((640, 96)), (40, 126))
    for theme_row, theme_key in enumerate(THEMES):
        for type_index, tile_type in enumerate(TYPES):
            for state_index, state in enumerate(STATES):
                key = f"slc{theme_key}{tile_type}{type_index % 4}{state}"
                preview.paste(sprites[key], (40 + type_index * 80, 250 + theme_row * 170 + state_index * 38))
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    preview.save(PREVIEW)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()
    if not args.source.exists():
        raise FileNotFoundError(f"Missing generated source art: {args.source}")

    args.output.mkdir(parents=True, exist_ok=True)
    source = Image.open(args.source)
    make_backgrounds(source, args.output)
    sprites: dict[str, Image.Image] = {}
    for theme_key in THEMES:
        for tile_type in TYPES:
            for orientation in range(4):
                for state in STATES:
                    resref = f"slc{theme_key}{tile_type}{orientation}{state}"
                    sprite = draw_tile(theme_key, tile_type, orientation, state)
                    sprite.save(args.output / f"{resref}.tga")
                    sprites[resref] = sprite

    backgrounds = {
        key: Image.open(args.output / f"slc_bg_{key}.tga").convert("RGB")
        for key in THEMES
    }
    make_preview(sprites, backgrounds)
    manifest = {
        "source": str(args.source.relative_to(ROOT)),
        "backgrounds": [f"slc_bg_{key}" for key in THEMES],
        "tile_count": len(sprites),
        "themes": THEMES,
        "types": list(TYPES),
        "orientations": 4,
        "states": list(STATES),
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Generated {len(sprites)} slicing tile sprites and 2 backgrounds in {args.output}")


if __name__ == "__main__":
    main()
