"""Run with: python -m unittest discover -s tools/tests -p test_weapon_grip_fit.py"""
import importlib.util
import unittest
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("weapon_grip_fit", ROOT / "tools/WeaponGripFit.py")
fit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fit)

HAKS_READY = all((fit.HAKS / folder).is_dir() for folder in fit.MODEL_FOLDERS)


class CatalogTests(unittest.TestCase):
    def test_reads_body_and_hand_moves_from_the_server_catalog(self):
        bodies, hands = fit.read_catalog()
        self.assertIn("pmz0_handr", bodies)
        self.assertIn("pmo0_handr201", hands)
        self.assertEqual(3, len(bodies["pmz0_handr"]))

    def test_a_hand_takes_its_own_move_then_its_bodys_then_none(self):
        bodies, hands = fit.read_catalog()
        np.testing.assert_allclose(fit.catalog_move("PMO0_HANDR201"), hands["pmo0_handr201"])
        np.testing.assert_allclose(fit.catalog_move("pmz0_handr009"), bodies["pmz0_handr"])
        np.testing.assert_allclose(fit.catalog_move("pmh0_handr001"), np.zeros(3))

    def test_moves_stay_within_the_limit(self):
        bodies, hands = fit.read_catalog()
        for key, move in {**bodies, **hands}.items():
            self.assertLessEqual(np.linalg.norm(move), fit.MAX_MOVE, key)

    @unittest.skipUnless(HAKS_READY, "needs the hand, weapon and body root hak folders")
    def test_committed_catalog_passes_the_check(self):
        self.assertEqual([], fit.check())

    @unittest.skipUnless(fit.RIFLE_BANKS.is_file(), "needs the SWLOR_Haks tools folder")
    def test_rifle_hold_banks_are_solved_for_the_catalog_moves(self):
        self.assertEqual([], fit.check_rifle_banks())

    def test_a_catalog_entry_is_written_in_millimetre_steps(self):
        self.assertEqual("new(0.008f, 0f, -0.012f)", fit.entry([0.008, 0.0001, -0.0124]))
        self.assertEqual("new(0f, 0f, 0.02f)", fit.entry([-0.0002, 0, 0.02]))


class DerivedMoveTests(unittest.TestCase):
    def test_the_reference_body_is_never_moved(self):
        move, how = fit.derived_move("pmh0_handr009")
        np.testing.assert_allclose(move, np.zeros(3))
        self.assertEqual("reference", how)

    def test_a_reviewed_reference_hand_keeps_its_catalog_move(self):
        move, how = fit.derived_move("pmh0_handr204")
        np.testing.assert_allclose(move, fit.read_catalog()[1]["pmh0_handr204"])
        self.assertEqual("reference", how)

    def test_the_left_anchor_mirrors_the_right(self):
        np.testing.assert_allclose(fit.anchor("l"), fit.anchor("r") * [-1, 1, 1])

    @unittest.skipUnless(HAKS_READY, "needs the hand, weapon and body root hak folders")
    def test_a_scaled_copy_of_a_reference_glove_is_derived_exactly_and_matches_the_catalog(self):
        move, how = fit.derived_move("pmd0_handr003")
        self.assertEqual("exact", how)
        self.assertLessEqual(np.linalg.norm(move - fit.catalog_move("pmd0_handr003")), fit.DERIVED_TOLERANCE)

    @unittest.skipUnless(HAKS_READY, "needs the hand, weapon and body root hak folders")
    def test_laying_surfaces_over_each_other_agrees_with_shared_vertices(self):
        exact = fit.exact_map("pmh0_handr063", "pfh0_handr063")
        registered = fit.registered_map("pmh0_handr063", "pfh0_handr063")
        self.assertIsNotNone(exact)
        self.assertIsNotNone(registered)
        point = np.append(fit.anchor("r"), 1)
        self.assertLess(np.linalg.norm(point @ exact - point @ registered), 0.002)

    @unittest.skipUnless(HAKS_READY, "needs the hand, weapon and body root hak folders")
    def test_a_hand_with_no_model_on_the_reference_body_is_left_to_the_eye(self):
        self.assertEqual((None, "by eye"), fit.derived_move("pmo0_handr201"))


