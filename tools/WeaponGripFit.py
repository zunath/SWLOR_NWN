#!/usr/bin/env python3
"""Preview and check how held pistols sit in each playable hand.

Pistols are fitted to the male human default hand (`pmh0_handr001`). Other bodies carry the
weapon hook somewhere else in a hand of another size, so the same pistol sits too high, too
low or beside the fist. `WeaponHandFitCatalog.cs` holds one weapon-space move per body and
side, which every hand style of that body inherits because they share a wrist, plus moves for
single hand models that need their own. The server applies the move to the held weapon
(`WeaponHandFit`). Nothing is per weapon: weapons keep their fit to the reference hand.

This tool draws hand and weapon together from the compiled models, in weapon space, so a move
can be judged in seconds without the client. Each tile shows the side view, the view from
below, and two cuts across the handle (hand outline in brown, weapon in blue), once as the
game shows it now and once with the catalog move. A good move puts the handle inside the
fist's opening with the index finger under the trigger guard. Open-fingered gloves, mittens
and claws cannot close around a handle; for them the handle only has to lie in the palm.

Rifles are not covered. Their combat clips turn the weapon hook about 54 degrees from the idle
clip, so one fixed move cannot be right in both, and moving a rifle also pulls it off the
support hand that `GenerateRifleHoldAnimations.py` placed. A rifle correction belongs in those
per-body hold banks.

Commands (run from the repository root; needs numpy, scipy and Pillow, and the hak folders
`sw_pt_rhand sw_pt_lhand sw_weapon sw_pt_root`):

    python tools/WeaponGripFit.py sheet --output <folder>                 # default hand of every body
    python tools/WeaponGripFit.py sheet --body pfh0 --all --output <folder>   # every hand style of a body
    python tools/WeaponGripFit.py sheet --body pfh0 --move -2 -1.5 13.5 --output <folder>  # try a move (mm)
    python tools/WeaponGripFit.py check                                   # catalog names and limits

See SWLOR.Game.Server/Readmes/WeaponHandFit.md.
"""
from __future__ import annotations

import argparse
import hashlib
import math
import re
import struct
import sys
from functools import lru_cache
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
HAKS = ROOT / "SWLOR_Haks"
MODEL_FOLDERS = ("sw_pt_rhand", "sw_pt_lhand", "sw_weapon", "sw_pt_root")
CATALOG = ROOT / "SWLOR.Game.Server/Feature/AppearanceDefinition/ItemAppearance/WeaponHandFitCatalog.cs"
APPEARANCES = ROOT / "SWLOR.Game.Server/Feature/AppearanceDefinition/ItemAppearance"

REFERENCE_BODY = "pmh0"
# Playable normal-phenotype bodies: human, elf, dwarf, half-orc and droid frames, both genders.
BODIES = ("pmh0", "pfh0", "pme0", "pfe0", "pmd0", "pfd0", "pmo0", "pfo0", "pmz0", "pfz0")
HOLDS = {
    # hold: (weapon class, appearance definition). Pistol clips leave the weapon hook at rest.
    "pistol": ("wbwsh", "PistolAppearanceDefinition.cs"),
}

GRIP_REACH = 0.012        # weapon surface this close to the reference hand is "held"
MAX_MOVE = 0.06           # a larger move means the catalog entry is wrong, not that the hand is unusual


# ---------------------------------------------------------------- compiled models
@lru_cache(maxsize=None)
def model_index() -> dict[str, Path]:
    index: dict[str, Path] = {}
    for folder in MODEL_FOLDERS:
        directory = HAKS / folder
        if not directory.is_dir():
            raise RuntimeError(f"Missing hak folder {directory}; add it to the SWLOR_Haks sparse checkout")
        for path in directory.iterdir():
            if path.suffix.lower() == ".mdl":
                index[path.stem.lower()] = path
    return index


def quaternion_matrix(q) -> np.ndarray:
    x, y, z, w = q
    n = x * x + y * y + z * z + w * w
    if n < 1e-12:
        return np.eye(3)
    s = 2 / n
    return np.array([[1 - s * (y * y + z * z), s * (x * y - z * w), s * (x * z + y * w)],
                     [s * (x * y + z * w), 1 - s * (x * x + z * z), s * (y * z - x * w)],
                     [s * (x * z - y * w), s * (y * z + x * w), 1 - s * (x * x + y * y)]])


def transform(rotation=None, position=None) -> np.ndarray:
    matrix = np.eye(4)
    if rotation is not None:
        matrix[:3, :3] = rotation
    if position is not None:
        matrix[:3, 3] = position
    return matrix


