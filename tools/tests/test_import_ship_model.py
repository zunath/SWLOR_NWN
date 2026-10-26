"""Ship model import tests. Run under Python, or Blender's Python for the GLB export test."""
import hashlib
import importlib.util
import json
import re
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("ship", ROOT / "tools/ImportShipModel.py")
ship = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ship)

try:
    import bpy
except ImportError:
    bpy = None

REFERENCE = """#MAXMODEL ASCII
newmodel c_test_ship
  classification CHARACTER
  setsupermodel c_test_ship null
beginmodelgeom c_test_ship
node dummy c_test_ship
  parent NULL
endnode
node dummy headconjure
  parent c_test_ship
  #part-number 1
  position 0 0.05 0.7
endnode
node dummy impact
  parent c_test_ship
  position 0 0 0.6
endnode
node emitter reactor
  parent c_test_ship
  position 0 -0.25 0.52
  texture fxpa_flare
  birthrate 30
endnode
node trimesh body
  parent c_test_ship
  position 0 0 0.5
  orientation 0 0 1 0
  bitmap c_test_ship
  verts 3
    -0.25 -0.4 -0.05
    0.25 -0.4 0.05
    0 0.5 0
  faces 1
    0 1 2 1 0 1 2 0
endnode
endmodelgeom c_test_ship
donemodel c_test_ship
"""


def manifest(**changes):
    config = {"schema_version": 1, "source": "a.glb", "sha256": "0" * 64, "model": "c_test_h1",
              "texture": "c_test_h1", "reference": "ref.mdl", "orientation": [[1, 0, 0], [0, 1, 0], [0, 0, 1]]}
    config.update(changes)
    return config


class ManifestTests(unittest.TestCase):
    def test_defaults_are_filled(self):
        config = ship.validate_manifest(manifest())
        self.assertEqual((config["max_faces"], config["texture_size"]), (4000, 1024))

    def test_rejects_long_or_uppercase_resrefs(self):
        for bad in ("c_test_hull_design_1", "C_Test"):
            with self.assertRaises(ValueError):
                ship.validate_manifest(manifest(model=bad))

    def test_rejects_mirroring_orientation(self):
        with self.assertRaisesRegex(ValueError, "proper rotation"):
            ship.validate_manifest(manifest(orientation=[[-1, 0, 0], [0, 1, 0], [0, 0, 1]]))

    def test_rejects_non_power_of_two_texture(self):
        with self.assertRaises(ValueError):
            ship.validate_manifest(manifest(texture_size=1000))


class ReferenceTests(unittest.TestCase):
    def test_keeps_non_mesh_nodes_and_measures_offset_hull(self):
        model, nodes, (low, high) = ship.parse_reference(REFERENCE)
        self.assertEqual(model, "c_test_ship")
        self.assertEqual([n["name"] for n in nodes], ["headconjure", "impact", "reactor"])
        self.assertNotIn("#part-number", nodes[0]["body"])
        self.assertEqual(low, [-0.25, -0.4, 0.45])
        self.assertEqual(high, [0.25, 0.5, 0.55])

    def test_rejects_animated_reference(self):
        with self.assertRaisesRegex(ValueError, "animations"):
            ship.parse_reference(REFERENCE + "newanim fly c_test_ship\n")

    def test_requires_attach_points(self):
        text = re.sub(r"node dummy impact.*?endnode\n", "", REFERENCE, flags=re.S)
        with self.assertRaisesRegex(ValueError, "headconjure or impact"):
            ship.parse_reference(text)


class FitTests(unittest.TestCase):
    def test_matches_longest_footprint_and_center(self):
        scale, offset = ship.fit_transform([-1, -2, 0], [1, 2, 1], [-0.25, -0.4, 0.45], [0.25, 0.5, 0.55])
        self.assertAlmostEqual(scale, 0.9 / 4)
        fitted = [[scale * p + o for p, o in zip(point, offset)] for point in ([-1, -2, 0], [1, 2, 1])]
        self.assertAlmostEqual((fitted[0][1] + fitted[1][1]) / 2, 0.05)
        self.assertAlmostEqual((fitted[0][2] + fitted[1][2]) / 2, 0.5)
        self.assertAlmostEqual(fitted[1][1] - fitted[0][1], 0.9)


