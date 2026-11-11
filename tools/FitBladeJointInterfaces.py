"""Remove the stray joint sleeves from compiled vibroblade/electroblade part models.

`MeshyWeaponParts.py` closes every cut of a fitted longsword part with a flat cap
and adds an elliptical sleeve (48 x 72 mm, 7 mm deep) centred on the stock weapon
axis. The fitted parts sit 20 mm off that axis and are mostly much slimmer, so on
every fitted part the sleeves read as metal bars sticking out of one side at each
joint: a middle shows one at its top and one at its bottom, a top or bottom one
where it meets the middle. The flat caps already close each cut, so the sleeves add
nothing but the bars.

This edits only the interface trimesh of each compiled part, in place of a decompile
round trip (which would average the authored tangent seams of the source surfaces):
sleeves that reach past their cut are dropped and the cut caps stay exactly as
authored. A few bottoms stop 3.5 mm short of the joint and have no cut there; their
sleeve is the only thing closing that slot, so it is rebuilt as a collar of the
grip's own end outline instead of being removed. Source trimeshes stay byte-identical.
"""
import argparse
import json
from pathlib import Path
import re
import struct
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import MeshyWeaponParts
import NativeTangentSeams

# Native vibroblade roles: the middle spans Y 0.085 to 0.135, the bottom lies below
# it and the top above. Each joint is (y, side), side +1 when the part lies above it.
JOINTS = {
    "t": ((0.135, 1),),
    "m": ((0.085, 1), (0.135, -1)),
    "b": ((0.085, -1),),
}
SLEEVE_DEPTH = 0.0075  # Authored interface geometry ends within this depth of a joint.
COLLAR_SAMPLE = 0.002  # A short grip's end cross-section is sampled over this length.
FIT_TOLERANCE = 0.0005
CAP_COVERAGE = 0.98  # Share of the cut outline a cap must fill.
PLANE = 1e-5
POSITION, UV, NORMAL, TANGENT, SIGN = 444, 452, 468, 488, 496
MODEL = re.compile(r"wswls_([tmb])_\d{3}")


def role_of(name):
    match = MODEL.fullmatch(name)
    if not match:
        raise ValueError(f"Not a vibroblade part model: {name}")
    return match[1]


def _distance(verts, joint):
    y, side = joint
    return side * (verts[:, 1] - y)


def _keys(positions):
    data = np.ascontiguousarray(positions, dtype="<f4").tobytes()
    return [data[i:i + 12] for i in range(0, len(data), 12)]


class _Union:
    def __init__(self, count):
        self.parent = list(range(count))

    def find(self, a):
        while self.parent[a] != a:
            self.parent[a] = self.parent[self.parent[a]]
            a = self.parent[a]
        return a

    def join(self, a, b):
        self.parent[self.find(b)] = self.find(a)


def components(positions, faces):
    """Component label per face; faces touching at the same position are connected."""
    union, first = _Union(len(positions)), {}
    for index, key in enumerate(_keys(positions)):
        if key in first:
            union.join(first[key], index)
        else:
            first[key] = index
    for a, b, c in faces:
        union.join(a, b)
        union.join(a, c)
    return np.array([union.find(int(a)) for a, _, _ in faces], dtype=int)


def _joint_of(ys, role):
    """The joint whose plane holds every given Y, or None."""
    for joint in JOINTS[role]:
        if np.all(np.abs(ys - joint[0]) < PLANE):
            return joint
    return None