class Model:
    """Geometry of a compiled MDL: node rest transforms and rendered triangles."""

    def __init__(self, data: bytes):
        if data[:4] != b"\0\0\0\0":
            raise ValueError("ASCII models are not supported; compile the model first")
        self.data = data
        self.raw = 12 + struct.unpack_from("<I", data, 4)[0]
        self.nodes: list[dict] = []
        self._walk(self._u32(12 + 0x48), None)

    def _u32(self, offset: int) -> int:
        return struct.unpack_from("<I", self.data, offset)[0]

    def _controllers(self, offset: int) -> tuple[tuple, tuple]:
        """First position and orientation key of a node header."""
        position, rotation = (0.0, 0.0, 0.0), (0.0, 0.0, 0.0, 1.0)
        keys, count, values = self._u32(offset + 0x54), self._u32(offset + 0x58), self._u32(offset + 0x60)
        for index in range(count):
            kind, rows, _, start, columns = struct.unpack_from("<IHHHB", self.data, 12 + keys + index * 12)
            if kind == 8 and rows:
                position = struct.unpack_from("<3f", self.data, 12 + values + start * 4)
            if kind == 20 and rows and columns == 4:
                rotation = struct.unpack_from("<4f", self.data, 12 + values + start * 4)
        return position, rotation

    def _walk(self, pointer: int, parent: int | None) -> None:
        offset, data = 12 + pointer, self.data
        name = data[offset + 32:offset + 64].split(b"\0", 1)[0].decode("latin1").lower()
        position, rotation = self._controllers(offset)
        local = transform(quaternion_matrix(rotation), position)
        world = local if parent is None else self.nodes[parent]["world"] @ local
        node = dict(name=name, parent=parent, local=local, world=world, vertices=None, faces=None)
        is_mesh = self._u32(offset + 0x6C) & 0x20
        if is_mesh and self._u32(offset + 0xDC):  # rendered trimesh; shadow and walk meshes are skipped
            face_pointer, face_count = self._u32(offset + 0x78), self._u32(offset + 0x7C)
            vertex_count = struct.unpack_from("<H", data, offset + 0x230)[0]
            vertex_pointer = self._u32(offset + 0x22C)
            if vertex_count and face_count and vertex_pointer != 0xFFFFFFFF:
                vertices = np.frombuffer(data, "<f4", 3 * vertex_count, self.raw + vertex_pointer)
                vertices = vertices.reshape(-1, 3).astype(float)
                faces = np.array([struct.unpack_from("<3H", data, 12 + face_pointer + 32 * i + 26)
                                  for i in range(face_count)])
                node["vertices"] = (world[:3, :3] @ vertices.T).T + world[:3, 3]
                node["faces"] = faces
        index = len(self.nodes)
        self.nodes.append(node)
        children, child_count = self._u32(offset + 0x48), self._u32(offset + 0x4C)
        for child in range(child_count):
            self._walk(self._u32(12 + children + 4 * child), index)

    def node(self, name: str) -> dict:
        for node in self.nodes:
            if node["name"] == name:
                return node
        raise KeyError(name)

    def mesh(self) -> tuple[np.ndarray, np.ndarray]:
        vertices, faces, base = [], [], 0
        for node in self.nodes:
            if node["vertices"] is None:
                continue
            vertices.append(node["vertices"])
            faces.append(node["faces"] + base)
            base += len(node["vertices"])
        if not vertices:
            raise ValueError("model has no rendered mesh")
        return np.vstack(vertices), np.vstack(faces)


@lru_cache(maxsize=64)
def load(name: str) -> Model:
    path = model_index().get(name.lower())
    if path is None:
        raise FileNotFoundError(f"model {name} is not in {', '.join(MODEL_FOLDERS)}")
    return Model(path.read_bytes())


# ---------------------------------------------------------------- hands and weapons in weapon space
@lru_cache(maxsize=None)
def hook(body: str, side: str, hold: str) -> np.ndarray:
    """The weapon hook's rest transform in the hand's frame."""
    skeleton = load(body)
    name = f"{side}hand"
    return np.linalg.inv(skeleton.node(name + "_g")["world"]) @ skeleton.node(name)["world"]


def hand_mesh(hand: str, hold: str) -> tuple[np.ndarray, np.ndarray]:
    vertices, faces = load(hand).mesh()
    side = hand[9]
    inverse = np.linalg.inv(hook(hand[:4], side, hold))
    return (inverse[:3, :3] @ vertices.T).T + inverse[:3, 3], faces


