#!/usr/bin/env python3
"""Offline contact sheets of a robe wearer's skeleton playing the 15 crafting-revamp work clips.

Run from the SWLOR_Haks checkout:
    python -B render_robe_sheet.py --wearer pmh97 --out sheet.png [--summary summary.json]

The wearer root is sampled through its own bridge bank chain (the models a client loads for a robe
wearer), with the same pose sampler the robe generator audits against (RobePoseAudit.Model.pose).
Light bones are the body skeleton, orange joints are garment joints, and a bold orange joint is a
garment joint that carries its own tracks in the clip, which only a bridge-owned copy provides.
This is an offline render of installed assets, not NWN client evidence.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path.cwd()
sys.path.insert(0, str(ROOT / "tools"))

import CompileModels as mdl  # noqa: E402
import GenerateTintMapAssets as tint  # noqa: E402
import RobePoseAudit as poses  # noqa: E402

GROUPS = (
    ("Crafting stations", ("sw_forgework", "sw_circuitry", "sw_fabricate", "sw_cooking", "sw_compound")),
    ("Gathering", ("sw_survey", "sw_strike", "sw_hvystrike", "sw_extract", "sw_search", "sw_dig")),
    ("Fishing Line", ("sw_reel", "sw_giveline", "sw_holdrod", "sw_landfish")),
)
PHASES = (0, 1 / 6, 2 / 6, 3 / 6, 4 / 6, 5 / 6, 1)
CELL_W, CELL_H, HEAD = 150, 210, 22
YAW = math.radians(35)


def project(point):
    x, y, z = point
    return (x * math.cos(YAW) + y * math.sin(YAW), z)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--wearer", required=True, help="Wearer root resref, such as pmh97")
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--summary", type=Path)
    parser.add_argument("--title", default="")
    args = parser.parse_args()

    active = tint.find_active_models()

    def load(name):
        path = active.get(name)
        return path.read_bytes() if path else None

    library = poses.Library(load)
    wearer = poses.Model(load(args.wearer))
    base = args.wearer[:3] + "0"
    body_names = {node[0] for node in library.model(base).nodes}
    clips = library.clips(wearer.parent)
    owners = library.owners(wearer.parent)
    bridge_banks = [name for name in owners.values() if name.startswith(wearer.parent[:6])]
    garment = [index for index, node in enumerate(wearer.nodes)
               if index > 0 and node[0] not in body_names and node[0].startswith("rg_") and node[2] >= 0]
    shown = {index for index, node in enumerate(wearer.nodes) if node[0] in body_names and (node[0].endswith("_g") or node[0] == "rootdummy")}

    def shown_parent(index):
        parent = wearer.nodes[index][1]
        while parent is not None and parent not in shown:
            parent = wearer.nodes[parent][1]
        return parent

    names = []
    seen = {}
    for index, (name, *_rest) in enumerate(wearer.nodes):
        key = name if name not in seen else f"{name}_copy{index}"
        seen[name] = True
        names.append(key)

    try:
        font = ImageFont.truetype("arial.ttf", 11)
        bold = ImageFont.truetype("arialbd.ttf", 12)
    except OSError:
        font = bold = ImageFont.load_default()

    rows = [(group, clip) for group, members in GROUPS for clip in members]
    width, height = CELL_W * len(PHASES), HEAD * 2 + CELL_H * len(rows)
    sheet = Image.new("RGB", (width, height), (17, 20, 25))
    draw = ImageDraw.Draw(sheet)
    draw.text((6, 4), args.title or f"{args.wearer} (bridge {wearer.parent}): work clips through the robe bridge chain, offline render",
              fill=(235, 235, 235), font=bold)
    draw.text((6, 4 + 13), "light: body bones   orange: garment joints   bold orange: garment joint with its own tracks in the clip",
              fill=(150, 160, 170), font=font)

    # Fixed framing from the idle pose of the wearer, so every cell shares one scale.
    rest = wearer.pose()
    rest_points = [project(rest[names[i]][0]) for i in shown]
    top = max(p[1] for p in rest_points)
    scale = (CELL_H - 50) / (top * 1.1)

    summary = {"wearer": args.wearer, "bridge": wearer.parent, "bridgeBanksOwningClips": sorted(set(bridge_banks)), "clips": {}}
    for row, (group, clip) in enumerate(rows):
        y0 = HEAD * 2 + row * CELL_H
        duration, nodes = clips[clip]
        owner = owners[clip]
        tracked_parts = {n[2] for n in nodes if n[2] >= 0 and n[3]}
        garment_tracked = [i for i in garment if wearer.nodes[i][2] >= 0 and wearer.nodes[i][2] in tracked_parts]
        draw.text((6, y0 + 2), f"{clip}  {duration:.2f}s  owner {owner}  garment joints with tracks: {len(garment_tracked)}",
                  fill=(235, 235, 235), font=bold)
        travel = 0.0
        first = None
        for column, phase in enumerate(PHASES):
            pose = wearer.pose(clips[clip], phase * duration, wearer.scale)
            pts = [project(pose[key][0]) for key in names]
            if first is None:
                first = pose
            for index in garment_tracked:
                a, b = pose[names[index]][0], first[names[index]][0]
                travel = max(travel, math.dist(a, b))
            ox = column * CELL_W + CELL_W / 2
            oy = y0 + CELL_H - 14
            def at(p):
                return (ox + p[0] * scale, oy - p[1] * scale)
            draw.line([(column * CELL_W + 4, oy), (column * CELL_W + CELL_W - 4, oy)], fill=(60, 66, 75))
            for index in sorted(shown):
                parent = shown_parent(index)
                if parent is not None:
                    draw.line([at(pts[parent]), at(pts[index])], fill=(205, 210, 218), width=2)
            for index in garment:
                colour = (255, 163, 51) if index in garment_tracked else (176, 120, 60)
                size = 3 if index in garment_tracked else 1
                parent = wearer.nodes[index][1]
                if parent is not None and index in garment_tracked:
                    draw.line([at(pts[parent]), at(pts[index])], fill=colour, width=1)
                x, y = at(pts[index])
                draw.ellipse([x - size, y - size, x + size, y + size], fill=colour)
            draw.text((column * CELL_W + 4, y0 + 18), f"{phase * duration:.2f}s", fill=(150, 160, 170), font=font)
        summary["clips"][clip] = {
            "durationSeconds": round(duration, 3), "owner": owner,
            "garmentJointsWithTracks": len(garment_tracked), "maxGarmentTravelMetres": round(travel, 3),
        }
    sheet.save(args.out, optimize=True)
    if args.summary:
        args.summary.write_text(json.dumps(summary, indent=2) + "\n")
    print(json.dumps({k: v for k, v in summary.items() if k != "clips"}))
    for clip, record in summary["clips"].items():
        print(clip, record)


if __name__ == "__main__":
    main()
