#!/usr/bin/env python3
"""Derive, preview and check how held pistols and rifles sit in each playable hand.

Weapons are fitted to the male human body (`pmh0`). Other bodies carry the weapon hook in a
hand of another size and place, so the same weapon sits too high, too low or beside the
hand. `WeaponHandFitCatalog.cs` holds one weapon-space move per body and side plus moves for
single hand models, and the server applies the move to the held weapon (`WeaponHandFit`).
Nothing is per weapon.

The standard is the same hand style on the male human. Almost every hand model on another
body is his model scaled and shifted, so `derive` finds that scale and shift and carries the
point where the back of the handle rests against the hand across it. Hands with no model on
the male human (Cathar, Wookiee and droid hands, a few remodelled gloves) are judged by eye
on review sheets: each tile shows the side view, the view from below, and two cuts across
the handle (hand outline in brown, weapon in blue), without and with the move.

A rifle takes the midpoint of what its idle and ready clips need (`rifle_move`), and the
rifle hold banks in SWLOR_Haks place the support hand for that move; `check` compares the
two tables.

Commands (run from the repository root; needs numpy, scipy and Pillow, and the hak folders
`sw_pt_rhand sw_pt_lhand sw_weapon sw_pt_root tools`):

    python tools/WeaponGripFit.py derive --body pfd0                      # derived catalog lines
    python tools/WeaponGripFit.py check                                   # names, limits, rifle banks
    python tools/WeaponGripFit.py check --derived                         # also every derived move (minutes)
    python tools/WeaponGripFit.py sheet --output <folder>                 # default hand of every body
    python tools/WeaponGripFit.py sheet --hand pfd0_handr209 --move 15 0 0 --output <folder>  # try a move (mm)
    python tools/WeaponGripFit.py weapons --class wbwxl --output <folder>  # every rifle model in the reference hand

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
MAX_MOVE = 0.07           # a larger move means the catalog entry is wrong, not that the hand is unusual
# Where the back of a pistol handle rests against the heel of the reference hand, toward the palm. A hand
# style on another body is the reference body's model scaled and shifted; the handle follows this point.
GRIP_ANCHOR = (0.008, -0.010, 0.020)
DERIVED_TOLERANCE = 0.005  # the catalog may differ from a derived move by this much
DERIVED_ENTRY = 0.003      # a derived hand this far from its body's move gets its own entry
REGISTERED_MISS = 0.004    # a looser surface match than this is another shape; that hand is judged by eye
REGISTERED_SCALE = (0.5, 2.0)  # a match outside this size range is a stub or a different part
# The rifle ready and attack clips turn the weapon hook against the hand (x y z w); the idle clip leaves it
# at rest. The female dwarf and half-orc frames inherit clips that turn it less. `WeaponHandFit.cs` holds
# the same numbers.
RIFLE_READY_TURN = (0.4155, -0.1652, 0.0770, 0.8911)
RIFLE_READY_TURN_SHORT_FRAMES = (0.3120, -0.1728, 0.0578, 0.9324)
RIFLE_BANKS = HAKS / "tools/GenerateRifleHoldAnimations.py"


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


@lru_cache(maxsize=1)
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


def rifle_move(move, hand: str) -> np.ndarray:
    """The one move a rifle takes in a hand whose pistol move is `move`: halfway between what the idle
    clip and the turned ready clip need (`WeaponHandFit.RifleMove`)."""
    move = np.asarray(move, float)
    turn = RIFLE_READY_TURN_SHORT_FRAMES if hand[:4].lower() in ("pfd0", "pfo0") else RIFLE_READY_TURN
    return (move + quaternion_matrix(turn).T @ move) / 2


def rifle_bank_moves() -> dict[str, np.ndarray] | None:
    """The per-body rifle moves the rifle hold banks were solved for, or None when the generator is absent."""
    if not RIFLE_BANKS.is_file():
        return None
    block = re.search(r"^WEAPON_MOVES = \{(.*?)^\}", RIFLE_BANKS.read_text(encoding="utf-8"), re.S | re.M)
    if block is None:
        return {}
    return {body: np.array([float(x), float(y), float(z)])
            for body, x, y, z in re.findall(r'"(\w{4})": \(([-\d.e]+), ([-\d.e]+), ([-\d.e]+)\)', block[1])}


def check_rifle_banks() -> list[str]:
    """The rifle hold banks place the support hand for one rifle move per body; it must be the catalog's."""
    recorded = rifle_bank_moves()
    if recorded is None:
        return []
    problems = []
    bodies, _ = read_catalog()
    for body in BODIES:
        expected = rifle_move(bodies.get(f"{body}_handr", np.zeros(3)), body)
        found = recorded.get(body, np.zeros(3))
        if np.abs(expected - found).max() > 0.0015:
            problems.append(f"{body}: the rifle hold bank is solved for {np.round(found * 1000, 1)} mm but the catalog "
                            f"gives {np.round(expected * 1000, 1)} mm; update WEAPON_MOVES in {RIFLE_BANKS.name} "
                            "and regenerate the banks")
    return problems


