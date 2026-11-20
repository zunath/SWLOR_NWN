"""Put every fitted composite weapon part on the stock joint axis, and give each guard back to
its middle.

`MeshyWeaponParts.py` cuts each fitted Meshy source at its family's stock joint planes but
leaves the source where the reviewed fit placed it. Those fits held the source off the
stock axis: every fitted vibroblade and greatsword sat about 20 mm off in X, and some
knife, polearm and vibroblade sources up to 47 mm off in Z. Parts cut from one source
still meet each other, but a top, middle or bottom from another source, or a stock part,
meets them off-centre at every joint (the November 2026 spear and greatsword reports).

This moves each source, as one rigid piece, so the core of its axis part (the grip, or the
polearm shaft) is centred on the stock axis X = Z = 0 that the stock parts share. Every part
cut from the source moves by the same offset, so its own parts still meet exactly and each
part keeps its own geometry. The edit is a translation of the compiled vertex positions
(with the face planes and bounding data that follow them); normals, tangents, UVs,
materials and faces stay byte-identical.

Parts are grouped into sources by their cuts and textures: two parts share a source when
their source surfaces meet a joint plane at the same points or bind the same texture.

The same stock cuts sliced most fitted greatsword guards, which are taller than the middle
band, so a top or bottom carried a plate of its own source's guard under any other middle.
`GUARD_PIECES` lists those parts; their guard pieces move back to their middle (see there).

Run it on the hak sources after every fitted import; it changes nothing on its own output.
"""
import argparse
import json
from pathlib import Path
import re
import struct
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import FitBladeJointInterfaces as fit
import NativeTangentSeams

# The role whose core defines a source's axis: the grip, or the polearm shaft. The twinblade
# bottom is its central grip.
AXIS_ROLE = {"wswls": "b", "wswgs": "b", "wswdg": "b", "wplss": "m", "wdbsw": "b"}
FAMILIES = tuple(AXIS_ROLE)
MODEL = re.compile(r"(" + "|".join(FAMILIES) + r")_([tmb])_\d{3}")
STEP = 0.004  # Sections are sampled every 4 mm along the part.
END_MARGIN = 0.002  # ...starting this far inside each end.
JOINT_REACH = 0.05  # A source without its axis part is centred on its parts' ends within this of a joint.
# A source whose core lies within this of the axis is left alone: 2 mm cannot be seen at a joint,
# and the cores of a source's parts differ by about that much.
AXIS_TOLERANCE = 0.002
RIM_PRECISION = 1e4  # Cut points are matched to 0.1 mm.
SHARED_CUT_POINTS = 3  # Parts whose cuts share this many points come from one source.
POSITION = fit.POSITION
# Model header bounding box and radius (file offsets): min, max, radius.
MODEL_MIN, MODEL_MAX, MODEL_RADIUS = 148, 160, 172


class Part:
    """A fitted part: its compiled bytes, rendered meshes and authored interface mesh."""

    def __init__(self, name, data):
        self.name, self.data = name, data
        self.family, self.role = MODEL.fullmatch(name).groups()
        self.joint_role = fit.role_of(name)
        self.raw, self.meshes = NativeTangentSeams.mesh_headers(data)
        self.interface = fit.interface_index(self.meshes, self.joint_role)
        self.sources = [m for i, m in enumerate(self.meshes) if i != self.interface]

    def source_geometry(self):
        """All source-surface vertices and faces of the part, as one indexed mesh."""
        positions, faces, base = [], [], 0
        for mesh in self.sources:
            positions.append(mesh["streams"][POSITION].astype(float))
            faces.append(mesh["faces"].astype(int) + base)
            base += len(positions[-1])
        return np.vstack(positions), np.vstack(faces)

    def cuts(self):
        """{joint y: set of points where the source surfaces meet the joint plane}.

        Unlike a rim, this keeps the surfaces that run on through the plane, so a middle that
        took back a guard piece still shares its cut with the top or bottom it came from.
        """
        result = {}
        for joint in fit.JOINTS[self.joint_role]:
            points = []
            for mesh in self.sources:
                positions = mesh["streams"][POSITION].astype(float)
                faces = mesh["faces"].astype(int)
                on = np.abs(positions[:, 1] - joint[0]) < fit.PLANE
                used = np.unique(faces[on[faces].sum(axis=1) >= 2])
                points.extend(positions[used[on[used]]][:, [0, 2]].tolist())
            if points:
                result[joint[0]] = set(map(tuple, np.round(np.asarray(points) * RIM_PRECISION).astype(int).tolist()))
        return result


