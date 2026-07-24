#!/usr/bin/env python3
"""Generate explicit START/GOAL slicing art from the original themed assets."""

from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
UI_DIRECTORY = ROOT / "SWLOR_Haks" / "sw_ui"

AMBER = (255, 196, 64, 255)
AMBER_GLOW = (255, 205, 72, 150)
MAGENTA = (255, 74, 124, 255)
MAGENTA_GLOW = (255, 50, 112, 150)
CREAM = (242, 234, 207, 255)
DARK_PANEL = (5, 12, 16, 225)


def load_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    candidates = [
        Path(os.environ.get("WINDIR", r"C:\Windows")) / "Fonts" / "arialbd.ttf",
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)

    return ImageFont.load_default(size=size)


def draw_centered_text(
    draw: ImageDraw.ImageDraw,
    bounds: tuple[int, int, int, int],
    text: str,
    font: ImageFont.FreeTypeFont | ImageFont.ImageFont,
    color: tuple[int, int, int, int],
) -> None:
    left, top, right, bottom = bounds
    text_bounds = draw.textbbox((0, 0), text, font=font)
    width = text_bounds[2] - text_bounds[0]
    height = text_bounds[3] - text_bounds[1]
    x = left + (right - left - width) / 2
    y = top + (bottom - top - height) / 2 - text_bounds[1]
    draw.text((x, y), text, font=font, fill=color)


def add_glow(
    image: Image.Image,
    color: tuple[int, int, int, int],
    draw_shape,
    radius: int,
) -> Image.Image:
    glow = Image.new("RGBA", image.size, (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    draw_shape(glow_draw, color)
    blurred = glow.filter(ImageFilter.GaussianBlur(radius))
    return Image.alpha_composite(Image.alpha_composite(image, blurred), glow)


def create_objective_banner(theme: str) -> None:
    source = UI_DIRECTORY / f"slc_bg_{theme}.tga"
    output = UI_DIRECTORY / f"slc_goal_{theme}.tga"
    image = Image.open(source).convert("RGBA")

    panel = Image.new("RGBA", image.size, (0, 0, 0, 0))
    panel_draw = ImageDraw.Draw(panel)
    panel_draw.rounded_rectangle((168, 7, 472, 89), radius=10, fill=DARK_PANEL, outline=(102, 84, 51, 235), width=2)
    image = Image.alpha_composite(image, panel)

    image = add_glow(
        image,
        AMBER_GLOW,
        lambda draw, color: draw.line((253, 50, 387, 50), fill=color, width=7),
        5,
    )

    draw = ImageDraw.Draw(image)
    draw.line((253, 50, 387, 50), fill=AMBER, width=3)
    draw.polygon(((387, 42), (402, 50), (387, 58)), fill=AMBER)

    draw.ellipse((207, 30, 247, 70), outline=AMBER, width=4)
    draw.polygon(((217, 40), (217, 60), (237, 50)), fill=AMBER)

    draw.ellipse((396, 27, 442, 73), outline=MAGENTA, width=4)
    draw.ellipse((406, 37, 432, 63), outline=MAGENTA, width=3)
    draw.ellipse((415, 46, 423, 54), fill=MAGENTA)
    draw.line((419, 24, 419, 33), fill=MAGENTA, width=3)
    draw.line((419, 67, 419, 76), fill=MAGENTA, width=3)
    draw.line((393, 50, 402, 50), fill=MAGENTA, width=3)
    draw.line((436, 50, 445, 50), fill=MAGENTA, width=3)

    heading_font = load_font(14)
    label_font = load_font(12)
    draw_centered_text(draw, (252, 13, 388, 34), "POWER ROUTE", heading_font, CREAM)
    draw_centered_text(draw, (187, 72, 267, 89), "START", label_font, AMBER)
    draw_centered_text(draw, (379, 72, 459, 89), "GOAL", label_font, MAGENTA)

    image.save(output, format="TGA")


def create_endpoint_tile(theme: str, tile_type: str, orientation: int, state: str) -> None:
    source = UI_DIRECTORY / f"slc{theme}{tile_type}{orientation}{state}.tga"
    output = UI_DIRECTORY / f"slcg{theme}{tile_type}{orientation}{state}.tga"
    image = Image.open(source).convert("RGBA")
    is_entry = tile_type == "e"
    accent = AMBER if is_entry else MAGENTA
    glow_color = AMBER_GLOW if is_entry else MAGENTA_GLOW

    image = add_glow(
        image,
        glow_color,
        lambda draw, color: draw.ellipse((25, 25, 47, 47), outline=color, width=4),
        3,
    )

    draw = ImageDraw.Draw(image)
    draw.ellipse((26, 26, 46, 46), outline=accent, width=2)
    if is_entry:
        draw.polygon(((29, 30), (29, 42), (41, 36)), fill=accent)
        badge = (3, 3, 34, 16)
        label = "START"
    else:
        draw.ellipse((31, 31, 41, 41), outline=accent, width=2)
        draw.ellipse((34, 34, 38, 38), fill=accent)
        draw.line((36, 22, 36, 27), fill=accent, width=2)
        draw.line((36, 45, 36, 50), fill=accent, width=2)
        draw.line((22, 36, 27, 36), fill=accent, width=2)
        draw.line((45, 36, 50, 36), fill=accent, width=2)
        badge = (39, 3, 69, 16)
        label = "GOAL"

    draw.rounded_rectangle(badge, radius=3, fill=(5, 12, 16, 235), outline=accent, width=1)
    draw_centered_text(draw, badge, label, load_font(7), accent)
    image.save(output, format="TGA")


def main() -> None:
    for theme in ("l", "t"):
        create_objective_banner(theme)
        for tile_type in ("e", "o"):
            for orientation in range(4):
                for state in ("u", "p", "s", "d"):
                    create_endpoint_tile(theme, tile_type, orientation, state)

    print("Generated 2 slicing objective banners and 64 explicit endpoint tiles.")


if __name__ == "__main__":
    main()
