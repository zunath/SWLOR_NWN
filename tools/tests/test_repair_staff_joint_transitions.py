"""Quarterstaff parts must meet at full width instead of pinching at every joint.

The asset audit needs the SWLOR_Haks sw_weapon folder; the repair tests use a
synthetic compiled part with an authored 25 mm transition, cut caps and sleeves.
"""
import math
from pathlib import Path
import struct
import sys
import unittest

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import NativeTangentSeams as seams
import RepairStaffJointTransitions as repair

WEAPONS = ROOT / "SWLOR_Haks/sw_weapon"
SEGMENTS = 24
SHOULDER, ENVELOPE = 0.05, 0.010


def ring(y, radius, offset=0.0):
    angles = 2 * math.pi * (np.arange(SEGMENTS) + offset) / SEGMENTS
    return np.column_stack([radius * np.cos(angles), np.full(SEGMENTS, y), radius * np.sin(angles)])


def tube(joint, side):
    """Pinched source section: full width from 25 mm in, linear to the envelope at the cut."""
    depths = (0.0, 0.002, 0.01, 0.025, 0.1, 0.2)
    rings = [ring(joint + side * d, ENVELOPE + (SHOULDER - ENVELOPE) * min(d / 0.025, 1)) for d in depths]
    positions = np.vstack(rings)
    faces = []
    for r in range(len(rings) - 1):
        for i in range(SEGMENTS):
            a, b = r * SEGMENTS + i, r * SEGMENTS + (i + 1) % SEGMENTS
            c, d = a + SEGMENTS, b + SEGMENTS
            faces += [[a, c, b], [b, c, d]] if side > 0 else [[a, b, c], [b, d, c]]
    return positions, np.asarray(faces)


def cap_and_sleeve(joint, side):
    """Authored interface: the cut-cap fan on the source rim plus an inner sleeve."""
    rim = ring(joint, ENVELOPE)
    sleeve_near, sleeve_far = ring(joint, ENVELOPE * 0.98, 0.5), ring(joint + side * 0.002, ENVELOPE * 0.98, 0.5)
    positions = np.vstack([[0, joint, 0], rim, sleeve_near, sleeve_far])
    faces = []
    for i in range(SEGMENTS):
        j = (i + 1) % SEGMENTS
        faces.append([0, 1 + i, 1 + j] if side > 0 else [0, 1 + j, 1 + i])
        a, b = 1 + SEGMENTS + i, 1 + SEGMENTS + j
        faces += [[a, b, a + SEGMENTS], [b, b + SEGMENTS, a + SEGMENTS]]
    return positions, np.asarray(faces)


def combine(parts):
    positions, faces = [], []
    for p, f in parts:
        faces.append(f + sum(len(x) for x in positions))
        positions.append(p)
    return np.vstack(positions), np.vstack(faces)


def frames(positions, faces):
    normals = np.zeros_like(positions)
    corners = positions[faces]
    face_normals = np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0])
    for k in range(3):
        np.add.at(normals, faces[:, k], face_normals)
    normals /= np.linalg.norm(normals, axis=1, keepdims=True)
    helper = np.where(np.abs(normals[:, 1:2]) > 0.9, [[1.0, 0, 0]], [[0, 1.0, 0]])
    tangents = helper - normals * np.sum(helper * normals, axis=1, keepdims=True)
    return normals, tangents / np.linalg.norm(tangents, axis=1, keepdims=True)


def compiled(meshes):
    """A minimal static rigid model accepted by NativeTangentSeams.mesh_headers."""
    nodes = 232 + 112
    model = bytearray(nodes + 624 * len(meshes) + 4 * len(meshes) + sum(len(f) * 32 + 8 for _, f in meshes))
    raw = bytearray()

    def u(offset, value):
        struct.pack_into("<I", model, offset, value)
    children = nodes + 624 * len(meshes)
    tail = children + 4 * len(meshes)
    u(72, 232)
    u(232 + 72, children)
    u(232 + 76, len(meshes))
    u(232 + 108, 1)
    for index, (positions, faces) in enumerate(meshes):
        node = nodes + 624 * index
        mesh = node + 112
        u(children + 4 * index, node)
        u(node + 108, 33)
        u(mesh + 108, 1)
        u(mesh + 436, 3)
        name = b"interface\0" if index else b"source\0"
        for field in (120, 312):
            model[mesh + field:mesh + field + len(name)] = name
        for field in (456, 460, 464, 472, 476, 480, 484, 492):
            struct.pack_into("<i", model, mesh + field, -1)
        struct.pack_into("<H", model, mesh + 448, len(positions))
        normals, tangents = frames(positions, faces)
        uv = np.column_stack([np.arctan2(positions[:, 2], positions[:, 0]), positions[:, 1]])
        for field, values in ((444, positions), (452, uv), (468, normals), (488, tangents), (496, np.ones((len(positions), 1)))):
            raw.extend(bytes((-len(raw)) % 4))
            u(mesh + field, len(raw))
            raw.extend(np.asarray(values, "<f4").tobytes())
        u(mesh + 8, tail)
        u(mesh + 12, len(faces))
        for i, face in enumerate(faces):
            struct.pack_into("<3H", model, tail + i * 32 + 26, *face)
        tail += len(faces) * 32
        u(mesh + 404, tail)
        u(mesh + 408, 1)
        u(tail, len(faces) * 3)
        u(mesh + 416, tail + 4)
        u(mesh + 420, 1)
        raw.extend(bytes((-len(raw)) % 4))
        u(tail + 4, len(raw))
        raw.extend(np.asarray(faces, "<u2").tobytes())
        tail += 8
    return struct.pack("<III", 0, len(model), len(raw)) + bytes(model) + bytes(raw)