# ---------------------------------------------------------------- derived moves
def anchor(side: str) -> np.ndarray:
    point = np.array(GRIP_ANCHOR)
    if side == "l":
        point[0] = -point[0]
    return point


@lru_cache(maxsize=None)
def exact_map(source: str, target: str):
    """The affine map between two models that share their vertices, as a 4x3 matrix, or None."""
    try:
        a, b = hand_mesh(target, "pistol")[0], hand_mesh(source, "pistol")[0]
    except (ValueError, FileNotFoundError):
        return None
    if len(a) != len(b):
        return None
    design = np.hstack([b, np.ones((len(b), 1))])
    solution = np.linalg.lstsq(design, a, rcond=None)[0]
    return solution if np.abs(design @ solution - a).max() < 0.001 else None


def registered_map(source: str, target: str):
    """A scale and shift laying one model's surface over another's when their vertices differ, or None."""
    result, miss = registered_fit(source, target)
    if result is None or miss > REGISTERED_MISS or not REGISTERED_SCALE[0] <= result[0, 0] <= REGISTERED_SCALE[1]:
        return None
    return result


@lru_cache(maxsize=None)
def registered_fit(source: str, target: str, keep: float = 0.85, rounds: int = 40):
    """(map, mean miss in metres) of the best scale and shift between two models' surfaces, or (None, inf)."""
    from scipy.spatial import cKDTree
    try:
        a = surface_samples(*hand_mesh(target, "pistol"), 0.003)
        b = surface_samples(*hand_mesh(source, "pistol"), 0.003)
    except (ValueError, FileNotFoundError):
        return None, math.inf
    low_a, high_a = np.percentile(a, 2, axis=0), np.percentile(a, 98, axis=0)
    low_b, high_b = np.percentile(b, 2, axis=0), np.percentile(b, 98, axis=0)
    if np.any(high_b - low_b < 1e-6) or np.any(high_a - low_a < 1e-6):
        return None, math.inf
    scale = float(np.cbrt(np.prod((high_a - low_a) / (high_b - low_b))))
    shift = (low_a + high_a) / 2 - scale * (low_b + high_b) / 2
    tree, miss = cKDTree(a), math.inf
    for _ in range(rounds):
        moved = b * scale + shift
        # Matched in both directions so neither surface can shrink into a corner of the other.
        there, index_there = tree.query(moved)
        back, index_back = cKDTree(moved).query(a)
        keep_there, keep_back = there <= np.quantile(there, keep), back <= np.quantile(back, keep)
        source_points = np.vstack([b[keep_there], b[index_back[keep_back]]])
        target_points = np.vstack([a[index_there[keep_there]], a[keep_back]])
        centered = source_points - source_points.mean(0)
        previous = shift
        scale = float((centered * (target_points - target_points.mean(0))).sum() / (centered ** 2).sum())
        shift = target_points.mean(0) - scale * source_points.mean(0)
        miss = (there[keep_there].mean() + back[keep_back].mean()) / 2
        if np.abs(shift - previous).max() < 2e-5:
            break
    return np.vstack([np.eye(3) * scale, shift]), miss