def weapon_mesh(name: str) -> tuple[np.ndarray, np.ndarray]:
    return load(name).mesh()


def hands(body: str, side: str) -> list[str]:
    pattern = re.compile(rf"{body}_hand{side}\d{{3}}$")
    return sorted(name for name in model_index() if pattern.match(name))


@lru_cache(maxsize=None)
def gauge_weapons(hold: str) -> tuple[str, ...]:
    """Native model names of every catalog appearance of this hold (editor id = color * 100 + model)."""
    prefix, definition = HOLDS[hold]
    text = (APPEARANCES / definition).read_text(encoding="utf-8")
    block = re.search(r"MiddleParts[^{]*\{(.*?)\};", text, re.S)[1]
    block = re.sub(r"//[^\n]*", "", block)
    names = []
    for value in re.findall(r"\d+", block):
        color, model = divmod(int(value), 100)
        name = f"{prefix}_m_{model:02d}{color}"
        if name in model_index():
            names.append(name)
    return tuple(names)


def surface_samples(vertices: np.ndarray, faces: np.ndarray, spacing: float) -> np.ndarray:
    triangles = vertices[faces]
    a, b, c = triangles[:, 0], triangles[:, 1], triangles[:, 2]
    area = 0.5 * np.linalg.norm(np.cross(b - a, c - a), axis=1)
    points = [a, b, c]
    for index in range(len(triangles)):
        steps = int(math.ceil(math.sqrt(2 * area[index]) / spacing))
        if steps < 2:
            points.append(((a[index] + b[index] + c[index]) / 3)[None])
            continue
        u, w = np.meshgrid(np.arange(steps + 1), np.arange(steps + 1))
        keep = u + w <= steps
        u, w = u[keep] / steps, w[keep] / steps
        points.append(a[index] + u[:, None] * (b[index] - a[index]) + w[:, None] * (c[index] - a[index]))
    return np.vstack(points)


@lru_cache(maxsize=None)
def handle_axis(hold: str) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
    """(point, along, across-u, across-v): the mean line of the gauge handles in the reference hand."""
    from scipy.spatial import cKDTree
    reference = cKDTree(surface_samples(*hand_mesh(f"{REFERENCE_BODY}_handr001", hold), 0.002))
    held = []
    for weapon in gauge_weapons(hold):
        try:
            points = surface_samples(*weapon_mesh(weapon), 0.004)
        except ValueError:
            continue  # an uncompiled or empty model cannot define the standard
        points = points[reference.query(points)[0] < GRIP_REACH]
        if len(points) >= 30:
            held.append(points)
    if len(held) < 5:
        raise RuntimeError(f"too few {hold} models reach the reference hand to define a handle axis")
    centers = np.array([points.mean(0) for points in held])
    point = np.median(centers, axis=0)
    cloud = np.vstack([points - points.mean(0) for points in held])
    along = np.linalg.svd(cloud, full_matrices=False)[2][0]
    if along[1] < 0:
        along = -along
    u = np.cross(along, [0.0, 0.0, 1.0])
    u /= np.linalg.norm(u)
    return point, along, u, np.cross(along, u)


# ---------------------------------------------------------------- catalog
ENTRY = re.compile(r'\["(\w+)"\] = new\(([-\d.]+)f, ([-\d.]+)f, ([-\d.]+)f\)')


def read_catalog() -> tuple[dict[str, np.ndarray], dict[str, np.ndarray]]:
    """(moves per body and side keyed like `pfh0_handr`, moves per hand model), in metres."""
    text = CATALOG.read_text(encoding="utf-8")
    tables = {}
    for name, block in re.findall(r"FitVector> (\w+) = new[^{]*\{(.*?)\n    \};", text, re.S):
        tables[name] = {key.lower(): np.array([float(x), float(y), float(z)])
                        for key, x, y, z in ENTRY.findall(block)}
    return tables.get("Bodies", {}), tables.get("Hands", {})


def catalog_move(hand: str) -> np.ndarray:
    bodies, single = read_catalog()
    hand = hand.lower()
    return single.get(hand, bodies.get(hand[:10], np.zeros(3)))


def geometry_key(hand: str) -> str:
    vertices, faces = load(hand).mesh()
    return hashlib.sha1(np.round(vertices, 5).tobytes() + faces.tobytes()).hexdigest()