class WriteTests(unittest.TestCase):
    def test_writes_root_reference_nodes_and_hull(self):
        _, nodes, _ = ship.parse_reference(REFERENCE)
        mesh = ([(0, 0, 0), (1, 0, 0), (0, 1, 0)], [(0, 0), (1, 0), (0, 1)], [(0, 1, 2)], [(0, 0, 1)] * 3)
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "c_test_h1.mdl"
            ship.write_mdl(path, "c_test_h1", "c_test_h1", nodes, mesh, {"reactor": [0, -0.3, 0.5]})
            text = path.read_text(encoding="ascii")
        self.assertEqual(re.findall(r"(?m)^node (\S+) (\S+)", text),
                         [("dummy", "c_test_h1"), ("dummy", "headconjure"), ("dummy", "impact"),
                          ("emitter", "reactor"), ("trimesh", "hull")])
        self.assertEqual(len(re.findall(r"(?m)^\s*parent c_test_h1$", text)), 4)
        self.assertNotIn("c_test_ship", text)
        self.assertIn("position 0 -0.3 0.5", text)
        self.assertIn("bitmap c_test_h1", text)

    def test_unknown_override_fails(self):
        _, nodes, _ = ship.parse_reference(REFERENCE)
        mesh = ([(0, 0, 0)] * 3, [(0, 0)] * 3, [(0, 1, 2)], [(0, 0, 1)] * 3)
        with tempfile.TemporaryDirectory() as tmp, self.assertRaisesRegex(ValueError, "unknown nodes"):
            ship.write_mdl(Path(tmp) / "x.mdl", "x", "x", nodes, mesh, {"engine": [0, 0, 0]})


@unittest.skipIf(bpy is None, "Run with Blender Python for the GLB export test")
class BlenderExportTests(unittest.TestCase):
    def test_glb_becomes_budgeted_fitted_ship(self):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = Path(tmp)
            bpy.ops.wm.read_factory_settings(use_empty=True)
            # A 4 m long box along Y; the glTF round trip keeps Blender axes.
            bpy.ops.mesh.primitive_cube_add(size=1)
            box = bpy.context.active_object
            box.scale = (1, 2, 0.5)
            bpy.ops.object.transform_apply(scale=True)
            box.modifiers.new("detail", "SUBSURF").levels = 3
            bpy.ops.object.modifier_apply(modifier="detail")
            bpy.ops.mesh.uv_texture_add()
            image = bpy.data.images.new("paint", 64, 64)
            image.pixels.foreach_set([0.5] * (64 * 64 * 4))
            material = bpy.data.materials.new("paint")
            material.use_nodes = True
            node = material.node_tree.nodes.new("ShaderNodeTexImage")
            node.image = image
            principled = material.node_tree.nodes["Principled BSDF"]
            material.node_tree.links.new(node.outputs["Color"], principled.inputs["Base Color"])
            box.data.materials.append(material)
            glb = tmp / "box.glb"
            bpy.ops.export_scene.gltf(filepath=str(glb), export_format="GLB", export_image_format="AUTO")
            (tmp / "ref.mdl").write_text(REFERENCE, encoding="ascii")
            config = manifest(source="box.glb", sha256=hashlib.sha256(glb.read_bytes()).hexdigest(),
                              reference="ref.mdl", max_faces=600, texture_size=256)
            (tmp / "fit.json").write_text(json.dumps(config), encoding="utf8")
            out = Path(tempfile.mkdtemp())
            try:
                ship.blender_export(tmp / "fit.json", out)
                report = json.loads((out / "export_manifest.json").read_text(encoding="utf8"))
                text = (out / "c_test_h1.mdl").read_text(encoding="ascii")
                self.assertTrue((out / "c_test_h1.tga").is_file())
            finally:
                for path in out.iterdir():
                    path.unlink()
                out.rmdir()
        self.assertLessEqual(report["faces"], 600)
        self.assertGreater(report["source_faces"], 600)
        _, _, (low, high) = ship.parse_reference(text)
        self.assertAlmostEqual(high[1] - low[1], 0.9, places=3)
        self.assertAlmostEqual((high[2] + low[2]) / 2, 0.5, places=3)


if __name__ == "__main__":
    import sys
    # Blender passes its own options in sys.argv; test arguments follow "--".
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ([] if bpy else sys.argv[1:])
    unittest.main(argv=["test_import_ship_model"] + argv, verbosity=2)
