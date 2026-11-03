import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("update_forge_bible", ROOT / "tools" / "UpdateForgeBible.py")
BIBLE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BIBLE)


class UpdateForgeBibleTests(unittest.TestCase):
    def test_every_band_multiplier_is_read_from_the_rules_in_code(self):
        bands = BIBLE.read_multipliers()

        self.assertEqual(5, len(bands))
        for _, multiplier in bands:
            self.assertRegex(multiplier, r"^\d+(\.\d+)?$")
            self.assertGreaterEqual(float(multiplier), 1.0)
        self.assertGreater(float(bands[0][1]), 1.0, "the lowest band is the one that needs a multiplier")

    def test_the_text_is_plain_ascii_and_lists_every_band(self):
        bands = [("levels 1-10", "1.5"), ("levels 11-20", "1.1"), ("level 41 and up", "1.0")]

        text = BIBLE.entry_text(bands)

        self.assertTrue(all(ord(character) < 127 for character in text))
        self.assertTrue(text.startswith(BIBLE.MARKER))
        self.assertIn("1.5 at levels 1-10, 1.1 at levels 11-20, 1.0 at level 41 and up", text)

    def test_the_committed_workbook_matches_the_rules_in_code(self):
        self.assertFalse(
            BIBLE.update_workbook(BIBLE.DEFAULT_WORKBOOK, check=True),
            "run python tools/UpdateForgeBible.py after changing the ForgeRules.XpMultiplier constants")


if __name__ == "__main__":
    unittest.main()