def classify(positions, faces, role):
    """Per-face kind for an interface trimesh: ('cap', joint), ('sleeve', joint) or None.

    Cut caps are flat fans in a joint plane; sleeves and collars are closed tubes
    reaching into the part from one joint.
    """
    labels = components(positions, faces)
    kinds = [None] * len(faces)
    for label in np.unique(labels):
        members = np.flatnonzero(labels == label)
        verts = positions[np.unique(faces[members])]
        joint = _joint_of(verts[:, 1], role)
        if joint is not None:
            kind = ("cap", joint)
        else:
            # A sleeve reaches from its joint plane into the part.
            near = [j for j in JOINTS[role]
                    if np.all((_distance(verts, j) > -PLANE) & (_distance(verts, j) < SLEEVE_DEPTH))
                    and np.abs(_distance(verts, j)).min() < PLANE and np.ptp(verts[:, 1]) > PLANE]
            if len(near) != 1:
                return None
            kind = ("tube", near[0])
        for face in members:
            kinds[face] = kind
    return kinds


def interface_index(meshes, role):
    """Index of the authored interface trimesh (caps and sleeves only), or None."""
    found = [i for i, mesh in enumerate(meshes)
             if classify(mesh["streams"][POSITION].astype(float), mesh["faces"].astype(int), role) is not None]
    if len(found) > 1:
        raise ValueError("Expected one authored interface trimesh")
    if found and len(meshes) == 1:
        raise ValueError("Expected source trimeshes beside the authored interface")
    return found[0] if found else None


def rim_points(sources, joint):
    """(x, z) points where the source surfaces are cut by a joint plane."""
    points = []
    for mesh in sources:
        positions = mesh["streams"][POSITION].astype(float)
        faces = mesh["faces"].astype(int)
        on = np.abs(positions[:, 1] - joint[0]) < PLANE
        used = np.unique(faces[on[faces].sum(axis=1) >= 2])
        points.extend(positions[used[on[used]]][:, [0, 2]])
    return np.asarray(points).reshape(-1, 2)


def _hull_contains(hull, points, tolerance=FIT_TOLERANCE):
    """True when every (x, z) point lies inside the convex polygon `hull`, within tolerance."""
    return MeshyWeaponParts.inside_convex(hull, points, tolerance)


def _area(polygon):
    polygon = np.asarray(polygon, dtype=float)
    following = np.roll(polygon, -1, axis=0)
    return abs(float(np.sum(polygon[:, 0] * following[:, 1] - following[:, 0] * polygon[:, 1]))) / 2


def _end_section(sources, joint):
    """Convex outline (x, z) of the source surfaces' end nearest a joint they stop short of,
    and that end's distance from the joint."""
    verts = np.vstack([m["streams"][POSITION][np.unique(m["faces"])] for m in sources]).astype(float)
    d = _distance(verts, joint)
    end = verts[d <= d.min() + COLLAR_SAMPLE]
    return np.asarray(MeshyWeaponParts.convex_hull(end[:, [0, 2]].tolist())), float(d.min())


def collar_triangles(outline, low, high):
    """Closed prism of a convex outline between two heights, faces wound outward."""
    outline = np.asarray(outline, dtype=float)
    centre = np.array([outline[:, 0].mean(), (low + high) / 2, outline[:, 1].mean()])
    ring = [np.column_stack([outline[:, 0], np.full(len(outline), y), outline[:, 1]]) for y in (low, high)]
    triangles = []
    for i in range(len(outline)):
        j = (i + 1) % len(outline)
        triangles += [[ring[0][i], ring[0][j], ring[1][i]], [ring[0][j], ring[1][j], ring[1][i]]]
    for y, r in ((low, ring[0]), (high, ring[1])):
        middle = np.array([centre[0], y, centre[2]])
        triangles += [[middle, r[i], r[(i + 1) % len(r)]] for i in range(len(r))]
    triangles = np.asarray(triangles)
    normals = np.cross(triangles[:, 1] - triangles[:, 0], triangles[:, 2] - triangles[:, 0])
    inward = np.sum(normals * (triangles.mean(axis=1) - centre), axis=1) < 0
    triangles[inward] = triangles[inward][:, ::-1]
    return triangles