def section_boxes(positions, faces, levels):
    """(x min, z min, x max, z max) of the mesh's section at each Y level; NaN where none."""
    triangles = positions[faces]
    boxes = np.full((len(levels), 4), np.nan)
    for index, y in enumerate(levels):
        d = triangles[:, :, 1] - y
        points = []
        for a, b in ((0, 1), (1, 2), (2, 0)):
            crossing = (d[:, a] > 0) != (d[:, b] > 0)
            if not crossing.any():
                continue
            pa, pb = triangles[crossing, a], triangles[crossing, b]
            t = (d[crossing, a] / (d[crossing, a] - d[crossing, b]))[:, None]
            points.append((pa + t * (pb - pa))[:, [0, 2]])
        if points:
            points = np.vstack(points)
            boxes[index] = [*points.min(axis=0), *points.max(axis=0)]
    return boxes


def core_centre(positions, faces, near=None):
    """(x, z) centre of a part's core: the median section centre over its slimmer half.

    Wide guards, pommels and blade shoulders cover a short stretch of the part, so they fall
    in the wider half and do not move the result. `near` limits the samples to JOINT_REACH
    of those joint heights.
    """
    low, high = positions[:, 1].min() + END_MARGIN, positions[:, 1].max() - END_MARGIN
    levels = np.arange(low, high, STEP)
    if near is not None:
        levels = levels[np.min(np.abs(levels[:, None] - np.asarray(near)[None, :]), axis=1) <= JOINT_REACH]
    boxes = section_boxes(positions, faces, levels)
    boxes = boxes[~np.isnan(boxes[:, 0])]
    if not len(boxes):
        raise ValueError("Part has no measurable section")
    area = (boxes[:, 2] - boxes[:, 0]) * (boxes[:, 3] - boxes[:, 1])
    core = boxes[area <= np.median(area)]
    return np.array([np.median((core[:, 0] + core[:, 2]) / 2), np.median((core[:, 1] + core[:, 3]) / 2)])


def sources(parts):
    """Lists of part names cut from one source: parts whose surfaces meet a joint plane at the
    same points, or that bind the same single texture (each fitted source has its own atlas,
    and each colour variant its own copy)."""
    names = sorted(parts)
    parent = {name: name for name in names}

    def find(name):
        while parent[name] != name:
            parent[name] = parent[parent[name]]
            name = parent[name]
        return name

    textures = {}
    for name in names:
        part = parts[name]
        if len(part.sources) != 1:
            continue  # Twinblade parts mix a grip texture every source shares with their own.
        key = (part.family, texture(part.data, part.sources[0]))
        if key in textures:
            parent[find(name)] = find(textures[key])
        else:
            textures[key] = name
    cuts = {name: parts[name].cuts() for name in names}
    for index, name in enumerate(names):
        for other in names[index + 1:]:
            if parts[other].family != parts[name].family:
                continue
            if any(len(points & cuts[other].get(y, set())) >= SHARED_CUT_POINTS for y, points in cuts[name].items()):
                parent[find(other)] = find(name)
    groups = {}
    for name in names:
        groups.setdefault(find(name), []).append(name)
    return sorted(groups.values())


def source_axis(parts, group):
    """(x, z) of a source's axis: the core centre of its axis part (the grip or shaft).

    A source shipped without its axis part is centred where its parts meet others: the core
    of each part's ends next to its joints.
    """
    family = parts[group[0]].family
    axis = [name for name in group if parts[name].role == AXIS_ROLE[family]]
    if axis:
        centres = [core_centre(*parts[name].source_geometry()) for name in axis]
    else:
        centres = [core_centre(*parts[name].source_geometry(), near=[y for y, _ in fit.JOINTS[parts[name].joint_role]])
                   for name in group]
    return np.median(np.asarray(centres), axis=0)


def mesh_nodes_at_origin(data):
    """True when no rendered mesh node carries its own transform (geometry is in model space)."""
    import WeaponGripFit
    model = WeaponGripFit.Model(data)
    return all(np.allclose(node["world"], np.eye(4)) for node in model.nodes if node["vertices"] is not None)


