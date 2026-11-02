import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("update_reaction_bible", ROOT / "tools" / "UpdateReactionBible.py")
BIBLE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BIBLE)

TEXT = BIBLE.row_text("3.0", "10", "10")


def sheet(rows):
    return (
        '<?xml version="1.0" encoding="utf-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">'
        f"<sheetData>{rows}</sheetData><mergeCells count=\"0\" /></worksheet>"
    )


def numbered(number, ordinal, text):
    return (
        f'<row r="{number}"><c r="A{number}" s="3"><v>{ordinal}</v></c>'
        f'<c r="B{number}" s="4" t="inlineStr"><is><t xml:space="preserve">{text}</t></is></c></row>'
    )


def blank_rows(first, last):
    return "".join(f'<row r="{n}" />' for n in range(first, last + 1))


class UpdateReactionBibleTests(unittest.TestCase):
    def test_adds_the_row_after_the_last_numbered_row_and_before_the_empty_rows(self):
        xml = sheet(numbered(3, "1.0", "One") + numbered(4, "2.0", "Two") + blank_rows(22, 24))

        updated = BIBLE.update_sheet(xml, TEXT)

        self.assertIn('<row r="5"><c r="A5" s="3"><v>3.0</v></c><c r="B5" s="4" t="inlineStr">', updated)
        self.assertLess(updated.index('<row r="5">'), updated.index('<row r="22" />'))
        self.assertIn("Reaction minigame", updated)

    def test_a_second_run_changes_nothing(self):
        xml = sheet(numbered(3, "1.0", "One") + blank_rows(22, 23))

        once = BIBLE.update_sheet(xml, TEXT)

        self.assertEqual(once, BIBLE.update_sheet(once, TEXT))

    def test_rewrites_its_own_row_in_place_when_the_text_changes(self):
        xml = sheet(numbered(3, "1.0", "One") + blank_rows(22, 23))
        once = BIBLE.update_sheet(xml, BIBLE.row_text("2.9", "10", "10"))

        twice = BIBLE.update_sheet(once, BIBLE.row_text("3.0", "10", "10"))

        self.assertEqual(twice.count("Reaction minigame"), 1)
        self.assertIn("pays 3.0 times", twice)
        self.assertNotIn("pays 2.9 times", twice)
        self.assertIn('<c r="A4" s="3"><v>2.0</v></c>', twice)

    def test_goes_after_rows_other_workstreams_added(self):
        xml = sheet(numbered(3, "1.0", "One") + numbered(4, "2.0", "Cooking minigame") + blank_rows(22, 23))

        updated = BIBLE.update_sheet(xml, TEXT)

        self.assertIn('<c r="A5" s="3"><v>3.0</v></c>', updated)
        self.assertLess(updated.index("Cooking minigame"), updated.index("Reaction minigame"))

    def test_refuses_to_run_into_the_rows_below_the_list(self):
        rows = "".join(numbered(n, f"{n - 2}.0", f"Row {n}") for n in range(3, 22))
        xml = sheet(rows + blank_rows(22, 23))

        with self.assertRaises(BIBLE.ReactionBibleError):
            BIBLE.update_sheet(xml, TEXT)

    def test_escapes_the_text(self):
        xml = sheet(numbered(3, "1.0", "One") + blank_rows(22, 22))

        updated = BIBLE.update_sheet(xml, "Espionage poison and trap crafting is now the Reaction minigame & <more>")

        self.assertIn("&amp; &lt;more&gt;", updated)

    def test_the_text_is_plain_ascii_and_names_the_constants_it_reads(self):
        text = BIBLE.row_text(BIBLE.read_multiplier(), BIBLE.read_last_multiplied_level(), BIBLE.read_unlock_rank())

        self.assertTrue(all(ord(character) < 127 for character in text))
        self.assertIn(f"up to level {BIBLE.read_last_multiplied_level()} pays {BIBLE.read_multiplier()} times", text)
        self.assertIn(f"rank {BIBLE.read_unlock_rank()}", text)

    def test_the_committed_workbook_matches_the_rules_in_code(self):
        self.assertFalse(
            BIBLE.update_workbook(BIBLE.DEFAULT_WORKBOOK, check=True),
            "run python tools/UpdateReactionBible.py after changing ReactionRules.LowLevelXpMultiplier, XpMultiplierLastLevel or StabilizeUnlockRank")


if __name__ == "__main__":
    unittest.main()
