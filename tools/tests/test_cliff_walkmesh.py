"""Movement regressions for the cliff entered in the Wildlands video."""
import json
import math
import unittest

from test_building_walkmesh import ROOT, assert_closed_mesh, intersections, read_mesh


class WildlandsCliffWalkmeshTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vertices, cls.faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc/dag_tnocliff2.pwk")

    def test_collision_is_a_closed_nonwalkable_volume(self):
        assert_closed_mesh(self, self.vertices, self.faces, 7)

    def test_body_blocks_entry_above_the_old_flat_collision(self):
        for height in (.1, 1.6, 2, 4, 8, 12, 15.6):
            for start in ((8, -15, height), (8, 15, height), (35, 0, height)):
                with self.subTest(height=height, start=start):
                    self.assertTrue(intersections(self.vertices, self.faces, start, (8, 0, height)))

    def test_interior_under_the_cliff_cap_is_solid(self):
        # The old ring also left this entire interior available to random spawns.
        for x, y in ((3, 0), (8, 0), (15, 0), (22, 0)):
            for height in (.1, 2, 8, 15.6):
                with self.subTest(point=(x, y, height)):
                    self.assertEqual(len(intersections(self.vertices, self.faces,
                                                       (x, y, height), (40.123, 18.789, height))) % 2, 1)

    def test_exterior_routes_and_space_above_the_cliff_remain_clear(self):
        for start, end in (((-1, -12, 2), (-1, 12, 2)),
                           ((3, 11, 2), (24, 11, 2)),
                           ((3, -11, 2), (24, -11, 2)),
                           ((31, -12, 2), (31, 12, 2)),
                           ((8, -15, 18), (8, 15, 18))):
            with self.subTest(start=start, end=end):
                self.assertFalse(intersections(self.vertices, self.faces, start, end))

    def test_reported_northwest_placement_blocks_the_raised_terrain_approach(self):
        area = json.loads((ROOT / "Module/git/viscarawildlands.git.json").read_text())
        cliff = next(p for p in area["Placeable List"]["value"]
                     if p["Appearance"]["value"] == 3994
                     and abs(p["X"]["value"] - 2.79) < .01
                     and abs(p["Y"]["value"] - 311.38) < .01)
        self.assertEqual(cliff["Static"]["value"], 1)
        angle = cliff["Bearing"]["value"]

        def local(x, y, z):
            dx, dy = x - cliff["X"]["value"], y - cliff["Y"]["value"]
            return (dx * math.cos(angle) + dy * math.sin(angle),
                    -dx * math.sin(angle) + dy * math.cos(angle), z - cliff["Z"]["value"])

        # The tile's walking surface is 2m here, above the previous PWK at 0m.
        outside, inside = local(13, 300, 2.1), local(3, 300, 2.1)
        self.assertTrue(intersections(self.vertices, self.faces, outside, inside))
        self.assertTrue(intersections(self.vertices, self.faces, inside, outside))
        self.assertFalse(intersections(self.vertices, self.faces,
                                       local(13, 298, 1.8), local(13, 304, 3.5)))


if __name__ == "__main__":
    unittest.main()
