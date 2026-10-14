"""Keep Veles sidewalks and news approaches clear without changing shared assets."""
import json
import math
import unittest

from test_building_walkmesh import ROOT, VelesFloor, assert_closed_mesh, intersections, read_mesh, two_da


class VelesSidewalkCollisionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = two_da("placeables")
        cls.objects = json.loads((ROOT / "Module/git/veles_exterior.git.json").read_text(
            encoding="utf-8"))["Placeable List"]["value"]
        cls.floor = VelesFloor()

    def test_all_four_paved_routes_use_a_model_without_placeable_collision(self):
        # News, opposite side of the street, and the two western paving strips.
        for x, y in ((105, 17), (125, 17), (75, 16.806), (55.04, 18.28)):
            with self.subTest(position=(x, y)):
                paving = [p for p in self.objects
                          if abs(p["X"]["value"] - x) < .05
                          and abs(p["Y"]["value"] - y) < .05
                          and -5.1 < p["Z"]["value"] < -4.9]
                self.assertEqual(len(paving), 1)
                row = self.rows[paving[0]["Appearance"]["value"]]
                model = row["ModelName"].lower()
                self.assertTrue((ROOT / "SWLOR_Haks/sw_plc_swtor" / (model + ".mdl")).is_file())
                # A wall PWK on a buried decoration still intersects the walking
                # plane. Paving uses the tile WOK, just like the road overlays.
                collisions = [p for p in (ROOT / "SWLOR_Haks").glob("sw_*/*.pwk")
                              if p.stem.lower() == model]
                self.assertEqual(collisions, [], "Decorative paving must not block the tile walkmesh")
                for dx in (-4, 0, 4):
                    for dy in (-.5, 0, .5):
                        self.assertIsNotNone(self.floor.height_at(x + dx, y + dy))

    def test_paving_preserves_the_visible_wall_geometry_and_original_collision(self):
        folder = ROOT / "SWLOR_Haks/sw_plc_swtor"
        original = (folder / "ASWTOR_023.mdl").read_bytes()
        paving = (folder / "sw_walkway.mdl").read_bytes()
        self.assertEqual(original.count(b"ASWTOR_023\0"), 2)
        self.assertEqual(paving.count(b"sw_walkway\0"), 2)
        self.assertEqual(paving.replace(b"sw_walkway\0", b"ASWTOR_023\0"), original)
        self.assertEqual(self.rows[31923]["ModelName"], "ASWTOR_023")
        self.assertTrue((folder / "ASWTOR_023.pwk").is_file())
        # The deeply buried wall is unrelated to the four exposed paving strips.
        buried = [p for p in self.objects
                  if p["Appearance"]["value"] == 31923]
        self.assertEqual(len(buried), 1)
        self.assertLess(buried[0]["Z"]["value"], -9)

    def test_news_entrance_keeps_the_stairs_clear_and_side_rails_solid(self):
        entrance = [p for p in self.objects
                    if abs(p["X"]["value"] - 94.604) < .01
                    and abs(p["Y"]["value"] - 16.97) < .01]
        self.assertEqual(len(entrance), 1)
        model = self.rows[entrance[0]["Appearance"]["value"]]["ModelName"].lower()
        folder = ROOT / "SWLOR_Haks/sw_plc_swtor"
        vertices, faces = read_mesh(folder / (model + ".pwk"))
        assert_closed_mesh(self, vertices, faces, 0)
        # PWK footprints must leave a body-width passage at every stair height,
        # including the buried front edge that blocked the reported approach.
        for x in (-.95, 0, .95):
            for z in (.01, .4, .78, 1.2):
                start, end = (x, -3, z), (x, .2, z)
                with self.subTest(x=x, z=z):
                    self.assertFalse(intersections(vertices, faces, start, end))
                    self.assertFalse(intersections(vertices, faces, end, start))
        for x in (-1.8, 1.8):
            self.assertTrue(intersections(vertices, faces, (x, -3, .8), (x, .2, .8)))
        original = (folder / "ASWTOR_175.mdl").read_bytes()
        variant = (folder / (model + ".mdl")).read_bytes()
        self.assertEqual(variant.replace(b"sw_openarc\0", b"ASWTOR_175\0"), original)
        self.assertEqual(self.rows[32076]["ModelName"], "ASWTOR_175")
        self.assertEqual(sum(p["Appearance"]["value"] == 32076 for p in self.objects), 3)

    def test_news_corner_pads_do_not_extend_invisible_walls_above_the_sidewalk(self):
        building = next(p for p in self.objects
                        if abs(p["X"]["value"] - 89.35) < .01
                        and abs(p["Y"]["value"] - 17) < .01)
        folder = ROOT / "SWLOR_Haks/sw_plc"
        model = self.rows[building["Appearance"]["value"]]["ModelName"].lower()
        vertices, faces = read_mesh(folder / (model + ".pwk"))
        assert_closed_mesh(self, vertices, faces, 7)

        def local(point):
            x, y, z = point
            dx, dy = x - building["X"]["value"], y - building["Y"]["value"]
            angle = building["Bearing"]["value"]
            return (dx * math.cos(angle) + dy * math.sin(angle),
                    -dx * math.sin(angle) + dy * math.cos(angle),
                    z - building["Z"]["value"])

        # These paths enter the red-marked patch beyond the generator pipe.
        # Checking only (97.1,9) misses the old wall at world Y 8.4965.
        original = read_mesh(folder / "swc_blg_corhis01.pwk")
        for x in (96.3, 97.1):
            for z in (.2, 1.0, 2.0):
                start, end = local((x, 10.8, z)), local((x, 6.5, z))
                with self.subTest(x=x, z=z):
                    self.assertTrue(intersections(*original, start, end))
                    self.assertFalse(intersections(vertices, faces, start, end))
                    self.assertFalse(intersections(vertices, faces, end, start))
        # Keep the real building wall and the low, buried corner platform solid.
        self.assertTrue(intersections(vertices, faces,
                                      local((100, 17, .2)), local((89.35, 17, .2))))
        self.assertTrue(intersections(vertices, faces, (-7, 9.5, -1), (-7, 9.5, .9)))
        original_mdl = (folder / "swc_blg_corhis01.mdl").read_bytes()
        variant = (folder / (model + ".mdl")).read_bytes()
        self.assertEqual(variant.replace(b"swc_news_bldg_01\0", b"swc_blg_corhis01\0"), original_mdl)
        self.assertEqual(self.rows[30140]["ModelName"], "swc_blg_corhis01")


if __name__ == "__main__":
    unittest.main()