class WeaponSheetTests(unittest.TestCase):
    @unittest.skipUnless(HAKS_READY, "needs the hand, weapon and body root hak folders")
    def test_every_compiled_cannon_is_drawn_in_the_reference_hand_and_the_rest_are_listed(self):
        import tempfile
        with tempfile.TemporaryDirectory() as folder:
            written, skipped = fit.write_weapon_sheets("wbwxh", Path(folder))
            self.assertTrue(written)
            self.assertTrue(all(path.is_file() for path in written))
        self.assertTrue(all(name.startswith("wbwxh_m_") for name in skipped))

    def test_an_uncompiled_model_is_read_with_its_nodes_placed(self):
        text = """node dummy gun
  parent NULL
endnode
node trimesh body
  parent gun
  position 0 0.1 0
  orientation 1 0 0 3.14159265
  verts 3
    0 0 0
    1 0 0
    0 0.5 0
  faces 1
    0 1 2 1 0 0 0 0
endnode
node trimesh empty
  parent gun
  verts 0
  faces 0
endnode
node trimesh hidden
  parent gun
  render 0
  verts 3
    0 0 0
    1 0 0
    0 1 0
  faces 1
    0 1 2 1 0 0 0 0
endnode
endmodelgeom gun
"""
        vertices, faces = fit.ascii_mesh(text)
        self.assertEqual(faces.tolist(), [[0, 1, 2]])
        np.testing.assert_allclose(vertices, [[0, 0.1, 0], [1, 0.1, 0], [0, -0.4, 0]], atol=1e-6)


class RifleMoveTests(unittest.TestCase):
    def test_a_move_along_the_turn_axis_is_the_same_in_every_clip(self):
        axis = np.array(fit.RIFLE_READY_TURN[:3])
        axis = axis / np.linalg.norm(axis) * 0.02
        np.testing.assert_allclose(fit.rifle_move(axis, "pmo0_handr201"), axis, atol=1e-5)

    def test_a_rifle_move_is_halfway_between_the_idle_and_ready_moves(self):
        idle = np.array([0.022, -0.005, -0.015])
        rifle = fit.rifle_move(idle, "pmo0_handr201")
        ready = 2 * rifle - idle
        self.assertAlmostEqual(np.linalg.norm(ready), np.linalg.norm(idle), places=5)
        self.assertLess(np.linalg.norm(rifle - idle), np.linalg.norm(idle) / 2)

    def test_the_female_dwarf_and_half_orc_frames_turn_less(self):
        move = np.array([0.0, 0.02, 0.0])
        tall = np.linalg.norm(fit.rifle_move(move, "pfh0_handr001") - move)
        self.assertLess(np.linalg.norm(fit.rifle_move(move, "pfd0_handr001") - move), tall)
        np.testing.assert_allclose(fit.rifle_move(move, "PFO0_handr201"), fit.rifle_move(move, "pfd0_handr001"))


class GeometryTests(unittest.TestCase):
    def test_identity_quaternion_is_no_rotation(self):
        np.testing.assert_allclose(fit.quaternion_matrix((0, 0, 0, 1)), np.eye(3))

    def test_quarter_turn_about_z_moves_x_onto_y(self):
        half = np.sqrt(0.5)
        rotated = fit.quaternion_matrix((0, 0, half, half)) @ np.array([1.0, 0.0, 0.0])
        np.testing.assert_allclose(rotated, [0.0, 1.0, 0.0], atol=1e-12)

    def test_a_plane_cuts_a_triangle_into_one_segment(self):
        vertices = np.array([[0.0, 0.0, -1.0], [1.0, 0.0, 1.0], [-1.0, 0.0, 1.0]])
        segments = fit.cut(vertices, np.array([[0, 1, 2]]), np.zeros(3), np.array([0.0, 0.0, 1.0]))
        self.assertEqual(1, len(segments))
        ends = sorted(point[0] for point in segments[0])
        np.testing.assert_allclose(ends, [-0.5, 0.5])

    def test_a_plane_that_misses_a_triangle_cuts_nothing(self):
        vertices = np.array([[0.0, 0.0, 1.0], [1.0, 0.0, 2.0], [-1.0, 0.0, 2.0]])
        self.assertEqual([], fit.cut(vertices, np.array([[0, 1, 2]]), np.zeros(3), np.array([0.0, 0.0, 1.0])))

    def test_surface_samples_cover_a_triangle_without_leaving_it(self):
        vertices = np.array([[0.0, 0.0, 0.0], [0.01, 0.0, 0.0], [0.0, 0.01, 0.0]])
        points = fit.surface_samples(vertices, np.array([[0, 1, 2]]), 0.002)
        self.assertGreater(len(points), 10)
        self.assertTrue(np.all(points >= -1e-12))
        self.assertTrue(np.all(points[:, 0] + points[:, 1] <= 0.01 + 1e-12))


if __name__ == "__main__":
    unittest.main()
