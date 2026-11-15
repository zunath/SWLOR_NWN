"""Vibroblade parts must not carry joint sleeves that stick out as bars.

The asset audit needs the SWLOR_Haks sw_weapon folder; the edit tests use synthetic
compiled parts with an off-axis source section, flat cut caps and centred sleeves.
"""
import math
from pathlib import Path
import sys
import tempfile
import unittest

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
sys.path.insert(0, str(Path(__file__).resolve().parent))
import FitBladeJointInterfaces as fit
import NativeTangentSeams as seams
from test_repair_staff_joint_transitions import compiled

WEAPONS = ROOT / "SWLOR_Haks/sw_weapon"
SEGMENTS = 16
CENTRE = np.array([-0.02, 0.0])  # Fitted parts sit 20 mm off the stock axis.
GRIP = (0.01, 0.012)
SLEEVE = (0.024, 0.036)
DEPTH = 0.007


def ring(y, radii, centre=CENTRE):
    angles = 2 * math.pi * np.arange(SEGMENTS) / SEGMENTS
    return np.column_stack([centre[0] + radii[0] * np.cos(angles), np.full(SEGMENTS, y),
                            centre[1] + radii[1] * np.sin(angles)])


def tube(low, high, radii, centre=CENTRE, closed=False):
    positions = np.vstack([ring(low, radii, centre), ring(high, radii, centre)])
    faces = []
    for i in range(SEGMENTS):
        a, b = i, (i + 1) % SEGMENTS
        faces += [[a, a + SEGMENTS, b], [b, a + SEGMENTS, b + SEGMENTS]]
    if closed:
        positions = np.vstack([positions, [[centre[0], low, centre[1]], [centre[0], high, centre[1]]]])
        for i in range(SEGMENTS):
            j = (i + 1) % SEGMENTS
            faces += [[2 * SEGMENTS, j, i], [2 * SEGMENTS + 1, SEGMENTS + i, SEGMENTS + j]]
    return positions, np.asarray(faces)


def cap(y, side):
    """Flat cut cap on the source rim, facing out of the part."""
    rim = ring(y, GRIP)
    positions = np.vstack([[CENTRE[0], y, CENTRE[1]], rim])
    faces = [[0, 1 + i, 1 + (i + 1) % SEGMENTS] if side > 0 else [0, 1 + (i + 1) % SEGMENTS, 1 + i]
             for i in range(SEGMENTS)]
    return positions, np.asarray(faces)


def combine(parts):
    positions, faces = [], []
    for p, f in parts:
        faces.append(f + sum(len(x) for x in positions))
        positions.append(p)
    return np.vstack(positions), np.vstack(faces)


def middle():
    source = tube(0.085, 0.135, GRIP)
    interface = combine([cap(0.085, 1), cap(0.135, -1),
                         tube(0.085, 0.085 + DEPTH, SLEEVE, np.zeros(2), True),
                         tube(0.135 - DEPTH, 0.135, SLEEVE, np.zeros(2), True)])
    return compiled([source, interface])


def short_bottom():
    """A grip that stops 3.5 mm below the joint; its sleeve is all that closes the slot."""
    source = tube(-0.1, 0.0815, GRIP, closed=True)
    interface = tube(0.085 - DEPTH, 0.085, SLEEVE, np.zeros(2), True)
    return compiled([source, interface])


def meshes(data):
    return seams.mesh_headers(data)[1]


def interface_positions(data, role):
    found = meshes(data)
    return found[fit.interface_index(found, role)]["streams"][fit.POSITION]


