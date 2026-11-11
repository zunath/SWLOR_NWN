"""Widen pinched quarterstaff joint transitions in compiled native part models.

The fitted staff parts narrow over the last 25 mm before every part boundary to
the 10 mm native connector envelope, so two such parts meet tip to tip. This
post-processes the compiled models in place of a decompile round trip (which
would average the authored tangent seams): it extrudes each part's own shoulder
cross-section through that band, widens the matching cut cap so the end stays
closed and rebuilds only the moved vertices' frames, face planes and bounds.
Every other rendered corner stays byte-identical; UVs, materials, the inner
joint sleeves and parts without a constriction are unchanged.
"""
import argparse
import json
import math
from pathlib import Path
import re
import struct
import sys

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import NativeTangentSeams

# Native quarterstaff roles (BlasterModelImport.md): bottom is the central grip,
# middle both adjacent bands and top both terminal sections. Each joint is
# (y, side) where side is +1 when the part lies above the joint.
JOINTS = {
    "t": ((-0.6, -1), (1.2, 1)),
    "m": ((-0.6, 1), (-0.3, -1), (0.9, 1), (1.2, -1)),
    "b": ((-0.3, 1), (0.9, -1)),
}
TRANSITION = 0.025  # Measured length of the authored transition band.
SHOULDER_SAMPLE = 0.003  # Shoulder cross-section samples beyond the band.
SLEEVE_DEPTH = 0.0025  # Authored interface sleeves end within this depth.
ENVELOPE_LIMIT = 0.0135  # Largest authored connector sleeve radius.
CONSTRICTION = 0.75  # A cut rim narrower than this share of its shoulder is pinched.
PLANE = 1e-5
BINS = 72
POSITION, UV, NORMAL, TANGENT, SIGN = 444, 452, 468, 488, 496
MODEL = re.compile(r"wdbqs_([tmb])_\d{3}")


def role_of(name):
    match = MODEL.fullmatch(name)
    if not match:
        raise ValueError(f"Not a quarterstaff part model: {name}")
    return match[1]


def _distance(verts, joint):
    y, side = joint
    return side * (verts[:, 1] - y)


def _radius(verts):
    return np.hypot(verts[:, 0], verts[:, 2])


def _unit(vectors):
    length = np.linalg.norm(vectors, axis=-1, keepdims=True)
    return vectors / np.where(length > 1e-12, length, 1)


def is_interface(verts, role):
    """Authored cut caps and inner sleeves hold only connector-sized joint geometry."""
    near = np.zeros(len(verts), bool)
    for joint in JOINTS[role]:
        d = _distance(verts, joint)
        near |= (d > -PLANE) & (d < SLEEVE_DEPTH)
    return bool(len(verts)) and near.all() and _radius(verts).max() <= ENVELOPE_LIMIT


def joint_constrictions(meshes, role, limit=CONSTRICTION):
    """Joints whose cut rim is narrower than `limit` of the part's shoulder 25 mm inside.

    `meshes` are vertex position arrays of every rendered trimesh in the part.
    """
    verts = np.vstack([v for v in meshes if len(v)])
    radius = _radius(verts)
    found = []
    for joint in JOINTS[role]:
        d = _distance(verts, joint)
        rim = (d > -PLANE) & (d < 0.0005)
        shoulder = (d >= TRANSITION - 0.0005) & (d <= TRANSITION + SHOULDER_SAMPLE)
        if rim.any() and shoulder.any() and radius[rim].max() < limit * radius[shoulder].max():
            found.append((joint[0], round(float(radius[rim].max()), 4), round(float(radius[shoulder].max()), 4)))
    return found


