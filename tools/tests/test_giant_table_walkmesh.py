"""Collision coverage for the Abandoned Station's giant briefing table."""
import unittest

from test_building_walkmesh import (
    ROOT, assert_closed_mesh, intersections, read_mesh, roof_height_at, two_da,
)


class GiantTableWalkmeshTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vertices, cls.faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc/swc_table_giant.pwk")

    def test_appearance_loads_the_collision_model(self):
        self.assertEqual(two_da("placeables")[30044]["ModelName"], "swc_table_giant")

    def test_body_is_a_closed_unwalkable_volume(self):
        assert_closed_mesh(self, self.vertices, self.faces, 7)
        self.assertLess(min(v[2] for v in self.vertices), 0)
        self.assertAlmostEqual(max(v[2] for v in self.vertices), .939145, places=5)

    def test_table_blocks_crossings_from_both_axes(self):
        for z in (.01, .5, .85):
            for x in (-8, -4, 0, 4, 8):
                with self.subTest(x=x, z=z):
                    self.assertTrue(intersections(self.vertices, self.faces, (x, -7, z), (x, 7, z)))
            for y in (-4, 0, 4):
                with self.subTest(y=y, z=z):
                    self.assertTrue(intersections(self.vertices, self.faces, (-12, y, z), (13, y, z)))

    def test_recess_and_surrounding_aisles_remain_clear(self):
        # These points are outside the tabletop, including the open notch at model +X.
        for x, y in ((10, 0), (11, 2.5), (-9, -4), (-9, 4.5), (0, -5.5), (0, 6)):
            with self.subTest(point=(x, y)):
                self.assertIsNone(roof_height_at(self.vertices, self.faces, x, y))
        for start, end in (((12, 0, .1), (9, 0, .1)),
                           ((-12, -5.5, .1), (13, -5.5, .1)),
                           ((-12, 6, .1), (13, 6, .1)),
                           ((-12, 0, 1.1), (13, 0, 1.1))):
            with self.subTest(start=start, end=end):
                self.assertFalse(intersections(self.vertices, self.faces, start, end))


if __name__ == "__main__":
    unittest.main()