class BladeJointInterfaceTests(unittest.TestCase):
    def test_sleeves_leave_and_caps_stay(self):
        before = middle()
        self.assertTrue(fit.interface_problems(meshes(before), "wswls_m"))
        after, report = fit.fit_bytes(before, "wswls_m")
        self.assertEqual(4 * SEGMENTS, report["0.085"]["sleeve_triangles_removed"])
        self.assertEqual(4 * SEGMENTS, report["0.135"]["sleeve_triangles_removed"])
        self.assertEqual([], fit.interface_problems(meshes(after), "wswls_m"))
        old, new = meshes(before), meshes(after)
        index = fit.interface_index(old, "wswls_m")
        source = 1 - index
        self.assertTrue(np.array_equal(fit._corner_rows(old[source]), fit._corner_rows(new[source])))
        for joint in fit.JOINTS["wswls_m"]:
            self.assertTrue(np.array_equal(fit._cap_rows(old[index], "wswls_m", joint),
                                           fit._cap_rows(new[index], "wswls_m", joint)))
        positions = interface_positions(after, "wswls_m")
        self.assertLessEqual(positions[:, 0].max(), CENTRE[0] + GRIP[0] + 1e-6)

    def test_fitting_is_idempotent(self):
        after, _ = fit.fit_bytes(middle(), "wswls_m")
        self.assertEqual((after, None), fit.fit_bytes(after, "wswls_m"))

    def test_a_short_grip_keeps_a_collar_of_its_own_section(self):
        before = short_bottom()
        after, report = fit.fit_bytes(before, "wswls_b")
        self.assertEqual(3.5, report["0.085"]["source_end_gap_mm"])
        self.assertEqual([], fit.interface_problems(meshes(after), "wswls_b"))
        collar = interface_positions(after, "wswls_b")
        self.assertAlmostEqual(0.085, collar[:, 1].max(), places=5)
        self.assertLessEqual(collar[:, 1].min(), 0.0815)
        for axis, (centre, radius) in enumerate(zip(CENTRE, GRIP)):
            values = collar[:, [0, 2][axis]]
            self.assertAlmostEqual(centre - radius, values.min(), places=4)
            self.assertAlmostEqual(centre + radius, values.max(), places=4)

    def test_audit_reports_a_bar_and_an_uncapped_cut(self):
        source = tube(0.085, 0.135, GRIP)
        sleeve_only = compiled([source, combine([tube(0.085, 0.085 + DEPTH, SLEEVE, np.zeros(2), True),
                                                 cap(0.135, -1)])])
        problems = fit.interface_problems(meshes(sleeve_only), "wswls_m")
        self.assertIn("cut at y=0.085 is not capped", problems)
        self.assertIn("sleeve at y=0.085 reaches past the part section", problems)

    def test_audit_reports_a_cap_that_leaves_the_cut_open(self):
        positions, faces = cap(0.085, 1)
        partial = compiled([tube(0.085, 0.135, GRIP), combine([(positions[:5], faces[:3]), cap(0.135, -1)])])
        self.assertIn("cap at y=0.085 leaves part of the cut open", fit.interface_problems(meshes(partial), "wswls_m"))

    def test_a_sleeve_inside_its_cut_stays(self):
        inner = (GRIP[0] * 0.8, GRIP[1] * 0.8)
        before = compiled([tube(0.085, 0.135, GRIP),
                           combine([cap(0.085, 1), cap(0.135, -1),
                                    # Off the cap's centre, as authored sleeves are, so the two stay apart.
                                    tube(0.085, 0.085 + DEPTH, inner, CENTRE + [0.001, 0.0], closed=True),
                                    tube(0.135 - DEPTH, 0.135, SLEEVE, np.zeros(2), True)])])
        after, report = fit.fit_bytes(before, "wswls_m")
        self.assertEqual(0, report["0.085"]["sleeve_triangles_removed"])
        self.assertEqual(4 * SEGMENTS, report["0.135"]["sleeve_triangles_removed"])
        self.assertEqual([], fit.interface_problems(meshes(after), "wswls_m"))
        self.assertLess(interface_positions(after, "wswls_m")[:, 1].min(), 0.085 + DEPTH + 1e-6)

    def test_a_partial_cap_is_refused_when_its_sleeve_leaves(self):
        positions, faces = cap(0.085, 1)
        before = compiled([tube(0.085, 0.135, GRIP),
                           combine([(positions[:5], faces[:3]), cap(0.135, -1),
                                    tube(0.085, 0.085 + DEPTH, SLEEVE, np.zeros(2), True)])])
        with self.assertRaisesRegex(ValueError, "leaves part of the cut open"):
            fit.fit_bytes(before, "wswls_m")

    def test_a_paired_short_end_keeps_its_own_outline(self):
        # A twinblade top: the lower blade stops 3.5 mm short of its joint; the upper one,
        # past the opposite joint, is wider and must not shape the lower collar.
        low, high = fit.PAIRED["wdbsw"][0], fit.PAIRED["wdbsw"][3]
        lower = tube(low - 0.1, low - 0.0035, GRIP, closed=True)
        upper = tube(high, high + 0.1, (0.03, 0.03), closed=True)
        wide_cap = ring(high, (0.03, 0.03))
        upper_cap = (np.vstack([[CENTRE[0], high, CENTRE[1]], wide_cap]),
                     np.asarray([[0, 1 + i, 1 + (i + 1) % SEGMENTS] for i in range(SEGMENTS)]))
        sleeve = tube(low - DEPTH, low, SLEEVE, np.zeros(2), True)
        before = compiled([combine([lower, upper]), combine([upper_cap, sleeve])])
        after, report = fit.fit_bytes(before, "wdbsw_t")
        self.assertGreater(report[str(low)]["collar_triangles"], 0)
        collar = interface_positions(after, "wdbsw_t")
        collar = collar[collar[:, 1] < low + 1e-6]
        self.assertLess(np.abs(collar[:, 0] - CENTRE[0]).max(), GRIP[0] + 1e-4)
        self.assertEqual([], fit.interface_problems(meshes(after), "wdbsw_t"))

    def test_audit_reports_a_cap_wider_than_a_short_end(self):
        grip = tube(-0.1, 0.0815, GRIP, closed=True)
        wide = cap(0.085, -1)
        wide = (wide[0] * [3.0, 1.0, 3.0], wide[1])
        problems = fit.interface_problems(meshes(compiled([grip, combine([wide, tube(0.085 - DEPTH, 0.085, GRIP, closed=True)])])), "wswls_b")
        self.assertIn("cap at y=0.085 reaches past the part section", problems)

    def test_a_missing_source_folder_is_an_error(self):
        with tempfile.TemporaryDirectory() as temporary:
            with self.assertRaises(FileNotFoundError):
                fit.run(Path(temporary) / "missing", Path(temporary) / "out")

    def test_roles_follow_the_native_part_partitions(self):
        self.assertEqual("wswls_t", fit.role_of("wswls_t_011"))
        self.assertEqual(((0.111, 1), (0.17, -1)), fit.JOINTS[fit.role_of("wswgs_m_021")])
        # Paired roles: the twinblade middle holds both bands next to the central grip.
        self.assertEqual(4, len(fit.JOINTS[fit.role_of("wdbsw_m_021")]))
        with self.assertRaises(ValueError):
            fit.role_of("wbwpi_m_021")


