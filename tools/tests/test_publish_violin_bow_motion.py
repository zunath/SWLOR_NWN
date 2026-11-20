"""Guards for narrow bow-track publication and preservation of native motion."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import PublishViolinBowMotion as bow


def bank(value=0, dense=False):
    clips = []
    for clip in sorted(bow.CLIPS | {"unrelated"}):
        nodes = ["  node dummy rootdummy\n    parent model\n    positionkey " +
                 ("3\n      0 0 0 1\n      .5 0 0 1\n      1 0 0 1\n" if dense else
                  "2\n      0 0 0 1\n      1 0 0 1\n") + "  endnode\n"]
        for joint in sorted(bow.JOINTS):
            angle = value if clip in bow.CLIPS else 0
            nodes.append(f"  node dummy {joint}\n    parent rootdummy\n    orientationkey 2\n"
                         f"      0 0 0 1 {angle}\n      1 0 0 1 {angle}\n  endnode\n")
        if clip == "unrelated":
            nodes[0] = "  node dummy rootdummy\n    parent model\n    position 0 0 1\n  endnode\n"
        clips.append(f"newanim {clip} model\n  length 1\n" + "".join(nodes) + f"doneanim {clip} model\n")
    return "# SWLOR authored animations for model\n" + "".join(clips)


class PublishViolinBowMotionTests(unittest.TestCase):
    def test_dense_bow_keys_preserve_body_position_tracks_and_other_clips(self):
        before = bank()
        candidate = bank(.7, True)
        actual, count = bow.publish(before, candidate)
        self.assertEqual(60, count)
        self.assertEqual(bank(.7), actual)
        self.assertNotIn("positionkey 3", actual)

    def test_rejects_timing_changes(self):
        with self.assertRaisesRegex(ValueError, "timing"):
            bow.publish(bank(), bank(.7).replace("length 1", "length 2", 1))

    def test_rejects_unrelated_motion_changes(self):
        candidate = bank(.7).replace("position 0 0 1", "position 0 0 2")
        with self.assertRaisesRegex(ValueError, "unrelated"):
            bow.publish(bank(), candidate)

    def test_rejects_parent_binding_changes(self):
        with self.assertRaisesRegex(ValueError, "binding"):
            bow.publish(bank(), bank(.7).replace("parent rootdummy", "parent wrong", 1))

    def test_rejects_missing_bow_controller(self):
        candidate = bank(.7).replace("orientationkey 2", "invalidkey 2", 1)
        with self.assertRaisesRegex(ValueError, "orientation"):
            bow.publish(bank(), candidate)

    def test_rejects_model_geometry_changes(self):
        with self.assertRaisesRegex(ValueError, "geometry"):
            bow.publish(bank(), bank(.7) + "node dummy unexpected\n  parent model\nendnode\n")


if __name__ == "__main__":
    unittest.main()
