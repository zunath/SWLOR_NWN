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
        self.assertIn("pfh0_handr204", hands)
        self.assertEqual(3, len(bodies["pmz0_handr"]))

    def test_a_hand_takes_its_own_move_then_its_bodys_then_none(self):
        bodies, hands = fit.read_catalog()
        np.testing.assert_allclose(fit.catalog_move("PFH0_HANDR204"), hands["pfh0_handr204"])
        np.testing.assert_allclose(fit.catalog_move("pmz0_handr009"), bodies["pmz0_handr"])
        np.testing.assert_allclose(fit.catalog_move("pmh0_handr001"), np.zeros(3))

    def test_moves_stay_within_the_limit(self):
        bodies, hands = fit.read_catalog()
        for key, move in {**bodies, **hands}.items():
            self.assertLessEqual(np.linalg.norm(move), fit.MAX_MOVE, key)

    @unittest.skipUnless(HAKS_READY, "needs the hand, weapon and body root hak folders")
    def test_committed_catalog_passes_the_check(self):
        self.assertEqual([], fit.check())


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
