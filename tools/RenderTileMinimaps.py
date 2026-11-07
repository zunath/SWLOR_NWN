"""Render orthographic minimap tiles from CleanModels ASCII and the original textures.

Input plan: [{model, image, owner, interior, size}]. Models use their authored
transforms and the fixed 10-metre tile bounds. Downward-facing ceilings are culled.
Interior walls receive a thin outline so vertical faces remain legible overhead.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re

import numpy as np
from PIL import Image, ImageDraw

from TileMinimaps import Resources, ROOT

NODE = re.compile(r"(?ims)^[ \t]*node[ \t]+(\S+)[ \t]+(\S+)[^\n]*\n(.*?)^[ \t]*endnode\b")


def field(body, name, default=""):
    match = re.search(r"(?im)^[ \t]*" + name + r"[ \t]+([^\r\n]+)", body)
    return match[1].strip() if match else default


def array(body, name, columns, dtype=float):
    match = re.search(r"(?im)^[ \t]*" + name + r"[ \t]+(\d+)[ \t]*\r?\n", body)
    if not match:
        return np.empty((0, columns), dtype=dtype)
    lines = body[match.end():].splitlines()[:int(match[1])]
    result = np.array([line.split()[:columns] for line in lines], dtype=dtype)
    if result.shape != (int(match[1]), columns):
        raise ValueError(f"Malformed {name} array")
    return result


def local_transform(body):
    matrix = np.eye(4)
    axis_angle = np.array(field(body, "orientation", "0 0 1 0").split(), dtype=float)
    axis, angle = axis_angle[:3], axis_angle[3]
    length = np.linalg.norm(axis)
    if length > 1e-10 and abs(angle) > 1e-10:
        x, y, z = axis / length
        skew = np.array([[0, -z, y], [z, 0, -x], [-y, x, 0]])
        matrix[:3, :3] = np.eye(3) + math.sin(angle) * skew + (1 - math.cos(angle)) * skew @ skew
    matrix[:3, :3] *= float(field(body, "scale", "1"))
    matrix[:3, 3] = np.array(field(body, "position", "0 0 0").split(), dtype=float)
    return matrix


def meshes(text):
    geometry = re.split(r"(?im)^[ \t]*endmodelgeom\b", text, maxsplit=1)[0]
    nodes = [(kind.lower(), name.lower(), body) for kind, name, body in NODE.findall(geometry)]
    parents = {name: (field(body, "parent", "null").lower(), local_transform(body))
               for _, name, body in nodes}
    transforms = {}

    def transform(name, visiting=None):
        if name in transforms:
            return transforms[name]
        visiting = set() if visiting is None else visiting
        if name in visiting:
            raise ValueError("Cyclic model hierarchy")
        visiting.add(name)
        parent, matrix = parents[name]
        if parent in parents:
            matrix = transform(parent, visiting) @ matrix
        transforms[name] = matrix
        visiting.remove(name)
        return matrix

    for kind, name, body in nodes:
        if kind not in {"trimesh", "danglymesh"} or field(body, "render", "1") == "0":
            continue
        vertices = array(body, "verts", 3)
        faces = array(body, "faces", 8, int)
        if not len(vertices) or not len(faces):
            continue
        matrix = transform(name)
        vertices = np.column_stack((vertices, np.ones(len(vertices)))) @ matrix.T
        yield {"name": name, "vertices": vertices[:, :3], "faces": faces,
               "uv": array(body, "tverts", 3), "bitmap": field(body, "bitmap", "null").lower()[:16],
               "alpha": float(field(body, "alpha", "1")),
               "diffuse": np.array(field(body, "diffuse", "1 1 1").split(), dtype=float)}


def raster_triangle(points, uv, texture, color, alpha, pixels, depth):
    size = pixels.shape[0]
    projected = np.column_stack(((points[:, 0] + 5) * size / 10,
                                 (5 - points[:, 1]) * size / 10))
    left, top = np.maximum(0, np.floor(projected.min(axis=0)).astype(int))
    right, bottom = np.minimum(size - 1, np.ceil(projected.max(axis=0)).astype(int))
    if right < left or bottom < top:
        return
    a, b, c = projected
    divisor = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
    if abs(divisor) < 1e-8:
        return
    yy, xx = np.mgrid[top:bottom + 1, left:right + 1]
    xx, yy = xx + .5, yy + .5
    w0 = ((b[1] - c[1]) * (xx - c[0]) + (c[0] - b[0]) * (yy - c[1])) / divisor
    w1 = ((c[1] - a[1]) * (xx - c[0]) + (a[0] - c[0]) * (yy - c[1])) / divisor
    weights = np.stack((w0, w1, 1 - w0 - w1), axis=-1)
    z = weights @ points[:, 2]
    mask = (weights.min(axis=-1) >= -1e-7) & (z >= depth[top:bottom + 1, left:right + 1] - 1e-5)
    if not mask.any():
        return
    sampled = np.ones((*mask.shape, 4))
    if texture is not None and uv is not None:
        coords = weights @ uv[:, :2]
        h, w = texture.shape[:2]
        cols = np.floor(np.mod(coords[..., 0], 1) * w).astype(int) % w
        rows = np.floor((1 - np.mod(coords[..., 1], 1)) * h).astype(int) % h
        sampled = texture[rows, cols]
    opacity = sampled[..., 3] * alpha
    mask &= opacity > .05
    target = pixels[top:bottom + 1, left:right + 1]
    rgb = np.clip(sampled[..., :3] * color, 0, 1)
    target[mask] = (rgb * opacity[..., None] + target * (1 - opacity[..., None]))[mask]
    opaque = mask & (opacity >= .98)
    depth[top:bottom + 1, left:right + 1][opaque] = z[opaque]


def render(text, resources, interior=False, size=32, texture_cache=None, texture_aliases=None):
    resolution = size * 4
    pixels = np.zeros((resolution, resolution, 3))
    depth = np.full((resolution, resolution), -np.inf)
    textures = {} if texture_cache is None else texture_cache
    triangles, walls, missing = [], [], set()
    light = np.array([-.3, .4, .8660254])
    for mesh in meshes(text):
        if interior and re.search(r"ceiling|roof", mesh["name"]):
            continue
        bitmap = mesh["bitmap"]
        if bitmap in {"null", "****", ""}:
            continue
        resolved = (texture_aliases or {}).get(bitmap, bitmap)
        if resolved not in textures:
            name = resources.texture_name(resolved)
            textures[resolved] = None if name is None else np.asarray(resources.image(name), dtype=float) / 255
        texture = textures[resolved]
        if texture is None:
            missing.add(bitmap)
        for face in mesh["faces"]:
            points = mesh["vertices"][face[:3]]
            normal = np.cross(points[1] - points[0], points[2] - points[0])
            norm = np.linalg.norm(normal)
            if norm < 1e-8:
                continue
            normal /= norm
            if interior and abs(normal[2]) < .05 and points[:, 2].max() > .5:
                walls.append(points)
            if normal[2] <= .01:
                continue
            uv = mesh["uv"][face[4:7]] if len(mesh["uv"]) else None
            shade = .65 + .35 * max(0, float(normal @ light))
            if bitmap.startswith("layer"):
                shade = 1
            triangles.append((points[:, 2].mean(), points, uv, texture,
                              mesh["diffuse"] * shade, mesh["alpha"]))
    if missing:
        raise ValueError("Missing model textures: " + ", ".join(sorted(missing)))
    for _, points, uv, texture, color, alpha in sorted(triangles, key=lambda t: t[0]):
        raster_triangle(points, uv, texture, color, alpha, pixels, depth)
    image = Image.fromarray(np.uint8(np.clip(pixels * 255, 0, 255)))
    if interior:
        draw = ImageDraw.Draw(image)
        for points in walls:
            projected = [((float(p[0]) + 5) * resolution / 10,
                          (5 - float(p[1])) * resolution / 10) for p in points]
            a, b = max(((a, b) for a in projected for b in projected),
                       key=lambda pair: sum((pair[0][i] - pair[1][i]) ** 2 for i in (0, 1)))
            draw.line((a, b), fill=(22, 23, 26), width=2)
    return image.resize((size, size), Image.Resampling.LANCZOS)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ascii", type=Path, required=True)
    parser.add_argument("--plan", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--game", type=Path, required=True)
    args = parser.parse_args()
    resources = Resources(ROOT / "SWLOR_Haks", ROOT / "Module/ifo/module.ifo.json", args.game)
    textures, manifest, failures = {}, [], []
    try:
        for row in json.loads(args.plan.read_text()):
            source = args.ascii / (row["model"] + ".mdl")
            try:
                image = render(source.read_text(encoding="latin1"), resources,
                               row["interior"], row["size"], textures, row.get("texture_aliases"))
                target = args.output / row["owner"] / (row["image"] + ".tga")
                target.parent.mkdir(parents=True, exist_ok=True)
                image.save(target)
                manifest.append(dict(row, ascii_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                                     sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
            except (ValueError, OSError) as error:
                failures.append(dict(row, error=str(error)))
        args.output.mkdir(parents=True, exist_ok=True)
        (args.output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
        (args.output / "failures.json").write_text(json.dumps(failures, indent=2) + "\n")
        print(json.dumps({"rendered": len(manifest), "failures": failures}, indent=2))
        raise SystemExit(bool(failures))
    finally:
        resources.close()


if __name__ == "__main__":
    main()
