"""Authored saved-location recovery regions must match collision and land safely."""
import json
import math
import unittest

from test_building_walkmesh import ROOT, VelesFloor, intersections, read_mesh, two_da


def contains(polygon, x, y):
    inside = False
    for a, b in zip(polygon, polygon[1:] + polygon[:1]):
        if (a[1] > y) != (b[1] > y):
            if x < (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]) + a[0]:
                inside = not inside
    return inside


class LoginRecoveryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.area = json.loads((ROOT / "Module/git/veles_exterior.git.json").read_text())
        cls.region = next(t for t in cls.area["TriggerList"]["value"]
                          if t["TemplateResRef"]["value"] == "login_recovery")
        cls.variables = {v["Name"]["value"]: v["Value"]["value"]
                         for v in cls.region["VarTable"]["value"]}
        cls.polygon = [(p["PointX"]["value"] + cls.region["XPosition"]["value"],
                        p["PointY"]["value"] + cls.region["YPosition"]["value"])
                       for p in cls.region["Geometry"]["value"]]
        cls.building = next(p for p in cls.area["Placeable List"]["value"]
                            if p["Appearance"]["value"] == 30143
                            and abs(p["X"]["value"] - 165.024139) < .001
                            and abs(p["Y"]["value"] - 142.483902) < .001)
        cls.vertices, cls.faces = read_mesh(ROOT / "SWLOR_Haks/sw_plc/swc_blg_corhis04.pwk")

    def test_reported_position_is_recovered_but_street_and_roof_heights_are_not(self):
        self.assertTrue(contains(self.polygon, 156.23833, 129.15797))
        self.assertLessEqual(self.variables["LOGIN_RECOVERY_MIN_Z"], .2)
        self.assertGreaterEqual(self.variables["LOGIN_RECOVERY_MAX_Z"], .2)
        self.assertLess(self.variables["LOGIN_RECOVERY_MAX_Z"],
                        7.88 + self.building["Z"]["value"], "Do not redirect positions above the lowest roof")
        self.assertFalse(contains(self.polygon, 150, 129.15797))
        self.assertFalse(contains(self.polygon, 155.5, 129.15797))

    def test_region_follows_the_building_including_recesses(self):
        angle = self.building["Bearing"]["value"]
        z = .2 - self.building["Z"]["value"]
        for x in range(154, 178, 2):
            for y in range(122, 164, 2):
                dx, dy = x - self.building["X"]["value"], y - self.building["Y"]["value"]
                local = (dx * math.cos(angle) + dy * math.sin(angle),
                         -dx * math.sin(angle) + dy * math.cos(angle), z)
                solid = len(intersections(self.vertices, self.faces, local, (40.123, 35.789, z))) % 2 == 1
                with self.subTest(point=(x, y)):
                    self.assertEqual(contains(self.polygon, x, y), solid)

    def test_recovery_destination_has_room_on_native_floor_outside_placeables(self):
        waypoints = [p for p in self.area["WaypointList"]["value"]
                     if p["Tag"]["value"] == self.variables["LOGIN_RECOVERY_WAYPOINT"]]
        self.assertEqual(len(waypoints), 1)
        x, y, z = (waypoints[0][a + "Position"]["value"] for a in "XYZ")
        floor, rows = VelesFloor(), two_da("placeables")
        for ox, oy in ((0, 0), (.5, 0), (-.5, 0), (0, .5), (0, -.5)):
            self.assertAlmostEqual(floor.height_at(x + ox, y + oy), z, places=3)
        for instance in self.area["Placeable List"]["value"]:
            if math.hypot(instance["X"]["value"] - x, instance["Y"]["value"] - y) > 30:
                continue
            model = rows[instance["Appearance"]["value"]]["ModelName"]
            paths = [p for folder in ("sw_plc", "sw_plc_swtor", "sw_plc_mdrn", "sw_plc_cep")
                     for p in (ROOT / "SWLOR_Haks" / folder).glob(model + ".pwk")]
            if not paths or "node trimesh" not in paths[0].read_text():
                continue
            vertices, faces = read_mesh(paths[0])
            angle = instance["Bearing"]["value"]
            for ox, oy in ((0, 0), (.5, 0), (-.5, 0), (0, .5), (0, -.5)):
                dx, dy = x + ox - instance["X"]["value"], y + oy - instance["Y"]["value"]
                px, py = dx * math.cos(angle) + dy * math.sin(angle), -dx * math.sin(angle) + dy * math.cos(angle)
                with self.subTest(model=model, offset=(ox, oy)):
                    # A vertical probe detects a projected placeable footprint even
                    # when its collision is a single plane instead of a sealed volume.
                    self.assertFalse(intersections(vertices, faces, (px, py, -10), (px, py, 200)))

    def test_region_has_no_enter_exit_or_heartbeat_behavior(self):
        for script in ("ScriptOnEnter", "ScriptOnExit", "ScriptHeartbeat", "ScriptUserDefine"):
            self.assertEqual(self.region[script]["value"], "")
        self.assertEqual(self.region["TrapFlag"]["value"], 0)


if __name__ == "__main__":
    unittest.main()