def translate_bytes(before, offset):
    """Return the compiled model moved by (x, z) `offset`; only positions and the bounding
    data, face planes and model bounds that follow them change."""
    if not mesh_nodes_at_origin(before):
        raise ValueError("Rendered mesh nodes carry their own transform")
    shift = np.array([offset[0], 0.0, offset[1]])
    raw, meshes = NativeTangentSeams.mesh_headers(before)
    after = bytearray(before)
    written = set()
    for mesh in meshes:
        header = mesh["header"]
        stream = struct.unpack_from("<i", before, header + POSITION)[0]
        if stream in written:
            raise ValueError("Rendered meshes share a position stream")
        written.add(stream)
        positions = mesh["streams"][POSITION].astype(np.float64) + shift
        after[raw + stream:raw + stream + mesh["count"] * 12] = positions.astype("<f4").tobytes()
        for index in range(len(mesh["faces"])):
            record = mesh["face_offset"] + index * 32
            normal = np.asarray(struct.unpack_from("<3f", before, record))
            distance = struct.unpack_from("<f", before, record + 12)[0]
            struct.pack_into("<f", after, record + 12, distance - float(normal @ shift))
        for field in (20, 32, 48):  # Mesh bounding box min, max and average point.
            values = np.asarray(struct.unpack_from("<3f", before, header + field)) + shift
            struct.pack_into("<3f", after, header + field, *values)
    used = np.vstack([np.frombuffer(after, "<f4", m["count"] * 3, raw + struct.unpack_from("<i", before, m["header"] + POSITION)[0])
                      .reshape(-1, 3)[np.unique(m["faces"])] for m in meshes]).astype(float)
    low = np.minimum(np.asarray(struct.unpack_from("<3f", before, MODEL_MIN)), used.min(axis=0))
    high = np.maximum(np.asarray(struct.unpack_from("<3f", before, MODEL_MAX)), used.max(axis=0))
    radius = max(struct.unpack_from("<f", before, MODEL_RADIUS)[0], float(np.linalg.norm(high - low)) / 2)
    struct.pack_into("<3f", after, MODEL_MIN, *low)
    struct.pack_into("<3f", after, MODEL_MAX, *high)
    struct.pack_into("<f", after, MODEL_RADIUS, radius)
    after = bytes(after)
    validate_translation(before, after, shift)
    return after


def validate_translation(before, after, shift):
    """Every rendered corner keeps its attributes; positions move by exactly `shift`."""
    _, old = NativeTangentSeams.mesh_headers(before)
    _, new = NativeTangentSeams.mesh_headers(after)
    if len(old) != len(new):
        raise ValueError("Rendered mesh count changed")
    for a, b in zip(old, new):
        if not np.array_equal(a["faces"], b["faces"]):
            raise ValueError("Faces changed")
        for field, _ in NativeTangentSeams.STREAMS:
            if field != POSITION and not np.array_equal(a["streams"][field], b["streams"][field]):
                raise ValueError("A vertex attribute other than position changed")
        expected = (a["streams"][POSITION].astype(np.float64) + shift).astype("<f4")
        if not np.array_equal(expected, b["streams"][POSITION]):
            raise ValueError("Positions did not move by the source offset")