def plan(meshes, role):
    """The interface edit for a part, or None when its interfaces already fit.

    Returns (interface index, kept original face indices, new flat triangles (n, 3, 3),
    report). Cut caps stay as authored. A sleeve that reaches past its cut is dropped
    (the cap closes the cut); one inside the cut stays. A sleeve on a joint the source
    stops short of becomes a collar of the source's own end outline, unless it already
    lies within it.
    """
    index = interface_index(meshes, role)
    if index is None:
        return None
    interface = meshes[index]
    sources = [m for i, m in enumerate(meshes) if i != index]
    positions = interface["streams"][POSITION].astype(float)
    faces = interface["faces"].astype(int)
    kinds = classify(positions, faces, role)
    labels = components(positions, faces)
    keep, triangles, report = [], [], {}
    for joint in JOINTS[role]:
        caps = [i for i, kind in enumerate(kinds) if kind == ("cap", joint)]
        tubes = [i for i, kind in enumerate(kinds) if kind == ("tube", joint)]
        keep.extend(caps)
        entry = {"cap_triangles": len(caps), "sleeve_triangles_removed": 0, "collar_triangles": 0}
        rim = rim_points(sources, joint)
        if len(rim):
            hull = MeshyWeaponParts.convex_hull(rim.tolist())
            for label in np.unique(labels[tubes]) if tubes else []:
                members = [i for i in tubes if labels[i] == label]
                if _hull_contains(hull, positions[np.unique(faces[members])][:, [0, 2]]):
                    keep.extend(members)
                else:
                    entry["sleeve_triangles_removed"] += len(members)
        elif tubes:
            outline, gap = _end_section(sources, joint)
            tube = positions[np.unique(faces[tubes])]
            if _hull_contains(outline, tube[:, [0, 2]]):
                keep.extend(tubes)
            else:
                collar = collar_triangles(outline, tube[:, 1].min(), tube[:, 1].max())
                triangles.extend(collar)
                entry["sleeve_triangles_removed"] = len(tubes)
                entry["collar_triangles"] = len(collar)
            entry["source_end_gap_mm"] = round(gap * 1000, 2)
        elif caps:
            raise ValueError(f"Cut cap at {joint[0]} without a source rim")
        report[str(joint[0])] = entry
    unchanged = not triangles and len(keep) == len(faces)
    return None if unchanged else (index, np.asarray(sorted(keep), dtype=int), np.asarray(triangles, dtype=float), report)