@lru_cache(maxsize=None)
def derived_move(hand: str) -> tuple[np.ndarray | None, str]:
    """(move in metres, how it was found). A hand that cannot be tied to the reference body gives (None, "by eye").

    The same hand style on the reference body is the standard: wherever its handle rests, the handle rests on
    this model. A reference hand normally has no move; the few that hold the handle badly themselves carry a
    reviewed catalog entry, and every model tied to them inherits it. A female or non-human model is tied to
    the reference through its own human-frame model when it cannot be tied directly; a human-frame model with
    its own catalog entry, or one judged by eye, passes its catalog move along.
    """
    hand = hand.lower()
    body, side, style = hand[:4], hand[9], hand[-3:]
    point = anchor(side)
    if body == REFERENCE_BODY:
        return catalog_move(hand), "reference"
    reference = f"{REFERENCE_BODY}_hand{side}{style}"
    human = f"{body[:2]}h0_hand{side}{style}"
    for how, find in (("exact", exact_map), ("registered", registered_map)):
        direct = find(reference, hand) if reference in model_index() else None
        if direct is not None:
            return np.append(point + catalog_move(reference), 1) @ direct - point, how
        if human != hand and human in model_index():
            through = find(human, hand)
            if through is not None:
                start, _ = derived_move(human)
                if start is None or human in read_catalog()[1]:
                    start = catalog_move(human)
                return np.append(point + start, 1) @ through - point, f"{how} through {human[:4]}"
    return None, "by eye"


def derive(bodies, sides) -> list[str]:
    """Catalog lines for the derived moves: one per body and side, then the hands that differ from it."""
    lines = []
    for body in bodies:
        for side in sides:
            found, by_eye = {}, []
            for hand in hands(body, side):
                try:
                    move, how = derived_move(hand)
                except ValueError:
                    continue
                if move is None:
                    by_eye.append(hand)
                else:
                    found[hand] = (move, how)
            if not found:
                lines.append(f"// {body} {side}: no hand can be derived; all {len(by_eye)} are judged by eye")
                continue
            family = np.round(np.median([move for move, _ in found.values()], axis=0), 3)
            lines.append(f'["{body}_hand{side}"] = {entry(family)},  // {len(found)} derived hands')
            for hand, (move, how) in found.items():
                if np.linalg.norm(move - family) > DERIVED_ENTRY:
                    lines.append(f'    ["{hand}"] = {entry(np.round(move, 3))},  // {how}')
            lines.append(f"    // by eye: {' '.join(name[-3:] for name in by_eye) or 'none'}")
    return lines


def entry(move) -> str:
    return "new(" + ", ".join(f"{0.0 if abs(value) < 5e-4 else value:.3f}".rstrip("0").rstrip(".") + "f"
                              for value in move) + ")"


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
                 size: int = 180, rows_per_sheet: int = 5, only: set[str] | None = None) -> list[Path]:
    """Now-and-moved tiles. `trial` replaces the catalog move (metres) to try a value before saving it."""
    from PIL import Image
    hold = "pistol"
    weapon = weapon or sheet_weapon(hold)
    output.mkdir(parents=True, exist_ok=True)
    written = []
    for body in bodies:
        for side in sides:
            names = hands(body, side) if every_style else [f"{body}_hand{side}001"]
            if only is not None:
                names = [name for name in names if name in only]
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


