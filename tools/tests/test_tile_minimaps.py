import sys
from pathlib import Path
import unittest

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from TileMinimaps import replace_images, sections
from RenderTileMinimaps import meshes, raster_triangle


class TileMinimapTests(unittest.TestCase):
    def test_set_edit_preserves_other_tiles_and_fields(self):
        source = (b"; header\r\n[TILES]\r\nCount=2\r\n[TILE0]\r\nModel=foo\r\n"
                  b"ImageMap2D=missing\r\n[TILE0DOOR0]\r\nType=7\r\n"
                  b"[TILE1]\r\nModel=bar\r\nImageMap2D=keep\r\n")
        edited = replace_images(source, {0: "mi_foo"})
        self.assertEqual(source.replace(b"ImageMap2D=missing", b"ImageMap2D=mi_foo"), edited)

    def test_missing_field_is_added_to_correct_tile(self):
        source = b"[TILE0]\r\nModel=foo\r\n[TILE1]\r\nModel=bar\r\n"
        edited = replace_images(source, {0: "mi_foo"})
        doc = sections(edited)
        self.assertEqual("mi_foo", doc["tile0"]["imagemap2d"])
        self.assertNotIn("imagemap2d", doc["tile1"])
        self.assertNotIn(b"\n", edited.replace(b"\r\n", b""))

    def test_invalid_resref_and_missing_tile_are_rejected(self):
        for changes in ({0: "x" * 17}, {1: "mi_foo"}):
            with self.assertRaises(ValueError):
                replace_images(b"[TILE0]\nModel=foo\n", changes)

    def test_parent_rotation_applies_to_mesh_positions(self):
        text = """node dummy root
 parent NULL
 position 1 2 0
 orientation 0 0 1 1.5707963267948966
endnode
node trimesh floor
 parent root
 position 2 0 0
 bitmap floor
 verts 3
 0 0 0
 1 0 0
 0 1 0
 faces 1
 0 1 2 0 0 1 2 0
endnode
endmodelgeom root
"""
        mesh = next(meshes(text))
        np.testing.assert_allclose(mesh["vertices"], [[1, 4, 0], [1, 5, 0], [0, 4, 0]], atol=1e-10)

    def test_north_is_top_and_higher_geometry_occludes(self):
        pixels = np.zeros((10, 10, 3))
        depth = np.full((10, 10), -np.inf)
        points = np.array([[-5, 5, 1], [5, 5, 1], [-5, -5, 1]], dtype=float)
        raster_triangle(points, None, None, np.array([1, 0, 0]), 1, pixels, depth)
        raster_triangle(points - [0, 0, 1], None, None, np.array([0, 1, 0]), 1, pixels, depth)
        np.testing.assert_equal(pixels[0, 0], [1, 0, 0])
        np.testing.assert_equal(pixels[-1, -1], [0, 0, 0])


if __name__ == "__main__":
    unittest.main()
