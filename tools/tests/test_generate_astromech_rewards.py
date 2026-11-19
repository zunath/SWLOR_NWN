"""Astromech forms of reward and looted gear: the naming and resref rules, and that the generated files are current."""
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

    def test_every_loot_form_is_distinct_and_listed_once(self):
        loot = [twin for twin in rewards.TWINS if twin.looted]
        self.assertGreater(len(loot), 700)
        self.assertEqual(len({twin.resref for twin in rewards.TWINS}), len(rewards.TWINS))
        self.assertEqual(len({twin.source for twin in rewards.TWINS}), len(rewards.TWINS))
        self.assertEqual(len({twin.name for twin in rewards.TWINS}), len(rewards.TWINS))
        for twin in loot:
            self.assertFalse(twin.resref.startswith(rewards.RESERVED_PREFIXES), twin.resref)
            self.assertTrue(twin.name.isascii(), twin.name)

    def test_generated_files_match_the_generator(self):
        result = subprocess.run([sys.executable, str(TOOLS / "GenerateAstromechRewards.py"), "--check"],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, (result.stdout + result.stderr)[-2000:])


if __name__ == "__main__":
    unittest.main()