# ---------------------------------------------------------------- guard pieces
# Many fitted sources have guards taller than their family's middle band (greatsword Y 0.111
# to 0.17, vibroblade 0.085 to 0.135, knife 0.075 to 0.098, twinblade ends 45 mm), so the stock
# cuts left slices of the guard on the bottom or top (the November 2026 greatsword report).
# Each entry names a top or bottom and the height its guard reaches (one per joint, for a
# twinblade end): every surface piece of that part lying wholly between this height and the
# joint is the guard's. Those pieces go back to the middle cut from the same source and bound
# to the same texture, or are dropped where that middle was never shipped. Reviewed against
# side views of every fitted source; grip ferrules and blade shoulders that only touch a joint
# stay.
GUARD_PIECES = {
    "wswgs_b_012": 0.095, "wswgs_b_013": 0.095, "wswgs_b_014": 0.095,  # Guard's lower rim
    "wswgs_t_012": 0.19, "wswgs_t_013": 0.19,  # Crossguard tips
    "wswgs_b_022": 0.05, "wswgs_b_023": 0.05,  # Lower half of the guard block
    "wswgs_b_031": 0.05, "wswgs_b_032": 0.05, "wswgs_b_033": 0.05, "wswgs_b_034": 0.05,  # Guard plate
    "wswgs_t_031": 0.19, "wswgs_t_032": 0.19, "wswgs_t_033": 0.19, "wswgs_t_034": 0.19,  # Guard horns
    "wswgs_b_041": 0.08, "wswgs_b_101": 0.08,  # Hooked quillon
    "wswgs_t_014": 0.175,  # Guard slivers
    "wswgs_t_044": 0.218, "wswgs_t_104": 0.218,  # Crossbar, knobs and guard collar
    "wswgs_b_051": 0.06, "wswgs_b_052": 0.06, "wswgs_b_053": 0.06, "wswgs_b_054": 0.06,  # Guard plate
    "wswls_b_012": 0.045, "wswls_b_013": 0.045,  # Guard wings
    "wswls_b_022": 0.03, "wswls_b_023": 0.03,  # Guard core and curled quillon
    "wswls_b_031": 0.045, "wswls_b_032": 0.045, "wswls_b_033": 0.045, "wswls_b_034": 0.045,  # Guard slab
    "wswls_b_062": 0.04, "wswls_b_063": 0.04, "wswls_b_064": 0.04,  # Tip of the hooked guard
    "wswls_b_082": 0.05, "wswls_b_083": 0.05, "wswls_b_084": 0.05,  # Guard arm
    "wswdg_b_042": 0.045, "wswdg_b_043": 0.045, "wswdg_b_044": 0.045,  # Lower guard block
    "wswdg_b_052": 0.028, "wswdg_b_053": 0.028, "wswdg_b_054": 0.028,  # Guard core and quillon
    "wswdg_b_061": 0.02, "wswdg_b_062": 0.02, "wswdg_b_063": 0.02, "wswdg_b_064": 0.02,  # Guard horns
    "wdbsw_t_031": (-0.215, 0.88), "wdbsw_t_033": (-0.215, 0.88), "wdbsw_t_034": (-0.215, 0.88),  # Guard horns
    "wdbsw_t_032": -0.136,  # Guard tips
}
GAP_TOLERANCE = 0.0005  # A part that stops this far short of its joint gets a collar.


def texture(data, mesh):
    return data[mesh["header"] + 120:mesh["header"] + 152].split(b"\0", 1)[0].decode("latin1").lower()


def corner_streams(mesh, faces):
    """Per-corner stream rows of the given faces: {field: (n, 3, columns)}."""
    return {field: mesh["streams"][field][faces] for field, _ in NativeTangentSeams.STREAMS}


def indexed(corners):
    """Indexed streams and faces for per-corner stream rows."""
    count = len(corners[POSITION])
    columns = dict(NativeTangentSeams.STREAMS)
    streams = {field: np.asarray(values).reshape(count * 3, columns[field]) for field, values in corners.items()}
    return streams, np.arange(count * 3).reshape(-1, 3)


def merged(streams, faces, extra_streams, extra_faces):
    base = len(streams[POSITION])
    return ({field: np.vstack([streams[field], extra_streams[field]]) for field in streams},
            np.vstack([faces, extra_faces + base]))


def flat(triangles):
    """Indexed streams and faces for authored flat interface triangles (MeshyWeaponParts basis)."""
    basis = fit.MeshyWeaponParts.flat_basis(np.asarray(triangles, dtype=float))
    corners = {POSITION: basis["positions"], fit.UV: basis["uv"], fit.NORMAL: basis["normals"],
               fit.TANGENT: basis["tangents"], fit.SIGN: basis["signs"][..., None]}
    return indexed(corners)


def bounds(entry):
    """The guard heights listed for a part: one per joint its guard crosses."""
    return entry if isinstance(entry, tuple) else (entry,)


def guard_joint(part, bound):
    """The joint whose guard reaches `bound`: the nearest one with `bound` on the part's side."""
    candidates = [joint for joint in fit.JOINTS[part.joint_role] if joint[1] * (bound - joint[0]) > 0]
    if not candidates:
        raise ValueError(f"{part.name}: y={bound} lies past every joint")
    return min(candidates, key=lambda joint: abs(bound - joint[0]))


