"""Fitted weapon parts must meet every other part on the stock joint axis, and a guard must
belong to its middle, not to the top or bottom a stock cut sliced it into.

The asset audits need the SWLOR_Haks sw_weapon folder; the edit tests use synthetic
compiled parts: a grip and guard cut through at the vibroblade joint (Y 0.085).
"""
from pathlib import Path
import struct
import sys
import unittest

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
sys.path.insert(0, str(Path(__file__).resolve().parent))
import AlignWeaponPartJoints as align
import FitBladeJointInterfaces as fit
import NativeTangentSeams as seams
from test_fit_blade_joint_interfaces import GRIP, combine, tube
from test_repair_staff_joint_transitions import compiled

WEAPONS = ROOT / "SWLOR_Haks/sw_weapon"
OFF_AXIS = np.array([-0.02, 0.0])  # Where the fitted vibroblades sat before the fix.
GUARD = (0.03, 0.05)
LOW, HIGH = fit.JOINTS["wswls_m"][0][0], fit.JOINTS["wswls_m"][1][0]


def disc(y, radii, side, centre=OFF_AXIS):
    """A flat cap facing out of the part (side +1: the part lies above y)."""
    rim = tube(y, y, radii, centre)[0][:16]
    positions = np.vstack([[centre[0], y, centre[1]], rim])
    faces = [[0, 1 + i, 1 + (i + 1) % 16] if side > 0 else [0, 1 + (i + 1) % 16, 1 + i] for i in range(16)]
    return positions, np.asarray(faces)


def named(data, role, *textures):
    """Bind the source meshes to textures (in compile order) and the interface to another, as
    the shipped parts do."""
    found = seams.mesh_headers(data)[1]
    interface = fit.interface_index(found, role)
    data = bytearray(data)
    # The native node walk visits the compiled meshes last first.
    order = [i for i in reversed(range(len(found))) if i != interface]
    names = {index: texture for index, texture in zip(order, textures)}
    for index, mesh in enumerate(found):
        name = "interface" if index == interface else names[index]
        for field in (120, 312):
            data[mesh["header"] + field:mesh["header"] + field + 32] = name.encode().ljust(32, b"\0")
    return bytes(data)


def bottom(texture="steel", guard=True, centre=OFF_AXIS):
    """A grip up to Y 0.06 under a guard block that the joint at 0.085 cuts."""
    pieces = [tube(-0.1, 0.06, GRIP, centre, closed=True)]
    if guard:
        pieces.append(tube(0.06, LOW, GUARD, centre))
    source = combine(pieces)
    interface = disc(LOW, GUARD if guard else GRIP, -1, centre)
    return named(compiled([source, interface]), "wswls_b", texture)


def middle(texture="steel", centre=OFF_AXIS):
    """The rest of the guard block over the joint, and the blade core running on to 0.135."""
    source = combine([tube(LOW, 0.1, GUARD, centre), tube(LOW, HIGH, GRIP, centre)])
    interface = combine([disc(LOW, GUARD, 1, centre), disc(HIGH, GRIP, -1, centre)])
    return named(compiled([source, interface]), "wswls_m", texture)


OUTER_LOW, INNER_LOW, INNER_HIGH, OUTER_HIGH = fit.PAIRED["wdbsw"]
CENTRE = np.zeros(2)


def twin_end():
    """A twinblade top: both blades, and guard horns over the upper blade that the cut left on it."""
    source = combine([tube(OUTER_LOW - 0.1, OUTER_LOW, GRIP, CENTRE, closed=True),
                      tube(OUTER_HIGH, OUTER_HIGH + 0.1, GRIP, CENTRE, closed=True),
                      tube(OUTER_HIGH, OUTER_HIGH + 0.04, GUARD, CENTRE)])
    interface = combine([disc(OUTER_LOW, GRIP, -1, CENTRE), disc(OUTER_HIGH, GUARD, 1, CENTRE)])
    return named(compiled([source, interface]), "wdbsw_t", "steel")


def twin_bands():
    """A twinblade middle: grip bands in the texture every source shares, and its own guard over
    the upper band in the source's texture."""
    shared = combine([tube(OUTER_LOW, INNER_LOW, GRIP, CENTRE), tube(INNER_HIGH, OUTER_HIGH, GRIP, CENTRE)])
    own = tube(INNER_HIGH + 0.01, OUTER_HIGH, GUARD, CENTRE)
    interface = combine([disc(OUTER_LOW, GRIP, 1, CENTRE), disc(INNER_LOW, GRIP, -1, CENTRE),
                         disc(INNER_HIGH, GRIP, 1, CENTRE), disc(OUTER_HIGH, GUARD, -1, CENTRE)])
    return named(compiled([shared, own, interface]), "wdbsw_m", "grip", "steel")


