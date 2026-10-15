"""Check Veles seat access against the actual furniture collision meshes."""
import json
import math
import re
import unittest

from test_building_walkmesh import ROOT, intersections, read_mesh, roof_height_at, two_da


class SeatingWalkmeshTests(unittest.TestCase):
    models = {"daf_sw068": "daf_sw068.pwk", "aswtor_125": "ASWTOR_125.pwk"}

    def test_fountain_and_shelter_seat_use_points_are_reachable(self):
        objects = json.loads((ROOT / "Module/git/veles_exterior.git.json").read_text())["Placeable List"]["value"]
        seats = [p for p in objects if p["Appearance"]["value"] == 1604]
        seat_mesh = (ROOT / "SWLOR_Haks/sw_plc_cep/zlc_x94.pwk").read_text()
        use = re.search(r"node dummy \S+_use01\s+parent \S+\s+position (\S+) (\S+)", seat_mesh)
        use_x, use_y = map(float, use.groups())
        counts = {31831: 0, 31571: 0}
        for prop in objects:
            appearance = prop["Appearance"]["value"]
            if appearance not in counts:
                continue
            model = "daf_sw298" if appearance == 31831 else "daf_sw070"
            vertices, faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc_swtor" / (model + ".pwk"))
            px, py = (prop[k]["value"] for k in ("X", "Y"))
            bearing = prop["Bearing"]["value"]
            c, s = math.cos(bearing), math.sin(bearing)
            for seat in seats:
                dx, dy = seat["X"]["value"] - px, seat["Y"]["value"] - py
                if math.hypot(dx, dy) > 1.6:
                    continue
                x, y = c * dx + s * dy, -s * dx + c * dy
                angle = seat["Bearing"]["value"] - bearing
                ux = x + math.cos(angle) * use_x - math.sin(angle) * use_y
                uy = y + math.sin(angle) * use_x + math.cos(angle) * use_y
                distance = math.hypot(ux - x, uy - y)
                start = (x + (ux - x) / distance * 2, y + (uy - y) / distance * 2, .1)
                with self.subTest(model=model, prop=(px, py), seat=(x, y)):
                    self.assertEqual(seat["Useable"]["value"], 1)
                    self.assertEqual(seat["Static"]["value"], 0)
                    self.assertIn(seat["OnUsed"]["value"], ("zep_use_chair", "x0_o2_use_chair", "sit"))
                    # ActionSit walks to the PWK use dummy, not the seat origin.
                    self.assertIsNone(roof_height_at(vertices, faces, ux, uy), "The chair use point must be outside furniture collision")
                    self.assertFalse(intersections(vertices, faces, start, (ux, uy, .1)), "The front approach must reach the chair use point")
                counts[appearance] += 1
        self.assertEqual(counts, {31831: 11, 31571: 9})

    def test_shelter_back_and_supports_still_block_movement(self):
        vertices, faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc_swtor/daf_sw070.pwk")
        for x in (-1.8, 0, 1.8):
            with self.subTest(x=x):
                self.assertTrue(intersections(vertices, faces, (x, -2, .1), (x, 1, .1)))
        self.assertTrue(all(face[-1] == 0 for face in faces))

    def test_placed_seats_and_their_approaches_clear_furniture_collision(self):
        rows = two_da("placeables")
        seat_mesh = (ROOT / "SWLOR_Haks/sw_plc_cep/zlc_x94.pwk").read_text()
        use = re.search(r"node dummy \S+_use01\s+parent \S+\s+position (\S+) (\S+)", seat_mesh)
        use_x, use_y = map(float, use.groups())
        counts = dict.fromkeys(self.models, 0)
        for area in ("veles_exterior", "veles_sheriff"):
            objects = json.loads((ROOT / f"Module/git/{area}.git.json").read_text())["Placeable List"]["value"]
            seats = [p for p in objects if p["Appearance"]["value"] == 1604]
            for prop in objects:
                model = rows[prop["Appearance"]["value"]]["ModelName"].lower()
                if model not in self.models:
                    continue
                vertices, faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc_swtor" / self.models[model])
                px, py = (prop[k]["value"] for k in ("X", "Y"))
                bearing = prop["Bearing"]["value"]
                c, s = math.cos(bearing), math.sin(bearing)
                for seat in seats:
                    dx, dy = seat["X"]["value"] - px, seat["Y"]["value"] - py
                    if math.hypot(dx, dy) > 1.6 or abs(seat["Z"]["value"] - prop["Z"]["value"]) > .8:
                        continue
                    x, y = c * dx + s * dy, -s * dx + c * dy
                    angle = seat["Bearing"]["value"] - bearing
                    ux = x + math.cos(angle) * use_x - math.sin(angle) * use_y
                    uy = y + math.sin(angle) * use_x + math.cos(angle) * use_y
                    distance = math.hypot(ux - x, uy - y)
                    start_x = x + (ux - x) / distance * 2
                    start_y = y + (uy - y) / distance * 2
                    with self.subTest(area=area, model=model, position=(px, py), seat=(x, y)):
                        self.assertIsNone(roof_height_at(vertices, faces, x, y), "The seat marker must be outside the solid prop")
                        # Room for a body at the invisible chair's real use point.
                        for ox, oy in ((0, 0), (.25, 0), (-.25, 0), (0, .25), (0, -.25)):
                            self.assertFalse(intersections(vertices, faces, (start_x + ox, start_y + oy, .1), (ux + ox, uy + oy, .1)), "Front approach must reach the seat use point")
                    counts[model] += 1
        self.assertEqual(counts, {"daf_sw068": 42, "aswtor_125": 18})

    def test_furniture_backs_still_block_movement(self):
        for model, filename in self.models.items():
            with self.subTest(model=model):
                vertices, faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc_swtor" / filename)
                self.assertTrue(intersections(vertices, faces, (0, -2, .1), (0, 2, .1)))
                self.assertTrue(all(face[-1] == 0 for face in faces))


if __name__ == "__main__":
    unittest.main()