def guard_pieces(part, bound, faces=None):
    """(joint, face mask) of the part's source mesh pieces lying wholly between `bound` and the
    joint they touch."""
    if len(part.sources) != 1:
        raise ValueError(f"{part.name}: expected one source mesh")
    joint = guard_joint(part, bound)
    y, side = joint
    positions = part.sources[0]["streams"][POSITION].astype(float)
    faces = part.sources[0]["faces"].astype(int) if faces is None else faces
    labels = fit.components(positions, faces)
    pieces = np.zeros(len(faces), dtype=bool)
    reach = side * (bound - y)
    for label in np.unique(labels):
        members = labels == label
        d = side * (positions[np.unique(faces[members])][:, 1] - y)
        if d.min() >= -fit.PLANE and d.max() <= reach + fit.PLANE:
            pieces |= members
    return joint, pieces


def without_joint(streams, faces, joint, role):
    """An interface mesh's streams and faces without its caps and tubes at `joint`."""
    kinds = fit.classify(np.asarray(streams[POSITION], dtype=float), faces, role)
    if kinds is None:
        raise ValueError("Interface mesh no longer classifies")
    keep = np.asarray([i for i, kind in enumerate(kinds) if kind is None or kind[1] != joint], dtype=int)
    return indexed({field: values[faces[keep]] for field, values in streams.items()})


def mesh_arrays(mesh):
    return {field: mesh["streams"][field] for field, _ in NativeTangentSeams.STREAMS}, mesh["faces"].astype(int)


def closing(source_streams, source_faces, joint, role):
    """Flat triangles that close the source surfaces at `joint`: a cap over their cut, or a collar
    of their own end outline where they stop short of it."""
    sources = [{"streams": source_streams, "faces": source_faces}]
    rim = fit.rim_points(sources, joint)
    if len(rim):
        return fit.MeshyWeaponParts.cap_triangles(rim.tolist(), joint[0], -joint[1])  # Faces out of the part.
    outline, gap = fit._end_section(sources, joint, role)
    if gap <= GAP_TOLERANCE:
        raise ValueError(f"Source surfaces end at y={joint[0]} without a cut")
    end = joint[0] + joint[1] * gap
    return fit.collar_triangles(outline, min(joint[0], end), max(joint[0], end))


def give_back_guards(parts, guards=None):
    """{part name: new compiled bytes} with every listed guard piece moved to its middle.

    Each top or bottom keeps only its own surfaces and is closed again at its joint (a cap,
    or a collar where it now stops short). The pieces go to each middle that shares the part's
    cut at that joint and has a source mesh in the same texture, and that middle's cap at the
    joint shrinks to the surfaces that still end there. With no such middle they are dropped.
    """
    guards = GUARD_PIECES if guards is None else guards
    group_of = {name: group for group in sources(parts) for name in group}
    cuts = {}
    meshes = {}  # part name: {mesh index: (streams, faces)}
    received = {}  # (middle, mesh index, joint y): per-corner streams of the pieces it takes back

    def current(name):
        if name not in meshes:
            meshes[name] = {i: mesh_arrays(mesh) for i, mesh in enumerate(parts[name].meshes)}
        return meshes[name]

    def cut(name):
        if name not in cuts:
            cuts[name] = parts[name].cuts()
        return cuts[name]

    for name, entry in sorted(guards.items()):
        if name not in parts:
            raise ValueError(f"{name}: not a fitted part")
        part = parts[name]
        if part.role == "m":
            raise ValueError(f"{name}: guard pieces are listed on tops and bottoms")
        source_index = next(i for i in range(len(part.meshes)) if i != part.interface)
        own = texture(part.data, part.sources[0])
        for bound in bounds(entry):
            state = current(name)
            streams, faces = state[source_index]
            joint, pieces = guard_pieces(part, bound, faces)
            if not pieces.any():
                raise ValueError(f"{name}: no guard piece between y={bound} and its joint")
            moved = {field: values[faces[pieces]] for field, values in streams.items()}
            kept = (streams, faces[~pieces])
            state[source_index] = kept
            state[part.interface] = merged(*without_joint(*state[part.interface], joint, part.joint_role),
                                           *flat(closing(*kept, joint, part.joint_role)))
            for middle in group_of[name]:
                other = parts[middle]
                if other.role != "m" or joint[0] not in cut(middle) or joint[0] not in cut(name):
                    continue
                if len(cut(middle)[joint[0]] & cut(name)[joint[0]]) < SHARED_CUT_POINTS:
                    continue
                for index, mesh in enumerate(other.meshes):
                    if index == other.interface or texture(other.data, mesh) != own:
                        continue
                    previous = received.setdefault((middle, index, joint[0]), moved)
                    if previous is not moved and not all(np.array_equal(previous[f], moved[f]) for f in moved):
                        raise ValueError(f"{middle}: different guard pieces come back at y={joint[0]}")
    for (name, index, y), moved in sorted(received.items()):
        state = current(name)
        state[index] = merged(*state[index], *indexed(moved))
    for name in sorted({key[0] for key in received}):
        part, state = parts[name], current(name)
        sources_now = [{"streams": state[i][0], "faces": state[i][1]} for i in state if i != part.interface]
        for y in sorted({key[2] for key in received if key[0] == name}):
            joint = next(j for j in fit.JOINTS[part.joint_role] if j[0] == y)
            interface = without_joint(*state[part.interface], joint, part.joint_role)
            rim = fit.rim_points(sources_now, joint)
            if len(rim):
                interface = merged(*interface, *flat(fit.MeshyWeaponParts.cap_triangles(rim.tolist(), joint[0], -joint[1])))
            state[part.interface] = interface
    result = {}
    for name, state in sorted(meshes.items()):
        original = {i: mesh_arrays(mesh) for i, mesh in enumerate(parts[name].meshes)}
        changed = {i: arrays for i, arrays in state.items()
                   if not (np.array_equal(arrays[1], original[i][1]) and all(
                       np.array_equal(arrays[0][f], original[i][0][f]) for f in arrays[0]))}
        result[name] = rewrite(parts[name].data, changed)
    return result


