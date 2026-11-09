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

    def test_sunken_water_tower_leaves_northwest_square_seats_reachable(self):
        objects = json.loads((ROOT / "Module/git/veles_exterior.git.json").read_text(encoding="cp1252"))["Placeable List"]["value"]
        tower = next(p for p in objects if p["Appearance"]["value"] == 31967
                     and abs(p["X"]["value"] - 100.85) < .01 and abs(p["Y"]["value"] - 89.23) < .01)
        vertices, faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc_swtor/ASWTOR_067.pwk")
        seat_mesh = (ROOT / "SWLOR_Haks/sw_plc_cep/zlc_x94.pwk").read_text()
        use = re.search(r"node dummy \S+_use01\s+parent \S+\s+position (\S+) (\S+)", seat_mesh)
        use_x, use_y = map(float, use.groups())
        tx, ty = tower["X"]["value"], tower["Y"]["value"]
        bearing = tower["Bearing"]["value"]
        c, s = math.cos(bearing), math.sin(bearing)
        # The tower is sunk into the ground, so the walking plane cuts it well above the model's base.
        street = .2 - tower["Z"]["value"]

        def local(x, y):
            dx, dy = x - tx, y - ty
            return c * dx + s * dy, -s * dx + c * dy

        # Client probes show the engine blocks the whole 2D footprint of every face that reaches the
        # walking plane (a face topping out 0.04 m below it still blocked); faces well below it are ignored.
        blocking = [f for f in faces if max(vertices[i][2] for i in f[:3]) > street - .5]

        def in_blocked_footprint(x, y):
            for face in blocking:
                (ax, ay, _), (bx, by, _), (cx, cy, _) = (vertices[i] for i in face[:3])
                det = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
                if abs(det) < 1e-9:
                    continue
                u = ((by - cy) * (x - cx) + (cx - bx) * (y - cy)) / det
                v = ((cy - ay) * (x - cx) + (ax - cx) * (y - cy)) / det
                if u >= 0 and v >= 0 and u + v <= 1:
                    return True
            return False

        seats = [p for p in objects if p["Appearance"]["value"] == 1604
                 and math.hypot(p["X"]["value"] - tx, p["Y"]["value"] - ty) < 8]
        self.assertEqual(len(seats), 6, "Bench 001 and the Bus Stop each carry three invisible seats")
        for seat in seats:
            sx, sy = seat["X"]["value"], seat["Y"]["value"]
            angle = seat["Bearing"]["value"]
            ux = sx + math.cos(angle) * use_x - math.sin(angle) * use_y
            uy = sy + math.sin(angle) * use_x + math.cos(angle) * use_y
            with self.subTest(seat=(sx, sy)):
                self.assertFalse(in_blocked_footprint(*local(sx, sy)), "The seat must not be inside the tower collision")
                self.assertFalse(in_blocked_footprint(*local(ux, uy)), "The use point must not be inside the tower collision")

        # The shaft that is visible at street level still blocks movement.
        for point in ((0, 0), (1.5, 0), (-1.5, 0), (0, 1.5), (0, -1.4)):
            with self.subTest(shaft=point):
                self.assertTrue(in_blocked_footprint(*point))

    def test_water_tower_base_still_blocks_movement_when_not_sunken(self):
        vertices, faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc_swtor/ASWTOR_067.pwk")
        self.assertTrue(all(face[-1] == 0 for face in faces))
        for point in ((3, 3.9), (-2.8, 3.9), (-2.8, -5), (3, -5), (0, 0)):
            with self.subTest(base=point):
                hits = intersections(vertices, faces, (*point, .1), (50.123, 47.456, .1))
                self.assertEqual(len(hits) % 2, 1)
        for point in ((3.6, 0), (-3.4, 0), (0, -5.6), (0, 4.5)):
            with self.subTest(outside=point):
                self.assertEqual(len(intersections(vertices, faces, (*point, .1), (50.123, 47.456, .1))) % 2, 0)

    def test_furniture_backs_still_block_movement(self):
        for model, filename in self.models.items():
            with self.subTest(model=model):
                vertices, faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc_swtor" / filename)
                self.assertTrue(intersections(vertices, faces, (0, -2, .1), (0, 2, .1)))
                self.assertTrue(all(face[-1] == 0 for face in faces))


if __name__ == "__main__":
    unittest.main()