def parts(**models):
    return {name: align.Part(name, data) for name, data in models.items()}


def meshes(data):
    return seams.mesh_headers(data)[1]


class AxisTests(unittest.TestCase):
    def test_a_source_moves_as_one_piece_onto_the_axis(self):
        found = parts(wswls_b_011=bottom(), wswls_m_011=middle(), wswls_b_021=bottom("brass", centre=np.zeros(2)))
        self.assertEqual([["wswls_b_011", "wswls_m_011"]], [group for group, _ in align.plan(found)])
        (group, offset), = align.plan(found)
        np.testing.assert_allclose(offset, -OFF_AXIS, atol=2e-4)

    def test_the_grip_defines_the_axis_not_a_wide_guard(self):
        part = align.Part("wswls_b_011", bottom())
        np.testing.assert_allclose(align.core_centre(*part.source_geometry()), OFF_AXIS, atol=2e-4)

    def test_translation_moves_positions_and_face_planes_only(self):
        before = middle()
        after = align.translate_bytes(before, -OFF_AXIS)
        for old, new in zip(meshes(before), meshes(after)):
            np.testing.assert_array_equal(old["faces"], new["faces"])
            for field, _ in seams.STREAMS:
                if field != fit.POSITION:
                    np.testing.assert_array_equal(old["streams"][field], new["streams"][field])
            np.testing.assert_allclose(new["streams"][fit.POSITION] - old["streams"][fit.POSITION],
                                       np.tile([0.02, 0.0, 0.0], (len(old["streams"][fit.POSITION]), 1)), atol=1e-6)
            for field in (20, 32, 48):  # Bounding box and average point follow the geometry.
                moved = np.subtract(struct.unpack_from("<3f", after, new["header"] + field),
                                    struct.unpack_from("<3f", before, old["header"] + field))
                np.testing.assert_allclose(moved, [0.02, 0.0, 0.0], atol=1e-6)
        self.assertEqual([], align.plan(parts(wswls_b_011=align.translate_bytes(bottom(), -OFF_AXIS),
                                              wswls_m_011=after)))

    def test_variants_in_one_texture_share_a_source_without_a_shared_cut(self):
        found = parts(wswls_b_011=bottom(guard=False), wswls_m_011=middle())
        self.assertEqual([["wswls_b_011", "wswls_m_011"]], align.sources(found))
        found = parts(wswls_b_011=bottom("brass", guard=False), wswls_m_011=middle())
        self.assertEqual([["wswls_b_011"], ["wswls_m_011"]], align.sources(found))


