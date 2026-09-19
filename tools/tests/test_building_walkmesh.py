"""Geometry regressions for building collision throughout Veles.

Run with: python -m unittest discover -s tools/tests -p test_building_walkmesh.py
These inspect the shipped PWK; an in-game pathfinding check is still required.
"""
from collections import Counter
import json
import math
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[2]
PWK = ROOT / "SWLOR_Haks/sw_plc/swc_blg_corhis04.pwk"

# Heights measured from the model geometry, not the placement's current Z.
REPAIRED_ROOFS = {
    "swc_blg_corhis01": 15.56, "swc_blg_corhis02": 19.66,
    "swc_blg_corhis03": 17.27, "swc_blg_corhis04": 22.68,
    "swc_bld_bk_cor01": 29.56, "swc_bld_bk_cor02": 17.79,
    "swc_bld_bk_cor03": 17.79, "swc_bldg_b_sky04": 155.99,
    "swc_prison01": 51.459, "swc_fctry_item1": 7.7,
    "swc_hse_lrg_gen3": 7.64,
}
EXISTING_VOLUMES = {
    "swc_fctry_machn1": "sw_plc/swc_fctry_machn1.pwk",
    "daf_sw164": "sw_plc_swtor/daf_sw164.pwk",
    "aswtor_111": "sw_plc_swtor/ASWTOR_111.pwk",
    "aswtor_112": "sw_plc_swtor/ASWTOR_112.pwk",
    "daf_sw284": "sw_plc_swtor/daf_sw284.pwk",
    "aswtor_067": "sw_plc_swtor/ASWTOR_067.pwk",
}


def read_mesh(path):
    lines = [line.split() for line in path.read_text().splitlines()]
    vi = next(i for i, line in enumerate(lines) if line[:1] == ["verts"])
    fi = next(i for i, line in enumerate(lines) if line[:1] == ["faces"])
    vertices = [tuple(map(float, line)) for line in lines[vi + 1:vi + 1 + int(lines[vi][1])]]
    faces = [tuple(map(int, line)) for line in lines[fi + 1:fi + 1 + int(lines[fi][1])]]
    return vertices, faces


def subtract(a, b):
    return tuple(x - y for x, y in zip(a, b))


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def intersections(vertices, faces, start, end):
    """Intersect a finite movement segment with triangles (Moller-Trumbore)."""
    direction = subtract(end, start)
    hits = set()
    for face in faces:
        a, b, c = (vertices[i] for i in face[:3])
        edge1, edge2 = subtract(b, a), subtract(c, a)
        p = cross(direction, edge2)
        determinant = dot(edge1, p)
        if abs(determinant) < 1e-10:
            continue
        offset = subtract(start, a)
        u = dot(offset, p) / determinant
        q = cross(offset, edge1)
        v = dot(direction, q) / determinant
        t = dot(edge2, q) / determinant
        if u >= -1e-8 and v >= -1e-8 and u + v <= 1 + 1e-8 and 0 < t < 1:
            hits.add(round(t, 8))
    return hits


def assert_closed_mesh(test, vertices, faces, material):
    test.assertTrue(all(math.isfinite(value) for vertex in vertices for value in vertex))
    edges, directed = Counter(), Counter()
    for face in faces:
        test.assertEqual(face[-1], material, "Preserve the collision surface material")
        a, b, c = face[:3]
        test.assertTrue(all(0 <= i < len(vertices) for i in (a, b, c)))
        normal = cross(subtract(vertices[b], vertices[a]), subtract(vertices[c], vertices[a]))
        test.assertGreater(dot(normal, normal), 1e-12)
        for edge in ((a, b), (b, c), (c, a)):
            edges[tuple(sorted(edge))] += 1
            directed[edge] += 1
    test.assertTrue(edges)
    test.assertTrue(all(count == 2 for count in edges.values()), "Every boundary must be sealed")
    test.assertEqual(directed, Counter({(b, a): count for (a, b), count in directed.items()}))


def two_da(name):
    lines = (ROOT / "SWLOR_Haks/sw_2da" / (name + ".2da")).read_text().splitlines()
    columns = lines[2].split()
    # Physical row order is authoritative; human-readable row labels may differ.
    return [dict(zip(columns, [s.strip('"') for s in re.findall(r'"[^"]*"|\S+', line)[1:]]))
            for line in lines[3:] if line.strip()]