@unittest.skipUnless(WEAPONS.is_dir(), "SWLOR_Haks sw_weapon is not checked out")
class ShippedBladeJointAuditTests(unittest.TestCase):
    # Static parts of these families without an authored interface: blanks and whole
    # replacement models that were never cut. A part leaving or joining this list means
    # its interface went unrecognised (or was newly authored): check it, then update.
    UNCUT = frozenset({
        "wdbsw_b_015", "wdbsw_m_015", "wdbsw_m_019", "wdbsw_m_029", "wdbsw_t_015",
        "wplss_b_017", "wplss_b_018", "wplss_b_029", "wplss_b_039", "wplss_b_049", "wplss_b_071",
        "wplss_b_141", "wplss_b_142", "wplss_b_143", "wplss_b_181", "wplss_m_181", "wplss_t_071",
        "wswdg_b_031", "wswdg_b_032", "wswdg_b_033", "wswdg_b_034", "wswdg_b_035", "wswdg_b_161",
        "wswdg_b_162", "wswdg_m_161", "wswdg_m_162",
        "wswgs_b_015", "wswgs_b_071", "wswgs_b_105", "wswgs_m_015", "wswgs_m_048", "wswgs_t_071", "wswgs_t_105",
        "wswls_b_091", "wswls_m_016", "wswls_t_016", "wswls_t_091",
    })

    def test_no_fitted_weapon_part_carries_a_stray_joint_sleeve(self):
        checked, problems, uncut = 0, {}, set()
        for path in sorted(WEAPONS.glob("*.mdl")):
            if not fit.MODEL.fullmatch(path.stem):
                continue
            data = path.read_bytes()
            if not fit.supported(data):
                continue
            found = meshes(data)
            if fit.interface_index(found, fit.role_of(path.stem)) is None:
                uncut.add(path.stem)
                continue
            checked += 1
            issues = fit.interface_problems(found, fit.role_of(path.stem))
            if issues:
                problems[path.stem] = issues
        self.assertGreater(checked, 300)
        self.assertEqual(set(self.UNCUT), uncut)
        self.assertEqual({}, problems)


if __name__ == "__main__":
    unittest.main()