class GuardTests(unittest.TestCase):
    def test_a_guard_slice_goes_back_to_its_middle(self):
        found = parts(wswls_b_011=bottom(), wswls_m_011=middle())
        edited = align.give_back_guards(found, {"wswls_b_011": 0.05})
        grip, guard = meshes(edited["wswls_b_011"]), meshes(edited["wswls_m_011"])
        # The bottom keeps only its grip, closed by a collar of its own outline up to the joint.
        source = grip[1 - fit.interface_index(grip, "wswls_b")]["streams"][fit.POSITION]
        self.assertLessEqual(source[:, 1].max(), 0.06 + 1e-6)
        collar = grip[fit.interface_index(grip, "wswls_b")]["streams"][fit.POSITION]
        self.assertAlmostEqual(LOW, collar[:, 1].max(), places=5)
        self.assertAlmostEqual(0.06, collar[:, 1].min(), places=5)
        self.assertLessEqual(np.abs(collar[:, 0] - OFF_AXIS[0]).max(), GRIP[0] + 1e-4)
        # The middle holds the whole guard, and its cap shrinks to the blade core that ends there.
        index = fit.interface_index(guard, "wswls_m")
        source = guard[1 - index]["streams"][fit.POSITION]
        self.assertAlmostEqual(0.06, source[:, 1].min(), places=5)
        cap = guard[index]["streams"][fit.POSITION]
        cap = cap[np.abs(cap[:, 1] - LOW) < 1e-6]
        self.assertLessEqual(np.abs(cap[:, 0] - OFF_AXIS[0]).max(), GRIP[0] + 1e-4)
        for name, role in (("wswls_b_011", "wswls_b"), ("wswls_m_011", "wswls_m")):
            self.assertEqual([], fit.interface_problems(meshes(edited[name]), role), name)
        # Every source triangle survives, on one side or the other.
        def source_faces(found, role):
            return sum(len(m["faces"]) for i, m in enumerate(found) if i != fit.interface_index(found, role))
        self.assertEqual(source_faces(meshes(bottom()), "wswls_b") + source_faces(meshes(middle()), "wswls_m"),
                         source_faces(grip, "wswls_b") + source_faces(guard, "wswls_m"))

    def test_a_guard_slice_without_its_middle_is_dropped(self):
        found = parts(wswls_b_011=bottom(), wswls_m_011=middle("brass"))
        edited = align.give_back_guards(found, {"wswls_b_011": 0.05})
        self.assertEqual(["wswls_b_011"], sorted(edited))
        self.assertEqual([], fit.interface_problems(meshes(edited["wswls_b_011"]), "wswls_b"))

    def test_a_listed_part_without_a_guard_slice_is_an_error(self):
        found = parts(wswls_b_011=bottom(guard=False), wswls_m_011=middle())
        with self.assertRaisesRegex(ValueError, "no guard piece"):
            align.give_back_guards(found, {"wswls_b_011": 0.05})

    def test_alignment_is_idempotent(self):
        found = parts(wswls_b_011=bottom(), wswls_m_011=middle())
        original = dict(align.GUARD_PIECES)
        try:
            align.GUARD_PIECES.clear()
            align.GUARD_PIECES["wswls_b_011"] = 0.05
            changed, _ = align.align(found)
            self.assertEqual(["wswls_b_011", "wswls_m_011"], sorted(changed))
            again, report = align.align(parts(**changed))
            self.assertEqual({}, again)
        finally:
            align.GUARD_PIECES.clear()
            align.GUARD_PIECES.update(original)

    def test_a_twinblade_end_gives_its_horns_to_the_band_in_its_own_texture(self):
        found = parts(wdbsw_t_031=twin_end(), wdbsw_m_031=twin_bands())
        self.assertEqual((OUTER_HIGH, 1), align.guard_joint(found["wdbsw_t_031"], OUTER_HIGH + 0.05))
        self.assertEqual((OUTER_LOW, -1), align.guard_joint(found["wdbsw_t_031"], OUTER_LOW - 0.01))
        edited = align.give_back_guards(found, {"wdbsw_t_031": OUTER_HIGH + 0.05})
        end, bands = meshes(edited["wdbsw_t_031"]), meshes(edited["wdbsw_m_031"])
        self.assertEqual([], fit.interface_problems(end, "wdbsw_t"))
        self.assertEqual([], fit.interface_problems(bands, "wdbsw_m"))
        index = fit.interface_index(end, "wdbsw_t")
        self.assertLess(np.abs(end[1 - index]["streams"][fit.POSITION][:, 0]).max(), GUARD[0] - 1e-3)
        original = meshes(twin_bands())
        before = {align.texture(twin_bands(), m): len(m["faces"]) for m in original}
        after = {align.texture(edited["wdbsw_m_031"], m): len(m["faces"]) for m in bands}
        self.assertEqual(before["grip"], after["grip"])
        self.assertGreater(after["steel"], before["steel"])
        # The upper cap now covers only the blade that still ends at the joint.
        cap = bands[fit.interface_index(bands, "wdbsw_m")]["streams"][fit.POSITION]
        cap = cap[np.abs(cap[:, 1] - OUTER_HIGH) < 1e-6]
        self.assertLessEqual(np.abs(cap[:, 0]).max(), GRIP[0] + 1e-4)

    def test_a_surface_running_through_a_joint_has_no_rim_there(self):
        through = {"streams": {fit.POSITION: tube(0.06, 0.1, GUARD)[0]}, "faces": tube(0.06, 0.1, GUARD)[1]}
        self.assertEqual(0, len(fit.rim_points([through], (LOW, 1))))
        ending = combine([tube(0.06, LOW, GUARD), tube(LOW, 0.1, GUARD)])
        self.assertEqual(0, len(fit.rim_points([{"streams": {fit.POSITION: ending[0]}, "faces": ending[1]}], (LOW, 1))))
        cut = tube(LOW, 0.1, GUARD)
        self.assertEqual(32, len(fit.rim_points([{"streams": {fit.POSITION: cut[0]}, "faces": cut[1]}], (LOW, 1))))


@unittest.skipUnless(WEAPONS.is_dir(), "SWLOR_Haks sw_weapon is not checked out")
class ShippedJointAlignmentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.parts = align.load(WEAPONS)

    def test_every_fitted_source_is_on_the_stock_axis(self):
        self.assertGreater(len(self.parts), 300)
        off = {group[0]: [round(float(v) * 1000, 1) for v in offset] for group, offset in align.plan(self.parts)}
        self.assertEqual({}, off)

    def test_no_guard_piece_is_left_on_a_top_or_bottom(self):
        left = [name for name, entry in align.GUARD_PIECES.items() for bound in align.bounds(entry)
                if align.guard_pieces(self.parts[name], bound)[1].any()]
        self.assertEqual([], left)


if __name__ == "__main__":
    unittest.main()