class VelesFloor:
    def __init__(self):
        self.area = json.loads((ROOT / "Module/are/veles_exterior.are.json").read_text())
        self.width = self.area["Width"]["value"]
        self.height = self.area["Height"]["value"]
        folder = ROOT / "SWLOR_Haks/sw_t_modernex"
        tileset = (folder / (self.area["Tileset"]["value"] + ".set")).read_text()
        self.transition = float(re.search(r"(?m)^Transition=(\S+)", tileset)[1])
        self.models = {int(n): re.search(r"(?m)^Model=(\S+)", body)[1]
                       for n, body in re.findall(r"(?ms)^\[TILE(\d+)\]\s*\n(.*?)(?=^\[|\Z)", tileset)}
        used = {self.models[t["Tile_ID"]["value"]] for t in self.area["Tile_List"]["value"]}
        self.meshes = {name: read_mesh(folder / (name + ".wok")) for name in used}
        self.surfaces = two_da("surfacemat")

    def height_at(self, x, y):
        ix, iy = math.floor(x / 10), math.floor(y / 10)
        if not (0 <= ix < self.width and 0 <= iy < self.height):
            return None
        tile = self.area["Tile_List"]["value"][iy * self.width + ix]
        angle = tile["Tile_Orientation"]["value"] * math.pi / 2
        dx, dy = x - (ix * 10 + 5), y - (iy * 10 + 5)
        x, y = dx * math.cos(angle) + dy * math.sin(angle), -dx * math.sin(angle) + dy * math.cos(angle)
        vertices, faces = self.meshes[self.models[tile["Tile_ID"]["value"]]]
        heights = []
        for face in faces:
            if self.surfaces[face[-1]]["Walk"] != "1":
                continue
            a, b, c = (vertices[i] for i in face[:3])
            det = (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
            if abs(det) < 1e-10:
                continue
            u = ((x-a[0])*(c[1]-a[1])-(y-a[1])*(c[0]-a[0])) / det
            v = ((b[0]-a[0])*(y-a[1])-(b[1]-a[1])*(x-a[0])) / det
            if min(u, v) >= -1e-8 and u + v <= 1 + 1e-8:
                heights.append(a[2]+u*(b[2]-a[2])+v*(c[2]-a[2])+self.transition*tile["Tile_Height"]["value"])
        return max(heights) if heights else None


class BuildingWalkmeshTests(unittest.TestCase):
    mesh_path = PWK
    appearance = 30143
    placement = (191.94, 196.75)
    approach = ((186.3, 187), (186.3, 192))  # South wall beside the blue awning.
    exterior_points = ((-5, -4.5), (7.8, 8), (-10, 0), (10, 0), (0, -19), (0, 19))
    interior_points = ((0, 0), (6, -5), (-7, -12))
    probe_heights = (.01, .85, 2, 7.7)

    @classmethod
    def setUpClass(cls):
        cls.vertices, cls.faces = read_mesh(cls.mesh_path)

    def test_collision_is_closed_non_degenerate_and_non_walkable(self):
        assert_closed_mesh(self, self.vertices, self.faces, 7)

    def test_walls_block_entry_at_street_height_for_the_reported_instance(self):
        area = json.loads((ROOT / "Module/git/veles_exterior.git.json").read_text())
        building = next(p for p in area["Placeable List"]["value"]
                        if p["Appearance"]["value"] == self.appearance
                        and abs(p["X"]["value"] - self.placement[0]) < .01
                        and abs(p["Y"]["value"] - self.placement[1]) < .01)
        self.assertEqual(building["Static"]["value"], 1)
        z = .2 - building["Z"]["value"]
        # Approach all four outer walls on the actual local walking plane.
        for start in ((-20, 0, z), (20, 0, z), (0, -25, z), (0, 25, z)):
            with self.subTest(start=start):
                self.assertTrue(intersections(self.vertices, self.faces, start, (0, 0, z)))
        # World-space approach through the wall shown in the report.
        angle = building["Bearing"]["value"]
        def local(x, y):
            dx, dy = x - building["X"]["value"], y - building["Y"]["value"]
            return (dx * math.cos(angle) + dy * math.sin(angle),
                    -dx * math.sin(angle) + dy * math.cos(angle), z)
        self.assertTrue(intersections(self.vertices, self.faces,
                                      local(*self.approach[0]), local(*self.approach[1])))

    def test_recesses_and_outside_routes_remain_open(self):
        for x, y in self.exterior_points:
            with self.subTest(point=(x, y)):
                self.assertFalse(intersections(self.vertices, self.faces, (x, y, -1), (x, y, 10)))

    def test_building_body_stays_solid_above_the_foundation(self):
        for z in self.probe_heights:
            for x, y in self.interior_points:
                with self.subTest(point=(x, y, z)):
                    # Odd crossings to an exterior point mean the start is inside.
                    self.assertEqual(len(intersections(self.vertices, self.faces,
                                                       (x, y, z), (30.123, 26.789, z))) % 2, 1)


class CorHistoric02WalkmeshTests(BuildingWalkmeshTests):
    mesh_path = ROOT / "SWLOR_Haks/sw_plc/swc_blg_corhis02.pwk"
    appearance = 30141
    placement = (158.741333, 181.466141)
    approach = ((172, 180), (168, 180))  # East wall with the two posters.
    exterior_points = ((-6, 0), (-6, 6), (8, 0), (8, 6), (0, -12.5), (0, 12.5))
    # Include the corner wings and a projecting column, as well as the main body.
    interior_points = ((0, 0), (6, -5), (-7, -10), (-7, 10), (8, 2.7))
    probe_heights = (.01, .891101, 2, 9.1)


class CorHistoric04NorthernAwningTests(unittest.TestCase):
    mesh_path = PWK

    def test_awning_wall_blocks_entry_while_generator_recess_stays_clear(self):
        area = json.loads((ROOT / "Module/git/veles_exterior.git.json").read_text())
        building = next(p for p in area["Placeable List"]["value"]
                        if p["Appearance"]["value"] == 30143
                        and abs(p["X"]["value"] - 151.761215) < .01
                        and abs(p["Y"]["value"] - 191.022629) < .01)
        self.assertEqual(building["Static"]["value"], 1)
        vertices, faces = read_mesh(self.mesh_path)
        angle = building["Bearing"]["value"]
        street_height = .2 - building["Z"]["value"]

        def local(x, y):
            dx, dy = x - building["X"]["value"], y - building["Y"]["value"]
            return (dx * math.cos(angle) + dy * math.sin(angle),
                    -dx * math.sin(angle) + dy * math.cos(angle), street_height)

        # The third report shows the east wall, beside the northern blue awning.
        for start, end in (((161, 198), (158, 198)), ((161, 200), (158, 200)),
                           ((162, 202.2), (157, 202.2))):
            with self.subTest(approach=(start, end)):
                self.assertTrue(intersections(vertices, faces, local(*start), local(*end)))

        # Both generators occupy the existing recess; the bin and awning approach
        # are outside the facade and must not acquire building collision.
        for point in ((156, 194.37), (156, 196.1), (159.916, 197.491), (160.6, 202.2)):
            with self.subTest(clear_point=point):
                x, y, _ = local(*point)
                self.assertFalse(intersections(vertices, faces, (x, y, -1), (x, y, 10)))


class VelesBuildingWalkmeshAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rows = two_da("placeables")
        cls.surfaces = two_da("surfacemat")
        area = json.loads((ROOT / "Module/git/veles_exterior.git.json").read_text())
        cls.placements = []
        for instance in area["Placeable List"]["value"]:
            row = cls.rows[instance["Appearance"]["value"]]
            label = row["Label"].lower()
            # Catalog categories distinguish building shells and factory structures
            # from ships, wall signs and the separately placed Naboo door surrounds.
            if re.search(r"\b(building|house|factory|facility|tower)\b", label) and not re.search(r"\bdoor\b", label):
                cls.placements.append((row["ModelName"].lower(), instance))
        cls.meshes = {name: read_mesh(PWK.parent / (name + ".pwk")) for name in REPAIRED_ROOFS}

    def test_every_building_model_in_the_area_has_volumetric_collision(self):
        used = {name for name, _ in self.placements}
        self.assertEqual(used, set(REPAIRED_ROOFS) | set(EXISTING_VOLUMES),
                         "Review newly placed building models as part of this corpus audit")
        self.assertEqual(len(self.placements), 64)
        for name in used:
            with self.subTest(model=name):
                vertices, faces = (self.meshes[name] if name in self.meshes else
                                   read_mesh(ROOT / "SWLOR_Haks" / EXISTING_VOLUMES[name]))
                self.assertTrue(faces)
                self.assertGreater(max(v[2] for v in vertices)-min(v[2] for v in vertices), 1)
                self.assertTrue(all(self.surfaces[f[-1]]["Walk"] == "0" for f in faces))

    def test_repaired_meshes_are_sealed_and_reach_their_roofs(self):
        for name, (vertices, faces) in self.meshes.items():
            with self.subTest(model=name):
                # The Mustafar house intentionally used Obscuring rather than Nonwalk.
                assert_closed_mesh(self, vertices, faces, 2 if name == "swc_hse_lrg_gen3" else 7)
                self.assertLess(min(v[2] for v in vertices), 0)
                self.assertAlmostEqual(max(v[2] for v in vertices), REPAIRED_ROOFS[name], places=2)

    def test_repaired_instances_block_real_ground_and_elevated_streets(self):
        floor = VelesFloor()
        checked, elevated, above_roof = 0, 0, 0
        for name, instance in self.placements:
            if name not in self.meshes:
                continue
            vertices, faces = self.meshes[name]
            bottom, top = min(v[2] for v in vertices), max(v[2] for v in vertices)
            caps = [f for f in faces if all(abs(vertices[i][2]-bottom) < 1e-8 for i in f[:3])]
            # Sample the concave footprint's triangle interiors, including wings
            # and projecting columns, rather than assuming the model origin is solid.
            probes = [tuple(sum(vertices[i][d] for i in f[:3])/3 for d in (0, 1))
                      for f in caps[::max(1, len(caps)//12)]]
            angle = instance["Bearing"]["value"]
            for x, y in probes:
                wx = instance["X"]["value"] + x*math.cos(angle)-y*math.sin(angle)
                wy = instance["Y"]["value"] + x*math.sin(angle)+y*math.cos(angle)
                ground = floor.height_at(wx, wy)
                if ground is None:
                    continue
                z = ground - instance["Z"]["value"]
                endpoint = (max(v[0] for v in vertices)+25.123, max(v[1] for v in vertices)+23.789, z)
                with self.subTest(model=name, world=(wx, wy, ground)):
                    hits = intersections(vertices, faces, (x, y, z), endpoint)
                    if bottom + .001 < z < top - .001:
                        self.assertEqual(len(hits) % 2, 1, "Wall volume must intersect the terrain's walking plane")
                        checked += 1
                        elevated += ground > 10
                    elif z > top + .001:
                        self.assertFalse(hits, "Walkable terrain above a submerged model's roof must remain clear")
                        above_roof += 1
        self.assertGreater(checked, 100)
        self.assertGreater(elevated, 10)
        self.assertGreater(above_roof, 5)

    def test_skyscraper_preserves_recessed_entrances_and_chamfered_corners(self):
        vertices, faces = self.meshes["swc_bldg_b_sky04"]
        for point in ((0, 17), (0, -17), (-30, 16), (30, -16)):
            with self.subTest(clear=point):
                self.assertFalse(intersections(vertices, faces, (*point, -1), (*point, 160)))
        for start, end in (((0, 18, .2), (0, 14, .2)), ((0, -18, .2), (0, -14, .2)),
                           ((35, 0, .2), (30, 0, .2))):
            with self.subTest(wall=start):
                self.assertTrue(intersections(vertices, faces, start, end))

    def test_prison_spires_block_entry_above_the_main_roof(self):
        vertices, faces = self.meshes["swc_prison01"]
        # Mesh1's two tall spires and their Mesh5 caps in swc_prison01.mdl.
        # Sample the broad bases, tapered shafts, collars and narrow top caps.
        spires = (
            ((5.4714, -10.93675), (36.131, 37, 38, 41, 42.1, 45, 49.8, 50.5, 51.4, 51.458)),
            ((2.187, -6.56), (36.131, 38, 39.5, 40, 46.8, 47.3, 47.41)),
        )
        for (x, y), heights in spires:
            for z in heights:
                for dx, dy in ((-3, 0), (3, 0), (0, -3), (0, 3)):
                    with self.subTest(spire=(x, y), z=z, approach=(dx, dy)):
                        self.assertEqual(len(intersections(vertices, faces,
                                                          (x+dx, y+dy, z), (x, y, z))), 1)
            # Each spire joins the main body with no buried cap inside the solid.
            self.assertEqual(len(intersections(vertices, faces, (x, y, 35), (x, y, 52))), 1)

    def test_prison_keeps_space_between_and_above_spires_clear(self):
        vertices, faces = self.meshes["swc_prison01"]
        for z in (36.14, 40, 48, 51.5):
            with self.subTest(clear_roof=z):
                self.assertFalse(intersections(vertices, faces, (-20, 0, z), (20, 0, z)))
        # Raising the whole footprint, or extruding either spire's broad base to
        # its full height, would add invisible walls at these clear model points.
        for x, y, bottom, top in ((6.3, -10.94, 38, 52), (2.45, -6.56, 40, 52),
                                  (2.187, -6.56, 47.43, 52), (5.4714, -10.93675, 51.46, 52)):
            with self.subTest(clear_column=(x, y)):
                self.assertFalse(intersections(vertices, faces, (x, y, bottom), (x, y, top)))


if __name__ == "__main__":
    unittest.main()
