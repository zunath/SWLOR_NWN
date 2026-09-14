"""Keep shared robe animation libraries in their bounded dedicated HAKs."""
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("hak_reorganization", Path(__file__).resolve().parents[1] / "reorganize_hak_sources.py")
reorg = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reorg)


class AnimationHakRoutingTests(unittest.TestCase):
    def test_bridge_routing_survives_reorganization(self):
        for name, expected in (("pmh_ra001", "sw_anim_m"), ("pfa_ra999", "sw_anim_f")):
            for prior in ("sw_pt_root", expected):
                record = {"stem": name, "ext": "mdl", "source_hak": prior}
                self.assertEqual(expected, reorg.initial_owner(record, {}, [], {}))

    def test_body_roots_remain_in_root_archive(self):
        record = {"stem": "pmh34", "ext": "mdl", "source_hak": "sw_pt_root"}
        self.assertEqual("sw_pt_root", reorg.initial_owner(record, {}, [], {}))

    def test_both_archive_names_are_required_and_resource_safe(self):
        for name in ("sw_anim_m", "sw_anim_f"):
            self.assertEqual(1, reorg.TARGET_HAKS.count(name))
            self.assertLessEqual(len(name), 16)


if __name__ == "__main__":
    unittest.main()