# ---------------------------------------------------------------- review sheets
VIEWS = {  # weapon space: barrel -Z, grip -Y. (right, up, toward camera)
    "side": ((0, 0, 1), (0, 1, 0), (-1, 0, 0)),
    "below": ((0, 0, -1), (1, 0, 0), (0, -1, 0)),
}
LIGHT = np.array([0.4, -0.5, 0.75]) / np.linalg.norm([0.4, -0.5, 0.75])
SKIN, GUN = (214, 160, 120), (90, 110, 150)


def raster(meshes, axes, size: int, center, span: float):
    from PIL import Image
    right, up, depth = (np.array(axis, float) for axis in axes)
    image = np.full((size, size, 3), 245, np.uint8)
    buffer = np.full((size, size), -1e9)
    for vertices, faces, color in meshes:
        p = vertices - np.array(center)
        x = (p @ right) / span * size + size / 2
        y = size / 2 - (p @ up) / span * size
        z = p @ depth
        triangles = vertices[faces]
        normals = np.cross(triangles[:, 1] - triangles[:, 0], triangles[:, 2] - triangles[:, 0])
        length = np.linalg.norm(normals, axis=1)
        length[length == 0] = 1
        shade = 0.35 + 0.65 * np.abs((normals / length[:, None]) @ LIGHT)
        for index, corner in enumerate(faces):
            xs, ys, zs = x[corner], y[corner], z[corner]
            x0, x1 = int(max(0, math.floor(xs.min()))), int(min(size - 1, math.ceil(xs.max())))
            y0, y1 = int(max(0, math.floor(ys.min()))), int(min(size - 1, math.ceil(ys.max())))
            denominator = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
            if x0 > x1 or y0 > y1 or abs(denominator) < 1e-12:
                continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            w0 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / denominator
            w1 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / denominator
            w2 = 1 - w0 - w1
            depth_here = w0 * zs[0] + w1 * zs[1] + w2 * zs[2]
            window = buffer[y0:y1 + 1, x0:x1 + 1]
            visible = (w0 >= 0) & (w1 >= 0) & (w2 >= 0) & (depth_here > window)
            window[visible] = depth_here[visible]
            image[y0:y1 + 1, x0:x1 + 1][visible] = np.clip(np.array(color) * shade[index], 0, 255)
    return Image.fromarray(image)


def cut(vertices: np.ndarray, faces: np.ndarray, origin: np.ndarray, normal: np.ndarray) -> list:
    """Segments where a mesh crosses a plane."""
    distance = (vertices - origin) @ normal
    segments = []
    for corner in faces:
        points = []
        for i, j in ((0, 1), (1, 2), (2, 0)):
            di, dj = distance[corner[i]], distance[corner[j]]
            if (di > 0) != (dj > 0):
                points.append(vertices[corner[i]] + di / (di - dj) * (vertices[corner[j]] - vertices[corner[i]]))
        if len(points) == 2:
            segments.append(points)
    return segments


def cut_image(meshes, hold: str, level: float, size: int, span: float = 0.2):
    """A cut across the handle axis: hand outline in brown, weapon outline in blue."""
    from PIL import Image, ImageDraw
    point, along, u, v = handle_axis(hold)
    origin = point + level * along
    image = Image.new("RGB", (size, size), (245, 245, 245))
    draw = ImageDraw.Draw(image)
    for vertices, faces, color in meshes:
        for start, end in cut(vertices, faces, origin, along):
            line = [(size / 2 + ((q - origin) @ u) / span * size, size / 2 - ((q - origin) @ v) / span * size)
                    for q in (start, end)]
            draw.line(line, fill=color, width=2)
    draw.text((3, size - 12), f"cut {level * 1000:+.0f} mm", fill="black")
    return image


def tile(hand: str, hold: str, weapon: str, move, label: str, size: int):
    from PIL import Image, ImageDraw
    hand_vertices, hand_faces = hand_mesh(hand, hold)
    weapon_vertices, weapon_faces = weapon_mesh(weapon)
    point = handle_axis(hold)[0]
    weapon_vertices = weapon_vertices + np.array(move)
    meshes = [(hand_vertices, hand_faces, SKIN), (weapon_vertices, weapon_faces, GUN)]
    lines = [(hand_vertices, hand_faces, (170, 80, 30)), (weapon_vertices, weapon_faces, (40, 70, 200))]
    images = [raster(meshes, axes, size, point, 0.34) for axes in VIEWS.values()]
    images += [cut_image(lines, hold, level, size) for level in (0.012, -0.012)]
    out = Image.new("RGB", (size * len(images), size + 13), "white")
    for index, image in enumerate(images):
        out.paste(image, (index * size, 13))
    ImageDraw.Draw(out).text((3, 1), label, fill="black")
    return out


