import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("update_forge_bible", ROOT / "tools" / "UpdateForgeBible.py")
BIBLE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BIBLE)


class UpdateForgeBibleTests(unittest.TestCase):
    def test_the_multiplier_is_read_from_the_rules_in_code(self):
        multiplier = BIBLE.read_multiplier()

        self.assertRegex(multiplier, r"^\d+(\.\d+)?$")
        self.assertGreater(float(multiplier), 1.0)

    def test_the_text_is_plain_ascii_and_carries_the_multiplier(self):
        text = BIBLE.entry_text("1.6")

        self.assertTrue(all(ord(character) < 127 for character in text))
        self.assertTrue(text.startswith(BIBLE.MARKER))
        self.assertIn("multiplied by 1.6", text)

    def test_the_committed_workbook_matches_the_rules_in_code(self):
        self.assertFalse(
            BIBLE.update_workbook(BIBLE.DEFAULT_WORKBOOK, check=True),
            "run python tools/UpdateForgeBible.py after changing ForgeRules.XpMultiplier")


if __name__ == "__main__":
    unittest.main()
