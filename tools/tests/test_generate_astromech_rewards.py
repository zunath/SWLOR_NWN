"""Astromech forms of reward and found gear: the naming, resref and store rules, and that the generated files are current."""
import importlib
import subprocess
import sys
import unittest
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOLS))
rewards = importlib.import_module("GenerateAstromechRewards")


class LootFormRuleTests(unittest.TestCase):
    def test_the_module_name_takes_the_place_of_the_humanoid_slot_word(self):
        self.assertEqual(rewards.loot_name("Overcharge Sash", 21, {}, "x"), "Overcharge Power Cell")
        self.assertEqual(rewards.loot_name("Dune Glare Visor", 17, {}, "x"), "Dune Glare Sensor Package")
        self.assertEqual(rewards.loot_name("Killbox Carbine", rewards.RIFLE[0], {}, "x"), "Killbox Blaster Mount")
        self.assertEqual(rewards.loot_name("Wrecking Beam Staff", rewards.STAFF[0], {}, "x"), "Wrecking Beam Shock Prod")

    def test_a_name_without_a_slot_word_keeps_all_of_itself(self):
        self.assertEqual(rewards.loot_name("DLHL-005 K", 17, {}, "x"), "DLHL-005 K Sensor Package")
        self.assertEqual(rewards.loot_name("Visor", 17, {}, "x"), "Visor Sensor Package", "a lone slot word is the name")

    def test_a_listed_name_wins(self):
        self.assertEqual(rewards.loot_name("Sacred Boots", 26, {"sc_boots": "Sacred Light Drive Unit"}, "sc_boots"),
                         "Sacred Light Drive Unit")

    def test_a_resref_is_squeezed_into_a_resource_name(self):
        self.assertEqual(rewards.loot_resref("sc_boots"), "am_sc_boots")
        self.assertEqual(rewards.loot_resref("oneshot_w2abcd"), "am_oneshotw2abcd", "underscores go first")
        first, second = rewards.loot_resref("barrieroverseda"), rewards.loot_resref("barrieroversedb")
        self.assertEqual((first, second), ("am_barrieroverda", "am_barrieroverdb"), "the last two characters stay")
        for source in ("a", "abcdefghijklm", "abcdefghijklmn", "abc_defghijklmnop"):
            self.assertLessEqual(len(rewards.loot_resref(source)), 16)

    def test_every_found_form_is_distinct_and_listed_once(self):
        loot = [twin for twin in rewards.TWINS if twin.found]
        self.assertGreater(len(loot), 1000)
        self.assertEqual(len({twin.resref for twin in rewards.TWINS}), len(rewards.TWINS))
        self.assertEqual(len({twin.source for twin in rewards.TWINS}), len(rewards.TWINS))
        self.assertEqual(len({twin.name for twin in rewards.TWINS}), len(rewards.TWINS))
        for twin in loot:
            self.assertFalse(twin.resref.startswith(rewards.RESERVED_PREFIXES), twin.resref)
            self.assertTrue(twin.name.isascii(), twin.name)

    def test_gear_is_found_in_loot_stores_and_slicing_rewards(self):
        sources = {twin.source for twin in rewards.TWINS if twin.found}
        for source in ("cara_armor", "fabr_bracer_1", "slg_lastcover"):  # looted, sold by a guild store, sliced
            self.assertIn(source, sources)
        self.assertIn("fabr_bracer_1", rewards.store_stock()["gp_fab_1"])
        self.assertIn("slg_lastcover", rewards.slicing_resrefs())

    def test_a_piece_that_asks_for_no_rank_is_not_gear(self):
        forms = dict(rewards.all_forms())
        plain = [source for source in rewards.store_stock()["startingitems"]
                 if (item := rewards.read_item(source)) is not None and not rewards.asks_for_a_rank(item)]
        self.assertTrue(plain)
        self.assertFalse([source for source in plain if source in forms])

    def test_a_droid_copy_in_a_store_is_sold_as_the_form_of_the_humanoid_piece(self):
        forms = dict(rewards.all_forms())
        self.assertNotIn("dhar005", forms, "a droid copy sold by a store has no form of its own")
        self.assertEqual(rewards.stats(rewards.read_item("dhar005")), rewards.stats(rewards.read_item("ec_armor")))
        self.assertEqual(rewards.store_equals(["ec_armor", "dhar005", "nothing_here"], forms),
                         [("dhar005", forms["ec_armor"])])
        # Sold alone, the droid copy still brings a form with its stats.
        (source, form), = rewards.store_equals(["dhar005"], forms)
        humanoid = next(piece for piece, resref in forms.items() if resref == form)
        self.assertEqual(rewards.stats(rewards.read_item(humanoid)), rewards.stats(rewards.read_item("dhar005")))

    def test_a_looted_droid_piece_keeps_a_form_of_its_own(self):
        forms = dict(rewards.all_forms())
        self.assertIn("dhar005s", rewards.looted_resrefs())
        self.assertTrue(rewards.is_droid_only(rewards.read_item("dhar005s")))
        self.assertIn("dhar005s", forms)

    def test_generated_files_match_the_generator(self):
        result = subprocess.run([sys.executable, str(TOOLS / "GenerateAstromechRewards.py"), "--check"],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, (result.stdout + result.stderr)[-2000:])


if __name__ == "__main__":
    unittest.main()