def _profile(angles, radii):
    """Largest radius per angular bin, circularly interpolated over empty bins."""
    bins = np.full(BINS, np.nan)
    index = np.floor((angles % (2 * math.pi)) / (2 * math.pi) * BINS).astype(int) % BINS
    for i, radius in zip(index, radii):
        bins[i] = radius if np.isnan(bins[i]) else max(bins[i], radius)
    known = np.flatnonzero(~np.isnan(bins))
    if len(known) < BINS // 4:
        raise ValueError("Too few shoulder cross-section samples")
    centers = (np.arange(BINS) + 0.5) * 2 * math.pi / BINS
    ext = np.r_[centers[known] - 2 * math.pi, centers[known], centers[known] + 2 * math.pi]
    values = np.tile(bins[known], 3)
    return lambda theta: np.interp(theta % (2 * math.pi), ext, values)


def _vertex_normals(verts, faces, normals, moved):
    """Area-weighted normals of the moved vertices from their own faces.

    Duplicated backfaces have reversed winding, so their sums point inward already.
    """
    corners = verts[faces]
    face_normals = np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0])
    total = np.zeros_like(verts)
    for k in range(3):
        np.add.at(total, faces[:, k], face_normals)
    result = normals.copy()
    valid = moved & (np.linalg.norm(total, axis=1) > 1e-14)
    result[valid] = _unit(total[valid])
    return result


def repair_streams(meshes, role):
    """Return repaired position/normal/tangent streams and a per-joint report.

    `meshes` hold the native streams of every rendered trimesh (see NativeTangentSeams).
    """
    interface = [is_interface(mesh["streams"][POSITION], role) for mesh in meshes]
    if not any(interface) or all(interface):
        raise ValueError("Expected authored interface and source trimeshes")
    positions = [mesh["streams"][POSITION].astype(float) for mesh in meshes]
    moved = [np.zeros(len(p), bool) for p in positions]
    report = []
    for joint in JOINTS[role]:
        source = np.vstack([p for p, flag in zip(positions, interface) if not flag])
        d = _distance(source, joint)
        radius = _radius(source)
        shoulder = (d >= TRANSITION - 0.0005) & (d <= TRANSITION + SHOULDER_SAMPLE)
        rim = np.abs(d) < PLANE
        if not shoulder.any() or not rim.any():
            raise ValueError(f"Joint {joint[0]} lacks a shoulder or cut rim")
        target = _profile(np.arctan2(source[shoulder, 2], source[shoulder, 0]), radius[shoulder])
        rim_before = float(radius[rim].max())
        rim_moves = {}
        for index, flag in enumerate(interface):
            if flag:
                continue
            verts = positions[index]
            dm = _distance(verts, joint)
            current = _radius(verts)
            wanted = target(np.arctan2(verts[:, 2], verts[:, 0]))
            grow = (dm > -PLANE) & (dm < TRANSITION - 1e-6) & (current < wanted)
            new = verts.copy()
            scale = wanted[grow] / np.maximum(current[grow], 1e-9)
            new[grow, 0] *= scale
            new[grow, 2] *= scale
            for i in np.flatnonzero(grow & (np.abs(dm) < PLANE)):
                rim_moves[verts[i].astype("<f4").tobytes()] = new[i]
            positions[index] = new
            moved[index] |= grow
        # Cut caps are fans whose rim corners are the original source rim points.
        widened = 0
        for index, flag in enumerate(interface):
            if not flag:
                continue
            verts = positions[index]
            faces = meshes[index]["faces"]
            dm = _distance(verts, joint)
            keys = [v.astype("<f4").tobytes() for v in verts]
            on_rim = np.array([key in rim_moves for key in keys])
            cap = (np.abs(dm[faces]) < PLANE).all(axis=1) & (on_rim[faces].sum(axis=1) >= 2)
            corners = np.unique(faces[cap])
            targets = corners[on_rim[corners]]
            if np.isin(faces[~cap], targets).any():
                raise ValueError("A cut-cap rim vertex is shared with sleeve geometry")
            new = verts.copy()
            for i in targets:
                new[i] = rim_moves[keys[i]]
            positions[index] = new
            moved[index][targets] = True
            widened += len(targets)
        after = np.vstack([p for p, flag in zip(positions, interface) if not flag])
        report.append({"y": joint[0], "rim_radius_before": round(rim_before, 4),
                       "rim_radius_after": round(float(_radius(after)[rim].max()), 4),
                       "shoulder_radius": round(float(radius[shoulder].max()), 4),
                       "cap_corners_widened": widened})
    results = []
    for mesh, verts, flags, flag in zip(meshes, positions, moved, interface):
        normals = mesh["streams"][NORMAL].astype(float)
        tangents = mesh["streams"][TANGENT].astype(float)
        if not flag and flags.any():
            normals = _vertex_normals(verts, mesh["faces"].astype(int), normals, flags)
            tangent = tangents[flags] - normals[flags] * np.sum(tangents[flags] * normals[flags], axis=1, keepdims=True)
            if np.any(np.linalg.norm(tangent, axis=1) < 1e-6):
                raise ValueError("Moved tangent became parallel to its normal")
            tangents[flags] = _unit(tangent)
        results.append({"positions": verts, "normals": normals, "tangents": tangents, "moved": flags})
    return results, report