def fit_bytes(before, role):
    """Return (bytes, report) with fitted interfaces, or (before, None) when nothing changes."""
    raw, meshes = NativeTangentSeams.mesh_headers(before)
    planned = plan(meshes, role)
    if planned is None:
        return before, None
    index, keep, triangles, report = planned
    info = meshes[index]
    old_faces = info["faces"].astype(int)
    streams = {field: info["streams"][field] for field, _ in NativeTangentSeams.STREAMS}
    faces = [old_faces[keep]]
    if len(triangles):
        basis = MeshyWeaponParts.flat_basis(triangles)
        added = {POSITION: basis["positions"], UV: basis["uv"], NORMAL: basis["normals"],
                 TANGENT: basis["tangents"], SIGN: basis["signs"][..., None]}
        base = len(streams[POSITION])
        for field, columns in NativeTangentSeams.STREAMS:
            streams[field] = np.vstack([streams[field], np.asarray(added[field], "<f4").reshape(-1, columns)])
        faces.append(base + np.arange(len(triangles) * 3).reshape(-1, 3))
    faces = np.vstack(faces)
    if len(faces) > len(old_faces):
        raise ValueError("Fitted interface needs more faces than the native face array holds")
    if len(streams[POSITION]) > NativeTangentSeams.MAX_VERTICES:
        raise ValueError("Fitted interface exceeds the native vertex limit")

    after = bytearray(before)
    allowed = set(range(8, 12))

    def append(values):
        after.extend(bytes((-len(after)) % 4))
        offset = len(after) - raw
        after.extend(np.ascontiguousarray(values).tobytes())
        return offset

    header, face_offset = info["header"], info["face_offset"]
    template = before[face_offset:face_offset + 32]
    positions = streams[POSITION].astype(float)
    for new, face in enumerate(faces):
        a, b, c = positions[face]
        normal = np.cross(b - a, c - a)
        normal /= np.linalg.norm(normal)
        record = bytearray(template)
        struct.pack_into("<4f", record, 0, *normal, -float(normal @ a))
        struct.pack_into("<3h", record, 20, -1, -1, -1)
        struct.pack_into("<3H", record, 26, *face)
        after[face_offset + new * 32:face_offset + new * 32 + 32] = record
    end = face_offset + len(old_faces) * 32
    after[face_offset + len(faces) * 32:end] = bytes(end - face_offset - len(faces) * 32)
    allowed.update(range(face_offset, end))
    struct.pack_into("<II", after, header + 12, len(faces), len(faces))
    allowed.update(range(header + 12, header + 20))
    count_pointer = 12 + struct.unpack_from("<I", before, header + 404)[0]
    struct.pack_into("<I", after, count_pointer, len(faces) * 3)
    allowed.update(range(count_pointer, count_pointer + 4))
    for field, _ in NativeTangentSeams.STREAMS:
        struct.pack_into("<I", after, header + field, append(np.asarray(streams[field], "<f4")))
        allowed.update(range(header + field, header + field + 4))
    struct.pack_into("<H", after, header + 448, len(streams[POSITION]))
    allowed.update(range(header + 448, header + 450))
    struct.pack_into("<I", after, info["draw_pointer"], append(faces.astype("<u2")))
    allowed.update(range(info["draw_pointer"], info["draw_pointer"] + 4))
    used = positions[np.unique(faces)]
    average = np.asarray(struct.unpack_from("<3f", before, header + 48))
    struct.pack_into("<3f", after, header + 20, *used.min(axis=0))
    struct.pack_into("<3f", after, header + 32, *used.max(axis=0))
    struct.pack_into("<f", after, header + 44, float(np.linalg.norm(used - average, axis=1).max()))
    allowed.update(range(header + 20, header + 48))
    struct.pack_into("<I", after, 8, len(after) - raw)
    changed = {i for i, (old, new) in enumerate(zip(before, after)) if old != new}
    if not changed <= allowed:
        raise ValueError("Interface fitting changed unrelated native metadata")
    after, _ = NativeTangentSeams.compact_model_bytes(bytes(after))
    validate(before, after, role)
    return after, report


def _corner_rows(info, faces=None):
    faces = info["faces"] if faces is None else faces
    rows = np.hstack([info["streams"][field][faces.reshape(-1)] for field, _ in NativeTangentSeams.STREAMS])
    return rows[np.lexsort(rows.T[::-1])]


def _cap_rows(info, role, joint):
    faces = info["faces"].astype(int)
    kinds = classify(info["streams"][POSITION].astype(float), faces, role)
    return _corner_rows(info, faces[[kind == ("cap", joint) for kind in kinds]])