def pinched_top():
    source = combine([tube(1.2, 1), tube(-0.6, -1)])
    interface = combine([cap_and_sleeve(1.2, 1), cap_and_sleeve(-0.6, -1)])
    return compiled([source, interface])


def split(data, reference=None):
    """(source, interface) trimeshes, classified on the unrepaired reference model."""
    _, meshes = seams.mesh_headers(data)
    _, original = seams.mesh_headers(reference or data)
    interface = [repair.is_interface(m["streams"][repair.POSITION], "t") for m in original]
    return meshes[interface.index(False)], meshes[interface.index(True)]


class StaffJointRepairTests(unittest.TestCase):
    def test_pinched_joints_widen_to_the_shoulder_and_stay_capped(self):
        before = pinched_top()
        self.assertEqual(2, len(repair.joint_constrictions(repair.mesh_positions(before), "t")))
        after, report = repair.repair_bytes(before, "t")
        self.assertEqual([], repair.joint_constrictions(repair.mesh_positions(after), "t"))
        self.assertEqual({-0.6, 1.2}, {joint["y"] for joint in report})
        self.assertTrue(all(joint["cap_corners_widened"] == SEGMENTS for joint in report))
        old_source, _ = split(before)
        source, interface = split(after, before)
        corners = source["streams"][repair.POSITION][source["faces"]].reshape(-1, 3)
        old = old_source["streams"][repair.POSITION][old_source["faces"]].reshape(-1, 3)
        np.testing.assert_array_equal(old[:, 1], corners[:, 1])
        for joint, side in ((1.2, 1), (-0.6, -1)):
            d = side * (corners[:, 1] - joint)
            radius = np.hypot(corners[:, 0], corners[:, 2])
            np.testing.assert_allclose(radius[d < 0.025 + 1e-6], SHOULDER, rtol=1e-3)
            np.testing.assert_array_equal(old[d > 0.025 + 1e-6], corners[d > 0.025 + 1e-6])
            # The widened cut cap closes exactly on the moved source rim.
            cap = interface["streams"][repair.POSITION]
            on_plane = cap[np.abs(cap[:, 1] - joint) < 1e-6]
            rim = {tuple(p) for p in corners[np.abs(d) < 1e-6].round(6)}
            self.assertEqual(rim, {tuple(p) for p in on_plane.round(6) if np.hypot(p[0], p[2]) > 0.02})
        # Moved band normals turn radial and keep pointing outward.
        positions = source["streams"][repair.POSITION]
        normals = source["streams"][repair.NORMAL]
        depth = np.minimum(np.abs(positions[:, 1] - 1.2), np.abs(positions[:, 1] + 0.6))
        moved = depth < 0.02
        self.assertLess(np.abs(normals[moved, 1]).max(), 1e-3)
        self.assertGreater((normals[moved, 0] * positions[moved, 0] + normals[moved, 2] * positions[moved, 2]).min(), 0)
        # The inner sleeves stay at the connector envelope.
        sleeve = np.hypot(interface["streams"][repair.POSITION][:, 0], interface["streams"][repair.POSITION][:, 2])
        self.assertTrue(np.any(np.abs(sleeve - ENVELOPE * 0.98) < 1e-6))

    def test_repair_is_idempotent_and_skips_parts_without_a_constriction(self):
        after, _ = repair.repair_bytes(pinched_top(), "t")
        again, report = repair.repair_bytes(after, "t")
        self.assertIsNone(report)
        self.assertEqual(after, again)

    def test_validation_rejects_changed_corners_outside_the_bands(self):
        before = pinched_top()
        after, _ = repair.repair_bytes(before, "t")
        raw, _ = seams.mesh_headers(after)
        tampered = bytearray(after)
        source, _ = split(after, before)
        offset = raw + struct.unpack_from("<i", after, source["header"] + repair.UV)[0]
        far = int(np.flatnonzero(source["streams"][repair.POSITION][:, 1] > 1.35)[0])
        struct.pack_into("<f", tampered, offset + far * 8, 0.5)
        with self.assertRaisesRegex(ValueError, "UV"):
            repair.validate(before, bytes(tampered), "t")

    def test_roles_follow_the_native_staff_partition(self):
        self.assertEqual("m", repair.role_of("wdbqs_m_021"))
        with self.assertRaises(ValueError):
            repair.role_of("wdbsw_m_021")


@unittest.skipUnless(WEAPONS.is_dir(), "SWLOR_Haks sw_weapon is not checked out")
class ShippedStaffJointAuditTests(unittest.TestCase):
    def test_no_quarterstaff_part_pinches_at_a_joint(self):
        checked, pinched = 0, {}
        for path in sorted(WEAPONS.glob("wdbqs_*.mdl")):
            if not repair.MODEL.fullmatch(path.stem):
                continue
            data = path.read_bytes()
            if data[:4] != bytes(4):
                continue
            checked += 1
            found = repair.joint_constrictions(repair.mesh_positions(data), repair.role_of(path.stem))
            if found:
                pinched[path.stem] = found
        self.assertGreater(checked, 50)
        self.assertEqual({}, pinched, "Rim radius below 75% of the shoulder 25 mm inside: (y, rim, shoulder)")


if __name__ == "__main__":
    unittest.main()
