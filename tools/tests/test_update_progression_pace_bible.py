import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("update_progression_pace_bible", ROOT / "tools" / "UpdateProgressionPaceBible.py")
BIBLE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BIBLE)

VALUES = {"pace": "1.05", "guild": "50", "experiment": "62", "completion": "31", "level_bonus": "10"}
TEXT = BIBLE.row_text(VALUES)


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


class UpdateProgressionPaceBibleTests(unittest.TestCase):
    def test_the_row_states_every_decision(self):
        for needle in ("1.05 times", "50% of the XP chart's pay", "62% of the chart's XP", "31% for completing", "10% per research level", "Hunter's Guild gives none"):
            self.assertIn(needle, TEXT)

    def test_reads_the_decisions_from_the_source(self):
        values = BIBLE.read_values()

        self.assertEqual(values["pace"], "1.05")
        self.assertEqual(values["guild"], "50")
        self.assertEqual(values["experiment"], "62")
        self.assertEqual(values["completion"], "31")
        self.assertEqual(values["level_bonus"], "10")

    def test_a_missing_constant_is_an_error(self):
        with self.assertRaises(BIBLE.ProgressionPaceBibleError):
            BIBLE.read_constant(ROOT / "tools" / "UpdateProgressionPaceBible.py", "NoSuchConstant", "int")

    def test_adds_the_row_after_the_last_numbered_row_and_before_the_empty_rows(self):
        xml = sheet(numbered(3, "1.0", "One") + numbered(4, "2.0", "Two") + blank_rows(22, 24))

        updated = BIBLE.update_sheet(xml, TEXT)

        self.assertIn('<row r="5"><c r="A5" s="3"><v>3.0</v></c><c r="B5" s="4" t="inlineStr">', updated)
        self.assertLess(updated.index('<row r="5">'), updated.index('<row r="22" />'))
        self.assertIn("Crafting progression pace", updated)

    def test_a_second_run_changes_nothing(self):
        xml = sheet(numbered(3, "1.0", "One") + blank_rows(22, 23))

        once = BIBLE.update_sheet(xml, TEXT)

        self.assertEqual(once, BIBLE.update_sheet(once, TEXT))

    def test_rewrites_its_own_row_in_place_when_the_text_changes(self):
        xml = sheet(numbered(3, "1.0", "One") + blank_rows(22, 23))
        once = BIBLE.update_sheet(xml, BIBLE.row_text({**VALUES, "pace": "1.1"}))

        twice = BIBLE.update_sheet(once, TEXT)

        self.assertEqual(twice.count("Crafting progression pace"), 1)
        self.assertIn("is 1.05 times", twice)
        self.assertNotIn("is 1.1 times", twice)
        self.assertIn('<c r="A4" s="3"><v>2.0</v></c>', twice)

    def test_rewrites_its_own_row_in_place_when_the_text_is_a_shared_string(self):
        # A workbook saved by Google keeps cell text in sharedStrings.xml, so the marker is found through the index.
        existing = '<row r="4"><c r="A4" s="3"><v>2.0</v></c><c r="B4" s="4" t="s"><v>1</v></c></row>'
        xml = sheet(numbered(3, "1.0", "One") + existing + blank_rows(22, 23))
        shared = ["One", "Crafting progression pace: an older wording"]

        updated = BIBLE.update_sheet(xml, TEXT, shared)

        self.assertEqual(updated.count("Crafting progression pace"), 1)
        self.assertNotIn('<row r="5"', updated)
        self.assertIn('<c r="B4" s="4" t="inlineStr">', updated)
        self.assertEqual(updated, BIBLE.update_sheet(updated, TEXT, shared))

    def test_a_shared_string_that_is_not_the_marker_is_left_alone(self):
        existing = '<row r="4"><c r="A4" s="3"><v>2.0</v></c><c r="B4" s="4" t="s"><v>0</v></c></row>'
        xml = sheet(numbered(3, "1.0", "One") + existing + blank_rows(22, 23))

        updated = BIBLE.update_sheet(xml, TEXT, ["Cooking minigame"])

        self.assertIn('<c r="B4" s="4" t="s"><v>0</v></c>', updated)
        self.assertIn('<row r="5"><c r="A5" s="3"><v>3.0</v></c>', updated)

    def test_goes_after_rows_other_workstreams_added(self):
        xml = sheet(numbered(3, "1.0", "One") + numbered(4, "2.0", "Cooking minigame") + blank_rows(22, 23))

        updated = BIBLE.update_sheet(xml, TEXT)

        self.assertIn('<c r="A5" s="3"><v>3.0</v></c>', updated)
        self.assertLess(updated.index("Cooking minigame"), updated.index("Crafting progression pace"))

    def test_refuses_to_run_into_the_rows_below_the_list(self):
        rows = "".join(numbered(n, f"{n - 2}.0", f"Row {n}") for n in range(3, 22))
        xml = sheet(rows + blank_rows(22, 23))

        with self.assertRaises(BIBLE.ProgressionPaceBibleError):
            BIBLE.update_sheet(xml, TEXT)

    def test_escapes_the_text(self):
        xml = sheet(numbered(3, "1.0", "One") + blank_rows(22, 22))

        updated = BIBLE.update_sheet(xml, "Crafting progression pace & <more>")

        self.assertIn("&amp; &lt;more&gt;", updated)

    def test_the_committed_workbook_already_has_the_row_the_code_asks_for(self):
        workbook = ROOT / "design" / "bible" / "SWLOR Design Bible.xlsx"

        self.assertFalse(
            BIBLE.update_workbook(workbook, check=True),
            "the Bible's System Changes & Migrations row is out of date; run tools/UpdateProgressionPaceBible.py",
        )


if __name__ == "__main__":
    unittest.main()
