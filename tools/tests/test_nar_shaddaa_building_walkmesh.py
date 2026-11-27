"""Collision regressions for the [SWTOR] Building 002 tower (daf_sw012) on Nar Shaddaa.

Run with: python -m unittest discover -s tools/tests -p test_nar_shaddaa_building_walkmesh.py
These inspect the shipped PWK; an in-game walk check is still required.
"""
import json
import math
import unittest

from test_building_walkmesh import ROOT, assert_closed_mesh, intersections, read_mesh, roof_height_at, two_da

PWK = ROOT / "SWLOR_Haks/sw_plc_swtor/daf_sw012.pwk"
# Measured from daf_sw012.mdl: a 32.85 m square core with four buttresses per
# side (two wide, one narrow in the middle) that stop at local Z 15.5064.
CORE = 16.4232
BUTTRESS_TOP = 15.5064
WIDE_OUTER, NARROW_OUTER = 18.2754, 18.7693
BOTTOM, TOP = -6.70325, 40.15038


def inside(vertices, faces, point):
    return len(intersections(vertices, faces, point, (60.123, 47.789, point[2]))) % 2 == 1


def side_points(x, y):
    """The same local point on all four sides of the 4-fold symmetric tower."""
    return ((x, y), (-y, x), (-x, -y), (y, -x))


class SwtorBuilding002WalkmeshTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vertices, cls.faces = read_mesh(PWK)
        cls.text = PWK.read_text()

    def test_collision_is_closed_and_keeps_its_extent_and_use_nodes(self):
        assert_closed_mesh(self, self.vertices, self.faces, 0)
        self.assertAlmostEqual(min(v[2] for v in self.vertices), BOTTOM, places=4)
        self.assertAlmostEqual(max(v[2] for v in self.vertices), TOP, places=4)
        self.assertAlmostEqual(max(abs(c) for v in self.vertices for c in v[:2]), NARROW_OUTER, places=4)
        for node in ("p12_pwk_use01", "p12_pwk_use02"):
            self.assertIn("node dummy " + node, self.text)
        self.assertIn("position  0.00000 -19.12082  0.00000", self.text)
        self.assertIn("position  0.00000  19.12082  0.00000", self.text)

    def test_recesses_and_corners_between_buttresses_are_open(self):
        # The old PWK was one 37.5 m box, so these visible gaps between the
        # buttresses and around each corner were invisible walls up to 2.4 m deep.
        clear = ((13.56, 18.62), (13.56, 17.0), (17.0, 17.0), (17.7, 13.0),
                 (3.0, 17.0), (-3.0, 17.0), (-13.0, 17.2), (16.6, 16.6))
        for x, y in clear:
            for point in side_points(x, y):
                for z in (.01, .5, 1.8, 10):
                    with self.subTest(point=point, z=z):
                        self.assertFalse(inside(self.vertices, self.faces, (*point, z)))

    def test_core_and_buttresses_block_up_to_their_visible_tops(self):
        solid = ((0, 0), (0, 16.3), (16.3, 16.3), (0, 18.6), (7.9, 18.2), (-7.9, 18.2), (5, 17.5), (-11, 16.8))
        for x, y in solid:
            for point in side_points(x, y):
                for z in (.01, 1, 15.4):
                    with self.subTest(point=point, z=z):
                        self.assertTrue(inside(self.vertices, self.faces, (*point, z)))
        # Above the buttresses only the core continues to the old top.
        for x, y in side_points(7.9, 18.2) + side_points(0, 18.6):
            with self.subTest(buttress=(x, y)):
                self.assertAlmostEqual(roof_height_at(self.vertices, self.faces, x, y), BUTTRESS_TOP, places=3)
        for x, y in side_points(0, 16.3) + side_points(16.3, 16.3):
            with self.subTest(core=(x, y)):
                self.assertAlmostEqual(roof_height_at(self.vertices, self.faces, x, y), TOP, places=3)
        for x, y in side_points(7.9, 18.2):
            self.assertFalse(inside(self.vertices, self.faces, (x, y, BUTTRESS_TOP + .1)))

    def test_smugglers_housing_report_walks_reach_the_visible_wall(self):
        rows = two_da("placeables")
        area = json.loads((ROOT / "Module/git/smug_housings.git.json").read_text(encoding="cp1252"))
        building = next(p for p in area["Placeable List"]["value"]
                        if abs(p["X"]["value"] - 97.96) < .01 and abs(p["Y"]["value"] - 122.64) < .01)
        self.assertEqual(rows[building["Appearance"]["value"]]["ModelName"].lower(), "daf_sw012")
        angle, z = building["Bearing"]["value"], .5 - building["Z"]["value"]

        def local(x, y):
            dx, dy = x - building["X"]["value"], y - building["Y"]["value"]
            return (dx * math.cos(angle) + dy * math.sin(angle),
                    -dx * math.sin(angle) + dy * math.cos(angle), z)

        # Reported spot (79.34, 136.20) and the walks checked in the client:
        # north of the wide buttress, the north-west corner, and between buttresses.
        for start, end in (((77.5, 136.2), (80.9, 136.2)), ((77.5, 138.5), (80.9, 140.3)),
                           ((77.5, 125.8), (80.9, 125.8)), ((77.5, 141.5), (80.9, 141.5))):
            with self.subTest(walk=(start, end)):
                self.assertFalse(intersections(self.vertices, self.faces, local(*start), local(*end)))
                self.assertFalse(inside(self.vertices, self.faces, local(*end)))
        # The core wall and both kinds of buttress still stop the PC at their faces.
        for start, end, face_x in (((77.5, 136.2), (83.0, 136.2), building["X"]["value"] - CORE),
                                   ((77.5, 130.5), (81.0, 130.5), building["X"]["value"] - WIDE_OUTER),
                                   ((77.5, 122.6), (81.0, 122.6), building["X"]["value"] - NARROW_OUTER)):
            with self.subTest(wall=(start, end)):
                hits = intersections(self.vertices, self.faces, local(*start), local(*end))
                self.assertTrue(hits)
                self.assertAlmostEqual(start[0] + min(hits) * (end[0] - start[0]), face_x, delta=.01)


if __name__ == "__main__":
    unittest.main()
