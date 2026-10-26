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

try:
    import numpy as np
except ImportError:
    np = None

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


NESTED = """newmodel c_nest
beginmodelgeom c_nest
node dummy c_nest
  parent NULL
endnode
node dummy headconjure
  parent c_nest
  position 0 0 1
  scale 0.5
endnode
node dummy impact
  parent c_nest
  position 0 0 0.5
endnode
node dummy rootdummy
  parent c_nest
  position 0 1 0
  orientation 0 0 1 1.5707963
endnode
node emitter reactor
  parent rootdummy
  position 1 0 0
  texture fxpa_flare
endnode
node emitter reactor
  parent c_nest
  position 0 -1 0
  texture fxpa_flare
endnode
node trimesh hull
  parent rootdummy
  orientation 0 0 1 3.1415927
  scale 2
  verts 2
    1 0 0
    0 0.5 0.25
  faces 0
endnode
endmodelgeom c_nest
donemodel c_nest
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

    def test_rejects_out_of_range_material_and_adjustments(self):
        for bad in ({"material": {"ambient": [1.2, 1, 1]}}, {"material": {"specular": [0, 0, 0]}},
                    {"texture_adjust": {"exposure": 3}}, {"texture_adjust": {"gamma": 1}},
                    {"normal_map": "directx"}):
            with self.assertRaises(ValueError, msg=str(bad)):
                ship.validate_manifest(manifest(**bad))

    def test_rejects_emitters_with_node_overrides_and_long_normal_textures(self):
        for bad in ({"emitters": [[0, 0, 0]], "node_positions": {"reactor": [0, 0, 0]}},
                    {"emitters": [[0, 0]]},
                    {"normal_map": "opengl", "texture": "c_test_texture_1"}):
            with self.assertRaises(ValueError, msg=str(bad)):
                ship.validate_manifest(manifest(**bad))

    def test_accepts_lighting_adjustments_and_normal_map(self):
        config = ship.validate_manifest(manifest(material={"ambient": [0.7, 0.7, 0.7]}, normal_map="opengl",
                                                 texture_adjust={"exposure": 0.9, "contrast": 1.1}))
        self.assertEqual(config["normal_map"], "opengl")

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

    def test_flattens_nested_rotated_scaled_nodes(self):
        _, nodes, (low, high) = ship.parse_reference(NESTED)
        by_name = {n["name"]: n for n in nodes}
        self.assertEqual(sorted(by_name), ["headconjure", "impact", "reactor", "reactor_2", "rootdummy"])
        # rootdummy at (0,1,0) turned 90 degrees about Z: its child at local (1,0,0) lands at (0,2,0).
        for actual, expected in zip(by_name["reactor"]["position"], [0, 2, 0]):
            self.assertAlmostEqual(actual, expected, places=5)
        self.assertIn("parent c_nest", by_name["reactor"]["body"])
        self.assertNotIn("parent rootdummy", by_name["reactor"]["body"])
        self.assertNotIn("scale", by_name["headconjure"]["body"])
        # Hull: rotated 90 + 180 degrees about Z and scaled 2 under rootdummy.
        self.assertAlmostEqual(low[0], 0.0, places=5)
        self.assertAlmostEqual(high[0], 1.0, places=5)
        self.assertAlmostEqual(low[1], -1.0, places=5)
        self.assertAlmostEqual(high[1], 1.0, places=5)
        self.assertAlmostEqual(high[2], 0.5, places=5)

    def test_rejects_unknown_parent(self):
        with self.assertRaisesRegex(ValueError, "unknown parent"):
            ship.parse_reference(NESTED.replace("parent rootdummy\n  position 1", "parent ghost\n  position 1"))

    def test_emitters_replace_every_reference_emitter(self):
        _, nodes, _ = ship.parse_reference(NESTED)
        nodes, positions = ship.replace_emitters(nodes, [[-0.1, -1, 0.5], [0.1, -1, 0.5]])
        self.assertEqual([n["name"] for n in nodes if n["type"] == "emitter"], ["reactor1", "reactor2"])
        mesh = ([(0, 0, 0)] * 3, [(0, 0)] * 3, [(0, 1, 2)], [(0, 0, 1)] * 3)
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "x.mdl"
            ship.write_mdl(path, "x", "x", nodes, mesh, positions)
            text = path.read_text(encoding="ascii")
        self.assertEqual(re.findall(r"(?m)^node emitter (\S+)", text), ["reactor1", "reactor2"])
        self.assertIn("position -0.1 -1 0.5", text)
        self.assertIn("position 0.1 -1 0.5", text)

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

    def test_node_copies_add_a_second_engine(self):
        _, nodes, _ = ship.parse_reference(REFERENCE)
        mesh = ([(0, 0, 0)] * 3, [(0, 0)] * 3, [(0, 1, 2)], [(0, 0, 1)] * 3)
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "x.mdl"
            ship.write_mdl(path, "x", "x", nodes, mesh, {"reactor": [-0.1, -0.3, 0.5]},
                           {"reactor2": {"from": "reactor", "position": [0.1, -0.3, 0.5]}})
            text = path.read_text(encoding="ascii")
        self.assertEqual(re.findall(r"(?m)^node emitter (\S+)", text), ["reactor", "reactor2"])
        self.assertEqual(text.count("texture fxpa_flare"), 2)
        self.assertIn("position -0.1 -0.3 0.5", text)
        self.assertIn("position 0.1 -0.3 0.5", text)

    def test_node_copy_of_unknown_node_fails(self):
        _, nodes, _ = ship.parse_reference(REFERENCE)
        mesh = ([(0, 0, 0)] * 3, [(0, 0)] * 3, [(0, 1, 2)], [(0, 0, 1)] * 3)
        with tempfile.TemporaryDirectory() as tmp, self.assertRaisesRegex(ValueError, "unknown node"):
            ship.write_mdl(Path(tmp) / "x.mdl", "x", "x", nodes, mesh, None,
                           {"engine2": {"from": "engine", "position": [0, 0, 0]}})

    def test_material_and_normal_map_lines(self):
        _, nodes, _ = ship.parse_reference(REFERENCE)
        mesh = ([(0, 0, 0)] * 3, [(0, 0)] * 3, [(0, 1, 2)], [(0, 0, 1)] * 3)
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "x.mdl"
            ship.write_mdl(path, "x", "x", nodes, mesh, material={"ambient": [0.7, 0.7, 0.7]}, normal_mapped=True)
            text = path.read_text(encoding="ascii")
        self.assertIn("ambient 0.7 0.7 0.7", text)
        self.assertIn("diffuse 1 1 1", text)
        self.assertIn("materialname x", text)
        self.assertIn("renderhint NormalAndSpecMapped", text)

    @unittest.skipIf(np is None, "numpy is needed for texture adjustment")
    def test_adjust_pixels_darkens_and_keeps_alpha_opaque(self):
        pixels = [1.0, 1.0, 1.0, 0.2, 0.5, 0.5, 0.5, 1.0]
        out = ship.adjust_pixels(pixels, {"exposure": 0.8, "contrast": 1.0})
        self.assertAlmostEqual(float(out[0]), 0.8, places=5)
        self.assertAlmostEqual(float(out[4]), 0.4, places=5)
        self.assertEqual([float(out[3]), float(out[7])], [1.0, 1.0])

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
            bumps = bpy.data.images.new("bumps", 64, 64)
            bumps.pixels.foreach_set([0.5, 0.5, 1.0, 1.0] * (64 * 64))
            bumps.colorspace_settings.name = "Non-Color"
            bump_node = material.node_tree.nodes.new("ShaderNodeTexImage")
            bump_node.image = bumps
            normal = material.node_tree.nodes.new("ShaderNodeNormalMap")
            material.node_tree.links.new(bump_node.outputs["Color"], normal.inputs["Color"])
            material.node_tree.links.new(normal.outputs["Normal"], principled.inputs["Normal"])
            box.data.materials.append(material)
            glb = tmp / "box.glb"
            bpy.ops.export_scene.gltf(filepath=str(glb), export_format="GLB", export_image_format="AUTO")
            (tmp / "ref.mdl").write_text(REFERENCE, encoding="ascii")
            config = manifest(source="box.glb", sha256=hashlib.sha256(glb.read_bytes()).hexdigest(),
                              reference="ref.mdl", max_faces=600, texture_size=256, normal_map="opengl",
                              material={"ambient": [0.7, 0.7, 0.7]}, texture_adjust={"exposure": 0.9})
            (tmp / "fit.json").write_text(json.dumps(config), encoding="utf8")
            out = Path(tempfile.mkdtemp())
            try:
                ship.blender_export(tmp / "fit.json", out)
                report = json.loads((out / "export_manifest.json").read_text(encoding="utf8"))
                text = (out / "c_test_h1.mdl").read_text(encoding="ascii")
                self.assertTrue((out / "c_test_h1.tga").is_file())
                self.assertTrue((out / "c_test_h1_n.tga").is_file())
                mtr = (out / "c_test_h1.mtr").read_bytes()
            finally:
                for path in out.iterdir():
                    path.unlink()
                out.rmdir()
        self.assertEqual(mtr, b"renderhint NormalAndSpecMapped\r\ntexture0 c_test_h1\r\ntexture1 c_test_h1_n\r\ntexture2 null\r\n")
        self.assertIn("materialname c_test_h1", text)
        self.assertEqual(report["normal_map"], "opengl")
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