def rewrite(before, replacements):
    """Compiled model bytes with whole meshes replaced: {mesh index: (streams, faces)}.

    A face array that grows moves to the end of the model data; raw data offsets are relative
    to their block, so they still hold. Unused vertices are compacted away afterwards.
    """
    raw, meshes = NativeTangentSeams.mesh_headers(before)
    model, data = bytearray(before[:raw]), bytearray(before[raw:])

    def append_raw(values):
        data.extend(bytes((-len(data)) % 4))
        offset = len(data)
        data.extend(np.ascontiguousarray(values).tobytes())
        return offset

    for index, (streams, faces) in sorted(replacements.items()):
        info = meshes[index]
        header, old_count = info["header"], len(info["faces"])
        faces = np.asarray(faces, dtype=int)
        positions = np.asarray(streams[POSITION], dtype=float)
        if len(positions) > NativeTangentSeams.MAX_VERTICES:
            raise ValueError("Mesh exceeds the native vertex limit")
        template = before[info["face_offset"]:info["face_offset"] + 32]
        records = bytearray()
        for face in faces:
            a, b, c = positions[face]
            normal = np.cross(b - a, c - a)
            length = np.linalg.norm(normal)
            normal = normal / length if length > 0 else np.array([0.0, 1.0, 0.0])
            record = bytearray(template)
            struct.pack_into("<4f", record, 0, *normal, -float(normal @ a))
            struct.pack_into("<3h", record, 20, -1, -1, -1)
            struct.pack_into("<3H", record, 26, *face)
            records.extend(record)
        if len(faces) <= old_count:
            start = info["face_offset"]
            model[start:start + old_count * 32] = bytes(records) + bytes((old_count - len(faces)) * 32)
        else:
            model.extend(bytes((-len(model)) % 4))
            struct.pack_into("<I", model, header + 8, len(model) - 12)
            model.extend(records)
        struct.pack_into("<II", model, header + 12, len(faces), len(faces))
        struct.pack_into("<I", model, 12 + struct.unpack_from("<I", before, header + 404)[0], len(faces) * 3)
        for field, columns in NativeTangentSeams.STREAMS:
            values = np.asarray(streams[field], dtype="<f4").reshape(-1, columns)
            struct.pack_into("<I", model, header + field, append_raw(values))
        struct.pack_into("<H", model, header + 448, len(positions))
        struct.pack_into("<I", model, info["draw_pointer"], append_raw(faces.astype("<u2")))
        used = positions[np.unique(faces)]
        average = np.asarray(struct.unpack_from("<3f", before, header + 48))
        struct.pack_into("<3f", model, header + 20, *used.min(axis=0))
        struct.pack_into("<3f", model, header + 32, *used.max(axis=0))
        struct.pack_into("<f", model, header + 44, float(np.linalg.norm(used - average, axis=1).max()))
    model.extend(bytes((-len(model)) % 4))
    struct.pack_into("<II", model, 4, len(model) - 12, len(data))
    after = bytes(model + data)
    _, new = NativeTangentSeams.mesh_headers(after)
    every = np.vstack([m["streams"][POSITION][np.unique(m["faces"])] for m in new]).astype(float)
    low = np.minimum(np.asarray(struct.unpack_from("<3f", before, MODEL_MIN)), every.min(axis=0))
    high = np.maximum(np.asarray(struct.unpack_from("<3f", before, MODEL_MAX)), every.max(axis=0))
    after = bytearray(after)
    struct.pack_into("<3f", after, MODEL_MIN, *low)
    struct.pack_into("<3f", after, MODEL_MAX, *high)
    struct.pack_into("<f", after, MODEL_RADIUS, max(struct.unpack_from("<f", before, MODEL_RADIUS)[0],
                                                    float(np.linalg.norm(high - low)) / 2))
    after, _ = NativeTangentSeams.compact_model_bytes(bytes(after))
    return after