def interface_problems(meshes, role):
    """Interface geometry that does not fit its own part; empty when every interface fits.

    Every cut joint is closed by a flat cap that fills the cut's outline and stays
    within it, and any tube (sleeve or collar) lies within the part's own section:
    the cut's outline, or the end outline of a source that stops short of its joint.
    """
    index = interface_index(meshes, role)
    if index is None:
        return []
    interface = meshes[index]
    sources = [m for i, m in enumerate(meshes) if i != index]
    positions = interface["streams"][POSITION].astype(float)
    faces = interface["faces"].astype(int)
    kinds = classify(positions, faces, role)
    problems = []
    for joint in JOINTS[role]:
        rim = rim_points(sources, joint)
        caps = [faces[i] for i, kind in enumerate(kinds) if kind == ("cap", joint)]
        tubes = [faces[i] for i, kind in enumerate(kinds) if kind == ("tube", joint)]
        if len(rim):
            hull = MeshyWeaponParts.convex_hull(rim.tolist())
            if not caps:
                problems.append(f"cut at y={joint[0]} is not capped")
            else:
                corners = positions[np.vstack(caps)][:, :, [0, 2]]
                if not _hull_contains(hull, corners.reshape(-1, 2)):
                    problems.append(f"cap at y={joint[0]} reaches past the cut")
                (ax, az), (bx, bz) = (corners[:, 1] - corners[:, 0]).T, (corners[:, 2] - corners[:, 0]).T
                covered = float(np.sum(np.abs(ax * bz - az * bx))) / 2
                if covered < _area(hull) * CAP_COVERAGE:
                    problems.append(f"cap at y={joint[0]} leaves part of the cut open")
            outline = hull
        elif tubes:
            outline = _end_section(sources, joint)[0]
        else:
            continue
        if tubes and not _hull_contains(outline, positions[np.unique(np.vstack(tubes))][:, [0, 2]]):
            problems.append(f"sleeve at y={joint[0]} reaches past the part section")
    return problems


def validate(before, after, role):
    """Source trimeshes keep every rendered corner and every interface fits its part."""
    _, old_meshes = NativeTangentSeams.mesh_headers(before)
    _, new_meshes = NativeTangentSeams.mesh_headers(after)
    if len(old_meshes) != len(new_meshes):
        raise ValueError("Rendered trimesh count changed")
    index = interface_index(old_meshes, role)
    if interface_index(new_meshes, role) != index:
        raise ValueError("Interface trimesh changed position")
    for position, (old, new) in enumerate(zip(old_meshes, new_meshes)):
        if position != index and not np.array_equal(_corner_rows(old), _corner_rows(new)):
            raise ValueError("Interface fitting changed a source trimesh")
    for joint in JOINTS[role]:
        if not np.array_equal(_cap_rows(old_meshes[index], role, joint), _cap_rows(new_meshes[index], role, joint)):
            raise ValueError(f"Interface fitting changed the cut cap at y={joint[0]}")
    problems = interface_problems(new_meshes, role)
    if problems:
        raise ValueError("; ".join(problems))


def run(sw_weapon, output, models=None):
    sw_weapon, output = Path(sw_weapon).resolve(), Path(output).resolve()
    if output.exists() and any(output.iterdir()):
        raise FileExistsError("Use a fresh output directory")
    names = models or sorted(path.stem for path in sw_weapon.glob("wswls_*.mdl") if MODEL.fullmatch(path.stem))
    output.mkdir(parents=True, exist_ok=True)
    reports = {}
    for name in names:
        before = (sw_weapon / (name + ".mdl")).read_bytes()
        if not supported(before):
            continue
        try:
            after, report = fit_bytes(before, role_of(name))
        except ValueError as error:
            raise ValueError(f"{name}: {error}") from error
        if report is None:
            continue
        (output / (name + ".mdl")).write_bytes(after)
        reports[name] = report
    (output / "joint-interfaces.json").write_text(json.dumps(reports, indent=2) + "\n")
    return reports


def supported(data):
    """Static rigid compiled models (the fitted parts); legacy animated or skinned parts have no interface."""
    try:
        NativeTangentSeams.mesh_headers(data)
    except ValueError:
        return False
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sw-weapon", required=True, help="SWLOR_Haks/sw_weapon")
    parser.add_argument("--output", required=True, help="Fresh local output directory for the edited models")
    parser.add_argument("models", nargs="*", help="Optional wswls_[tmb]_NNN models (default: every fitted part)")
    args = parser.parse_args()
    reports = run(args.sw_weapon, args.output, args.models)
    print(f"Fitted joint interfaces of {len(reports)} vibroblade part models")


if __name__ == "__main__":
    main()
