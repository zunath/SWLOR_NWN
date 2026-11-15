"""MeshyCreatureExport: reference parsing, skinned ASCII output and the paw stance used to spread joints."""
from pathlib import Path
import sys
import unittest

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import MeshyCreatureExport as creature

REFERENCE = """newmodel c_test
setsupermodel c_test c_wolf
beginmodelgeom c_test
node dummy c_test
  parent NULL
endnode
node dummy wolf_rootdummy
  parent c_test
  position 0 -0.4 0.8
  orientation 0 0 1 0
endnode
node trimesh wolf_pelvis
  parent wolf_rootdummy
  position 0 0.3 0
  orientation 1 0 0 0.25
  bitmap c_test
  verts 3
    0 0 0
    0.1 0 0
    0 0.1 0
  tverts 3
    0 0 0
    1 0 0
    0 1 0
  faces 1
    0 1 2 1 0 1 2 0
endnode
endmodelgeom c_test
newanim walk c_test
  node dummy wolf_pelvis
    orientationkey 1
      0 0 0 1 0
  endnode
doneanim walk c_test
"""


class ReferenceTests(unittest.TestCase):
    def test_parse_reads_geometry_only(self):
        nodes = creature.parse_reference(REFERENCE)
        self.assertEqual([n['name'] for n in nodes], ['c_test', 'wolf_rootdummy', 'wolf_pelvis'])
        pelvis = nodes[2]
        self.assertEqual(pelvis['parent'], 'wolf_rootdummy')
        self.assertEqual(pelvis['orientation'], (1.0, 0.0, 0.0, 0.25))
        self.assertEqual(pelvis['verts'][1], (0.1, 0.0, 0.0))
        self.assertEqual(pelvis['faces'], [(0, 1, 2)])

    def test_parse_rejects_a_reference_without_a_root(self):
        with self.assertRaises(ValueError):
            creature.parse_reference('node dummy a\n  parent b\nendnode\n')


class OutputTests(unittest.TestCase):
    def mesh(self, weights=None):
        verts = [(0, 0, 0), (1, 0, 0), (0, 1, 0)]
        normals = [(0, 0, 1)] * 3
        uvs = [(0, 0), (1, 0), (0, 1)]
        return verts, normals, uvs, [(0, 1, 2)], weights or [[('wolf_pelvis', 1.0)]] * 3

    def test_reference_nodes_become_dummies_and_the_root_takes_the_new_name(self):
        nodes = creature.parse_reference(REFERENCE)
        text = creature.write_mdl('c_new', 'c_wolf', nodes, 'c_test', self.mesh(), 'c_new')
        self.assertIn('setsupermodel c_new c_wolf', text)
        self.assertIn('node dummy c_new\n  parent NULL', text)
        self.assertIn('node dummy wolf_rootdummy\n  parent c_new', text)
        self.assertIn('node dummy wolf_pelvis\n  parent wolf_rootdummy', text)
        self.assertNotIn('trimesh', text)
        self.assertIn('node skin c_new_skin', text)
        self.assertIn('  weights 3\n    wolf_pelvis 1.000000', text)

    def test_rejects_a_vertex_without_unit_normal(self):
        verts, normals, uvs, faces, weights = self.mesh()
        normals = [(0, 0, 2)] * 3
        with self.assertRaises(ValueError):
            creature.write_mdl('c_new', 'c_wolf', creature.parse_reference(REFERENCE), 'c_test',
                               (verts, normals, uvs, faces, weights), 'c_new')

    def test_rejects_missing_weights(self):
        verts, normals, uvs, faces, weights = self.mesh()
        with self.assertRaises(ValueError):
            creature.write_mdl('c_new', 'c_wolf', creature.parse_reference(REFERENCE), 'c_test',
                               (verts, normals, uvs, faces, weights[:2]), 'c_new')


class StanceTests(unittest.TestCase):
    def test_stance_measures_paw_spacing_not_the_tail(self):
        # Four paws at y = +-0.5, x = +-0.2, a body above, and a long tail reaching y = -2 well above the floor.
        paws = [(x, y, 0.0) for x in (-0.2, 0.2) for y in (-0.5, 0.5)]
        body = [(0, y, 1.0) for y in np.linspace(-0.6, 0.8, 8)]
        tail = [(0, y, 0.9) for y in np.linspace(-0.6, -2.0, 8)]
        width, length, middle = creature.foot_stance(np, np.array(paws + body + tail))
        self.assertAlmostEqual(width, 0.2)
        self.assertAlmostEqual(length, 1.0)
        self.assertAlmostEqual(middle, 0.0)


if __name__ == '__main__':
    unittest.main()