def sheet_weapon(hold: str) -> str:
    """A mid-catalog weapon of this hold that has a compiled model."""
    names = gauge_weapons(hold)
    for name in names[len(names) // 2:] + names[:len(names) // 2]:
        try:
            weapon_mesh(name)
            return name
        except ValueError:
            continue
    raise RuntimeError(f"no compiled {hold} model")


def write_sheets(bodies, sides, every_style: bool, trial, output: Path, weapon: str | None = None,
                 size: int = 180, rows_per_sheet: int = 5) -> list[Path]:
    """Now-and-moved tiles. `trial` replaces the catalog move (metres) to try a value before saving it."""
    from PIL import Image
    hold = "pistol"
    weapon = weapon or sheet_weapon(hold)
    output.mkdir(parents=True, exist_ok=True)
    written = []
    for body in bodies:
        for side in sides:
            names = hands(body, side) if every_style else [f"{body}_hand{side}001"]
            seen, rows = set(), []
            for hand in names:
                try:
                    key = geometry_key(hand)
                except ValueError:
                    continue  # a hand part with no rendered mesh shows no hand to fit
                move = catalog_move(hand) if trial is None else np.array(trial)
                if (key, tuple(move)) in seen:
                    continue
                seen.add((key, tuple(move)))
                millimetres = [round(float(value) * 1000, 1) for value in move]
                before = tile(hand, hold, weapon, (0, 0, 0), f"{hand} + {weapon}: now", size)
                after = tile(hand, hold, weapon, move, f"moved {millimetres} mm", size)
                row = Image.new("RGB", (before.width + after.width + 8, before.height), "white")
                row.paste(before, (0, 0))
                row.paste(after, (before.width + 8, 0))
                rows.append(row)
            for page in range(0, len(rows), rows_per_sheet):
                group = rows[page:page + rows_per_sheet]
                sheet = Image.new("RGB", (group[0].width, sum(item.height for item in group)), "white")
                top = 0
                for item in group:
                    sheet.paste(item, (0, top))
                    top += item.height
                path = output / f"{hold}-{body}-{side}-{page // rows_per_sheet + 1}.png"
                sheet.save(path)
                written.append(path)
    return written


# ---------------------------------------------------------------- commands
def check() -> list[str]:
    """Problems with the committed catalog."""
    problems = []
    bodies, single = read_catalog()
    known = {f"{body}_hand{side}" for body in BODIES for side in "rl"}
    for key, move in bodies.items():
        if key not in known:
            problems.append(f"{key}: not a playable body and side")
        if np.linalg.norm(move) > MAX_MOVE:
            problems.append(f"{key}: {np.linalg.norm(move) * 1000:.0f} mm exceeds {MAX_MOVE * 1000:.0f} mm")
    for hand, move in single.items():
        if hand not in model_index():
            problems.append(f"{hand}: the catalog names a hand model that is not in the haks")
        elif hand[:10] not in known:
            problems.append(f"{hand}: not a hand of a playable body")
        if np.linalg.norm(move) > MAX_MOVE:
            problems.append(f"{hand}: {np.linalg.norm(move) * 1000:.0f} mm exceeds {MAX_MOVE * 1000:.0f} mm")
        if hand[:10] in bodies and np.allclose(move, bodies[hand[:10]]):
            problems.append(f"{hand}: repeats its body's move; delete the entry")
    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=("sheet", "check"))
    parser.add_argument("--body", action="append", choices=BODIES)
    parser.add_argument("--side", action="append", choices=("r", "l"))
    parser.add_argument("--all", action="store_true", help="every hand style, not only the default hand")
    parser.add_argument("--weapon", help="native pistol model to draw, such as wbwsh_m_013")
    parser.add_argument("--move", type=float, nargs=3, metavar=("X", "Y", "Z"),
                        help="draw this move in millimetres instead of the catalog's")
    parser.add_argument("--output", type=Path, help="folder for review sheets")
    args = parser.parse_args()

    if args.command == "check":
        problems = check()
        print("\n".join(problems) if problems else "Weapon hand fit check passed.")
        return 1 if problems else 0

    if not args.output:
        parser.error("sheet needs --output")
    trial = None if args.move is None else [value / 1000 for value in args.move]
    for path in write_sheets(args.body or BODIES, args.side or ("r", "l"), args.all, trial, args.output, args.weapon):
        print(path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