def write_weapon_sheets(weapon_class: str, output: Path, size: int = 180, per_sheet: int = 16) -> tuple[list[Path], list[str]]:
    """Every middle-part model of a weapon class in the reference hand; returns the sheets and the models skipped.

    The hand moves assume a weapon is fitted to the reference hand, so a weapon whose grip is not in that fist
    here sits wrong on every body. Models stored uncompiled cannot be read and are listed instead.
    """
    from PIL import Image
    output.mkdir(parents=True, exist_ok=True)
    names = sorted(name for name in model_index() if re.fullmatch(rf"{weapon_class}_m_\d+", name))
    tiles, skipped = [], []
    for name in names:
        try:
            tiles.append(tile(f"{REFERENCE_BODY}_handr001", "pistol", name, (0, 0, 0), name, size))
        except ValueError:
            skipped.append(name)
    written = []
    for page in range(0, len(tiles), per_sheet):
        group = tiles[page:page + per_sheet]
        width, height = group[0].width, group[0].height
        sheet = Image.new("RGB", (width * 2 + 8, height * ((len(group) + 1) // 2)), "white")
        for index, item in enumerate(group):
            sheet.paste(item, ((index % 2) * (width + 8), (index // 2) * height))
        path = output / f"{weapon_class}-{page // per_sheet + 1}.png"
        sheet.save(path)
        written.append(path)
    return written, skipped


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


def check_derived(bodies=BODIES, sides=("r", "l")) -> list[str]:
    """Hands whose catalog move is not the move derived from the reference body. Needs the hak models."""
    problems = []
    for body in bodies:
        for side in sides:
            for hand in hands(body, side):
                try:
                    move, how = derived_move(hand)
                except ValueError:
                    continue  # no rendered mesh, so no hand to fit
                if move is None:
                    continue
                if how.startswith("registered") and hand in read_catalog()[1]:
                    continue  # a surface match is an estimate; an entry reviewed in the client outranks it
                gap = np.linalg.norm(catalog_move(hand) - move)
                if gap > DERIVED_TOLERANCE:
                    problems.append(f"{hand}: the catalog is {gap * 1000:.0f} mm from the derived move "
                                    f"{entry(np.round(move, 3))} ({how})")
    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("command", choices=("sheet", "weapons", "derive", "check"))
    parser.add_argument("--body", action="append", choices=BODIES)
    parser.add_argument("--side", action="append", choices=("r", "l"))
    parser.add_argument("--all", action="store_true", help="every hand style, not only the default hand")
    parser.add_argument("--hand", action="append", help="draw only this hand model, such as pfd0_handr209")
    parser.add_argument("--derived", action="store_true",
                        help="check: also compare every hand with its derived move (slow, needs the hak models)")
    parser.add_argument("--weapon", help="native pistol model to draw, such as wbwsh_m_013")
    parser.add_argument("--class", dest="weapon_class", choices=("wbwsh", "wbwxl", "wbwxh"), default="wbwsh",
                        help="weapons: pistols (wbwsh), rifles (wbwxl) or cannons (wbwxh)")
    parser.add_argument("--move", type=float, nargs=3, metavar=("X", "Y", "Z"),
                        help="draw this move in millimetres instead of the catalog's")
    parser.add_argument("--output", type=Path, help="folder for review sheets")
    args = parser.parse_args()

    if args.command == "check":
        problems = check() + check_rifle_banks()
        if args.derived:
            problems += check_derived(args.body or BODIES, args.side or ("r", "l"))
        print("\n".join(problems) if problems else "Weapon hand fit check passed.")
        if rifle_bank_moves() is None:
            print(f"Not checked: the rifle hold banks' move table ({RIFLE_BANKS.relative_to(ROOT).as_posix()} is not "
                  "checked out; add `tools` to the SWLOR_Haks sparse checkout).")
        return 1 if problems else 0

    if args.command == "derive":
        for line in derive(args.body or BODIES, args.side or ("r", "l")):
            print(line)
        return 0

    if not args.output:
        parser.error(f"{args.command} needs --output")
    if args.command == "weapons":
        written, skipped = write_weapon_sheets(args.weapon_class, args.output)
        for path in written:
            print(path)
        if skipped:
            print("not compiled, not drawn:", " ".join(skipped))
        return 0
    trial = None if args.move is None else [value / 1000 for value in args.move]
    only = {name.lower() for name in args.hand} if args.hand else None
    bodies = args.body or (sorted({name[:4] for name in only}) if only else BODIES)
    for path in write_sheets(bodies, args.side or ("r", "l"), args.all or bool(only), trial, args.output, args.weapon,
                             only=only):
        print(path)
    return 0


if __name__ == "__main__":
    sys.exit(main())