def load(sw_weapon, names=None):
    parts = {}
    for path in sorted(Path(sw_weapon).glob("*.mdl")):
        if not MODEL.fullmatch(path.stem) or (names and path.stem not in names):
            continue
        data = path.read_bytes()
        if not fit.supported(data):
            continue  # Legacy animated or skinned parts were never fitted.
        part = Part(path.stem, data)
        if part.interface is not None:  # Only fitted parts carry an authored interface.
            parts[path.stem] = part
    return parts


def plan(parts):
    """[(source part names, (x, z) offset that centres the source)] for every off-axis source."""
    result = []
    for group in sources(parts):
        offset = -source_axis(parts, group)
        if np.abs(offset).max() > AXIS_TOLERANCE:
            result.append((group, offset))
    return result


def align(parts):
    """({part name: new compiled bytes}, report): every source centred, then every listed guard
    piece given back to its middle. Running it on its own output changes nothing."""
    changed, report = {}, {"sources": [], "guard_parts": []}
    for group, offset in plan(parts):
        for name in group:
            try:
                changed[name] = translate_bytes(parts[name].data, offset)
            except ValueError as error:
                raise ValueError(f"{name}: {error}") from error
        report["sources"].append({"parts": group, "offset_mm": [round(float(v) * 1000, 2) for v in offset]})
    parts = {name: Part(name, changed[name]) if name in changed else part for name, part in parts.items()}
    pending = {name: entry for name, entry in GUARD_PIECES.items()
               if name in parts and all(guard_pieces(parts[name], bound)[1].any() for bound in bounds(entry))}
    guarded = give_back_guards(parts, pending) if pending else {}
    changed.update(guarded)
    report["guard_parts"] = sorted(guarded)
    return changed, report


def run(sw_weapon, output):
    sw_weapon, output = Path(sw_weapon).resolve(), Path(output).resolve()
    if not sw_weapon.is_dir():
        raise FileNotFoundError(f"No such sw_weapon folder: {sw_weapon}")
    if output.exists() and any(output.iterdir()):
        raise FileExistsError("Use a fresh output directory")
    changed, report = align(load(sw_weapon))
    output.mkdir(parents=True, exist_ok=True)
    for name, data in sorted(changed.items()):
        (output / (name + ".mdl")).write_bytes(data)
    (output / "joint-alignment.json").write_text(json.dumps(report, indent=2) + "\n")
    return changed, report


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--sw-weapon", required=True, help="SWLOR_Haks/sw_weapon")
    parser.add_argument("--output", required=True, help="Fresh local output directory for the edited models")
    args = parser.parse_args()
    changed, report = run(args.sw_weapon, args.output)
    print(f"Centred {sum(len(entry['parts']) for entry in report['sources'])} parts of {len(report['sources'])} sources; "
          f"gave guard pieces back in {len(report['guard_parts'])} parts; {len(changed)} models written")


if __name__ == "__main__":
    main()