def mesh_positions(data):
    """Vertex positions of every trimesh in a compiled model, without layout limits."""
    if data[:4] != bytes(4):
        raise ValueError("Expected a compiled native model")
    raw = 12 + struct.unpack_from("<I", data, 4)[0]
    result, pending = [], [struct.unpack_from("<I", data, 12 + 72)[0]]
    while pending:
        node = 12 + pending.pop()
        children, count = struct.unpack_from("<II", data, node + 72)
        pending.extend(struct.unpack_from("<I", data, 12 + children + i * 4)[0] for i in range(count))
        if struct.unpack_from("<I", data, node + 108)[0] & 0x20:
            mesh = node + 112
            vertices = struct.unpack_from("<H", data, mesh + 448)[0]
            start = struct.unpack_from("<i", data, mesh + POSITION)[0]
            if vertices and start >= 0:
                result.append(np.frombuffer(data, "<f4", vertices * 3, raw + start).reshape(-1, 3).astype(float))
    return result


def repair_bytes(before, role):
    """Repair a compiled part; returns (bytes, report) or (before, None) when unaffected."""
    if not joint_constrictions(mesh_positions(before), role):
        return before, None
    raw, headers = NativeTangentSeams.mesh_headers(before)
    results, report = repair_streams(headers, role)
    after = bytearray(before)
    allowed = set(range(8, 12))

    def append(values):
        after.extend(bytes((-len(after)) % 4))
        offset = len(after) - raw
        after.extend(np.asarray(values, dtype="<f4").tobytes())
        return offset

    for info, result in zip(headers, results):
        if not result["moved"].any():
            continue
        header = info["header"]
        for field, key in ((POSITION, "positions"), (NORMAL, "normals"), (TANGENT, "tangents")):
            # Raw blocks may be shared after compaction, so write fresh ones.
            struct.pack_into("<I", after, header + field, append(result[key]))
            allowed.update(range(header + field, header + field + 4))
        verts = result["positions"].astype("<f4").astype(float)
        faces = info["faces"].astype(int)
        touched = np.flatnonzero(result["moved"][faces].any(axis=1))
        for face in touched:
            a, b, c = verts[faces[face]]
            normal = np.cross(b - a, c - a)
            length = np.linalg.norm(normal)
            if length < 1e-14:
                raise ValueError("Repair produced a degenerate triangle")
            normal /= length
            offset = info["face_offset"] + face * 32
            struct.pack_into("<4f", after, offset, *normal, -float(normal @ a))
            allowed.update(range(offset, offset + 16))
        used = verts[np.unique(faces)]
        average = np.asarray(struct.unpack_from("<3f", before, header + 48))
        struct.pack_into("<3f", after, header + 20, *used.min(axis=0))
        struct.pack_into("<3f", after, header + 32, *used.max(axis=0))
        struct.pack_into("<f", after, header + 44, float(np.linalg.norm(used - average, axis=1).max()))
        allowed.update(range(header + 20, header + 48))
    struct.pack_into("<I", after, 8, len(after) - raw)
    changed = {i for i, (old, new) in enumerate(zip(before, after)) if old != new}
    if not changed <= allowed:
        raise ValueError("Joint repair changed unrelated native metadata")
    after, _ = NativeTangentSeams.compact_model_bytes(bytes(after))
    validate(before, after, role)
    return after, report


def _corner_rows(info, faces):
    index = info["faces"][faces].reshape(-1)
    rows = np.hstack([info["streams"][field][index] for field, _ in NativeTangentSeams.STREAMS])
    return rows[np.lexsort(rows.T[::-1])]


def validate(before, after, role):
    """Unmoved corners stay byte-identical, UVs never change, frames stay valid."""
    _, old_meshes = NativeTangentSeams.mesh_headers(before)
    _, new_meshes = NativeTangentSeams.mesh_headers(after)
    if len(old_meshes) != len(new_meshes):
        raise ValueError("Rendered trimesh count changed")
    for old, new in zip(old_meshes, new_meshes):
        if len(old["faces"]) != len(new["faces"]):
            raise ValueError("Triangle count changed")
        old_corners = old["streams"][POSITION][old["faces"]]
        new_corners = new["streams"][POSITION][new["faces"]]
        same = (old_corners == new_corners).all(axis=(1, 2))
        if not np.array_equal(old["streams"][UV][old["faces"]], new["streams"][UV][new["faces"]]):
            raise ValueError("Joint repair changed rendered UV corners")
        if not np.array_equal(_corner_rows(old, same), _corner_rows(new, same)):
            raise ValueError("Joint repair changed corners outside the joint bands")
        if not np.array_equal(old_corners[..., 1], new_corners[..., 1]):
            raise ValueError("Joint repair moved vertices along the staff axis")
        used = np.unique(new["faces"])
        normals, tangents = new["streams"][NORMAL][used], new["streams"][TANGENT][used]
        if np.max(np.abs(np.linalg.norm(normals, axis=1) - 1)) > 1e-4 or \
           np.max(np.abs(np.linalg.norm(tangents, axis=1) - 1)) > 1e-4 or \
           np.max(np.abs(np.sum(normals * tangents, axis=1))) > 1e-4 or \
           not set(new["streams"][SIGN][used, 0]) <= {-1.0, 1.0}:
            raise ValueError("Invalid native corner frames after joint repair")
    remaining = joint_constrictions([m["streams"][POSITION] for m in new_meshes], role)
    if remaining:
        raise ValueError(f"Joint constriction remains: {remaining}")


def run(sw_weapon, output, models=None):
    sw_weapon, output = Path(sw_weapon).resolve(), Path(output).resolve()
    if output.exists() and any(output.iterdir()):
        raise FileExistsError("Use a fresh output directory")
    names = models or sorted(path.stem for path in sw_weapon.glob("wdbqs_*.mdl")
                             if MODEL.fullmatch(path.stem) and path.read_bytes()[:4] == bytes(4))
    output.mkdir(parents=True, exist_ok=True)
    reports = {}
    for name in names:
        after, report = repair_bytes((sw_weapon / (name + ".mdl")).read_bytes(), role_of(name))
        if report is None:
            continue
        (output / (name + ".mdl")).write_bytes(after)
        reports[name] = report
    (output / "joint-repair.json").write_text(json.dumps(reports, indent=2) + "\n")
    return reports


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sw-weapon", required=True, help="SWLOR_Haks/sw_weapon")
    parser.add_argument("--output", required=True, help="Fresh local output directory for repaired models")
    parser.add_argument("models", nargs="*", help="Optional wdbqs_[tmb]_NNN models (default: every constricted part)")
    args = parser.parse_args()
    reports = run(args.sw_weapon, args.output, args.models)
    print(f"Repaired {len(reports)} staff part models: {', '.join(reports)}")


if __name__ == "__main__":
    main()
